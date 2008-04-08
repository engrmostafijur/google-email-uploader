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

using Google.MailClientInterfaces;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Configuration;

namespace GoogleEmailUploader {
  public class GoogleEmailUploaderConfig {
    static int maximumMailsPerBatch;
    static int normalBatchSize;
    static int maximumBatchSize;
    static int minimumPauseTimeSeconds;
    static int maximumPauseTimeSeconds;
    static int failedMailHeadLineCount;
    static string emailMigrationUrl =
        "https://apps-apis.google.com/a/feeds/migration/2.0/{0}/{1}/mail/batch";
    static bool traceEnabled;
    static bool logFullXml;

    static int TryGetConfigIntValue(string key,
                                    int defaultValue) {
      string value;
      try {
        value = ConfigurationSettings.AppSettings[key];
        if (value != null) {
          return int.Parse(value);
        }
      } catch {
        // If there is error let the default value take precedence.
      }
      return defaultValue;
    }

    static bool TryGetConfigBoolValue(string key,
                                      bool defaultValue) {
      string value;
      try {
        value = ConfigurationSettings.AppSettings[key];
        if (value != null) {
          return bool.Parse(value);
        }
      } catch {
        // If there is error let the default value take precedence.
      }
      return defaultValue;
    }

    public static void InitializeConfiguration() {
      System.Console.WriteLine(System.Environment.OSVersion.Version);
      GoogleEmailUploaderConfig.maximumMailsPerBatch =
          GoogleEmailUploaderConfig.TryGetConfigIntValue("MaximumMailsPerBatch",
                                                         15);
      GoogleEmailUploaderConfig.normalBatchSize =
          GoogleEmailUploaderConfig.TryGetConfigIntValue("NormalBatchSize",
                                                         512 * 1024);
      GoogleEmailUploaderConfig.maximumBatchSize =
          GoogleEmailUploaderConfig.TryGetConfigIntValue("MaximumBatchSize",
                                                         16 * 1024 * 1024);
      GoogleEmailUploaderConfig.minimumPauseTimeSeconds =
          GoogleEmailUploaderConfig.TryGetConfigIntValue(
              "MinimumPauseTimeSeconds",
              90);
      GoogleEmailUploaderConfig.maximumPauseTimeSeconds =
          GoogleEmailUploaderConfig.TryGetConfigIntValue(
              "MaximumPauseTimeSeconds",
              180);
      GoogleEmailUploaderConfig.failedMailHeadLineCount =
          GoogleEmailUploaderConfig.TryGetConfigIntValue(
              "FailedMailHeadLineCount",
              40);
      try {
        string value = ConfigurationSettings.AppSettings["EmailMigrationUrl"];
        if (value != null) {
          GoogleEmailUploaderConfig.emailMigrationUrl = value;
        }
      } catch {
        // If there is error let the default value stay
      }
      GoogleEmailUploaderConfig.traceEnabled =
          GoogleEmailUploaderConfig.TryGetConfigBoolValue("TraceEnabled",
                                                          true);
      GoogleEmailUploaderConfig.logFullXml =
          GoogleEmailUploaderConfig.TryGetConfigBoolValue("LogFullXml",
                                                          false);
    }

    internal static int MaximumMailsPerBatch {
      get {
        return GoogleEmailUploaderConfig.maximumMailsPerBatch;
      }
    }

    internal static int NormalBatchSize {
      get {
        return GoogleEmailUploaderConfig.normalBatchSize;
      }
    }

    internal static int MaximumBatchSize {
      get {
        return GoogleEmailUploaderConfig.maximumBatchSize;
      }
    }

    internal static int MinimumPauseTime {
      get {
        return GoogleEmailUploaderConfig.minimumPauseTimeSeconds;
      }
    }

    internal static int MaximumPauseTime {
      get {
        return GoogleEmailUploaderConfig.maximumPauseTimeSeconds;
      }
    }

    internal static int FailedMailHeadLineCount {
      get {
        return GoogleEmailUploaderConfig.failedMailHeadLineCount;
      }
    }

    internal static string EmailMigrationUrl {
      get {
        return GoogleEmailUploaderConfig.emailMigrationUrl;
      }
    }

    internal static bool TraceEnabled {
      get {
        return GoogleEmailUploaderConfig.traceEnabled;
      }
    }

    internal static bool LogFullXml {
      get {
        return GoogleEmailUploaderConfig.logFullXml;
      }
    }
  }

  public class GoogleEmailUploaderTrace {
    static StreamWriter traceStreamWriter;
    static string indentString;

    static string GetDateTime() {
      return DateTime.Now.ToString("HH:mm:ss.fffffff dd-MM-yyyy");
    }

    [Conditional("TRACE")]
    public static void Initalize(string traceFilePath) {
      if (GoogleEmailUploaderConfig.TraceEnabled) {
        GoogleEmailUploaderTrace.traceStreamWriter =
            File.AppendText(traceFilePath);
        GoogleEmailUploaderTrace.traceStreamWriter.WriteLine(
            "{0} Starting run",
            GoogleEmailUploaderTrace.GetDateTime());
        GoogleEmailUploaderTrace.indentString = "  ";
      }
    }

    [Conditional("TRACE")]
    internal static void EnteringMethod(string methodName) {
      if (GoogleEmailUploaderConfig.TraceEnabled) {
        GoogleEmailUploaderTrace.traceStreamWriter.Write(
            GoogleEmailUploaderTrace.GetDateTime());
        GoogleEmailUploaderTrace.traceStreamWriter.Write(
            GoogleEmailUploaderTrace.indentString);
        GoogleEmailUploaderTrace.traceStreamWriter.WriteLine(
            "EnteringMethod: {0}",
            methodName);
        GoogleEmailUploaderTrace.traceStreamWriter.Flush();
        GoogleEmailUploaderTrace.indentString =
            GoogleEmailUploaderTrace.indentString + "  ";
      }
    }

    [Conditional("TRACE")]
    internal static void ExitingMethod(string methodName) {
      if (GoogleEmailUploaderConfig.TraceEnabled) {
        GoogleEmailUploaderTrace.traceStreamWriter.Write(
            GoogleEmailUploaderTrace.GetDateTime());
        GoogleEmailUploaderTrace.indentString =
            GoogleEmailUploaderTrace.indentString.Substring(2);
        GoogleEmailUploaderTrace.traceStreamWriter.Write(
            GoogleEmailUploaderTrace.indentString);
        GoogleEmailUploaderTrace.traceStreamWriter.WriteLine(
            "ExitingMethod: {0}",
            methodName);
        GoogleEmailUploaderTrace.traceStreamWriter.Flush();
      }
    }

    [Conditional("TRACE")]
    internal static void WriteXml(string xml) {
      if (GoogleEmailUploaderConfig.TraceEnabled) {
        GoogleEmailUploaderTrace.traceStreamWriter.WriteLine(xml);
        GoogleEmailUploaderTrace.traceStreamWriter.Flush();
      }
    }

    [Conditional("TRACE")]
    internal static void WriteLine(string message, params object[] args) {
      if (GoogleEmailUploaderConfig.TraceEnabled) {
        GoogleEmailUploaderTrace.traceStreamWriter.Write(
            GoogleEmailUploaderTrace.GetDateTime());
        GoogleEmailUploaderTrace.traceStreamWriter.Write(
            GoogleEmailUploaderTrace.indentString);
        GoogleEmailUploaderTrace.traceStreamWriter.WriteLine(message, args);
        GoogleEmailUploaderTrace.traceStreamWriter.Flush();
      }
    }

    [Conditional("TRACE")]
    public static void Close() {
      if (GoogleEmailUploaderConfig.TraceEnabled) {
        GoogleEmailUploaderTrace.traceStreamWriter.WriteLine(
            "{0} Ending run",
            GoogleEmailUploaderTrace.GetDateTime());
        GoogleEmailUploaderTrace.traceStreamWriter.Close();
      }
    }
  }

