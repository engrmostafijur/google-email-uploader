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
//<GoogleEmailUploader>
//  <User EmailId="foo@bar.com" ArchiveEverything="False" UploadSpeed="0.0022068824276766" FolderToLabelMapping="True">
//    <Client Name="Microsoft Outlook" SelectionState="False">
//      <Store DisplayName="Insight Server Folders" Persist="0000000038A1BB1005E5101AA1BB08002B2A56C20000436F6E6E6563746F722E646C6C00433A5C446F63756D656E747320616E642053657474696E67735C706172616D5C4170706C69636174696F6E20446174615C42796E6172695C42796E61726920496E736967687420436F6E6E6563746F7220332E305C4163636F756E74735C706172616D2D62795C666F6F40746573742E636F6D315C4C6F63616C43616368652E646200" SelectionState="False">
//        <Contact SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//      </Store>
//    </Client>
//    <Client Name="Outlook Express" SelectionState="False">
//      <Store DisplayName="OE Store" Persist="OE Store" SelectionState="False">
//        <Contact SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Deleted Items" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Drafts" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Inbox" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Outbox" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Sent Items" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//      </Store>
//    </Client>
//    <Client Name="Thunderbird" SelectionState="False">
//      <Store DisplayName="Local Folders" Persist="Local Folders" SelectionState="False">
//        <Contact SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Temp" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Trash" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Unsent Messages" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//      </Store>
//      <Store DisplayName="pop.gmail.com" Persist="pop.gmail.com" SelectionState="False">
//        <Contact SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Inbox" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//        <Folder Name="Trash" SelectionState="False" UploadedItemCount="0" FailedItemCount="0" LastUploadedItemId="" />
//      </Store>
//    </Client>
//  </User>
//</GoogleEmailUploader>

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
    const string ContactsElementName = "Contacts";
    const string FolderElementName = "Folder";
    const string MailElementName = "Mail";
    const string MailIdAttrName = "MailId";
    const string ContactElementName = "Contact";
    const string ContactIdAttrName = "ContactId";
    const string FailureReasonAttrName = "FailureReason";
    const string ArchiveEverythingAttrName = "ArchiveEverything";
    const string FolderToLabelMappingAttrName = "FolderToLabelMapping";
    const string UploadSpeedAttrName = "UploadSpeed";
    const string NameAttrName = "Name";
    const string SelectionStateAttrName = "SelectionState";
    const string DisplayNameAttrName = "DisplayName";
    const string PathAttrName = "Path";
    const string PersistNameAttrName = "Persist";

    readonly string lkgStateFilePath;
    readonly string emailId;
    readonly XmlDocument xmlDocument;
    readonly XmlElement googleEmailUploaderXmlElement;
    readonly XmlElement userXmlElement;

    internal LKGStatePersistor(string emailId) {
      this.lkgStateFilePath =
          Path.Combine(Application.LocalUserAppDataPath,
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
              xmlElement.GetAttribute(LKGStatePersistor.MailIdAttrName);
          if (emailId == this.emailId) {
            return xmlElement;
          }
        }
      }
      XmlElement newXmlElement =
          this.xmlDocument.CreateElement(LKGStatePersistor.UserElementName);
      newXmlElement.SetAttribute(
          LKGStatePersistor.MailIdAttrName,
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
      foreach (XmlNode childXmlNode in folderXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.MailElementName) {
          continue;
        }
        string mailId =
            childXmlElement.GetAttribute(LKGStatePersistor.MailIdAttrName);
        if (mailId == null || mailId.Length == 0) {
          continue;
        }
        string failureReason =
            childXmlElement.GetAttribute(
                LKGStatePersistor.FailureReasonAttrName);
        if (failureReason == null || failureReason.Length == 0) {
          folderModel.SuccessfullyUploaded(mailId);
        } else {
          FailedMailDatum failedMailDatum =
            new FailedMailDatum(childXmlElement.InnerText, failureReason);
          folderModel.FailedToUpload(mailId, failedMailDatum);
        }
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
    /// Loads the selection state of the contacts in the given store.
    /// </summary>
    void LoadContactsState(XmlElement contactsXmlElement,
                           StoreModel storeModel) {
      if (contactsXmlElement.GetAttribute(
            LKGStatePersistor.SelectionStateAttrName) == bool.FalseString) {
        storeModel.IsContactSelected = false;
      } else {
        storeModel.IsContactSelected = true;
      }
      foreach (XmlNode childXmlNode in contactsXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.ContactElementName) {
          continue;
        }
        string contactId =
            childXmlElement.GetAttribute(LKGStatePersistor.ContactIdAttrName);
        if (contactId == null || contactId.Length == 0) {
          continue;
        }
        string failureReason =
            childXmlElement.GetAttribute(
                LKGStatePersistor.FailureReasonAttrName);
        if (failureReason == null || failureReason.Length == 0) {
          storeModel.SuccessfullyUploaded(contactId);
        } else {
          FailedContactDatum failedContactDatum =
            new FailedContactDatum(childXmlElement.InnerText, failureReason);
          storeModel.FailedToUpload(contactId, failedContactDatum);
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
      foreach (XmlNode childXmlNode in storeXmlElement.ChildNodes) {
        XmlElement childXmlElement = childXmlNode as XmlElement;
        if (childXmlElement == null) {
          continue;
        }
        if (childXmlElement.Name != LKGStatePersistor.ContactsElementName) {
          continue;
        }
        this.LoadContactsState(childXmlElement, storeModel);
        break;
      }
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
        if (!File.Exists(filePath)) {
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
        if (uploadSpeed <= 0.0) {
          uploadSpeed = 0.00005;
        }
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

      foreach (string mailId in folderModel.MailUploadData.Keys) {
        XmlElement uploadedEmailXmlElement =
            this.xmlDocument.CreateElement(
                LKGStatePersistor.MailElementName);
        folderXmlElement.AppendChild(uploadedEmailXmlElement);
        uploadedEmailXmlElement.SetAttribute(
            LKGStatePersistor.MailIdAttrName,
            mailId);
        FailedMailDatum failedMailDatum =
            (FailedMailDatum)folderModel.MailUploadData[mailId];
        if (failedMailDatum != null) {
          // In case of failure to upload we set the reason, otherwise not.
          uploadedEmailXmlElement.SetAttribute(
              LKGStatePersistor.FailureReasonAttrName,
              failedMailDatum.FailureReason);
          uploadedEmailXmlElement.InnerText = failedMailDatum.MailHead;
        }
      }
      foreach (FolderModel childFolderModel in folderModel.Children) {
        this.SaveFolderModelState(
            folderXmlElement,
            childFolderModel);
      }
    }

    /// <summary>
    /// Saves the contact state within the given store.
    /// </summary>
    void SaveContactsState(XmlElement storeXmlElement,
                           StoreModel storeModel) {
      XmlElement contactsXmlElement =
          this.xmlDocument.CreateElement(LKGStatePersistor.ContactsElementName);
      storeXmlElement.AppendChild(contactsXmlElement);
      contactsXmlElement.SetAttribute(
          LKGStatePersistor.SelectionStateAttrName,
          storeModel.IsContactSelected.ToString());

      foreach (string contactId in storeModel.ContactUploadData.Keys) {
        XmlElement uploadedContactXmlElement =
            this.xmlDocument.CreateElement(
                LKGStatePersistor.ContactElementName);
        contactsXmlElement.AppendChild(uploadedContactXmlElement);
        uploadedContactXmlElement.SetAttribute(
            LKGStatePersistor.ContactIdAttrName,
            contactId);
        FailedContactDatum failedContactDatum =
            (FailedContactDatum)storeModel.ContactUploadData[contactId];
        if (failedContactDatum != null) {
          // In case of failure to upload we set the reason, otherwise not.
          uploadedContactXmlElement.SetAttribute(
              LKGStatePersistor.FailureReasonAttrName,
              failedContactDatum.FailureReason);
          uploadedContactXmlElement.InnerText = failedContactDatum.ContactName;
        }
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
      this.SaveContactsState(storeXmlElement, storeModel);
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
          LKGStatePersistor.MailIdAttrName,
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

