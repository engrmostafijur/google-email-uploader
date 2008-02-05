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

using GoogleEmailUploader;

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Xml;

namespace GoogleEmailUploaderTestScript {

  class TestMemoryStream : MemoryStream {
    protected override void Dispose(bool disposing) {
    }

    public override void Close() {
    }
  }

  enum RequestMethod {
    Get,
    Post
  }

  enum DMAPIState {
    Success,
    Timeout,
    BadRequest,
    Unauthorized,
    Forbidden,
    InternalError,
    BadGateway,
    ServiceUnavailable,
    IncorrectResponse,
  }

  enum GAIAState {
    Authenticate,
    Timeout,
    BadAuthentication,
    NotVerified,
    TermsNotAgreed,
    CaptchaRequired,
    Unknown,
    AccountDeleted,
    AccountDisabled,
    ServiceDisabled,
    ServiceUnavailable,
    IncorrectResponse,
  }

  class HttpFactory : IHttpFactory {
    const string DMAPIUrl =
        "https://apps-apis.google.com/a/feeds/migration/2.0";
    const string AccountLoginUrl =
        "https://www.google.com/accounts/ClientLogin";
    const string CaptchaUrl =
        "http://www.google.com/accounts/Captcha";
    const string AuthenticateResponseTemplate =
@"SID=SIDSIDSIDSIDSIDSIDSID
LSID=LSIDSIDLSIDLSIDLSIDLSIDLSID
Auth={0}";
    const string CaptchaResponseTemplate =
@"Url=http://www.google.com/login/captcha
Error=CaptchaRequired
CaptchaToken={0}
CaptchaUrl=Captcha?ctoken={0}";
    const string CaptchaUrlTemplate =
        "http://www.google.com/accounts/Captcha?ctoken={0}";

    internal string AuthToken;
    internal string CaptchaToken;
    internal DMAPIState DMAPIState;
    internal GAIAState GAIAState;
    TextWriter textWriter;

    internal HttpFactory() {
      this.DMAPIState = DMAPIState.Success;
      this.GAIAState = GAIAState.Authenticate;
      this.AuthToken = "AuthAuthAuth";
      this.CaptchaToken = "CaptchaCaptchaCaptcha";
    }

    string GetResponseXml(string responseCode,
                          TestMemoryStream requestStream) {
      XmlDocument requestXml = new XmlDocument();
      requestXml.Load(requestStream);
      XmlElement feedElement = requestXml.LastChild as XmlElement;
      XmlDocument responseXml = new XmlDocument();
      XmlElement responseFeedElement =
          responseXml.CreateElement("a",
                                    "feed",
                                    "http://www.w3.org/2005/Atom");
      responseXml.AppendChild(feedElement);
      foreach (XmlNode childNode in feedElement.ChildNodes) {
        if (childNode.LocalName != "entry") {
          continue;
        }
        XmlElement entryElement = childNode as XmlElement;
        if (entryElement == null) {
          continue;
        }
        uint batchId = 0xFFFFFFFF;
        foreach (XmlNode childNode1 in entryElement.ChildNodes) {
          if (childNode.NamespaceURI !=
              "http://schemas.google.com/gdata/batch") {
            continue;
          }
          XmlElement xmlElement = childNode1 as XmlElement;
          if (xmlElement == null) {
            continue;
          }
          if (xmlElement.LocalName == "id") {
            try {
              batchId = uint.Parse(xmlElement.InnerText);
            } catch {
              // Ignore the error while parsing batchId
            }
          }
        }
        entryElement =
            responseXml.CreateElement("a",
                                      "entry",
                                      "http://www.w3.org/2005/Atom");
        XmlElement batchElement =
            responseXml.CreateElement("batch",
                                      "id",
                                      "http://schemas.google.com/gdata/batch");
        batchElement.Value = batchId.ToString();
        XmlElement statusElement =
            responseXml.CreateElement("batch",
                                      "status",
                                      "http://schemas.google.com/gdata/batch");
        XmlAttribute codeAttribute = responseXml.CreateAttribute("code");
        codeAttribute.Value = responseCode;
        statusElement.AppendChild(codeAttribute);
        entryElement.AppendChild(batchElement);
        entryElement.AppendChild(statusElement);
        responseFeedElement.AppendChild(entryElement);
      }
      StringBuilder sb = new StringBuilder(1024);
      using (StringWriter stringWriter = new StringWriter(sb)) {
        XmlTextWriter responseXmlTextWriter =
            new XmlTextWriter(stringWriter);
        responseXmlTextWriter.Formatting = Formatting.Indented;
        responseXml.WriteTo(responseXmlTextWriter);
        responseXmlTextWriter.Close();
      }
      return sb.ToString();
    }

