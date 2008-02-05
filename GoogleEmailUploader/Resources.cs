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
using System.Drawing;
using System.Resources;
using System.Globalization;

namespace GoogleEmailUploader {
  /*
   * To add resources first add it using test editor into Resources.resx.
   * Please edit the resx file using text editor not in VS resource designer.
   * Then add a property to access the resource into the class below.
   * Take a look at the string and icon sample given.
   */

  /// <summary>
  /// A strongly-typed resource class, for looking up localized strings, etc.
  /// </summary>
  class Resources {
    private static ResourceManager resourceManager;

    internal static ResourceManager ResourceManager {
      get {
        if (resourceManager == null) {
          resourceManager =
              new ResourceManager(
                  "GoogleEmailUploader.Resources",
                  typeof(Resources).Assembly); ;
        }
        return resourceManager;
      }
    }

    // Add strings here...

    internal static string Locale {
      get {
        return Resources.ResourceManager.GetString("Locale");
      }
    }

    internal static string NoClientPlugins {
      get {
        return Resources.ResourceManager.GetString("NoClientPlugins");
      }
    }

    internal static string ProcessCloseTemplate {
      get {
        return Resources.ResourceManager.GetString("ProcessCloseTemplate");
      }
    }

    internal static string GoogleEmailUploaderAppName {
      get {
        return Resources.ResourceManager.GetString(
            "GoogleEmailUploaderAppName");
      }
    }

    internal static string EmailText {
      get {
        return Resources.ResourceManager.GetString("EmailText");
      }
    }

    internal static string PasswordText {
      get {
        return Resources.ResourceManager.GetString("PasswordText");
      }
    }

    internal static string SigninText {
      get {
        return Resources.ResourceManager.GetString("SigninText");
      }
    }

    internal static string CancelText {
      get {
        return Resources.ResourceManager.GetString("CancelText");
      }
    }

    internal static string FolderLabelCheckBoxText {
      get {
        return Resources.ResourceManager.GetString("FolderLabelCheckBoxText");
      }
    }

    internal static string UploadText {
      get {
        return Resources.ResourceManager.GetString("UploadText");
      }
    }

    internal static string NextText {
      get {
        return Resources.ResourceManager.GetString("NextText");
      }
    }

    internal static string PauseText {
      get {
        return Resources.ResourceManager.GetString("PauseText");
      }
    }

    internal static string ResumeText {
      get {
        return Resources.ResourceManager.GetString("ResumeText");
      }
    }

    internal static string AbortText {
      get {
        return Resources.ResourceManager.GetString("AbortText");
      }
    }

    internal static string BackText {
      get {
        return Resources.ResourceManager.GetString("BackText");
      }
    }

    internal static string FromTemplateText {
      get {
        return Resources.ResourceManager.GetString("FromTemplateText");
      }
    }

    internal static string InstructionsText {
      get {
        return Resources.ResourceManager.GetString("InstructionsText");
      }
    }

    internal static string TitleText {
      get {
        return Resources.ResourceManager.GetString("TitleText");
      }
    }

    internal static string CaptchaInstructionsText {
      get {
        return Resources.ResourceManager.GetString("CaptchaInstructionsText");
      }
    }

    internal static string SignInInfoText {
      get {
        return Resources.ResourceManager.GetString("SignInInfoText");
      }
    }

    internal static string SigningInInfoText {
      get {
        return Resources.ResourceManager.GetString("SigningInInfoText");
      }
    }

    internal static string SignedInInfoText {
      get {
        return Resources.ResourceManager.GetString("SignedInInfoText");
      }
    }

    internal static string SignedInTryAgainInfoText {
      get {
        return Resources.ResourceManager.GetString("SignedInTryAgainInfoText");
      }
    }

    internal static string LoadingClientsText {
      get {
        return Resources.ResourceManager.GetString("LoadingClientsText");
      }
    }

    internal static string SignedInConnectionFailureText {
      get {
        return Resources.ResourceManager.GetString(
            "SignedInConnectionFailureText");
      }
    }

    internal static string SignedInTimeoutText {
      get {
        return Resources.ResourceManager.GetString("SignedInTimeoutText");
      }
    }

    internal static string SignedInUnknownText {
      get {
        return Resources.ResourceManager.GetString("SignedInUnknownText");
      }
    }

