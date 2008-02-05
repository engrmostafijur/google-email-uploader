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
using System.Collections.Generic;
using System.Text;
using Google.MailClientInterfaces;
using System.Collections;

namespace GoogleEmailUploaderTestScript {
  class TestClient : IClient {
    ArrayList loadedStores;
    ArrayList loadedStoreFileNames;
    Hashtable createdFileStores;

    internal TestClient() {
      this.loadedStores = new ArrayList();
      this.loadedStoreFileNames = new ArrayList();
      this.createdFileStores = new Hashtable();
    }

    internal void AddStore(TestStore store) {
      this.loadedStores.Add(store);
    }

    internal void CreateStore(string fileName,
                              TestStore store) {
      this.createdFileStores.Add(fileName, store);
    }

    internal void ClearStores() {
      this.loadedStores.Clear();
      this.loadedStoreFileNames.Clear();
      this.createdFileStores.Clear();
    }

    #region IClient Members

    public string Name {
      get {
        return "Test Client";
      }
    }

    public IEnumerable Stores {
      get {
        return this.loadedStores;
      }
    }

    public IStore OpenStore(string filename) {
      TestStore store = this.createdFileStores[filename] as TestStore;
      if (store != null) {
        this.loadedStores.Add(store);
      }
      return store;
    }

    public IEnumerable LoadedStoreFileNames {
      get {
        return this.loadedStoreFileNames;
      }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
    }

    #endregion
  }

  class TestStore : IStore {
    TestClient testClient;
    string displayName;
    string persistName;
    ArrayList folders;

    internal TestStore(TestClient testClient,
                       string displayName,
                       string persistName) {
      this.testClient = testClient;
      this.displayName = displayName;
      this.persistName = persistName;
      this.folders = new ArrayList();
    }

    internal void AddFolder(TestFolder testFolder) {
      this.folders.Add(testFolder);
    }

    #region IStore Members

    public IClient Client {
      get {
        return this.testClient;
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

    #endregion
  }

  class TestFolder : IFolder {
    TestStore store;
    TestFolder parentFolder;
    FolderKind folderKind;
    string name;
    uint mailCount;
    ArrayList subfolders;

    internal TestFolder(TestStore store,
                        TestFolder parentFolder,
                        FolderKind folderKind,
                        string name,
                        uint mailCount) {
      this.store = store;
      this.parentFolder = parentFolder;
      this.folderKind = folderKind;
      this.name = name;
      this.mailCount = mailCount;
      this.subfolders = new ArrayList();
    }

    internal void AddSubFolder(TestFolder subFolder) {
      this.subfolders.Add(subFolder);
    }

    #region IFolder Members

    public FolderKind Kind {
      get {
        return this.folderKind;
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

    public string Name {
      get {
        return this.name;
      }
    }

    public IEnumerable SubFolders {
      get {
        return this.subfolders;
      }
    }

    public uint MailCount {
      get {
        return this.mailCount;
      }
    }

    public IEnumerable Mails {
      get {
        ArrayList arrayList = new ArrayList();
        for (int i = 0; i < this.mailCount; ++i) {
          arrayList.Add(new TestMail(this,
                                     this.name + i.ToString(),
                                     i % 3 == 0,
                                     i % 2 == 0,
                                     (uint)i * 20));
        }
        return arrayList;
      }
    }

    #endregion
  }

  class TestMail : IMail {
    TestFolder folder;
    string mailId;
    bool isRead;
    bool isStarred;
    uint messageSize;

    internal TestMail(TestFolder folder,
                      string mailId,
                      bool isRead,
                      bool isStarred,
                      uint messageSize) {
      this.folder = folder;
      this.mailId = mailId;
      this.isRead = isRead;
      this.isStarred = isStarred;
      this.messageSize = messageSize;
    }

    #region IMail Members

    public IFolder Folder {
      get {
        return this.folder;
      }
    }

    public string MailId {
      get { return this.mailId; }
    }

    public bool IsRead {
      get { return this.isRead; }
    }

    public bool IsStarred {
      get { return this.isStarred; }
    }

    public uint MessageSize {
      get { return this.messageSize; }
    }

    public byte[] Rfc822Buffer {
      get {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(mailId);
        sb.AppendLine(mailId);
        sb.AppendLine(mailId);
        sb.AppendLine(mailId);
        sb.AppendLine(mailId);
        return Encoding.UTF8.GetBytes(sb.ToString());
      }
    }

    #endregion

    #region IDisposable Members

    public void Dispose() {
    }

    #endregion
  }
}
