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

using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Xml;

// Sample of persisted xml
// <GoogleEmailUploader>
//   <User EMailId="foo@bar.com">
//     <Client Name="Microsoft Outlook" SelectionState="True">
//       <Store DisplayName="Personal Folders" Persist="Personal Folders" SelectionState="True">
//         <Folder Name="Deleted Items" SelectionState="True" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="Inbox" SelectionState="True" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="Sent Items" SelectionState="True" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="Drafts" SelectionState="True" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="dd" SelectionState="True" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="">
//           <Folder Name="ff" SelectionState="True" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         </Folder>
//       </Store>
//     </Client>
//     <Client Name="Outlook Express" SelectionState="False">
//       <Store DisplayName="OE Store" Persist="OE Store" SelectionState="False">
//         <Folder Name="Bar" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="">
//           <Folder Name="Car" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="">
//             <Folder Name="Tar" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//           </Folder>
//         </Folder>
//         <Folder Name="Deleted Items" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="Drafts" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="Inbox" SelectionState="False" UploadedMailCount="1" FailedMailCount="0" LastUploadedMailId="24" />
//         <Folder Name="Outbox" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//         <Folder Name="Sent Items" SelectionState="False" UploadedMailCount="0" FailedMailCount="0" LastUploadedMailId="" />
//       </Store>
//     </Client>
//   </User>
// </GoogleEmailUploader>

namespace GoogleEmailUploader {

  /// <summary>
  /// This class is responsible for persisting and restoring the LKG state.
  /// The LKG state is persisted by walking the Model tree and storing the
  /// state of each model. For all models we store/restore selection state.
  /// For clients, we also store/restore all the loaded stores.
  /// For folders, we store/restore failed mail count, uplaoded mail count and
  /// last uploaded mailid.
  /// The persistence is done as XML. The expected size of this will be at most
  /// in tens of KB's, so we are storing this as XML DOM rather tan using more
  /// efficent but hard to use XML reader/writer.
  /// </summary>
  class LKGStatePersistor {
    const string GoogleEmailUploaderElementName = "GoogleEmailUploader";
    const string UserElementName = "User";
    const string ClientElementName = "Client";
    const string LoadedStoreElementName = "LoadedStore";
    const string StoreElementName = "Store";
    const string FolderElementName = "Folder";
    const string FailedMailName = "FailedMail";
    const string ReasonAttrName = "Reason";
    const string EmailIdAttrName = "EmailId";
    const string ArchiveEverythingAttrName = "ArchiveEverything";
    const string FolderToLabelMappingAttrName = "FolderToLabelMapping";
    const string UploadSpeedAttrName = "UploadSpeed";
    const string NameAttrName = "Name";
    const string SelectionStateAttrName = "SelectionState";
    const string DisplayNameAttrName = "DisplayName";
    const string PathAttrName = "Path";
    const string PersistNameAttrName = "Persist";
    const string UploadedMailCountAttrName = "UploadedMailCount";
    const string FailedMailCountAttrName = "FailedMailCount";
    const string LastUploadedMailIdAttrName = "LastUploadedMailId";

    readonly string lkgStateFilePath;
    readonly string emailId;
    readonly XmlDocument xmlDocument;
    readonly XmlElement googleEmailUploaderXmlElement;
    readonly XmlElement userXmlElement;

    internal LKGStatePersistor(string emailId) {
      string assemblyDirectory =
          Path.GetDirectoryName(this.GetType().Assembly.Location);
      this.lkgStateFilePath =
          Path.Combine(assemblyDirectory,
                       "UserData.xml");
      this.emailId = emailId;
      if (!File.Exists(this.lkgStateFilePath)) {
        this.xmlDocument =
            this.CreateEmptyDocument(out this.googleEmailUploaderXmlElement);
      } else {
        try {
          this.xmlDocument = new XmlDocument();
          this.xmlDocument.Load(this.lkgStateFilePath);
          if (this.xmlDocument.ChildNodes.Count == 1) {
            this.googleEmailUploaderXmlElement =
                this.xmlDocument.ChildNodes[0] as XmlElement;
            if (this.googleEmailUploaderXmlElement == null ||
                this.googleEmailUploaderXmlElement.Name
                    != LKGStatePersistor.GoogleEmailUploaderElementName
            ) {
              this.xmlDocument =
                  this.CreateEmptyDocument(
                      out this.googleEmailUploaderXmlElement);
            }
          } else {
            this.xmlDocument =
                this.CreateEmptyDocument(
                    out this.googleEmailUploaderXmlElement);
          }
        } catch (XmlException) {
          this.xmlDocument =
              this.CreateEmptyDocument(out this.googleEmailUploaderXmlElement);
        }
      }
      this.userXmlElement = this.GetUserXmlElement();
      this.xmlDocument.Save(this.lkgStateFilePath);
    }