    internal static string CaptchaErrorMessageText {
      get {
        return Resources.ResourceManager.GetString("CaptchaErrorMessageText");
      }
    }

    internal static string SignInHeaderText {
      get {
        return Resources.ResourceManager.GetString("SignInHeaderText");
      }
    }

    internal static string SelectEmailHeaderText {
      get {
        return Resources.ResourceManager.GetString("SelectEmailHeaderText");
      }
    }

    internal static string LabelHeaderText {
      get {
        return Resources.ResourceManager.GetString("LabelHeaderText");
      }
    }

    internal static string ImportHeaderText {
      get {
        return Resources.ResourceManager.GetString("ImportHeaderText");
      }
    }

    internal static string SeparatorText {
      get {
        return Resources.ResourceManager.GetString("SeparatorText");
      }
    }

    internal static string ConfirmHeaderText {
      get {
        return Resources.ResourceManager.GetString("ConfirmHeaderText");
      }
    }

    internal static string UploadHeaderText {
      get {
        return Resources.ResourceManager.GetString("UploadHeaderText");
      }
    }

    internal static string SelectUploadEmailsText {
      get {
        return Resources.ResourceManager.GetString("SelectUploadEmailsText");
      }
    }

    internal static string ImportText {
      get {
        return Resources.ResourceManager.GetString("ImportText");
      }
    }

    internal static string ImportInfo {
      get {
        return Resources.ResourceManager.GetString("ImportInfo");
      }
    }

    internal static string ConfirmInstructionText {
      get {
        return Resources.ResourceManager.GetString("ConfirmInstructionText");
      }
    }

    internal static string UploadedMailsTemplateText {
      get {
        return Resources.ResourceManager.GetString("UploadedMailsTemplateText");
      }
    }

    internal static string FailedMailsTemplateText {
      get {
        return Resources.ResourceManager.GetString("FailedMailsTemplateText");
      }
    }

    internal static string MinimizeToTrayText {
      get {
        return Resources.ResourceManager.GetString("MinimizeToTrayText");
      }
    }

    internal static string AddStore {
      get {
        return Resources.ResourceManager.GetString("AddStore");
      }
    }

    internal static string CouldNotOpenStoreTemplate {
      get {
        return Resources.ResourceManager.GetString("CouldNotOpenStoreTemplate");
      }
    }

    internal static string PauseUserAction {
      get {
        return Resources.ResourceManager.GetString("PauseUserAction");
      }
    }

    internal static string PauseConnectionFailuresTemplate {
      get {
        return Resources.ResourceManager.GetString(
            "PauseConnectionFailuresTemplate");
      }
    }

    internal static string ServiceUnavailableTemplate {
      get {
        return Resources.ResourceManager.GetString(
            "ServiceUnavailableTemplate");
      }
    }

    internal static string ServerInternalErrorTemplate {
      get {
        return Resources.ResourceManager.GetString(
            "ServerInternalErrorTemplate");
      }
    }

    internal static string FolderTemplateText {
      get {
        return Resources.ResourceManager.GetString(
            "FolderTemplateText");
      }
    }

    internal static string ThunderbirdText {
      get {
        return Resources.ResourceManager.GetString(
            "ThunderbirdText");
      }
    }

    internal static string MicrosoftOutlookText {
      get {
        return Resources.ResourceManager.GetString(
            "MicrosoftOutlookText");
      }
    }

    internal static string OutlookExpressText {
      get {
        return Resources.ResourceManager.GetString(
            "OutlookExpressText");
      }
    }

    internal static string ImportToGmailText {
      get {
        return Resources.ResourceManager.GetString(
            "ImportToGmailText");
      }
    }

    internal static string SignInIntroText {
      get {
        return Resources.ResourceManager.GetString(
            "SignInIntroText");
      }
    }

    internal static string ReasonTemplate {
      get {
        return Resources.ResourceManager.GetString(
            "ReasonTemplate");
      }
    }

    internal static string LoggedInInfo {
      get {
        return Resources.ResourceManager.GetString(
            "LoggedInInfo");
      }
    }

    internal static string SelectEmailPrograms {
      get {
        return Resources.ResourceManager.GetString(
            "SelectEmailPrograms");
      }
    }

    internal static string CustomizeText {
      get {
        return Resources.ResourceManager.GetString(
            "CustomizeText");
      }
    }

