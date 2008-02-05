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

#include "./outlook_client.h"

#define PR_FLAG_FOLLOW_UP PROP_TAG(PT_LONG, 0x1090)
static const int kFollowUpFlagValue = 2;

static SizedSPropTagArray(1, kProfileColsA) =
    {1, {PR_DISPLAY_NAME_A}};
static SizedSPropTagArray(1, kProfileColsW) =
    {1, {PR_DISPLAY_NAME_W}};
static SizedSPropTagArray(3, kMsgStoreCols) =
    {3, {PR_ENTRYID,
         PR_DISPLAY_NAME,
         PR_PROVIDER_DISPLAY}};
static SizedSPropTagArray(6, kContentTableCols) =
    {6, {PR_ENTRYID,
         PR_MESSAGE_DELIVERY_TIME,
         PR_MESSAGE_SIZE,
         PR_MESSAGE_FLAGS,
         PR_FLAG_FOLLOW_UP,
         PR_MESSAGE_CLASS}};
static SizedSSortOrderSet(1, kContentTableSortOrder) =
    {1, 0, 0, {PR_MESSAGE_DELIVERY_TIME,
               TABLE_SORT_ASCEND}};
static SizedSPropTagArray(3, kSubFolderCols) =
    {3, {PR_ENTRYID,
         PR_DISPLAY_NAME,
         PR_CONTENT_COUNT}};
static SizedSPropTagArray(1, kMessageServiceCols) =
    {1, {PR_SERVICE_UID}};
static wchar_t kHexMap[16] = {
  '0',
  '1',
  '2',
  '3',
  '4',
  '5',
  '6',
  '7',
  '8',
  '9',
  'A',
  'B',
  'C',
  'D',
  'E',
  'F',
};

