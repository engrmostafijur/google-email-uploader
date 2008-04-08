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
    const string EntryElementName = "entry";
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
    static readonly bool[] IsPrintableAscii = {
      false,   // 0x00
      false,   // 0x01
      false,   // 0x02
      false,   // 0x03
      false,   // 0x04
      false,   // 0x05
      false,   // 0x06
      false,   // 0x07
      false,   // 0x08
      true,    // 0x09
      true,    // 0x0A
      false,   // 0x0B
      false,   // 0x0C
      true,    // 0x0D
      false,   // 0x0E
      false,   // 0x0F

      false,   // 0x10
      false,   // 0x11
      false,   // 0x12
      false,   // 0x13
      false,   // 0x14
      false,   // 0x15
      false,   // 0x16
      false,   // 0x17
      false,   // 0x18
      false,   // 0x19
      false,   // 0x1A
      false,   // 0x1B
      false,   // 0x1C
      false,   // 0x1D
      false,   // 0x1E
      false,   // 0x1F

      true,   // 0x20
      true,   // 0x21
      true,   // 0x22
      true,   // 0x23
      true,   // 0x24
      true,   // 0x25
      true,   // 0x26
      true,   // 0x27
      true,   // 0x28
      true,   // 0x29
      true,   // 0x2A
      true,   // 0x2B
      true,   // 0x2C
      true,   // 0x2D
      true,   // 0x2E
      true,   // 0x2F

      true,   // 0x30
      true,   // 0x31
      true,   // 0x32
      true,   // 0x33
      true,   // 0x34
      true,   // 0x35
      true,   // 0x36
      true,   // 0x37
      true,   // 0x38
      true,   // 0x39
      true,   // 0x3A
      true,   // 0x3B
      true,   // 0x3C
      true,   // 0x3D
      true,   // 0x3E
      true,   // 0x3F

      true,   // 0x40
      true,   // 0x41
      true,   // 0x42
      true,   // 0x43
      true,   // 0x44
      true,   // 0x45
      true,   // 0x46
      true,   // 0x47
      true,   // 0x48
      true,   // 0x49
      true,   // 0x4A
      true,   // 0x4B
      true,   // 0x4C
      true,   // 0x4D
      true,   // 0x4E
      true,   // 0x4F

      true,   // 0x50
      true,   // 0x51
      true,   // 0x52
      true,   // 0x53
      true,   // 0x54
      true,   // 0x55
      true,   // 0x56
      true,   // 0x57
      true,   // 0x58
      true,   // 0x59
      true,   // 0x5A
      true,   // 0x5B
      true,   // 0x5C
      true,   // 0x5D
      true,   // 0x5E
      true,   // 0x5F

      true,   // 0x60
      true,   // 0x61
      true,   // 0x62
      true,   // 0x63
      true,   // 0x64
      true,   // 0x65
      true,   // 0x66
      true,   // 0x67
      true,   // 0x68
      true,   // 0x69
      true,   // 0x6A
      true,   // 0x6B
      true,   // 0x6C
      true,   // 0x6D
      true,   // 0x6E
      true,   // 0x6F

      true,   // 0x70
      true,   // 0x71
      true,   // 0x72
      true,   // 0x73
      true,   // 0x74
      true,   // 0x75
      true,   // 0x76
      true,   // 0x77
      true,   // 0x78
      true,   // 0x79
      true,   // 0x7A
      true,   // 0x7B
      true,   // 0x7C
      true,   // 0x7D
      true,   // 0x7E
      false,  // 0x7F
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
          GoogleEmailUploaderConfig.MaximumBatchSize);
      this.MemoryBufferArray = new char[MailBatch.DefaultCopyStepSize];
      this.MailBatchData = new ArrayList();
    }

    public uint MailCount {
      get {
        return this.mailCount;
      }
    }

    public uint FailedCount {
      get {
        return this.failedCount;
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
                                                MailBatch.FeedElementName,
                                                MailBatch.AtomNS);
      this.batchXmlTextWriter.WriteAttributeString("xmlns",
                                                   "app",
                                                   MailBatch.XmlNS,
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
      Debug.Assert(rfc822Buffer.Length > 0 &&
          rfc822Buffer.Length <= GoogleEmailUploaderConfig.MaximumBatchSize);
      bool canAdd = (
          // If its multimail batch let it be almost default mail batch size
          this.mailCount > 0 &&
          this.MemoryStream.Length + rfc822Buffer.Length + 2048
            <= GoogleEmailUploaderConfig.NormalBatchSize &&
          this.mailCount < GoogleEmailUploaderConfig.MaximumMailsPerBatch
        ) || (
          // If this mail is HUGE then its ok to be in singleton batch.
          this.mailCount == 0 &&
          rfc822Buffer.Length <=
              GoogleEmailUploaderConfig.MaximumBatchSize);

      if (!canAdd) {
        return false;
      }

      this.batchXmlTextWriter.WriteStartElement(MailBatch.EntryElementName,
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
        this.batchXmlTextWriter.WriteStartElement(MailBatch.IdElementName,
                                                  MailBatch.GDataBatchNS);
        this.batchXmlTextWriter.WriteString(this.mailCount.ToString());
        this.batchXmlTextWriter.WriteEndElement();
      }
      // Write out rfc822...
      {
        bool containsNonPrintASCII = false;
        // This test is rough. utf8 is multi byte, so having the illegal xml
        // byte need not mean its illegal xml. We could send every thing as
        // base64. This just helps us to optimize saving bytes on the wire.
        for (int i = 0; i < rfc822Buffer.Length; ++i) {
          byte b = rfc822Buffer[i];
          if (b >= 0x80 ||
              !MailBatch.IsPrintableAscii[b]) {
            containsNonPrintASCII = true;
            break;
          }
        }
        this.batchXmlTextWriter.WriteStartElement("rfc822Msg",
                                                  MailBatch.AppsNS);
        if (containsNonPrintASCII) {
          // If the rfc822 contains illegal xml chars then
          // we use base64 encoding.
          this.batchXmlTextWriter.WriteAttributeString("encoding",
                                                       "base64");
          this.batchXmlTextWriter.WriteBase64(rfc822Buffer,
                                              0,
                                              rfc822Buffer.Length);
        } else {
          // Otherwise we embed the rfc as is.
          string rfcString = Encoding.UTF8.GetString(rfc822Buffer);
          this.batchXmlTextWriter.WriteString(rfcString);
        }
        this.batchXmlTextWriter.WriteEndElement();
      }
      // Write out mail item properties except IS_TRASH. We will not move
      // anything to Trash folder as it automatically empties the Trash.
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
      }

      // Write out labels...
      {
        string[] labels = folderModel.Labels;
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
          GoogleEmailUploaderConfig.NormalBatchSize;
    }

    internal void FinishBatch() {
      // Close the feed
      this.batchXmlTextWriter.WriteEndElement();
      // close the document
      this.batchXmlTextWriter.WriteEndDocument();
      this.batchXmlTextWriter.Flush();
    }

    internal string GetBatchXML() {
      return Encoding.UTF8.GetString(this.MemoryStream.GetBuffer(),
                                     0,
                                     (int)this.MemoryStream.Length);
    }

    void GetEntryDetails(XmlElement entryElement,
                         out uint batchId,
                         out UploadResult itemUploadResult,
                         out string failureReason) {
      batchId = 0xFFFFFFFF;
      itemUploadResult = UploadResult.Unknown;
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
              itemUploadResult = UploadResult.Created;
              break;
            case MailBatch.BadRequestHttpCode:
              itemUploadResult = UploadResult.BadRequest;
              break;
            case MailBatch.InternalErrorHttpCode:
              itemUploadResult = UploadResult.InternalError;
              break;
            case MailBatch.ServiceUnavailableHttpCode:
              itemUploadResult = UploadResult.ServiceUnavailable;
              break;
          }
        }
      }
    }

    internal UploadResult ProcessResponse(Stream responseStream) {
      UploadResult batchUploadResult = UploadResult.Created;
      this.failedCount = this.mailCount;
      try {
        XmlDocument responseXmlDocument = new XmlDocument();
        responseXmlDocument.Load(responseStream);
        StringBuilder sb = new StringBuilder(1024);
        using (StringWriter stringWriter = new StringWriter(sb)) {
          XmlTextWriter responseXmlTextWriter =
              new XmlTextWriter(stringWriter);
          responseXmlTextWriter.Formatting = Formatting.Indented;
          responseXmlDocument.WriteTo(responseXmlTextWriter);
          responseXmlTextWriter.Close();
        }
        this.responseXml = sb.ToString();
        if (responseXmlDocument.LastChild == null ||
            responseXmlDocument.LastChild.NamespaceURI != MailBatch.AtomNS ||
            responseXmlDocument.LastChild.LocalName !=
                MailBatch.FeedElementName) {
          return UploadResult.Unknown;
        }
        XmlElement feedElement = responseXmlDocument.LastChild as XmlElement;
        if (feedElement == null) {
          return UploadResult.Unknown;
        }
        string[] failReasonArray = new string[this.mailCount];
        UploadResult[] uploadResultArray =
            new UploadResult[this.mailCount];
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
          UploadResult mailUploadResult;
          this.GetEntryDetails(entryElement,
                               out batchId,
                               out mailUploadResult,
                               out failueReason);
          if (batchId >= this.mailCount) {
            continue;
          }
          if (mailUploadResult == UploadResult.ServiceUnavailable) {
            return UploadResult.ServiceUnavailable;
          }
          failReasonArray[batchId] = failueReason;
          uploadResultArray[batchId] = mailUploadResult;
        }
        for (int i = 0; i < this.mailCount; ++i) {
          string failueReason = failReasonArray[i];
          UploadResult mailUploadResult = uploadResultArray[i];
          if (failueReason == null) {
            continue;
          }
          if (mailUploadResult != UploadResult.Created) {
            ((MailBatchDatum)this.MailBatchData[i]).SetFailure(
                failueReason);
          } else {
            this.failedCount--;
          }
          if (mailUploadResult < batchUploadResult) {
            batchUploadResult = mailUploadResult;
          }
        }
        return batchUploadResult;
      } catch (XmlException) {
        return UploadResult.Unknown;
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
  }

  /// <summary>
  /// </summary>
  public class ContactEntry {
    const string XmlNS = "http://www.w3.org/2000/xmlns/";
    const string AtomNS = "http://www.w3.org/2005/Atom";
    const string GDataNS = "http://schemas.google.com/g/2005";
    const string GDataKindURI = "http://schemas.google.com/g/2005#kind";
    const string ContactItemURI =
        "http://schemas.google.com/g/2005#contact";
    const string WorkRelURI =
        "http://schemas.google.com/g/2005#work";
    const string EntryElementName = "entry";
    const string TitleElementName = "title";
    const string ContentElementName = "content";
    const string EmailElementName = "email";
    const string IMElementName = "im";
    const string PhoneNumberElementName = "phoneNumber";
    const string PostalAddressElementName = "postalAddress";
    const string OrganizationElementName = "organization";
    const string OrgNameElementName = "orgName";
    const string OrgTitleElementName = "orgTitle";

    const string LabelAttrName = "label";
    const string RelationAttrName = "rel";
    const string AddressAttrName = "address";
    const string PrimaryAttrName = "primary";
    const string ProtocolAttrName = "protocol";

    readonly MemoryStream MemoryStream;
    readonly GoogleEmailUploaderModel googleEmailUploaderModel;

    IContact contact;
    StoreModel storeModel;
    bool uploaded;
    string failedReason;
    string updateUrl;

    string responseString;

    internal ContactEntry(GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.googleEmailUploaderModel = googleEmailUploaderModel;
      this.MemoryStream = new MemoryStream(1024 * 1024);
    }

    internal void SetContact(IContact contact, StoreModel storeModel) {
      this.contact = contact;
      this.storeModel = storeModel;
      this.uploaded = false;
      this.failedReason = null;
      this.updateUrl = null;
      this.ContactToXml(contact.Title,
                        contact.OrganizationName,
                        contact.OrganizationTitle,
                        contact.HomePageUrl,
                        contact.Notes,
                        contact.EmailAddresses,
                        contact.IMIdentities,
                        contact.PhoneNumbers,
                        contact.PostalAddresses);
    }

    public long Length {
      get {
        return this.MemoryStream.Length;
      }
    }

    public StoreModel StoreModel {
      get {
        return this.storeModel;
      }
    }

    public string ContactId {
      get {
        return this.contact.ContactId;
      }
    }

    public string ContactName {
      get {
        return this.contact.Title;
      }
    }

    public bool Uploaded {
      get {
        return this.uploaded;
      }
    }

    public string FailedReason {
      get {
        return this.failedReason;
      }
    }

    /// <summary>
    /// This not equal to null also means that we are resolving the conflict.
    /// </summary>
    public string UpdateUrl {
      get {
        return this.updateUrl;
      }
    }

    public void CopyTo(Stream stream) {
      this.MemoryStream.WriteTo(stream);
    }

    internal void ContactToXml(
        string title,
        string organizationName,
        string organizationTitle,
        string homePage,
        string notes,
        IEnumerable emailAddresses,
        IEnumerable imIdentities,
        IEnumerable phoneNumbers,
        IEnumerable postalAddresses) {
      this.MemoryStream.Position = 0;
      this.MemoryStream.SetLength(0);
      XmlTextWriter xmlTextWriter = new XmlTextWriter(this.MemoryStream,
                                                      Encoding.UTF8);
      xmlTextWriter.Formatting = Formatting.Indented;
      // Start the document
      xmlTextWriter.WriteStartDocument();

      // Start the entry...
      xmlTextWriter.WriteStartElement("a",
                                      ContactEntry.EntryElementName,
                                      ContactEntry.AtomNS);
      xmlTextWriter.WriteAttributeString("xmlns",
                                         "gd",
                                         ContactEntry.XmlNS,
                                         ContactEntry.GDataNS);
      {
        // Add the category element to the feed.
        xmlTextWriter.WriteStartElement("category",
                                        ContactEntry.AtomNS);
        xmlTextWriter.WriteAttributeString("scheme",
                                           ContactEntry.GDataKindURI);
        xmlTextWriter.WriteAttributeString(
            "term",
            ContactEntry.ContactItemURI);
        xmlTextWriter.WriteEndElement();
      }
      // Write out the title
      {
        xmlTextWriter.WriteStartElement(ContactEntry.TitleElementName,
                                        ContactEntry.AtomNS);
        xmlTextWriter.WriteString(title);
        xmlTextWriter.WriteEndElement();
      }
      // Write out the organization info
      if ((organizationName != null && organizationName.Length != 0) ||
          (organizationTitle != null && organizationTitle.Length != 0)) {
        xmlTextWriter.WriteStartElement(ContactEntry.OrganizationElementName,
                                        ContactEntry.GDataNS);
        xmlTextWriter.WriteAttributeString(
            ContactEntry.RelationAttrName,
            ContactEntry.WorkRelURI);
        if (organizationName != null && organizationName.Length != 0) {
          xmlTextWriter.WriteStartElement(ContactEntry.OrgNameElementName,
                                          ContactEntry.GDataNS);
          xmlTextWriter.WriteString(organizationName);
          xmlTextWriter.WriteEndElement();
        }
        if (organizationTitle != null && organizationTitle.Length != 0) {
          xmlTextWriter.WriteStartElement(ContactEntry.OrgTitleElementName,
                                          ContactEntry.GDataNS);
          xmlTextWriter.WriteString(organizationTitle);
          xmlTextWriter.WriteEndElement();
        }
        xmlTextWriter.WriteEndElement();
      }
      if (homePage != null && homePage.Length != 0) {
        string homePageInNotes = string.Format(Resources.HomePageTemplateText,
                                               homePage);
        homePageInNotes += "\r\n";
        if (notes != null && notes.Length != 0) {
          notes = homePageInNotes + "\r\n" + notes;
        } else {
          notes = homePageInNotes;
        }
      }
      if (this.googleEmailUploaderModel.
          IsEmailAddressCollision(emailAddresses)) {
        StringBuilder notesStringBuilder = new StringBuilder();
        foreach (EmailContact emailContact in emailAddresses) {
          string template =
              ContactEntry.GetNotesEmailTemplate(emailContact.Relation);
          if (emailContact.Relation == ContactRelation.Label) {
            notesStringBuilder.AppendFormat(template,
                                            emailContact.Label,
                                            emailContact.EmailAddress);
          } else {
            notesStringBuilder.AppendFormat(template,
                                            emailContact.EmailAddress);
          }
          notesStringBuilder.Append("\r\n");
        }
        if (notes != null) {
          notesStringBuilder.Append("\r\n");
          notesStringBuilder.Append(notes);
        }
        if (notesStringBuilder.Length > 0) {
          xmlTextWriter.WriteStartElement(ContactEntry.ContentElementName,
                                          ContactEntry.AtomNS);
          xmlTextWriter.WriteString(notesStringBuilder.ToString());
          xmlTextWriter.WriteEndElement();
        }
      } else {
        // Write out the notes
        if (notes != null && notes.Length != 0) {
          xmlTextWriter.WriteStartElement(ContactEntry.ContentElementName,
                                          ContactEntry.AtomNS);
          xmlTextWriter.WriteString(notes);
          xmlTextWriter.WriteEndElement();
        }
        // Write out email ids
        foreach (EmailContact emailContact in emailAddresses) {
          xmlTextWriter.WriteStartElement(ContactEntry.EmailElementName,
                                          ContactEntry.GDataNS);
          if (emailContact.Relation == ContactRelation.Label) {
            xmlTextWriter.WriteAttributeString(ContactEntry.LabelAttrName,
                                               emailContact.Label);
          } else {
            xmlTextWriter.WriteAttributeString(
                ContactEntry.RelationAttrName,
                ContactEntry.GetRelationURI(emailContact.Relation));
          }
          if (emailContact.IsPrimary) {
            xmlTextWriter.WriteAttributeString(ContactEntry.PrimaryAttrName,
                                               "true");
          }
          xmlTextWriter.WriteAttributeString(ContactEntry.AddressAttrName,
                                             emailContact.EmailAddress);
          xmlTextWriter.WriteEndElement();
        }
      }
      // Write out IM's
      foreach (IMContact imContact in imIdentities) {
        xmlTextWriter.WriteStartElement(ContactEntry.IMElementName,
                                        ContactEntry.GDataNS);
        if (imContact.Relation == ContactRelation.Label) {
          xmlTextWriter.WriteAttributeString(ContactEntry.LabelAttrName,
                                             imContact.Label);
        } else {
          xmlTextWriter.WriteAttributeString(
              ContactEntry.RelationAttrName,
              ContactEntry.GetRelationURI(imContact.Relation));
        }
        xmlTextWriter.WriteAttributeString(ContactEntry.AddressAttrName,
                                           imContact.IMAddress);
        xmlTextWriter.WriteAttributeString(ContactEntry.ProtocolAttrName,
                                           imContact.Protocol);
        xmlTextWriter.WriteEndElement();
      }
      // Write out Phone numbers
      foreach (PhoneContact phoneContact in phoneNumbers) {
        xmlTextWriter.WriteStartElement(ContactEntry.PhoneNumberElementName,
                                        ContactEntry.GDataNS);
        if (phoneContact.Relation == ContactRelation.Label) {
          xmlTextWriter.WriteAttributeString(ContactEntry.LabelAttrName,
                                             phoneContact.Label);
        } else {
          xmlTextWriter.WriteAttributeString(
              ContactEntry.RelationAttrName,
              ContactEntry.GetRelationURI(phoneContact.Relation));
        }
        xmlTextWriter.WriteString(phoneContact.PhoneNumber);
        xmlTextWriter.WriteEndElement();
      }
      // Write out postal addresses
      foreach (PostalContact postalContact in postalAddresses) {
        xmlTextWriter.WriteStartElement(ContactEntry.PostalAddressElementName,
                                        ContactEntry.GDataNS);
        if (postalContact.Relation == ContactRelation.Label) {
          xmlTextWriter.WriteAttributeString(ContactEntry.LabelAttrName,
                                             postalContact.Label);
        } else {
          xmlTextWriter.WriteAttributeString(
              ContactEntry.RelationAttrName,
              ContactEntry.GetRelationURI(postalContact.Relation));
        }
        xmlTextWriter.WriteString(postalContact.PostalAddress);
        xmlTextWriter.WriteEndElement();
      }

      // End Entry
      xmlTextWriter.WriteEndElement();
      // close the document
      xmlTextWriter.WriteEndDocument();
      xmlTextWriter.Flush();

      this.uploaded = true;
      this.failedReason = null;
    }

    static string GetNotesEmailTemplate(ContactRelation contactRelation) {
      switch (contactRelation) {
        case ContactRelation.Home:
          return Resources.HomeEmailTemplateText;
        case ContactRelation.Mobile:
          return Resources.OtherEmailTemplateText;
        case ContactRelation.Pager:
          return Resources.OtherEmailTemplateText;
        case ContactRelation.Work:
          return Resources.WorkEmailTemplateText;
        case ContactRelation.HomeFax:
          return Resources.OtherEmailTemplateText;
        case ContactRelation.WorkFax:
          return Resources.OtherEmailTemplateText;
        case ContactRelation.Other:
          return Resources.OtherEmailTemplateText;
        case ContactRelation.Label:
          return Resources.LabelEmailTemplateText;
        default:
          Debug.Fail("Should not be called for this");
          return string.Empty;
      }
    }

    static string GetRelationURI(ContactRelation contactRelation) {
      switch (contactRelation) {
        case ContactRelation.Home:
          return "http://schemas.google.com/g/2005#home";
        case ContactRelation.Mobile:
          return "http://schemas.google.com/g/2005#mobile";
        case ContactRelation.Pager:
          return "http://schemas.google.com/g/2005#pager";
        case ContactRelation.Work:
          return "http://schemas.google.com/g/2005#work";
        case ContactRelation.HomeFax:
          return "http://schemas.google.com/g/2005#home_fax";
        case ContactRelation.WorkFax:
          return "http://schemas.google.com/g/2005#work_fax";
        case ContactRelation.Other:
          return "http://schemas.google.com/g/2005#other";
        case ContactRelation.Label:
        default:
          Debug.Fail("Should not be called for this");
          return string.Empty;
      }
    }

    static ContactRelation GetRelationEnum(string relation) {
      switch (relation) {
        case "http://schemas.google.com/g/2005#home":
          return ContactRelation.Home;
        case "http://schemas.google.com/g/2005#mobile":
          return ContactRelation.Mobile;
        case "http://schemas.google.com/g/2005#pager":
          return ContactRelation.Pager;
        case "http://schemas.google.com/g/2005#work":
          return ContactRelation.Work;
        case "http://schemas.google.com/g/2005#home_fax":
          return ContactRelation.HomeFax;
        case "http://schemas.google.com/g/2005#work_fax":
          return ContactRelation.WorkFax;
        case "http://schemas.google.com/g/2005#other":
          return ContactRelation.Other;
        default:
          return ContactRelation.Label;
      }
    }

    internal string GetEntryXML() {
      return Encoding.UTF8.GetString(this.MemoryStream.GetBuffer(),
                                     0,
                                     (int)this.MemoryStream.Length);
    }

    internal UploadResult ProcessUploadResponse(Stream responseStream) {
      UploadResult batchUploadResult = UploadResult.Created;
      try {
        this.responseString = string.Empty;
        using (StreamReader textReader = new StreamReader(responseStream)) {
          this.responseString = textReader.ReadToEnd();
        }
        XmlDocument responseXmlDocument = new XmlDocument();
        responseXmlDocument.LoadXml(this.responseString);

        // Nothing to process for the contacts api as it throws exception when
        // it fails.

        StringBuilder sb = new StringBuilder(1024);
        using (StringWriter stringWriter = new StringWriter(sb)) {
          XmlTextWriter responseXmlTextWriter =
              new XmlTextWriter(stringWriter);
          responseXmlTextWriter.Formatting = Formatting.Indented;
          responseXmlDocument.WriteTo(responseXmlTextWriter);
          responseXmlTextWriter.Close();
        }
        this.responseString = sb.ToString();
        return batchUploadResult;
      } catch (IOException) {
        return UploadResult.Unknown;
      } catch (XmlException) {
        return UploadResult.Unknown;
      }
    }

    void ExtractContactInformation(
        XmlDocument xmlDocument,
        out string updateUrl,
        out string title,
        out string organizationName,
        out string organizationTitle,
        out string notes,
        ArrayList emailAddresses,
        ArrayList imIdentities,
        ArrayList phoneNumbers,
        ArrayList postalAddresses) {
      if (xmlDocument.LastChild == null ||
          xmlDocument.LastChild.NamespaceURI != ContactEntry.AtomNS ||
          xmlDocument.LastChild.LocalName !=
              ContactEntry.EntryElementName) {
        throw new XmlException("Conflict return entry has errors");
      }
      updateUrl = null;
      title = string.Empty;
      organizationName = null;
      organizationTitle = null;
      notes = null;
      foreach (XmlNode childNode in xmlDocument.LastChild.ChildNodes) {
        XmlElement xmlElement = childNode as XmlElement;
        if (xmlElement == null) {
          continue;
        }
        if (xmlElement.NamespaceURI == ContactEntry.AtomNS) {
          if (xmlElement.LocalName == ContactEntry.TitleElementName) {
            title = xmlElement.InnerText;
          } else if (xmlElement.LocalName == ContactEntry.ContentElementName) {
            notes = xmlElement.InnerText;
          } else if (xmlElement.LocalName == "link") {
            string relation =
                xmlElement.GetAttribute(ContactEntry.RelationAttrName);
            string hRef =
                xmlElement.GetAttribute("href");
            if (relation == "edit") {
              this.updateUrl = hRef;
            }
          }
        } else if (childNode.NamespaceURI == ContactEntry.GDataNS) {
          string relationString =
              xmlElement.GetAttribute(ContactEntry.RelationAttrName);
          string labelString =
              xmlElement.GetAttribute(ContactEntry.LabelAttrName);
          ContactRelation relation =
              ContactEntry.GetRelationEnum(relationString);
          if (relation == ContactRelation.Label &&
              labelString == string.Empty) {
            continue;
          }
          if (xmlElement.LocalName == ContactEntry.OrganizationElementName) {
            foreach (XmlNode grandChildNode in xmlElement.ChildNodes) {
              XmlElement grandChildXmlElement = grandChildNode as XmlElement;
              if (grandChildXmlElement == null) {
                continue;
              }
              if (grandChildXmlElement.NamespaceURI != ContactEntry.GDataNS) {
                continue;
              }
              if (grandChildXmlElement.LocalName
                  == ContactEntry.OrgNameElementName) {
                organizationName = grandChildXmlElement.InnerText;
              } else if (grandChildXmlElement.LocalName
                  == ContactEntry.OrgTitleElementName) {
                organizationTitle = grandChildXmlElement.InnerText;
              }
            }
          } else if (xmlElement.LocalName == ContactEntry.EmailElementName) {
            string address =
                xmlElement.GetAttribute(ContactEntry.AddressAttrName);
            if (address == string.Empty) {
              continue;
            }
            string isPrimary =
                xmlElement.GetAttribute(ContactEntry.PrimaryAttrName);
            emailAddresses.Add(
                new EmailContact(address,
                    labelString,
                    relation,
                    isPrimary == "true"));
          } else if (xmlElement.LocalName == ContactEntry.IMElementName) {
            string address =
                xmlElement.GetAttribute(ContactEntry.AddressAttrName);
            string protocol =
                xmlElement.GetAttribute(ContactEntry.ProtocolAttrName);
            if (address == string.Empty || protocol == string.Empty) {
              continue;
            }
            imIdentities.Add(
                new IMContact(address, protocol, labelString, relation));
          } else if (xmlElement.LocalName ==
              ContactEntry.PhoneNumberElementName) {
            string phoneNumber = xmlElement.InnerText;
            if (phoneNumber == string.Empty) {
              continue;
            }
            phoneNumbers.Add(
                new PhoneContact(phoneNumber, labelString, relation));
          } else if (xmlElement.LocalName ==
              ContactEntry.PostalAddressElementName) {
            string postalAddress = xmlElement.InnerText;
            if (postalAddress == string.Empty) {
              continue;
            }
            postalAddresses.Add(
                new PostalContact(postalAddress, labelString, relation));
          }
        }
      }
    }

    static bool IsEmailAddressInList(
        EmailContact emailContact, ArrayList emailAddresses) {
      foreach (EmailContact emailContactIter in emailAddresses) {
        if (emailContact.EmailAddress == emailContactIter.EmailAddress) {
          return true;
        }
      }
      return false;
    }

    static bool IsIMIdentityInList(
        IMContact imContact, ArrayList imIdentities) {
      foreach (IMContact imContactIter in imIdentities) {
        if (imContact.IMAddress == imContactIter.IMAddress) {
          return true;
        }
      }
      return false;
    }

    static bool IsPhoneNumberInList(
        PhoneContact phoneContact, ArrayList phoneNumbers) {
      foreach (PhoneContact phoneContactIter in phoneNumbers) {
        if (phoneContact.PhoneNumber == phoneContactIter.PhoneNumber) {
          return true;
        }
      }
      return false;
    }

    static bool IsPostalAddressInList(
        PostalContact postalContact, ArrayList postalAddresses) {
      foreach (PostalContact postalContactIter in postalAddresses) {
        if (postalContact.PostalAddress == postalContactIter.PostalAddress) {
          return true;
        }
      }
      return false;
    }

    void MergeContactInformation(
        ref string title,
        ref string organizationName,
        ref string organizationTitle,
        ref string homePage,
        ref string notes,
        ArrayList emailAddresses,
        ArrayList imIdentities,
        ArrayList phoneNumbers,
        ArrayList postalAddresses) {
      // The input is the information in Google.
      // Need to merge the contact info with it.
      // We give preference to the information thats already in
      // the google cloud
      if (title == null || title.Length == 0) {
        // Give preference to the title in google
        title = this.contact.Title;
      }
      if (organizationName == null || organizationName.Length == 0) {
        organizationName = this.contact.OrganizationName;
      }
      if (organizationTitle == null || organizationTitle.Length == 0) {
        organizationTitle = this.contact.OrganizationTitle;
      }
      homePage = this.contact.HomePageUrl;
      if (this.contact.Notes != null) {
        if (notes != this.contact.Notes) {
          // If the notes we have in google is not same as notes in contact
          // then concat the contact notes to the google notes.
          StringBuilder sb = new StringBuilder();
          sb.Append(notes);
          sb.Append("\r\n\r\n");
          sb.Append(this.contact.Notes);
          notes = sb.ToString();
        }
      }
      foreach (EmailContact emailContact in this.contact.EmailAddresses) {
        if (!ContactEntry.IsEmailAddressInList(emailContact, emailAddresses)) {
          emailAddresses.Add(emailContact);
        }
      }
      foreach (IMContact imContact in this.contact.IMIdentities) {
        if (!ContactEntry.IsIMIdentityInList(imContact, imIdentities)) {
          imIdentities.Add(imContact);
        }
      }
      foreach (PhoneContact phoneContact in this.contact.PhoneNumbers) {
        if (!ContactEntry.IsPhoneNumberInList(phoneContact, phoneNumbers)) {
          phoneNumbers.Add(phoneContact);
        }
      }
      foreach (PostalContact postalContact in this.contact.PostalAddresses) {
        if (!ContactEntry.IsPostalAddressInList(postalContact, postalAddresses)) {
          postalAddresses.Add(postalContact);
        }
      }
    }

    internal bool ResolveConflict(string googleContactXml) {
      try {
        GoogleEmailUploaderTrace.EnteringMethod("ContactEntry.ResolveConflict");
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(googleContactXml);
        string title;
        string organizationName;
        string organizationTitle;
        string homePage = null;
        string notes;
        ArrayList emailAddresses = new ArrayList();
        ArrayList imIdentities = new ArrayList();
        ArrayList phoneNumbers = new ArrayList();
        ArrayList postalAddresses = new ArrayList();
        this.ExtractContactInformation(
          xmlDocument,
          out this.updateUrl,
          out title,
          out organizationName,
          out organizationTitle,
          out notes,
          emailAddresses,
          imIdentities,
          phoneNumbers,
          postalAddresses);
        if (this.updateUrl == null) {
          return false;
        }
        this.MergeContactInformation(
          ref title,
          ref organizationName,
          ref organizationTitle,
          ref homePage,
          ref notes,
          emailAddresses,
          imIdentities,
          phoneNumbers,
          postalAddresses);
        this.ContactToXml(
          title,
          organizationName,
          organizationTitle,
          homePage,
          notes,
          emailAddresses,
          imIdentities,
          phoneNumbers,
          postalAddresses);
        return true;
      } catch (Exception ex) {
        GoogleEmailUploaderTrace.WriteLine(ex.ToString());
        // If we get an exception then we say we cant resolve.
        return false;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod("ContactEntry.ResolveConflict");
      }
    }

    internal string ResponseString {
      get {
        return this.responseString;
      }
    }
  }

  /// <summary>
  /// Reason why the upload is considered done
  /// </summary>
  public enum DoneReason {
    /// <summary>
    /// The upload was stopped
    /// </summary>
    Stopped,

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
  enum UploadResult {
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
    /// The contact was already existing resulting in conflict.
    /// Action: Resolve the conflict and try upload again.
    /// </summary>
    Conflict,

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
    const string ContactMigrationURLTemplate =
        "http://www.google.com/m8/feeds/contacts/{0}/full";
    const string AuthorizationHeaderTag = "Authorization";
    const string GoogleAuthorizationTemplate = "GoogleLogin auth={0}";
    const string ContentType = "application/atom+xml; charset=UTF-8";

    readonly IHttpFactory HttpFactory;
    readonly string EmailId;
    readonly string Password;
    readonly string UserName;
    readonly string DomainName;
    readonly string MailAuthenticationToken;
    readonly string ContactAuthenticationToken;
    // https://apps-api.google.com/a/feeds/migration/2.0/domain.com/user/mail/batch
    readonly string batchMailUploadUrl;
    readonly string batchContactUploadUrl;
    readonly GoogleEmailUploaderModel GoogleEmailUploaderModel;
    readonly MailBatch MailBatch;
    readonly ContactEntry ContactEntry;
    internal readonly ManualResetEvent PauseEvent;
    readonly string ApplicationName;

    Thread UploadThread;

    internal MailUploader(IHttpFactory httpFactory,
                          string emailId,
                          string password,
                          string applicationName,
                          GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.HttpFactory = httpFactory;
      this.EmailId = emailId;
      this.Password = password;
      GoogleAuthenticator authenticator =
          new GoogleAuthenticator(httpFactory,
                                  AccountType.GoogleOrHosted,
                                  applicationName);
      AuthenticationResponse resp =
          authenticator.AuthenticateForService(this.EmailId,
                                               this.Password,
                                               "apps");
      this.MailAuthenticationToken = resp.AuthToken;
      resp = authenticator.AuthenticateForService(this.EmailId,
                                                  this.Password,
                                                  "cp");
      this.ContactAuthenticationToken = resp.AuthToken;
      this.GoogleEmailUploaderModel = googleEmailUploaderModel;
      string[] splits = emailId.Split('@');
      this.UserName = splits[0];
      this.DomainName = splits[1];
      this.MailBatch = new MailBatch(googleEmailUploaderModel);
      this.ContactEntry = new ContactEntry(googleEmailUploaderModel);
      this.PauseEvent = new ManualResetEvent(true);
      this.batchMailUploadUrl =
          string.Format(
              GoogleEmailUploaderConfig.EmailMigrationUrl,
              this.DomainName,
              this.UserName);
      this.batchContactUploadUrl =
          string.Format(
              MailUploader.ContactMigrationURLTemplate,
              this.EmailId);
      this.ApplicationName = applicationName;
    }

    IHttpRequest CreateProperHttpPostRequest(string url,
                                             string authToken) {
      IHttpRequest httpRequest = this.HttpFactory.CreatePostRequest(url);
      httpRequest.AddToHeader(
          MailUploader.AuthorizationHeaderTag,
          string.Format(MailUploader.GoogleAuthorizationTemplate,
                        authToken));
      httpRequest.UserAgent = this.ApplicationName;
      httpRequest.ContentType = MailUploader.ContentType;
      return httpRequest;
    }

    IHttpRequest CreateProperHttpPutRequest(string url,
                                            string authToken) {
      IHttpRequest httpRequest = this.HttpFactory.CreatePutRequest(url);
      httpRequest.AddToHeader(
          MailUploader.AuthorizationHeaderTag,
          string.Format(MailUploader.GoogleAuthorizationTemplate,
                        authToken));
      httpRequest.UserAgent = this.ApplicationName;
      httpRequest.ContentType = MailUploader.ContentType;
      return httpRequest;
    }

    internal void StartUpload() {
      this.UploadThread = new Thread(new ThreadStart(this.UploadMethod));
      this.UploadThread.Start();
    }

    internal void PauseUpload() {
      this.PauseEvent.Reset();
    }

    internal void ResumeUpload() {
      this.PauseEvent.Set();
    }

    internal void StopUpload() {
      if (this.UploadThread != null) {
        this.UploadThread.Abort();
      }
    }

    internal UploadResult TestEmailUpload(out double timeMilliseconds) {
      DateTime startTime = DateTime.Now;
      UploadResult batchUploadResult;
      IHttpResponse httpResponse = null;
      try {
        IHttpRequest httpRequest =
            this.CreateProperHttpPostRequest(this.batchMailUploadUrl,
                                         this.MailAuthenticationToken);
        this.MailBatch.CreateTestBatch();
        httpRequest.ContentLength = this.MailBatch.Length;
        try {
          using (Stream httpWebStreamRequestStream =
              httpRequest.GetRequestStream()) {
            this.MailBatch.CopyTo(httpWebStreamRequestStream);
          }
        } catch (IOException) {
          return UploadResult.OtherException;
        }
        httpResponse = httpRequest.GetResponse();
        using (Stream respStream = httpResponse.GetResponseStream()) {
          batchUploadResult = this.MailBatch.ProcessResponse(respStream);
        }
      } catch (HttpException httpException) {
        switch (httpException.Status) {
          case HttpExceptionStatus.Unauthorized:
            batchUploadResult = UploadResult.Unauthorized;
            break;
          case HttpExceptionStatus.Forbidden:
            batchUploadResult = UploadResult.Forbidden;
            break;
          case HttpExceptionStatus.BadGateway:
            batchUploadResult = UploadResult.BadGateway;
            break;
          case HttpExceptionStatus.Conflict:
          case HttpExceptionStatus.ProtocolError:
          case HttpExceptionStatus.Timeout:
          case HttpExceptionStatus.Other:
          default:
            batchUploadResult = UploadResult.OtherException;
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
    bool TryUploadContactEntry(out UploadResult uploadResult) {
      this.GoogleEmailUploaderModel.ContactEntryUploadTryStart(
          this.ContactEntry);
      IHttpResponse httpResponse = null;
      try {
        IHttpRequest httpRequest =
            this.CreateProperHttpPostRequest(this.batchContactUploadUrl,
                                             this.ContactAuthenticationToken);
        httpRequest.ContentLength = this.ContactEntry.Length;
        try {
          using (Stream httpWebRequestStream = httpRequest.GetRequestStream()) {
            this.ContactEntry.CopyTo(httpWebRequestStream);
          }
        } catch (IOException) {
          uploadResult = UploadResult.OtherException;
          return true;
        }
        httpResponse = httpRequest.GetResponse();
        using (Stream respStream = httpResponse.GetResponseStream()) {
          uploadResult = this.ContactEntry.ProcessUploadResponse(respStream);
          this.GoogleEmailUploaderModel.ContactEntryUploaded(
              this.ContactEntry,
              uploadResult);
          return false;
        }
      } catch (HttpException httpException) {
        switch (httpException.Status) {
          case HttpExceptionStatus.BadRequest:
            uploadResult = UploadResult.BadRequest;
            using (Stream respStream =
                httpException.Response.GetResponseStream()) {
              this.ContactEntry.ProcessUploadResponse(respStream);
              this.GoogleEmailUploaderModel.ContactEntryUploaded(
                  this.ContactEntry,
                  uploadResult);
            }
            return false;
          case HttpExceptionStatus.Conflict:
            uploadResult = UploadResult.Conflict;
            string response = httpException.GetResponseString();
              // Try to resolve the contact
            if (!this.ContactEntry.ResolveConflict(response)) {
                // Some problem in resolving the conflict.
                bool tryAgain =
                      this.GoogleEmailUploaderModel.ContactEntryUploadFailure(
                        this.ContactEntry,
                        uploadResult);
                return tryAgain;
              } else {
                // Conflict was resolved so we say don't try upload again.
                // Instead we should not try to update the already existing
                // entry
                return false;
              }
          case HttpExceptionStatus.Unauthorized:
            uploadResult = UploadResult.Unauthorized;
            break;
          case HttpExceptionStatus.Forbidden:
            uploadResult = UploadResult.Forbidden;
            break;
          case HttpExceptionStatus.BadGateway:
            uploadResult = UploadResult.BadGateway;
            break;
          case HttpExceptionStatus.ProtocolError:
          case HttpExceptionStatus.Timeout:
          case HttpExceptionStatus.Other:
          default:
            uploadResult = UploadResult.OtherException;
            break;
        }
        this.GoogleEmailUploaderModel.HttpRequestFailure(
            httpException,
            httpException.GetResponseString(),
            uploadResult);
        if (httpException.Response != null) {
          httpException.Response.Close();
        }
        // If it is not forbidden or unauthorized or conflict,
        // need to try again
        return uploadResult != UploadResult.Forbidden &&
            uploadResult != UploadResult.Unauthorized;
      } finally {
        if (httpResponse != null) {
          httpResponse.Close();
        }
      }
    }

    bool TryUpdateContactEntry(out UploadResult uploadResult) {
      this.GoogleEmailUploaderModel.ContactEntryUploadTryStart(
          this.ContactEntry);
      IHttpResponse httpResponse = null;
      try {
        IHttpRequest httpRequest =
            this.CreateProperHttpPutRequest(this.ContactEntry.UpdateUrl,
                                            this.ContactAuthenticationToken);
        httpRequest.ContentLength = this.ContactEntry.Length;
        try {
          using (Stream httpWebRequestStream = httpRequest.GetRequestStream()) {
            this.ContactEntry.CopyTo(httpWebRequestStream);
          }
        } catch (IOException) {
          uploadResult = UploadResult.OtherException;
          return true;
        }
        httpResponse = httpRequest.GetResponse();
        using (Stream respStream = httpResponse.GetResponseStream()) {
          uploadResult = this.ContactEntry.ProcessUploadResponse(respStream);
          this.GoogleEmailUploaderModel.ContactEntryUploaded(
              this.ContactEntry,
              uploadResult);
          return false;
        }
      } catch (HttpException httpException) {
        switch (httpException.Status) {
          case HttpExceptionStatus.BadRequest:
            uploadResult = UploadResult.BadRequest;
            using (Stream respStream =
                httpException.Response.GetResponseStream()) {
              uploadResult = this.ContactEntry.ProcessUploadResponse(respStream);
              this.GoogleEmailUploaderModel.ContactEntryUploaded(
                  this.ContactEntry,
                  uploadResult);
            }
            return false;
          case HttpExceptionStatus.Conflict:
            uploadResult = UploadResult.Conflict;
            return false;
          case HttpExceptionStatus.Unauthorized:
            uploadResult = UploadResult.Unauthorized;
            break;
          case HttpExceptionStatus.Forbidden:
            uploadResult = UploadResult.Forbidden;
            break;
          case HttpExceptionStatus.BadGateway:
            uploadResult = UploadResult.BadGateway;
            break;
          case HttpExceptionStatus.ProtocolError:
          case HttpExceptionStatus.Timeout:
          case HttpExceptionStatus.Other:
          default:
            uploadResult = UploadResult.OtherException;
            break;
        }
        this.GoogleEmailUploaderModel.HttpRequestFailure(
            httpException,
            httpException.GetResponseString(),
            uploadResult);
        if (httpException.Response != null) {
          httpException.Response.Close();
        }
        // If it is not forbidden or unauthorized or conflict,
        // need to try again
        return uploadResult != UploadResult.Forbidden &&
            uploadResult != UploadResult.Unauthorized;
      } finally {
        if (httpResponse != null) {
          httpResponse.Close();
        }
      }
    }

    // Returns true if we need to retry the upload...
    bool TryUploadEmailBatch(out UploadResult batchUploadResult) {
      IHttpResponse httpResponse = null;
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "MailUploader.TryUploadBatch");
        this.GoogleEmailUploaderModel.MailBatchUploadTryStart(this.MailBatch);
        IHttpRequest httpRequest =
            this.CreateProperHttpPostRequest(this.batchMailUploadUrl,
                                         this.MailAuthenticationToken);
        httpRequest.ContentLength = this.MailBatch.Length;
        try {
          using (Stream httpWebRequestStream = httpRequest.GetRequestStream()) {
            this.MailBatch.CopyTo(httpWebRequestStream);
          }
        } catch (IOException) {
          batchUploadResult = UploadResult.OtherException;
          return true;
        }
        httpResponse = httpRequest.GetResponse();
        using (Stream respStream = httpResponse.GetResponseStream()) {
          batchUploadResult = this.MailBatch.ProcessResponse(respStream);
          if (batchUploadResult >= UploadResult.BadRequest) {
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
            batchUploadResult = UploadResult.Unauthorized;
            break;
          case HttpExceptionStatus.Forbidden:
            batchUploadResult = UploadResult.Forbidden;
            break;
          case HttpExceptionStatus.BadGateway:
            batchUploadResult = UploadResult.BadGateway;
            break;
          case HttpExceptionStatus.Conflict:
          case HttpExceptionStatus.ProtocolError:
          case HttpExceptionStatus.Timeout:
          case HttpExceptionStatus.Other:
          default:
            batchUploadResult = UploadResult.OtherException;
            break;
        }
        this.GoogleEmailUploaderModel.HttpRequestFailure(
            httpException,
            httpException.GetResponseString(),
            batchUploadResult);
        if (httpException.Response != null) {
          httpException.Response.Close();
        }
        // If it is not forbidden or unauthorized, need to try again
        return batchUploadResult != UploadResult.Forbidden &&
            batchUploadResult != UploadResult.Unauthorized;
      } finally {
        if (httpResponse != null) {
          httpResponse.Close();
        }
        GoogleEmailUploaderTrace.ExitingMethod(
            "MailUploader.TryUploadBatch");
      }
    }

    // Returns false if mails ended up.
    bool GetNextEmailBatch() {
      this.MailBatch.StartBatch();
      this.GoogleEmailUploaderModel.FillMailBatch(this.MailBatch);
      this.MailBatch.FinishBatch();
      return this.MailBatch.MailCount != 0;
    }

    // Main look that runs of the background thread...
    void UploadMethod() {
      DoneReason doneReason = DoneReason.Stopped;
      try {
        while (true) {
          StoreModel storeModel;
          IContact contact =
              this.GoogleEmailUploaderModel.GetNextContactEntry(out storeModel);
          if (contact == null) {
            // contact == null => we are done with all contacts.
            break;
          }
          this.ContactEntry.SetContact(contact, storeModel);
          // Keep trying till succeeds...
          while (true) {
#if DEBUG
            string reqXml = this.ContactEntry.GetEntryXML();
#endif
            // Wait if we are in pause mode...
            this.PauseEvent.WaitOne();
            UploadResult uploadResult;
            if (this.ContactEntry.UpdateUrl == null) {
              // Try to upload the mail batch
              bool retry = this.TryUploadContactEntry(out uploadResult);
              if (retry) {
                continue;
              }
              if (uploadResult == UploadResult.Conflict) {
                continue;
              }
            } else {
              // Retrying after the conflict was resolved.
              bool retry =
                this.TryUpdateContactEntry(out uploadResult);
              if (retry) {
                continue;
              }
            }
            if (uploadResult == UploadResult.Unauthorized) {
              doneReason = DoneReason.Unauthorized;
              goto skipContactsUpload;
            } else if (uploadResult == UploadResult.Forbidden) {
              doneReason = DoneReason.Forbidden;
              goto skipContactsUpload;
            } else {
              break;
            }
          }
        }
      skipContactsUpload:
        while (this.GetNextEmailBatch()) {
#if DEBUG
          string reqXml = this.MailBatch.GetBatchXML();
#endif
          // Keep trying till succeeds...
          while (true) {
            // Wait if we are in pause mode...
            this.PauseEvent.WaitOne();
            // Try to upload the mail batch
            UploadResult batchUploadResult;
            DateTime start = DateTime.Now;
            bool retry = this.TryUploadEmailBatch(out batchUploadResult);
            if (retry) {
              continue;
            }
            // we are doign throtlling here
            // 0.0009 is q per milli secs
            TimeSpan timeTaken = DateTime.Now - start;
            int milliSecsToSleep =
                (int)(this.MailBatch.MailCount / 0.0009
                    - timeTaken.Milliseconds);
            if (milliSecsToSleep > 0) {
              Thread.Sleep(milliSecsToSleep);
            }
            if (batchUploadResult == UploadResult.Unauthorized) {
              doneReason = DoneReason.Unauthorized;
              goto skipEmailUploads;
            } else if (batchUploadResult == UploadResult.Forbidden) {
              doneReason = DoneReason.Forbidden;
              goto skipEmailUploads;
            } else {
              break;
            }
          }
          TimeSpan timeSpan = DateTime.Now - this.MailBatch.StartDateTime;
          this.GoogleEmailUploaderModel.UpdateUploadSpeed(
              this.MailBatch.MailCount,
              timeSpan);
        }
        doneReason = DoneReason.Completed;
      skipEmailUploads:
        ;
      } catch (ThreadAbortException) {
        //  We catch this exception so that this does not shut down
        //  the application.
      } catch (Exception excep) {
        GoogleEmailUploaderTrace.WriteLine(excep.ToString());
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