  public enum ModelState {
    Initialized,
    SignedIn,
    Uploading,
    UploadingPause,
  }

  public abstract class TreeNodeModel {
    internal readonly GoogleEmailUploaderModel GoogleEmailUploaderModel;
    TreeNodeModel parent;
    bool isSelected;

    internal TreeNodeModel(TreeNodeModel parent,
                           GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.parent = parent;
      this.GoogleEmailUploaderModel = googleEmailUploaderModel;
      this.isSelected = false;
    }

    public abstract string DisplayName {
      get;
    }

    public TreeNodeModel Parent {
      get {
        return this.parent;
      }
    }

    public abstract IEnumerable Children {
      get;
    }

    public bool IsSelected {
      set {
        this.isSelected = value;
      }
      get {
        return this.isSelected;
      }
    }
  }

  public sealed class ClientModel : TreeNodeModel {
    public readonly IClient Client;
    readonly ArrayList storeModels;

    internal ClientModel(IClient client,
                         GoogleEmailUploaderModel googleEmailUploaderModel)
      : base(null, googleEmailUploaderModel) {
      this.Client = client;
      this.storeModels = new ArrayList();
      foreach (IStore storeIter in client.Stores) {
        this.storeModels.Add(
            new StoreModel(
                this,
                storeIter,
                googleEmailUploaderModel));
      }
    }

    public override string DisplayName {
      get { return this.Client.Name; }
    }

    public override IEnumerable Children {
      get { return this.storeModels; }
    }

    public StoreModel OpenStore(string fileName) {
      IStore store = this.Client.OpenStore(fileName);
      if (store == null) {
        return null;
      }
      StoreModel storeModel = new StoreModel(this,
                                             store,
                                             this.GoogleEmailUploaderModel);
      this.storeModels.Add(storeModel);
      return storeModel;
    }
  }

  public class FailedContactDatum {
    public readonly string ContactName;
    public readonly string FailureReason;

    internal FailedContactDatum(string contactName, string failureReason) {
      this.ContactName = contactName;
      this.FailureReason = failureReason;
    }
  }

  public sealed class StoreModel : TreeNodeModel {
    public readonly IStore Store;
    readonly ArrayList folderModels;
    uint uploadedContactCount;
    uint failedContactCount;
    // contactData hashtable is used for knowing what is happening with each
    // contact. If the contact id is not present in this then the contact is not
    // uploaded if is present and value is:
    //    null -> Successfully uploaded
    //    non null -> value should be of type FailedContactDatum
    Hashtable contactUploadData;
    bool isContactSelected;
    string fullStorePath;

    internal StoreModel(TreeNodeModel parent,
                        IStore store,
                        GoogleEmailUploaderModel googleEmailUploaderModel)
      : base(parent, googleEmailUploaderModel) {
      this.Store = store;
      this.folderModels = new ArrayList();
      foreach (IFolder folderIter in store.Folders) {
        this.folderModels.Add(
            new FolderModel(
                this,
                folderIter,
                googleEmailUploaderModel));
      }
      this.contactUploadData = new Hashtable();
    }

    public override string DisplayName {
      get { return this.Store.DisplayName; }
    }

    public override IEnumerable Children {
      get { return this.folderModels; }
    }

    public string FullStorePath {
      get {
        if (this.fullStorePath != null) {
          return this.fullStorePath;
        }
        StringBuilder sb = new StringBuilder(1024);
        sb.Append('/');
        sb.Append(this.Parent.DisplayName);
        sb.Append('/');
        sb.Append(this.DisplayName);
        sb.Append('/');
        this.fullStorePath = sb.ToString();
        return this.fullStorePath;
      }
    }

    public uint UploadedContactCount {
      get {
        return this.uploadedContactCount;
      }
    }

    public uint FailedContactCount {
      get {
        return this.failedContactCount;
      }
    }

    public bool IsContactSelected {
      set {
        this.isContactSelected = value;
      }
      get {
        return this.isContactSelected;
      }
    }

    public Hashtable ContactUploadData {
      get {
        return this.contactUploadData;
      }
    }

    public bool IsUploaded(string contactId) {
      if (contactId == null || contactId.Length == 0) {
        return false;
      }
      return this.contactUploadData.ContainsKey(contactId);
    }

    public void SuccessfullyUploaded(string contactId) {
      this.uploadedContactCount++;
      if (contactId == null || contactId.Length == 0) {
        return;
      }
      if (this.contactUploadData.ContainsKey(contactId)) {
        return;
      }
      this.contactUploadData.Add(contactId, null);
    }

    public void FailedToUpload(string contactId,
                               FailedContactDatum failedContactDatum) {
      this.failedContactCount++;
      if (contactId == null || contactId.Length == 0) {
        return;
      }
      if (this.contactUploadData.ContainsKey(contactId)) {
        return;
      }
      this.contactUploadData.Add(contactId, failedContactDatum);
    }
  }

  public class FailedMailDatum {
    public readonly string MailHead;
    public readonly string FailureReason;

    internal FailedMailDatum(string mailHead, string failureReason) {
      this.MailHead = mailHead;
      this.FailureReason = failureReason;
    }
  }

  public sealed class FolderModel : TreeNodeModel {
    public readonly IFolder Folder;
    readonly ArrayList subFolderModels;
    ClientModel clientModel;
    StoreModel storeModel;
    string folderPath;
    string fullFolderPath;
    string[] labels;
    uint uploadedMailCount;
    uint failedMailCount;
    // mailData hashtable is used for knowing what is happening with each
    // mail. If the mail id is not present in this then the mail is not
    // uploaded if is present and value is:
    //    null -> Successfully uploaded
    //    non null -> value should be of type FailedMailDatum
    Hashtable mailUploadData;

    internal FolderModel(TreeNodeModel parent,
                         IFolder folder,
                         GoogleEmailUploaderModel googleEmailUploaderModel)
      : base(parent, googleEmailUploaderModel) {
      this.Folder = folder;
      this.subFolderModels = new ArrayList();
      foreach (IFolder folderIter in folder.SubFolders) {
        this.subFolderModels.Add(
            new FolderModel(
                this,
                folderIter,
                googleEmailUploaderModel));
      }
      this.mailUploadData = new Hashtable();
    }
    
    public override string DisplayName {
      get { return this.Folder.Name; }
    }

    public override IEnumerable Children {
      get { return this.subFolderModels; }
    }

    static void AppendReverse(StringBuilder sb,
                              string s) {
      for (int i = s.Length - 1; i >= 0; --i) {
        sb.Append(s[i]);
      }
    }

    static void Reverse(StringBuilder sb) {
      int half = sb.Length / 2;
      for (int i = 0; i < half; ++i) {
        int ip = sb.Length - i - 1;
        char c = sb[i];
        sb[i] = sb[ip];
        sb[ip] = c;
      }
    }

    public ClientModel ClientModel {
      get {
        if (this.clientModel != null) {
          return this.clientModel;
        }
        TreeNodeModel model = this;
        while (model != null) {
          if (model is ClientModel) {
            break;
          }
          model = model.Parent;
        }
        this.clientModel = (ClientModel)model;
        return this.clientModel;
      }
    }

    public StoreModel StoreModel {
      get {
        if (this.storeModel != null) {
          return this.storeModel;
        }
        TreeNodeModel model = this;
        while (model != null) {
          if (model is StoreModel) {
            break;
          }
          model = model.Parent;
        }
        this.storeModel = (StoreModel)model;
        return this.storeModel;
      }
    }


