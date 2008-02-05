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
using System.Text;
using System.IO;

using Google.MailClientInterfaces;

namespace Google.Thunderbird {
  internal class ThunderbirdStore : IStore {
    ThunderbirdProfile profile;
    IClient client;
    ArrayList folders;
    string persistName;
    string displayName;
    string storePath;

    internal ThunderbirdStore(ThunderbirdProfile profile,
                              string persistName,
                              string displayName,
                              string storePath) {
      this.profile = profile;
      this.persistName = persistName;
      this.displayName = displayName;
      this.client = profile.GetClient();
      this.storePath = storePath;

      this.folders = new ArrayList();
      this.PopulateStore();
    }

    public ThunderbirdStore(string storePath,
                            ThunderbirdClient client) {
      this.storePath = Path.GetDirectoryName(storePath);
      this.persistName = Path.GetFileName(this.storePath);
      this.displayName = this.persistName;
      this.profile = null;
      this.client = client;
      this.folders = new ArrayList();

      string folderName = Path.GetFileName(storePath);
      FolderKind folderKind = this.GetFolderKind(folderName);
      ThunderbirdFolder folder = new ThunderbirdFolder(
          folderKind,
          folderName,
          storePath,
          null,
          this);
      this.folders.Add(folder);
    }

    public IClient Client {
      get {
        return this.client;
      }
    }

    public string PersistName {
      get {
        return this.persistName;
      }
    }

    public string DisplayName {
      get {
        return this.displayName;
      }
    }

    public IEnumerable Folders {
      get {
        return this.folders;
      }
    }

    public void Dispose() {
    }

    public string StorePath {
      get {
        return this.storePath;
      }
    }

    // For every store, we will check for all the msf files present. Then, we
    // will check if the corresponding mbox files are present. If they are, we
    // will create a new folder and then add them to our folders_ list.
    public void PopulateStore() {
      string folderPath = System.IO.Path.GetFullPath(this.storePath);
      foreach (string filePath in
          Directory.GetFiles(folderPath,
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
          doesFileExist = File.Exists(Path.Combine(folderPath, tempFileName));
          if (!doesFileExist) {
            continue;
          }

          FolderKind folderKind = this.GetFolderKind(tempFileName);
          ThunderbirdFolder folder =
              new ThunderbirdFolder(folderKind,
                                    tempFileName,
                                    Path.Combine(folderPath, tempFileName),
                                    null,
                                    this);
          folders.Add(folder);
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