    internal static string ArchiveEverythingText {
      get {
        return Resources.ResourceManager.GetString(
            "ArchiveEverythingText");
      }
    }

    internal static string ArchiveEverythingInfo {
      get {
        return Resources.ResourceManager.GetString(
            "ArchiveEverythingInfo");
      }
    }

    internal static string ReadyText {
      get {
        return Resources.ResourceManager.GetString(
            "ReadyText");
      }
    }

    internal static string NoteText {
      get {
        return Resources.ResourceManager.GetString(
            "NoteText");
      }
    }

    internal static string UploadInstruction {
      get {
        return Resources.ResourceManager.GetString(
            "UploadInstruction");
      }
    }

    internal static string TipTitle {
      get {
        return Resources.ResourceManager.GetString(
            "TipTitle");
      }
    }

    internal static string TipText {
      get {
        return Resources.ResourceManager.GetString(
            "TipText");
      }
    }

    internal static string ImportCompleteText {
      get {
        return Resources.ResourceManager.GetString(
            "ImportCompleteText");
      }
    }

    internal static string ImportCompleteHeader {
      get {
        return Resources.ResourceManager.GetString(
            "ImportCompleteHeader");
      }
    }

    internal static string ImportCompleteInfo {
      get {
        return Resources.ResourceManager.GetString(
            "ImportCompleteInfo");
      }
    }

    internal static string ImportIncompleteHeader {
      get {
        return Resources.ResourceManager.GetString(
            "ImportIncompleteHeader");
      }
    }

    internal static string ImportAbortedHeader {
      get {
        return Resources.ResourceManager.GetString(
            "ImportAbortedHeader");
      }
    }

    internal static string ImportForbiddenHeader {
      get {
        return Resources.ResourceManager.GetString(
            "ImportForbiddenHeader");
      }
    }

    internal static string ImportUnauthorizedHeader {
      get {
        return Resources.ResourceManager.GetString(
            "ImportUnauthorizedHeader");
      }
    }

    internal static string FinishText {
      get {
        return Resources.ResourceManager.GetString(
            "FinishText");
      }
    }

    internal static string AbortWaitingText {
      get {
        return Resources.ResourceManager.GetString(
            "AbortWaitingText");
      }
    }

    internal static string ErrorHeaderText {
      get {
        return Resources.ResourceManager.GetString(
            "ErrorHeaderText");
      }
    }

    internal static string OpenLogText {
      get {
        return Resources.ResourceManager.GetString(
            "OpenLogText");
      }
    }

    internal static string ToSeeDetailsText {
      get {
        return Resources.ResourceManager.GetString(
            "ToSeeDetailsText");
      }
    }

    internal static string SelectionInfoTemplateText {
      get {
        return Resources.ResourceManager.GetString(
            "SelectionInfoTemplateText");
      }
    }

    internal static string ReadingMailsTemplate {
      get {
        return Resources.ResourceManager.GetString(
            "ReadingMailsTemplate");
      }
    }

    internal static string UploadingMailsTemplate {
      get {
        return Resources.ResourceManager.GetString(
            "UploadingMailsTemplate");
      }
    }

    // Add icons here...

    internal static Image WarningImage {
      get {
        object obj = Resources.ResourceManager.GetObject("WarningImage");
        return (Image)obj;
      }
    }

    internal static Image GoogleEmailUploaderBackgroundImage {
      get {
        object obj = Resources.ResourceManager.GetObject(
            "GoogleEmailUploaderBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image GoogleEmailUploaderWithCaptchaBackgroundImage {
      get {
        object obj = Resources.ResourceManager.GetObject(
            "GoogleEmailUploaderWithCaptchaBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image SignInPanelBackgroundImage {
      get {
        object obj = Resources.ResourceManager.GetObject(
            "SignInPanelBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image SignInPanelWithCaptchaBackgroundImage {
      get {
        object obj = Resources.ResourceManager.GetObject(
            "SignInPanelWithCaptchaBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image ProcessBarBackground {
      get {
        object obj = Resources.ResourceManager.GetObject(
            "ProgressBarBackground");
        return (Image)obj;
      }
    }

    internal static Image GoogleEmailUploaderImportCompleteBackgroundImage {
      get {
        object obj = Resources.ResourceManager.GetObject(
            "GoogleEmailUploaderImportCompleteBackgroundImage");
        return (Image)obj;
      }
    }
  }
}