    HttpResponse GetDMAPIResponse(HttpRequest httpRequest) {
      string responseString;
      switch (this.DMAPIState) {
        case DMAPIState.Success:
          responseString = this.GetResponseXml("201",
                                               httpRequest.TestMemoryStream);
          break;
        case DMAPIState.Timeout:
          throw new HttpException("Time out",
                                  HttpExceptionStatus.Timeout,
                                  null);
        case DMAPIState.BadRequest:
          responseString = this.GetResponseXml("400",
                                               httpRequest.TestMemoryStream);
          break;
        case DMAPIState.Unauthorized:
          throw new HttpException("Unauthorized",
                                  HttpExceptionStatus.Unauthorized,
                                  null);
        case DMAPIState.Forbidden:
          throw new HttpException("Forbidden",
                                  HttpExceptionStatus.Forbidden,
                                  null);
        case DMAPIState.InternalError:
          responseString = this.GetResponseXml("500",
                                               httpRequest.TestMemoryStream);
          break;
        case DMAPIState.BadGateway:
          throw new HttpException("BadGateway",
                                  HttpExceptionStatus.BadGateway,
                                  null);
        case DMAPIState.ServiceUnavailable:
          responseString = this.GetResponseXml("503",
                                               httpRequest.TestMemoryStream);
          break;
        case DMAPIState.IncorrectResponse:
          responseString = "Incorrect Response";
          break;
        default:
          Debug.Fail("What kind of DMAPI state is this?");
          break;
      }
      return null;
    }

    HttpResponse GetGAIAResponse(HttpRequest httpRequest) {
      string responseString = string.Empty;
      switch (this.GAIAState) {
        case GAIAState.Authenticate: {
            responseString =
                string.Format(HttpFactory.AuthenticateResponseTemplate,
                              this.AuthToken);
            return new HttpResponse(Encoding.UTF8.GetBytes(responseString));
          }
        case GAIAState.Timeout:
          throw new HttpException("Time out",
                                  HttpExceptionStatus.Timeout,
                                  null);
        case GAIAState.BadAuthentication:
          responseString = "Error=BadAuthentication";
          break;
        case GAIAState.NotVerified:
          responseString = "Error=BadAuthentication";
          break;
        case GAIAState.TermsNotAgreed:
          responseString = "Error=TermsNotAgreed";
          break;
        case GAIAState.CaptchaRequired:
          responseString =
              string.Format(HttpFactory.CaptchaResponseTemplate,
                            this.CaptchaToken);
          break;
        case GAIAState.Unknown:
          responseString = "Error=Unknown";
          break;
        case GAIAState.AccountDeleted:
          responseString = "Error=AccountDeleted";
          break;
        case GAIAState.AccountDisabled:
          responseString = "Error=AccountDisabled";
          break;
        case GAIAState.ServiceDisabled:
          responseString = "Error=ServiceDisabled";
          break;
        case GAIAState.ServiceUnavailable:
          responseString = "Error=ServiceUnavailable";
          break;
        case GAIAState.IncorrectResponse:
          responseString = "Incorrect Response";
          break;
      }
      HttpResponse exceptionResponse =
          new HttpResponse(Encoding.UTF8.GetBytes(responseString));
      throw new HttpException("Forbidden",
                              HttpExceptionStatus.Forbidden,
                              exceptionResponse);
    }

