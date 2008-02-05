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
  internal class ThunderbirdFolder : IFolder {
    string folderPath;
    string name;
    uint messageCount;
    FolderKind folderKind;
    IFolder parentFolder;
    IStore store;
    ArrayList subFolders;

    internal ThunderbirdFolder(FolderKind folderKind,
                               string name,
                               string folderPath,
                               IFolder parentFolder,
                               IStore clientStore) {
      this.folderKind = folderKind;
      this.name = name;
      this.folderPath = folderPath;
      this.parentFolder = parentFolder;
      this.store = clientStore;

      this.messageCount = this.CountEmails();
      this.subFolders = new ArrayList();
      this.PopulateFolder();
    }

    public FolderKind Kind {
      get {
        return this.folderKind;
      }
    }

    public uint MailCount {
      get {
        return this.messageCount;
      }
    }

    public string Name {
      get {
        return this.name;
      }
    }

    public IFolder ParentFolder {
      get {
        return this.parentFolder;
      }
    }

    public IStore Store {
      get {
        return this.store;
      }
    }

    public IEnumerable SubFolders {
      get {
        return this.subFolders;
      }
    }

    public IEnumerable Mails {
      get {
        return new ThunderbirdEmailEnumerable(this);
      }
    }

    public string FolderPath {
      get {
        return this.folderPath;
      }
    }

    private uint CountEmails() {
      uint numEmails = 0;
      try {
        using (StreamReader fileReader = new StreamReader(folderPath)) {
          while (fileReader.Peek() != -1) {
            string line = fileReader.ReadLine();
            if (0 == line.IndexOf(ThunderbirdConstants.MboxMailStart)) {
              ++numEmails;
            }

            // Check if the message has been expunged from the mailbox.
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
                // The flow should never reach here if the mbox file is correct.
                // In case it reaches here we will assume that there was an
                // error here and decrement the count of mail and continue.
                --numEmails;
                continue;
              }
              int deleted = statusNum & 0x0008;
              if (deleted > 0) {
                --numEmails;
              }
            }
          }
        }
      } catch (IOException) {
        // There might be 2 reasons for the program to come here.
        // 1. The file does not exist.
        // 2. The file is beign read by some other program.
        // In both cases we should not do anything.
      }
      return numEmails;
    }

    // For every folder check whether a correspoding .sbd is present. If it
    // is present just populate it and add it to subfolder's list.
    private void PopulateFolder() {
      string subFoldersPath = Path.GetFullPath(
          this.folderPath + ThunderbirdConstants.ThunderbirdDir);
      
      bool doesSubFolderExist = Directory.Exists(subFoldersPath);
      if (!doesSubFolderExist) {
        return;
      }

      foreach (string filePath in
          Directory.GetFiles(subFoldersPath,
                             ThunderbirdConstants.StarDotMSF)) {
        string fileName = Path.GetFileName(filePath);

        int msfIndex =
            fileName.LastIndexOf(ThunderbirdConstants.ThunderbirdMSFExtension);
        int fileLength = fileName.Length;

        // Check to see if there is an msf file present. If it is, check to see
        // if the corresponding msf file is present. This is introduced  because
        // there were cases in which msf files were present but mbox files were
        // not present which was causing scav to crash.
        if (msfIndex != -1 &&
            fileLength != ThunderbirdConstants.ThunderbirdMSFLen &&
            (msfIndex + ThunderbirdConstants.ThunderbirdMSFLen == fileLength)) {
          // Strip out the msf extension and check whether the file exists.
          string tempFileName = fileName.Substring(0, msfIndex);
          bool doesFileExist = false;
          doesFileExist = File.Exists(Path.Combine(subFoldersPath,
                                                   tempFileName));
          if (!doesFileExist) {
            continue;
          }

          FolderKind folderKind = this.GetFolderKind(tempFileName);
          ThunderbirdFolder folder = new ThunderbirdFolder(
              folderKind,
              tempFileName,
              Path.Combine(subFoldersPath, tempFileName),
              this,
              this.store);
      
          this.subFolders.Add(folder);
        }
      }
    }

    private FolderKind GetFolderKind(string folderName) {
      switch (folderName) {
        case ThunderbirdConstants.Inbox:
          return FolderKind.Inbox;
        case ThunderbirdConstants.Drafts:
          return FolderKind.Draft;
        case ThunderbirdConstants.Sent:
          return FolderKind.Sent;
        case ThunderbirdConstants.Trash:
          return FolderKind.Trash;
        default:
          return FolderKind.Other;
      }
    }
  }
}