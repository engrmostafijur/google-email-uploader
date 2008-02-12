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
using System.IO;
using System.Text;

using Google.MailClientInterfaces;

namespace Google.Thunderbird {
  internal class ThunderbirdEmailMessage : IMail {
    long initialMessagePosition;
    long finalMessagePosition;
    ThunderbirdFolder folder;
    string mailId;
    bool read;
    bool starred;
    uint messageSize;
    byte[] message;

    internal ThunderbirdEmailMessage(ThunderbirdFolder folder,
                                     string mailId,
                                     bool read,
                                     bool starred,
                                     long initialMessagePosition,
                                     long finalMessagePosition) {
      this.folder = folder;
      this.mailId = mailId;
      this.read = read;
      this.starred = starred;
      this.initialMessagePosition = initialMessagePosition;
      this.finalMessagePosition = finalMessagePosition;
      this.messageSize = 
          (uint)(this.finalMessagePosition - this.initialMessagePosition);
    }

    public IFolder Folder {
      get {
        return this.folder;
      }
    }

    public string MailId {
      get {
        return this.mailId;
      }
    }

    public bool IsRead {
      get {
        return this.read;
      }
    }

    public bool IsStarred {
      get {
        return this.starred;
      }
    }

    public uint MessageSize {
      get {
        return this.messageSize;
      }
    }

    public void Dispose() {
    }

    public byte[] Rfc822Buffer {
      get {
        if (this.message != null) {
          return this.message;
        }

        try {
          Encoding encoding;
          using (FileStream fileReader = File.OpenRead(folder.FolderPath)) {
            fileReader.Seek(this.initialMessagePosition, SeekOrigin.Begin);
            using (StreamReader fileStreamReader =
                new StreamReader(fileReader, Encoding.Default)) {
              bool hasFromField = false;
              bool hasDateField = false;

              encoding = fileStreamReader.CurrentEncoding;
              StringBuilder messageString = new StringBuilder();
              messageString.Capacity = (int)this.messageSize;
              string line = "";
              int count = 0;

              while (count < this.messageSize) {
                line = fileStreamReader.ReadLine();
                
                if (!hasFromField &&
                    line.StartsWith(ThunderbirdConstants.MandatoryFromField)) {
                  hasFromField = true;
                }

                if (!hasDateField &&
                    line.StartsWith(ThunderbirdConstants.MandatoryDateField)) {
                  hasDateField = true;
                }

                count += encoding.GetByteCount(line) + 2;

                // Considering the boundary condition here.
                if (count >= this.messageSize) {
                  break;
                }
                messageString.Append(line);
                messageString.Append("\r\n");
              }

              // Check whether the message complies with the rfc882
              // specifications.
              if (hasFromField && hasDateField) {
                this.message = encoding.GetBytes(messageString.ToString());
              } else {
                this.message = new byte[0];
              }
            }
          }
        } catch {
          this.message = new byte[0];
        }
        return this.message;
      }
    }
  }
}