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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;

namespace GoogleEmailUploader {
  class MailBatchDatum {
    internal readonly FolderModel FolderModel;
    internal readonly string MailId;
    internal readonly string MessageHead;
    bool uploaded;
    string failedReason;

    internal MailBatchDatum(FolderModel folderModel,
                            string mailId,
                            string messageHead) {
      this.FolderModel = folderModel;
      this.MailId = mailId;
      this.MessageHead = messageHead;
      this.uploaded = true;
    }

    internal void SetFailure(string failedReason){
      this.uploaded = false;
      this.failedReason = failedReason;
    }

    internal bool Uploaded {
      get {
        return this.uploaded;
      }
    }

    internal string FailedReason {
      get {
        return this.failedReason;
      }
    }
  }

  /// <summary>
  /// This represents a set of mails to be loaded at a time. We build XML
  /// representation of DMAPI batch using the mails added.
  /// </summary>
  public class MailBatch {
    const string XmlNS = "http://www.w3.org/2000/xmlns/";
    const string AtomNS = "http://www.w3.org/2005/Atom";
    const string AppsNS = "http://schemas.google.com/apps/2006";
    const string GDataBatchNS =
        "http://schemas.google.com/gdata/batch";
    const string GDataKindURI = "http://schemas.google.com/g/2005#kind";
    const string AppsMailItemURI =
        "http://schemas.google.com/apps/2006#mailItem";
    const string FeedElementName = "feed";
    const string IdElementName = "id";
    const string StatusElementName = "status";
    const string CodeAttributeName = "code";
    const string ReasonAttributeName = "reason";
    const string UploadTestString =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<a:feed xmlns=""http://schemas.google.com/apps/2006"" xmlns:batch=""http://schemas.google.com/gdata/batch"" xmlns:a=""http://www.w3.org/2005/Atom"">
  <a:entry>
    <a:category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://schemas.google.com/apps/2006#mailItem"" />
    <rfc822Msg>
From: ""Google Mail Uploader"" &lt;noreply@google.com&gt;
Date: Mon, 16 Jul 2007 00:00:00 +0530
This is an upload test mail.
    </rfc822Msg>
    <mailItemProperty value=""IS_TRASH"" />
    <batch:id>0</batch:id>
  </a:entry>
</a:feed>";
    const string CreatedHttpCode = "201";
    const string BadRequestHttpCode = "400";
    const string InternalErrorHttpCode = "500";
    const string ServiceUnavailableHttpCode = "503";

    const int DefaultCopyStepSize = 16 * 1024;
    // [#x0-#x8], [#xB-#xC], [#xF-#x1F]
    static readonly bool[] IsIllegalXml = {
      true,   // 0x00
      true,   // 0x01
      true,   // 0x02
      true,   // 0x03
      true,   // 0x04
      true,   // 0x05
      true,   // 0x06
      true,   // 0x07
      true,   // 0x08
      false,  // 0x09
      false,  // 0x0A
      true,   // 0x0B
      true,   // 0x0C
      false,  // 0x0D
      false,  // 0x0E
      true,   // 0x0F
      true,   // 0x10
      true,   // 0x11
      true,   // 0x12
      true,   // 0x13
      true,   // 0x14
      true,   // 0x15
      true,   // 0x16
      true,   // 0x17
      true,   // 0x18
      true,   // 0x19
      true,   // 0x1A
      true,   // 0x1B
      true,   // 0x1C
      true,   // 0x1D
      true,   // 0x1E
      true,   // 0x1F
    };

    readonly GoogleEmailUploaderModel GoogleEmailUploaderModel;
    readonly MemoryStream MemoryStream;
    readonly char[] MemoryBufferArray;
    uint mailCount;
    FolderModel lastAddedFolderModel;
    XmlTextWriter batchXmlTextWriter;
    DateTime startDateTime;

    string responseXml;
    uint failedCount;
    internal readonly ArrayList MailBatchData;

    internal MailBatch(GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.GoogleEmailUploaderModel = googleEmailUploaderModel;
      this.MemoryStream = new MemoryStream(
          GoogleEmailUploaderConfig.MaximumMailBatchSize);
      this.MemoryBufferArray = new char[MailBatch.DefaultCopyStepSize];
      this.MailBatchData = new ArrayList();
    }

