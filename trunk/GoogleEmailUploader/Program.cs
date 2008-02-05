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

    static bool EnsureClientsNotRunning(ArrayList clientFactoryList) {
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
          StringBuilder sb = new StringBuilder(Resources.ProcessCloseTemplate);
          foreach (Process process in mailProcesses) {
            sb.Append("\n  ");
            sb.Append(process.ProcessName);
          }
          MessageBox.Show(sb.ToString(),
                          Resources.GoogleEmailUploaderAppName);
          return false;
        }
      } catch {
        return true;
      }
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
            string assemblyDirectory =
                Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string traceFilePath =
                Path.Combine(assemblyDirectory,
                             "GoogleEmailUploaderTrace.txt");

            GoogleEmailUploaderConfig.InitializeConfiguration();
            GoogleEmailUploaderTrace.Initalize(traceFilePath);
            GoogleEmailUploaderModel.LoadClientFactories();
            if (GoogleEmailUploaderModel.ClientFactories.Count == 0) {
              MessageBox.Show(Resources.NoClientPlugins,
                              Resources.GoogleEmailUploaderAppName);
              return;
            }
            if (!Program.EnsureClientsNotRunning(
                GoogleEmailUploaderModel.ClientFactories)) {
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
                SelectView selectView = new SelectView(googleEmailUploaderModel);
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

