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
using System.Globalization;

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
    int initialFileSeekPosition;
    int carriageReturnSize;
    Encoding encoding;

    internal ThunderbirdEmailEnumerator(ThunderbirdFolder folder) {
      this.folder = folder;
      this.isRead = false;
      this.isStarred = false;
      this.hasFileReadError = false;

      try {
        this.currentPositionInFile = 0;
        this.initialMessagePosition = 0;
        this.finalMessagePosition = 0;
        this.initialFileSeekPosition = 0;
        this.carriageReturnSize = 0;

        this.fileStream = new FileStream(this.folder.FolderPath,
                                         FileMode.Open,
                                         FileAccess.Read,
                                         FileShare.Read);
        if (this.fileStream.CanSeek) {
          // Get the byte order mark, if there is one.
          byte[] bom = new byte[4];
          this.fileStream.Read(bom, 0, 4);
          if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) {
            this.initialFileSeekPosition = 3;
            this.fileStream.Seek(3, SeekOrigin.Begin);
            this.encoding = Encoding.UTF8;
          } else if ((bom[0] == 0xff && bom[1] == 0xfe)) {
            this.initialFileSeekPosition = 2;
            this.fileStream.Seek(2, SeekOrigin.Begin);
            this.encoding = Encoding.Unicode;
          } else if (bom[0] == 0xfe && bom[1] == 0xff) {
            this.initialFileSeekPosition = 2;
            this.fileStream.Seek(2, SeekOrigin.Begin);
            this.encoding = Encoding.BigEndianUnicode;
          } else if (bom[0] == 0 &&
                     bom[1] == 0 &&
                     bom[2] == 0xfe &&
                     bom[3] == 0xff) {
            // Encoding.UTF32 is not supported in VS2003. We will be returning
            // has fileReadErrors = true in this case. If you are using VS2005,
            // please comment the line "this.hasFileReadError = true;" and
            // uncomment both the lines following it.
            this.hasFileReadError = true;

            // this.initialFileSeekPosition = 4;
            // this.encoding = Encoding.UTF32;
          } else if ((bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 76) &&
                     (bom[3] == 38 ||
                      bom[3] == 39 ||
                      bom[3] == 0x2b ||
                      bom[3] == 0x2f)) {
            this.initialFileSeekPosition = 4;
            this.encoding = Encoding.UTF7;
          } else {
            this.initialFileSeekPosition = 0;
            this.encoding = Encoding.ASCII;
            this.fileStream.Seek(0, SeekOrigin.Begin);
          }

          this.fileStream.Seek(this.initialFileSeekPosition, SeekOrigin.Begin);
          this.currentPositionInFile = this.initialFileSeekPosition;
          this.fileReader = new StreamReader(fileStream, this.encoding);
          this.carriageReturnSize =
              this.encoding.GetByteCount(ThunderbirdConstants.CarriageReturn);

          // Before proceeding with reading the file check if the file reader
          // exists.
          if (!this.hasFileReadError) {
            // Move the file_reader_ to the first "From - " if it exists.
            while (this.fileReader.Peek() != -1) {
              string line = fileReader.ReadLine();

              // We need to add the size of "\r\n" to position depending upon
              // the current encoding. The line read using ReadLine() does not
              // include the carriage return, thus we need to add its size also.
              this.currentPositionInFile +=
                  (encoding.GetByteCount(line) + this.carriageReturnSize);
              if (line.StartsWith(ThunderbirdConstants.MboxMailStart)) {
                break;
              }
            }
          }
        } else {
          this.hasFileReadError = true;
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
            this.finalMessagePosition,
            this.encoding,
            this.carriageReturnSize);
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
            this.currentPositionInFile += this.carriageReturnSize;
            continue;
          }

          this.currentPositionInFile +=
              encoding.GetByteCount(line) + this.carriageReturnSize;

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
              this.currentPositionInFile +=
                  encoding.GetByteCount(line) + this.carriageReturnSize;
              return true;
            }

            // Increment the current position in the file.
            this.currentPositionInFile +=
                encoding.GetByteCount(line) + this.carriageReturnSize;

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
                statusNum = int.Parse(status, NumberStyles.HexNumber);
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
                  this.currentPositionInFile +=
                      encoding.GetByteCount(line) + this.carriageReturnSize;
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
        this.fileStream.Seek(this.initialFileSeekPosition, SeekOrigin.Begin);
        this.fileReader = new StreamReader(fileStream, this.encoding);

        while (this.fileReader.Peek() != -1) {
          string line = fileReader.ReadLine();
          this.currentPositionInFile +=
              encoding.GetByteCount(line) + this.carriageReturnSize;
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