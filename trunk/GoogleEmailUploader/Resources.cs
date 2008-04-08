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
    private static ResourceManager stringsResourceManager;
    private static ResourceManager imagesResourceManager;

    internal static ResourceManager StringsResourceManager {
      get {
        if (stringsResourceManager == null) {
          stringsResourceManager =
              new ResourceManager(
                  "GoogleEmailUploader.Resources.Strings",
                  typeof(Resources).Assembly);
        }
        return stringsResourceManager;
      }
    }

    internal static ResourceManager ImagesResourceManager {
      get {
        if (imagesResourceManager == null) {
          imagesResourceManager =
              new ResourceManager(
                  "GoogleEmailUploader.Resources.Images",
                  typeof(Resources).Assembly);
        }
        return imagesResourceManager;
      }
    }

    // Add strings here...

    internal static string LocaleText {
      get {
        return Resources.StringsResourceManager.GetString("LocaleText");
      }
    }

    internal static string CantSigninHelpUrl {
      get {
        return Resources.StringsResourceManager.GetString("CantSigninHelpUrl");
      }
    }

    internal static string ImportedText {
      get {
        return Resources.StringsResourceManager.GetString("ImportedText");
      }
    }

    internal static string CantSigninText {
      get {
        return Resources.StringsResourceManager.GetString("CantSigninText");
      }
    }

    internal static string NoClientPluginsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "NoClientPluginsText");
      }
    }

    internal static string ProcessCloseTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ProcessCloseTemplateText");
      }
    }

    internal static string GoogleEmailUploaderAppNameText {
      get {
        return Resources.StringsResourceManager.GetString(
            "GoogleEmailUploaderAppNameText");
      }
    }

    internal static string AccountText {
      get {
        return Resources.StringsResourceManager.GetString("AccountText");
      }
    }

    internal static string EmailText {
      get {
        return Resources.StringsResourceManager.GetString("EmailText");
      }
    }

    internal static string PasswordText {
      get {
        return Resources.StringsResourceManager.GetString("PasswordText");
      }
    }

    internal static string SigninText {
      get {
        return Resources.StringsResourceManager.GetString("SigninText");
      }
    }

    internal static string CancelText {
      get {
        return Resources.StringsResourceManager.GetString("CancelText");
      }
    }

    internal static string ContactsTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ContactsTemplateText");
      }
    }

    internal static string FolderLabelCheckBoxText {
      get {
        return Resources.StringsResourceManager.GetString(
            "FolderLabelCheckBoxText");
      }
    }

    internal static string NextText {
      get {
        return Resources.StringsResourceManager.GetString("NextText");
      }
    }

    internal static string PauseText {
      get {
        return Resources.StringsResourceManager.GetString("PauseText");
      }
    }

    internal static string ResumeText {
      get {
        return Resources.StringsResourceManager.GetString("ResumeText");
      }
    }

    internal static string StopText {
      get {
        return Resources.StringsResourceManager.GetString("StopText");
      }
    }

    internal static string BackText {
      get {
        return Resources.StringsResourceManager.GetString("BackText");
      }
    }

    internal static string InstructionsText {
      get {
        return Resources.StringsResourceManager.GetString("InstructionsText");
      }
    }

    internal static string CaptchaInstructionsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "CaptchaInstructionsText");
      }
    }

    internal static string SignInInfoText {
      get {
        return Resources.StringsResourceManager.GetString("SignInInfoText");
      }
    }

    internal static string SigningInInfoText {
      get {
        return Resources.StringsResourceManager.GetString("SigningInInfoText");
      }
    }

    internal static string SignedInInfoText {
      get {
        return Resources.StringsResourceManager.GetString("SignedInInfoText");
      }
    }

    internal static string SignedInTryAgainInfoText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SignedInTryAgainInfoText");
      }
    }

    internal static string LoadingClientsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "LoadingClientsText");
      }
    }

    internal static string SignedInConnectionFailureText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SignedInConnectionFailureText");
      }
    }

    internal static string SignedInTimeoutText {
      get {
        return Resources.StringsResourceManager.GetString(
          "SignedInTimeoutText");
      }
    }

    internal static string SignedInUnknownText {
      get {
        return Resources.StringsResourceManager.GetString(
          "SignedInUnknownText");
      }
    }

    internal static string CaptchaErrorMessageText {
      get {
        return Resources.StringsResourceManager.GetString(
            "CaptchaErrorMessageText");
      }
    }

    internal static string SignInHeaderText {
      get {
        return Resources.StringsResourceManager.GetString("SignInHeaderText");
      }
    }

    internal static string SelectHeaderText {
      get {
        return Resources.StringsResourceManager.GetString("SelectHeaderText");
      }
    }

    internal static string LabelHeaderText {
      get {
        return Resources.StringsResourceManager.GetString("LabelHeaderText");
      }
    }

    internal static string SeparatorText {
      get {
        return Resources.StringsResourceManager.GetString("SeparatorText");
      }
    }

    internal static string UploadHeaderText {
      get {
        return Resources.StringsResourceManager.GetString("UploadHeaderText");
      }
    }

    internal static string SelectUploadFoldersText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SelectUploadFoldersText");
      }
    }

    internal static string UploadText {
      get {
        return Resources.StringsResourceManager.GetString("UploadText");
      }
    }

    internal static string UploadingMailText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadingMailText");
      }
    }

    internal static string UploadingContactsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadingContactsText");
      }
    }

    internal static string ConfirmInstructionText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ConfirmInstructionText");
      }
    }

    internal static string UploadedItemsTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadedItemsTemplateText");
      }
    }

    internal static string FailedItemTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "FailedItemsTemplateText");
      }
    }

    internal static string FailedItemsTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "FailedItemsTemplateText");
      }
    }

    internal static string MinimizeToTrayText {
      get {
        return Resources.StringsResourceManager.GetString("MinimizeToTrayText");
      }
    }

    internal static string AddMailBoxTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "AddMailBoxTemplateText");
      }
    }

    internal static string CouldNotOpenStoreTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "CouldNotOpenStoreTemplateText");
      }
    }

    internal static string PauseUserActionText {
      get {
        return Resources.StringsResourceManager.GetString(
            "PauseUserActionText");
      }
    }

    internal static string PauseConnectionFailuresTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "PauseConnectionFailuresTemplateText");
      }
    }

    internal static string ServiceUnavailableTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ServiceUnavailableTemplateText");
      }
    }

    internal static string ServerInternalErrorTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ServerInternalErrorTemplateText");
      }
    }

    internal static string FolderTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "FolderTemplateText");
      }
    }

    internal static string StoreTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "StoreTemplateText");
      }
    }

    internal static string ThunderbirdText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ThunderbirdText");
      }
    }

    internal static string MicrosoftOutlookText {
      get {
        return Resources.StringsResourceManager.GetString(
            "MicrosoftOutlookText");
      }
    }

    internal static string OutlookExpressText {
      get {
        return Resources.StringsResourceManager.GetString(
            "OutlookExpressText");
      }
    }

    internal static string UploadToGmailText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadToGmailText");
      }
    }

    internal static string SignInIntroText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SignInIntroText");
      }
    }

    internal static string ReasonTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ReasonTemplateText");
      }
    }

    internal static string LoggedInInfoText {
      get {
        return Resources.StringsResourceManager.GetString(
            "LoggedInInfoText");
      }
    }

    internal static string SelectEmailProgramsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SelectEmailProgramsText");
      }
    }

    internal static string CustomizeText {
      get {
        return Resources.StringsResourceManager.GetString(
            "CustomizeText");
      }
    }

    internal static string ArchiveEverythingText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ArchiveEverythingText");
      }
    }

    internal static string FolderInfoText {
      get {
        return Resources.StringsResourceManager.GetString(
            "FolderInfoText");
      }
    }

    internal static string ArchiveEverythingInfoText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ArchiveEverythingInfoText");
      }
    }

    internal static string ReadyText {
      get {
        return Resources.StringsResourceManager.GetString(
            "ReadyText");
      }
    }

    internal static string UploadInstructionText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadInstructionText");
      }
    }

    internal static string UploadCompleteText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadCompleteText");
      }
    }

    internal static string UploadStoppedText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadStoppedText");
      }
    }

    internal static string UploadForbiddenText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadForbiddenText");
      }
    }

    internal static string UploadCompleteHeaderText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadCompleteHeaderText");
      }
    }

    internal static string UploadCompleteInfoText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadCompleteInfoText");
      }
    }

    internal static string UploadStoppedHeaderText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadStoppedHeaderText");
      }
    }

    internal static string UploadErrorsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadErrorsText");
      }
    }

    internal static string UploadForbiddenHeaderText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadForbiddenHeaderText");
      }
    }

    internal static string UploadUnauthorizedHeaderText {
      get {
        return Resources.StringsResourceManager.GetString(
            "UploadUnauthorizedHeaderText");
      }
    }

    internal static string FinishText {
      get {
        return Resources.StringsResourceManager.GetString(
            "FinishText");
      }
    }

    internal static string StopWaitingText {
      get {
        return Resources.StringsResourceManager.GetString(
            "StopWaitingText");
      }
    }

    internal static string OpenLogText {
      get {
        return Resources.StringsResourceManager.GetString(
            "OpenLogText");
      }
    }

    internal static string SelectionInfoSSTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SelectionInfoSSTemplateText");
      }
    }

    internal static string SelectionInfoSPTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SelectionInfoSPTemplateText");
      }
    }

    internal static string SelectionInfoPSTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SelectionInfoPSTemplateText");
      }
    }

    internal static string SelectionInfoPPTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "SelectionInfoPPTemplateText");
      }
    }

    internal static string LessThan10MinsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "LessThan10MinsText");
      }
    }

    internal static string LessThan30MinsText {
      get {
        return Resources.StringsResourceManager.GetString(
            "LessThan30MinsText");
      }
    }

    internal static string LessThan1HourText {
      get {
        return Resources.StringsResourceManager.GetString(
            "LessThan1HourText");
      }
    }

    internal static string Between1To3HoursText {
      get {
        return Resources.StringsResourceManager.GetString(
            "Between1To3HoursText");
      }
    }

    internal static string Between3To5HoursText {
      get {
        return Resources.StringsResourceManager.GetString(
            "Between3To5HoursText");
      }
    }

    internal static string Between5To10HoursText {
      get {
        return Resources.StringsResourceManager.GetString(
            "Between5To10HoursText");
      }
    }

    internal static string Between10To15HoursText {
      get {
        return Resources.StringsResourceManager.GetString(
            "Between10To15HoursText");
      }
    }

    internal static string Between15To24HoursText {
      get {
        return Resources.StringsResourceManager.GetString(
            "Between15To24HoursText");
      }
    }

    internal static string MoreThanDayText {
      get {
        return Resources.StringsResourceManager.GetString(
            "MoreThanDayText");
      }
    }

    internal static string PrimaryEmailTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "PrimaryEmailTemplateText");
      }
    }

    internal static string HomeEmailTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "HomeEmailTemplateText");
      }
    }

    internal static string WorkEmailTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "WorkEmailTemplateText");
      }
    }

    internal static string OtherEmailTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "OtherEmailTemplateText");
      }
    }

    internal static string LabelEmailTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "LabelEmailTemplateText");
      }
    }

    internal static string HomePageTemplateText {
      get {
        return Resources.StringsResourceManager.GetString(
            "HomePageTemplateText");
      }
    }

    // Add icons here...

    internal static Icon GMailIcon {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "GMailIcon");
        return (Icon)obj;
      }
    }

    internal static Image GoogleLogoImage {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "GoogleLogoImage");
        return (Image)obj;
      }
    }

    internal static Image GoogleEmailUploaderBackgroundImage {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "GoogleEmailUploaderBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image GoogleEmailUploaderWithCaptchaBackgroundImage {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "GoogleEmailUploaderWithCaptchaBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image SignInBackgroundImage {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "SignInBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image SignInWithCaptchaBackgroundImage {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "SignInWithCaptchaBackgroundImage");
        return (Image)obj;
      }
    }

    internal static Image GoogleEmailUploaderImportCompleteBackgroundImage {
      get {
        object obj = Resources.ImagesResourceManager.GetObject(
            "GoogleEmailUploaderImportCompleteBackgroundImage");
        return (Image)obj;
      }
    }
  }
}
