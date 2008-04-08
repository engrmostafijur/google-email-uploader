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
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GoogleEmailUploader {
  /// <summary>
  /// Enum representing the result of authentication.
  /// </summary>
  public enum AuthenticationResultKind {
    /// <summary>
    /// The login request used a username or password that is recognized.
    /// </summary>
    Authenticated,
    /// <summary>
    /// The login request used a username or password that is not
    /// recognized.
    /// </summary>
    BadAuthentication,
    /// <summary>
    /// The account email address has not been verified. The user will need
    /// to access their Google account directly to resolve the issue before
    /// logging in using a non-Google application.
    /// </summary>
    NotVerified,
    /// <summary>
    /// The user has not agreed to terms. The user will need to access their
    /// Google account directly to resolve the issue before logging in using
    /// a non-Google application.
    /// </summary>
    TermsNotAgreed,
    /// <summary>
    /// A CAPTCHA is required. (A response with this error code will also
    /// contain an image URL and a CAPTCHA token.)
    /// </summary>
    CAPTCHARequired,
    /// <summary>
    /// The error is unknown or unspecified; the request contained invalid
    /// input or was malformed.
    /// </summary>
    Unknown,
    /// <summary>
    /// The user account has been deleted.
    /// </summary>
    AccountDeleted,
    /// <summary>
    /// The user account has been disabled.
    /// </summary>
    AccountDisabled,
    /// <summary>
    /// The user's access to the specified service has been disabled.
    /// (The user account may still be valid.)
    /// </summary>
    ServiceDisabled,
    /// <summary>
    /// The service is not available; try again later.
    /// </summary>
    ServiceUnavailable,
    /// <summary>
    /// The request timed out.
    /// </summary>
    TimeOut,
    /// <summary>
    /// There was error in connecting to the service.
    /// </summary>
    ConnectionFailure,
    /// <summary>
    /// Response could not be parsed.
    /// </summary>
    ResponseParseError,
  }

  /// <summary>
  /// Class representing the actual response from the Google Authentication
  /// server.
  /// </summary>
  public class AuthenticationResponse {
    /// <summary>
    /// The result of the authentication.
    /// </summary>
    public AuthenticationResultKind AuthenticationResult {
      get {
        return this.authenticationResult;
      }
    }
    AuthenticationResultKind authenticationResult;

    /// <summary>
    /// Helpful url if its non null.
    /// </summary>
    public string Url {
      get {
        return this.url;
      }
    }
    string url;

    /// <summary>
    /// Non null only when AuthenticationResult is CAPTCHARequired.
    /// </summary>
    public string CAPTCHAUrl {
      get {
        return this.captchaUrl;
      }
    }
    string captchaUrl;

    /// <summary>
    /// Use only when AuthenticationResult is CAPTCHARequired.
    /// Returns null if there was error getting image.
    /// </summary>
    public Image CAPTCHAImage {
      get {
        return this.captchaImage;
      }
    }
    Image captchaImage;

    /// <summary>
    /// Non null only when AuthenticationResult is CAPTCHARequired.
    /// </summary>
    public string CAPTCHAToken {
      get {
        return this.captchaToken;
      }
    }
    string captchaToken;

    /// <summary>
    /// Non null only when AuthenticationResult is Authenticated.
    /// </summary>
    public string AuthToken {
      get {
        return this.authToken;
      }
    }
    string authToken;

    /// <summary>
    /// Non null only when AuthenticationResult is Authenticated.
    /// </summary>
    public string SIDToken {
      get {
        return this.sidToken;
      }
    }
    string sidToken;

    /// <summary>
    /// Non null only when AuthenticationResult is Authenticated.
    /// </summary>
    public string LSIDToken {
      get {
        return this.lsidToken;
      }
    }
    string lsidToken;

    /// <summary>
    /// Non null only when AuthenticationResult is ConnectionFailure.
    /// </summary>
    public HttpException HttpException {
      get {
        return this.httpException;
      }
    }
    HttpException httpException;

    /// <summary>
    /// Factory method for creating the response when authentication succeeds.
    /// </summary>
    public static AuthenticationResponse CreateAuthenticatedResponse(
        string authToken,
        string sidToken,
        string lsidToken) {
      AuthenticationResponse authenticationResponse =
          new AuthenticationResponse();
      authenticationResponse.authenticationResult =
          AuthenticationResultKind.Authenticated;
      authenticationResponse.authToken = authToken;
      authenticationResponse.sidToken = sidToken;
      authenticationResponse.lsidToken = lsidToken;
      return authenticationResponse;
    }

    /// <summary>
    /// Factory method for creating the response when autentication needs
    /// solution for captcha challenge.
    /// </summary>
    public static AuthenticationResponse CreateCAPTCHAResponse(
        string captchaUrl,
        string captchaToken,
        string url,
        Image captchaImage) {
      AuthenticationResponse authenticationResponse =
          new AuthenticationResponse();
      authenticationResponse.authenticationResult =
          AuthenticationResultKind.CAPTCHARequired;
      authenticationResponse.captchaUrl = captchaUrl;
      authenticationResponse.captchaToken = captchaToken;
      authenticationResponse.url = url;
      authenticationResponse.captchaImage = captchaImage;
      return authenticationResponse;
    }

    /// <summary>
    /// Factory method for creating response when there is error connection.
    /// </summary>
    public static AuthenticationResponse CreateConnectionFailureResponse(
      HttpException httpException) {
      Debug.Assert(httpException != null);
      AuthenticationResponse authenticationResponse =
          new AuthenticationResponse();
      authenticationResponse.authenticationResult =
          AuthenticationResultKind.ConnectionFailure;
      authenticationResponse.httpException = httpException;
      return authenticationResponse;
    }

    /// <summary>
    /// Factory method for creating response when the connection timed out.
    /// </summary>
    public static AuthenticationResponse CreateTimeoutResponse() {
      AuthenticationResponse authenticationResponse =
          new AuthenticationResponse();
      authenticationResponse.authenticationResult =
          AuthenticationResultKind.TimeOut;
      return authenticationResponse;
    }

    /// <summary>
    /// Factory method for creating response when there is error in parsing
    /// server response.
    /// </summary>
    public static AuthenticationResponse CreateParseErrorResponse() {
      AuthenticationResponse authenticationResponse =
          new AuthenticationResponse();
      authenticationResponse.authenticationResult =
          AuthenticationResultKind.ResponseParseError;
      return authenticationResponse;
    }

    /// <summary>
    /// Factory method for creating response when there is authentication
    /// failure.
    /// </summary>
    public static AuthenticationResponse CreateFailureResponse(
        AuthenticationResultKind authenticationResultKind,
        string url) {
      Debug.Assert(
          authenticationResultKind != AuthenticationResultKind.Authenticated &&
          authenticationResultKind !=
            AuthenticationResultKind.CAPTCHARequired &&
          authenticationResultKind !=
            AuthenticationResultKind.ConnectionFailure &&
          authenticationResultKind != AuthenticationResultKind.TimeOut &&
          authenticationResultKind !=
            AuthenticationResultKind.ResponseParseError);
      AuthenticationResponse authenticationResponse =
          new AuthenticationResponse();
      authenticationResponse.authenticationResult = authenticationResultKind;
      authenticationResponse.url = url;
      return authenticationResponse;
    }
  }

  /// <summary>
  /// Enumeration identifing kind of google account.
  /// </summary>
  enum AccountType {
    /// <summary>
    /// Authenticate as a Google account only
    /// </summary>
    Google,
    /// <summary>
    /// Authenticate as a hosted account only
    /// </summary>
    Hosted,
    /// <summary>
    /// Authenticate first as a hosted account; if attempt fails, authenticate
    /// as a Google account
    /// </summary>
    GoogleOrHosted,
  }

  /// <summary>
  /// Class encapsulating the authentication logic.
  /// </summary>
  class GoogleAuthenticator {
    const string CheckPasswordTemplate =
        "accountType={0}&Email={1}&Passwd={2}&source={3}";
    const string CheckPasswordCAPTCHATemplate =
        "accountType={0}&Email={1}&Passwd={2}&source={3}"
          + "&logintoken={4}&logincaptcha={5}";
    const string AuthenticateTemplate =
        "accountType={0}&Email={1}&Passwd={2}&service={3}&source={4}";
    const string AuthenticationURL =
        "https://www.google.com/accounts/ClientLogin";
    const string CAPTCHAURLPrefix = "http://www.google.com/accounts/";
    const string ApplicationURLEncoded = "application/x-www-form-urlencoded";
    static readonly char[] SplitChars = new char[] {
        '\r',
        '\n'};
    const string SID = "SID";
    const string LSID = "LSID";
    const string Auth = "Auth";
    const string Url = "Url";
    const string Error = "Error";
    const string CaptchaToken = "CaptchaToken";
    const string CaptchaUrl = "CaptchaUrl";
    const string BadAuthentication = "BadAuthentication";
    const string NotVerified = "NotVerified";
    const string TermsNotAgreed = "TermsNotAgreed";
    const string CaptchaRequired = "CaptchaRequired";
    const string Unknown = "Unknown";
    const string AccountDeleted = "AccountDeleted";
    const string AccountDisabled = "AccountDisabled";
    const string ServiceDisabled = "ServiceDisabled";
    const string ServiceUnavailable = "ServiceUnavailable";

    readonly IHttpFactory HttpFactory;
    readonly string AccountTypeName;
    readonly string ApplicationName;

    /// <summary>
    /// Constructor of GoogleAuthenticator.
    /// </summary>
    /// <param name="serviceName">
    /// Name of service for which authentication is requested
    /// </param>
    /// <param name="timeout">
    /// Timeout in miliseconds. Use Timeout.Infinite for no timeout.
    /// </param>
    internal GoogleAuthenticator(IHttpFactory httpFactory,
                                 AccountType accountType,
                                 string applicationName) {
      this.HttpFactory = httpFactory;
      switch (accountType) {
        case AccountType.Google:
          this.AccountTypeName = "GOOGLE";
          break;
        case AccountType.Hosted:
          this.AccountTypeName = "HOSTED";
          break;
        case AccountType.GoogleOrHosted:
          this.AccountTypeName = "HOSTED_OR_GOOGLE";
          break;
      }
      this.ApplicationName = applicationName;
    }

    Image DownloadCAPTCHAImage(string captchaUrl) {
      IHttpRequest httpRequest = this.HttpFactory.CreateGetRequest(captchaUrl);
      IHttpResponse httpResponse = null;
      try {
        httpResponse = httpRequest.GetResponse();
      } catch (HttpException) {
        return null;
      }
      Image retImage = null;
      using (Stream respStream = httpResponse.GetResponseStream()) {
        try {
          retImage = Image.FromStream(respStream);
        } catch (ArgumentException) {
          return null;
        }
      }
      httpResponse.Close();
      return retImage;
    }

    AuthenticationResponse ScanAndCreateResponse(string response) {
      if (response == null || response.Length == 0) {
        return AuthenticationResponse.CreateParseErrorResponse();
      }
      string[] splits = response.Split(GoogleAuthenticator.SplitChars);
      string sidValue = null;
      string lsidValue = null;
      string authValue = null;
      string urlValue = null;
      string errorValue = null;
      string captchaToken = null;
      string captchaUrlSuffix = null;
      for (int i = 0; i < splits.Length; ++i) {
        string split = splits[i];
        if (split.StartsWith(GoogleAuthenticator.SID)) {
          sidValue = split.Substring(GoogleAuthenticator.SID.Length + 1);
        } else if (split.StartsWith(GoogleAuthenticator.LSID)) {
          lsidValue = split.Substring(GoogleAuthenticator.LSID.Length + 1);
        } else if (split.StartsWith(GoogleAuthenticator.Auth)) {
          authValue = split.Substring(GoogleAuthenticator.Auth.Length + 1);
        } else if (split.StartsWith(GoogleAuthenticator.Url)) {
          urlValue = split.Substring(GoogleAuthenticator.Url.Length + 1);
        } else if (split.StartsWith(GoogleAuthenticator.Error)) {
          errorValue = split.Substring(GoogleAuthenticator.Error.Length + 1);
        } else if (split.StartsWith(GoogleAuthenticator.CaptchaToken)) {
          captchaToken = split.Substring(
              GoogleAuthenticator.CaptchaToken.Length + 1);
        } else if (split.StartsWith(GoogleAuthenticator.CaptchaUrl)) {
          captchaUrlSuffix = split.Substring(
              GoogleAuthenticator.CaptchaUrl.Length + 1);
        }
      }
      if (errorValue != null) {
        switch (errorValue) {
          case GoogleAuthenticator.BadAuthentication:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.BadAuthentication, urlValue);
          case GoogleAuthenticator.NotVerified:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.NotVerified, urlValue);
          case GoogleAuthenticator.TermsNotAgreed:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.TermsNotAgreed, urlValue);
          case GoogleAuthenticator.CaptchaRequired: {
              Debug.Assert(captchaUrlSuffix != null && captchaToken != null);
              if (captchaUrlSuffix == null || captchaToken == null) {
                goto case GoogleAuthenticator.Unknown;
              }
              string captchaUrl =
                  GoogleAuthenticator.CAPTCHAURLPrefix + captchaUrlSuffix;
              return AuthenticationResponse.CreateCAPTCHAResponse(
                  captchaUrl,
                  captchaToken,
                  urlValue,
                  this.DownloadCAPTCHAImage(captchaUrl));
            }
          case GoogleAuthenticator.Unknown:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.Unknown, urlValue);
          case GoogleAuthenticator.AccountDeleted:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.AccountDeleted, urlValue);
          case GoogleAuthenticator.AccountDisabled:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.AccountDisabled, urlValue);
          case GoogleAuthenticator.ServiceDisabled:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.ServiceDisabled, urlValue);
          case GoogleAuthenticator.ServiceUnavailable:
            return AuthenticationResponse.CreateFailureResponse(
                AuthenticationResultKind.ServiceUnavailable, urlValue);
        }
      } else if (lsidValue != null) {
        // Since google services do not use sid and lsid,
        // we can ignore the case where sidValue and lsidValue are null.
        Debug.Assert(sidValue != null);
        return AuthenticationResponse.CreateAuthenticatedResponse(
            authValue,
            sidValue,
            lsidValue);
      }
      return AuthenticationResponse.CreateParseErrorResponse();
    }

    /// <summary>
    /// Autenticate with email and password for particular service
    /// </summary>
    internal AuthenticationResponse AuthenticateForService(string emailId,
                                                           string password,
                                                           string serviceName) {
      int atIndex = emailId.IndexOf('@');
      if (atIndex == -1 || atIndex >= emailId.Length) {
        return AuthenticationResponse.CreateFailureResponse(
            AuthenticationResultKind.BadAuthentication,
            null);
      }
      string requestString =
          string.Format(
              GoogleAuthenticator.AuthenticateTemplate,
              this.AccountTypeName,
              emailId,
              password,
              serviceName,
              this.ApplicationName);
      return this.AuthenticateAndParseResponse(requestString);
    }

    /// <summary>
    /// Check email and password
    /// </summary>
    internal AuthenticationResponse CheckPassword(string emailId,
                                                  string password) {
      int atIndex = emailId.IndexOf('@');
      if (atIndex == -1 || atIndex >= emailId.Length) {
        return AuthenticationResponse.CreateFailureResponse(
            AuthenticationResultKind.BadAuthentication,
            null);
      }
      string requestString =
          string.Format(
              GoogleAuthenticator.CheckPasswordTemplate,
              this.AccountTypeName,
              emailId,
              password,
              this.ApplicationName);
      return this.AuthenticateAndParseResponse(requestString);
    }

    /// <summary>
    /// Check email and password with CAPTCHA solution
    /// </summary>
    internal AuthenticationResponse CheckPasswordCAPTCHA(
        string emailId,
        string password,
        string captchaToken,
        string captchaSolution) {
      int atIndex = emailId.IndexOf('@');
      if (atIndex == -1 || atIndex >= emailId.Length) {
        return AuthenticationResponse.CreateFailureResponse(
            AuthenticationResultKind.BadAuthentication,
            null);
      }
      string requestString =
          string.Format(
              GoogleAuthenticator.CheckPasswordCAPTCHATemplate,
              this.AccountTypeName,
              emailId,
              password,
              this.ApplicationName,
              captchaToken,
              captchaSolution);
      return this.AuthenticateAndParseResponse(requestString);
    }

    private AuthenticationResponse AuthenticateAndParseResponse(
        string requestString) {
      try {
        GoogleEmailUploaderTrace.EnteringMethod(
            "GoogleAuthenticator.AuthenticateAndParseResponse");
        IHttpResponse httpResponse = null;
        try {
          IHttpRequest httpRequest =
              this.HttpFactory.CreatePostRequest(
                  GoogleAuthenticator.AuthenticationURL);
          httpRequest.ContentType = GoogleAuthenticator.ApplicationURLEncoded;
          using (Stream newStream = httpRequest.GetRequestStream()) {
            byte[] data = Encoding.UTF8.GetBytes(requestString);
            newStream.Write(data, 0, data.Length);
          }
          httpResponse = httpRequest.GetResponse();
        } catch (HttpException httpException) {
          GoogleEmailUploaderTrace.WriteLine(httpException.Message);
          if (httpException.Status == HttpExceptionStatus.Forbidden) {
            httpResponse = httpException.Response;
          } else {
            return AuthenticationResponse.CreateConnectionFailureResponse(
                httpException);
          }
        }
        Debug.Assert(httpResponse != null);
        string responseString = null;
        using (Stream respStream = httpResponse.GetResponseStream()) {
          using (StreamReader readStream =
              new StreamReader(respStream, Encoding.UTF8)) {
            responseString = readStream.ReadToEnd();
          }
        }
        httpResponse.Close();
        AuthenticationResponse authenticationResponse =
            this.ScanAndCreateResponse(responseString);
        GoogleEmailUploaderTrace.WriteLine("Authentication result: {0}",
            authenticationResponse.AuthenticationResult);
        return authenticationResponse;
      } finally {
        GoogleEmailUploaderTrace.ExitingMethod(
            "GoogleAuthenticator.AuthenticateAndParseResponse");
      }
    }
  }
}

