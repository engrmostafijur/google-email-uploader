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

#ifndef OUTLOOKCLIENT_OUTLOOK_CLIENT_H__
#define OUTLOOKCLIENT_OUTLOOK_CLIENT_H__

#define INITGUID
#define WIN32_LEAN_AND_MEAN
#define USES_IID_IMAPIFolder
#define USES_IID_IMAPITable

#include <mapiutil.h>
#include <mspst.h>

#using "GoogleEmailUploader.exe"

using ::Google::MailClientInterfaces::FolderKind;
using ::Google::MailClientInterfaces::IClient;
using ::Google::MailClientInterfaces::IClientFactory;
using ::Google::MailClientInterfaces::IFolder;
using ::Google::MailClientInterfaces::IMail;
using ::Google::MailClientInterfaces::IStore;

using ::System::Collections::ArrayList;
using ::System::Collections::IEnumerable;
using ::System::Collections::IEnumerator;
using ::System::Diagnostics::Debug;
using ::System::IDisposable;
using ::System::IntPtr;
using ::System::Object;
using ::System::Runtime::InteropServices::Marshal;
using ::System::String;
using ::System::Text::StringBuilder;

// Start of IConverterSession specifics
// The definition of converter session is not in platform SDK. Copied it from
// http://blogs.msdn.com/stephen_griffin/archive/2007/06/22/iconvertersession-do-you-converter-session.aspx

// Class Identifiers
// {4e3a7680-b77a-11d0-9da5-00c04fd65685}
DEFINE_GUID(CLSID_IConverterSession, 0x4e3a7680, 0xb77a,
            0x11d0, 0x9d, 0xa5, 0x0, 0xc0, 0x4f, 0xd6, 0x56, 0x85);

// Interface Identifiers
// {4b401570-b77b-11d0-9da5-00c04fd65685}
DEFINE_GUID(IID_IConverterSession, 0x4b401570, 0xb77b,
            0x11d0, 0x9d, 0xa5, 0x0, 0xc0, 0x4f, 0xd6, 0x56, 0x85);

// These are out of mimeole.h, which is a pain to include
typedef enum tagMIMESAVETYPE {
  SAVE_RFC822 = 0,
  SAVE_RFC1521 = 1
} MIMESAVETYPE;

typedef enum tagENCODINGTYPE {
  IET_BINARY = 0,
  IET_BASE64 = 1,
  IET_UUENCODE = 2,
  IET_QP = 3,
  IET_7BIT = 4,
  IET_8BIT = 5,
  IET_INETCSET = 6,
  IET_UNICODE = 7,
  IET_RFC1522 = 8,
  IET_ENCODED = 9,
  IET_CURRENT = 10,
  IET_UNKNOWN = 11,
  IET_BINHEX40 = 12,
  IET_LAST = 13
} ENCODINGTYPE;

typedef enum tagCCSF {
  CCSF_SMTP = 0x0002,
  CCSF_NOHEADERS = 0x0004,
  CCSF_NO_MSGID = 0x4000,
  CCSF_USE_RTF = 0x0080,
  CCSF_INCLUDE_BCC = 0x0020
} CCSF;

interface IConverterSession : public IUnknown {
 public:
  virtual HRESULT PlaceHolder1();
  virtual HRESULT STDMETHODCALLTYPE SetEncoding(ENCODINGTYPE et);
  virtual HRESULT PlaceHolder2();
  virtual HRESULT STDMETHODCALLTYPE MIMEToMAPI(LPSTREAM pstm,
                                               LPMESSAGE pmsg,
                                               LPCSTR pszSrcSrv,
                                               ULONG ulFlags);
  virtual HRESULT STDMETHODCALLTYPE MAPIToMIMEStm(LPMESSAGE pmsg,
                                                  LPSTREAM pstm,
                                                  ULONG ulFlags);
  virtual HRESULT PlaceHolder3();
  virtual HRESULT PlaceHolder4();
  virtual HRESULT PlaceHolder5();
  virtual HRESULT STDMETHODCALLTYPE SetTextWrapping(BOOL fWrapText,
                                                    ULONG ulWrapWidth);
  virtual HRESULT STDMETHODCALLTYPE SetSaveFormat(MIMESAVETYPE mstSaveFormat);
  virtual HRESULT PlaceHolder8();
  virtual HRESULT PlaceHolder9();
};
// End of IConverterSession specifics