    HttpResponse GetCaptchaResponse(HttpRequest httpRequest) {
      if (!httpRequest.Url.Equals(string.Format(HttpFactory.CaptchaUrlTemplate,
                                                this.CaptchaToken))) {
        throw new HttpException("Other",
                                HttpExceptionStatus.Other,
                                null);
      }
      Image image = new Bitmap(20, 20);
      TestMemoryStream testMemoryStream = new TestMemoryStream();
      image.Save(testMemoryStream, ImageFormat.Jpeg);
      return new HttpResponse(testMemoryStream.GetBuffer());
    }

    internal HttpResponse GetResponseFor(HttpRequest httpRequest) {
      if (this.textWriter != null) {
        this.textWriter.WriteLine("Request:");
        this.textWriter.WriteLine(
            Encoding.UTF8.GetString(httpRequest.TestMemoryStream.GetBuffer(),
                                    0,
                                    (int)httpRequest.TestMemoryStream.Length));
      }
      try {
        HttpResponse httpResponse;
        if (httpRequest.Url.StartsWith(HttpFactory.DMAPIUrl)) {
          httpResponse = this.GetDMAPIResponse(httpRequest);
        } else if (httpRequest.Url.StartsWith(HttpFactory.AccountLoginUrl)) {
          httpResponse = this.GetGAIAResponse(httpRequest);
        } else if (httpRequest.Url.StartsWith(HttpFactory.CaptchaUrl)) {
          httpResponse = this.GetCaptchaResponse(httpRequest);
        } else {
          throw new HttpException("Other",
                                  HttpExceptionStatus.Other,
                                  null);
        }
        if (this.textWriter != null) {
          this.textWriter.WriteLine("Response:");
          this.textWriter.WriteLine(
              Encoding.UTF8.GetString(httpResponse.response));
        }
        return httpResponse;
      } catch (Exception e) {
        if (this.textWriter != null) {
          this.textWriter.WriteLine("Response: Exception");
          this.textWriter.WriteLine(e.Message);
        }
        throw;
      }
    }

    internal void StartRecording(TextWriter textWriter) {
      this.textWriter = textWriter;
    }

    internal void StopRecording() {
      this.textWriter = null;
    }

    #region IHttpFactory Members

    IHttpRequest IHttpFactory.CreateGetRequest(string url) {
      return new HttpRequest(this,
                             url,
                             RequestMethod.Get);
    }

    IHttpRequest IHttpFactory.CreatePostRequest(string url) {
      return new HttpRequest(this,
                             url,
                             RequestMethod.Post);
    }

    #endregion
  }

  class HttpRequest : IHttpRequest {
    readonly HttpFactory httpFactory;
    internal readonly string Url;
    internal readonly RequestMethod Method;
    internal TestMemoryStream TestMemoryStream;
    internal string ContentType;
    internal string UserAgent;
    internal long ContentLength;
    internal Hashtable HeaderTable;

    internal HttpRequest(HttpFactory httpFactory,
                         string url,
                         RequestMethod method) {
      this.httpFactory = httpFactory;
      this.Url = url;
      this.Method = method;
      if (method == RequestMethod.Post) {
        this.TestMemoryStream = new TestMemoryStream();
      }
      this.HeaderTable = new Hashtable();
    }

    #region IHttpRequest Members

    string IHttpRequest.ContentType {
      set {
        this.ContentType = value;
      }
    }

    string IHttpRequest.UserAgent {
      set {
        this.UserAgent = value;
      }
    }

    long IHttpRequest.ContentLength {
      set {
        this.ContentLength = value;
      }
    }

    void IHttpRequest.AddToHeader(string key, string value) {
      this.HeaderTable.Add(key, value);
    }

    Stream IHttpRequest.GetRequestStream() {
      return this.TestMemoryStream;
    }

    IHttpResponse IHttpRequest.GetResponse() {
      return this.httpFactory.GetResponseFor(this);
    }

    #endregion
  }

  class HttpResponse : IHttpResponse {
    internal readonly byte[] response;

    internal HttpResponse(byte[] response) {
      this.response = response;
    }

    #region IHttpResponse Members

    Stream IHttpResponse.GetResponseStream() {
      return new MemoryStream(this.response);
    }

    void IHttpResponse.Close() {
    }

    #endregion
  }
}
