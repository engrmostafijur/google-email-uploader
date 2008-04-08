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
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace GoogleEmailUploader {
  delegate void StringDelegate(string text);

  delegate void StringBoolDelegate(string text, bool enabled);

  class UploadView : Form {
    readonly GoogleEmailUploaderModel googleEmailUploaderModel;

    NotifyIcon notificationTrayIcon;
    ProgressBar uploadProgressBar;

    Label uploadedMailCountLabel;
    LinkLabel failedMailCountLinkLabel;
    Label uploadInfo;

    Label messageLabel;

    Button pauseResumeButton;

    uint currentMailBatchSize;

    internal UploadView(GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.currentMailBatchSize = 0;
      this.googleEmailUploaderModel = googleEmailUploaderModel;

      this.notificationTrayIcon = new NotifyIcon();
      this.notificationTrayIcon.Icon = Resources.GMailIcon;
      this.notificationTrayIcon.Text = Resources.GoogleEmailUploaderAppNameText;
      this.notificationTrayIcon.Visible = false;
      this.notificationTrayIcon.DoubleClick +=
          new EventHandler(this.notificationTryIcon_DoubleClick);

      this.StartPosition = FormStartPosition.CenterScreen;
      this.Text = Resources.GoogleEmailUploaderAppNameText;
      this.Icon = Resources.GMailIcon;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
      this.BackColor = Color.White;
      this.Size = new Size(530, 370);
      this.Closing +=
          new CancelEventHandler(this.UploadView_FormClosing);
      this.InitializeComponent();
      this.HookModelEvents();
      this.Load += new EventHandler(this.UploadView_Load);
    }

    void InitializeComponent() {
      // Upload Info Label.
      this.uploadInfo = new Label();
      this.uploadInfo.Location = new Point(35, 55);
      this.uploadInfo.Size = new Size(230, 30);
      this.uploadInfo.Font = new Font("Arial", 9.25F);
      this.uploadInfo.Text = string.Empty;

      // Upload Progress Bar.
      this.uploadProgressBar = new ProgressBar();
      this.uploadProgressBar.Location = new Point(35, 90);
      this.uploadProgressBar.Size = new Size(200, 12);
      this.uploadProgressBar.Enabled = true;
      this.uploadProgressBar.Visible = true;
      this.uploadProgressBar.Maximum =
          (int)this.googleEmailUploaderModel.TotalSelectedItemCount;
      this.uploadProgressBar.BringToFront();
      this.uploadProgressBar.ForeColor = SystemColors.Highlight;

      // Uploaded Value Label
      this.uploadedMailCountLabel = new Label();
      this.uploadedMailCountLabel.Location = new Point(35, 105);
      this.uploadedMailCountLabel.Size = new Size(250, 30);
      this.uploadedMailCountLabel.ForeColor = Program.DisabledGreyColor;
      this.uploadedMailCountLabel.Font = new Font("Arial", 9.25F);
      this.uploadedMailCountLabel.Text = string.Empty;

      // Errors Label
      this.failedMailCountLinkLabel = new LinkLabel();
      this.failedMailCountLinkLabel.Location = new Point(35, 138);
      this.failedMailCountLinkLabel.AutoSize = true;
      this.failedMailCountLinkLabel.Font = new Font("Arial", 9.25F);
      this.failedMailCountLinkLabel.Text = string.Empty;
      this.failedMailCountLinkLabel.Click +=
          new EventHandler(this.openLogLabel_Click);

      // Pause message Label
      this.messageLabel = new Label();
      this.messageLabel.Location = new Point(35, 160);
      this.messageLabel.Size = new Size(340, 30);
      this.messageLabel.Font = new Font("Tahoma", 8, FontStyle.Bold);
      this.messageLabel.Text = string.Empty;

      // Upload Instructions Label.
      Label uploadInstructionLabel = new Label();
      uploadInstructionLabel.Location = new Point(35, 200);
      uploadInstructionLabel.Size = new Size(470, 45);
      uploadInstructionLabel.Font = new Font("Arial", 9.25F);
      uploadInstructionLabel.Text = Resources.UploadInstructionText;

      // Minimize to Tray LinkLabel.
      LinkLabel minimizeToTrayLinkLabel = new LinkLabel();
      minimizeToTrayLinkLabel.Location = new Point(35, 255);
      minimizeToTrayLinkLabel.Size = new Size(170, 15);
      minimizeToTrayLinkLabel.Text = Resources.MinimizeToTrayText;
      minimizeToTrayLinkLabel.Font = new Font("Arial", 9.25F);
      minimizeToTrayLinkLabel.Click +=
          new EventHandler(this.minimizeToTrayLinkLabel_Click);

      // Pause Button.
      this.pauseResumeButton = new Button();
      this.pauseResumeButton.Location = new Point(30, 287);
      this.pauseResumeButton.Size = new Size(72, 23);
      this.pauseResumeButton.Font = new Font("Arial", 9.25F);
      this.pauseResumeButton.Text = Resources.PauseText;
      this.pauseResumeButton.BackColor = SystemColors.Control;
      this.pauseResumeButton.Click +=
          new EventHandler(this.pauseResumeButton_Click);

      // Stop Button.
      Button stopButton = new Button();
      stopButton.Location = new Point(120, 287);
      stopButton.Size = new Size(72, 23);
      stopButton.Font = new Font("Arial", 9.25F);
      stopButton.Text = Resources.StopText;
      stopButton.BackColor = SystemColors.Control;
      stopButton.Click += new EventHandler(this.stopButton_Click);

      Program.AddHeaderStrip(3, this.Controls);

      this.Controls.Add(this.uploadInfo);
      this.Controls.Add(this.uploadProgressBar);
      this.Controls.Add(this.uploadedMailCountLabel);
      this.Controls.Add(this.failedMailCountLinkLabel);
      this.Controls.Add(this.messageLabel);
      this.Controls.Add(minimizeToTrayLinkLabel);
      this.Controls.Add(uploadInstructionLabel);
      this.Controls.Add(this.pauseResumeButton);
      this.Controls.Add(stopButton);
    }

    void HookModelEvents() {
      this.googleEmailUploaderModel.MailBatchFillingStartEvent +=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchFillingStartEvent);
      this.googleEmailUploaderModel.MailBatchFillingEvent +=
          new MailDelegate(this.googleEmailUploaderModel_MailBatchFillingEvent);
      this.googleEmailUploaderModel.MailBatchFillingEndEvent +=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchFillingEndEvent);
      this.googleEmailUploaderModel.MailBatchUploadTryStartEvent +=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchUploadTryStartEvent);
      this.googleEmailUploaderModel.UploadPausedEvent +=
          new UploadPausedDelegate(
              this.googleEmailUploaderModel_UploadPausedEvent);
      this.googleEmailUploaderModel.PauseCountDownEvent +=
          new PauseCountDownDelegate(
              this.googleEmailUploaderModel_PauseCountDownEvent);
      this.googleEmailUploaderModel.MailBatchUploadedEvent +=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchUploadedEvent);
      this.googleEmailUploaderModel.ContactUploadTryStartEvent +=
          new ContactEntryDelegate(
              this.googleEmailUploaderModel_ContactUploadTryStartEvent);
      this.googleEmailUploaderModel.ContactUploadedEvent +=
          new ContactEntryDelegate(
              this.googleEmailUploaderModel_ContactUploadedEvent);
      this.googleEmailUploaderModel.UploadDoneEvent +=
          new UploadDoneDelegate(
              this.googleEmailUploaderModel_UploadDoneEvent);
    }

    void UnhookModelEvents() {
      this.googleEmailUploaderModel.MailBatchFillingStartEvent -=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchFillingStartEvent);
      this.googleEmailUploaderModel.MailBatchFillingEvent -=
          new MailDelegate(this.googleEmailUploaderModel_MailBatchFillingEvent);
      this.googleEmailUploaderModel.MailBatchFillingEndEvent -=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchFillingEndEvent);
      this.googleEmailUploaderModel.MailBatchUploadTryStartEvent -=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchUploadTryStartEvent);
      this.googleEmailUploaderModel.UploadPausedEvent -=
          new UploadPausedDelegate(
              this.googleEmailUploaderModel_UploadPausedEvent);
      this.googleEmailUploaderModel.PauseCountDownEvent -=
          new PauseCountDownDelegate(
              this.googleEmailUploaderModel_PauseCountDownEvent);
      this.googleEmailUploaderModel.MailBatchUploadedEvent -=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchUploadedEvent);
      this.googleEmailUploaderModel.ContactUploadTryStartEvent -=
          new ContactEntryDelegate(
              this.googleEmailUploaderModel_ContactUploadTryStartEvent);
      this.googleEmailUploaderModel.ContactUploadedEvent -=
          new ContactEntryDelegate(
              this.googleEmailUploaderModel_ContactUploadedEvent);
      this.googleEmailUploaderModel.UploadDoneEvent -=
          new UploadDoneDelegate(this.googleEmailUploaderModel_UploadDoneEvent);
    }

    void MorphToDoneState(DoneReason doneReason) {
      this.Controls.Clear();

      Label uploadCompleteHeaderLabel = new Label();
      uploadCompleteHeaderLabel.Location = new Point(60, 23);
      uploadCompleteHeaderLabel.AutoSize = true;
      uploadCompleteHeaderLabel.Font = new Font("Arial", 11F, FontStyle.Bold);
      uploadCompleteHeaderLabel.BackColor = Program.TopStripBackColor;

      Label uploadCompleteTextLabel = new Label();
      uploadCompleteTextLabel.Location = new Point(35, 60);
      uploadCompleteTextLabel.Font = new Font("Arial", 9.25F);

      if (doneReason == DoneReason.Completed) {
        this.BackgroundImage =
            Resources.GoogleEmailUploaderImportCompleteBackgroundImage;
        uploadCompleteTextLabel.Size = new Size(250, 30);
        uploadCompleteHeaderLabel.Text = Resources.UploadCompleteHeaderText;
        uploadCompleteTextLabel.Text = Resources.UploadCompleteText;

        Label uploadCompleteInfoLabel = new Label();
        uploadCompleteInfoLabel.Location = new Point(35, 95);
        uploadCompleteInfoLabel.Size = new Size(250, 45);
        uploadCompleteInfoLabel.Font = new Font("Arial", 9.25F);
        uploadCompleteInfoLabel.Text = Resources.UploadCompleteInfoText;
        this.Controls.Add(uploadCompleteInfoLabel);

        if (this.googleEmailUploaderModel.HasFailures) {
          Label errorsTextLabel = new Label();
          errorsTextLabel.Location = new Point(35, 145);
          errorsTextLabel.Size = new Size(250, 20);
          errorsTextLabel.Font = new Font("Arial", 9.25F);
          errorsTextLabel.Text =
              string.Format(Resources.UploadErrorsText,
                            this.googleEmailUploaderModel.TotalFailedItemCount);
          errorsTextLabel.ForeColor = Color.Red;
          this.Controls.Add(errorsTextLabel);

          LinkLabel openLogLabel = new LinkLabel();
          openLogLabel.Location = new Point(35, 165);
          openLogLabel.Font = new Font("Arial", 9.25F);
          openLogLabel.AutoSize = true;
          openLogLabel.Text = Resources.OpenLogText;
          openLogLabel.Click += new EventHandler(this.openLogLabel_Click);
          this.Controls.Add(openLogLabel);
        }

      } else {
        this.BackgroundImage =
            Resources.GoogleEmailUploaderBackgroundImage;
        uploadCompleteTextLabel.Size = new Size(250, 60);
        if (doneReason == DoneReason.Stopped) {
          uploadCompleteTextLabel.Text = Resources.UploadStoppedText;
          uploadCompleteHeaderLabel.Text = Resources.UploadStoppedHeaderText;
        } else if (doneReason == DoneReason.Unauthorized) {
          uploadCompleteTextLabel.Text = Resources.UploadForbiddenText;
          uploadCompleteHeaderLabel.Text = Resources.UploadUnauthorizedHeaderText;
        } else if (doneReason == DoneReason.Forbidden) {
          uploadCompleteTextLabel.Text = Resources.UploadForbiddenText;
          uploadCompleteHeaderLabel.Text = Resources.UploadForbiddenHeaderText;
        }
      }

      Button finishButton = new Button();
      finishButton.Location = new Point(30, 287);
      finishButton.Size = new Size(72, 23);
      finishButton.Font = new Font("Arial", 9.25F);
      finishButton.Text = Resources.FinishText;
      finishButton.FlatStyle = FlatStyle.System;
      finishButton.Click += new EventHandler(finishButton_Click);

      this.Controls.Add(uploadCompleteHeaderLabel);
      this.Controls.Add(uploadCompleteTextLabel);
      this.Controls.Add(finishButton);

      this.ActiveControl = finishButton;
    }

    void openLogLabel_Click(object sender, EventArgs e) {
      string tempFilePath = Path.GetTempFileName();
      tempFilePath = Path.ChangeExtension(tempFilePath, ".txt");
      using (StreamWriter streamWriter = File.AppendText(tempFilePath)) {
        foreach (StoreModel storeModel in
            googleEmailUploaderModel.FlatStoreModelList) {
          if (!storeModel.IsContactSelected) {
            continue;
          }
          string storeString = string.Format(Resources.StoreTemplateText,
                                             storeModel.FullStorePath);
          streamWriter.WriteLine(storeString);
          streamWriter.WriteLine(new string('=', storeString.Length));
          foreach (FailedContactDatum failedContactDatum
              in storeModel.ContactUploadData.Values) {
            if (failedContactDatum == null) {
              continue;
            }
            streamWriter.WriteLine(failedContactDatum.ContactName);
            streamWriter.WriteLine(Resources.ReasonTemplateText,
                                   failedContactDatum.FailureReason);
            streamWriter.WriteLine(new string('-', storeString.Length));
          }
        }
        foreach (FolderModel folderModel in
            googleEmailUploaderModel.FlatFolderModelList) {
          if (!folderModel.IsSelected) {
            continue;
          }
          string folderString = string.Format(Resources.FolderTemplateText,
                                              folderModel.FullFolderPath);
          streamWriter.WriteLine(folderString);
          streamWriter.WriteLine(new string('=', folderString.Length));
          foreach (FailedMailDatum failedMailDatum
              in folderModel.MailUploadData.Values) {
            if (failedMailDatum == null) {
              continue;
            }
            streamWriter.WriteLine(failedMailDatum.MailHead);
            streamWriter.WriteLine(Resources.ReasonTemplateText,
                                   failedMailDatum.FailureReason);
            streamWriter.WriteLine(new string('-', folderString.Length));
          }
        }
      }
      Process.Start(tempFilePath);
    }

    void finishButton_Click(object sender, EventArgs e) {
      this.Close();
    }

    void UploadView_Load(object sender, EventArgs e) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.Invoke(new BoolDelegate(this.UpdateStatistics),
                    new object[] { false });
      }
    }

    void UploadView_FormClosing(object sender, CancelEventArgs e) {
      this.UnhookModelEvents();
      this.googleEmailUploaderModel.StopUpload();
      Application.DoEvents();
    }

    void pauseResumeButton_Click(object sender, EventArgs e) {
      if (this.googleEmailUploaderModel.IsPaused) {
        this.pauseResumeButton.Text = Resources.PauseText;
        this.googleEmailUploaderModel.ResumeUpload();
      } else {
        this.pauseResumeButton.Text = Resources.ResumeText;
        this.googleEmailUploaderModel.PauseUpload();
      }
    }

    void stopButton_Click(object sender, EventArgs e) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        string pauseReasonText = Resources.StopWaitingText;
        this.Invoke(new StringDelegate(this.UpdateMessageLabel),
                    new object[] { pauseReasonText });
      }
      this.pauseResumeButton.Enabled = false;
      this.googleEmailUploaderModel.StopUpload();
    }

    void googleEmailUploaderModel_MailBatchFillingStartEvent(
        MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.currentMailBatchSize = mailBatch.MailCount;
          this.Invoke(new BoolDelegate(this.UpdateStatistics),
                      new object[] { true });
        }
      }
    }

    void googleEmailUploaderModel_MailBatchFillingEvent(MailBatch mailBatch,
                                                        IMail mail) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.currentMailBatchSize = mailBatch.MailCount;
          this.Invoke(new BoolDelegate(this.UpdateStatistics),
                      new object[] { true });
        }
      }
    }

    void googleEmailUploaderModel_MailBatchFillingEndEvent(
        MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.currentMailBatchSize = mailBatch.MailCount;
          this.Invoke(new BoolDelegate(this.UpdateStatistics),
                      new object [] { true });
        }
      }
    }

    void googleEmailUploaderModel_MailBatchUploadTryStartEvent(
        MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.Invoke(new StringDelegate(this.UpdateMessageLabel),
                      new object[] { string.Empty });
        }
      }
    }

    void googleEmailUploaderModel_UploadPausedEvent(PauseReason pauseReason) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        string pauseResumeButtonText = Resources.PauseText;
        string pauseReasonText = string.Empty;
        bool enabled = true;
        if (pauseReason != PauseReason.Resuming) {
          pauseReasonText = UploadView.GetProperPauseMessage(pauseReason, 0);
          pauseResumeButtonText = Resources.ResumeText;
          enabled = (pauseReason == PauseReason.UserAction);
        }
        this.Invoke(new StringDelegate(this.UpdateMessageLabel),
                    new object[] { pauseReasonText });
        this.Invoke(
            new StringBoolDelegate(this.SetPauseResumeButtonText),
            new object[] { pauseResumeButtonText, enabled });
      }
    }

    void googleEmailUploaderModel_PauseCountDownEvent(PauseReason pauseReason,
                                            int remainingCount) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        string pauseReasonText =
            UploadView.GetProperPauseMessage(pauseReason, remainingCount);
        this.Invoke(new StringDelegate(this.UpdateMessageLabel),
                    new object[] { pauseReasonText });
      }
    }

    void googleEmailUploaderModel_MailBatchUploadedEvent(MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.Invoke(new BoolDelegate(this.UpdateStatistics),
                    new object[] { true });
      }
    }

    void googleEmailUploaderModel_ContactUploadTryStartEvent(
        ContactEntry contactEntry) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.Invoke(new StringDelegate(this.UpdateMessageLabel),
                      new object[] { string.Empty });
        }
      }
    }

    void googleEmailUploaderModel_ContactUploadedEvent(
        ContactEntry contactEntry) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.Invoke(new BoolDelegate(this.UpdateStatistics),
                    new object[] { false });
      }
    }

    void googleEmailUploaderModel_UploadDoneEvent(DoneReason doneReason) {
      if (this.notificationTrayIcon.Visible) {
        this.Invoke(new VoidDelegate(this.ForceShow));
      }
      this.UnhookModelEvents();
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.Invoke(new UploadDoneDelegate(this.MorphToDoneState),
                    new object[] { doneReason });
      }
    }

    void UpdateStatistics(bool isMail) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        TimeSpan timeSpan = this.googleEmailUploaderModel.UploadTimeRemaining;
        this.uploadedMailCountLabel.Text =
            string.Format(Resources.UploadedItemsTemplateText,
                          this.googleEmailUploaderModel.
                              UploadedContactCount.ToString(),
                          this.googleEmailUploaderModel.
                              SelectedContactCount.ToString(),
                          this.googleEmailUploaderModel.
                              UploadedEmailCount.ToString(),
                          this.googleEmailUploaderModel.
                              SelectedEmailCount.ToString(),
                          timeSpan.Hours,
                          timeSpan.Minutes);
        uint failedItemCount =
            this.googleEmailUploaderModel.TotalFailedItemCount;
        if (failedItemCount > 0) {
          if (failedItemCount == 1) {
            this.failedMailCountLinkLabel.Text =
                string.Format(Resources.FailedItemTemplateText,
                              failedItemCount.ToString());
          } else {
            this.failedMailCountLinkLabel.Text =
                string.Format(Resources.FailedItemsTemplateText,
                              failedItemCount.ToString());
          }
        }

        int newValue =
            (int)(this.googleEmailUploaderModel.TotalUploadedItemCount
                + this.googleEmailUploaderModel.TotalFailedItemCount);
        if (newValue >= this.uploadProgressBar.Maximum) {
          this.uploadProgressBar.Maximum = newValue;
        }
        this.uploadProgressBar.Value = newValue;
        ClientModel clientModel =
            this.googleEmailUploaderModel.CurrentClientModel;
        if (clientModel != null) {
          this.uploadInfo.Text =
              string.Format(
                  isMail ?
                      Resources.UploadingMailText :
                      Resources.UploadingContactsText,
                  clientModel.DisplayName);
        }
      }
    }

    void UpdateMessageLabel(string messageText) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.messageLabel.Text = messageText;
      }
    }

    static string GetProperPauseMessage(PauseReason pauseReason,
                                        int remainingCount) {
      string pauseReasonText;
      switch (pauseReason) {
        case PauseReason.UserAction:
          pauseReasonText = Resources.PauseUserActionText;
          break;
        case PauseReason.ConnectionFailures:
          pauseReasonText =
              string.Format(Resources.PauseConnectionFailuresTemplateText,
                            remainingCount);
          break;
        case PauseReason.ServiceUnavailable:
          pauseReasonText =
              string.Format(Resources.ServiceUnavailableTemplateText,
                            remainingCount);
          break;
        case PauseReason.ServerInternalError:
          pauseReasonText =
              string.Format(Resources.ServerInternalErrorTemplateText,
                            remainingCount);
          break;
        case PauseReason.Resuming:
          pauseReasonText = string.Empty;
          break;
        default:
          Debug.Fail("What kind of reason is this?");
          pauseReasonText = string.Empty;
          break;
      }
      return pauseReasonText;
    }

    void SetPauseResumeButtonText(string text, bool enabled) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.pauseResumeButton.Text = text;
        this.pauseResumeButton.Enabled = enabled;
      }
    }

    void notificationTryIcon_DoubleClick(object sender, EventArgs e) {
      this.ForceShow();
    }

    void minimizeToTrayLinkLabel_Click(object sender, EventArgs e) {
      this.notificationTrayIcon.Visible = true;
      this.ShowInTaskbar = false;
      // One hide does not seem to be enough for .Net Fx 1.1 to hide it.
      // Tell it twice it does the job.
      this.Hide();
      this.Hide();
    }

    void ForceShow() {
      this.WindowState = FormWindowState.Normal;
      this.Show();
      // Activate the form.
      this.Activate();
      this.Focus();
      this.notificationTrayIcon.Visible = false;
      this.ShowInTaskbar = true;
    }

    protected override void WndProc(ref Message message) {
      if (message.Msg == Program.MessageId) {
        this.ForceShow();
      } else {
        base.WndProc(ref message);
      }
    }
  }
}