    public string FolderPath {
      get {
        if (this.folderPath != null) {
          return this.folderPath;
        }
        StringBuilder sb = new StringBuilder(1024);
        FolderModel folderModel = this;
        while (folderModel != null) {
          if (sb.Length > 0) {
            sb.Append('/');
          }
          FolderModel.AppendReverse(
              sb,
              folderModel.DisplayName);
          folderModel = folderModel.Parent as FolderModel;
        }
        FolderModel.Reverse(sb);
        this.folderPath = sb.ToString();
        return this.folderPath;
      }
    }

    public string FullFolderPath {
      get {
        if (this.fullFolderPath != null) {
          return this.fullFolderPath;
        }
        StringBuilder sb = new StringBuilder(1024);
        sb.Append('/');
        sb.Append(this.ClientModel.DisplayName);
        sb.Append('/');
        sb.Append(this.StoreModel.DisplayName);
        sb.Append('/');
        sb.Append(this.FolderPath);
        this.fullFolderPath = sb.ToString();
        return this.fullFolderPath;
      }
    }

    public string[] Labels {
      get {
        if (this.labels != null) {
          return this.labels;
        }

        // If the user has opted not to use folder to label
        // mapping.
        if (this.GoogleEmailUploaderModel.IsFolderToLabelMappingEnabled) {
          string label = Resources.ImportedText;
          if (this.FolderPath.Length > 0) {
            label += "/" + this.FolderPath;
          }
          this.labels = new string[] {
              label};
        }
        return this.labels;
      }
    }

    public void ResetLabels() {
      this.labels = null;
    }

    public uint UploadedMailCount {
      get {
        return this.uploadedMailCount;
      }
    }

    public uint FailedMailCount {
      get {
        return this.failedMailCount;
      }
    }

    public Hashtable MailUploadData {
      get {
        return this.mailUploadData;
      }
    }

    public bool IsUploaded(string emailId) {
      if (emailId == null || emailId.Length == 0) {
        return false;
      }
      return this.mailUploadData.ContainsKey(emailId);
    }

    public void SuccessfullyUploaded(string emailId) {
      this.uploadedMailCount++;
      if (emailId == null || emailId.Length == 0) {
        return;
      }
      if (this.mailUploadData.ContainsKey(emailId)) {
        return;
      }
      this.mailUploadData.Add(emailId, null);
    }

    public void FailedToUpload(string emailId,
                               FailedMailDatum failedMailDatum) {
      this.failedMailCount++;
      if (emailId == null || emailId.Length == 0) {
        return;
      }
      if (this.mailUploadData.ContainsKey(emailId)) {
        return;
      }
      this.mailUploadData.Add(emailId, failedMailDatum);
    }
  }

  class MailIterator : IDisposable {
    readonly ArrayList folderModelFlatList;
    // Since we are keeping track of failed mail count
    // in GoogleEmailUploaderModel, we use the delegate to do the updating of
    // the count. Alternatively we could directly access the fields in
    // GoogleEmailUploaderModel.
    readonly VoidDelegate failedMailIncrementDelegate;
    int folderIterationIndex;
    FolderModel currentFolderModel;
    IEnumerator currentFolderEnumerator;
    IMail currentMail;

    internal MailIterator(ArrayList folderModelFlatList,
                          VoidDelegate failedMailIncrementDelegate) {
      this.folderModelFlatList = folderModelFlatList;
      this.failedMailIncrementDelegate = failedMailIncrementDelegate;
    }

    void DisposeCurrentEnumerator() {
      IDisposable disposable = this.currentFolderEnumerator as IDisposable;
      if (disposable != null) {
        disposable.Dispose();
      }
      this.currentFolderEnumerator = null;
    }

    void DisposeCurrentMail() {
      IDisposable disposable = this.currentMail as IDisposable;
      if (disposable != null) {
        disposable.Dispose();
      }
      this.currentMail = null;
    }

    public void Dispose() {
      this.DisposeCurrentMail();
      this.DisposeCurrentEnumerator();
    }

    bool MoveToNextNonEmptySelectedFolder() {
      this.DisposeCurrentEnumerator();
      while (this.folderIterationIndex < this.folderModelFlatList.Count) {
        this.currentFolderModel =
            (FolderModel)this.folderModelFlatList[this.folderIterationIndex];
        this.folderIterationIndex++;
        if (!this.currentFolderModel.IsSelected ||
            this.currentFolderModel.Folder.MailCount == 0) {
          continue;
        }
        this.currentFolderEnumerator =
            this.currentFolderModel.Folder.Mails.GetEnumerator();
        if (!this.currentFolderEnumerator.MoveNext()) {
          // We reached the end of the folder
          // so dispose enumerator and continue with the next folder.
          this.DisposeCurrentEnumerator();
          continue;
        }
        // There are mails and current points to the mail to be uploaded.
        return true;
      }
      this.currentFolderModel = null;
      return false;
    }

    internal bool MoveToNextMail() {
      while (true) {
        this.DisposeCurrentMail();
        if (this.currentFolderEnumerator == null ||
            !this.currentFolderEnumerator.MoveNext()) {
          if (!this.MoveToNextNonEmptySelectedFolder()) {
            return false;
          }
        }
        this.currentMail = (IMail)this.currentFolderEnumerator.Current;
        if (this.currentFolderModel.IsUploaded(this.currentMail.MailId)) {
          continue;
        }
        byte[] rfcByteBuffer = this.currentMail.Rfc822Buffer;
        if (rfcByteBuffer.Length >= 0 &&
            rfcByteBuffer.Length <=
                GoogleEmailUploaderConfig.MaximumBatchSize) {
          return true;
        }
        this.failedMailIncrementDelegate();
        string mailHead = MailBatch.GetMailHeader(this.currentMail);
        FailedMailDatum failedMailDatum =
            new FailedMailDatum(
                mailHead,
                string.Format(
                    "This email is larger than {0}MB and could not be uploaded",
                    GoogleEmailUploaderConfig.MaximumBatchSize/1024/1024));
        this.currentFolderModel.FailedToUpload(this.currentMail.MailId,
                                               failedMailDatum);
      }
    }

    internal IMail CurrentMail {
      get {
        return this.currentMail;
      }
    }

    internal FolderModel CurrentFolderModel {
      get {
        return this.currentFolderModel;
      }
    }

    internal ClientModel CurrentClientModel {
      get {
        if (this.currentFolderModel == null) {
          return null;
        }
        return this.currentFolderModel.ClientModel;
      }
    }
  }

  class ContactIterator : IDisposable {
    readonly ArrayList storeModelFlatList;
    int storeIterationIndex;
    StoreModel currentStoreModel;
    IEnumerator currentStoreEnumerator;
    IContact currentContact;

    internal ContactIterator(ArrayList storeModelFlatList) {
      this.storeModelFlatList = storeModelFlatList;
    }

    void DisposeCurrentEnumerator() {
      IDisposable disposable = this.currentStoreEnumerator as IDisposable;
      if (disposable != null) {
        disposable.Dispose();
      }
      this.currentStoreEnumerator = null;
    }

    void DisposeCurrentContact() {
      IDisposable disposable = this.currentContact as IDisposable;
      if (disposable != null) {
        disposable.Dispose();
      }
      this.currentContact = null;
    }

    public void Dispose() {
      this.DisposeCurrentContact();
      this.DisposeCurrentEnumerator();
    }

