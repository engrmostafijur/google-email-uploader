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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GoogleEmailUploader {

  enum LoginViewResult {
    Cancel,
    UploadForbidden,
    SignedIn,
  }

  class LoginView : Form {
    readonly GoogleEmailUploaderModel googleEmailUploaderModel;

    Panel loginPanel;
    TextBox emailTextBox;
    TextBox passwordTextBox;
    Button cancelButton;
    Button signInButton;
    Label errorMessageLabel;
    Label captchaImage;
    TextBox captchaTextBox;
    Label captchaInstructions;
    bool isCaptchaEnabled;
    string captchaToken;
    LoginViewResult result;

    internal LoginView(GoogleEmailUploaderModel googleEmailUploaderModel,
                       bool wasRestarted) {
      this.googleEmailUploaderModel = googleEmailUploaderModel;
      this.StartPosition = FormStartPosition.CenterScreen;
      this.Text = Resources.GoogleEmailUploaderAppName;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;

      this.BackColor = Color.White;
      this.Size = new Size(480, 370);

      // We have to take care of the case when the SignIn View is restarted.
      // In that case we will not display the introduction dialog but will 
      // directly jump to the sign in Dialog.
      this.SuspendLayout();
      if (!wasRestarted) {
        this.InitializeComponent();
      } else {
        this.nextButton_Click(null, EventArgs.Empty);
      }
      this.ResumeLayout(false);
      this.PerformLayout();

      this.result = LoginViewResult.Cancel;
      this.googleEmailUploaderModel.LoadingClientsEvent +=
          new VoidDelegate(this.googleEmailUploaderModel_LoadingClientsEvent);
    }

    void InitializeComponent() {
      // ImportToGmail Label.
      Label importToGmail = new Label();
      importToGmail.Location = new Point(35, 23);
      importToGmail.Size = new Size(200, 20);
      importToGmail.Font = new Font("Arial", 11F, FontStyle.Bold);
      importToGmail.BackColor = Color.FromArgb(229, 240, 254);
      importToGmail.Text = Resources.ImportToGmailText;

      // Instruction Label.
      Label instructionLabel = new Label();
      instructionLabel.Location = new Point(35, 60);
      instructionLabel.Size = new Size(260, 30);
      instructionLabel.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      instructionLabel.Text = Resources.InstructionsText;

      // Microsoft Outlook Label.
      Label microsoftOutlookLabel = new Label();
      microsoftOutlookLabel.Location = new Point(50, 105);
      microsoftOutlookLabel.Size = new Size(150, 15);
      microsoftOutlookLabel.Font = new Font("Arial", 9.25F);
      microsoftOutlookLabel.Text = Resources.MicrosoftOutlookText;

      // Microsoft Outlook Label.
      Label outlookExpressLabel = new Label();
      outlookExpressLabel.Location = new Point(50, 120);
      outlookExpressLabel.Size = new Size(150, 15);
      outlookExpressLabel.Font = new Font("Arial", 9.25F);
      outlookExpressLabel.Text = Resources.OutlookExpressText;

      // Thunderbird Label.
      Label thunderbirdLabel = new Label();
      thunderbirdLabel.Location = new Point(50, 135);
      thunderbirdLabel.Size = new Size(150, 15);
      thunderbirdLabel.Font = new Font("Arial", 9.25F);
      thunderbirdLabel.Text = Resources.ThunderbirdText;

      // Next Button.
      Button nextButton = new Button();
      nextButton.Location = new Point(30, 287);
      nextButton.Size = new Size(72, 23);
      nextButton.Font = new Font("Arial", 9.25F);
      nextButton.Text = Resources.NextText;
      nextButton.Click += new EventHandler(this.nextButton_Click);
      nextButton.FlatStyle = FlatStyle.System;

      // Cancel Button.
      this.cancelButton = new Button();
      this.cancelButton.Location = new Point(120, 287);
      this.cancelButton.Size = new Size(72, 23);
      this.cancelButton.Font = new Font("Arial", 9.25F);
      this.cancelButton.Text = Resources.CancelText;
      this.cancelButton.FlatStyle = FlatStyle.System;
      this.cancelButton.Click += new EventHandler(this.cancelButton_Click);

      this.Controls.Add(importToGmail);
      this.Controls.Add(instructionLabel);
      this.Controls.Add(microsoftOutlookLabel);
      this.Controls.Add(outlookExpressLabel);
      this.Controls.Add(thunderbirdLabel);
      this.Controls.Add(nextButton);
      this.Controls.Add(this.cancelButton);
      this.ActiveControl = nextButton;
      this.AcceptButton = nextButton;
    }

    void nextButton_Click(object sender, EventArgs e) {
      this.Controls.Clear();

      // SignInIntroText Label.
      Label signInIntroText = new Label();
      signInIntroText.Location = new Point(35, 60);
      signInIntroText.Size = new Size(250, 30);
      signInIntroText.Font = new Font("Arial", 9.25F);
      signInIntroText.Text = Resources.SignInIntroText;

      this.loginPanel = new Panel();
      this.loginPanel.BackgroundImage = Resources.SignInPanelBackgroundImage;
      this.loginPanel.Location = new Point(20, 100);
      this.loginPanel.Size = new Size(260, 170);

      // SignInInfoText Label.
      Label signInInfoText = new Label();
      signInInfoText.Location = new Point(90, 20);
      signInInfoText.Size = new Size(100, 15);
      signInInfoText.Font = new Font("Arial", 9.25F);
      signInInfoText.Text = Resources.SignInInfoText;
      signInInfoText.BackColor = Color.FromArgb(232, 238, 250);

      // EMail Label
      Label emailLabel = new Label();
      emailLabel.Location = new Point(20, 75);
      emailLabel.Size = new Size(70, 13);
      emailLabel.Font = new Font("Arial", 9.25F);
      emailLabel.Text = Resources.EmailText;
      emailLabel.BackColor = Color.FromArgb(232, 238, 250);

      // EMail Text Box
      this.emailTextBox = new TextBox();
      this.emailTextBox.Location = new Point(90, 70);
      this.emailTextBox.Size = new Size(140, 18);
      this.emailTextBox.TabIndex = 0;
      this.emailTextBox.BorderStyle = BorderStyle.FixedSingle;
      this.emailTextBox.Font = new Font("Arial", 9.25F);
      this.emailTextBox.Text = string.Empty;
      this.emailTextBox.TextChanged +=
          new EventHandler(this.emailPasswordTextBox_TextChanged);
      this.emailTextBox.LostFocus +=
          new EventHandler(this.emailTextBox_LostFocus);

      // Password Label
      Label passwordLabel = new Label();
      passwordLabel.Location = new Point(22, 105);
      passwordLabel.Size = new Size(65, 13);
      passwordLabel.Text = Resources.PasswordText;
      passwordLabel.Font = new Font("Arial", 9.25F);
      passwordLabel.BackColor = Color.FromArgb(232, 238, 250);

      // Password Text Box
      this.passwordTextBox = new TextBox();
      this.passwordTextBox.Location = new Point(90, 100);
      this.passwordTextBox.Size = new Size(140, 18);
      this.passwordTextBox.TabIndex = 1;
      this.passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
      this.passwordTextBox.Font = new Font("Arial", 9.25F);
      this.passwordTextBox.Text = string.Empty;
      this.passwordTextBox.PasswordChar = '*';
      this.passwordTextBox.TextChanged +=
          new EventHandler(this.emailPasswordTextBox_TextChanged);

      // Error Message Label
      this.errorMessageLabel = new Label();
      this.errorMessageLabel.Location = new Point(20, 125);
      this.errorMessageLabel.Size = new Size(220, 25);
      this.errorMessageLabel.Text = string.Empty;
      this.errorMessageLabel.TextAlign = ContentAlignment.MiddleCenter;
      this.errorMessageLabel.ForeColor = Color.DarkRed;
      this.errorMessageLabel.BackColor = Color.FromArgb(232, 238, 250);

      // Captcha Image Label.
      this.captchaImage = new Label();
      this.captchaImage.Location = new Point(35, 170);
      this.captchaImage.Size = new Size(200, 70);
      this.captchaImage.Hide();

      // Captcha Box
      this.captchaTextBox = new TextBox();
      this.captchaTextBox.Location = new Point(55, 250);
      this.captchaTextBox.Size = new Size(155, 20);
      this.captchaTextBox.Hide();
      this.captchaTextBox.BorderStyle = BorderStyle.FixedSingle;
      this.captchaTextBox.Font = new Font("Arial", 9.25F);

      // Captcha Instruction label.
      this.captchaInstructions = new Label();
      this.captchaInstructions.Location = new Point(30, 280);
      this.captchaInstructions.Size = new Size(220, 45);
      this.captchaInstructions.Hide();
      this.captchaInstructions.Font = new Font("Arial", 9.25F);
      this.captchaInstructions.Text = Resources.CaptchaInstructionsText;
      this.captchaInstructions.BackColor = Color.FromArgb(232, 238, 250);

      this.loginPanel.Controls.Add(signInInfoText);
      this.loginPanel.Controls.Add(emailLabel);
      this.loginPanel.Controls.Add(emailTextBox);
      this.loginPanel.Controls.Add(passwordLabel);
      this.loginPanel.Controls.Add(passwordTextBox);
      this.loginPanel.Controls.Add(errorMessageLabel);
      this.loginPanel.Controls.Add(this.captchaImage);
      this.loginPanel.Controls.Add(this.captchaTextBox);
      this.loginPanel.Controls.Add(this.captchaInstructions);
      
      // Sign In Button
      this.signInButton = new Button();
      this.signInButton.Location = new Point(30, 287);
      this.signInButton.Size = new Size(72, 23);
      this.signInButton.TabIndex = 2;
      this.signInButton.Text = Resources.SigninText;
      this.signInButton.Font = new Font("Arial", 9.25F);
      this.signInButton.BackColor = SystemColors.Control;
      this.signInButton.Click += new EventHandler(this.signInButton_Click);
      this.signInButton.Enabled = false;

      // Cancel Button
      this.cancelButton = new Button();
      this.cancelButton.Location = new Point(120, 287);
      this.cancelButton.Size = new Size(72, 23);
      this.cancelButton.Font = new Font("Arial", 9.25F);
      this.cancelButton.Text = Resources.CancelText;
      this.cancelButton.TabIndex = 3;
      this.cancelButton.FlatStyle = FlatStyle.System;
      this.cancelButton.Click += new EventHandler(this.cancelButton_Click);

      this.createSignInHeader();

      this.Controls.Add(this.loginPanel);
      this.Controls.Add(signInIntroText);
      this.Controls.Add(this.signInButton);
      this.Controls.Add(this.cancelButton);

      this.ActiveControl = this.emailTextBox;
      this.AcceptButton = this.signInButton;
    }

    void createSignInHeader() {
      Font defaultFont = new Font("Arial", 9.5F);
      Font separatorFont = new Font("Arial", 12F);
      Color backColor = Color.FromArgb(229, 240, 254);
      Color foreColor = Color.FromArgb(166, 166, 166);

      Label signInLabel = new Label();
      signInLabel.Location = new Point(25, 24);
      signInLabel.Size = new Size(49, 16);
      signInLabel.Font = new Font("Arial", 9.5F, FontStyle.Bold);
      signInLabel.BackColor = backColor;
      signInLabel.Text = Resources.SignInHeaderText;

      Label separator1 = new Label();
      separator1.Location = new Point(72, 23);
      separator1.Size = new Size(11, 15);
      separator1.Font = separatorFont;
      separator1.BackColor = backColor;
      separator1.ForeColor = foreColor;
      separator1.Text = Resources.SeparatorText;

      Label selectEmailLabel = new Label();
      selectEmailLabel.Location = new Point(85, 24);
      selectEmailLabel.Size = new Size(72, 15);
      selectEmailLabel.Font = defaultFont;
      selectEmailLabel.BackColor = backColor;
      selectEmailLabel.ForeColor = foreColor;
      selectEmailLabel.Text = Resources.SelectEmailHeaderText;

      Label separator2 = new Label();
      separator2.Location = new Point(154, 23);
      separator2.Size = new Size(11, 15);
      separator2.Font = separatorFont;
      separator2.BackColor = backColor;
      separator2.ForeColor = foreColor;
      separator2.Text = Resources.SeparatorText;

      Label labelLabel = new Label();
      labelLabel.Location = new Point(167, 24);
      labelLabel.Size = new Size(43, 15);
      labelLabel.ForeColor = foreColor;
      labelLabel.BackColor = backColor;
      labelLabel.Font = defaultFont;
      labelLabel.Text = Resources.LabelHeaderText;

      Label separator3 = new Label();
      separator3.Location = new Point(207, 23);
      separator3.Size = new Size(11, 15);
      separator3.Font = separatorFont;
      separator3.BackColor = backColor;
      separator3.ForeColor = foreColor;
      separator3.Text = Resources.SeparatorText;

      Label importLabel = new Label();
      importLabel.Location = new Point(221, 24);
      importLabel.Size = new Size(42, 15);
      importLabel.ForeColor = foreColor;
      importLabel.BackColor = backColor;
      importLabel.Font = defaultFont;
      importLabel.Text = Resources.ImportHeaderText;

      this.Controls.Add(signInLabel);
      this.Controls.Add(separator1);
      this.Controls.Add(selectEmailLabel);
      this.Controls.Add(separator2);
      this.Controls.Add(labelLabel);
      this.Controls.Add(separator3);
      this.Controls.Add(importLabel);
    }

    bool IsFormLoginValid() {
      if (this.emailTextBox.Text.Length != 0 &&
          this.passwordTextBox.Text.Length != 0) {
        return true;
      }
      return false;
    }

    void cancelButton_Click(object sender, EventArgs e) {
      this.Close();
    }

    void emailPasswordTextBox_TextChanged(object sender, EventArgs e) {
      if (this.IsFormLoginValid()) {
        signInButton.Enabled = true;
      } else {
        signInButton.Enabled = false;
      }
    }

    void emailTextBox_LostFocus(object sender, EventArgs e) {
      string login = this.emailTextBox.Text;
      login = login.Trim();
      if (login == null || login.Length == 0) {
        return;
      }
      int atPos = login.IndexOf('@');
      if (atPos == -1) {
        this.emailTextBox.Text = login + "@gmail.com";
      } else if (atPos == login.Length - 1) {
        this.emailTextBox.Text = login + "gmail.com";
      }
      this.emailTextBox.Text = this.emailTextBox.Text.Trim();
    }

    void googleEmailUploaderModel_LoadingClientsEvent() {
      this.errorMessageLabel.ForeColor = Color.DarkGreen;
      this.errorMessageLabel.Text = Resources.LoadingClientsText;
      this.errorMessageLabel.Refresh();
    }

    void signInButton_Click(object sender, EventArgs e) {
      string login = this.emailTextBox.Text;
      string password = this.passwordTextBox.Text;

      this.errorMessageLabel.Text = Resources.SigningInInfoText;
      this.errorMessageLabel.Refresh();

      // If captcha is displayed on the screen, then we also need to send the
      // captcha text also.
      AuthenticationResponse authResponse;
      if (this.isCaptchaEnabled) {
        string captcha = this.captchaTextBox.Text;
        authResponse =
            this.googleEmailUploaderModel.SignInCAPTCHA(
                login,
                password,
                captchaToken,
                captcha);
      } else {
        authResponse = this.googleEmailUploaderModel.SignIn(login, password);
      }

      if (authResponse.AuthenticationResult ==
          AuthenticationResultKind.Authenticated) {
        BatchUploadResult batchUploadResult =
            this.googleEmailUploaderModel.TestUpload();
        if (batchUploadResult == BatchUploadResult.Forbidden) {
          MessageBox.Show("You are not authorized to use this feature",
                          "Forbidden");
          this.result = LoginViewResult.UploadForbidden;
          this.Close();
          return;
        }

        this.errorMessageLabel.ForeColor = Color.DarkGreen;
        this.errorMessageLabel.Text = Resources.SignedInInfoText;
        this.result = LoginViewResult.SignedIn;
        this.Close();
      } else if (authResponse.AuthenticationResult ==
          AuthenticationResultKind.CAPTCHARequired) {
        this.BackgroundImage =
            Resources.GoogleEmailUploaderWithCaptchaBackgroundImage;
        this.loginPanel.BackgroundImage =
            Resources.SignInPanelWithCaptchaBackgroundImage;

        // Modify the error message label.
        this.errorMessageLabel.Location = new Point(20, 130);
        this.errorMessageLabel.Size = new Size(230, 30);
        this.errorMessageLabel.Text = Resources.CaptchaErrorMessageText;

        // There are a few cases to consider here
        // 1. When there was no captcha image present: In that case we just
        //    initialize the captchaImage label and set isCaptchaEnabled
        //    boolean to true.
        // 2. There was previously a captha image present: In this case we 
        //    refresh the previous captcha image and reinitialize it with the
        //    new image returned from the captchaURL.
        this.captchaToken = authResponse.CAPTCHAToken;
        this.captchaImage.Image = authResponse.CAPTCHAImage;
        if (this.isCaptchaEnabled) {
          this.captchaImage.Refresh();
        }
        this.captchaImage.Show();
        this.captchaTextBox.Clear();
        this.captchaTextBox.Show();
        this.captchaTextBox.TabIndex = 2;
        this.captchaInstructions.Show();

        // Modify all the other labels, panels and layouts accordingly.
        this.cancelButton.Location = new Point(120, 460);
        this.signInButton.Location = new Point(30, 460);
        this.BackgroundImage =
            Resources.GoogleEmailUploaderWithCaptchaBackgroundImage;
        this.loginPanel.Height = 340;
        this.Height = 550;

        this.isCaptchaEnabled = true;
        this.errorMessageLabel.ForeColor = Color.DarkRed;
        this.errorMessageLabel.Refresh();
      } else {
        if (this.isCaptchaEnabled) {
          this.captchaImage.Hide();
          this.captchaTextBox.Hide();
          this.captchaTextBox.Clear();
          this.captchaImage.Hide();

          this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
          this.Height = 370;
          this.loginPanel.Height = 170;
          this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
          this.loginPanel.BackgroundImage =
              Resources.SignInPanelBackgroundImage;
          this.errorMessageLabel.Location = new Point(20, 125);
          this.cancelButton.Location = new Point(120, 287);
          this.signInButton.Location = new Point(30, 287);
          this.emailTextBox.TabIndex = 2;
          this.passwordTextBox.TabIndex = 3;
        }
        if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.BadAuthentication) {
          this.errorMessageLabel.Text = Resources.SignedInTryAgainInfoText;
        } else if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.ConnectionFailure) {
          this.errorMessageLabel.Text = Resources.SignedInConnectionFailureText;
        } else if (authResponse.AuthenticationResult ==
            AuthenticationResultKind.TimeOut) {
          this.errorMessageLabel.Text = Resources.SignedInTimeoutText;
        } else {
          this.errorMessageLabel.Text = Resources.SignedInUnknownText;
        }
        this.errorMessageLabel.ForeColor = Color.DarkRed;
        this.errorMessageLabel.Refresh();
      }
    }

    protected override void WndProc(ref Message message) {
      if (message.Msg == Program.MessageId) {
        // Set the WindowState to normal if the form is minimized.
        if (this.WindowState == FormWindowState.Minimized) {
          this.Show();
          this.WindowState = FormWindowState.Normal;
        }
        // Activate the form.
        this.Activate();
        this.Focus();
      } else {
        base.WndProc(ref message);
      }
    }

    internal LoginViewResult Result {
      get {
        return this.result;
      }
    }
  }
}