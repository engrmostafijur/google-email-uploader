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

using System;
using System.Net;
using System.Text;
using System.IO;

namespace GoogleEmailUploader {
  // We abstract out the http request responses we do so that we can test
  // GoogleEmailUploader using a mocks.

  /// <summary>
  /// Interface for creating http requests.
  /// </summary>
  public interface IHttpFactory {
    /// <summary>
    /// Create a http get request.
    /// </summary>
    IHttpRequest CreateGetRequest(string url);

    /// <summary>
    /// Create a http post request.
    /// </summary>
    IHttpRequest CreatePostRequest(string url);
  }

  /// <summary>
  /// Iterface representing http request.
  /// </summary>
  public interface IHttpRequest {
    /// <summary>
    /// The content type of the http request.
    /// </summary>
    string ContentType {
      set;
    }

    /// <summary>
    /// The user agent for the request.
    /// </summary>
    string UserAgent {
      set;
    }

    /// <summary>
    /// The length of content.
    /// </summary>
    long ContentLength {
      set;
    }

    /// <summary>
    /// Method to add key value pairs to the request header.
    /// </summary>
    void AddToHeader(string key,
                     string value);

    /// <summary>
    /// Get the request stream to add the conent of the request.
    /// </summary>
    Stream GetRequestStream();

    /// <summary>
    /// Process the request and get the response.
    /// </summary>
    IHttpResponse GetResponse();
  }

  /// <summary>
  /// Interface representing http response.
  /// </summary>
  public interface IHttpResponse {
    /// <summary>
    /// Get the response stream to read the conent of response
    /// </summary>
    Stream GetResponseStream();

    /// <summary>
    /// Close the response.
    /// </summary>
    void Close();
  }

  /// <summary>
  /// Status for the http eexceptions
  /// </summary>
  public enum HttpExceptionStatus {
    /// <summary>
    /// The client provided with wrong credentials
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The client cant uplaod to the given account
    /// </summary>
    Forbidden,

    /// <summary>
    /// Connection closed because of large upload.
    /// </summary>
    BadGateway,

    /// <summary>
    /// Corresponds to WebExceptionStatus.ProtocolError which is not one of the
    /// above
    /// </summary>
    ProtocolError,

    /// <summary>
    /// Corresponds to WebExceptionStatus.Timeout
    /// </summary>
    Timeout,

    /// <summary>
    /// Corresponds to any other WebExceptionStatus
    /// </summary>
    Other,
  }

  public class HttpException : Exception {
    HttpExceptionStatus status;
    IHttpResponse response;

    public HttpException(string message,
                         HttpExceptionStatus status,
                         IHttpResponse response)
      : base(message) {
      this.status = status;
      this.response = response;
    }

    public HttpExceptionStatus Status {
      get {
        return this.status;
      }
    }

    public IHttpResponse Response {
      get {
        return this.response;
      }
    }

    internal static HttpException FromWebException(WebException webException) {
      GoogleEmailUploaderTrace.WriteLine(webException.ToString());
      HttpResponse httpResponse = null;
      if (webException.Response != null) {
        httpResponse = new HttpResponse((HttpWebResponse)webException.Response);
      }
      HttpExceptionStatus httpExceptionStatus;
      switch (webException.Status) {
        case WebExceptionStatus.ProtocolError:
          if (webException.Response != null) {
            HttpStatusCode httpStatusCode =
                ((HttpWebResponse)webException.Response).StatusCode;
            if (httpStatusCode == HttpStatusCode.Unauthorized) {
              httpExceptionStatus = HttpExceptionStatus.Unauthorized;
              break;
            } else if (httpStatusCode == HttpStatusCode.Forbidden) {
              httpExceptionStatus = HttpExceptionStatus.Forbidden;
              break;
            } else if (httpStatusCode == HttpStatusCode.BadGateway) {
              httpExceptionStatus = HttpExceptionStatus.BadGateway;
              break;
            }
          }
          httpExceptionStatus = HttpExceptionStatus.ProtocolError;
          break;
        case WebExceptionStatus.Timeout:
          httpExceptionStatus = HttpExceptionStatus.Timeout;
          break;
        default:
          httpExceptionStatus = HttpExceptionStatus.Other;
          break;
      }
      return new HttpException(webException.Message,
                               httpExceptionStatus,
                               httpResponse);
    }
  }

  class HttpFactory : IHttpFactory {
    IHttpRequest IHttpFactory.CreateGetRequest(string url) {
      try {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.Method = "GET";
        httpWebRequest.KeepAlive = false;
        httpWebRequest.ProtocolVersion = HttpVersion.Version10;
        return new HttpRequest(httpWebRequest);
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }

    IHttpRequest IHttpFactory.CreatePostRequest(string url) {
      try {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.Method = "POST";
        httpWebRequest.AllowAutoRedirect = false;
        httpWebRequest.KeepAlive = false;
        httpWebRequest.ProtocolVersion = HttpVersion.Version10;
        return new HttpRequest(httpWebRequest);
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }
  }

  class HttpRequest : IHttpRequest {
    HttpWebRequest httpWebRequest;

    internal HttpRequest(HttpWebRequest httpWebRequest) {
      this.httpWebRequest = httpWebRequest;
    }

    #region IHttpRequest Members

    string IHttpRequest.ContentType {
      set {
        this.httpWebRequest.ContentType = value;
      }
    }

    string IHttpRequest.UserAgent {
      set {
        this.httpWebRequest.UserAgent = value;
      }
    }

    long IHttpRequest.ContentLength {
      set {
        this.httpWebRequest.ContentLength = value;
      }
    }

    void IHttpRequest.AddToHeader(string key,
                                  string value) {
      try {
        this.httpWebRequest.Headers.Add(key,
                                        value);
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }

    Stream IHttpRequest.GetRequestStream() {
      try {
        return this.httpWebRequest.GetRequestStream();
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }

    IHttpResponse IHttpRequest.GetResponse() {
      try {
        HttpWebResponse httpWebResponse =
            (HttpWebResponse)this.httpWebRequest.GetResponse();
        return new HttpResponse(httpWebResponse);
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }

    #endregion
  }

  class HttpResponse : IHttpResponse {
    HttpWebResponse httpWebResponse;

    internal HttpResponse(HttpWebResponse httpWebResponse) {
      this.httpWebResponse = httpWebResponse;
    }

    #region IHttpResponse Members

    Stream IHttpResponse.GetResponseStream() {
      try {
        return this.httpWebResponse.GetResponseStream();
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }

    void IHttpResponse.Close() {
      try {
        this.httpWebResponse.Close();
      } catch (WebException we) {
        throw HttpException.FromWebException(we);
      }
    }

    #endregion
  }
}

