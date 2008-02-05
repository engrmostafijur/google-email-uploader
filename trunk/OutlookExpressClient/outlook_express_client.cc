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

#include "./outlook_express_client.h"

namespace Google {
namespace OutlookExpressClient {

OutlookExpressClient::OutlookExpressClient() {
  store_list_ = new ArrayList();
  loaded_store_file_names_ = new ArrayList();
  HRESULT hr = CoInitialize(0);
  if (FAILED(hr)) {
    return;
  }
  // StoreNamespace is root for OE. SO if we cant create one we assume that
  // OE is not installed.
  IStoreNamespace *store_namespace = NULL;
  hr = CoCreateInstance(CLSID_StoreNamespace,
                        NULL,
                        CLSCTX_INPROC_SERVER,
                        IID_IStoreNamespace,
                        reinterpret_cast<void**>(&store_namespace));
  if (FAILED(hr)) {
    return;
  }
  hr = store_namespace->Initialize(NULL,
                                   0);
  if (FAILED(hr)) {
    return;
  }
  OutlookExpressStore *outlookExpressStore =
      new OutlookExpressStore(this,
                              store_namespace);
  store_list_->Add(outlookExpressStore);
}

void OutlookExpressClient::Dispose() {
  Debug::Assert(store_list_ != NULL);
  for (int i = 0; i < store_list_->Count; ++i) {
    // ignore lint warning for the following dynamic cast.
    OutlookExpressStore *oe_store = dynamic_cast<OutlookExpressStore*>(
        store_list_->Item[i]);
    oe_store->Dispose();
  }
  store_list_ = NULL;
  CoUninitialize();
}

OutlookExpressStore::OutlookExpressStore(
    OutlookExpressClient *outlook_express_client,
    IStoreNamespace *store_namespace) {
  outlook_express_client_ = outlook_express_client;
  store_namespace_ = store_namespace;
  name_ = "OE Store";
}

void OutlookExpressStore::Dispose() {
  Debug::Assert(outlook_express_client_ != NULL);
  if (folder_list_ != NULL) {
    for (int i = 0; i < folder_list_->Count; ++i) {
      // ignore lint warning for the following dynamic cast.
      OutlookExpressFolder *folder = dynamic_cast<OutlookExpressFolder*>(
          folder_list_->Item[i]);
      folder->Dispose();
    }
  }
  store_namespace_->Release();
  outlook_express_client_ = NULL;
}

ArrayList *OutlookExpressStore::GetSubFolders(
    STOREFOLDERID parent_folder_id,
    OutlookExpressFolder *parent_folder) {
  Debug::Assert(outlook_express_client_ != NULL);
  ArrayList *folder_list = new ArrayList();
  FOLDERPROPS folder_props;
  HENUMSTORE store_enumerator = NULL;
  folder_props.cbSize = sizeof(FOLDERPROPS);
  HRESULT hr = store_namespace_->GetFirstSubFolder(parent_folder_id,
                                                   &folder_props,
                                                   &store_enumerator);
  if (FAILED(hr) || hr == S_FALSE) {
    return folder_list;
  }
  while (store_enumerator != NULL) {
    IStoreFolder *folder_store;
    hr = store_namespace_->OpenFolder(folder_props.dwFolderId,
                                      0,
                                      &folder_store);
    if (FAILED(hr)) {
      continue;
    }
    OutlookExpressFolder *oe_folder = new OutlookExpressFolder(this,
                                                               parent_folder,
                                                               folder_props,
                                                               folder_store);
    folder_list->Add(oe_folder);
    hr = store_namespace_->GetNextSubFolder(store_enumerator,
                                            &folder_props);
    if (hr == S_FALSE) {
      break;
    }
    if (FAILED(hr)) {
      // Ignore the error and iterate through rest of the folders.
      continue;
    }
  }
  store_namespace_->GetSubFolderClose(store_enumerator);
  return folder_list;
}

IEnumerable *OutlookExpressStore::get_Folders() {
  Debug::Assert(outlook_express_client_ != NULL);
  if (folder_list_ == NULL) {
    folder_list_ = GetSubFolders(FOLDERID_ROOT,
                                 NULL);
  }
  return folder_list_;
}

OutlookExpressFolder::OutlookExpressFolder(
    OutlookExpressStore *oe_client_store,
    OutlookExpressFolder *parent_folder,
    const tagFOLDERPROPS& folder_props,
    IStoreFolder *store_folder) {
  oe_client_store_ = oe_client_store;
  parent_folder_ = parent_folder;
  folder_id_ = folder_props.dwFolderId;
  folder_type_ = folder_props.sfType;
  message_count_ = folder_props.cMessage;
  name_ = new String(folder_props.szName);
  store_folder_ = store_folder;
}

void OutlookExpressFolder::Dispose() {
  Debug::Assert(oe_client_store_ != NULL);
  if (subfolders_ != NULL) {
    for (int i = 0; i < subfolders_->Count; ++i) {
      // ignore lint warning for the following dynamic cast.
      OutlookExpressFolder *subfolder = dynamic_cast<OutlookExpressFolder*>(
          subfolders_->Item[i]);
      subfolder->Dispose();
    }
  }
  store_folder_->Release();
  oe_client_store_ = NULL;
}

FolderKind OutlookExpressFolder::get_Kind() {
  Debug::Assert(oe_client_store_ != NULL);
  switch (folder_type_) {
    case FOLDER_INBOX:
      return FolderKind::Inbox;
    case FOLDER_SENT:
      return FolderKind::Sent;
    case FOLDER_DRAFT:
      return FolderKind::Draft;
    case FOLDER_DELETED:
      return FolderKind::Trash;
    default:
      return FolderKind::Other;
  }
}

IEnumerable *OutlookExpressFolder::get_SubFolders() {
  Debug::Assert(oe_client_store_ != NULL);
  if (subfolders_ != NULL) {
    return subfolders_;
  }
  subfolders_ = oe_client_store_->GetSubFolders(folder_id_,
                                                this);
  return subfolders_;
}

IEnumerable *OutlookExpressFolder::get_Mails() {
  Debug::Assert(oe_client_store_ != NULL);
  return new OutlookExpressEMailEnumerable(this);
}

OutlookExpressEMailMessage::OutlookExpressEMailMessage(
    OutlookExpressFolder *oe_folder,
    unsigned int message_id,
    unsigned int message_size,
    unsigned int message_flags) {
  oe_folder_ = oe_folder;
  message_id_ = message_id;
  message_size_ = message_size;
  message_flags_ = message_flags;
}

unsigned char OutlookExpressEMailMessage::get_Rfc822Buffer() __gc[] {
  Debug::Assert(oe_folder_ != NULL);
  if (buffer_ != NULL) {
    return buffer_;
  }
  ComPtr<IStream> stream;
  HRESULT hr = oe_folder_->StoreFolder->OpenMessage(
      message_id_,
      IID_IStream,
      reinterpret_cast<void**>(&stream));
  if (FAILED(hr)) {
    goto failed;
  }
  STATSTG stat;
  hr = stream->Stat(&stat,
                    STATFLAG_NONAME);
  if (FAILED(hr)) {
    goto failed;
  }
  LARGE_INTEGER pos;
  pos.QuadPart = 0;
  ULARGE_INTEGER result_pos;
  hr = stream->Seek(pos,
                    STREAM_SEEK_SET,
                    &result_pos);
  if (FAILED(hr) || result_pos.QuadPart != 0) {
    goto failed;
  }
  unsigned int size = static_cast<unsigned int>(stat.cbSize.QuadPart);
  unsigned char buffer __gc[] = new unsigned char __gc[size];
  ULONG read_byte_count = 0;
  // Extra block so that we pin for as small time as possible
  {
    unsigned char __pin *pinned_buffer = &buffer[0];
    hr = stream->Read(pinned_buffer,
                      size,
                      &read_byte_count);
  }
  if (FAILED(hr) || read_byte_count != size) {
    goto failed;
  }
  buffer_ = buffer;
  return buffer_;

 failed:
  buffer_ = new unsigned char __gc[0];
  return buffer_;
}

OutlookExpressEMailEnumerator::OutlookExpressEMailEnumerator(
    OutlookExpressFolder *oe_folder) {
  oe_folder_ = oe_folder;
  message_iterator_ = NULL;
}

OutlookExpressEMailEnumerator::~OutlookExpressEMailEnumerator() {
  if (message_iterator_) {
    oe_folder_->StoreFolder->GetMessageClose(message_iterator_);
    message_iterator_ = NULL;
  }
}

Object *OutlookExpressEMailEnumerator::get_Current() {
  return new OutlookExpressEMailMessage(oe_folder_,
                                        curr_message_id_,
                                        curr_message_size_,
                                        curr_message_flags_);
}

bool OutlookExpressEMailEnumerator::MoveNext() {
  HRESULT hr;
  MESSAGEPROPS message_props;
  message_props.cbSize = sizeof(message_props);
  // message_iterator_ == NULL means start of iteration
  if (message_iterator_ == NULL) {
    HENUMSTORE message_iterator;
    hr = oe_folder_->StoreFolder->GetFirstMessage(0,
                                                  0,
                                                  MESSAGEID_FIRST,
                                                  &message_props,
                                                  &message_iterator);
    message_iterator_ = message_iterator;
  } else {
    hr = oe_folder_->StoreFolder->GetNextMessage(message_iterator_,
                                                 0,
                                                 &message_props);
  }
  if (FAILED(hr) || hr == S_FALSE) {
    // Failed or no messages in the current folder. So we return false
    // indicating end of iteration.
    return false;
  }
  curr_message_size_ = message_props.cbMessage;
  curr_message_id_ = message_props.dwMessageId;
  curr_message_flags_ = message_props.dwFlags;
  return true;
}

void OutlookExpressEMailEnumerator::Reset() {
  if (message_iterator_) {
    oe_folder_->StoreFolder->GetMessageClose(message_iterator_);
    message_iterator_ = NULL;
  }
}

void OutlookExpressEMailEnumerator::Dispose() {
  // Reset does the disposing in our case.
  Reset();
  oe_folder_ = NULL;
}

}}  // End of namespaces
