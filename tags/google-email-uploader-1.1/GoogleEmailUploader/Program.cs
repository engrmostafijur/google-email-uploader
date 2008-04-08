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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Drawing;

namespace GoogleEmailUploader {
  class Program {
    const string UniqueIdentifier = "{ED54DAF4-45E9-4365-8D10-E07FDFA2A11D}";

    [DllImport("user32.dll")]
    static extern int BroadcastSystemMessage(int flags,
                                             ref int recipients,
                                             int message,
                                             int wParam,
                                             int lParam);

    // This ignores the current app Hex 2
    const int BSF_IGNORECURRENTTASK = 0x00000002;

    // This posts the message Hex 10
    const int BSF_POSTMESSAGE = 0x00000010;

    // This tells the windows message to just go to applications Hex 8
    const int BSM_APPLICATIONS = 0x00000008;

    [DllImport("user32.dll")]
    static extern int RegisterWindowMessage(string name);

    internal static int MessageId;

    internal static Color TopStripBackColor = Color.FromArgb(218, 238, 252);
    internal static Color DisabledGreyColor = Color.FromArgb(166, 166, 166);
    internal static Color SigninBackColor = Color.FromArgb(232, 238, 250);

    internal static bool EnsureClientsNotRunning(ArrayList clientFactoryList) {
      try {
        Process[] processes = Process.GetProcesses();
        ArrayList mailProcesses = new ArrayList();
        foreach (IClientFactory clientFactory in clientFactoryList) {
          foreach (string processName in clientFactory.ClientProcessNames) {
            foreach (Process process in processes) {
              if (process.ProcessName.ToLower() == processName) {
                mailProcesses.Add(process);
              }
            }
          }
        }
        if (mailProcesses.Count == 0) {
          return true;
        } else {
          StringBuilder sb = new StringBuilder(
              Resources.ProcessCloseTemplateText);
          foreach (Process process in mailProcesses) {
            sb.Append("\n  ");
            sb.Append(process.ProcessName);
          }
          MessageBox.Show(sb.ToString(),
                          Resources.GoogleEmailUploaderAppNameText);
          return false;
        }
      } catch {
        return true;
      }
    }

