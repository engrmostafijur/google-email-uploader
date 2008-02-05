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
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GoogleEmailUploader {

  enum SelectViewResult {
    Cancel,
    Closed,
    Restart,
    Upload,
  }

  class GoogleEmailUploaderTreeNode : TreeNode {
    internal readonly TreeNodeModel TreeNodeModel;

    internal GoogleEmailUploaderTreeNode(TreeNodeModel treeNodeModel) {
      this.TreeNodeModel = treeNodeModel;
      FolderModel folderModel = treeNodeModel as FolderModel;
      if (folderModel != null) {
        this.Text =
            treeNodeModel.DisplayName + " ["
                + folderModel.Folder.MailCount + "]";
      } else {
        this.Text = treeNodeModel.DisplayName;
      }
      this.Checked = treeNodeModel.IsSelected;
      foreach (TreeNodeModel childTreeNodeModel in treeNodeModel.Children) {
        GoogleEmailUploaderTreeNode childTreeNodeView =
            new GoogleEmailUploaderTreeNode(childTreeNodeModel);
        this.Nodes.Add(childTreeNodeView);
      }
    }

    void ChangeCheckedStateRec(bool isChecked) {
      this.TreeNodeModel.IsSelected = isChecked;
      this.Checked = isChecked;
      foreach (GoogleEmailUploaderTreeNode childTreeViewNode in this.Nodes) {
        childTreeViewNode.ChangeCheckedStateRec(isChecked);
      }
    }

    internal void CustomChangeCheckedState(bool isChecked) {
      this.ChangeCheckedStateRec(isChecked);
      if (isChecked) {
        GoogleEmailUploaderTreeNode parentIter =
            this.Parent as GoogleEmailUploaderTreeNode;
        while (parentIter != null) {
          parentIter.TreeNodeModel.IsSelected = true;
          parentIter.Checked = true;
          parentIter = parentIter.Parent as GoogleEmailUploaderTreeNode;
        }
      }
    }
  }

  class GoogleEmailUploaderTreeView : TreeView {
    internal bool SuspendCheckEvents;

    // This is to prevent infinite recursion because we are responding to the
    // after check event in the form.
    protected override void OnAfterCheck(TreeViewEventArgs e) {
      if (!this.SuspendCheckEvents) {
        base.OnAfterCheck(e);
      }
    }
  }

  class GoogleEmailUploaderCheckBox : CheckBox {
    internal GoogleEmailUploaderTreeNode AssociatedTreeNode;

    internal GoogleEmailUploaderCheckBox(
        GoogleEmailUploaderTreeNode associatedTreeNode) {
      this.AssociatedTreeNode = associatedTreeNode;
    }
  }

  class SelectView : Form {
    readonly GoogleEmailUploaderModel googleEmailUploaderModel;

    Button cancelButton;
    Button nextButton;
    Button backButton;
    bool isCustomized;
    Label selectionInfoLabel;

    // Non-customized dialog
    Label loggedInInfoLable;
    Label emailIDLabel;
    Label chooseEmailProgramsLabel;
    LinkLabel customizeLabel;
    ArrayList clientCheckBoxes;

    // Customized dialog
    Label selectEmailLabel;
    LinkLabel addAnotherMailbox;
    GoogleEmailUploaderTreeView selectionTreeView;

    // Label dailog
    CheckBox folderLabelMapping;
    CheckBox archiveEverything;
    Label archiveEverythingLabel;

    // Confirm dialog.
    Label readyLabel;
    Label confirmInstructionLabel;

    SelectViewResult result;

    internal SelectView(GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.StartPosition = FormStartPosition.CenterScreen;
      this.googleEmailUploaderModel = googleEmailUploaderModel;
      this.Text = Resources.GoogleEmailUploaderAppName;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.BackColor = Color.White;
      this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
      this.Size = new Size(480, 370);
      this.InitializeComponent();

      if (this.googleEmailUploaderModel.SelectedMailCount == 0) {
        this.nextButton.Enabled = false;
      } else {
        this.nextButton.Enabled = true;
      }

      this.result = SelectViewResult.Closed;
    }

    void InitializeComponent() {
      this.SuspendLayout();
      this.isCustomized = false;

      // Selection Tree View
      this.selectionTreeView = new GoogleEmailUploaderTreeView();
      this.selectionTreeView.ShowLines = true;
      this.selectionTreeView.Location = new Point(25, 85);
      this.selectionTreeView.Size = new System.Drawing.Size(250, 160);
      this.selectionTreeView.BorderStyle = BorderStyle.FixedSingle;
      this.selectionTreeView.CheckBoxes = true;
      this.selectionTreeView.Anchor =
          AnchorStyles.Top
          | AnchorStyles.Bottom
          | AnchorStyles.Left
          | AnchorStyles.Right;

      // loggedInInfo Label.
      this.loggedInInfoLable = new Label();
      this.loggedInInfoLable.Location = new Point(35, 60);
      this.loggedInInfoLable.Size = new Size(150, 15);
      this.loggedInInfoLable.Font = new Font("Arial", 9.25F);
      this.loggedInInfoLable.Text = Resources.LoggedInInfo;

      // emailID Label.
      this.emailIDLabel = new Label();
      this.emailIDLabel.Location = new Point(35, 75);
      this.emailIDLabel.Size = new Size(250, 15);
      this.emailIDLabel.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.emailIDLabel.Text = this.googleEmailUploaderModel.EmailID;

      // chooseEmailPrograms Label.
      this.chooseEmailProgramsLabel = new Label();
      this.chooseEmailProgramsLabel.Location = new Point(35, 105);
      this.chooseEmailProgramsLabel.Size = new Size(250, 30);
      this.chooseEmailProgramsLabel.Font = new Font("Arial", 9.25F);
      this.chooseEmailProgramsLabel.Text = Resources.SelectEmailPrograms;

      // customize LinkLabel.
      this.customizeLabel = new LinkLabel();
      this.customizeLabel.Location = new Point(106, 119);
      this.customizeLabel.Size = new Size(100, 15);
      this.customizeLabel.Font = new Font("Arial", 9.25F);
      this.customizeLabel.Text = Resources.CustomizeText;
      this.customizeLabel.Click += new EventHandler(this.customizeLabel_Click);

      // Select Email Clients.
      int initialVerticalPosition = 160;
      this.clientCheckBoxes = new ArrayList();
      foreach (TreeNodeModel treeNodeModel in
          this.googleEmailUploaderModel.ClientModels) {
        GoogleEmailUploaderTreeNode googleEmailUploaderTreeNode =
            new GoogleEmailUploaderTreeNode(treeNodeModel);
        this.selectionTreeView.Nodes.Add(googleEmailUploaderTreeNode);

        GoogleEmailUploaderCheckBox tempCheckBox =
            new GoogleEmailUploaderCheckBox(googleEmailUploaderTreeNode);
        tempCheckBox.Location = new Point(35, initialVerticalPosition);
        tempCheckBox.Size = new Size(200, 15);
        tempCheckBox.Font = new Font("Arial", 9.25F);
        tempCheckBox.Text = treeNodeModel.DisplayName;
        tempCheckBox.CheckState =
            googleEmailUploaderTreeNode.Checked
                ? CheckState.Checked
                : CheckState.Unchecked;
        tempCheckBox.CheckedChanged +=
            new EventHandler(this.loadedClientsCheckBox_CheckedChanged);
        this.clientCheckBoxes.Add(tempCheckBox);
        initialVerticalPosition += 16;
      }

      // Next Button
      this.nextButton = new Button();
      this.nextButton.Location = new Point(30, 287);
      this.nextButton.Size = new Size(72, 23);
      this.nextButton.Text = Resources.NextText;
      this.nextButton.Font = new Font("Arial", 9.25F);
      this.nextButton.BackColor = SystemColors.Control;
      this.nextButton.Enabled = false;
      this.nextButton.Click += new EventHandler(this.nextButtonInSelect_Click);

      // Cancel Button
      this.cancelButton = new Button();
      this.cancelButton.Location = new Point(120, 287);
      this.cancelButton.Size = new Size(72, 23);
      this.cancelButton.Font = new Font("Arial", 9.25F);
      this.cancelButton.Text = Resources.CancelText;
      this.cancelButton.FlatStyle = FlatStyle.System;
      this.cancelButton.Click += new EventHandler(this.cancelButton_Click);

      // Back Button
      this.backButton = new Button();
      this.backButton.Location = new Point(210, 287);
      this.backButton.Size = new Size(72, 23);
      this.backButton.Text = Resources.BackText;
      this.backButton.Font = new Font("Arial", 9.25F);
      this.backButton.BackColor = SystemColors.Control;
      this.backButton.Click += new EventHandler(this.backButtonInSelect_Click);

      this.addControlsForNonCustomizedSelectDialog();

      // Show the layout.
      this.ResumeLayout(false);
      this.PerformLayout();

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    }

    void backButtonInSelect_Click(object sender, EventArgs e) {
      this.Close();
      this.result = SelectViewResult.Restart;
    }

    void createNonCustomizeDialogHeader() {
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
      selectEmailLabel.Size = new Size(73, 15);
      selectEmailLabel.Font = new Font("Arial", 9.5F, FontStyle.Bold);
      selectEmailLabel.BackColor = backColor;
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

    void addControlsForNonCustomizedSelectDialog() {
      this.createNonCustomizeDialogHeader();

      this.selectionInfoLabel = new Label();
      this.selectionInfoLabel.Location = new Point(35, 250);
      this.selectionInfoLabel.Font = new Font("Arial", 9.25F);
      this.selectionInfoLabel.ForeColor = Color.FromArgb(166, 166, 166);
      this.selectionInfoLabel.AutoSize = true;

      foreach (GoogleEmailUploaderCheckBox checkBox in this.clientCheckBoxes) {
        this.Controls.Add(checkBox);
      }
      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.customizeLabel);
      this.Controls.Add(this.loggedInInfoLable);
      this.Controls.Add(this.emailIDLabel);
      this.Controls.Add(this.chooseEmailProgramsLabel);
      this.Controls.Add(this.selectionInfoLabel);

      this.UpdateSelectionInfoLabel();

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    }

    void addControlsForCustomizedSelectDialog() {
      this.createNonCustomizeDialogHeader();

      this.selectionInfoLabel = new Label();
      this.selectionInfoLabel.Location =
          new Point(this.addAnotherMailbox.Right + 15, 255);
      this.selectionInfoLabel.Font = new Font("Arial", 9.25F);
      this.selectionInfoLabel.ForeColor = Color.FromArgb(166, 166, 166);
      this.selectionInfoLabel.AutoSize = true;

      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.selectEmailLabel);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.selectionTreeView);
      this.Controls.Add(this.addAnotherMailbox);
      this.Controls.Add(this.selectionInfoLabel);

      this.UpdateSelectionInfoLabel();

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    }

    void UpdateSelectionInfoLabel() {
      TimeSpan timeSpan = this.googleEmailUploaderModel.UploadTimeRemaining;
      this.selectionInfoLabel.Text =
          string.Format(Resources.SelectionInfoTemplateText,
                        this.googleEmailUploaderModel.SelectedMailCount,
                        timeSpan.ToString());
    }

    void loadedClientsCheckBox_CheckedChanged(object sender, EventArgs e) {
      GoogleEmailUploaderCheckBox checkBox =
          (GoogleEmailUploaderCheckBox)sender;
      this.selectionTreeView.SuspendCheckEvents = true;
      checkBox.AssociatedTreeNode.CustomChangeCheckedState(
          checkBox.CheckState == CheckState.Checked);
      this.selectionTreeView.SuspendCheckEvents = false;
      this.googleEmailUploaderModel.ComputeMailCounts();
      this.UpdateSelectionInfoLabel();
      if (this.googleEmailUploaderModel.SelectedMailCount == 0) {
        this.nextButton.Enabled = false;
      } else {
        this.nextButton.Enabled = true;
      }
    }

    void customizeLabel_Click(object sender, EventArgs e) {
      this.Controls.Clear();
      this.isCustomized = true;

      // SelectEmail Label.
      this.selectEmailLabel = new Label();
      this.selectEmailLabel.Location = new Point(35, 60);
      this.selectEmailLabel.Size = new Size(260, 15);
      this.selectEmailLabel.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.selectEmailLabel.Text = Resources.SelectUploadEmailsText;

      this.selectionTreeView.AfterCheck +=
          new TreeViewEventHandler(this.selectionTreeView_AfterCheck);
      this.selectionTreeView.AfterSelect +=
      new TreeViewEventHandler(this.selectionTreeView_AfterSelect);

      // AddAnotherMailbox Label
      this.addAnotherMailbox = new LinkLabel();
      this.addAnotherMailbox.Location = new Point(33, 255);
      this.addAnotherMailbox.Size = new Size(150, 15);
      this.addAnotherMailbox.Font = new Font("Arial", 9.25F);
      this.addAnotherMailbox.Text = Resources.AddStore;
      this.addAnotherMailbox.Enabled = false;
      this.addAnotherMailbox.Click +=
          new EventHandler(this.addAnotherMailbox_Click);

      this.addControlsForCustomizedSelectDialog();
    }

    void nextButtonInSelect_Click(object sender, EventArgs e) {

      this.backButton.Click -= new EventHandler(this.backButtonInSelect_Click);
      this.backButton.Click +=
          new EventHandler(this.backButtonInLabelDialog_Click);

      this.Controls.Clear();
      this.nextButton.Click -= new EventHandler(this.nextButtonInSelect_Click);
      this.nextButton.Click +=
          new EventHandler(this.nextButtonInLabelDialog_Click);

      this.folderLabelMapping = new CheckBox();
      this.folderLabelMapping.Checked =
          this.googleEmailUploaderModel.IsFolderToLabelMappingEnabled;
      this.folderLabelMapping.Location = new Point(35, 60);
      this.folderLabelMapping.Size = new Size(220, 15);
      this.folderLabelMapping.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.folderLabelMapping.Text = Resources.FolderLabelCheckBoxText;

      this.archiveEverything = new CheckBox();
      this.archiveEverything.Checked =
          this.googleEmailUploaderModel.IsArchiveEverything;
      this.archiveEverything.Location = new Point(35, 90);
      this.archiveEverything.Size = new Size(220, 16);
      this.archiveEverything.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.archiveEverything.Text = Resources.ArchiveEverythingText;

      this.archiveEverythingLabel = new Label();
      this.archiveEverythingLabel.Location = new Point(48, 107);
      this.archiveEverythingLabel.Size = new Size(220, 30);
      this.archiveEverythingLabel.Font = new Font("Arial", 9.25F);
      this.archiveEverythingLabel.Text = Resources.ArchiveEverythingInfo;

      this.addControlsForLabelDialog();
    }

    void createSelectLabelDialogHeader() {
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
      labelLabel.Size = new Size(44, 15);
      labelLabel.BackColor = backColor;
      labelLabel.Font = new Font("Arial", 9.5F, FontStyle.Bold);
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

    void addControlsForLabelDialog() {
      this.createSelectLabelDialogHeader();

      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.folderLabelMapping);
      this.Controls.Add(this.archiveEverything);
      this.Controls.Add(this.archiveEverythingLabel);

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    }

    void backButtonInLabelDialog_Click(object sender, EventArgs e) {
      this.Controls.Clear();

      this.backButton.Click += new EventHandler(this.backButtonInSelect_Click);
      this.backButton.Click -=
          new EventHandler(this.backButtonInLabelDialog_Click);

      this.nextButton.Click -=
          new EventHandler(this.nextButtonInLabelDialog_Click);
      this.nextButton.Click += new EventHandler(this.nextButtonInSelect_Click);
      if (this.isCustomized) {
        this.addControlsForCustomizedSelectDialog();
      } else {
        this.addControlsForNonCustomizedSelectDialog();
      }
    }

    void nextButtonInLabelDialog_Click(object sender, EventArgs e) {
      this.Controls.Clear();

      this.nextButton.Text = Resources.ImportText;
      this.nextButton.Click -=
          new EventHandler(this.nextButtonInLabelDialog_Click);
      this.nextButton.Click +=
          new EventHandler(this.nextButtonInConfirmDialog_Click);

      this.backButton.Click -=
          new EventHandler(this.backButtonInLabelDialog_Click);
      this.backButton.Click +=
          new EventHandler(this.backButtonInConfirmDialog_Click);

      this.readyLabel = new Label();
      this.readyLabel.Location = new Point(35, 60);
      this.readyLabel.Size = new Size(200, 15);
      this.readyLabel.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.readyLabel.Text = Resources.ReadyText;

      this.confirmInstructionLabel = new Label();
      this.confirmInstructionLabel.Location = new Point(35, 100);
      this.confirmInstructionLabel.Size = new Size(200, 15);
      this.confirmInstructionLabel.Font = new Font("Arial", 9.25F);
      this.confirmInstructionLabel.Text = Resources.ConfirmInstructionText;

      this.addControlsForConfirmDialog();
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

    void nextButtonInConfirmDialog_Click(object sender, EventArgs e) {
      this.googleEmailUploaderModel.SetFolderToLabelMapping(
          this.folderLabelMapping.Checked);
      this.googleEmailUploaderModel.SetArchiving(
          this.archiveEverything.Checked);
      this.Close();
      this.result = SelectViewResult.Upload;
    }

    void backButtonInConfirmDialog_Click(object sender, EventArgs e) {
      this.Controls.Clear();

      this.nextButton.Text = Resources.NextText;
      this.nextButton.Click +=
          new EventHandler(this.nextButtonInLabelDialog_Click);
      this.nextButton.Click -=
          new EventHandler(this.nextButtonInConfirmDialog_Click);

      this.backButton.Click +=
          new EventHandler(this.backButtonInLabelDialog_Click);
      this.backButton.Click -=
          new EventHandler(this.backButtonInConfirmDialog_Click);

      this.addControlsForLabelDialog();
    }

    void addControlsForConfirmDialog() {
      this.createImportDialogHeader();
      this.Controls.Add(this.readyLabel);
      this.Controls.Add(this.confirmInstructionLabel);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.cancelButton);

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    }

    void cancelButton_Click(object sender, EventArgs e) {
      this.Close();
      this.result = SelectViewResult.Cancel;
    }

    void selectionTreeView_AfterCheck(object sender, TreeViewEventArgs e) {
      this.selectionTreeView.SuspendCheckEvents = true;
      GoogleEmailUploaderTreeNode node = (GoogleEmailUploaderTreeNode)e.Node;
      node.CustomChangeCheckedState(node.Checked);
      this.selectionTreeView.SuspendCheckEvents = false;
      this.googleEmailUploaderModel.ComputeMailCounts();
      this.UpdateSelectionInfoLabel();
      if (this.googleEmailUploaderModel.SelectedMailCount == 0) {
        this.nextButton.Enabled = false;
      } else {
        this.nextButton.Enabled = true;
      }
    }

    void selectionTreeView_AfterSelect(object sender, TreeViewEventArgs e) {
      GoogleEmailUploaderTreeNode googleEmailUploaderTreeNode =
          (GoogleEmailUploaderTreeNode)e.Node;
      this.addAnotherMailbox.Enabled =
          googleEmailUploaderTreeNode.TreeNodeModel is ClientModel;
    }

    void addAnotherMailbox_Click(object sender, EventArgs e) {
      GoogleEmailUploaderTreeNode googleEmailUploaderTreeNode = null;
      ClientModel clientModel = null;
      foreach (GoogleEmailUploaderTreeNode googleEmailUploaderTreeNodeIter in
          this.selectionTreeView.Nodes) {
        if (!googleEmailUploaderTreeNodeIter.IsSelected) {
          continue;
        }
        googleEmailUploaderTreeNode = googleEmailUploaderTreeNodeIter;
        clientModel = (ClientModel)googleEmailUploaderTreeNode.TreeNodeModel;
      }
      Debug.Assert(googleEmailUploaderTreeNode != null && clientModel != null);
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Multiselect = false;
      openFileDialog.CheckFileExists = true;
      DialogResult dialogResult = openFileDialog.ShowDialog();
      if (dialogResult == DialogResult.OK) {
        StoreModel addedStoreModel =
            clientModel.OpenStore(openFileDialog.FileName);
        if (addedStoreModel != null) {
          GoogleEmailUploaderTreeNode addedStoreTreeNode =
              new GoogleEmailUploaderTreeNode(addedStoreModel);
          googleEmailUploaderTreeNode.Nodes.Add(addedStoreTreeNode);
          if (googleEmailUploaderTreeNode.Checked) {
            addedStoreTreeNode.Checked = true;
          }
          this.googleEmailUploaderModel.BuildFolderModelFlatList();
          this.UpdateSelectionInfoLabel();
          if (this.googleEmailUploaderModel.SelectedMailCount == 0) {
            this.nextButton.Enabled = false;
          } else {
            this.nextButton.Enabled = true;
          }
        } else {
          MessageBox.Show(string.Format(Resources.CouldNotOpenStoreTemplate,
                                        openFileDialog.FileName));
        }
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

    internal SelectViewResult Result {
      get {
        return this.result;
      }
    }
  }
}