namespace Google { namespace OutlookClient {

COutlookAPI::COutlookAPI() {
  temp_profile_names_ = new ArrayList();
  HINSTANCE MAPI_library = FindOutlookDllAndInitFunctionPointers();
  if (!MAPI_library) {
    return;
  }
  HRESULT hr = CoInitialize(0);
  if (FAILED(hr)) {
    return;
  }
  hr = MAPI_initialize_(NULL);
  if (FAILED(hr)) {
    return;
  }
  // Testing if the profiles are unicode or not. Note that profile being
  // unicode is not related to API being unicode or not. Grrrr....
  unicode_profiles_option_ = MAPI_UNICODE;
  ComPtr<IProfAdmin> profile_admin;
  hr = MAPI_admin_profiles_(unicode_profiles_option_,
                            &profile_admin);
  if (FAILED(hr)) {
    unicode_profiles_option_ = 0;
    hr = MAPI_admin_profiles_(unicode_profiles_option_,
                              &profile_admin);
    if (FAILED(hr)) {
      return;
    }
  }
  // Initialize the converter session. We will use the same converter session
  // for all the email translations.
  ComPtr<IConverterSession> converter_session;
  hr = CoCreateInstance(CLSID_IConverterSession,
                        NULL,
                        CLSCTX_INPROC_SERVER,
                        IID_IConverterSession,
                        reinterpret_cast<void**>(&converter_session));
  if (FAILED(hr)) {
    return;
  }
  // Everything went fine with the initialization so we record the needed
  // information into fields.
  profile_admin_ = profile_admin.Detach();
  converter_session_ = converter_session.Detach();
  MAPI_library_ = MAPI_library;
}

void COutlookAPI::Dispose() {
  if (MAPI_library_ != NULL) {
    // Dispose all the profiles.
    for (int i = 0; i < temp_profile_names_->Count; ++i) {
      // Ignore lint warning for the following dynamic cast.
      String *profile_name = dynamic_cast<String*>(
        temp_profile_names_->Item[i]);
      HGlobalPtr<wchar_t> profile_name_native(
          GetProperNativeProfileName(profile_name));
      profile_admin_->DeleteProfile(profile_name_native,
                                    unicode_profiles_option_);
    }
    // Release the resources acquired during constructor.
    converter_session_->Release();
    profile_admin_->Release();
    MAPI_uninitialize_();
    CoUninitialize();
    FreeLibrary(MAPI_library_);
    // Mark as disposed.
    MAPI_library_ = NULL;
  }
}

IntPtr COutlookAPI::GetProperNativeProfileName(String *profile_name) {
  Debug::Assert(MAPI_library_ != NULL);
  if (IsUnicodeProfiles) {
    return Marshal::StringToHGlobalUni(profile_name);
  } else {
    return Marshal::StringToHGlobalAnsi(profile_name);
  }
}

ArrayList *COutlookAPI::GetProfileNames() {
  Debug::Assert(MAPI_library_ != NULL);
  ArrayList *array_list = new ArrayList();
  ComPtr<IMAPITable> profile_table;
  // Attempts to obtain the access to the profile table, which contains
  // information about all of the available profiles.
  HRESULT hr = profile_admin_->GetProfileTable(unicode_profiles_option_,
                                               &profile_table);
  if (FAILED(hr))  {
    return array_list;
  }
  LPSRowSet profile_rows = NULL;
  // Queries the table for the display name property of each profile within.
  if (IsUnicodeProfiles) {
    hr = HrQueryAllRows(profile_table,
                        (LPSPropTagArray)&kProfileColsW,
                        NULL,
                        NULL,
                        0,
                        &profile_rows);
    if (FAILED(hr)) {
      return array_list;
    }
  } else {
    hr = HrQueryAllRows(profile_table,
                        (LPSPropTagArray)&kProfileColsA,
                        NULL,
                        NULL,
                        0,
                        &profile_rows);
    if (FAILED(hr)) {
      return array_list;
    }
  }
  // For all the profiles found in the table, adds their display name to the
  // vector of profile names.
  for (unsigned int i = 0; i < profile_rows->cRows; ++i) {
    String *profile_name;
    if (IsUnicodeProfiles) {
      profile_name = new String(profile_rows->aRow[i].lpProps->Value.lpszW);
    } else {
      profile_name = new String(profile_rows->aRow[i].lpProps->Value.lpszA);
    }
    if (profile_name->StartsWith(kTempProfilePrefix)) {
      // Ignore and delete all the temp profiles that we have created.
      HGlobalPtr<wchar_t> profile_name_native(
        GetProperNativeProfileName(profile_name));
      profile_admin_->DeleteProfile(profile_name_native,
                                    unicode_profiles_option_);
      continue;
    }
    array_list->Add(profile_name);
  }
  FreeProws(profile_rows);
  return array_list;
}

LPMAPISESSION COutlookAPI::OpenSession(String *profile_name) {
  Debug::Assert(MAPI_library_ != NULL);
  LPMAPISESSION session = NULL;
  HGlobalPtr<wchar_t> profile_name_native(
    GetProperNativeProfileName(profile_name));
  // This can prompt UI for password protected profiles.
  HRESULT hr= MAPI_logon_ex_(0,
                             profile_name_native,
                             NULL,
                             unicode_profiles_option_
                               | MAPI_LOGON_UI | MAPI_EXTENDED,
                             &session);
  if (FAILED(hr)) {
    return NULL;
  }
  return session;
}

IMAPIFolder *COutlookAPI::GetRootFolder(IMsgStore *msg_store) {
  Debug::Assert(MAPI_library_ != NULL);
  AutoLPSPropValue ipmEId(MAPI_free_buffer_);
  HRESULT hr = HrGetOneProp(msg_store,
                            PR_IPM_SUBTREE_ENTRYID,
                            &ipmEId);
  if (FAILED(hr)) {
    return NULL;
  }
  SBinary storeEId = ipmEId->Value.bin;
  ULONG root_type;
  IMAPIFolder *root_folder = NULL;
  hr = msg_store->OpenEntry(storeEId.cb,
                            (LPENTRYID)storeEId.lpb,
                            NULL,
                            NULL,
                            &root_type,
                            reinterpret_cast<IUnknown**>(&root_folder));
  if (FAILED(hr)) {
    return NULL;
  }
  if (root_type != MAPI_FOLDER) {
    root_folder->Release();
    return NULL;
  }
  return root_folder;
}

bool COutlookAPI::IsMailFolder(String *folder_name) {
  Debug::Assert(MAPI_library_ != NULL);
  return String::Compare(folder_name, kCalendar) != 0
      && String::Compare(folder_name, kContacts) != 0
      && String::Compare(folder_name, kJournal) != 0
      && String::Compare(folder_name, kNotes) != 0
      && String::Compare(folder_name, kJunkEmail) != 0
      && String::Compare(folder_name, kOutbox) != 0
      && String::Compare(folder_name, kTasks) != 0;
}

FolderKind COutlookAPI::GetFolderKind(String *folder_name) {
  Debug::Assert(MAPI_library_ != NULL);
  if (String::Compare(folder_name, kInbox) == 0) {
    return FolderKind::Inbox;
  } else if (String::Compare(folder_name, kSentItems) == 0) {
    return FolderKind::Sent;
  } else if (String::Compare(folder_name, kDrafts) == 0) {
    return FolderKind::Draft;
  } else if (String::Compare(folder_name, kDeletedItems) == 0) {
    return FolderKind::Trash;
  } else {
    return FolderKind::Other;
  }
}

IMAPITable *COutlookAPI::CreateMAPIContentTable(IMAPIFolder *MAPI_folder) {
  Debug::Assert(MAPI_library_ != NULL);
  IMAPITable *MAPI_content_table;
  HRESULT hr = MAPI_folder->GetContentsTable(MAPI_DEFERRED_ERRORS,
                                             &MAPI_content_table);
  if (FAILED(hr)) {
    return NULL;
  }
  hr = MAPI_content_table->SetColumns((LPSPropTagArray)&kContentTableCols,
                                      0);
  if (FAILED(hr)) {
    return NULL;
  }
  // Sorts the table according to Message Delivery Time, in ascending order.
  hr = MAPI_content_table->SortTable((LPSSortOrderSet)&kContentTableSortOrder,
                                     0);
  if (FAILED(hr)) {
    return NULL;
  }
  return MAPI_content_table;
}

IStream *COutlookAPI::CreateStream() {
  Debug::Assert(MAPI_library_ != NULL);
  IStream *stream;
  HRESULT hr = CreateStreamOnHGlobal(NULL,
                                     true,
                                     &stream);
  if (FAILED(hr)) {
    return NULL;
  }
  return stream;
}

String* COutlookAPI::OpenTempPSTProfile(String *pst_file_path) {
  Debug::Assert(MAPI_library_ != NULL);
  String *profile_name = String::Concat(kTempProfilePrefix,
                                        (profile_counter_++).ToString());
  HGlobalPtr<wchar_t> profile_name_native(
      GetProperNativeProfileName(profile_name));
  profile_admin_->DeleteProfile(profile_name_native,
                                unicode_profiles_option_);
  // Create a new profile.
  HRESULT hr = profile_admin_->CreateProfile(profile_name_native,
                                             NULL,
                                             0,
                                             unicode_profiles_option_);
  if (FAILED(hr)) {
    return NULL;
  }
  temp_profile_names_->Add(profile_name);
  // Get the IMsgServiceAdmin interface off the profile.
  ComPtr<IMsgServiceAdmin> service_admin;
  hr = profile_admin_->AdminServices(profile_name_native,
                                     NULL,
                                     0,
                                     unicode_profiles_option_,
                                     &service_admin);
  if (FAILED(hr)) {
    return NULL;
  }
  // Create a new message service of type "MS PST" for this profile.
  hr = service_admin->CreateMsgService(reinterpret_cast<wchar_t*>("MSPST MS"),
                                       reinterpret_cast<wchar_t*>("MSPST MS"),
                                       0,
                                       0);
  if (FAILED(hr)) {
    return NULL;
  }
  // First, we need to get the Message Service table.
  ComPtr<IMAPITable> msg_service_table;
  hr = service_admin->GetMsgServiceTable(0,
                                         &msg_service_table);
  if (FAILED(hr)) {
    return NULL;
  }
  // Query the table to obtain the entry for the newly created message
  // service.
  LPSRowSet rows = NULL;
  hr = HrQueryAllRows(msg_service_table,
                      (LPSPropTagArray)&kMessageServiceCols,
                      NULL,
                      NULL,
                      0,
                      &rows);
  if (FAILED(hr)) {
    return NULL;
  }
  // Set up a SPropValue array for the properties one needs to configure.
  HGlobalPtr<char> file_path(Marshal::StringToHGlobalAnsi(pst_file_path));
  SPropValue rgval[1];
  ZeroMemory(&rgval[0],
             sizeof(SPropValue));
  rgval[0].ulPropTag = PR_PST_PATH;
  rgval[0].Value.lpszA = file_path;
  // Now configure the message service using the properties that we had set
  // previously.
  hr = service_admin->ConfigureMsgService(
      reinterpret_cast<LPMAPIUID>(rows->aRow->lpProps[0].Value.bin.lpb),
      NULL,
      SERVICE_UI_ALLOWED,
      1,
      rgval);
  FreeProws(rows);
  if (FAILED(hr)) {
    return NULL;
  }

  return profile_name;
}

HINSTANCE COutlookAPI::FindOutlookDllAndInitFunctionPointers() {
  // We lookup for outlook's installed API dll directly.
  // We do this because we dont want to go through the stub which does not
  // work when outlook is not the default client.
  HKEY key;
  LONG res = RegOpenKeyExW(HKEY_LOCAL_MACHINE,
                           L"SOFTWARE\\Clients\\Mail\\Microsoft Outlook",
                           0,
                           KEY_READ,
                           &key);
  if (res != ERROR_SUCCESS) {
    return NULL;
  }
  // Get the path of dll.
  DWORD type;
  DWORD size = MAX_PATH + 1;
  wchar_t path[MAX_PATH + 1];
  LONG res1 = RegQueryValueExW(key,
                               L"DLLPathEx",
                               NULL,
                               &type,
                               reinterpret_cast<LPBYTE>(path),
                               &size);
  if (res1 != ERROR_SUCCESS ||
      (type != REG_SZ && type != REG_EXPAND_SZ) ||
      size > MAX_PATH) {
    RegCloseKey(key);
    return NULL;
  }
  // Expand the path in case it contains environment variables.
  if (type == REG_EXPAND_SZ) {
    wchar_t expanded_path[MAX_PATH + 1];
    DWORD esize = ExpandEnvironmentStrings(path,
                                           expanded_path,
                                           MAX_PATH + 1);
    if (esize > MAX_PATH) {
      RegCloseKey(key);
      return NULL;
    }
    lstrcpyW(expanded_path,
             path);
  }
  RegCloseKey(key);

  // Load that library and get the addresses of the needed exported functions.
  HINSTANCE MAPI_library = LoadLibrary(path);
  if (MAPI_library == 0) {
    return NULL;
  }
  MAPI_initialize_ = reinterpret_cast<MAPIINITIALIZE*>(
      GetProcAddress(MAPI_library,
                     "MAPIInitialize"));
  if (MAPI_initialize_ == NULL) {
    return NULL;
  }
  MAPI_uninitialize_ = reinterpret_cast<MAPIUNINITIALIZE*>(
      GetProcAddress(MAPI_library,
                     "MAPIUninitialize"));
  if (MAPI_uninitialize_ == NULL) {
    return NULL;
  }
  MAPI_admin_profiles_ = reinterpret_cast<MAPIADMINPROFILES*>(
      GetProcAddress(MAPI_library,
                     "MAPIAdminProfiles"));
  if (MAPI_admin_profiles_ == NULL) {
    return NULL;
  }
  MAPI_logon_ex_ = reinterpret_cast<MAPILOGONEX*>(
      GetProcAddress(MAPI_library,
                     "MAPILogonEx"));
  if (MAPI_logon_ex_ == NULL) {
    return NULL;
  }
  MAPI_allocate_buffer_ = reinterpret_cast<MAPIALLOCATEBUFFER*>(
      GetProcAddress(MAPI_library,
                     "MAPIAllocateBuffer"));
  if (MAPI_allocate_buffer_ == NULL) {
    return NULL;
  }
  MAPI_free_buffer_ = reinterpret_cast<MAPIFREEBUFFER*>(
      GetProcAddress(MAPI_library,
                     "MAPIFreeBuffer"));
  if (MAPI_free_buffer_ == NULL) {
    return NULL;
  }
  return MAPI_library;
}

OutlookClient::OutlookClient() {
  outlook_API_ = new COutlookAPI();
  profiles_ = new ArrayList();
  stores_ = new ArrayList();
  loaded_store_file_names_ = new ArrayList();
  if (!outlook_API_->OutlookAvailable) {
    return;
  }
  ArrayList *profile_name_list = outlook_API_->GetProfileNames();
  for (int i = 0; i < profile_name_list->Count; ++i) {
    // Ignore lint warning for the following dynamic cast.
    String *profile_name = dynamic_cast<String*>(profile_name_list->Item[i]);
    OutlookProfile *outlook_profile = new OutlookProfile(this,
                                                         profile_name);
    // Store the profile object and store all its stores.
    profiles_->Add(outlook_profile);
    stores_->AddRange(outlook_profile->Stores);
  }
}

IStore* OutlookClient::OpenStore(String *filename) {
  Debug::Assert(outlook_API_ != NULL);
  if (!outlook_API_->OutlookAvailable) {
    return NULL;
  }
  for (int i = 0; i < loaded_store_file_names_->Count; ++i) {
    if (loaded_store_file_names_->Item[i]->Equals(filename)) {
      // Already loaded.
      return NULL;
    }
  }
  String *pst_profile_name = outlook_API_->OpenTempPSTProfile(filename);
  if (pst_profile_name == NULL) {
    return NULL;
  }
  OutlookProfile *outlook_profile = new OutlookProfile(this,
                                                       pst_profile_name);
  if (outlook_profile->Stores->Count != 1) {
    return NULL;
  }
  profiles_->Add(outlook_profile);
  stores_->AddRange(outlook_profile->Stores);
  loaded_store_file_names_->Add(filename);

  return dynamic_cast<IStore*>(outlook_profile->Stores->Item[0]);
}

void OutlookClient::Dispose() {
  Debug::Assert(outlook_API_ != NULL);
  for (int i = 0; i < profiles_->Count; ++i) {
    // ignore lint warning for the following dynamic cast.
    OutlookProfile *profile = dynamic_cast<OutlookProfile*>(
        profiles_->Item[i]);
    profile->Dispose();
  }
  outlook_API_->Dispose();
  outlook_API_ = NULL;
}

OutlookProfile::OutlookProfile(OutlookClient *outlook_client,
                               String *profile_name) {
  outlook_client_ = outlook_client;
  profile_name_ = profile_name;
  MAPI_session_ = outlook_client->OutlookAPI->OpenSession(profile_name);
}

ArrayList *OutlookProfile::get_Stores() {
  Debug::Assert(outlook_client_ != NULL);
  if (stores_ != NULL) {
    return stores_;
  }
  stores_ = new ArrayList();
  if (MAPI_session_ == NULL) {
    return stores_;
  }
  // Open the stores table
  ComPtr<IMAPITable> msgStoresTable;
  HRESULT hr = MAPI_session_->GetMsgStoresTable(0,
                                                &msgStoresTable);
  if (FAILED(hr)) {
    return stores_;
  }
  // Get the info about all the stores in the current profile.
  LPSRowSet msgStoreRows = NULL;
  hr = HrQueryAllRows(msgStoresTable,
                      (LPSPropTagArray)&kMsgStoreCols,
                      NULL,
                      NULL,
                      0,
                      &msgStoreRows);
  if (FAILED(hr)) {
    return stores_;
  }
  if (msgStoreRows->cRows <= 0) {
    FreeProws(msgStoreRows);
    return stores_;
  }
  for (unsigned int k = 0; k < msgStoreRows->cRows; ++k) {
    IMsgStore *msg_store = NULL;
    SBinary defaultEntryId = msgStoreRows->aRow[k].lpProps[0].Value.bin;
    hr = MAPI_session_->OpenMsgStore(0,
                                     defaultEntryId.cb,
                                     (LPENTRYID)defaultEntryId.lpb,
                                     NULL,
                                     MAPI_BEST_ACCESS,
                                     &msg_store);
    if (FAILED(hr)) { 
      continue;
    }
    // defaultEntryId is persistent identifier for the store in the datebase.
    // We use it as persistent name for the store.
    StringBuilder *persist_name_builder = new StringBuilder();
    for (unsigned long i = 0; i < defaultEntryId.cb; ++i) {
      unsigned char byte = defaultEntryId.lpb[i];
      persist_name_builder->Append(kHexMap[byte >> 4]);
      persist_name_builder->Append(kHexMap[byte & 0x0F]);
    }
    String *store_display_name = new String(
        msgStoreRows->aRow[k].lpProps[1].Value.lpszW);
    // For now we use the display name as persist name.
    OutlookStore *outlook_store = new OutlookStore(
        this,
        persist_name_builder->ToString(),
        store_display_name,
        msg_store);
    stores_->Add(outlook_store);
  }
  FreeProws(msgStoreRows);

  return stores_;
}

void OutlookProfile::Dispose() {
  Debug::Assert(outlook_client_ != NULL);
  if (stores_ != NULL) {
    for (int i = 0; i < stores_->Count; ++i) {
      // Ignore lint warning for the following dynamic cast.
      OutlookStore *store = dynamic_cast<OutlookStore*>(
          stores_->Item[i]);
      store->Dispose();
    }
  }
  if (MAPI_session_ != NULL) {
    MAPI_session_->Release();
  }
  outlook_client_ = NULL;
}

OutlookStore::OutlookStore(OutlookProfile *outlook_profile,
                           String *persist_name,
                           String *display_name,
                           IMsgStore *MAPI_store) {
  outlook_profile_ = outlook_profile;
  persist_name_ = persist_name;
  display_name_ = display_name;
  MAPI_store_ = MAPI_store;
  MAPI_root_folder_ =
      outlook_profile->Client->OutlookAPI->GetRootFolder(MAPI_store_);
}

IEnumerable *OutlookStore::get_Folders() {
  Debug::Assert(outlook_profile_ != NULL);
  if (folders_ != NULL) {
    return folders_;
  }
  if (MAPI_root_folder_ == NULL) {
    folders_ = new ArrayList();
    return folders_;
  }
  folders_ = GetSubFolders(MAPI_root_folder_,
                           NULL);
  return folders_;
}

void OutlookStore::Dispose() {
  Debug::Assert(outlook_profile_ != NULL);
  if (folders_ != NULL) {
    for (int i = 0; i < folders_->Count; ++i) {
      // Ignore lint warning for the following dynamic cast.
      OutlookFolder *folder = dynamic_cast<OutlookFolder*>(
          folders_->Item[i]);
      folder->Dispose();
    }
  }
  if (MAPI_root_folder_ != NULL) {
    MAPI_root_folder_->Release();
  }
  MAPI_store_->Release();
  outlook_profile_ = NULL;
}

ArrayList *OutlookStore::GetSubFolders(IMAPIFolder *MAPI_parent_folder,
                                       OutlookFolder *parent_folder) {
  Debug::Assert(outlook_profile_ != NULL);
  ArrayList *sub_folders = new ArrayList();
  ComPtr<IMAPITable> hierarchy;
  HRESULT hr = MAPI_parent_folder->OpenProperty(
      PR_CONTAINER_HIERARCHY,
      &IID_IMAPITable,
      NULL,
      0,
      reinterpret_cast<IUnknown**>(&hierarchy));

  if (FAILED(hr)) {
    return sub_folders;
  }
  LPSRowSet rows;
  // Obtains all subdirectories of this folder.
  hr = HrQueryAllRows(hierarchy,
                      (LPSPropTagArray)&kSubFolderCols,
                      NULL,
                      NULL,
                      0,
                      &rows);
  if (FAILED(hr)) {
    return sub_folders;
  }
  // For each subdirectory, obtains a pointer to it, and adds the
  // pointer to the sub_folders array lsit if the directory is valid
  // for traversal.
  for (unsigned int i = 0; i < rows->cRows; ++i) {
    ComPtr<IMAPIFolder> MAPI_child_folder;
    ULONG child_type;
    hr = MAPI_parent_folder->OpenEntry(
        rows->aRow[i].lpProps[0].Value.bin.cb,
        (LPENTRYID) rows->aRow[i].lpProps[0].Value.bin.lpb,
        NULL,
        0,
        &child_type,
        reinterpret_cast<IUnknown**>(&MAPI_child_folder));
    if (FAILED(hr)) {
      // Continue with other folders in case of failure...
      continue;
    }
    if (child_type != MAPI_FOLDER) {
      continue;
    }
    String *folder_name = new String(rows->aRow[i].lpProps[1].Value.lpszW);
    if (!outlook_profile_->Client->OutlookAPI->IsMailFolder(folder_name)) {
      continue;
    }
    OutlookFolder *child_folder = new OutlookFolder(
        this,
        parent_folder,
        folder_name,
        MAPI_child_folder.Detach(),
        rows->aRow[i].lpProps[2].Value.l);
    sub_folders->Add(child_folder);
  }
  FreeProws(rows);
  return sub_folders;
}

OutlookFolder::OutlookFolder(OutlookStore *outlook_store,
                             OutlookFolder *parent_folder,
                             String *name,
                             IMAPIFolder *MAPI_folder,
                             unsigned int message_count) {
  outlook_store_ = outlook_store;
  parent_folder_ = parent_folder; 
  name_ = name;
  MAPI_folder_ = MAPI_folder;
  folder_kind_ =
    outlook_store->Profile->Client->OutlookAPI->GetFolderKind(name);
  message_count_ = message_count;
}

IEnumerable *OutlookFolder::get_SubFolders() {
  Debug::Assert(outlook_store_ != NULL);
  if (subfolders_ != NULL) {
    return subfolders_;
  }
  subfolders_ = outlook_store_->GetSubFolders(MAPI_folder_,
                                              this);
  return subfolders_;
}

IEnumerable *OutlookFolder::get_Mails() {
  Debug::Assert(outlook_store_ != NULL);
  return new OutlookEMailEnumerable(this);
}

void OutlookFolder::Dispose() {
  Debug::Assert(outlook_store_ != NULL);
  if (subfolders_ != NULL) {
    for (int i = 0; i < subfolders_->Count; ++i) {
      // Ignore lint warning for the following dynamic cast.
      OutlookFolder *subfolder = dynamic_cast<OutlookFolder*>(
          subfolders_->Item[i]);
      subfolder->Dispose();
    }
  }
  MAPI_folder_->Release();
  outlook_store_ = NULL;
}

OutlookEMailMessage::OutlookEMailMessage(
    OutlookFolder *outlook_folder,
    unsigned int message_row_id,
    unsigned int message_size,
    bool is_read,
    bool is_flagged,
    bool is_mail,
    unsigned char message_entry_id __gc[]) {
  outlook_folder_ = outlook_folder;
  message_row_id_ = message_row_id;
  message_size_ = message_size;
  is_read_ = is_read;
  is_flagged_ = is_flagged;
  is_mail_ = is_mail;
  message_entry_id_ = message_entry_id;
}

unsigned char OutlookEMailMessage::get_Rfc822Buffer() __gc[] {
  Debug::Assert(outlook_folder_ != NULL);
  if (buffer_ != NULL) {
    return buffer_;
  }
  // Open the message.
  ComPtr<IMessage> message;
  if (!is_mail_) {
    goto failed;
  }
  HRESULT hr;
  ULONG child_type;
  {
    // Pin the entry id to pass it to the unmanaged world.
    void __pin *entry_id = &message_entry_id_[0];
    hr = outlook_folder_->MAPIFolder->OpenEntry(
        message_entry_id_->Length,
        (LPENTRYID)entry_id,
        NULL,
        0,
        &child_type,
        reinterpret_cast<IUnknown**>(&message));
    if (FAILED(hr) || child_type != MAPI_MESSAGE) {
      goto failed;
    }
  }
  {
    // Create stream for converting message to MIME format.
    ComPtr<IStream> stream(
        outlook_folder_->InternalStore->
            Profile->Client->OutlookAPI->CreateStream());
    if (stream == NULL) {
      goto failed;
    }
    IConverterSession *converter_session =
        outlook_folder_->InternalStore->
            Profile->Client->OutlookAPI->ConverterSession;
    hr = converter_session->MAPIToMIMEStm(message,
                                          stream,
                                          CCSF_SMTP);
    if (FAILED(hr)) {
      goto failed;
    }
    ::STATSTG stat;
    hr = stream->Stat(&stat,
                      STATFLAG_NONAME);
    if (FAILED(hr)) {
      goto failed;
    }
    LARGE_INTEGER pos;
    pos.QuadPart = 0;
    ULARGE_INTEGER result_pos;
    // Since the stream was written, its current pointer points to the end.
    // We seek to the start for reading.
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
  }

 failed:
  buffer_ = new unsigned char __gc[0];
  return buffer_;
}

OutlookEMailEnumerator::OutlookEMailEnumerator(OutlookFolder *outlook_folder) {
  outlook_folder_ = outlook_folder;
  MAPI_content_table_ = NULL;
}

Object *OutlookEMailEnumerator::get_Current() {
  return new OutlookEMailMessage(outlook_folder_,
                                 curr_message_row_id_,
                                 curr_message_size_,
                                 curr_message_is_read_,
                                 curr_message_is_flagged_,
                                 curr_message_is_mail_,
                                 curr_message_entry_id_);
}

bool OutlookEMailEnumerator::MoveNext() {
  // MAPI_content_table_ == NULL indicates the start of iteration.
  if (MAPI_content_table_ == NULL) {
    // So we create a new MAPI_content_table_ for starting
    // iterating through the mails.
    MAPI_content_table_ = outlook_folder_->CreateMAPIContentTable();
    if (MAPI_content_table_ == NULL) {
      // In case of error, we return false indicating empty enumeration.
      return false;
    }
  }
  ULONG row_id, num, den;
  // We query the current position of the cursor.
  HRESULT hr = MAPI_content_table_->QueryPosition(&row_id,
                                                  &num,
                                                  &den);
  if (FAILED(hr)) {
    return false;
  }
  // We read 1 row from the content table.
  LPSRowSet rows = NULL;
  hr = MAPI_content_table_->QueryRows(1,
                                      0,
                                      &rows);
  if (FAILED(hr) || rows == NULL || rows->cRows == 0) {
    // In case of failure or we could not read a row we indicate end of
    // iteration
    return false;
  }
  // Store the info about mail in the curr_* fields.
  curr_message_row_id_ = row_id;
  curr_message_size_ = rows->aRow[0].lpProps[2].Value.l;
  curr_message_is_read_ =
      (rows->aRow[0].lpProps[3].Value.l & MSGFLAG_READ) != 0;
  curr_message_is_flagged_ =
      rows->aRow[0].lpProps[4].Value.l == kFollowUpFlagValue;
  String *message_class_name =
      new String(rows->aRow[0].lpProps[5].Value.lpszW);
  curr_message_is_mail_ = message_class_name->StartsWith("IPM.Note");
  unsigned int count = rows->aRow[0].lpProps[0].Value.bin.cb;
  curr_message_entry_id_ = new unsigned char __gc[count];
  unsigned char *msg_entry_id_orig = rows->aRow[0].lpProps[0].Value.bin.lpb;
  for (unsigned int i = 0; i < count; ++i) {
    curr_message_entry_id_[i] = msg_entry_id_orig[i];
  }
  FreeProws(rows);
  return true;
}

void OutlookEMailEnumerator::Reset() {
  if (MAPI_content_table_ != NULL) {
    MAPI_content_table_->Release();
    MAPI_content_table_ = NULL;
    curr_message_row_id_ = 0;
  }
}

void OutlookEMailEnumerator::Dispose() {
  // Reset does the disposing in out case.
  Reset();
  outlook_folder_ = NULL;
}

}}  // End of namespace