    XmlDocument CreateEmptyDocument(
        out XmlElement googleEmailClientXmlElement) {
      XmlDocument xmlDocument = new XmlDocument();
      googleEmailClientXmlElement =
          xmlDocument.CreateElement(
              LKGStatePersistor.GoogleEmailUploaderElementName);
      xmlDocument.AppendChild(googleEmailClientXmlElement);
      return xmlDocument;
    }

    XmlElement GetUserXmlElement() {
      foreach (XmlElement xmlElement in
          this.googleEmailUploaderXmlElement.ChildNodes) {
        if (xmlElement.Name == LKGStatePersistor.UserElementName) {
          string emailId =
              xmlElement.GetAttribute(LKGStatePersistor.EmailIdAttrName);
          if (emailId == this.emailId) {
            return xmlElement;
          }
        }
      }
      XmlElement newXmlElement =
          this.xmlDocument.CreateElement(LKGStatePersistor.UserElementName);
      newXmlElement.SetAttribute(
          LKGStatePersistor.EmailIdAttrName,
          this.emailId);
      this.googleEmailUploaderXmlElement.AppendChild(newXmlElement);
      return newXmlElement;
    }

    static void LoadSelectedState(
        XmlElement xmlElement,
        TreeNodeModel treeNodeModel) {
      if (xmlElement.GetAttribute(LKGStatePersistor.SelectionStateAttrName)
            == bool.FalseString) {
        treeNodeModel.IsSelected = false;
      } else {
        treeNodeModel.IsSelected = true;
      }
    }

    /// <summary>
    /// Loads the folder state i.e. SelectionState, UploadedMailCount,
    /// FailedMailCount, LastUploadedMailId. Then it recurses on the
    /// sub folders.
    /// </summary>
    void LoadFolderModelState(XmlElement folderXmlElement,
                              FolderModel folderModel) {
      LKGStatePersistor.LoadSelectedState(
          folderXmlElement,
          folderModel);
      string uploadedMailCount =
          folderXmlElement.GetAttribute(
              LKGStatePersistor.UploadedMailCountAttrName);
      try {
        folderModel.UploadedMailCount = uint.Parse(uploadedMailCount);
      } catch {
        folderModel.UploadedMailCount = 0;
      }
      string failedMailCount =
          folderXmlElement.GetAttribute(
              LKGStatePersistor.FailedMailCountAttrName);
      try {
        folderModel.FailedMailCount = uint.Parse(failedMailCount);
      } catch {
        folderModel.FailedMailCount = 0;
      }
      string lastUploadedMailId =
          folderXmlElement.GetAttribute(
              LKGStatePersistor.LastUploadedMailIdAttrName);
      folderModel.LastUploadedMailId = lastUploadedMailId;
      foreach (XmlNode childXmlNode in folderXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.FailedMailName) {
          continue;
        }
        string failureReason =
            childXmlElement.GetAttribute(LKGStatePersistor.ReasonAttrName);
        if (failureReason == null) {
          failureReason = "Unknown";
        }
        FailedMailDatum failedMailDatum =
          new FailedMailDatum(childXmlElement.InnerText, failureReason);
        folderModel.FailedMailData.Add(failedMailDatum);
      }
      this.LoadFolderModelsState(
          folderXmlElement.ChildNodes,
          folderModel.Children);
    }

