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
using System.Text;

namespace Google.Thunderbird {
  internal class ThunderbirdConstants {
    internal static string ClientName {
      get {
        return "Thunderbird";
      }
    }


    internal static string ProfilePath {
      get {
        return "Thunderbird\\Profiles";
      }
    }

    internal static string StarDotStar {
      get {
        return "*.*";
      }
    }

    internal static string ParentDir {
      get {
        return "..";
      }
    }

    internal static string CurrentDir {
      get {
        return ".";
      }
    }

    internal static string ThunderbirdMSFExtension {
      get {
        return ".msf";
      }
    }

    internal static string XMozillaStatus {
      get {
        return "X-Mozilla-Status: ";
      }
    }

    internal static string MboxMailStart {
      get {
        return "From - ";
      }
    }

    internal static string StarDotMSF {
      get {
        return "*.msf";
      }
    }

    internal static string XMozillaStatus2 {
      get {
        return "X-Mozilla-Status2: ";
      }
    }

    internal static string MessageIDStart {
      get {
        return "message-id: <";
      }
    }

    internal static string MessageIDEnd {
      get {
        return ">";
      }
    }

    internal static string ThunderbirdDir {
      get {
        return ".sbd";
      }
    }

    internal static int MessageIdLen {
      get {
        return 13;
      }
    }

    internal static int ThunderbirdMSFLen {
      get {
        return 4;
      }
    }

    internal static string Mail {
      get {
        return "Mail";
      }
    }

    internal static string XAccountKey {
      get {
        return "X-Account-Key:";
      }
    }

    internal static string MandatoryFromField {
      get {
        return "From:";
      }
    }

    internal static string MandatoryDateField {
      get {
        return "Date:";
      }
    }

    internal static string CarriageReturn {
      get {
        return "\r\n";
      }
    }

    internal const string Inbox = "Inbox";

    internal const string Sent = "Sent";

    internal const string Drafts = "Drafts";

    internal const string Trash = "Trash";
  }
}