    public uint MailCount {
      get {
        return this.mailCount;
      }
    }

    public long Length {
      get {
        return this.MemoryStream.Length;
      }
    }

    public FolderModel LastAddedFolderModel {
      get {
        return this.lastAddedFolderModel;
      }
    }

    public void CopyTo(Stream stream) {
      this.MemoryStream.WriteTo(stream);
    }

    internal void CreateTestBatch() {
      this.MemoryStream.Position = 0;
      this.MemoryStream.SetLength(0);
      this.MailBatchData.Clear();
      this.mailCount = 1;
      byte[] uploadBuffer = new byte[MailBatch.UploadTestString.Length];
      uploadBuffer = Encoding.UTF8.GetBytes(MailBatch.UploadTestString);
      this.MemoryStream.Write(uploadBuffer,
                              0,
                              MailBatch.UploadTestString.Length);

      MailBatchDatum batchData =
          new MailBatchDatum(null,
                             String.Empty,
                             String.Empty);
      this.MailBatchData.Add(batchData);
    }

    internal void StartBatch() {
      this.MemoryStream.Position = 0;
      this.MemoryStream.SetLength(0);
      this.mailCount = 0;
      this.MailBatchData.Clear();
      this.lastAddedFolderModel = null;
      this.batchXmlTextWriter = new XmlTextWriter(this.MemoryStream,
                                             Encoding.UTF8);
      this.batchXmlTextWriter.Formatting = Formatting.Indented;
      // Start the document
      this.batchXmlTextWriter.WriteStartDocument();
      // Start the feed...
      this.batchXmlTextWriter.WriteStartElement("a",
                                           "feed",
                                           MailBatch.AtomNS);
      this.batchXmlTextWriter.WriteAttributeString("xmlns",
                                              MailBatch.AppsNS);
      this.batchXmlTextWriter.WriteAttributeString("xmlns",
                                              "batch",
                                              MailBatch.XmlNS,
                                              MailBatch.GDataBatchNS);
      this.startDateTime = DateTime.Now;
    }

    static bool IsAncestor(IFolder folder,
                           FolderKind folderKind) {
      while (folder != null) {
        if (folder.Kind == folderKind) {
          return true;
        }
        folder = folder.ParentFolder;
      }
      return false;
    }

