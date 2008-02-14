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
    static int maximumMailsPerBatch = 40;
    static int normalMailBatchSize = 512 * 1024;
    static int maximumMailBatchSize = 16 * 1024 * 1024;
    static int minimumPauseTime = 90;  // 90 seconds
    static int maximumPauseTime = 180;  // 180 seconds
    static int failedMailHeadLineCount = 32;
    static string emailMigrationUrl =
        "https://apps-apis.google.com/a/feeds/migration/2.0/{0}/{1}/mail/batch";
    static bool traceEnabled = true;

    public static void InitializeConfiguration() {
      System.Console.WriteLine(System.Environment.OSVersion.Version);
      string value;
      try {
        value = ConfigurationSettings.AppSettings["MaximumMailsPerBatch"];
        if (value != null) {
          GoogleEmailUploaderConfig.maximumMailsPerBatch = int.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["NormalMailBatchSize"];
        if (value != null) {
          GoogleEmailUploaderConfig.normalMailBatchSize = int.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["MaximumMailBatchSize"];
        if (value != null) {
          GoogleEmailUploaderConfig.maximumMailBatchSize = int.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["MinimumPauseTime"];
        if (value != null) {
          GoogleEmailUploaderConfig.minimumPauseTime = int.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["MaximumPauseTime"];
        if (value != null) {
          GoogleEmailUploaderConfig.maximumPauseTime = int.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["FailedMailHeadLineCount"];
        if (value != null) {
          GoogleEmailUploaderConfig.failedMailHeadLineCount = int.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["EmailMigrationUrl"];
        if (value != null) {
          GoogleEmailUploaderConfig.emailMigrationUrl = value;
        }
      } catch {
        // If there is error let the default value stay
      }
      try {
        value = ConfigurationSettings.AppSettings["TraceEnabled"];
        if (value != null) {
          GoogleEmailUploaderConfig.traceEnabled = bool.Parse(value);
        }
      } catch {
        // If there is error let the default value stay
      }
    }

    internal static int MaximumMailsPerBatch {
      get {
        return GoogleEmailUploaderConfig.maximumMailsPerBatch;
      }
    }

    internal static int NormalMailBatchSize {
      get {
        return GoogleEmailUploaderConfig.normalMailBatchSize;
      }
    }

    internal static int MaximumMailBatchSize {
      get {
        return GoogleEmailUploaderConfig.maximumMailBatchSize;
      }
    }

    internal static int MinimumPauseTime {
      get {
        return GoogleEmailUploaderConfig.minimumPauseTime;
      }
    }

    internal static int MaximumPauseTime {
      get {
        return GoogleEmailUploaderConfig.maximumPauseTime;
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

    protected virtual void ResetLKG() {
      this.isSelected = false;
    }

    public void ResetLKGRec() {
      this.ResetLKG();
      foreach (TreeNodeModel child in this.Children) {
        child.ResetLKGRec();
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

  public sealed class StoreModel : TreeNodeModel {
    public readonly IStore Store;
    readonly ArrayList folderModels;

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
    }

    public override string DisplayName {
      get { return this.Store.DisplayName; }
    }

    public override IEnumerable Children {
      get { return this.folderModels; }
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
    string lastUploadedMailId;
    ArrayList failedMailData;

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
      this.failedMailData = new ArrayList();
      this.lastUploadedMailId = string.Empty;
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
          sb.Append('/');
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

        string fromLabel =
            string.Format(Resources.FromTemplateText,
                          this.ClientModel.DisplayName);
        // If the user has opted not to use folder to label
        // mapping.
        if (!this.GoogleEmailUploaderModel.IsFolderToLabelMappingEnabled
            || this.FolderPath.Length == 0) {
          this.labels = new string[] {
              fromLabel};
        } else {
          this.labels = new string[] {
            fromLabel,
            this.FolderPath};
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
      set {
        this.uploadedMailCount = value;
      }
    }

    public uint FailedMailCount {
      get {
        return this.failedMailCount;
      }
      set {
        this.failedMailCount = value;
      }
    }

    public string LastUploadedMailId {
      get {
        return this.lastUploadedMailId;
      }
      set {
        this.lastUploadedMailId = value;
      }
    }

    public ArrayList FailedMailData {
      get {
        return this.failedMailData;
      }
    }

    protected override void ResetLKG() {
      base.ResetLKG();
      this.uploadedMailCount = 0;
      this.failedMailCount = 0;
      this.failedMailData.Clear();
      this.lastUploadedMailId = string.Empty;
    }
  }

  class MailIterator : IDisposable {
    readonly ArrayList folderModelFlatList;
    // Since we are keeping track of considered and failed mail count
    // in GoogleEmailUploaderModel, we use the delegate to do the updating of
    // the counts. Alternatively we could directly access the fields in
    // GoogleEmailUploaderModel.
    readonly VoidDelegate consideredMailIncrementDelegate;
    readonly VoidDelegate failedMailIncrementDelegate;
    int folderIterationIndex;
    FolderModel currentFolderModel;
    IEnumerator currentFolderEnumerator;
    IMail currentMail;

    internal MailIterator(ArrayList folderModelFlatList,
                          VoidDelegate consideredMailIncrementDelegate,
                          VoidDelegate failedMailIncrementDelegate) {
      this.folderModelFlatList = folderModelFlatList;
      this.consideredMailIncrementDelegate = consideredMailIncrementDelegate;
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
        if (!this.currentFolderModel.IsSelected) {
          continue;
        }
        this.currentFolderEnumerator =
            this.currentFolderModel.Folder.Mails.GetEnumerator();
        string lastUpLoadedMailId =
            this.currentFolderModel.LastUploadedMailId;
        if (lastUpLoadedMailId != string.Empty) {
          // if there is last uploaded mail id
          // we skip till that point and return
          while (this.currentFolderEnumerator.MoveNext()) {
            IMail currentMail = (IMail)this.currentFolderEnumerator.Current;
            if (currentMail.MailId == lastUpLoadedMailId) {
              // Found the last uploaded mail so we break.
              break;
            }
          }
        }
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
        this.consideredMailIncrementDelegate();
        byte[] rfcByteBuffer = this.currentMail.Rfc822Buffer;
        if (rfcByteBuffer.Length > 0 &&
            rfcByteBuffer.Length <=
                GoogleEmailUploaderConfig.MaximumMailBatchSize) {
          return true;
        }
        this.failedMailIncrementDelegate();
        if( rfcByteBuffer.Length != 0){
          string mailHead = MailBatch.GetMailHeader(this.currentMail);
          this.currentFolderModel.FailedMailData.Add(
            new FailedMailDatum(mailHead, "Mail too large"));
        }
        this.currentFolderModel.FailedMailCount++;
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
  }

  public delegate void MailBatchDelegate(MailBatch mailBatch);

  public delegate void MailDelegate(MailBatch mailBatch,
                                    IMail mail);

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

  public delegate void VoidDelegate();

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
    const string AppsServiceName = "apps";

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
    string authenticationToken;
    LKGStatePersistor lkgStatePersistor;
    ArrayList clientModels;
    ArrayList flatFolderModelList;
    bool isFolderToLabelMappingEnabled;
    bool isArchiveEverything;

    // In uploading state+ these will be non null
    MailUploader mailUploader;
    MailIterator mailIterator;
    Timer pauseTimer;
    public event MailDelegate MailBatchFillingEvent;
    public event MailBatchDelegate MailBatchFilledEvent;
    public event MailBatchDelegate MailBatchUploadTryStartEvent;
    public event UploadPausedDelegate UploadPausedEvent;
    public event PauseCountDownDelegate PauseCountDownEvent;
    public event MailBatchDelegate MailBatchUploadedEvent;
    public event UploadDoneDelegate UploadDoneEvent;
    // This indicates that we should use mailIterator's current mail before
    // asking for more emails.
    bool useCurrent;
    uint selectedMailCount;
    uint uploadedMailCount;
    uint consideredMailCount;
    uint failedMailCount;
    int pauseTime;
    double uploadSpeed; // Milliseconds per mail

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
                          Resources.Locale);
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

    void FillFolderModelsRec(ArrayList folderList,
                             IEnumerable forestModels) {
      foreach (TreeNodeModel tnm in forestModels) {
        FolderModel fm = tnm as FolderModel;
        if (fm != null) {
          folderList.Add(fm);
        }
        this.FillFolderModelsRec(
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
                                  this.ApplicationName,
                                  GoogleEmailUploaderModel.AppsServiceName);
      this.emailId = null;
      this.password = null;
      this.authenticationToken = null;
      this.lkgStatePersistor = null;
      this.flatFolderModelList = null;
      this.mailUploader = null;
      this.mailIterator = null;
      this.pauseTimer = null;
      this.modelState = ModelState.Initialized;
    }

    void TransitionToSignedInState(string emailId,
                                   string password,
                                   string authenticationToken) {
      Debug.Assert(this.modelState == ModelState.Initialized);
      this.emailId = emailId;
      this.password = password;
      this.authenticationToken = authenticationToken;
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
      this.BuildFolderModelFlatList();
      this.mailUploader = new MailUploader(this.HttpFactory,
                                           this.emailId,
                                           this.authenticationToken,
                                           this.ApplicationName,
                                           this);
    }

    void TransitionToUploadingState() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.lkgStatePersistor.SaveLKGState(this);
      this.ComputeMailCounts();
      this.mailIterator =
          new MailIterator(
              this.flatFolderModelList,
              new VoidDelegate(this.IncrementConsideredMailCount),
              new VoidDelegate(this.IncrementFailedMailCount));
      this.useCurrent = false;
      this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
      this.modelState = ModelState.Uploading;
    }

    void IncrementConsideredMailCount() {
      this.consideredMailCount++;
    }

    void IncrementFailedMailCount() {
      this.failedMailCount++;
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
          this.gaiaAuthenticator.Authenticate(
              emailId,
              password);
        if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.Authenticated) {
          this.TransitionToSignedInState(
              emailId,
              password,
              authResponse.AuthToken);
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
            this.gaiaAuthenticator.AuthenticateCAPTCHA(
                emailId,
                password,
                captchaToken,
                captchaSolution);
        if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.Authenticated) {
          this.TransitionToSignedInState(
              emailId,
              password,
              authResponse.AuthToken);
        }
        return authResponse;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.SignInCAPTCHA");
      }
    }

    /// <summary>
    /// Call to this method causes amnesia with respect to LKG state.
    /// </summary>
    public void ClearLKG() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      foreach (ClientModel clientModel in this.clientModels) {
        clientModel.ResetLKGRec();
      }
      this.lkgStatePersistor.SaveLKGState(this);
    }

    /// <summary>
    /// This method builds the array of flattened folder models.
    /// </summary>
    public void BuildFolderModelFlatList() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.flatFolderModelList = new ArrayList();
      this.FillFolderModelsRec(this.flatFolderModelList,
                               this.clientModels);
      this.ComputeMailCounts();
    }

    /// <summary>
    /// This methods computes various mail counts. This method is
    /// expected to be called everytime the selection is changed by the UI.
    /// </summary>
    public void ComputeMailCounts() {
      Debug.Assert(this.modelState == ModelState.SignedIn);
      this.selectedMailCount = 0;
      this.consideredMailCount = 0;
      this.failedMailCount = 0;
      foreach (FolderModel fm in this.flatFolderModelList) {
        if (!fm.IsSelected) {
          continue;
        }
        this.selectedMailCount += fm.Folder.MailCount;
        // All the mails that were uplaoded or failed are considered.
        this.consideredMailCount += fm.UploadedMailCount;
        this.consideredMailCount += fm.FailedMailCount;
        this.failedMailCount += fm.FailedMailCount;
      }
      this.uploadedMailCount = this.consideredMailCount - this.failedMailCount;
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

    internal BatchUploadResult TestUpload() {
      Debug.Assert  (this.modelState == ModelState.SignedIn);
      double uploadMilliseconds;
      BatchUploadResult result =
          this.mailUploader.TestUpload(out uploadMilliseconds);
      this.uploadSpeed = 1 / uploadMilliseconds;
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
    /// This method aborts the upload. It does this by aborting the
    /// upload thread. When the thread is aborted finally, UploadDoneEvent is
    /// raised. There might be slight delay before the thread actually aborts
    /// because of synchronized calls on the network sockets.
    /// </summary>
    public void AbortUpload() {
      Debug.Assert(this.modelState == ModelState.Uploading ||
                   this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.AbortUpload");
        this.mailUploader.AbortUpload();
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.AbortUpload");
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
          this.authenticationToken = null;
          this.lkgStatePersistor.SaveLKGState(this);
          this.lkgStatePersistor = null;
          this.flatFolderModelList = null;
          this.DisposeClientModels();
        } else if (this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause) {
          this.emailId = null;
          this.password = null;
          this.authenticationToken = null;
          this.lkgStatePersistor = null;
          this.flatFolderModelList = null;
          this.mailUploader = null;
          this.mailIterator.Dispose();
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
    /// returns the collection of client factories
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
    /// Total number of mails available in all the selected folders.
    /// </summary>
    public uint SelectedMailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.selectedMailCount;
      }
    }

    /// <summary>
    /// Number mails that are either uploaded or failed or being uploaded.
    /// </summary>
    public uint ConsideredMailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.consideredMailCount;
      }
    }

    /// <summary>
    /// Number mails that are remaining to be uploaded.
    /// </summary>
    public uint RemainingMailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.selectedMailCount - this.consideredMailCount;
      }
    }

    /// <summary>
    /// Number of mails that failed to be uploaded eiter because of large size
    /// or because they could not be read from the client.
    /// </summary>
    public uint FailedMailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.failedMailCount;
      }
    }

    /// <summary>
    /// Number of mails that got uploaded.
    /// </summary>
    public uint UploadedMailCount {
      get {
        Debug.Assert(this.modelState == ModelState.SignedIn ||
            this.modelState == ModelState.Uploading ||
            this.modelState == ModelState.UploadingPause);
        return this.uploadedMailCount;
      }
    }

    /// <summary>
    /// Returns the time remaining for uploading rest of the mails.
    /// </summary>
    public TimeSpan UploadTimeRemaining {
      get {
        double millisecsNeeded =
            (this.selectedMailCount - this.consideredMailCount) /
                this.uploadSpeed;
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(millisecsNeeded);
        timeSpan = timeSpan.Subtract(
            TimeSpan.FromMilliseconds(timeSpan.Milliseconds));
        return timeSpan;
      }
    }

    /// <summary>
    /// Returns the upload speed in terms of mails per millisecond.
    /// </summary>
    public double UploadSpeed {
      get {
        return this.uploadSpeed;
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
    /// Returns the current folder model being considered.
    /// </summary>
    public FolderModel CurrentFolderModel {
      get {
        return this.mailIterator.CurrentFolderModel;
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
        return Resources.LessThan10Mins;
      } else if (remainingTime < 30.0 * 60) {
        return Resources.LessThan30Mins;
      } else if (remainingTime < 1.0 * 60 * 60) {
        return Resources.LessThan1Hour;
      } else if (remainingTime < 3.0 * 60 * 60) {
        return Resources.Between1To3Hours;
      } else if (remainingTime < 5.0 * 60 * 60) {
        return Resources.Between3To5Hours;
      } else if (remainingTime < 10.0 * 60 * 60) {
        return Resources.Between5To10Hours;
      } else if (remainingTime < 15.0 * 60 * 60) {
        return Resources.Between10To15Hours;
      } else if (remainingTime < 24.0 * 60 * 60) {
        return Resources.Between15To24Hours;
      } else {
        return Resources.MoreThanDay;
      }
    }

    internal void SetUploadSpeed(double uploadSpeed) {
      this.uploadSpeed = uploadSpeed;
    }

    internal void UpdateUploadSpeed(uint mailCount,
                                    TimeSpan timeTaken) {
      double oldTime = this.consideredMailCount / this.uploadSpeed;
      double totalMails = this.consideredMailCount + mailCount;
      double totalTimeTaken = oldTime + timeTaken.TotalMilliseconds;
      this.uploadSpeed = totalMails / totalTimeTaken;
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
            "GoogleEmailUploaderModel.OnResume()");
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
            "GoogleEmailUploaderModel.OnResume()");
      }
    }

    void ProcessUploadData(MailBatch mailBatch) {
      Debug.Assert(this.modelState == ModelState.UploadingPause ||
                   this.modelState == ModelState.Uploading);
      foreach (MailBatchDatum batchData in mailBatch.MailBatchData) {
        if (batchData.Uploaded) {
          batchData.FolderModel.UploadedMailCount++;
          this.uploadedMailCount++;
        } else {
          batchData.FolderModel.FailedMailCount++;
          this.failedMailCount++;
          batchData.FolderModel.FailedMailData.Add(
            new FailedMailDatum(batchData.MessageHead, batchData.FailedReason));
        }
        batchData.FolderModel.LastUploadedMailId = batchData.MailId;
      }
      this.lkgStatePersistor.SaveLKGState(this);
    }

    internal void FillMailBatch(MailBatch mailBatch) {
      Debug.Assert(this.modelState == ModelState.Uploading ||
                   this.modelState == ModelState.UploadingPause);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(  
            "GoogleEmailUploaderModel.FillMailBatch()");
        if (this.MailBatchFillingEvent != null) {
          this.MailBatchFillingEvent(mailBatch,
                                     null);
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
          IMail currentMail = this.mailIterator.CurrentMail;
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
        if (this.MailBatchFilledEvent != null) {
          this.MailBatchFilledEvent(mailBatch);
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.FillMailBatch()");
      }
    }

    internal void MailBatchUploadTryStart(MailBatch mailBatch) {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.MailBatchUploadStart()");
        if (this.MailBatchUploadTryStartEvent != null) {
          this.MailBatchUploadTryStartEvent(mailBatch);
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.MailBatchUploadStart()");
      }
    }

    internal void HttpRequestFailure(
        HttpException httpException,
        BatchUploadResult batchUploadResult) {
      Debug.Assert(batchUploadResult == BatchUploadResult.Unauthorized ||
                   batchUploadResult == BatchUploadResult.Forbidden ||
                   batchUploadResult == BatchUploadResult.BadGateway ||
                   batchUploadResult == BatchUploadResult.OtherException);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.HttpRequestFailure()");

        string exceptionResponseString = string.Empty;
        if (httpException.Response != null) {
          using (Stream respStream =
              httpException.Response.GetResponseStream()) {
            using (StreamReader textReader = new StreamReader(respStream)) {
              exceptionResponseString = textReader.ReadToEnd();
            }
          }
        }
        GoogleEmailUploaderTrace.WriteLine(
            "Exception: {0}", httpException.Message);
        GoogleEmailUploaderTrace.WriteLine("Response message: {0}",
                                 exceptionResponseString);
        if (batchUploadResult != BatchUploadResult.Unauthorized ||
            batchUploadResult != BatchUploadResult.Forbidden) {
          this.TimedPauseUpload(PauseReason.ConnectionFailures);
          this.RaisePauseTime();
        }
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.HttpRequestFailure()");
      }
    }

    internal bool MailBatchUploadFailure(
        MailBatch mailBatch,
        BatchUploadResult batchUploadResult) {
      Debug.Assert(batchUploadResult == BatchUploadResult.ServiceUnavailable ||
                   batchUploadResult == BatchUploadResult.InternalError);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.MailBatchUploadFailure()");
        GoogleEmailUploaderTrace.WriteLine("Batch statistics: {0}",
                                 mailBatch.GetBatchInfo());
        if (batchUploadResult == BatchUploadResult.InternalError) {
          if (this.pauseTime == GoogleEmailUploaderConfig.MaximumPauseTime) {
            GoogleEmailUploaderTrace.WriteLine(
                "Batch failed with internal error");
            GoogleEmailUploaderTrace.WriteLine("Batch -");
            GoogleEmailUploaderTrace.WriteXml(mailBatch.GetBatchXML());
            GoogleEmailUploaderTrace.WriteLine("Result -");
            GoogleEmailUploaderTrace.WriteXml(mailBatch.ResponseXml);
            this.ProcessUploadData(mailBatch);
            this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
            return false;
          }
          this.TimedPauseUpload(PauseReason.ServerInternalError);
          this.RaisePauseTime();
        } else {
          GoogleEmailUploaderTrace.WriteLine("Batch failed with service unavailable");
          this.TimedPauseUpload(PauseReason.ServiceUnavailable);
          this.RaisePauseTime();
        }
        return true;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.MailBatchUploadFailure()");
      }
    }

    internal void MailBatchUploaded(
        MailBatch mailBatch,
        BatchUploadResult batchUploadResult) {
      Debug.Assert(batchUploadResult == BatchUploadResult.BadRequest ||
                   batchUploadResult == BatchUploadResult.Created);
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleEmailUploaderModel.MailBatchUploaded()");
        GoogleEmailUploaderTrace.WriteLine("Batch statistics: {0}",
                                 mailBatch.GetBatchInfo());
        this.ProcessUploadData(mailBatch);
        if (this.MailBatchUploadedEvent != null) {
          this.MailBatchUploadedEvent(mailBatch);
        }
        this.pauseTime = GoogleEmailUploaderConfig.MinimumPauseTime;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleEmailUploaderModel.MailBatchUploaded()");
      }
    }

    internal void UploadDone(DoneReason doneReason) {
      if (this.UploadDoneEvent != null) {
        this.UploadDoneEvent(doneReason);
      }
    }
  }
}