namespace Google {
namespace OutlookClient {
__gc class OutlookStore;
__gc class OutlookFolder;
__gc class OutlookEMailMessage;

// This class encapsulates helper functions for using the outlook MAPI.
// This class is also responsible for loading PST files and cleaning up the
// temporary profiles thus created when the API is disposed.
// We are compelled to use the function pointers for MAPI access of Outlook
// because otherwise Outlook has to be default mail provider.
__gc class COutlookAPI {
 public:
  COutlookAPI();

 private public:
  void Dispose();

  __property bool get_OutlookAvailable() {
    return MAPI_library_ != NULL;
  }

  __property IConverterSession *get_ConverterSession() {
    Debug::Assert(MAPI_library_ != NULL);
    return converter_session_;
  }

  __property bool get_IsUnicodeProfiles() {
    Debug::Assert(MAPI_library_ != NULL);
    return unicode_profiles_option_ == MAPI_UNICODE;
  }

  // This takes care of giving proper profile name depending on whether we have
  // unicode or non unicode profiles.
  IntPtr GetProperNativeProfileName(String *profile_name);

  // Returns list of all the profile names in the outlook
  ArrayList *GetProfileNames();

  // Returns NULL if the session could not be opened.
  LPMAPISESSION OpenSession(String *profile_name);

  // Returns NULL as root folder in case of error.
  IMAPIFolder *GetRootFolder(IMsgStore *msg_store);

  // Some folders such as Calender etc non mail folders.
  bool IsMailFolder(String *folder_name);

  FolderKind GetFolderKind(String *folder_name);
  IMAPITable *CreateMAPIContentTable(IMAPIFolder *MAPI_folder);

  // Create a stream on Widows global memory.
  IStream *CreateStream();

  // Creates a temporary profile for opening the PST file.
  // If unsuccessful returns NULL.
  String *OpenTempPSTProfile(String *pst_file_path);

 private:
  // This function finds the Outlook dll from registry and loads
  // the function pointers to the exported functions.
  HINSTANCE FindOutlookDllAndInitFunctionPointers();

  // MAPI library and functions
  // MAPI_library_ == NULL means disposed
  HINSTANCE MAPI_library_;
  MAPIINITIALIZE *MAPI_initialize_;
  MAPIADMINPROFILES *MAPI_admin_profiles_;
  MAPIUNINITIALIZE *MAPI_uninitialize_;
  MAPILOGONEX *MAPI_logon_ex_;
  MAPIALLOCATEBUFFER *MAPI_allocate_buffer_;
  MAPIFREEBUFFER *MAPI_free_buffer_;

  ULONG unicode_profiles_option_;
  IProfAdmin *profile_admin_;
  IConverterSession *converter_session_;
  int profile_counter_;
  ArrayList *temp_profile_names_;

  static String *kCalendar = "Calendar";
  static String *kContacts = "Contacts";
  static String *kDeletedItems = "Deleted Items";
  static String *kDrafts = "Drafts";
  static String *kJournal = "Journal";
  static String *kJunkEmail = "Junk E-mail";
  static String *kNotes = "Notes";
  static String *kOutbox = "Outbox";
  static String *kSentItems = "Sent Items";
  static String *kTasks = "Tasks";
  static String *kInbox = "Inbox";
  static String *kTempProfilePrefix = "Temp PST Profile - ";
};

// Client owns all the profiles and outlook API.
__gc public class OutlookClient : public IClient {
 public:
  OutlookClient();

  __property String *get_Name() {
    Debug::Assert(outlook_API_ != NULL);
    return "Microsoft Outlook";
  }

  __property IEnumerable *get_Stores() {
    Debug::Assert(outlook_API_ != NULL);
    return stores_;
  }

  __property IEnumerable *get_LoadedStoreFileNames() {
    Debug::Assert(outlook_API_ != NULL);
    return loaded_store_file_names_;
  }

  IStore* OpenStore(String *filename);
  void Dispose();

 private public:
  __property COutlookAPI *get_OutlookAPI() {
    Debug::Assert(outlook_API_ != NULL);
    return outlook_API_;
  }

 private:
  // outlook_API_ == null means disposed...
  COutlookAPI *outlook_API_;
  ArrayList *profiles_;
  ArrayList *stores_;
  ArrayList *loaded_store_file_names_;
};

// Profile owns all the stores.
__gc class OutlookProfile {
 public:
  OutlookProfile(OutlookClient *outlook_client,
                 String *profile_name);
  __property ArrayList *get_Stores();

 public private:
  void Dispose();

  __property OutlookClient *get_Client() {
    Debug::Assert(outlook_client_ != NULL);
    return outlook_client_;
  }

 private:
  // outlook_client_ == NULL indicated disposed.
  OutlookClient *outlook_client_;
  String *profile_name_;
  LPMAPISESSION MAPI_session_;
  ArrayList *stores_;
};

// Store owns all the direct folders.
__gc class OutlookStore : public IStore {
 public:
  OutlookStore(OutlookProfile *outlook_profile,
               String *persist_name,
               String *display_name,
               IMsgStore *MAPI_store);

  __property String *get_PersistName() {
    Debug::Assert(outlook_profile_ != NULL);
    return persist_name_;
  }

  __property String *get_DisplayName() {
    Debug::Assert(outlook_profile_ != NULL);
    return display_name_;
  }

  __property IClient *get_Client() {
    Debug::Assert(outlook_profile_ != NULL);
    return outlook_profile_->Client;
  }

  __property IEnumerable *get_Folders();

 public private:
  void Dispose();

  __property OutlookProfile *get_Profile() {
    Debug::Assert(outlook_profile_ != NULL);
    return outlook_profile_;
  }

  // Returns the list of subfolders of the parent folder using the MAPI
  ArrayList *GetSubFolders(IMAPIFolder *MAPI_folder,
                           OutlookFolder *parentFolder);

 private:
  // outlook_profile_ == NULL indicates this object has been disposed.
  OutlookProfile *outlook_profile_;
  String *persist_name_;
  String *display_name_;
  IMsgStore *MAPI_store_;
  IMAPIFolder *MAPI_root_folder_;
  ArrayList *folders_;
};

// Folder owns all the sub folders.
__gc class OutlookFolder : public IFolder {
 public:
  OutlookFolder(OutlookStore *outlook_store,
                OutlookFolder *parent_folder,
                String *name,
                IMAPIFolder *MAPI_folder,
                unsigned int message_count);

  __property FolderKind get_Kind() {
    Debug::Assert(outlook_store_ != NULL);
    return folder_kind_;
  }

  __property IStore *get_Store() {
    Debug::Assert(outlook_store_ != NULL);
    return outlook_store_;
  }

  __property IFolder *get_ParentFolder() {
    Debug::Assert(outlook_store_ != NULL);
    return parent_folder_;
  }

  __property String *get_Name() {
    Debug::Assert(outlook_store_ != NULL);
    return name_;
  }

  __property unsigned int get_MailCount() {
    Debug::Assert(outlook_store_ != NULL);
    return message_count_;
  }

  __property IEnumerable *get_SubFolders();
  __property IEnumerable *get_Mails();

 public private:
  void Dispose();

  __property OutlookStore *get_InternalStore() {
    Debug::Assert(outlook_store_ != NULL);
    return outlook_store_;
  }

  __property IMAPIFolder *get_MAPIFolder() {
    Debug::Assert(outlook_store_ != NULL);
    return MAPI_folder_;
  }

  IMAPITable *CreateMAPIContentTable() {
    Debug::Assert(outlook_store_ != NULL);
    return InternalStore->Profile->Client->OutlookAPI->CreateMAPIContentTable(
        MAPI_folder_);
  }

 private:
  // outlook_store_ == NULL indicates this object has been disposed.
  OutlookStore *outlook_store_;
  OutlookFolder *parent_folder_;
  String *name_;
  IMAPIFolder *MAPI_folder_;
  unsigned int message_count_;
  FolderKind folder_kind_;
  ArrayList *subfolders_;
};

// Email message is computed on demand. These are meant to live for small time.
__gc class OutlookEMailMessage : public IMail {
 public:
  OutlookEMailMessage(OutlookFolder *outlook_folder,
                      unsigned int message_row_id,
                      unsigned int message_size,
                      bool is_read,
                      bool is_flagged,
                      bool is_mail,
                      unsigned char message_entry_id_ __gc[]);

  void Dispose() {
    Debug::Assert(outlook_folder_ != NULL);
    outlook_folder_ = NULL;
    buffer_ = NULL;
  }

  __property IFolder *get_Folder() {
    Debug::Assert(outlook_folder_ != NULL);
    return outlook_folder_;
  }

  __property String *get_MailId() {
    Debug::Assert(outlook_folder_ != NULL);
    return message_row_id_.ToString();
  }

  __property bool get_IsRead() {
    Debug::Assert(outlook_folder_ != NULL);
    return is_read_;
  }

  __property bool get_IsStarred() {
    Debug::Assert(outlook_folder_ != NULL);
    return is_flagged_;
  }

  __property unsigned int get_MessageSize() {
    Debug::Assert(outlook_folder_ != NULL);
    return message_size_;
  }

  __property unsigned char get_Rfc822Buffer() __gc[];

 private:
  // outlook_folder_ == NULL implies this is disposed.
  OutlookFolder *outlook_folder_;
  unsigned int message_row_id_;
  unsigned int message_size_;
  bool is_read_;
  bool is_flagged_;
  bool is_mail_;
  unsigned char message_entry_id_ __gc[];
  unsigned char buffer_ __gc[];
};

// We implement IDisposable.
// When used in foreach in C#/VB compiler calls dispose when done with the
// iterations
__gc class OutlookEMailEnumerator : public IEnumerator, public IDisposable {
 public:
  OutlookEMailEnumerator(OutlookFolder *outlook_folder);

  __property Object *get_Current();
  bool MoveNext();
  void Reset();
  void Dispose();

 private:
  OutlookFolder *outlook_folder_;
  IMAPITable *MAPI_content_table_;
  unsigned int curr_message_row_id_;
  unsigned int curr_message_size_;
  bool curr_message_is_read_;
  bool curr_message_is_flagged_;
  bool curr_message_is_mail_;
  unsigned char curr_message_entry_id_ __gc[];
};

__gc class OutlookEMailEnumerable : public IEnumerable {
 public:
  OutlookEMailEnumerable(OutlookFolder *outlook_folder) {
    outlook_folder_ = outlook_folder;
  }

  IEnumerator *GetEnumerator() {
    return new OutlookEMailEnumerator(outlook_folder_);
  }

 private:
  OutlookFolder *outlook_folder_;
};

__gc class OutlookClientFactory : public IClientFactory {
public:
  OutlookClientFactory() {
    clientProcessNames = new ArrayList();
    clientProcessNames->Add(new String("outlook"));
  }

  IEnumerable *get_ClientProcessNames(){
    return clientProcessNames;
  }

  IClient *CreateClient() {
    return new OutlookClient();
  }

 private:
  ArrayList *clientProcessNames;
};
}}

