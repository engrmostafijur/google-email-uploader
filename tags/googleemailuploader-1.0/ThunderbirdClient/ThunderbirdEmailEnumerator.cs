// Copyright 2007 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
//
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.IO;
using System.Text;

using Google.MailClientInterfaces;

namespace Google.Thunderbird {
  internal class ThunderbirdEmailEnumerator : IEnumerator,
                                              IDisposable {
    FileStream fileStream;
    StreamReader fileReader;
    ThunderbirdFolder folder;
    string currentMessageId;
    bool isRead;
    bool isStarred;
    bool hasFileReadError;
    long currentPositionInFile;
    long initialMessagePosition;
    long finalMessagePosition;
    Encoding encoding;

    internal ThunderbirdEmailEnumerator(ThunderbirdFolder folder) {
      this.folder = folder;
      this.isRead = false;
      this.isStarred = false;
      this.hasFileReadError = false;

      try {
        this.fileStream = File.OpenRead(this.folder.FolderPath);
        this.fileReader = new StreamReader(fileStream, Encoding.Default);
        this.encoding = Encoding.Default;

        this.currentPositionInFile = 0;
        this.initialMessagePosition = 0;
        this.finalMessagePosition = 0;

        // Before proceeding with reading the file check if the file reader
        // exists.
        if (!this.hasFileReadError) {
          // Move the file_reader_ to the first "From - " if it exists.
          while (this.fileReader.Peek() != -1) {
            string line = fileReader.ReadLine();

            // We need to add 2 to position because of the presence of "\r\n".
            this.currentPositionInFile += (encoding.GetByteCount(line) + 2);
            if (line.StartsWith(ThunderbirdConstants.MboxMailStart)) {
              break;
            }
          }
        }
      } catch (IOException) {
        // There might be 2 reasons for the program to come here.
        // 1. The file does not exist.
        // 2. The file is beign read by some other program.
        this.hasFileReadError = true;
      }
    }

    public Object Current {
      get {
        return new ThunderbirdEmailMessage(
            this.folder,
            this.currentMessageId,
            this.isRead,
            this.isStarred,
            this.initialMessagePosition,
            this.finalMessagePosition);
      }
    }

    public bool MoveNext() {
      if (this.hasFileReadError) {
        return false;
      }

      try {
        // From - is not a part of rfc822. This initialization should take care 
        // of it
        this.initialMessagePosition = this.currentPositionInFile;

        // Check for the end of stream. Return false if we hit it.
        if (this.fileReader.Peek() == -1) {
          return false;
        }

        while (this.fileReader.Peek() != -1) {
          bool isFirstMessageId = true;
          string line = this.fileReader.ReadLine();

          // Consume all the blank lines before we reach the next message or
          // eof.
          if ((0 == line.Length) && (this.fileReader.Peek() != -1)) {
            this.currentPositionInFile += 2;
            continue;
          }

          this.currentPositionInFile += encoding.GetByteCount(line) + 2;

          // If we have reached the eof return false. Also mark the finish
          // offset.
          if (this.fileReader.Peek() == -1) {
            this.finalMessagePosition = this.currentPositionInFile;
            return false;
          }

          while (this.fileReader.Peek() != -1) {
            line = this.fileReader.ReadLine();

            // See if we have "From - " at the beginning of the line. If it is
            // we have reached the next message. Initialize the end of the
            // current message and exit the loop.
            if (line.StartsWith(ThunderbirdConstants.MboxMailStart)) {
              this.finalMessagePosition = this.currentPositionInFile;

              // Increment the current position in file as we have not done it.
              this.currentPositionInFile += encoding.GetByteCount(line) + 2;
              return true;
            }

            // Increment the current position in the file.
            this.currentPositionInFile += encoding.GetByteCount(line) + 2;

            // If we find the message-id of the current message, populate the
            // variable message_id_.
            if (isFirstMessageId &&
                line.ToLower().StartsWith(
                    ThunderbirdConstants.MessageIDStart)) {
              int endingIndex =
                  line.IndexOf(ThunderbirdConstants.MessageIDEnd);
              string messageId = line.Substring(
                  ThunderbirdConstants.MessageIdLen,
                  endingIndex - ThunderbirdConstants.MessageIdLen);
              this.currentMessageId = messageId;
              isFirstMessageId = false;
            }

            // If we find X-Mozilla-Status, set up the flags.
            if (line.StartsWith(ThunderbirdConstants.XMozillaStatus)) {
              int xMozillaStatusLen =
                  ThunderbirdConstants.XMozillaStatus.Length;
              string status = line.Substring(
                  xMozillaStatusLen - 1,
                  line.Length - xMozillaStatusLen + 1);
              int statusNum = 0;
              try {
                statusNum = int.Parse(status);
              } catch {
                // We should never come here if the mbox file is correctly
                // written. In case we come here we move to default behavior
                // which is unread, unstarred and not deleted and continue with
                // building the message.
                continue;
              }

              int read = statusNum & 0x0001;
              isRead = (read > 0);

              int starred = statusNum & 0x0004;
              isStarred = (starred > 0);

              // If the message has been expunged from the mbox, find the next
              // "From - ".
              int deleted = statusNum & 0x0008;
              if (deleted > 0) {
                while (this.fileReader.Peek() != -1) {
                  isFirstMessageId = false;
                  line = this.fileReader.ReadLine();
                  this.currentPositionInFile += encoding.GetByteCount(line) + 2;
                  if (line.IndexOf(
                      ThunderbirdConstants.MboxMailStart) == 0) {
                    this.initialMessagePosition = this.currentPositionInFile;
                    break;
                  }
                }

                if (this.fileReader.Peek() == -1) {
                  return false;
                }
              }
            }
          }

          // If we reach the eof on this control path, we have the last email to
          // take care of. In that case just add the last line.
          this.finalMessagePosition = this.currentPositionInFile;
          return true;
        }

        return true;
      } catch (IOException) {
        this.hasFileReadError = true;
        return false;
      }
    }

    public void Reset() {
      try {
        this.fileReader.Close();
        this.fileStream.Close();

        this.currentPositionInFile = 0;
        this.initialMessagePosition = 0;
        this.finalMessagePosition = 0;
        this.hasFileReadError = false;

        this.fileStream = File.OpenRead(this.folder.FolderPath);
        this.fileReader = new StreamReader(fileStream, Encoding.Default);

        while (this.fileReader.Peek() != -1) {
          string line = fileReader.ReadLine();
          this.currentPositionInFile += encoding.GetByteCount(line) + 2;
          if (0 == line.IndexOf(ThunderbirdConstants.MboxMailStart)) {
            break;
          }
        }
      } catch {
        // There might be 2 reasons for the program to come here.
        // 1. The file does not exist.
        // 2. The file is beign read by some other program.
        this.hasFileReadError = true;
        return;
      }
    }

    public void Dispose() {
      this.fileReader.Close();
      this.fileStream.Close();
    }
  }
}