    /// <summary>
    /// Loads the state of list of folders. This is done by walking the xml and
    /// finding corresponding folder model and loading it.
    /// </summary>
    void LoadFolderModelsState(XmlNodeList folderXmlElements,
                               IEnumerable folders) {
      foreach (XmlNode childXmlNode in folderXmlElements) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.FolderElementName) {
          continue;
        }
        string folderName =
            childXmlElement.GetAttribute(LKGStatePersistor.NameAttrName);
        foreach (FolderModel folderModel in folders) {
          if (folderName != folderModel.Folder.Name) {
            continue;
          }
          this.LoadFolderModelState(
              childXmlElement,
              folderModel);
        }
      }
    }

    /// <summary>
    /// Loads the selection state of the store, and then loads all the
    /// state of all the subfolders.
    /// </summary>
    void LoadStoreModelState(XmlElement storeXmlElement,
                             StoreModel storeModel) {
      LKGStatePersistor.LoadSelectedState(
          storeXmlElement,
          storeModel);
      this.LoadFolderModelsState(
          storeXmlElement.ChildNodes,
          storeModel.Children);
    }

    /// <summary>
    /// Restores the selection state of the given client model, and then
    /// walks the xml and loads all the loaded stores. Then it goes on to load
    /// the state of each store.
    /// </summary>
    void LoadClientModelState(XmlElement clientXmlElement,
                              ClientModel clientModel) {
      LKGStatePersistor.LoadSelectedState(
          clientXmlElement,
          clientModel);
      foreach (XmlNode childXmlNode in clientXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.LoadedStoreElementName) {
          continue;
        }
        string filePath =
            childXmlElement.GetAttribute(LKGStatePersistor.PathAttrName);
        if (filePath == null || filePath.Length == 0) {
          continue;
        }
        clientModel.OpenStore(filePath);
      }
      foreach (XmlNode childXmlNode in clientXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.StoreElementName) {
          continue;
        }
        string storeDisplayName =
            childXmlElement.GetAttribute(LKGStatePersistor.DisplayNameAttrName);
        string storePersistName =
            childXmlElement.GetAttribute(LKGStatePersistor.PersistNameAttrName);
        foreach (StoreModel storeModel in clientModel.Children) {
          if (storeDisplayName != storeModel.Store.DisplayName ||
              storePersistName != storeModel.Store.PersistName) {
            continue;
          }
          this.LoadStoreModelState(
              childXmlElement,
              storeModel);
        }
      }
    }

    /// <summary>
    /// Loads the LKG state of the list of clients using the information
    /// persisted.
    /// This is done by walking the xml tree and finding the client model
    /// corresponding to the tree node and restoring its state.
    /// </summary>
    internal void LoadLKGState(GoogleEmailUploaderModel uploaderModel) {
      string archiveEverythingString =
          this.userXmlElement.GetAttribute(
              LKGStatePersistor.ArchiveEverythingAttrName);
      bool archiveEverything = false;
      try {
        archiveEverything = bool.Parse(archiveEverythingString);
      } catch {
        // If we get an exception we assume by default we do not archive
        // everything
      }
      uploaderModel.SetArchiving(archiveEverything);
      string labelMappingString =
          this.userXmlElement.GetAttribute(
              LKGStatePersistor.FolderToLabelMappingAttrName);
      bool labelMapping = true;
      try {
        labelMapping = bool.Parse(labelMappingString);
      } catch {
        // If we get an exception we assume by default we do label mapping
      }
      uploaderModel.SetFolderToLabelMapping(labelMapping);
      string uploadSpeedString =
          this.userXmlElement.GetAttribute(
              LKGStatePersistor.UploadSpeedAttrName);
      double uploadSpeed;
      try {
        uploadSpeed = double.Parse(uploadSpeedString);
        uploaderModel.SetUploadSpeed(uploadSpeed);
      } catch {
        // If we get an exception we dont update the speed. Let the speed
        // be whatever is the speed for the test upload.
      }
      foreach (XmlNode childXmlNode in this.userXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.ClientElementName) {
          continue;
        }
        string clientName =
            childXmlElement.GetAttribute(LKGStatePersistor.NameAttrName);
        foreach (ClientModel clientModel in uploaderModel.ClientModels) {
          if (clientName != clientModel.Client.Name) {
            continue;
          }
          this.LoadClientModelState(
              childXmlElement,
              clientModel);
        }
      }
    }

    /// <summary>
    /// Persists the selection state, uploaded mail count, failed mail count and
    /// last uplaoded mail id. Ten recurses to persist all the sub folders.
    /// </summary>
    void SaveFolderModelState(XmlElement parentXmlElement,
                              FolderModel folderModel) {
      XmlElement folderXmlElement =
          this.xmlDocument.CreateElement(LKGStatePersistor.FolderElementName);
      parentXmlElement.AppendChild(folderXmlElement);
      folderXmlElement.SetAttribute(
          LKGStatePersistor.NameAttrName,
          folderModel.Folder.Name);
      folderXmlElement.SetAttribute(
          LKGStatePersistor.SelectionStateAttrName,
          folderModel.IsSelected.ToString());
      folderXmlElement.SetAttribute(
          LKGStatePersistor.UploadedMailCountAttrName,
          folderModel.UploadedMailCount.ToString());
      folderXmlElement.SetAttribute(
          LKGStatePersistor.FailedMailCountAttrName,
          folderModel.FailedMailCount.ToString());
      folderXmlElement.SetAttribute(
          LKGStatePersistor.LastUploadedMailIdAttrName,
          folderModel.LastUploadedMailId);
      foreach (FailedMailDatum failedMailDatum in folderModel.FailedMailData) {
        XmlElement failedMailXmlElement =
            this.xmlDocument.CreateElement(
                LKGStatePersistor.FailedMailName);
        failedMailXmlElement.SetAttribute(
            LKGStatePersistor.ReasonAttrName,
            failedMailDatum.FailureReason);
        folderXmlElement.AppendChild(failedMailXmlElement);
        failedMailXmlElement.InnerText = failedMailDatum.MailHead;
      }
      foreach (FolderModel childFolderModel in folderModel.Children) {
        this.SaveFolderModelState(
            folderXmlElement,
            childFolderModel);
      }
    }

    /// <summary>
    /// Persists the store selection state, and state of all the subfolders.
    /// </summary>
    void SaveStoreModelState(XmlElement clientXmlElement,
                             StoreModel storeModel) {
      XmlElement storeXmlElement =
          this.xmlDocument.CreateElement(LKGStatePersistor.StoreElementName);
      clientXmlElement.AppendChild(storeXmlElement);
      storeXmlElement.SetAttribute(
          LKGStatePersistor.DisplayNameAttrName,
          storeModel.Store.DisplayName);
      storeXmlElement.SetAttribute(
          LKGStatePersistor.PersistNameAttrName,
          storeModel.Store.PersistName);
      storeXmlElement.SetAttribute(
          LKGStatePersistor.SelectionStateAttrName,
          storeModel.IsSelected.ToString());
      foreach (FolderModel folderModel in storeModel.Children) {
        this.SaveFolderModelState(
            storeXmlElement,
            folderModel);
      }
    }

    /// <summary>
    /// Persists the selection state of the client, file names of all the stores
    /// loaded into the client and all the stores in the client.
    /// </summary>
    void SaveClientModelState(XmlElement userXmlElement,
                              ClientModel clientModel) {
      XmlElement clientXmlElement =
          this.xmlDocument.CreateElement(LKGStatePersistor.ClientElementName);
      userXmlElement.AppendChild(clientXmlElement);
      clientXmlElement.SetAttribute(
          LKGStatePersistor.NameAttrName,
          clientModel.Client.Name);
      clientXmlElement.SetAttribute(
          LKGStatePersistor.SelectionStateAttrName,
          clientModel.IsSelected.ToString());
      foreach (
        string storeFileName in
          clientModel.Client.LoadedStoreFileNames) {
        XmlElement loadedStoreXmlElement =
          this.xmlDocument.CreateElement(
              LKGStatePersistor.LoadedStoreElementName);
        clientXmlElement.AppendChild(loadedStoreXmlElement);
        loadedStoreXmlElement.SetAttribute(
            LKGStatePersistor.PathAttrName,
            storeFileName);
      }
      foreach (StoreModel storeModel in clientModel.Children) {
        this.SaveStoreModelState(
            clientXmlElement,
            storeModel);
      }
    }

    /// <summary>
    /// Saves the LKG state of all the clients in model.
    /// </summary>
    internal void SaveLKGState(GoogleEmailUploaderModel uploaderModel) {
      this.userXmlElement.RemoveAll();
      this.userXmlElement.SetAttribute(
          LKGStatePersistor.EmailIdAttrName,
          this.emailId);
      this.userXmlElement.SetAttribute(
          LKGStatePersistor.ArchiveEverythingAttrName,
          uploaderModel.IsArchiveEverything.ToString());
      this.userXmlElement.SetAttribute(
          LKGStatePersistor.UploadSpeedAttrName,
          uploaderModel.UploadSpeed.ToString());
      this.userXmlElement.SetAttribute(
          LKGStatePersistor.FolderToLabelMappingAttrName,
          uploaderModel.IsFolderToLabelMappingEnabled.ToString());
      foreach (ClientModel clientModel in uploaderModel.ClientModels) {
        this.SaveClientModelState(
            this.userXmlElement,
            clientModel);
      }
      this.xmlDocument.Save(this.lkgStateFilePath);
    }
  }
}