    bool MoveToNextNonEmptySelectedStore() {
      this.DisposeCurrentEnumerator();
      while (this.storeIterationIndex < this.storeModelFlatList.Count) {
        this.currentStoreModel =
            (StoreModel)this.storeModelFlatList[this.storeIterationIndex];
        this.storeIterationIndex++;
        if (!this.currentStoreModel.IsContactSelected ||
            this.currentStoreModel.Store.ContactCount == 0) {
          continue;
        }
        this.currentStoreEnumerator =
            this.currentStoreModel.Store.Contacts.GetEnumerator();
        if (!this.currentStoreEnumerator.MoveNext()) {
          // We reached the end of the folder
          // so dispose enumerator and continue with the next folder.
          this.DisposeCurrentEnumerator();
          continue;
        }
        // There are mails and current points to the mail to be uploaded.
        return true;
      }
      this.currentStoreModel = null;
      return false;
    }

    internal bool MoveToNextContact() {
      while (true) {
        this.DisposeCurrentContact();
        if (this.currentStoreEnumerator == null ||
            !this.currentStoreEnumerator.MoveNext()) {
          if (!this.MoveToNextNonEmptySelectedStore()) {
            return false;
          }
        }
        this.currentContact = (IContact)this.currentStoreEnumerator.Current;
        if (this.currentStoreModel.IsUploaded(this.currentContact.ContactId)) {
          continue;
        }
        return true;
      }
    }

    internal IContact CurrentContact {
      get {
        return this.currentContact;
      }
    }

    internal StoreModel CurrentStoreModel {
      get {
        return this.currentStoreModel;
      }
    }

    internal ClientModel CurrentClientModel {
      get {
        if (this.currentStoreModel == null) {
          return null;
        }
        return (ClientModel)this.currentStoreModel.Parent;
      }
    }
  }

  public delegate void MailBatchDelegate(MailBatch mailBatch);

  public delegate void MailDelegate(MailBatch mailBatch,
                                    IMail mail);

  public delegate void ContactEntryDelegate(ContactEntry contactEntry);

  public delegate void ContactDelegate(IContact contact);

  public delegate void VoidDelegate();

  public delegate void BoolDelegate(bool boolValue);

  public enum PauseReason {
    UserAction,
    ConnectionFailures,
    ServiceUnavailable,
    ServerInternalError,
    Resuming,
  }

  public delegate void UploadPausedDelegate(PauseReason pauseReason);

  public delegate void PauseCountDownDelegate(PauseReason pauseReason,
                                              int remainingCount);

  public delegate void UploadDoneDelegate(DoneReason doneReason);

  class CountDownTicker {
    PauseReason pauseReason;
    int remainingTickCounts;

    internal CountDownTicker(int tickCountRemaining,
                             PauseReason pauseReason) {
      this.pauseReason = pauseReason;
      this.remainingTickCounts = tickCountRemaining;
    }

    internal void Tick() {
      this.remainingTickCounts--;
    }

    internal bool IsCountdownDone {
      get {
        return this.remainingTickCounts < 0;
      }
    }

    internal void CallPauseCountDownDelegate(
        PauseCountDownDelegate pauseCountDownDelegate) {
      pauseCountDownDelegate(this.pauseReason, this.remainingTickCounts);
    }
  }

  public class GoogleEmailUploaderModel : IDisposable {
    // Like GoogleMailMigrationUtility/1.0.0.0/en-US
    const string UserAgentTemplate = "GoogleMailMigrationUtility/{0}/{1}";

    static ArrayList clientFactories;

    readonly IHttpFactory HttpFactory;
    readonly string ApplicationName;

    ModelState modelState;

    // In initaialized state this will be non null.
    GoogleAuthenticator gaiaAuthenticator;
    public event VoidDelegate LoadingClientsEvent;

    // In Signed in state+ these will be non null.
    string emailId;
    string password;
    LKGStatePersistor lkgStatePersistor;
    ArrayList clientModels;
    ArrayList flatStoreModelList;
    ArrayList flatFolderModelList;
    bool isFolderToLabelMappingEnabled;
    bool isArchiveEverything;

    // In uploading state+ these will be non null
    MailUploader mailUploader;
    ContactIterator contactIterator;
    MailIterator mailIterator;
    Hashtable contactEmailData;
    Timer pauseTimer;
    public event ContactDelegate ContactReadingEvent;
    public event ContactEntryDelegate ContactUploadTryStartEvent;
    public event ContactEntryDelegate ContactUploadedEvent;
    public event MailBatchDelegate MailBatchFillingStartEvent;
    public event MailDelegate MailBatchFillingEvent;
    public event MailBatchDelegate MailBatchFillingEndEvent;
    public event MailBatchDelegate MailBatchUploadTryStartEvent;
    public event MailBatchDelegate MailBatchUploadedEvent;
    public event UploadPausedDelegate UploadPausedEvent;
    public event PauseCountDownDelegate PauseCountDownEvent;
    public event UploadDoneDelegate UploadDoneEvent;

    // This indicates that we should use mailIterator's current mail before
    // asking for more email.
    bool useCurrent;
    uint selectedContactCount;
    uint uploadedContactCount;
    uint failedContactCount;
    uint selectedEmailCount;
    uint uploadedEmailCount;
    uint failedEmailCount;
    int pauseTime;
    double uploadMailsPerMilliSecond = 0.0009; // mails per millisecond

    public static void LoadClientFactories() {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.LoadClientFactories");
        ArrayList clientFactoryList = new ArrayList();
        string assemblyDirectory =
            Path.GetDirectoryName(typeof(Program).Assembly.Location);
        foreach (string fileName in Directory.GetFiles(assemblyDirectory,
                                                       "*.dll")) {
          try {
            Assembly assembly = Assembly.LoadFrom(fileName);
            object[] attributes =
                assembly.GetCustomAttributes(typeof(ClientFactoryAttribute),
                                             false);
            if (attributes == null ||
                attributes.Length == 0) {
              continue;
            }
            foreach (ClientFactoryAttribute clientFactoryAttribute in
                attributes) {
              IClientFactory clientFactory =
                  Activator.CreateInstance(
                      clientFactoryAttribute.ClientFactoryType) as
                          IClientFactory;
              if (clientFactory == null) {
                continue;
              }
              clientFactoryList.Add(clientFactory);
              GoogleEmailUploaderTrace.WriteLine("Loaded client factory: {0}",
                                       fileName);
            }
          } catch (Exception exception) {
            //  Ignore the exceptions while loading clients
            GoogleEmailUploaderTrace.WriteLine("Exception({0}): {1}",
                                     fileName,
                                     exception.Message);
          }
        }
        GoogleEmailUploaderModel.clientFactories = clientFactoryList;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.LoadClientFactories");
      }
    }

    public GoogleEmailUploaderModel()
      : this(new HttpFactory()) {
    }