    internal unsafe bool AddMail(IMail mail,
                                 FolderModel folderModel) {
      byte[] rfc822Buffer = mail.Rfc822Buffer;
      Debug.Assert(rfc822Buffer.Length <=
          GoogleEmailUploaderConfig.MaximumMailBatchSize);
      bool canAdd = (
          // If its multimail batch let it be almost default mail batch size
          this.mailCount > 0 &&
          this.MemoryStream.Length + rfc822Buffer.Length + 2048
            <= GoogleEmailUploaderConfig.NormalMailBatchSize
        ) || (
          // If this mail is HUGE then its ok to be in singleton batch.
          this.mailCount == 0 &&
          rfc822Buffer.Length <=
              GoogleEmailUploaderConfig.MaximumMailBatchSize);

      if (!canAdd) {
        return false;
      }

      this.batchXmlTextWriter.WriteStartElement("entry",
                                                MailBatch.AtomNS);
      {
        // Add the category element to the feed.
        this.batchXmlTextWriter.WriteStartElement("category",
                                                  MailBatch.AtomNS);
        this.batchXmlTextWriter.WriteAttributeString("scheme",
                                                     MailBatch.GDataKindURI);
        this.batchXmlTextWriter.WriteAttributeString("term",
                                                     MailBatch.AppsMailItemURI);
        this.batchXmlTextWriter.WriteEndElement();
      }
      // Write out batchId
      {
        this.batchXmlTextWriter.WriteStartElement("id",
                                                  MailBatch.GDataBatchNS);
        this.batchXmlTextWriter.WriteString(this.mailCount.ToString());
        this.batchXmlTextWriter.WriteEndElement();
      }
      // Write out rfc822...
      {
        byte[] rfc822Message = mail.Rfc822Buffer;
        this.batchXmlTextWriter.WriteStartElement("rfc822Msg",
                                                  MailBatch.AppsNS);
        fixed (byte* srcStart = rfc822Message) {
          byte* srcEnd = srcStart + rfc822Message.Length;
          fixed (char* destStart = this.MemoryBufferArray) {
            byte* srcCurrStart = srcStart;
            while (srcCurrStart < srcEnd) {
              // check the limits of the block
              byte* srcCurrEnd = srcCurrStart + MailBatch.DefaultCopyStepSize;
              if (srcCurrEnd > srcEnd) {
                srcCurrEnd = srcEnd;
              }
              byte* srcIter = srcCurrStart;
              char* destIter = destStart;
              // copy the block
              while (srcIter < srcCurrEnd) {
                byte b = *srcIter;
                srcIter++;
                // Filter illegal xml chars...
                if (b < 0x20 &&
                    MailBatch.IsIllegalXml[b]) {
                  continue;
                }
                *destIter = (char)b;
                destIter++;
              }
              // Write the block to xml stream
              this.batchXmlTextWriter.WriteChars(
                  this.MemoryBufferArray,
                  0,
                  (int)(destIter - destStart));
              // Move to next block
              srcCurrStart = srcCurrEnd;
            }
          }
        }
        this.batchXmlTextWriter.WriteEndElement();
      }
      // Write out mail item properties...
      {
        if (!mail.IsRead) {
          this.batchXmlTextWriter.WriteStartElement("mailItemProperty",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("value",
                                                       "IS_UNREAD");
          this.batchXmlTextWriter.WriteEndElement();
        }
        if (mail.IsStarred) {
          this.batchXmlTextWriter.WriteStartElement("mailItemProperty",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("value",
                                                       "IS_STARRED");
          this.batchXmlTextWriter.WriteEndElement();
        }
        if (MailBatch.IsAncestor(mail.Folder, FolderKind.Inbox) &&
            !this.GoogleEmailUploaderModel.IsArchiveEverything) {
          this.batchXmlTextWriter.WriteStartElement("mailItemProperty",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("value",
                                                       "IS_INBOX");
          this.batchXmlTextWriter.WriteEndElement();
        }
        if (MailBatch.IsAncestor(mail.Folder, FolderKind.Sent)) {
          this.batchXmlTextWriter.WriteStartElement("mailItemProperty",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("value",
                                                       "IS_SENT");
          this.batchXmlTextWriter.WriteEndElement();
        }
        if (MailBatch.IsAncestor(mail.Folder, FolderKind.Draft)) {
          this.batchXmlTextWriter.WriteStartElement("mailItemProperty",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("value",
                                                       "IS_DRAFT");
          this.batchXmlTextWriter.WriteEndElement();
        }
        if (MailBatch.IsAncestor(mail.Folder, FolderKind.Trash)) {
          this.batchXmlTextWriter.WriteStartElement("mailItemProperty",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("value",
                                                       "IS_TRASH");
          this.batchXmlTextWriter.WriteEndElement();
        }
      }
      // Write out labels...
      string[] labels = folderModel.Labels;
      {
        for (int i = 0; i < labels.Length; ++i) {
          this.batchXmlTextWriter.WriteStartElement("label",
                                                    MailBatch.AppsNS);
          this.batchXmlTextWriter.WriteAttributeString("labelName",
                                                       labels[i]);
          this.batchXmlTextWriter.WriteEndElement();
        }
      }
      this.batchXmlTextWriter.WriteEndElement();
      this.mailCount++;
      this.lastAddedFolderModel = folderModel;

      MailBatchDatum batchData =
          new MailBatchDatum(folderModel,
                             mail.MailId,
                             MailBatch.GetMailHeader(mail));
      this.MailBatchData.Add(batchData);
      return true;
    }

    internal static string GetMailHeader(IMail mail) {
      using (MemoryStream memoryStream =
          new MemoryStream(mail.Rfc822Buffer, false)) {
        using (StreamReader streamReader = new StreamReader(memoryStream)) {
          StringBuilder sb = new StringBuilder();
          int linesRead = 0;
          while (linesRead
                    < GoogleEmailUploaderConfig.FailedMailHeadLineCount &&
                 streamReader.Peek() != -1) {
            sb.Append(streamReader.ReadLine());
            sb.Append("\r\n");
            linesRead++;
          }
          sb.Append("...");
          return sb.ToString();
        }
      }
    }

    internal bool IsBatchFilled() {
      return this.MemoryStream.Length + 2048 >
          GoogleEmailUploaderConfig.NormalMailBatchSize;
    }

    internal void FinishBatch() {
      // Close the feed
      this.batchXmlTextWriter.WriteEndElement();
      // close the document
      this.batchXmlTextWriter.WriteEndDocument();
      this.batchXmlTextWriter.Flush();
    }

    internal string GetBatchXML() {
      this.MemoryStream.Seek(0, SeekOrigin.Begin);
      return Encoding.UTF8.GetString(this.MemoryStream.GetBuffer(),
                                     0,
                                     (int)this.MemoryStream.Length);
    }

    void GetEntryDetails(XmlElement entryElement,
                         out uint batchId,
                         out BatchUploadResult itemUploadResult,
                         out string failureReason) {
      batchId = 0xFFFFFFFF;
      itemUploadResult = BatchUploadResult.Unknown;
      failureReason = string.Empty;
      foreach (XmlNode childNode in entryElement.ChildNodes) {
        if (childNode.NamespaceURI != MailBatch.GDataBatchNS) {
          continue;
        }
        XmlElement xmlElement = childNode as XmlElement;
        if (xmlElement == null) {
          continue;
        }
        if (xmlElement.LocalName == MailBatch.IdElementName) {
          try {
            batchId = uint.Parse(xmlElement.InnerText);
          } catch {
            // Ignore the error while parsing batchId
          }
        } else if (xmlElement.LocalName == MailBatch.StatusElementName) {
          string retCode =
              xmlElement.GetAttribute(MailBatch.CodeAttributeName);
          string reason =
              xmlElement.GetAttribute(MailBatch.ReasonAttributeName);
          if (retCode == null || reason == null) {
            continue;
          }
          failureReason = reason;
          switch (retCode) {
            case MailBatch.CreatedHttpCode:
              itemUploadResult = BatchUploadResult.Created;
              break;
            case MailBatch.BadRequestHttpCode:
              itemUploadResult = BatchUploadResult.BadRequest;
              break;
            case MailBatch.InternalErrorHttpCode:
              itemUploadResult = BatchUploadResult.InternalError;
              break;
            case MailBatch.ServiceUnavailableHttpCode:
              itemUploadResult = BatchUploadResult.ServiceUnavailable;
              break;
          }
        }
      }
    }

    internal BatchUploadResult ProcessResponse(Stream responseStream) {
      BatchUploadResult batchUploadResult = BatchUploadResult.Created;
      this.failedCount = this.mailCount;
      try {
        XmlDocument responseXmlDocument = new XmlDocument();
        responseXmlDocument.Load(responseStream);
        if (responseXmlDocument.LastChild == null ||
            responseXmlDocument.LastChild.NamespaceURI != MailBatch.AtomNS ||
            responseXmlDocument.LastChild.LocalName !=
                MailBatch.FeedElementName) {
          return BatchUploadResult.Unknown;
        }
        XmlElement feedElement = responseXmlDocument.LastChild as XmlElement;
        if (feedElement == null) {
          return BatchUploadResult.Unknown;
        }
        foreach (XmlNode childNode in feedElement.ChildNodes) {
          if (childNode.LocalName != "entry" ||
              childNode.NamespaceURI != MailBatch.AtomNS) {
            continue;
          }
          XmlElement entryElement = childNode as XmlElement;
          if (entryElement == null) {
            continue;
          }
          uint batchId;
          string failueReason;
          BatchUploadResult itemUploadResult;
          this.GetEntryDetails(entryElement,
                               out batchId,
                               out itemUploadResult,
                               out failueReason);
          if (batchId >= this.mailCount) {
            continue;
          }
          if (itemUploadResult != BatchUploadResult.Created) {
            ((MailBatchDatum)this.MailBatchData[(int)batchId]).SetFailure(
                failueReason);
          } else {
            this.failedCount--;
          }
          if (itemUploadResult < batchUploadResult) {
            batchUploadResult = itemUploadResult;
          }
        }
        StringBuilder sb = new StringBuilder(1024);
        using (StringWriter stringWriter = new StringWriter(sb)) {
          XmlTextWriter responseXmlTextWriter =
              new XmlTextWriter(stringWriter);
          responseXmlTextWriter.Formatting = Formatting.Indented;
          responseXmlDocument.WriteTo(responseXmlTextWriter);
          responseXmlTextWriter.Close();
        }
        this.responseXml = sb.ToString();
        return batchUploadResult;
      } catch (XmlException) {
        return BatchUploadResult.Unknown;
      }
    }

    internal string ResponseXml {
      get {
        return this.responseXml;
      }
    }

    internal DateTime StartDateTime {
      get {
        return this.startDateTime;
      }
    }

    internal string GetBatchInfo() {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("Size: {0} MailCount: {1} FailCount: {2}",
                      this.MemoryStream.Length,
                      this.mailCount,
                      this.failedCount);
      return sb.ToString();
    }
  }

  /// <summary>
  /// Reason why the upload is considered done
  /// </summary>
  public enum DoneReason {
    /// <summary>
    /// The upload was aborted
    /// </summary>
    Aborted,

    /// <summary>
    /// The upload unauthorized because of wrong username/password
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The upload is forbidden because user might not have agreed to the
    /// terms of service.
    /// </summary>
    Forbidden,

    /// <summary>
    /// The upload is completed. All the mails were uploaded.
    /// </summary>
    Completed,
  }

  /// <summary>
  /// The result of TryUpload method. If a batch has multiple of these for
  /// individual items the least value is the value for the batch.
  /// </summary>
  enum BatchUploadResult {
    /// <summary>
    /// Unrecognized return code.
    /// Action: Continue with next upload.
    /// </summary>
    Unknown,

    /// <summary>
    /// The client provides wrong login credentials (username and password).
    /// Action: Go back to login screen
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Various authorization errors occur (you try to migrate mail to the
    /// wrong destination user; your account is suspended; you haven't agreed
    /// to the TOS; etc.)
    /// Action: Give corresponding message in UI and show a helper url.
    /// Then go to login screen.
    /// </summary>
    Forbidden,

    /// <summary>
    /// This has been seen occasionally if the connection gets severed during
    /// a large upload. It is not actually a DMAPI-specific error, and should
    /// happen infrequently.
    /// Action: Retry again
    /// </summary>
    BadGateway,

    /// <summary>
    /// Some other exception occured.
    /// Action: Retry again.
    /// </summary>
    OtherException,

    /// <summary>
    /// The mail upload rate is being intentionally throttled to avoid
    /// overwhelming the message router. Try again in 30 seconds.
    /// Action: Sleep for some time.
    /// </summary>
    ServiceUnavailable,

    /// <summary>
    /// Something in the DMAPI or GData server broke. Hopefully this should
    /// never happen.
    /// Action: Note the failure count and continue with next upload.
    /// </summary>
    InternalError,

    /// <summary>
    /// The XML is malformed (sent at the GData parsing level), or the mail
    /// message is malformed (at the DMAPI level). For example, sent if the
    /// mail message has invalid headers.
    /// Action: Note the failure count and continue with next upload.
    /// </summary>
    BadRequest,

    /// <summary>
    /// A mail item was inserted successfully.
    /// Action: Continue with next upload.
    /// </summary>
    Created,
  }

  // This class is meant to be used by a single thread.
  // That thread will be blocked at GetAvailableMailBatch or UploadBatch
  class MailUploader {
    const string MigrationURLTemplate =
        "https://apps-apis.google.com/a/feeds/migration/2.0/{0}/{1}/mail/batch";
    const string AuthorizationHeaderTag = "Authorization";
    const string GoogleAuthorizationTemplate = "GoogleLogin auth={0}";
    const string ContentType = "application/atom+xml; charset=UTF-8";

    readonly IHttpFactory HttpFactory;
    readonly string EmailId;
    readonly string UserName;
    readonly string DomainName;
    readonly string AuthenticationToken;
    // https://apps-api.google.com/a/feeds/migration/2.0/domain.com/user/mail/batch
    readonly string batchMailUploadUrl;
    readonly GoogleEmailUploaderModel GoogleEmailUploaderModel;
    readonly MailBatch MailBatch;
    internal readonly ManualResetEvent PauseEvent;
    readonly string ApplicationName;

    Thread UploadThread;

    internal MailUploader(IHttpFactory httpFactory,
                          string emailId,
                          string authenticationToken,
                          string applicationName,
                          GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.HttpFactory = httpFactory;
      this.EmailId = emailId;
      this.AuthenticationToken = authenticationToken;
      this.GoogleEmailUploaderModel = googleEmailUploaderModel;
      string[] splits = emailId.Split('@');
      this.UserName = splits[0];
      this.DomainName = splits[1];
      this.MailBatch = new MailBatch(googleEmailUploaderModel);
      this.PauseEvent = new ManualResetEvent(true);
      this.batchMailUploadUrl =
          string.Format(
              MailUploader.MigrationURLTemplate,
              this.DomainName,
              this.UserName);
      this.ApplicationName = applicationName;
    }

    IHttpRequest CreateProperHttpRequest(string url) {
      IHttpRequest httpRequest = this.HttpFactory.CreatePostRequest(url);
      httpRequest.AddToHeader(
          MailUploader.AuthorizationHeaderTag,
          string.Format(MailUploader.GoogleAuthorizationTemplate,
                        this.AuthenticationToken));
      httpRequest.UserAgent = this.ApplicationName;
      httpRequest.ContentType = MailUploader.ContentType;
      return httpRequest;
    }

    internal void StartUpload() {
      this.UploadThread = new Thread(new ThreadStart(this.UploadMailsMethod));
      this.UploadThread.Start();
    }

    internal void PauseUpload() {
      this.PauseEvent.Reset();
    }

    internal void ResumeUpload() {
      this.PauseEvent.Set();
    }

    internal void AbortUpload() {
      if (this.UploadThread != null) {
        this.UploadThread.Abort();
      }
    }

    internal BatchUploadResult TestUpload(out double timeMilliseconds) {
      DateTime startTime = DateTime.Now;
      BatchUploadResult batchUploadResult;
      IHttpResponse httpResponse = null;
      try {
        IHttpRequest httpRequest =
            this.CreateProperHttpRequest(this.batchMailUploadUrl);
        this.MailBatch.CreateTestBatch();
        httpRequest.ContentLength = this.MailBatch.Length;
        try {
          using (Stream httpWebStreamRequestStream =
              httpRequest.GetRequestStream()) {
            this.MailBatch.CopyTo(httpWebStreamRequestStream);
          }
        } catch (IOException) {
          return BatchUploadResult.OtherException;
        }
        httpResponse = httpRequest.GetResponse();
        using (Stream respStream = httpResponse.GetResponseStream()) {
          batchUploadResult = this.MailBatch.ProcessResponse(respStream);
        }
      } catch (HttpException httpException) {
        switch (httpException.Status) {
          case HttpExceptionStatus.Unauthorized:
            batchUploadResult = BatchUploadResult.Unauthorized;
            break;
          case HttpExceptionStatus.Forbidden:
            batchUploadResult = BatchUploadResult.Forbidden;
            break;
          case HttpExceptionStatus.BadGateway:
            batchUploadResult = BatchUploadResult.BadGateway;
            break;
          case HttpExceptionStatus.ProtocolError:
          case HttpExceptionStatus.Timeout:
          case HttpExceptionStatus.Other:
          default:
            batchUploadResult = BatchUploadResult.OtherException;
            break;
        }
        if (httpException.Response != null) {
          httpException.Response.Close();
        }
        return batchUploadResult;
      } finally {
        if (httpResponse != null) {
          httpResponse.Close();
        }
        DateTime endTime = DateTime.Now;
        TimeSpan timeSpan = endTime - startTime;
        timeMilliseconds = timeSpan.TotalMilliseconds;
      }
      return batchUploadResult;
    }

    // Returns true if we need to retry the upload...
    bool TryUploadBatch(out BatchUploadResult batchUploadResult) {
      this.GoogleEmailUploaderModel.MailBatchUploadTryStart(this.MailBatch);
      IHttpResponse httpResponse = null;
      try {
        IHttpRequest httpRequest =
            this.CreateProperHttpRequest(this.batchMailUploadUrl);
        httpRequest.ContentLength = this.MailBatch.Length;
        try {
          using (Stream httpWebRequestStream = httpRequest.GetRequestStream()) {
            this.MailBatch.CopyTo(httpWebRequestStream);
          }
        } catch (IOException) {
          batchUploadResult = BatchUploadResult.OtherException;
          return true;
        }
        httpResponse = httpRequest.GetResponse();
        using (Stream respStream = httpResponse.GetResponseStream()) {
          batchUploadResult = this.MailBatch.ProcessResponse(respStream);
          if (batchUploadResult >= BatchUploadResult.BadRequest) {
            this.GoogleEmailUploaderModel.MailBatchUploaded(this.MailBatch,
                                                     batchUploadResult);
            return false;
          } else {
            // Not uploaded. Inform the provider and try again if needed.
            bool tryAgain = 
                this.GoogleEmailUploaderModel.MailBatchUploadFailure(
                    this.MailBatch,
                    batchUploadResult);
            return tryAgain;
          }
        }
      } catch (HttpException httpException) {
        switch (httpException.Status) {
          case HttpExceptionStatus.Unauthorized:
            batchUploadResult = BatchUploadResult.Unauthorized;
            break;
          case HttpExceptionStatus.Forbidden:
            batchUploadResult = BatchUploadResult.Forbidden;
            break;
          case HttpExceptionStatus.BadGateway:
            batchUploadResult = BatchUploadResult.BadGateway;
            break;
          case HttpExceptionStatus.ProtocolError:
          case HttpExceptionStatus.Timeout:
          case HttpExceptionStatus.Other:
          default:
            batchUploadResult = BatchUploadResult.OtherException;
            break;
        }
        this.GoogleEmailUploaderModel.HttpRequestFailure(httpException,
                                                  batchUploadResult);
        if (httpException.Response != null) {
          httpException.Response.Close();
        }
        // If it is not forbidden or unauthorized, need to try again
        return batchUploadResult != BatchUploadResult.Forbidden &&
            batchUploadResult != BatchUploadResult.Unauthorized;
      } finally {
        if (httpResponse != null) {
          httpResponse.Close();
        }
      }
    }

    // Returns false if mails ended up.
    bool GetNextMailBatch() {
      this.MailBatch.StartBatch();
      this.GoogleEmailUploaderModel.FillMailBatch(this.MailBatch);
      this.MailBatch.FinishBatch();
      return this.MailBatch.MailCount != 0;
    }

    // Main look that runs of the background thread...
    void UploadMailsMethod() {
      DoneReason doneReason = DoneReason.Completed;
      try {
        while (this.GetNextMailBatch()) {
#if DEBUG
          string respXml = this.MailBatch.GetBatchXML();
#endif
          // Keep trying till succeeds...
          while (true) {
            // Wait if we are in pause mode...
            this.PauseEvent.WaitOne();
            // Try to upload the mail batch
            BatchUploadResult batchUploadResult;
            bool retry = this.TryUploadBatch(out batchUploadResult);
            if (retry) {
              continue;
            }
            if (batchUploadResult == BatchUploadResult.Unauthorized) {
              doneReason = DoneReason.Unauthorized;
              goto skipUploads;
            } else if (batchUploadResult == BatchUploadResult.Forbidden) {
              doneReason = DoneReason.Forbidden;
              goto skipUploads;
            } else {
              break;
            }
          }
          TimeSpan timeSpan = DateTime.Now - this.MailBatch.StartDateTime;
          this.GoogleEmailUploaderModel.UpdateUploadSpeed(
              this.MailBatch.MailCount,
              timeSpan);
        }
      skipUploads:
        ;
      } catch (ThreadAbortException) {
        //  We catch this exception so that this does not shut down
        //  the application.
        doneReason = DoneReason.Aborted;
      } finally {
        this.UploadThread = null;
        this.GoogleEmailUploaderModel.UploadDone(doneReason);
      }
    }

    internal void WaitForUploadingThread() {
      if (this.UploadThread != null) {
        this.UploadThread.Join();
      }
    }
  }
}

