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
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Microsoft.Win32;

using Google.MailClientInterfaces;

namespace Google.Thunderbird {
  public class ThunderbirdClientFactory : IClientFactory {
    ArrayList processNames;

    public ThunderbirdClientFactory() {
      this.processNames = new ArrayList();
      this.processNames.Add("thunderbird");
    }

    public IEnumerable ClientProcessNames {
      get { return this.processNames; }
    }

    public IClient CreateClient() {
      if (this.DetectThunderbird()) {
        return new ThunderbirdClient();
      } else {
        return null;
      }
    }

    // Detects whether thunderbird exists or not. For checking whether it
    // exists, we probe HKCU\SOFTWARE\Clients\Mozilla Thunderbird to see if
    // it exists. If it does then we return true else false is returned.
    private bool DetectThunderbird() {
      RegistryKey regKey = Registry.LocalMachine;
      regKey = regKey.OpenSubKey(
          "SOFTWARE\\Clients\\Mail\\Mozilla Thunderbird");
      if (null == regKey) {
        return false;
      }

      regKey.Close();
      return true;
    }
  }

  internal class ThunderbirdClient : IClient {
    ArrayList storeFileNames;
    ArrayList stores;
    ArrayList profiles;

    internal ThunderbirdClient() {
      this.profiles = new ArrayList();
      this.stores = new ArrayList();
      this.storeFileNames = new ArrayList();

      this.PopulateProfiles();
      foreach (ThunderbirdProfile thunderbirdProfile in this.profiles) {
        foreach (ThunderbirdStore thunderbirdStore in
            thunderbirdProfile.GetStores()) {
          this.stores.Add(thunderbirdStore);
        }
      }
    }

    // The name of the client.
    public string Name {
      get {
        return ThunderbirdConstants.ClientName;
      }
    }

    public bool SupportsContacts {
      get { return false; }
    }

    public bool SupportsLoadingStore {
      get {
        return true;
      }
    }

    public IEnumerable Stores {
      get {
        return this.stores;
      }
    }

    public IEnumerable LoadedStoreFileNames {
      get {
        return this.storeFileNames;
      }
    }

    public IStore OpenStore(string filePath) {
      filePath = Path.GetFullPath(filePath);
      // Check whether the mbox file has already been added to the list of
      // stores.
      if (this.IsStoreAdded(filePath)) {
        return null;
      }

      // Check if the file is being read by any other program. If it is
      // being read do not add it to the list.
      try {
        using (FileStream fileStream = File.OpenRead(filePath)) {
          // Just checking if the file can be opened. We don't need to do
          // anything. Using will take care of closing the filestream.
        }
      } catch (IOException) {
        return null;
      }

      ThunderbirdStore store = new ThunderbirdStore(filePath, this);
      this.stores.Add(store);
      this.storeFileNames.Add(filePath);
      return store;
    }

    public void Dispose() {
    }

    // Checks if the store is already added.
    private bool IsStoreAdded(string filePath) {
      foreach (ThunderbirdStore store in this.stores) {
        if (this.IsFolderPresentInStore(store, filePath)) {
          return true;
        }
      }
      return false;
    }

    // Checks whether the mbox file is present in a folder.
    private bool IsFolderPresentInStore(ThunderbirdStore store,
                                       string filePath) {
      foreach (ThunderbirdFolder folder in store.Folders) {
        if (this.IsFolderPresentInFolder(folder, filePath)) {
          return true;
        }
      }
      return false;
    }

    // Checks whether the folder is a subfolder of the folder.
    private bool IsFolderPresentInFolder(ThunderbirdFolder folder,
                                        string filePath) {
      if (folder.FolderPath.Equals(filePath)) {
        return true;
      }

      foreach (ThunderbirdFolder childFolder in folder.SubFolders) {
        if (IsFolderPresentInFolder(childFolder, filePath)) {
          return true;
        }
      }
      return false;
    }


    // Populate the profiles vector.
    private void PopulateProfiles() {
      string searchPath;
      searchPath = Path.GetFullPath(
          Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
      searchPath = Path.Combine(searchPath, ThunderbirdConstants.ProfilePath);
      if (!Directory.Exists(searchPath)) {
        return;
      }

      foreach (string profileName in Directory.GetDirectories(searchPath)) {
        string tempProfileName = Path.GetFileName(profileName);
        ThunderbirdProfile profile = new ThunderbirdProfile(
            this,
            tempProfileName,
            Path.Combine(searchPath, profileName));
        this.profiles.Add(profile);
      }
    }
  }
}