    internal static void AddHeaderStrip(
        int current,
        Control.ControlCollection controlCollection) {
      Debug.Assert(current >= 0 && current < 4);

      Font defaultFont = new Font("Arial", 9.5F);
      Font boldFont = new Font("Arial", 9.5F, FontStyle.Bold);
      Font separatorFont = new Font("Arial", 12F);
      Color backColor = Program.TopStripBackColor;
      Color foreColor = Program.DisabledGreyColor;

      Label signInLabel = new Label();
      signInLabel.Location = new Point(25, 24);
      signInLabel.AutoSize = true;
      signInLabel.Font = defaultFont;
      signInLabel.ForeColor = foreColor;
      signInLabel.BackColor = backColor;
      signInLabel.Text = Resources.SignInHeaderText;
      if (current == 0) {
        signInLabel.ForeColor = Control.DefaultForeColor;
        signInLabel.Font = boldFont;
      }
      controlCollection.Add(signInLabel);

      Label separator1 = new Label();
      separator1.Location = new Point(signInLabel.Right + 2, 23);
      separator1.AutoSize = true;
      separator1.ForeColor = foreColor;
      separator1.BackColor = backColor;
      separator1.Font = separatorFont;
      separator1.Text = Resources.SeparatorText;
      controlCollection.Add(separator1);

      Label selectEmailLabel = new Label();
      selectEmailLabel.Location = new Point(separator1.Right + 2, 24);
      selectEmailLabel.AutoSize = true;
      selectEmailLabel.Font = defaultFont;
      selectEmailLabel.ForeColor = foreColor;
      selectEmailLabel.BackColor = backColor;
      selectEmailLabel.Text = Resources.SelectHeaderText;
      if (current == 1) {
        selectEmailLabel.ForeColor = Control.DefaultForeColor;
        selectEmailLabel.Font = boldFont;
      }
      controlCollection.Add(selectEmailLabel);

      Label separator2 = new Label();
      separator2.Location = new Point(selectEmailLabel.Right + 2, 23);
      separator2.AutoSize = true;
      separator2.ForeColor = foreColor;
      separator2.BackColor = backColor;
      separator2.Font = separatorFont;
      separator2.Text = Resources.SeparatorText;
      controlCollection.Add(separator2);

      Label labelLabel = new Label();
      labelLabel.Location = new Point(separator2.Right + 2, 24);
      labelLabel.AutoSize = true;
      labelLabel.ForeColor = foreColor;
      labelLabel.BackColor = backColor;
      labelLabel.Font = defaultFont;
      labelLabel.Text = Resources.LabelHeaderText;
      if (current == 2) {
        labelLabel.ForeColor = Control.DefaultForeColor;
        labelLabel.Font = boldFont;
      }
      controlCollection.Add(labelLabel);

      Label separator3 = new Label();
      separator3.Location = new Point(labelLabel.Right + 2, 23);
      separator3.AutoSize = true;
      separator3.ForeColor = foreColor;
      separator3.BackColor = backColor;
      separator3.Font = separatorFont;
      separator3.Text = Resources.SeparatorText;
      controlCollection.Add(separator3);

      Label uploadLabel = new Label();
      uploadLabel.Location = new Point(separator3.Right + 2, 24);
      uploadLabel.AutoSize = true;
      uploadLabel.ForeColor = foreColor;
      uploadLabel.BackColor = backColor;
      uploadLabel.Font = defaultFont;
      uploadLabel.Text = Resources.UploadHeaderText;
      if (current == 3) {
        uploadLabel.ForeColor = Control.DefaultForeColor;
        uploadLabel.Font = boldFont;
      }
      controlCollection.Add(uploadLabel);
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args) {
      Program.MessageId =
          Program.RegisterWindowMessage(Program.UniqueIdentifier);

      using (Mutex mutex =
          new Mutex(false, Program.UniqueIdentifier)) {
        if (mutex.WaitOne(1, false)) {
          // First instance...
          try {
            // Set up tracing facility
            string traceFilePath =
                Path.Combine(Application.LocalUserAppDataPath,
                             "GoogleEmailUploaderTrace.txt");

            GoogleEmailUploaderConfig.InitializeConfiguration();
            GoogleEmailUploaderTrace.Initalize(traceFilePath);
            GoogleEmailUploaderModel.LoadClientFactories();
            if (GoogleEmailUploaderModel.ClientFactories.Count == 0) {
              MessageBox.Show(Resources.NoClientPluginsText,
                              Resources.GoogleEmailUploaderAppNameText);
              return;
            }

            bool isRestarted = false;
            while (true) {
              // Create a google email uploader model.
              using (GoogleEmailUploaderModel googleEmailUploaderModel =
                  new GoogleEmailUploaderModel()) {

                // Try to login.
                LoginView loginView =
                    new LoginView(googleEmailUploaderModel, isRestarted);
                Application.Run(loginView);
                isRestarted = true;
                if (loginView.Result == LoginViewResult.UploadForbidden) {
                  continue;
                }

                if (loginView.Result == LoginViewResult.Cancel) {
                  // If could canceled or closed the login then
                  // exit the application.
                  break;
                }

                // Start the select View.
                SelectView selectView =
                    new SelectView(googleEmailUploaderModel);
                Application.Run(selectView);
                if (selectView.Result == SelectViewResult.Cancel ||
                    selectView.Result == SelectViewResult.Closed) {
                  // If could canceled or closed the select view then
                  // exit the application.
                  break;
                }

                if (selectView.Result == SelectViewResult.Restart) {
                  continue;
                }

                // Start the upload model.
                UploadView uploadView = new UploadView(googleEmailUploaderModel);
                googleEmailUploaderModel.StartUpload();
                Application.Run(uploadView);
                googleEmailUploaderModel.WaitForUploadingThread();
                break;
              }
            }
          } catch (Exception excep) {
            GoogleEmailUploaderTrace.WriteLine(excep.ToString());
          } finally {
            GoogleEmailUploaderTrace.Close();
          }
        } else {
          int bsmRecipients = Program.BSM_APPLICATIONS;
          Program.BroadcastSystemMessage(Program.BSF_IGNORECURRENTTASK |
                                            Program.BSF_POSTMESSAGE,
                                         ref bsmRecipients,
                                         Program.MessageId,
                                         0,
                                         0);
        }
      }
    }
  }
}

