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
using System.Runtime.InteropServices;
using System.IO;

namespace Google.Thunderbird {
  internal class ThunderbirdProfile {
    ThunderbirdClient client;
    string profileName;
    string profilePath;
    ArrayList stores;

    internal ThunderbirdProfile(ThunderbirdClient client,
                                string profileName,
                                string profilePath) {
      this.client = client;
      this.stores = new ArrayList();
      this.profileName = profileName;
      this.profilePath = profilePath;
      this.PopulateProfile();
    }


    public ArrayList GetStores() {
      return this.stores;
    }

    public ThunderbirdClient GetClient() {
      return this.client;
    }

    private void PopulateProfile() {
      string storeSearchPath = Path.GetFullPath(this.profilePath);
      storeSearchPath = Path.Combine(storeSearchPath,
                                     ThunderbirdConstants.Mail);
      // Check if the store path exist. If it does not exists, quit.
      if (!Directory.Exists(storeSearchPath)) {
        return;
      }

      foreach (string storePath in Directory.GetDirectories(storeSearchPath)) {
        string storeName = Path.GetFileName(storePath);
        ThunderbirdStore store =
            new ThunderbirdStore(this,
                                 storeName,
                                 storeName,
                                 storePath);
        this.stores.Add(store);
      }
    }
  }
}