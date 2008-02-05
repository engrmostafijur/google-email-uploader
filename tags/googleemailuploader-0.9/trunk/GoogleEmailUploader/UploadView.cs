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
  delegate void StringColorDelegate(string text,
                                    Color color);

  class UploadView : Form {
    readonly GoogleEmailUploaderModel googleEmailUploaderModel;

    NotifyIcon notificationTrayIcon;
    ProgressBar uploadProgressBar;

    Label uploadedMailCountLabel;
    Label failedMailCountLabel;
    Label importInfo;

    Label messageLabel;

    Button pauseResumeButton;

    uint currentMailBatchSize;

    internal UploadView(GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.currentMailBatchSize = 0;
      this.googleEmailUploaderModel = googleEmailUploaderModel;

      this.notificationTrayIcon = new NotifyIcon();
      this.notificationTrayIcon.Icon = this.Icon;
      this.notificationTrayIcon.Text = Resources.GoogleEmailUploaderAppName;
      this.notificationTrayIcon.Visible = false;
      this.notificationTrayIcon.DoubleClick +=
          new EventHandler(this.notificationTryIcon_DoubleClick);

      this.StartPosition = FormStartPosition.CenterScreen;
      this.Text = Resources.GoogleEmailUploaderAppName;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
      this.BackColor = Color.White;
      this.Size = new Size(480, 370);
      this.Closing +=
          new CancelEventHandler(this.UploadView_FormClosing);
      this.InitializeComponent();
      this.HookModelEvents();
      this.Load += new EventHandler(this.UploadView_Load);
    }

    void InitializeComponent() {
      this.SuspendLayout();

      // Import Info Label.
      this.importInfo = new Label();
      this.importInfo.Location = new Point(35, 55);
      this.importInfo.Size = new Size(180, 30);
      this.importInfo.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.importInfo.Text = string.Empty;

      // Upload Progress Bar.
      this.uploadProgressBar = new ProgressBar();
      this.uploadProgressBar.Location = new Point(35, 90);
      this.uploadProgressBar.Size = new Size(200, 12);
      this.uploadProgressBar.Enabled = true;
      this.uploadProgressBar.Visible = true;
      this.uploadProgressBar.Maximum =
          (int)this.googleEmailUploaderModel.SelectedMailCount;
      this.uploadProgressBar.BringToFront();
      this.uploadProgressBar.ForeColor = SystemColors.Highlight;

      // Uploaded Value Label
      this.uploadedMailCountLabel = new Label();
      this.uploadedMailCountLabel.Location = new Point(35, 105);
      this.uploadedMailCountLabel.Size = new Size(250, 30);
      this.uploadedMailCountLabel.ForeColor = Color.FromArgb(166, 166, 166);
      this.uploadedMailCountLabel.Font = new Font("Arial", 9.25F);
      this.uploadedMailCountLabel.Text = string.Empty;

      // Errors Label
      this.failedMailCountLabel = new Label();
      this.failedMailCountLabel.Location = new Point(35, 123);
      this.failedMailCountLabel.AutoSize = true;
      this.failedMailCountLabel.ForeColor = Color.Red;
      this.failedMailCountLabel.Font = new Font("Arial", 9.25F);
      this.failedMailCountLabel.Text = string.Empty;

      // Pause Label
      this.messageLabel = new Label();
      this.messageLabel.Location = new Point(35, 140);
      this.messageLabel.AutoSize = true;
      this.messageLabel.Font = new Font("Tahoma", 8, FontStyle.Bold);
      this.messageLabel.Text = string.Empty;

      // Minimize to Tray LinkLabel.
      LinkLabel minimizeToTrayLinkLabel = new LinkLabel();
      minimizeToTrayLinkLabel.Location = new Point(35, 230);
      minimizeToTrayLinkLabel.Size = new Size(170, 15);
      minimizeToTrayLinkLabel.Text = Resources.MinimizeToTrayText;
      minimizeToTrayLinkLabel.Font = new Font("Arial", 9.25F);
      minimizeToTrayLinkLabel.Click +=
          new EventHandler(this.minimizeToTrayLinkLabel_Click);

      // Note Label.
      Label noteLabel = new Label();
      noteLabel.Location = new Point(35, 170);
      noteLabel.Size = new Size(100, 15);
      noteLabel.Text = Resources.NoteText;
      noteLabel.Font = new Font("Arial", 9.25F, FontStyle.Bold);

      // Upload Instructions Label.
      Label uploadInstructionLabel = new Label();
      uploadInstructionLabel.Location = new Point(35, 188);
      uploadInstructionLabel.Size = new Size(300, 30);
      uploadInstructionLabel.Font = new Font("Arial", 9.25F);
      uploadInstructionLabel.Text = Resources.UploadInstruction;

      // Pause Button.
      this.pauseResumeButton = new Button();
      this.pauseResumeButton.Location = new Point(30, 287);
      this.pauseResumeButton.Size = new Size(72, 23);
      this.pauseResumeButton.Font = new Font("Arial", 9.25F);
      this.pauseResumeButton.Text = Resources.PauseText;
      this.pauseResumeButton.BackColor = SystemColors.Control;
      this.pauseResumeButton.ForeColor = Color.DarkRed;
      this.pauseResumeButton.Click +=
          new EventHandler(this.pauseResumeButton_Click);

      // Cancel Button.
      Button abortButton = new Button();
      abortButton.Location = new Point(120, 287);
      abortButton.Size = new Size(72, 23);
      abortButton.Font = new Font("Arial", 9.25F);
      abortButton.Text = Resources.AbortText;
      abortButton.BackColor = SystemColors.Control;
      abortButton.ForeColor = Color.Red;
      abortButton.Click += new EventHandler(abortButton_Click);

      this.createImportDialogHeader();
      this.Controls.Add(importInfo);
      this.Controls.Add(this.uploadProgressBar);
      this.Controls.Add(this.uploadedMailCountLabel);
      this.Controls.Add(this.failedMailCountLabel);
      this.Controls.Add(this.messageLabel);
      this.Controls.Add(minimizeToTrayLinkLabel);
      this.Controls.Add(noteLabel);
      this.Controls.Add(uploadInstructionLabel);
      this.Controls.Add(this.pauseResumeButton);
      this.Controls.Add(abortButton);

      // Show the layout.
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    void createImportDialogHeader() {
      Font defaultFont = new Font("Arial", 9.5F);
      Font separatorFont = new Font("Arial", 12F);
      Color backColor = Color.FromArgb(229, 240, 254);
      Color foreColor = Color.FromArgb(166, 166, 166);

      Label signInLabel = new Label();
      signInLabel.Location = new Point(25, 24);
      signInLabel.Size = new Size(47, 16);
      signInLabel.Font = defaultFont;
      signInLabel.BackColor = backColor;
      signInLabel.ForeColor = foreColor;
      signInLabel.Text = Resources.SignInHeaderText;

      Label separator1 = new Label();
      separator1.Location = new Point(69, 23);
      separator1.Size = new Size(11, 15);
      separator1.Font = separatorFont;
      separator1.BackColor = backColor;
      separator1.ForeColor = foreColor;
      separator1.Text = Resources.SeparatorText;

      Label selectEmailLabel = new Label();
      selectEmailLabel.Location = new Point(82, 24);
      selectEmailLabel.Size = new Size(71, 15);
      selectEmailLabel.Font = defaultFont;
      selectEmailLabel.ForeColor = foreColor;
      selectEmailLabel.BackColor = backColor;
      selectEmailLabel.Text = Resources.SelectEmailHeaderText;

      Label separator2 = new Label();
      separator2.Location = new Point(152, 23);
      separator2.Size = new Size(11, 15);
      separator2.Font = separatorFont;
      separator2.BackColor = backColor;
      separator2.ForeColor = foreColor;
      separator2.Text = Resources.SeparatorText;

      Label labelLabel = new Label();
      labelLabel.Location = new Point(167, 24);
      labelLabel.Size = new Size(42, 15);
      labelLabel.BackColor = backColor;
      labelLabel.Font = defaultFont;
      labelLabel.ForeColor = foreColor;
      labelLabel.Text = Resources.LabelHeaderText;

      Label separator3 = new Label();
      separator3.Location = new Point(205, 23);
      separator3.Size = new Size(11, 15);
      separator3.Font = separatorFont;
      separator3.BackColor = backColor;
      separator3.ForeColor = foreColor;
      separator3.Text = Resources.SeparatorText;

      Label importLabel = new Label();
      importLabel.Location = new Point(221, 24);
      importLabel.Size = new Size(46, 15);
      importLabel.BackColor = backColor;
      importLabel.Font = new Font(defaultFont, FontStyle.Bold);
      importLabel.Text = Resources.ImportHeaderText;

      this.Controls.Add(signInLabel);
      this.Controls.Add(separator1);
      this.Controls.Add(selectEmailLabel);
      this.Controls.Add(separator2);
      this.Controls.Add(labelLabel);
      this.Controls.Add(separator3);
      this.Controls.Add(importLabel);
    }

    void HookModelEvents() {
      this.googleEmailUploaderModel.MailBatchFillingEvent +=
          new MailDelegate(this.googleEmailUploaderModel_MailBatchFillingEvent);
      this.googleEmailUploaderModel.MailBatchFilledEvent +=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchFilledEvent);
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
      this.googleEmailUploaderModel.UploadDoneEvent +=
          new UploadDoneDelegate(
              this.googleEmailUploaderModel_UploadDoneEvent);
    }

    void UnhookModelEvents() {
      this.googleEmailUploaderModel.MailBatchFillingEvent -=
          new MailDelegate(this.googleEmailUploaderModel_MailBatchFillingEvent);
      this.googleEmailUploaderModel.MailBatchFilledEvent -=
          new MailBatchDelegate(
              this.googleEmailUploaderModel_MailBatchFilledEvent);
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
      this.googleEmailUploaderModel.UploadDoneEvent -=
          new UploadDoneDelegate(this.googleEmailUploaderModel_UploadDoneEvent);
    }

    void MorphToDoneState(DoneReason doneReason) {
      this.SuspendLayout();

      this.Controls.Clear();
      this.BackgroundImage =
          Resources.GoogleEmailUploaderImportCompleteBackgroundImage;

      Label importCompleteHeaderLabel = new Label();
      importCompleteHeaderLabel.Location = new Point(60, 23);
      importCompleteHeaderLabel.Size = new Size(140, 20);
      importCompleteHeaderLabel.Font = new Font("Arial", 11F, FontStyle.Bold);
      importCompleteHeaderLabel.BackColor = Color.FromArgb(229, 240, 254);

      if (doneReason == DoneReason.Completed) {
        importCompleteHeaderLabel.Text = Resources.ImportCompleteHeader;
      } else if (doneReason == DoneReason.Aborted) {
        importCompleteHeaderLabel.ForeColor = Color.Red;
        importCompleteHeaderLabel.Text = Resources.ImportAbortedHeader;
      } else if (doneReason == DoneReason.Unauthorized) {
        importCompleteHeaderLabel.ForeColor = Color.Red;
        importCompleteHeaderLabel.Text = Resources.ImportUnauthorizedHeader;
      } else if (doneReason == DoneReason.Forbidden) {
        importCompleteHeaderLabel.ForeColor = Color.Red;
        importCompleteHeaderLabel.Text = Resources.ImportForbiddenHeader;
      }

      Label importCompleteTextLabel = new Label();
      importCompleteTextLabel.Location = new Point(35, 60);
      importCompleteTextLabel.Size = new Size(220, 15);
      importCompleteTextLabel.Font = new Font("Arial", 9.25F);
      importCompleteTextLabel.Text = Resources.ImportCompleteText;

      Label noteLabel = new Label();
      noteLabel.Location = new Point(35, 90);
      noteLabel.Size = new Size(200, 15);
      noteLabel.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      noteLabel.Text = Resources.NoteText;

      Label importCompleteInfoLabel = new Label();
      importCompleteInfoLabel.Location = new Point(35, 105);
      importCompleteInfoLabel.Size = new Size(250, 30);
      importCompleteInfoLabel.Font = new Font("Arial", 9.25F);
      importCompleteInfoLabel.Text = Resources.ImportCompleteInfo;

      Button finishButton = new Button();
      finishButton.Location = new Point(30, 287);
      finishButton.Size = new Size(72, 23);
      finishButton.Font = new Font("Arial", 9.25F);
      finishButton.Text = Resources.FinishText;
      finishButton.FlatStyle = FlatStyle.System;
      finishButton.Click += new EventHandler(finishButton_Click);


      if (this.googleEmailUploaderModel.FailedMailCount > 0) {
        importCompleteHeaderLabel.Text = Resources.ImportIncompleteHeader;
        importCompleteHeaderLabel.ForeColor = Color.Red;

        LinkLabel openLogLabel = new LinkLabel();
        openLogLabel.Location = new Point(35, 160);
        openLogLabel.Font = new Font("Arial", 9.25F);
        openLogLabel.AutoSize = true;
        openLogLabel.Text = Resources.OpenLogText;
        openLogLabel.Click += new EventHandler(this.openLogLabel_Click);
        this.Controls.Add(openLogLabel);

        Label openFileLogCompleteLabel = new Label();
        openFileLogCompleteLabel.Location = new Point(110, 160);
        openFileLogCompleteLabel.AutoSize = true;
        openFileLogCompleteLabel.Font = new Font("Arial", 9.25F);
        openFileLogCompleteLabel.Text = Resources.ToSeeDetailsText;

        this.Controls.Add(openFileLogCompleteLabel);
      }

      this.Controls.Add(importCompleteHeaderLabel);
      this.Controls.Add(importCompleteTextLabel);
      this.Controls.Add(noteLabel);
      this.Controls.Add(importCompleteInfoLabel);
      this.Controls.Add(finishButton);

      this.ActiveControl = finishButton;

      // Show the layout.
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    void openLogLabel_Click(object sender, EventArgs e) {
      string tempFilePath = Path.GetTempFileName();
      tempFilePath = Path.ChangeExtension(tempFilePath, ".txt");
      using (StreamWriter streamWriter = File.AppendText(tempFilePath)) {
        foreach (FolderModel folderModel in
            googleEmailUploaderModel.FlatFolderModelList) {
          if (folderModel.FailedMailData.Count == 0 ||
              !folderModel.IsSelected) {
            continue;
          }
          string folderString = string.Format(Resources.FolderTemplateText,
                                              folderModel.FullFolderPath);
          streamWriter.WriteLine(folderString);
          streamWriter.WriteLine(new string('=', folderString.Length));
          foreach (FailedMailDatum failedMailDatum
              in folderModel.FailedMailData) {
            streamWriter.WriteLine(failedMailDatum.MailHead);
            streamWriter.WriteLine(Resources.ReasonTemplate,
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
        this.Invoke(new VoidDelegate(this.UpdateStatistics));
      }
    }

    void UploadView_FormClosing(object sender, CancelEventArgs e) {
      this.UnhookModelEvents();
      this.googleEmailUploaderModel.AbortUpload();
      Application.DoEvents();
    }

    void notificationTryIcon_DoubleClick(object sender, EventArgs e) {
      this.WindowState = FormWindowState.Normal;
      this.notificationTrayIcon.Visible = false;
      this.ShowInTaskbar = true;
      this.Show();
    }

    void pauseResumeButton_Click(object sender, EventArgs e) {
      if (this.googleEmailUploaderModel.IsPaused) {
        this.pauseResumeButton.Text = Resources.PauseText;
        this.pauseResumeButton.ForeColor = Color.DarkRed;
        this.googleEmailUploaderModel.ResumeUpload();
      } else {
        this.pauseResumeButton.Text = Resources.ResumeText;
        this.pauseResumeButton.ForeColor = Color.DarkGreen;
        this.googleEmailUploaderModel.PauseUpload();
      }
    }

    void minimizeToTrayLinkLabel_Click(object sender, EventArgs e) {
      this.Hide();
      this.notificationTrayIcon.Visible = true;
      this.ShowInTaskbar = false;
      this.WindowState = FormWindowState.Minimized;
    }

    void abortButton_Click(object sender, EventArgs e) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        string pauseReasonText = Resources.AbortWaitingText;
        this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                    new object[] { pauseReasonText,
                        Color.FromArgb(247, 179, 36) });
      }
      this.pauseResumeButton.Enabled = false;
      this.googleEmailUploaderModel.AbortUpload();
    }

    void googleEmailUploaderModel_MailBatchFillingEvent(MailBatch mailBatch,
                                                        IMail mail) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.currentMailBatchSize = mailBatch.MailCount;
          string message = string.Format(Resources.ReadingMailsTemplate,
                                         mailBatch.MailCount);
          this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                      new object[] { message, Color.DarkGreen });
        }
      }
    }

    void googleEmailUploaderModel_MailBatchFilledEvent(MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          this.currentMailBatchSize = mailBatch.MailCount;
          string message = string.Format(Resources.ReadingMailsTemplate,
                                         mailBatch.MailCount);
          this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                      new object[] { message, Color.DarkGreen });
        }
      }
    }

    void googleEmailUploaderModel_MailBatchUploadTryStartEvent(
        MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        if (!this.googleEmailUploaderModel.IsPaused) {
          string message = string.Format(Resources.UploadingMailsTemplate,
                                         mailBatch.MailCount);
          this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                      new object[] { message, Color.DarkGreen });
        }
      }
    }

    void googleEmailUploaderModel_UploadPausedEvent(PauseReason pauseReason) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        string pauseResumeButtonText;
        Color color;
        if (pauseReason == PauseReason.Resuming) {
          string message = string.Format(Resources.ReadingMailsTemplate,
                                         this.currentMailBatchSize.ToString());
          this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                      new object[] { message, Color.DarkGreen});

          color = Color.DarkRed;
          pauseResumeButtonText = Resources.PauseText;
        } else {
          string pauseReasonText =
              UploadView.GetProperPauseMessage(pauseReason, 0);
          this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                      new object[] { pauseReasonText, Color.DarkRed });

          color = Color.DarkGreen;
          pauseResumeButtonText = Resources.ResumeText;
        }
        this.Invoke(
            new StringColorDelegate(this.SetPauseResumeButtonText),
            new object[] { pauseResumeButtonText, color });
      }
    }

    void googleEmailUploaderModel_PauseCountDownEvent(PauseReason pauseReason,
                                            int remainingCount) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        string pauseReasonText =
            UploadView.GetProperPauseMessage(pauseReason, remainingCount);
        this.Invoke(new StringColorDelegate(this.UpdateMessageLabel),
                    new object[] { pauseReasonText, Color.DarkRed });
      }
    }

    void googleEmailUploaderModel_MailBatchUploadedEvent(MailBatch mailBatch) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.Invoke(new VoidDelegate(this.UpdateStatistics));
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

    void UpdateStatistics() {
      if (this.IsHandleCreated && !this.IsDisposed) {
        TimeSpan timeSpan = this.googleEmailUploaderModel.UploadTimeRemaining;
        this.uploadedMailCountLabel.Text =
            string.Format(Resources.UploadedMailsTemplateText,
                          this.googleEmailUploaderModel.
                              UploadedMailCount.ToString(),
                          this.googleEmailUploaderModel.
                              SelectedMailCount.ToString(),
                          timeSpan.ToString());
        if (this.googleEmailUploaderModel.FailedMailCount > 0) {
          this.failedMailCountLabel.Text =
              string.Format(Resources.FailedMailsTemplateText,
                            this.googleEmailUploaderModel.
                                FailedMailCount.ToString());
        }

        int newValue =
            (int)(this.googleEmailUploaderModel.UploadedMailCount
                + this.googleEmailUploaderModel.FailedMailCount);
        if (newValue >= this.uploadProgressBar.Maximum) {
          this.uploadProgressBar.Maximum = newValue;
        }
        this.uploadProgressBar.Value = newValue;
        FolderModel folderModel =
            this.googleEmailUploaderModel.CurrentFolderModel;
        if (folderModel != null) {
          this.importInfo.Text =
              string.Format(
                  Resources.ImportInfo,
                  folderModel.ClientModel.DisplayName);
        }
      }
    }

    void UpdateMessageLabel(string messageText, Color color) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.messageLabel.Text = messageText;
        this.messageLabel.ForeColor = color;
      }
    }

    static string GetProperPauseMessage(PauseReason pauseReason,
                                        int remainingCount) {
      string pauseReasonText;
      switch (pauseReason) {
        case PauseReason.UserAction:
          pauseReasonText = Resources.PauseUserAction;
          break;
        case PauseReason.ConnectionFailures:
          pauseReasonText =
              string.Format(Resources.PauseConnectionFailuresTemplate,
                            remainingCount);
          break;
        case PauseReason.ServiceUnavailable:
          pauseReasonText =
              string.Format(Resources.ServiceUnavailableTemplate,
                            remainingCount);
          break;
        case PauseReason.ServerInternalError:
          pauseReasonText =
              string.Format(Resources.ServerInternalErrorTemplate,
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

    void SetPauseResumeButtonText(string text,
                                  Color color) {
      if (this.IsHandleCreated && !this.IsDisposed) {
        this.pauseResumeButton.Text = text;
        this.pauseResumeButton.ForeColor = color;
      }
    }

    void ForceShow() {
      // Set the WindowState to normal if the form is minimized.
      if (this.WindowState == FormWindowState.Minimized) {
        this.WindowState = FormWindowState.Normal;
        this.Show();
      }
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