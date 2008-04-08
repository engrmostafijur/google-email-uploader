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
    Button signInButton;
    Label errorMessageLabel;
    Label captchaImage;
    TextBox captchaTextBox;
    Label captchaInstructions;
    LinkLabel cantSigninLinkLabel;
    bool isCaptchaEnabled;
    string captchaToken;
    LoginViewResult result;

    internal LoginView(GoogleEmailUploaderModel googleEmailUploaderModel,
                       bool wasRestarted) {
      this.googleEmailUploaderModel = googleEmailUploaderModel;
      this.StartPosition = FormStartPosition.CenterScreen;
      this.Text = Resources.GoogleEmailUploaderAppNameText;
      this.Icon = Resources.GMailIcon;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;

      this.BackColor = Color.White;
      this.Size = new Size(530, 370);

      // We have to take care of the case when the SignIn View is restarted.
      // In that case we will not display the introduction dialog but will 
      // directly jump to the sign in Dialog.
      if (!wasRestarted) {
        this.ShowInstructionsScreen();
      } else {
        this.ShowSigninScreen();
      }

      this.result = LoginViewResult.Cancel;
      this.googleEmailUploaderModel.LoadingClientsEvent +=
          new VoidDelegate(this.googleEmailUploaderModel_LoadingClientsEvent);
    }

    void ShowInstructionsScreen() {
      // UploadToGmail Label.
      Label uploadToGmail = new Label();
      uploadToGmail.Location = new Point(35, 23);
      uploadToGmail.AutoSize = true;
      uploadToGmail.Font = new Font("Arial", 11F, FontStyle.Bold);
      uploadToGmail.BackColor = Program.TopStripBackColor;
      uploadToGmail.Text = Resources.UploadToGmailText;

      // Instruction Label.
      Label instructionLabel = new Label();
      instructionLabel.Location = new Point(35, 60);
      instructionLabel.Size = new Size(260, 30);
      instructionLabel.Font = new Font("Arial", 9.25F);
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

      this.Controls.Add(uploadToGmail);
      this.Controls.Add(instructionLabel);
      this.Controls.Add(microsoftOutlookLabel);
      this.Controls.Add(outlookExpressLabel);
      this.Controls.Add(thunderbirdLabel);
      this.Controls.Add(nextButton);
      this.ActiveControl = nextButton;
      this.AcceptButton = nextButton;
    }

    void ShowSigninScreen() {
      this.Controls.Clear();

      // SignInIntroText Label.
      Label signInIntroText = new Label();
      signInIntroText.Location = new Point(25, 60);
      signInIntroText.Size = new Size(250, 30);
      signInIntroText.Font = new Font("Arial", 9.25F);
      signInIntroText.Text = Resources.SignInIntroText;

      this.loginPanel = new Panel();
      this.loginPanel.BackgroundImage = Resources.SignInBackgroundImage;
      this.loginPanel.Location = new Point(40, 110);
      this.loginPanel.Size = new Size(242, 149);

      // SignInInfoText Label.
      Label signInInfoText = new Label();
      signInInfoText.Location = new Point(75, 5);
      signInInfoText.AutoSize = true;
      signInInfoText.Font = new Font("Arial", 9.25F);
      signInInfoText.Text = Resources.SignInInfoText;
      signInInfoText.BackColor = Program.SigninBackColor;

      // Google logo
      PictureBox googleLogo = new PictureBox();
      googleLogo.BackgroundImage = Resources.GoogleLogoImage;
      googleLogo.Location = new Point(60, 25);
      googleLogo.Size = new Size(58, 26);

      // Account Label.
      Label accountText = new Label();
      accountText.Location = new Point(120, 25);
      accountText.AutoSize = true;
      accountText.Font = new Font("Arial", 13F, FontStyle.Bold);
      accountText.Text = Resources.AccountText;
      accountText.BackColor = Program.SigninBackColor;

      // EMail Label
      Label emailLabel = new Label();
      emailLabel.Location = new Point(10, 60);
      emailLabel.Size = new Size(68, 13);
      emailLabel.Font = new Font("Arial", 9.25F);
      emailLabel.Text = Resources.EmailText;
      emailLabel.BackColor = Program.SigninBackColor;
      emailLabel.TextAlign = ContentAlignment.MiddleRight;

      // EMail Text Box
      this.emailTextBox = new TextBox();
      this.emailTextBox.Location = new Point(80, 55);
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
      passwordLabel.Location = new Point(10, 90);
      passwordLabel.Size = new Size(68, 13);
      passwordLabel.Text = Resources.PasswordText;
      passwordLabel.Font = new Font("Arial", 9.25F);
      passwordLabel.BackColor = Program.SigninBackColor;
      passwordLabel.TextAlign = ContentAlignment.MiddleRight;

      // Password Text Box
      this.passwordTextBox = new TextBox();
      this.passwordTextBox.Location = new Point(80, 85);
      this.passwordTextBox.Size = new Size(140, 18);
      this.passwordTextBox.TabIndex = 1;
      this.passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
      this.passwordTextBox.Font = new Font("Arial", 9.25F);
      this.passwordTextBox.Text = string.Empty;
      this.passwordTextBox.PasswordChar = '*';
      this.passwordTextBox.TextChanged +=
          new EventHandler(this.emailPasswordTextBox_TextChanged);
      this.passwordTextBox.GotFocus +=
          new EventHandler(this.passwordTextBox_GotFocus);

      // Error Message Label
      this.errorMessageLabel = new Label();
      this.errorMessageLabel.Location = new Point(5, 105);
      this.errorMessageLabel.Size = new Size(232, 40);
      this.errorMessageLabel.Text = string.Empty;
      this.errorMessageLabel.Font = new Font("Arial", 9F);
      this.errorMessageLabel.TextAlign = ContentAlignment.MiddleCenter;
      this.errorMessageLabel.BackColor = Program.SigninBackColor;

      // Captcha Image Label.
      this.captchaImage = new Label();
      this.captchaImage.Location = new Point(25, 160);
      this.captchaImage.Size = new Size(200, 70);
      this.captchaImage.Hide();

      // Captcha Box
      this.captchaTextBox = new TextBox();
      this.captchaTextBox.Location = new Point(45, 240);
      this.captchaTextBox.Size = new Size(155, 20);
      this.captchaTextBox.Hide();
      this.captchaTextBox.BorderStyle = BorderStyle.FixedSingle;
      this.captchaTextBox.Font = new Font("Arial", 9.25F);

      // Captcha Instruction label.
      this.captchaInstructions = new Label();
      this.captchaInstructions.Location = new Point(20, 270);
      this.captchaInstructions.Size = new Size(220, 45);
      this.captchaInstructions.Hide();
      this.captchaInstructions.Font = new Font("Arial", 9.25F);
      this.captchaInstructions.Text = Resources.CaptchaInstructionsText;
      this.captchaInstructions.BackColor = Program.SigninBackColor;

      this.loginPanel.Controls.Add(signInInfoText);
      this.loginPanel.Controls.Add(googleLogo);
      this.loginPanel.Controls.Add(accountText);
      this.loginPanel.Controls.Add(emailLabel);
      this.loginPanel.Controls.Add(emailTextBox);
      this.loginPanel.Controls.Add(passwordLabel);
      this.loginPanel.Controls.Add(passwordTextBox);
      this.loginPanel.Controls.Add(errorMessageLabel);
      this.loginPanel.Controls.Add(this.captchaImage);
      this.loginPanel.Controls.Add(this.captchaTextBox);
      this.loginPanel.Controls.Add(this.captchaInstructions);

      this.cantSigninLinkLabel = new LinkLabel();
      this.cantSigninLinkLabel.Location = new Point(40, 262);
      this.cantSigninLinkLabel.Size = new Size(240, 20);
      this.cantSigninLinkLabel.TabIndex = 2;
      this.cantSigninLinkLabel.Text = Resources.CantSigninText;
      this.cantSigninLinkLabel.Font = new Font("Arial", 9.25F);
      this.cantSigninLinkLabel.TextAlign = ContentAlignment.MiddleCenter;
      this.cantSigninLinkLabel.Click +=
          new EventHandler(this.cantSigninLinkLabel_Click);

      // Sign In Button
      this.signInButton = new Button();
      this.signInButton.Location = new Point(30, 287);
      this.signInButton.Size = new Size(72, 23);
      this.signInButton.TabIndex = 2;
      this.signInButton.Text = Resources.SigninText;
      this.signInButton.Font = new Font("Arial", 9.25F);
      this.signInButton.Click += new EventHandler(this.signInButton_Click);
      this.signInButton.Enabled = false;
      this.signInButton.FlatStyle = FlatStyle.System;

      Program.AddHeaderStrip(0, this.Controls);

      this.Controls.Add(this.loginPanel);
      this.Controls.Add(signInIntroText);
      this.Controls.Add(this.signInButton);
      this.Controls.Add(this.cantSigninLinkLabel);

      this.ActiveControl = this.emailTextBox;
      this.AcceptButton = this.signInButton;
    }

    void cantSigninLinkLabel_Click(object sender, EventArgs e) {
      try {
        Process.Start(Resources.CantSigninHelpUrl);
      } catch {
        // ignore any error that happens because we cant navigate to the
        // help url
      }
    }

    void passwordTextBox_GotFocus(object sender, EventArgs e) {
      this.passwordTextBox.Text = string.Empty;
    }

    void nextButton_Click(object sender, EventArgs e) {
      if (!Program.EnsureClientsNotRunning(
          GoogleEmailUploaderModel.ClientFactories)) {
        return;
      }
      this.ShowSigninScreen();
    }

    bool IsFormLoginValid() {
      if (this.emailTextBox.Text.Length != 0 &&
          this.passwordTextBox.Text.Length != 0) {
        string blankString = new string(' ', this.passwordTextBox.Text.Length);
        if (blankString == this.passwordTextBox.Text) {
          return false;
        }
        return true;
      }
      return false;
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
      this.errorMessageLabel.ForeColor = Color.Black;
      this.errorMessageLabel.Text = Resources.LoadingClientsText;
      this.errorMessageLabel.Refresh();
    }

    void signInButton_Click(object sender, EventArgs e) {
      string login = this.emailTextBox.Text;
      string password = this.passwordTextBox.Text;

      this.errorMessageLabel.ForeColor = Color.Black;
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
        UploadResult batchUploadResult =
            this.googleEmailUploaderModel.TestEmailUpload();
        if (batchUploadResult == UploadResult.Forbidden) {
          MessageBox.Show("You are not authorized to use this feature",
                          "Forbidden");
          this.result = LoginViewResult.UploadForbidden;
          this.Close();
          return;
        }

        this.errorMessageLabel.ForeColor = Color.Black;
        this.errorMessageLabel.Text = Resources.SignedInInfoText;
        this.result = LoginViewResult.SignedIn;
        this.Close();
      } else if (authResponse.AuthenticationResult ==
          AuthenticationResultKind.CAPTCHARequired) {
        // Modify all the other labels, panels and layouts accordingly.
        this.cantSigninLinkLabel.Location = new Point(40, 436);
        this.signInButton.Location = new Point(30, 461);
        this.loginPanel.Height = 323;
        this.Height = 544;

        // Modify the error message label.
        this.errorMessageLabel.Text = Resources.CaptchaErrorMessageText;
        this.errorMessageLabel.ForeColor = Color.DarkRed;
        this.errorMessageLabel.Refresh();

        this.BackgroundImage =
            Resources.GoogleEmailUploaderWithCaptchaBackgroundImage;
        this.loginPanel.BackgroundImage =
            Resources.SignInWithCaptchaBackgroundImage;

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

        this.isCaptchaEnabled = true;
      } else {
        if (this.isCaptchaEnabled) {
          this.captchaImage.Hide();
          this.captchaTextBox.Hide();
          this.captchaTextBox.Clear();
          this.captchaImage.Hide();

          this.loginPanel.Height = 149;
          this.Height = 370;
          this.cantSigninLinkLabel.Location = new Point(30, 262);
          this.signInButton.Location = new Point(30, 287);
          this.emailTextBox.TabIndex = 2;
          this.passwordTextBox.TabIndex = 3;
          this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
          this.loginPanel.BackgroundImage = Resources.SignInBackgroundImage;
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