    public GoogleEmailUploaderModel(IHttpFactory httpFactory) {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel..ctor");
        this.HttpFactory = httpFactory;
        AssemblyName assemblyName = this.GetType().Assembly.GetName();
        this.ApplicationName =
            string.Format(GoogleEmailUploaderModel.UserAgentTemplate,
                          assemblyName.Version,
                          Resources.LocaleText);
        this.TransitionToInitializedState();
        GoogleEmailUploaderTrace.WriteLine(
            "GoogleEmailUploaderModel created: {0}",
            this.ApplicationName);
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel..ctor");
      }
    }

    void LoadClients() {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.LoadClients");
        foreach (IClientFactory clientFactory in
            GoogleEmailUploaderModel.clientFactories) {
          IClient client = clientFactory.CreateClient();
          if (client == null) {
            continue;
          }
          this.clientModels.Add(new ClientModel(client, this));
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.LoadClients");
      }
    }

    void FillModelsRec(ArrayList storeList,
                       ArrayList folderList,
                       IEnumerable forestModels) {
      foreach (TreeNodeModel tnm in forestModels) {
        StoreModel sm = tnm as StoreModel;
        if (sm != null) {
          storeList.Add(sm);
        }
        FolderModel fm = tnm as FolderModel;
        if (fm != null) {
          folderList.Add(fm);
        }
        this.FillModelsRec(storeList,
                           folderList,
                           tnm.Children);
      }
    }

    void DisposeClientModels() {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.DisposeClientModels");
        foreach (ClientModel clientModel in this.clientModels) {
          GoogleEmailUploaderTrace.WriteLine("Disposing client: {0}",
                                   clientModel.Client.Name);
          clientModel.Client.Dispose();
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.DisposeClientModels");
      }
    }

    void TransitionToInitializedState() {
      this.gaiaAuthenticator =
          new GoogleAuthenticator(this.HttpFactory,
                                  AccountType.GoogleOrHosted,
                                  this.ApplicationName);
      this.emailId = null;
      this.password = null;
      this.lkgStatePersistor = null;
      this.flatStoreModelList = null;
      this.flatFolderModelList = null;
      this.mailUploader = null;
      this.contactIterator = null;
      this.mailIterator = null;
      this.pauseTimer = null;
      this.modelState = ModelState.Initialized;
    }

    void TransitionToSignedInState(string emailId,
                                   string password) {
      Debug.Assert(this.modelState == ModelState.Initialized);
      this.emailId = emailId;
      this.password = password;
      if (this.LoadingClientsEvent != null) {
        this.LoadingClientsEvent();
      }
      this.gaiaAuthenticator = null;
      this.LoadingClientsEvent = null;
      this.lkgStatePersistor = new LKGStatePersistor(emailId);
      this.clientModels = new ArrayList();
      this.LoadClients();
      this.modelState = ModelState.SignedIn;
      this.lkgStatePersistor.LoadLKGState(this);
      this.BuildModelFlatList();
      this.mailUploader = new MailUploader(this.HttpFactory,
                                           this.emailId,
                                           this.password,
                                           this.ApplicationName,
                                           this);
    }

    void TransitionToUploadingState() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.lkgStatePersistor.SaveLKGState(this);
      this.ComputeEmailContactCounts();
      this.contactIterator =
          new ContactIterator(
              this.flatStoreModelList);
      this.mailIterator =
          new MailIterator(
              this.flatFolderModelList,
              new VoidDelegate(this.IncrementFailedMailCount));
      this.useCurrent = false;
      this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
      this.modelState = ModelState.Uploading;
    }

    void IncrementFailedMailCount() {
      this.failedEmailCount++;
    }

    /// <summary>
    /// Call this method to signin using Google authentication.
    /// </summary>
    public AuthenticationResponse SignIn(string emailId,
                                         string password) {
      Debug.Assert(this.modelState == ModelState.Initialized);
      Debug.Assert(this.LoadingClientsEvent != null);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(  
            "GoogleEmailUploaderModel.SignIn");
        AuthenticationResponse authResponse =
          this.gaiaAuthenticator.CheckPassword(
              emailId,
              password);
        if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.Authenticated) {
          this.TransitionToSignedInState(
              emailId,
              password);
        }
        return authResponse;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.SignIn");
      }
    }

    /// <summary>
    /// Call this method to sign in using  Google authentication
    /// and the solution to CAPTCHA challenge.
    /// </summary>
    public AuthenticationResponse SignInCAPTCHA(string emailId,
                                                string password,
                                                string captchaToken,
                                                string captchaSolution) {
      Debug.Assert(this.modelState == ModelState.Initialized);
      Debug.Assert(this.LoadingClientsEvent != null);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.SignInCAPTCHA");
        AuthenticationResponse authResponse =
            this.gaiaAuthenticator.CheckPasswordCAPTCHA(
                emailId,
                password,
                captchaToken,
                captchaSolution);
        if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.Authenticated) {
          this.TransitionToSignedInState(
              emailId,
              password);
        }
        return authResponse;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.SignInCAPTCHA");
      }
    }

    /// <summary>
    /// This method builds the array of flattened folder models.
    /// </summary>
    public void BuildModelFlatList() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.flatStoreModelList = new ArrayList();
      this.flatFolderModelList = new ArrayList();
      this.FillModelsRec(this.flatStoreModelList,
                         this.flatFolderModelList,
                         this.clientModels);
      this.ComputeEmailContactCounts();
    }

    /// <summary>
    /// This methods computes various mail counts. This method is
    /// expected to be called everytime the selection is changed by the UI.
    /// </summary>
    public void ComputeEmailContactCounts() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.selectedContactCount = 0;
      this.failedContactCount = 0;
      this.uploadedContactCount = 0;
      foreach (StoreModel sm in this.flatStoreModelList) {
        if (!sm.IsContactSelected) {
          continue;
        }
        this.selectedContactCount += sm.Store.ContactCount;
        // All the contacts that were uplaoded or failed are considered.
        this.uploadedContactCount += sm.UploadedContactCount;
        this.failedContactCount += sm.FailedContactCount;
      }

      this.selectedEmailCount = 0;
      this.failedEmailCount = 0;
      this.uploadedEmailCount = 0;
      foreach (FolderModel fm in this.flatFolderModelList) {
        if (!fm.IsSelected) {
          continue;
        }
        this.selectedEmailCount += fm.Folder.MailCount;
        // All the mails that were uplaoded or failed are considered.
        this.uploadedEmailCount += fm.UploadedMailCount;
        this.failedEmailCount += fm.FailedMailCount;
      }
    }

    /// <summary>
    /// This method sets the option to enable/disable folder to label mapping.
    /// </summary>
    public void SetFolderToLabelMapping(bool value) {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.isFolderToLabelMappingEnabled = value;
      if (this.flatFolderModelList != null) {
        foreach (FolderModel fm in this.flatFolderModelList) {
          fm.ResetLabels();
        }
      }
    }

    public void SetArchiving(bool value) {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.isArchiveEverything = value;
    }

    internal UploadResult TestEmailUpload() {
      Debug.Assert  (this.modelState == ModelState.SignedIn);
      double uploadMilliseconds;
      UploadResult result =
          this.mailUploader.TestEmailUpload(out uploadMilliseconds);
      return result;
    }

    /// <summary>
    /// This method starts the upload of mails using background thread.
    /// </summary>
    public void StartUpload() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.StartUpload");
        this.TransitionToUploadingState();
        this.mailUploader.StartUpload();
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.StartUpload");
      }
    }

    /// <summary>
    /// This method stops the upload. It does this by aborting the
    /// upload thread. When the thread is aborted finally, UploadDoneEvent is
    /// raised. There might be slight delay before the thread actually aborts
    /// because of synchronized calls on the network sockets.
    /// </summary>
    public void StopUpload() {
      Debug.Assert(this.modelState == ModelState.Uploading ||
                   this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.StopUpload");
        this.mailUploader.StopUpload();
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.StopUpload");
      }
    }

    /// <summary>
    /// This method pauses the upload. Call ResumeUpload to resume uploading.
    /// There might be slight delay before the thread actually pauses
    /// because of synchronized calls on the network sockets.
    /// </summary>
    public void PauseUpload() {
      Debug.Assert(this.modelState == ModelState.Uploading);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.PauseUpload");
        this.OnPause(PauseReason.UserAction);
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.PauseUpload");
      }
    }

    /// <summary>
    /// This method resumes the upload from the previous pause.
    /// </summary>
    public void ResumeUpload() {
      Debug.Assert(this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.ResumeUpload");
        this.OnResume();
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.ResumeUpload");
      }
    }

    /// <summary>
    /// Cleans up the model for disposing
    /// </summary>
    public void Dispose() {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.Dispose");
        if (this.modelState == ModelState.Initialized) {
          this.gaiaAuthenticator = null;
        } else if (this.modelState == ModelState.SignedIn) {
          this.emailId = null;
          this.password = null;
          this.lkgStatePersistor.SaveLKGState(this);
          this.lkgStatePersistor = null;
          this.flatStoreModelList = null;
          this.flatFolderModelList = null;
          this.DisposeClientModels();
        } else if (this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause) {
          this.emailId = null;
          this.password = null;
          this.lkgStatePersistor = null;
          this.flatStoreModelList = null;
          this.flatFolderModelList = null;
          this.mailUploader = null;
          this.contactIterator.Dispose();
          this.mailIterator.Dispose();
          this.contactIterator = null;
          this.mailIterator = null;
          if (this.pauseTimer != null) {
            this.pauseTimer.Dispose();
            this.pauseTimer = null;
          }
          this.DisposeClientModels();
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.Dispose");
      }
    }

    /// <summary>
    /// Waits for the uploading thread to finish.
    /// </summary>
    public void WaitForUploadingThread() {
      if (this.modelState == ModelState.Uploading ||
          this.modelState == ModelState.UploadingPause) {
        this.mailUploader.WaitForUploadingThread();
      }
    }

    /// <summary>
    /// Returns the collection of client factories
    /// </summary>
    public static ArrayList ClientFactories {
      get {
        return GoogleEmailUploaderModel.clientFactories;
      }
    }

    /// <summary>
    /// Collection of ClientModels that are loaded.
    /// </summary>
    public ArrayList ClientModels {
      get {
        Debug.Assert(this.modelState > ModelState.Initialized);
        return this.clientModels;
      }
    }

    /// <summary>
    /// Total number of email messages available in all the selected folders.
    /// </summary>
    public uint SelectedEmailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.selectedEmailCount;
      }
    }

    /// <summary>
    /// Total number of contacts available in all the selected folders.
    /// </summary>
    public uint SelectedContactCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.selectedContactCount;
      }
    }

    /// <summary>
    /// Total number of items available in all the selected folders.
    /// </summary>
    public uint TotalSelectedItemCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.selectedEmailCount + this.selectedContactCount;
      }
    }

    /// <summary>
    /// Number mails that are remaining to be uploaded.
    /// </summary>
    public uint RemainingItemCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        long ret =
            (long)this.selectedEmailCount
                - this.failedEmailCount
                - this.uploadedEmailCount
            + this.selectedContactCount
                - this.failedContactCount
                - this.uploadedContactCount;
        return (uint)Math.Max(ret, 0);
      }
    }

    /// <summary>
    /// Number of mails that failed to be uploaded eiter because of large size
    /// or because they could not be read from the client.
    /// </summary>
    public uint FailedEmailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.failedEmailCount;
      }
    }

    /// <summary>
    /// Number of contacts that failed to be uploaded.
    /// </summary>
    public uint FailedContactCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.failedContactCount;
      }
    }

    /// <summary>
    /// Indicates if there were failures in uplaoding email or contacts.
    /// </summary>
    public bool HasFailures {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.failedContactCount > 0 || this.failedEmailCount > 0;
      }
    }

    /// <summary>
    /// Indicates if there were failures in uplaoding email or contacts.
    /// </summary>
    public uint TotalFailedItemCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.failedContactCount + this.failedEmailCount;
      }
    }

    /// <summary>
    /// Number of mails that got uploaded.
    /// </summary>
    public uint UploadedEmailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.uploadedEmailCount;
      }
    }

    /// <summary>
    /// Number of contacts that got uploaded.
    /// </summary>
    public uint UploadedContactCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.uploadedContactCount;
      }
    }

    /// <summary>
    /// Sum of uploaded email and contacts
    /// </summary>
    public uint TotalUploadedItemCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.uploadedEmailCount + this.uploadedContactCount;
      }
    }

    /// <summary>
    /// Returns the time remaining for uploading rest of the mails.
    /// </summary>
    public TimeSpan UploadTimeRemaining {
      get {
        double millisecsNeeded =
            this.RemainingItemCount / this.uploadMailsPerMilliSecond;
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(millisecsNeeded);
        return timeSpan;
      }
    }

    /// <summary>
    /// Returns the upload speed in terms of mails per millisecond.
    /// </summary>
    public double UploadSpeed {
      get {
        return this.uploadMailsPerMilliSecond;
      }
    }

    /// <summary>
    /// Is the mail uploading paused.
    /// </summary>
    public bool IsPaused {
      get {
        Debug.Assert(this.modelState == ModelState.Uploading ||
                     this.modelState == ModelState.UploadingPause);
        return this.modelState == ModelState.UploadingPause;
      }
    }

    /// <summary>
    /// The Email ID of the user who has signed in.
    /// </summary>
    public string EmailID {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.emailId;
      }
    }

    /// <summary>
    /// List of all the store models. These include both the selected and
    /// unselected stores.
    /// </summary>
    public ArrayList FlatStoreModelList {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.flatStoreModelList;
      }
    }

    /// <summary>
    /// List of all the folder models. These include both the selected and
    /// unselected folders.
    /// </summary>
    public ArrayList FlatFolderModelList {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.flatFolderModelList;
      }
    }

    /// <summary>
    /// Returns the model state of the GoogleEmailUploader model.
    /// </summary>
    public ModelState ModelState {
      get {
        return this.modelState;
      }
    }

    /// <summary>
    /// Returns the current client model.
    /// </summary>
    public ClientModel CurrentClientModel {
      get {
        if (this.contactIterator.CurrentClientModel != null) {
          return this.contactIterator.CurrentClientModel;
        } else {
          return this.mailIterator.CurrentClientModel;
        }
      }
    }

    public bool IsFolderToLabelMappingEnabled {
      get {
        return this.isFolderToLabelMappingEnabled;
      }
    }

    public bool IsArchiveEverything {
      get {
        return this.isArchiveEverything;
      }
    }

    internal string BallParkEstimate() {
      double remainingTime = 2.0 * this.UploadTimeRemaining.TotalSeconds;
      if (remainingTime < 10.0 * 60) {
        return Resources.LessThan10MinsText;
      } else if (remainingTime < 30.0 * 60) {
        return Resources.LessThan30MinsText;
      } else if (remainingTime < 1.0 * 60 * 60) {
        return Resources.LessThan1HourText;
      } else if (remainingTime < 3.0 * 60 * 60) {
        return Resources.Between1To3HoursText;
      } else if (remainingTime < 5.0 * 60 * 60) {
        return Resources.Between3To5HoursText;
      } else if (remainingTime < 10.0 * 60 * 60) {
        return Resources.Between5To10HoursText;
      } else if (remainingTime < 15.0 * 60 * 60) {
        return Resources.Between10To15HoursText;
      } else if (remainingTime < 24.0 * 60 * 60) {
        return Resources.Between15To24HoursText;
      } else {
        return Resources.MoreThanDayText;
      }
    }

    internal void SetUploadSpeed(double uploadSpeed) {
      this.uploadMailsPerMilliSecond = uploadSpeed;
    }

    internal void UpdateUploadSpeed(uint mailCount,
                                    TimeSpan timeTaken) {
      uint consideredEmailCount =
          (this.failedEmailCount + this.uploadedEmailCount);
      double oldTime = consideredEmailCount / this.uploadMailsPerMilliSecond;
      double totalMails = consideredEmailCount + mailCount;
      double totalTimeTaken = oldTime + timeTaken.TotalMilliseconds;
      this.uploadMailsPerMilliSecond = totalMails / totalTimeTaken;
    }

    void TimedPauseUpload(PauseReason pauseReason) {
      Debug.Assert(this.modelState == ModelState.Uploading);
      this.OnPause(pauseReason);
      Debug.Assert(this.pauseTimer == null);
      // Kill previous timer if it exists.
      if (this.pauseTimer != null) {
        this.pauseTimer.Dispose();
        this.pauseTimer = null;
      }
      // Create a timer that calls PauseTimerCallback ever second.
      this.pauseTimer =
          new Timer(new TimerCallback(this.PauseTimerCallback),
                    new CountDownTicker(this.pauseTime,
                                        pauseReason),
                    0,
                    1000);
    }

    void PauseTimerCallback(object state) {
      if (this.modelState == ModelState.UploadingPause) {
        CountDownTicker countDownTicker = (CountDownTicker)state;
        countDownTicker.Tick();
        if (this.PauseCountDownEvent != null) {
          countDownTicker.CallPauseCountDownDelegate(this.PauseCountDownEvent);
        }
        if (countDownTicker.IsCountdownDone) {
          this.OnResume();
        }
      } else if(this.pauseTimer != null) {
        this.pauseTimer.Dispose();
        this.pauseTimer = null;
      }
    }

    void RaisePauseTime() {
      this.pauseTime *= 2;
      if (this.pauseTime > GoogleEmailUploaderConfig.MaximumPauseTime) {
        this.pauseTime = GoogleEmailUploaderConfig.MaximumPauseTime;
      }
    }

    void OnPause(PauseReason pauseReason) {
      Debug.Assert(this.modelState == ModelState.Uploading);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.OnPause(PauseReason)");
        this.mailUploader.PauseUpload();
        if (this.UploadPausedEvent != null) {
          this.UploadPausedEvent(pauseReason);
        }
        this.modelState = ModelState.UploadingPause;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.OnPause(PauseReason)");
      }
    }

    void OnResume() {
      Debug.Assert(this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.OnResume");
        if (this.pauseTimer != null) {
          this.pauseTimer.Dispose();
          this.pauseTimer = null;
        }
        this.modelState = ModelState.Uploading;
        this.mailUploader.ResumeUpload();
        if (this.UploadPausedEvent != null) {
          this.UploadPausedEvent(PauseReason.Resuming);
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.OnResume");
      }
    }

    void ProcessUploadContactEntry(ContactEntry contactEntry) {
      Debug.Assert(this.modelState == ModelState.UploadingPause ||
                   this.modelState == ModelState.Uploading);
      if (contactEntry.Uploaded) {
        contactEntry.StoreModel.SuccessfullyUploaded(contactEntry.ContactId);
        this.uploadedContactCount++;
      } else {
        FailedContactDatum failedContactDatum =
            new FailedContactDatum(contactEntry.ContactName,
                                   contactEntry.FailedReason);
        contactEntry.StoreModel.FailedToUpload(contactEntry.ContactId,
                                               failedContactDatum);
        this.failedContactCount++;
      }
      this.lkgStatePersistor.SaveLKGState(this);
    }

    void ProcessUploadMailBatchData(MailBatch mailBatch) {
      Debug.Assert(this.modelState == ModelState.UploadingPause ||
                   this.modelState == ModelState.Uploading);
      foreach (MailBatchDatum batchDatum in mailBatch.MailBatchData) {
        if (batchDatum.Uploaded) {
          batchDatum.FolderModel.SuccessfullyUploaded(batchDatum.MailId);
          this.uploadedEmailCount++;
        } else {
          FailedMailDatum failedMailDatum =
              new FailedMailDatum(batchDatum.MessageHead,
                                  batchDatum.FailedReason);
          batchDatum.FolderModel.FailedToUpload(batchDatum.MailId,
                                                failedMailDatum);
          this.failedEmailCount++;
        }
      }
      this.lkgStatePersistor.SaveLKGState(this);
    }

    void WriteCurrentStatistics(MailBatch mailBatch) {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat(
          "RamainingItemCount: {0}",
          this.RemainingItemCount);
      sb.AppendFormat(
          " UploadedEmailCount: {0} SelectedEmailCount: {1}" +
              " FailedEmailCount: {2}",
          this.UploadedEmailCount,
          this.SelectedEmailCount,
          this.FailedEmailCount);
      sb.AppendFormat(
          " UploadedContactCount: {0} SelectedContactCount: {1}" +
              " FailedContactCount: {2}",
          this.UploadedContactCount,
          this.SelectedContactCount,
          this.FailedContactCount);
      sb.AppendFormat(
          " UploadSpeed: {0}",
          this.UploadSpeed);
      sb.AppendFormat(
          " UploadTimeRemaining: {0}",
          this.UploadTimeRemaining);
      if (mailBatch != null) {
        sb.AppendFormat(
            " mailBatch.MailCount: {0}",
            mailBatch.MailCount);
        sb.AppendFormat(
            " mailBatch.FailedCount: {0}",
            mailBatch.FailedCount);
      }
      GoogleEmailUploaderTrace.WriteLine(sb.ToString());
    }

    internal bool IsEmailAddressCollision(IEnumerable emailAddresses) {
      if (this.contactEmailData == null) {
        this.contactEmailData = new Hashtable();
        using (ContactIterator localContactIterator =
            new ContactIterator(this.flatStoreModelList)) {
          while (localContactIterator.MoveToNextContact()) {
            IContact iterContact = localContactIterator.CurrentContact;
            foreach (EmailContact emailContact in iterContact.EmailAddresses) {
              string emailId = emailContact.EmailAddress.ToLower();
              if (this.contactEmailData.ContainsKey(emailId)) {
                int count = (int)this.contactEmailData[emailId];
                count++;
                this.contactEmailData[emailId] = count;
              } else {
                this.contactEmailData[emailId] = 1;
              }
            }
          }
        }
      }
      foreach (EmailContact emailContact in emailAddresses) {
        string emailId = emailContact.EmailAddress.ToLower();
        if (this.contactEmailData.ContainsKey(emailId)) {
          int number = (int)this.contactEmailData[emailId];
          if (number > 1) {
            return true;
          }
        }
      }
      return false;
    }

    internal IContact GetNextContactEntry(out StoreModel storeModel) {
      Debug.Assert(this.modelState == ModelState.Uploading ||
                   this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.GetNextContactEntry()");
        storeModel = null;
        if (this.contactIterator.MoveToNextContact()) {
          if (this.ContactReadingEvent != null) {
            this.ContactReadingEvent(this.contactIterator.CurrentContact);
          }
          storeModel = this.contactIterator.CurrentStoreModel;
          return this.contactIterator.CurrentContact;
        }
        return null;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.GetNextContactEntry()");
      }
    }

    internal void FillMailBatch(MailBatch mailBatch) {
      Debug.Assert(this.modelState == ModelState.Uploading ||
                   this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(  
            "GoogleEmailUploaderModel.FillMailBatch");
        this.WriteCurrentStatistics(mailBatch);
        if (this.MailBatchFillingStartEvent != null) {
          this.MailBatchFillingStartEvent(mailBatch);
        }
        if (this.useCurrent) {
          this.mailUploader.PauseEvent.WaitOne();
          bool added =
              mailBatch.AddMail(
                  this.mailIterator.CurrentMail,
                  this.mailIterator.CurrentFolderModel);
          Debug.Assert(added);
          if (this.MailBatchFillingEvent != null) {
            this.MailBatchFillingEvent(mailBatch,
                                       this.mailIterator.CurrentMail);
          }
          this.useCurrent = false;
        }
        while (this.mailIterator.MoveToNextMail()) {
          this.mailUploader.PauseEvent.WaitOne();
          if (this.mailIterator.CurrentMail.Rfc822Buffer.Length == 0) {
            // If we cant read the mail behave as if we have successfully
            // uploaded the mail.
            this.mailIterator.CurrentFolderModel.SuccessfullyUploaded(
                this.mailIterator.CurrentMail.MailId);
            this.uploadedEmailCount++;
            continue;
          }
          if (!mailBatch.AddMail(
                  this.mailIterator.CurrentMail,
                  this.mailIterator.CurrentFolderModel)) {
            // we could not add the current mail
            // so record so that we would add it in the next iteration.
            this.useCurrent = true;
            break;
          }
          if (this.MailBatchFillingEvent != null) {
            this.MailBatchFillingEvent(mailBatch,
                                       this.mailIterator.CurrentMail);
          }
          if (mailBatch.IsBatchFilled()) {
            // If mailbatch is filled to the rim then we cant add more.
            break;
          }
        }
        if (this.MailBatchFillingEndEvent != null) {
          this.MailBatchFillingEndEvent(mailBatch);
        }
      } finally {
        this.WriteCurrentStatistics(mailBatch);
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.FillMailBatch");
      }
    }

    internal void MailBatchUploadTryStart(MailBatch mailBatch) {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.MailBatchUploadStart");
        if (GoogleEmailUploaderConfig.LogFullXml) {
          GoogleEmailUploaderTrace.WriteLine("Request Xml:");
          GoogleEmailUploaderTrace.WriteXml(mailBatch.GetBatchXML());
        }
        this.WriteCurrentStatistics(mailBatch);
        if (this.MailBatchUploadTryStartEvent != null) {
          this.MailBatchUploadTryStartEvent(mailBatch);
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.MailBatchUploadTryStart");
      }
    }

    internal void ContactEntryUploadTryStart(ContactEntry contactEntry) {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.ContactBatchUploadTryStart()");
        if (GoogleEmailUploaderConfig.LogFullXml) {
          GoogleEmailUploaderTrace.WriteLine("Request Xml:");
          GoogleEmailUploaderTrace.WriteXml(contactEntry.GetEntryXML());
        }
        if (this.ContactUploadTryStartEvent != null) {
          this.ContactUploadTryStartEvent(contactEntry);
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.ContactBatchUploadTryStart()");
      }
    }

    internal void HttpRequestFailure(
        HttpException httpException,
        string exceptionResponseString,
        UploadResult batchUploadResult) {
      Debug.Assert(batchUploadResult == UploadResult.Unauthorized ||
                   batchUploadResult == UploadResult.Forbidden ||
                   batchUploadResult == UploadResult.BadGateway ||
                   batchUploadResult == UploadResult.OtherException);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.HttpRequestFailure");
        this.WriteCurrentStatistics(null);

        string headersString = string.Empty;
        if (httpException.Response != null) {
          headersString = httpException.Response.Headers;
        }
        GoogleEmailUploaderTrace.WriteLine(
            "Exception: {0}", httpException.Message);
        GoogleEmailUploaderTrace.WriteLine("Response Headers: {0}",
                                           headersString);
        GoogleEmailUploaderTrace.WriteLine("Response message: {0}",
                                           exceptionResponseString);
        if (batchUploadResult != UploadResult.Unauthorized &&
            batchUploadResult != UploadResult.Forbidden &&
            batchUploadResult != UploadResult.Conflict) {
          this.TimedPauseUpload(PauseReason.ConnectionFailures);
          this.RaisePauseTime();
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.HttpRequestFailure");
      }
    }

    internal bool ContactEntryUploadFailure(
        ContactEntry contactEntry,
        UploadResult uploadResult) {
      Debug.Assert(uploadResult == UploadResult.InternalError ||
          uploadResult == UploadResult.ServiceUnavailable ||
          uploadResult == UploadResult.Conflict ||
          uploadResult == UploadResult.Unknown);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.ContactBatchUploadFailure()");
        if (GoogleEmailUploaderConfig.LogFullXml) {
          GoogleEmailUploaderTrace.WriteLine("Response Xml:");
          GoogleEmailUploaderTrace.WriteXml(contactEntry.ResponseString);
        }
        GoogleEmailUploaderTrace.WriteLine("Contact upload failure ({0}): {1}",
                                 uploadResult,
                                 contactEntry.ContactName);
        if (uploadResult == UploadResult.InternalError ||
            uploadResult == UploadResult.Conflict ||
            uploadResult == UploadResult.Unknown) {
          if (this.pauseTime == GoogleEmailUploaderConfig.MaximumPauseTime) {
            GoogleEmailUploaderTrace.WriteLine(
                "Contact failed with internal error");
            GoogleEmailUploaderTrace.WriteLine("Xml -");
            GoogleEmailUploaderTrace.WriteXml(contactEntry.GetEntryXML());
            GoogleEmailUploaderTrace.WriteLine("Result -");
            GoogleEmailUploaderTrace.WriteXml(contactEntry.ResponseString);
            this.ProcessUploadContactEntry(contactEntry);
            this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
            return false;
          }
          this.TimedPauseUpload(PauseReason.ServerInternalError);
          this.RaisePauseTime();
        } else {
          GoogleEmailUploaderTrace.WriteLine(
              "Batch failed with service unavailable");
          this.TimedPauseUpload(PauseReason.ServiceUnavailable);
          this.RaisePauseTime();
        }
        return true;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.ContactBatchUploadFailure()");
      }
    }

    internal bool MailBatchUploadFailure(
        MailBatch mailBatch,
        UploadResult batchUploadResult) {
      Debug.Assert(batchUploadResult == UploadResult.InternalError ||
          batchUploadResult == UploadResult.ServiceUnavailable ||
          batchUploadResult == UploadResult.Unknown);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.MailBatchUploadFailure");
        if (GoogleEmailUploaderConfig.LogFullXml) {
          GoogleEmailUploaderTrace.WriteLine("Response Xml:");
          GoogleEmailUploaderTrace.WriteXml(mailBatch.ResponseXml);
        }
        this.WriteCurrentStatistics(mailBatch);
        if (batchUploadResult == UploadResult.InternalError ||
            batchUploadResult == UploadResult.Unknown) {
          if (this.pauseTime == GoogleEmailUploaderConfig.MaximumPauseTime) {
            this.ProcessUploadMailBatchData(mailBatch);
            this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
            return false;
          }
          this.TimedPauseUpload(PauseReason.ServerInternalError);
          this.RaisePauseTime();
        } else {
          GoogleEmailUploaderTrace.WriteLine(
              "Batch failed with service unavailable");
          this.TimedPauseUpload(PauseReason.ServiceUnavailable);
          this.RaisePauseTime();
        }
        return true;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.MailBatchUploadFailure");
      }
    }

    internal void ContactEntryUploaded(ContactEntry contactEntry,
                                       UploadResult uploadResult) {
      Debug.Assert(uploadResult == UploadResult.BadRequest ||
                   uploadResult == UploadResult.Created);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.ContactEntryUploaded()");
        if (GoogleEmailUploaderConfig.LogFullXml) {
          GoogleEmailUploaderTrace.WriteLine("Response:");
          GoogleEmailUploaderTrace.WriteXml(contactEntry.ResponseString);
        }
        GoogleEmailUploaderTrace.WriteLine("Contact uploaded: {0}",
                                           contactEntry.ContactName);
        this.ProcessUploadContactEntry(contactEntry);
        if (this.ContactUploadedEvent != null) {
          this.ContactUploadedEvent(contactEntry);
        }
        this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.ContactEntryUploaded()");
      }
    }

    internal void MailBatchUploaded(
        MailBatch mailBatch,
        UploadResult batchUploadResult) {
      Debug.Assert(batchUploadResult == UploadResult.BadRequest ||
                   batchUploadResult == UploadResult.Created);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.MailBatchUploaded");
        this.ProcessUploadMailBatchData(mailBatch);
        this.WriteCurrentStatistics(mailBatch);
        if (this.MailBatchUploadedEvent != null) {
          this.MailBatchUploadedEvent(mailBatch);
        }
        this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.MailBatchUploaded");
      }
    }

    internal void UploadDone(DoneReason doneReason) {
      if (this.UploadDoneEvent != null) {
        this.UploadDoneEvent(doneReason);
      }
    }
  }
}
