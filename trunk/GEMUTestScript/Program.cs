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

using MCI = Google.MailClientInterfaces;
using GoogleEmailUploader;

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace GoogleEmailUploaderTestScript {
  /// <summary>
  /// Enumeration representing the kind of command.
  /// </summary>
  enum CommandKind {
    // pcs
    PrintClients,
    // c <ident> = <num>
    // Selects numbered client
    Client,
    // pc <ident>
    // Prints all the stores opened by the mail client
    PrintClient,
    // s <ident> = <ident> <num>
    // Selects numbered store from the mail client
    Store,
    // ls <ident> <strlit>
    // Loads store given by file name in given client
    LoadStore,
    // ps <ident>
    // Prints the top level folders of the store associated with ident.
    PrintStore,
    // f <ident> = <ident> <number>
    // Selects numbered folder from either the store or folder associated with
    //  ident.
    Folder,
    // pf <ident>
    // Prints the folder and its subfolder names in the said folder.
    PrintFolder,
    // m <ident> = <ident> <number>
    // Selects numbered message from either the folder associated with ident.
    Mail,
    // pm <ident>
    // Prints the message associated with the ident.
    PrintMail,
    // sm
    SetModel,
    // smm
    SetMockModel,
    // ds [sc, to, br, ua, fb, ie, bg, su, ir]
    DMAPIState,
    // gs [au, to, ba, nv, tna, cr, uk, ad, ads, sd, su, ir]
    GAIAState,
    // hrstart
    HttpRecStart,
    // hrstop
    HttpRecStop,
    // l login password
    Login,
    // lc login password captchatoken captchasolution
    LoginCaptcha,
    // ffl
    FlatFolderList,
    // sel <num>
    Select,
    // usel <num>
    Unselect,
    // stat
    Statistics,
    // pe
    PrintEvents,
    // npe
    NoPrintEvents,
    // uplaod
    Upload,
    // pause
    Pause,
    // resume
    Resume,
    // abort
    Abort,
    // Lay on beach and do nothing.
    Empty,
    // Display help
    Help,
    // q
    // Duh.
    Quit,
  }

  /// <summary>
  /// Class representing the actual command.
  /// </summary>
  class Command {
    internal static readonly string[] CommandUsage = new string[] {
      "pcs                              "
          + "// Prints all the clients available",
      "c <ident> = <num>                "
          + "// Selects numbered client",
      "pc <ident>                       "
          + "// Prints all te stores opened by the mail client",
      "s <ident> = <ident> <num>        "
          + "// Selects numbered store in given client",
      "ls <ident> <strlit>              "
          + "// Loads store given by file name in given client",
      "ps <ident>                       "
          + "// Print the top level folders of the given store",
      "f <ident> = <ident> <number>     "
          + "// Selects numbered folder from the given store or folder",
      "pf <ident>                       "
          + "// Prints the folder and the name of subfolders",
      "m <ident> = <ident> <number>     "
          + "// Selects numbered mail from the given store or folder",
      "pm <ident>                       "
          + "// Prints the mail",
      "sm                               "
          + "// Set model",
      "smm                               "
          + "// Set mock model",
      "ds [sc, to, br, ua, fb, ie, bg, su]                "
          + "// Sets the state of dmapi mock server",
      "gs [au, to, ba, nv, tna, cr, uk, ad, ads, sd, su]  "
          + "// Sets the state of gaia mock server",
      "hrstart                          "
          + "// Start recording http requests",
      "hrstop                           "
          + "// Stop recording http requests",
      "l login password                 "
          + "// login ",
      "lc login password captchatoken captchasolution     "
          + "// login captcha",
      "ffl                              "
          + "// print flat folder list",
      "sel <num>                        "
          + "// select a folder",
      "usel <num>                       "
          + "// unselect a folder",
      "stats                            "
          + "// prints statistics",
      "pe                               "
          + "// Print events",
      "npe                              "
          + "// No print events",
      "upload                           "
          + "// Upload",
      "pause                            "
          + "// Pause",
      "resume                           "
          + "// Resume",
      "abort                            "
          + "// Abort",
      "h                                "
          + "// Display help",
      "q                                "
          + "// Quit",
    };
    internal readonly CommandKind Kind;
    internal readonly Token Target;
    internal readonly Token Source1;
    internal readonly Token Source2;
    internal readonly Token Source3;
    internal readonly Token Source4;

    internal Command(CommandKind kind,
                     Token target,
                     Token source1,
                     Token source2) {
      this.Kind = kind;
      this.Target = target;
      this.Source1 = source1;
      this.Source2 = source2;
    }

    internal Command(CommandKind kind,
                     Token target,
                     Token source1,
                     Token source2,
                     Token source3,
                     Token source4) {
      this.Kind = kind;
      this.Target = target;
      this.Source1 = source1;
      this.Source2 = source2;
      this.Source3 = source3;
      this.Source4 = source4;
    }
  }

  /// <summary>
  /// Enumeration representing the kind of lexical token.
  /// </summary>
  enum TokenKind {
    PrintClients,
    Client,
    PrintClient,
    Store,
    LoadStore,
    PrintStore,
    Folder,
    PrintFolder,
    Mail,
    PrintMail,
    SetModel,
    SetMockModel,
    DMAPIState,
    GAIAState,
    HttpRecStart,
    HttpRecStop,
    Login,
    LoginCaptcha,
    FlatFolderList,
    Select,
    Unselect,
    Statistics,
    PrintEvents,
    NoPrintEvents,
    Upload,
    Pause,
    Resume,
    Abort,
    Help,
    Quit,
    // non command tokens.
    Success,
    Timeout,
    BadRequest,
    Unauthorized,
    Forbidden,
    InternalError,
    BadGateway,
    ServiceUnavailable,
    Authenticate,
    BadAuthentication,
    NotVerified,
    TermsNotAgreed,
    CaptchaRequired,
    Unknown,
    AccountDeleted,
    AccountDisabled,
    ServiceDisabled,
    IncorrectResponse,
    Identifier,
    UInt,
    StringLiteral,
    Equals,
    Error,
    EOL,
  }

  /// <summary>
  /// Class representing the lexical token. This is used in parsing
  /// the command.
  /// </summary>
  class Token {
    public readonly TokenKind TokenKind;
    public readonly int StartIndex;
    public readonly string StringValue;
    public readonly uint UIntValue;

    public Token(TokenKind tokenKind,
                 int startIndex,
                 string stringValue,
                 uint uintValue) {
      this.TokenKind = tokenKind;
      this.StartIndex = startIndex;
      this.StringValue = stringValue;
      this.UIntValue = uintValue;
    }
  }

  /// <summary>
  /// The class that actually does the parsing and running of commands.
  /// </summary>
  class Program {
    static HttpFactory HttpFactory;
    static GoogleEmailUploaderModel GoogleEmailUploaderModel;
    static TextWriter OutputTextStream;
    static TextReader InputTextStream;
    static Hashtable IdentToObjectMap;
    static bool PrintEvents;

    static Token GetNextToken(string str, ref int index) {
      while (index < str.Length && char.IsWhiteSpace(str, index)) {
        index++;
      }
      if (index >= str.Length) {
        return new Token(TokenKind.EOL,
                         index,
                         null,
                         0);
      }
      int startIndex = index;
      if (char.IsDigit(str, index)) {
        while (index < str.Length && char.IsDigit(str, index)) {
          index++;
        }
        uint value = uint.Parse(str.Substring(startIndex,
                                              index - startIndex));
        return new Token(TokenKind.UInt,
                         startIndex,
                         null,
                         value);
      } else if (str[index] == '"') {
        index++;
        while (index < str.Length && str[index] != '"') {
          index++;
        }
        string value = str.Substring(startIndex + 1,
                                     index - startIndex - 1);
        if (index < str.Length) {
          index++;
        }
        return new Token(TokenKind.StringLiteral,
                         startIndex,
                         value,
                         0);
      } else if (char.IsLetter(str, index)) {
        while (index < str.Length &&
          (char.IsLetterOrDigit(str, index) ||
            str[index] == '_')) {
          index++;
        }
        string value = str.Substring(startIndex,
                                     index - startIndex).ToLower();
        TokenKind tokenKind;
        switch (value) {
          case "pcs":
            tokenKind = TokenKind.PrintClients;
            break;
          case "c":
            tokenKind = TokenKind.Client;
            break;
          case "pc":
            tokenKind = TokenKind.PrintClient;
            break;
          case "s":
            tokenKind = TokenKind.Store;
            break;
          case "ls":
            tokenKind = TokenKind.LoadStore;
            break;
          case "ps":
            tokenKind = TokenKind.PrintStore;
            break;
          case "f":
            tokenKind = TokenKind.Folder;
            break;
          case "pf":
            tokenKind = TokenKind.PrintFolder;
            break;
          case "m":
            tokenKind = TokenKind.Mail;
            break;
          case "pm":
            tokenKind = TokenKind.PrintMail;
            break;
          case "sm":
            tokenKind = TokenKind.SetModel;
            break;
          case "smm":
            tokenKind = TokenKind.SetMockModel;
            break;
          case "ds":
            tokenKind = TokenKind.DMAPIState;
            break;
          case "gs":
            tokenKind = TokenKind.GAIAState;
            break;
          case "hrstart":
            tokenKind = TokenKind.HttpRecStart;
            break;
          case "hrstop":
            tokenKind = TokenKind.HttpRecStop;
            break;
          case "l":
            tokenKind = TokenKind.Login;
            break;
          case "lc":
            tokenKind = TokenKind.LoginCaptcha;
            break;
          case "ffl":
            tokenKind = TokenKind.FlatFolderList;
            break;
          case "sel":
            tokenKind = TokenKind.Select;
            break;
          case "usel":
            tokenKind = TokenKind.Unselect;
            break;
          case "upload":
            tokenKind = TokenKind.Upload;
            break;
          case "pause":
            tokenKind = TokenKind.Pause;
            break;
          case "resume":
            tokenKind = TokenKind.Resume;
            break;
          case "abort":
            tokenKind = TokenKind.Abort;
            break;
          case "sc":
            tokenKind = TokenKind.Success;
            break;
          case "to":
            tokenKind = TokenKind.Timeout;
            break;
          case "br":
            tokenKind = TokenKind.BadRequest;
            break;
          case "ua":
            tokenKind = TokenKind.Unauthorized;
            break;
          case "fb":
            tokenKind = TokenKind.Forbidden;
            break;
          case "ie":
            tokenKind = TokenKind.InternalError;
            break;
          case "bg":
            tokenKind = TokenKind.BadGateway;
            break;
          case "su":
            tokenKind = TokenKind.ServiceUnavailable;
            break;
          case "au":
            tokenKind = TokenKind.Authenticate;
            break;
          case "ba":
            tokenKind = TokenKind.BadAuthentication;
            break;
          case "nv":
            tokenKind = TokenKind.NotVerified;
            break;
          case "tna":
            tokenKind = TokenKind.TermsNotAgreed;
            break;
          case "cr":
            tokenKind = TokenKind.CaptchaRequired;
            break;
          case "uk":
            tokenKind = TokenKind.Unknown;
            break;
          case "ad":
            tokenKind = TokenKind.AccountDeleted;
            break;
          case "ads":
            tokenKind = TokenKind.AccountDisabled;
            break;
          case "sd":
            tokenKind = TokenKind.ServiceDisabled;
            break;
          case "ir":
            tokenKind = TokenKind.IncorrectResponse;
            break;
          case "stats":
            tokenKind = TokenKind.Statistics;
            break;
          case "pe":
            tokenKind = TokenKind.PrintEvents;
            break;
          case "npe":
            tokenKind = TokenKind.NoPrintEvents;
            break;
          case "q":
            tokenKind = TokenKind.Quit;
            break;
          case "h":
            tokenKind = TokenKind.Help;
            break;
          case "help":
            tokenKind = TokenKind.Help;
            break;
          default:
            tokenKind = TokenKind.Identifier;
            break;
        }
        return new Token(tokenKind,
                         startIndex,
                         value,
                         0);
      } if (str[index] == '=') {
        index++;
        return new Token(TokenKind.Equals,
                         startIndex,
                         null,
                         0);
      } else {
        index++;
        return new Token(TokenKind.Error,
                         startIndex,
                         null,
                         0);
      }
    }

    static Token[] GetTokens(string str) {
      int strIndex = 0;
      int arrayIndex = 0;
      Token[] tokenArray = new Token [16];
      while (true) {
        Token token = Program.GetNextToken(str,
                                           ref strIndex);
        tokenArray[arrayIndex++] = token;
        if (token.TokenKind == TokenKind.EOL) {
          break;
        }
      }
      return tokenArray;
    }

    static void AddErrorAtToken(Token token,
                                string errorMessage) {
      string blankStr = new string(' ',
                                   token.StartIndex + 9);
      Program.OutputTextStream.Write(blankStr);
      Program.OutputTextStream.Write("^ ");
      Program.OutputTextStream.WriteLine(errorMessage);
    }

    static Command GetCommand() {
      Program.OutputTextStream.Write("Yes Boss>");
      string str = Program.InputTextStream.ReadLine().Trim();
      Token[] tokenArray = GetTokens(str);
      switch (tokenArray[0].TokenKind) {
        case TokenKind.PrintClients: {
            if (tokenArray[1].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.PrintClients]);
              break;
            }
            return new Command(CommandKind.PrintClients,
                               null,
                               null,
                               null);
          }
        case TokenKind.Client: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.Equals ||
                tokenArray[3].TokenKind != TokenKind.UInt ||
                tokenArray[4].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Client]);
              break;
            }
            return new Command(CommandKind.Client,
                               tokenArray[1],
                               tokenArray[3],
                               null);
          }
        case TokenKind.PrintClient: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.PrintClient]);
              break;
            }
            return new Command(CommandKind.PrintClient,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.Store: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.Equals ||
                tokenArray[3].TokenKind != TokenKind.Identifier ||
                tokenArray[4].TokenKind != TokenKind.UInt ||
                tokenArray[5].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Store]);
              break;
            }
            return new Command(CommandKind.Store,
                               tokenArray[1],
                               tokenArray[3],
                               tokenArray[4]);
          }
        case TokenKind.LoadStore: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.StringLiteral ||
                tokenArray[3].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.LoadStore]);
              break;
            }
            return new Command(CommandKind.LoadStore,
                               null,
                               tokenArray[1],
                               tokenArray[2]);
          }
        case TokenKind.PrintStore: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.PrintStore]);
              break;
            }
            return new Command(CommandKind.PrintStore,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.Folder: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.Equals ||
                tokenArray[3].TokenKind != TokenKind.Identifier ||
                tokenArray[4].TokenKind != TokenKind.UInt ||
                tokenArray[5].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Folder]);
              break;
            }
            return new Command(CommandKind.Folder,
                               tokenArray[1],
                               tokenArray[3],
                               tokenArray[4]);
          }
        case TokenKind.PrintFolder: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.PrintFolder]);
              break;
            }
            return new Command(CommandKind.PrintFolder,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.Mail: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.Equals ||
                tokenArray[3].TokenKind != TokenKind.Identifier ||
                tokenArray[4].TokenKind != TokenKind.UInt ||
                tokenArray[5].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Mail]);
              break;
            }
            return new Command(CommandKind.Mail,
                               tokenArray[1],
                               tokenArray[3],
                               tokenArray[4]);
          }
        case TokenKind.PrintMail: {
            if (tokenArray[1].TokenKind != TokenKind.Identifier ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.PrintMail]);
              break;
            }
            return new Command(CommandKind.PrintMail,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.SetModel: {
            if (tokenArray[1].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.SetModel]);
              break;
            }
            return new Command(CommandKind.SetModel,
                               null,
                               null,
                               null);
          }
        case TokenKind.SetMockModel: {
            if (tokenArray[1].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.SetMockModel]);
              break;
            }
            return new Command(CommandKind.SetMockModel,
                               null,
                               null,
                               null);
          }
        case TokenKind.DMAPIState: {
            if (!(tokenArray[1].TokenKind == TokenKind.Success ||
                tokenArray[1].TokenKind == TokenKind.Timeout ||
                tokenArray[1].TokenKind == TokenKind.BadRequest ||
                tokenArray[1].TokenKind == TokenKind.Unauthorized ||
                tokenArray[1].TokenKind == TokenKind.Forbidden ||
                tokenArray[1].TokenKind == TokenKind.InternalError ||
                tokenArray[1].TokenKind == TokenKind.BadGateway ||
                tokenArray[1].TokenKind == TokenKind.ServiceUnavailable ||
                tokenArray[1].TokenKind == TokenKind.IncorrectResponse) ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.DMAPIState]);
              break;
            }
            return new Command(CommandKind.DMAPIState,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.GAIAState: {
            if (!(tokenArray[1].TokenKind == TokenKind.Authenticate ||
                tokenArray[1].TokenKind == TokenKind.Timeout ||
                tokenArray[1].TokenKind == TokenKind.BadAuthentication ||
                tokenArray[1].TokenKind == TokenKind.NotVerified ||
                tokenArray[1].TokenKind == TokenKind.TermsNotAgreed ||
                tokenArray[1].TokenKind == TokenKind.CaptchaRequired ||
                tokenArray[1].TokenKind == TokenKind.Unknown ||
                tokenArray[1].TokenKind == TokenKind.AccountDeleted ||
                tokenArray[1].TokenKind == TokenKind.AccountDisabled ||
                tokenArray[1].TokenKind == TokenKind.ServiceDisabled ||
                tokenArray[1].TokenKind == TokenKind.ServiceUnavailable ||
                tokenArray[1].TokenKind == TokenKind.IncorrectResponse) ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.GAIAState]);
              break;
            }
            return new Command(CommandKind.GAIAState,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.HttpRecStart: {
            if (tokenArray[1].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.HttpRecStart]);
              break;
            }
            return new Command(CommandKind.HttpRecStart,
                               null,
                               null,
                               null);
          }
        case TokenKind.HttpRecStop: {
            if (tokenArray[1].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.HttpRecStop]);
              break;
            }
            return new Command(CommandKind.HttpRecStop,
                               null,
                               null,
                               null);
          }
        case TokenKind.Login: {
            if (tokenArray[1].TokenKind != TokenKind.StringLiteral ||
                tokenArray[2].TokenKind != TokenKind.StringLiteral ||
                tokenArray[3].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Login]);
              break;
            }
            return new Command(CommandKind.Login,
                               null,
                               tokenArray[1],
                               tokenArray[2]);
          }
        case TokenKind.LoginCaptcha: {
            if (tokenArray[1].TokenKind != TokenKind.StringLiteral ||
                tokenArray[2].TokenKind != TokenKind.StringLiteral ||
                tokenArray[3].TokenKind != TokenKind.StringLiteral ||
                tokenArray[4].TokenKind != TokenKind.StringLiteral ||
                tokenArray[5].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.LoginCaptcha]);
              break;
            }
            return new Command(CommandKind.LoginCaptcha,
                               null,
                               tokenArray[1],
                               tokenArray[2],
                               tokenArray[3],
                               tokenArray[4]);
          }
        case TokenKind.FlatFolderList:
          if (tokenArray[1].TokenKind != TokenKind.EOL) {
            Program.AddErrorAtToken(
                tokenArray[0],
                Command.CommandUsage[(int)TokenKind.FlatFolderList]);
            break;
          }
          return new Command(CommandKind.FlatFolderList,
                             null,
                             null,
                             null);
        case TokenKind.Select: {
            if (tokenArray[1].TokenKind != TokenKind.UInt ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Select]);
              break;
            }
            return new Command(CommandKind.Select,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.Unselect: {
            if (tokenArray[1].TokenKind != TokenKind.UInt ||
                tokenArray[2].TokenKind != TokenKind.EOL) {
              Program.AddErrorAtToken(
                  tokenArray[0],
                  Command.CommandUsage[(int)TokenKind.Unselect]);
              break;
            }
            return new Command(CommandKind.Unselect,
                               null,
                               tokenArray[1],
                               null);
          }
        case TokenKind.Statistics:
          if (tokenArray[1].TokenKind != TokenKind.EOL) {
            Program.AddErrorAtToken(
                tokenArray[0],
                Command.CommandUsage[(int)TokenKind.Statistics]);
            break;
          }
          return new Command(CommandKind.Statistics,
                             null,
                             null,
                             null);
        case TokenKind.PrintEvents:
          if (tokenArray[1].TokenKind != TokenKind.EOL) {
            Program.AddErrorAtToken(
                tokenArray[0],
                Command.CommandUsage[(int)TokenKind.PrintEvents]);
            break;
          }
          return new Command(CommandKind.PrintEvents,
                             null,
                             null,
                             null);
        case TokenKind.NoPrintEvents:
          if (tokenArray[1].TokenKind != TokenKind.EOL) {
            Program.AddErrorAtToken(
                tokenArray[0],
                Command.CommandUsage[(int)TokenKind.NoPrintEvents]);
            break;
          }
          return new Command(CommandKind.NoPrintEvents,
                             null,
                             null,
                             null);
        case TokenKind.Upload:
          return new Command(CommandKind.Upload,
                             null,
                             null,
                             null);
        case TokenKind.Pause:
          return new Command(CommandKind.Pause,
                             null,
                             null,
                             null);
        case TokenKind.Resume:
          return new Command(CommandKind.Resume,
                             null,
                             null,
                             null);
        case TokenKind.Abort:
          return new Command(CommandKind.Abort,
                             null,
                             null,
                             null);
        case TokenKind.Help:
          return new Command(CommandKind.Help,
                             null,
                             null,
                             null);
        case TokenKind.Quit:
          return new Command(CommandKind.Quit,
                             null,
                             null,
                             null);
        case TokenKind.Identifier:
        case TokenKind.UInt:
        case TokenKind.Error:
          Program.AddErrorAtToken(tokenArray[0],
                                  "I am confused. What did u say?");
          break;
        case TokenKind.EOL:
          break;
      }
      return new Command(CommandKind.Empty,
                         null,
                         null,
                         null);
    }

    static void ExecuteCommand(Command command) {
      switch (command.Kind) {
        case CommandKind.PrintClients: {
            int index = 0;
            foreach (ClientModel clientModel in
                Program.GoogleEmailUploaderModel.ClientModels) {
              Program.OutputTextStream.WriteLine("  {0, 5} : {1}",
                                                 index,
                                                 clientModel.DisplayName);
              index++;
            }
            break;
          }
        case CommandKind.Client: {
            ClientModel foundClientModel = null;
            uint index = command.Source1.UIntValue;
            foreach (ClientModel clientModel in
                Program.GoogleEmailUploaderModel.ClientModels) {
              if (index == 0) {
                foundClientModel = clientModel;
                break;
              }
              index--;
            }
            if (foundClientModel == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find client with given index");
            } else {
              Program.IdentToObjectMap[command.Target.StringValue] =
                  foundClientModel;
            }
            break;
          }
        case CommandKind.PrintClient: {
            ClientModel clientModel =
                Program.IdentToObjectMap[command.Source1.StringValue]
                    as ClientModel;
            int index = 0;
            foreach (StoreModel storeModel in clientModel.Children) {
              Program.OutputTextStream.WriteLine("  {0, 5} : {1}",
                                                 index,
                                                 storeModel.DisplayName);
              index++;
            }
            break;
          }
        case CommandKind.Store: {
            ClientModel clientModel =
                Program.IdentToObjectMap[command.Source1.StringValue]
                    as ClientModel;
            StoreModel foundStoreModel = null;
            uint index = command.Source2.UIntValue;
            foreach (StoreModel storeModel in clientModel.Children) {
              if (index == 0) {
                foundStoreModel = storeModel;
                break;
              }
              index--;
            }
            if (foundStoreModel == null) {
              Program.AddErrorAtToken(
                  command.Source2,
                  "Could not find store with given index");
            } else {
              Program.IdentToObjectMap[command.Target.StringValue] =
                  foundStoreModel;
            }
            break;
          }
        case CommandKind.LoadStore: {
            ClientModel clientModel =
                Program.IdentToObjectMap[command.Source1.StringValue]
                    as ClientModel;
            string filename = command.Source2.StringValue;
            StoreModel openedStore = clientModel.OpenStore(filename);
            if (openedStore == null) {
              Program.AddErrorAtToken(
                  command.Source2,
                  "Could not open store");
            }
            break;
          }
        case CommandKind.PrintStore: {
            StoreModel storeModel =
                Program.IdentToObjectMap[command.Source1.StringValue]
                    as StoreModel;
            if (storeModel == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find store associated with given identifier");
              break;
            }
            Program.OutputTextStream.WriteLine(
                "Name : {0}", storeModel.DisplayName);
            Program.OutputTextStream.WriteLine("Persist Name : {0}",
                                               storeModel.Store.PersistName);
            Program.PrintFolderList("Folders:",
                                    storeModel.Children);
            break;
          }
        case CommandKind.Folder: {
            StoreModel storeModel =
              Program.IdentToObjectMap[command.Source1.StringValue]
                  as StoreModel;
            FolderModel folderModel =
              Program.IdentToObjectMap[command.Source1.StringValue]
                  as FolderModel;
            IEnumerable folders = null;
            if (storeModel != null) {
              folders = storeModel.Children;
            } else if (folderModel != null) {
              folders = folderModel.Children;
            } else {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find client store or folder associated with"
                      + "given identifier");
              break;
            }
            FolderModel foundFolderModel = null;
            uint index = command.Source2.UIntValue;
            foreach (FolderModel folderModelIter in folders) {
              if (index == 0) {
                foundFolderModel = folderModelIter;
                break;
              }
              index--;
            }
            if (foundFolderModel == null) {
              Program.AddErrorAtToken(
                  command.Source2,
                  "Could not find folder with given index");
            } else {
              Program.IdentToObjectMap[command.Target.StringValue] =
                  foundFolderModel;
            }
            break;
          }
        case CommandKind.PrintFolder: {
            FolderModel folderModel =
                Program.IdentToObjectMap[command.Source1.StringValue]
                  as FolderModel;
            if (folderModel == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find folder associated with given identifier");
            }
            Program.OutputTextStream.WriteLine(
                "Name : {0} Kind: {1} ParentFolder: {2}"
                    + "Store: {3} MailCount: {4}",
                folderModel.DisplayName,
                folderModel.Folder.Kind,
                folderModel.Parent != null
                    ? folderModel.Parent.DisplayName
                    : null,
                folderModel.StoreModel.DisplayName,
                folderModel.Folder.MailCount);
            Program.PrintFolderList("Sub folders:",
                                    folderModel.Children);
            break;
          }
        case CommandKind.Mail: {
            FolderModel folderModel =
                Program.IdentToObjectMap[command.Source1.StringValue]
                    as FolderModel;
            if (folderModel == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find folder associated with given identifier");
              break;
            }
            MCI.IMail foundMail = null;
            uint index = command.Source2.UIntValue;
            foreach (MCI.IMail mail in folderModel.Folder.Mails) {
              if (index == 0) {
                foundMail = mail;
                break;
              }
              index--;
            }
            if (foundMail == null) {
              Program.AddErrorAtToken(
                  command.Source2,
                  "Could not find message with given index");
            } else {
              Program.IdentToObjectMap[command.Target.StringValue] = foundMail;
            }
            break;
          }
        case CommandKind.PrintMail: {
            MCI.IMail mail =
                Program.IdentToObjectMap[command.Source1.StringValue]
                    as MCI.IMail;
            if (mail == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find mail associated with given identifier");
              break;
            } else {
              Program.PrintMail(mail);
            }
            break;
          }
        case CommandKind.SetModel: {
            Program.GoogleEmailUploaderModel.Dispose();
            Program.GoogleEmailUploaderModel = new GoogleEmailUploaderModel();
            break;
          }
        case CommandKind.SetMockModel: {
            Program.GoogleEmailUploaderModel.Dispose();
            Program.GoogleEmailUploaderModel = new GoogleEmailUploaderModel(
                Program.HttpFactory);
            break;
          }
        case CommandKind.DMAPIState: {
            if (command.Source1.TokenKind == TokenKind.Success) {
              Program.HttpFactory.DMAPIState = DMAPIState.Success;
            } else if (command.Source1.TokenKind == TokenKind.Timeout) {
              Program.HttpFactory.DMAPIState = DMAPIState.Timeout;
            } else if (command.Source1.TokenKind == TokenKind.BadRequest) {
              Program.HttpFactory.DMAPIState = DMAPIState.BadRequest;
            } else if (command.Source1.TokenKind == TokenKind.Unauthorized) {
              Program.HttpFactory.DMAPIState = DMAPIState.Unauthorized;
            } else if (command.Source1.TokenKind == TokenKind.Forbidden) {
              Program.HttpFactory.DMAPIState = DMAPIState.Forbidden;
            } else if (command.Source1.TokenKind == TokenKind.InternalError) {
              Program.HttpFactory.DMAPIState = DMAPIState.InternalError;
            } else if (command.Source1.TokenKind == TokenKind.BadGateway) {
              Program.HttpFactory.DMAPIState = DMAPIState.BadGateway;
            } else if (command.Source1.TokenKind ==
                TokenKind.ServiceUnavailable) {
              Program.HttpFactory.DMAPIState = DMAPIState.ServiceUnavailable;
            } else if (command.Source1.TokenKind ==
                TokenKind.IncorrectResponse) {
              Program.HttpFactory.DMAPIState = DMAPIState.IncorrectResponse;
            }
            break;
          }
        case CommandKind.GAIAState: {
            if (command.Source1.TokenKind == TokenKind.Authenticate) {
              Program.HttpFactory.GAIAState = GAIAState.Authenticate;
            } else if (command.Source1.TokenKind == TokenKind.Timeout) {
              Program.HttpFactory.GAIAState = GAIAState.Timeout;
            } else if (command.Source1.TokenKind ==
                TokenKind.BadAuthentication) {
              Program.HttpFactory.GAIAState = GAIAState.BadAuthentication;
            } else if (command.Source1.TokenKind == TokenKind.NotVerified) {
              Program.HttpFactory.GAIAState = GAIAState.NotVerified;
            } else if (command.Source1.TokenKind == TokenKind.TermsNotAgreed) {
              Program.HttpFactory.GAIAState = GAIAState.TermsNotAgreed;
            } else if (command.Source1.TokenKind == TokenKind.CaptchaRequired) {
              Program.HttpFactory.GAIAState = GAIAState.CaptchaRequired;
            } else if (command.Source1.TokenKind == TokenKind.Unknown) {
              Program.HttpFactory.GAIAState = GAIAState.Unknown;
            } else if (command.Source1.TokenKind == TokenKind.AccountDeleted) {
              Program.HttpFactory.GAIAState = GAIAState.AccountDeleted;
            } else if (command.Source1.TokenKind == TokenKind.AccountDisabled) {
              Program.HttpFactory.GAIAState = GAIAState.AccountDisabled;
            } else if (command.Source1.TokenKind == TokenKind.ServiceDisabled) {
              Program.HttpFactory.GAIAState = GAIAState.ServiceDisabled;
            } else if (command.Source1.TokenKind ==
                TokenKind.ServiceUnavailable) {
              Program.HttpFactory.GAIAState = GAIAState.ServiceUnavailable;
            } else if (command.Source1.TokenKind ==
                TokenKind.IncorrectResponse) {
              Program.HttpFactory.GAIAState = GAIAState.IncorrectResponse;
            }
            break;
          }
        case CommandKind.HttpRecStart: {
            Program.HttpFactory.StartRecording(Program.OutputTextStream);
            break;
          }
        case CommandKind.HttpRecStop: {
            Program.HttpFactory.StopRecording();
            break;
          }
        case CommandKind.Login: {
            AuthenticationResponse aResponse =
                Program.GoogleEmailUploaderModel.SignIn(
                    command.Source1.StringValue,
                    command.Source2.StringValue);
            Program.PrintAuthenticationResponse(aResponse);
            break;
          }
        case CommandKind.LoginCaptcha: {
            AuthenticationResponse aResponse =
                Program.GoogleEmailUploaderModel.SignInCAPTCHA(
                    command.Source1.StringValue,
                    command.Source2.StringValue,
                    command.Source3.StringValue,
                    command.Source4.StringValue);
            Program.PrintAuthenticationResponse(aResponse);
            break;
          }
        case CommandKind.FlatFolderList: {
            int index = 0;
            foreach(FolderModel folderModel in
                Program.GoogleEmailUploaderModel.FlatFolderModelList){
              Program.OutputTextStream.WriteLine("  {0, 5} : {1} {2}",
                                                 index,
                                                 folderModel.IsSelected
                                                     ? "*"
                                                     : " ",
                                                 folderModel.FullFolderPath);
              index++;
            }
            break;
          }
        case CommandKind.Select: {
            FolderModel foundFolderModel = null;
            uint index = command.Source1.UIntValue;
            foreach (FolderModel folderModel in
                Program.GoogleEmailUploaderModel.FlatFolderModelList) {
              if (index == 0) {
                foundFolderModel = folderModel;
                break;
              }
              index--;
            }
            if (foundFolderModel == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find folder with given index");
            } else {
              foundFolderModel.IsSelected = true;
              Program.GoogleEmailUploaderModel.ComputeMailCounts();
            }
            break;
          }
        case CommandKind.Unselect: {
            FolderModel foundFolderModel = null;
            uint index = command.Source1.UIntValue;
            foreach (FolderModel folderModel in
                Program.GoogleEmailUploaderModel.FlatFolderModelList) {
              if (index == 0) {
                foundFolderModel = folderModel;
                break;
              }
              index--;
            }
            if (foundFolderModel == null) {
              Program.AddErrorAtToken(
                  command.Source1,
                  "Could not find folder with given index");
            } else {
              foundFolderModel.IsSelected = false;
              Program.GoogleEmailUploaderModel.ComputeMailCounts();
            }
            break;
          }
        case CommandKind.Statistics: {
            Program.OutputTextStream.WriteLine(
                "  State: " + Program.GoogleEmailUploaderModel.ModelState);
            Program.OutputTextStream.WriteLine(
                "  FailedMails: " +
                    Program.GoogleEmailUploaderModel.FailedMailCount);
            Program.OutputTextStream.WriteLine(
                "  ConsideredMails: " +
                    Program.GoogleEmailUploaderModel.ConsideredMailCount);
            Program.OutputTextStream.WriteLine(
                "  SelectedMails: " +
                    Program.GoogleEmailUploaderModel.SelectedMailCount);
            Program.OutputTextStream.WriteLine(
                "  UploadedMails: " +
                    Program.GoogleEmailUploaderModel.UploadedMailCount);
            if (Program.GoogleEmailUploaderModel.CurrentFolderModel != null) {
              Program.OutputTextStream.WriteLine(
                  "  Current Folder: " +
                      Program.GoogleEmailUploaderModel.
                          CurrentFolderModel.FullFolderPath);
            }
            break;
          }
        case CommandKind.PrintEvents: {
            Program.PrintEvents = true;
            break;
          }
        case CommandKind.NoPrintEvents: {
            Program.PrintEvents = false;
            break;
          }
        case CommandKind.Upload: {
            Program.HookModelEvents();
            Program.GoogleEmailUploaderModel.StartUpload();
            break;
          }
        case CommandKind.Pause: {
            Program.GoogleEmailUploaderModel.PauseUpload();
            break;
          }
        case CommandKind.Resume: {
            Program.GoogleEmailUploaderModel.ResumeUpload();
            break;
          }
        case CommandKind.Abort: {
            Program.GoogleEmailUploaderModel.AbortUpload();
            break;
          }
        case CommandKind.Help: {
            for (int i = 0; i < Command.CommandUsage.Length; ++i) {
              Program.OutputTextStream.WriteLine(Command.CommandUsage[i]);
            }
            break;
          }
      }
    }

    private static void PrintMail(MCI.IMail mail) {
      Program.OutputTextStream.Write("  Folder: {0} MailId: {1}",
                                     mail.Folder.Name,
                                     mail.MailId);
      if (mail.IsRead) {
        Program.OutputTextStream.Write(" isRead");
      }
      if (mail.IsStarred) {
        Program.OutputTextStream.Write(" isStared");
      }
      Program.OutputTextStream.WriteLine(" {0}",
                                         mail.MessageSize);
      byte[] rfcBuffer = mail.Rfc822Buffer;
      if (rfcBuffer.Length == 0) {
        Program.OutputTextStream.WriteLine("Empty Mail");
      } else {
        for (int i = 0; i < rfcBuffer.Length; ++i) {
          Program.OutputTextStream.Write((char)rfcBuffer[i]);
        }
        Program.OutputTextStream.WriteLine();
      }
    }

    static void PrintFolderList(string heading,
                                IEnumerable folders) {
      Program.OutputTextStream.WriteLine(heading);
      uint index = 0;
      foreach (FolderModel folder in folders) {
        Program.OutputTextStream.WriteLine("  {0, 5} : {1}",
                                           index,
                                           folder.DisplayName);
        index++;
      }
    }

    static void PrintAuthenticationResponse(AuthenticationResponse aResponse) {
      Program.OutputTextStream.WriteLine("Auth Result: {0}",
                                         aResponse.AuthenticationResult);
      Program.OutputTextStream.WriteLine("Url: {0}",
                                         aResponse.Url);
      Program.OutputTextStream.WriteLine("CAPTCHAUrl: {0}",
                                         aResponse.CAPTCHAUrl);
      Program.OutputTextStream.WriteLine("CAPTCHAToken: {0}",
                                         aResponse.CAPTCHAToken);
      Program.OutputTextStream.WriteLine("AuthToken: {0}",
                                         aResponse.AuthToken);
      Program.OutputTextStream.WriteLine("SIDToken: {0}",
                                         aResponse.SIDToken);
      Program.OutputTextStream.WriteLine("LSIDToken: {0}",
                                         aResponse.LSIDToken);
      if (aResponse.HttpException != null) {
        Program.OutputTextStream.WriteLine("HttpException: {0}",
                                           aResponse.HttpException.Message);
      }
    }

    static void PrintUsage() {
      Program.OutputTextStream.WriteLine(
          "GoogleEmailUploaderTestScript -i:<input file name> "
              + "-o:<output file name>");
    }

    static bool ProcessArgs(string[] args) {
      for (int i = 0; i < args.Length; ++i) {
        if (args[i].StartsWith("-i:")) {
          if (Program.InputTextStream != null) {
            Program.PrintUsage();
            return false;
          }
          Program.InputTextStream = File.OpenText(args[i].Substring(3));
        } else if (args[i].StartsWith("-o:")) {
          if (Program.OutputTextStream != null) {
            Program.PrintUsage();
            return false;
          }
          Stream stream = File.OpenWrite(args[i].Substring(3));
          Program.OutputTextStream = new StreamWriter(stream);
        }
      }
      if (Program.InputTextStream == null) {
        Program.InputTextStream = Console.In;
      }
      if (Program.OutputTextStream == null) {
        Program.OutputTextStream = Console.Out;
      }
      return true;
    }

    static void HookModelEvents() {
      Program.GoogleEmailUploaderModel.MailBatchFillingEvent +=
          new MailDelegate(
              Program.googleEmailUploaderModel_MailBatchFillingEvent);
      Program.GoogleEmailUploaderModel.MailBatchFilledEvent +=
          new MailBatchDelegate(
              Program.googleEmailUploaderModel_MailBatchFilledEvent);
      Program.GoogleEmailUploaderModel.MailBatchUploadTryStartEvent +=
          new MailBatchDelegate(
              Program.googleEmailUploaderModel_MailBatchUploadTryStartEvent);
      Program.GoogleEmailUploaderModel.UploadPausedEvent +=
          new UploadPausedDelegate(
              Program.googleEmailUploaderModel_UploadPausedEvent);
      Program.GoogleEmailUploaderModel.PauseCountDownEvent +=
          new PauseCountDownDelegate(
              Program.googleEmailUploaderModel_PauseCountDownEvent);
      Program.GoogleEmailUploaderModel.MailBatchUploadedEvent +=
          new MailBatchDelegate(
              Program.googleEmailUploaderModel_MailBatchUploadedEvent);
      Program.GoogleEmailUploaderModel.UploadDoneEvent +=
          new UploadDoneDelegate(
              Program.googleEmailUploaderModel_UploadDoneEvent);
    }

    static void UnhookModelEvents() {
      Program.GoogleEmailUploaderModel.MailBatchFillingEvent -=
          new MailDelegate(
              Program.googleEmailUploaderModel_MailBatchFillingEvent);
      Program.GoogleEmailUploaderModel.MailBatchFilledEvent -=
          new MailBatchDelegate(
              Program.googleEmailUploaderModel_MailBatchFilledEvent);
      Program.GoogleEmailUploaderModel.MailBatchUploadTryStartEvent -=
          new MailBatchDelegate(
              Program.googleEmailUploaderModel_MailBatchUploadTryStartEvent);
      Program.GoogleEmailUploaderModel.UploadPausedEvent -=
          new UploadPausedDelegate(
              Program.googleEmailUploaderModel_UploadPausedEvent);
      Program.GoogleEmailUploaderModel.PauseCountDownEvent -=
          new PauseCountDownDelegate(
              Program.googleEmailUploaderModel_PauseCountDownEvent);
      Program.GoogleEmailUploaderModel.MailBatchUploadedEvent -=
          new MailBatchDelegate(
              Program.googleEmailUploaderModel_MailBatchUploadedEvent);
      Program.GoogleEmailUploaderModel.UploadDoneEvent -=
          new UploadDoneDelegate(
              Program.googleEmailUploaderModel_UploadDoneEvent);
    }

    static void googleEmailUploaderModel_MailBatchFillingEvent(
        MailBatch mailBatch,
        MCI.IMail mail) {
      if (Program.PrintEvents) {
        Program.OutputTextStream.WriteLine(
            "googleEmailUploaderModel_MailBatchFillingEvent");
      }
    }

    static void googleEmailUploaderModel_MailBatchFilledEvent(
        MailBatch mailBatch) {
      if (Program.PrintEvents) {
        Program.OutputTextStream.WriteLine(
            "googleEmailUploaderModel_MailBatchFilledEvent");
      }
    }

    static void googleEmailUploaderModel_MailBatchUploadTryStartEvent(
        MailBatch mailBatch) {
      if (Program.PrintEvents) {
        Program.OutputTextStream.WriteLine(
            "googleEmailUploaderModel_MailBatchUploadTryStartEvent");
      }
    }

    static void googleEmailUploaderModel_UploadPausedEvent(
        PauseReason pauseReason) {
      if (Program.PrintEvents) {
        Program.OutputTextStream.WriteLine(
            "googleEmailUploaderModel_UploadPausedEvent: {0}",
            pauseReason);
      }
    }

    static void googleEmailUploaderModel_PauseCountDownEvent(
        PauseReason pauseReason,
        int remainingCount) {
      if (Program.PrintEvents) {
        Program.OutputTextStream.WriteLine(
            "googleEmailUploaderModel_PauseCountDownEvent: {0} {1}",
            pauseReason,
            remainingCount);
      }
    }

    static void googleEmailUploaderModel_MailBatchUploadedEvent(
        MailBatch mailBatch) {
      if (Program.PrintEvents) {
        Program.OutputTextStream.WriteLine(
            "googleEmailUploaderModel_MailBatchUploadedEvent");
      }
    }

    static void googleEmailUploaderModel_UploadDoneEvent(
        DoneReason doneReadon) {
      Program.OutputTextStream.WriteLine(
          "googleEmailUploaderModel_UploadDoneEvent: {0}",
          doneReadon);
      Program.UnhookModelEvents();
    }

    [STAThread]
    static void Main(string[] args) {
      if (!Program.ProcessArgs(args)) {
        Program.PrintUsage();
        return;
      }
      GoogleEmailUploaderTrace.Initalize("TestTrace.txt");
      GoogleEmailUploaderModel.LoadClientFactories();
      Program.HttpFactory = new HttpFactory();
      Program.GoogleEmailUploaderModel = new GoogleEmailUploaderModel(
          Program.HttpFactory);
      Program.IdentToObjectMap = new Hashtable();
      while (true) {
        Command command = Program.GetCommand();
        if (command.Kind == CommandKind.Quit) {
          break;
        }
        Program.ExecuteCommand(command);
      }
      Program.GoogleEmailUploaderModel.Dispose();
      GoogleEmailUploaderTrace.Close();
    }
  }
}