// The following are smart/scoped pointer classes.

template<typename Type>
class ComPtr {
 public:
  ComPtr() {
    ptr_ = NULL;
  }

  ComPtr(Type *ptr) {
    ptr_ = ptr;
  }

  ~ComPtr() {
    if (ptr_) {
      ptr_->Release();
      ptr_ = NULL;
    }
  }

  operator Type*() {
    return this->ptr_;
  }

  Type *operator ->() {
    return this->ptr_;
  }

  Type **operator &() {
    return &this->ptr_;
  }

  Type *Detach() {
    Type *ret_ptr = this->ptr_;
    this->ptr_ = NULL;
    return ret_ptr;
  }

 private:
  Type *ptr_;
};

class AutoLPSPropValue {
 public:
  AutoLPSPropValue(MAPIFREEBUFFER *MAPI_free_buffer) {
    this->prop_value_ = NULL;
    MAPI_free_buffer_ = MAPI_free_buffer;
  }

  ~AutoLPSPropValue() {
    if (this->prop_value_ != NULL) {
      MAPI_free_buffer_(this->prop_value_);
      this->prop_value_= NULL;
    }
  }

  operator LPSPropValue() {
    return this->prop_value_;
  }

  LPSPropValue operator ->() {
    return this->prop_value_;
  }

  LPSPropValue *operator &() {
    return &this->prop_value_;
  }

 private:
  LPSPropValue prop_value_;
  MAPIFREEBUFFER *MAPI_free_buffer_;
};

template<typename Type>
class HGlobalPtr {
 public:
  HGlobalPtr() {
    ptr_ = NULL;
  }

  HGlobalPtr(IntPtr int_ptr) {
    ptr_ = static_cast<Type*>(int_ptr.ToPointer());
  }

  ~HGlobalPtr() {
    if (ptr_) {
      Marshal::FreeHGlobal(ptr_);
      ptr_ = NULL;
    }
  }

  operator Type*() {
    return this->ptr_;
  }

  Type *operator ->() {
    return this->ptr_;
  }

 private:
  Type *ptr_;
};

#endif  // OUTLOOKCLIENT_OUTLOOK_CLIENT_H__
