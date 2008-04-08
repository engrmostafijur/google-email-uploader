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

  /// <summary>
  /// We extend the framework tree node so that we can store our model
  /// within it.
  /// </summary>
  abstract class GemuTreeNode : TreeNode {
    internal readonly TreeNodeModel TreeNodeModel;
    protected uint cummulativeCount;

    internal GemuTreeNode(TreeNodeModel treeNodeModel) {
      this.TreeNodeModel = treeNodeModel;
    }

    internal abstract void ChangeCheckedStateRec(bool isChecked);

    internal void CustomChangeCheckedState(bool isChecked) {
      this.ChangeCheckedStateRec(isChecked);
      if (isChecked) {
        GemuTreeNode parentIter =
            this.Parent as GemuTreeNode;
        while (parentIter != null) {
          parentIter.TreeNodeModel.IsSelected = true;
          parentIter.Checked = true;
          parentIter = parentIter.Parent as GemuTreeNode;
        }
      }
    }

    internal uint CummmulativeCount {
      get {
        return this.cummulativeCount;
      }
    }

    internal void AddStoreChild(GemuTreeNode storeTreeNode) {
      Debug.Assert(this.TreeNodeModel is ClientModel);
      Debug.Assert(storeTreeNode.TreeNodeModel is StoreModel);
      this.cummulativeCount += storeTreeNode.CummmulativeCount;
      this.Text =
          this.TreeNodeModel.DisplayName + " ["
              + cummulativeCount + "]";
      this.Nodes.Add(storeTreeNode);
    }
  }

  /// <summary>
  /// This represents normal folder/client/store tree node. These are not
  /// special like the contact tree node below.
  /// </summary>
  class NormalGemuTreeNode : GemuTreeNode {
    internal NormalGemuTreeNode(TreeNodeModel treeNodeModel)
      : base(treeNodeModel) {
      uint cummulativeCount = 0;
      FolderModel folderModel = treeNodeModel as FolderModel;
      if (folderModel != null) {
        cummulativeCount += folderModel.Folder.MailCount;
      }
      foreach (TreeNodeModel childTreeNodeModel in treeNodeModel.Children) {
        GemuTreeNode childTreeNodeView =
            new NormalGemuTreeNode(childTreeNodeModel);
        this.Nodes.Add(childTreeNodeView);
        cummulativeCount += childTreeNodeView.CummmulativeCount;
      }
      StoreModel storeModel = treeNodeModel as StoreModel;
      if (storeModel != null && storeModel.Store.ContactCount > 0) {
        ContactGemuTreeNode contactTreeNode =
            new ContactGemuTreeNode(storeModel);
        this.Nodes.Add(contactTreeNode);
        cummulativeCount += contactTreeNode.CummmulativeCount;
      }
      this.Text =
          treeNodeModel.DisplayName + " ["
              + cummulativeCount + "]";
      this.Checked = treeNodeModel.IsSelected;
      this.cummulativeCount = cummulativeCount;
    }

    internal override void ChangeCheckedStateRec(bool isChecked) {
      this.TreeNodeModel.IsSelected = isChecked;
      this.Checked = isChecked;
      foreach (GemuTreeNode childTreeNode in this.Nodes) {
        childTreeNode.ChangeCheckedStateRec(isChecked);
      }
    }
  }

  /// <summary>
  /// This represents the tree node corresponding to the contacts in the
  /// particular client.
  /// </summary>
  class ContactGemuTreeNode : GemuTreeNode {
    internal ContactGemuTreeNode(StoreModel storeModel)
      : base(storeModel) {
      this.Text = string.Format(Resources.ContactsTemplateText,
                                storeModel.Store.ContactCount);
      this.Checked = storeModel.IsContactSelected;
      this.cummulativeCount = storeModel.Store.ContactCount;
    }

    internal override void ChangeCheckedStateRec(bool isChecked) {
      ((StoreModel)this.TreeNodeModel).IsContactSelected = isChecked;
      this.Checked = isChecked;
    }
  }

  class GemuTreeView : TreeView {
    internal bool SuspendCheckEvents;

    // This is to prevent infinite recursion because we are responding to the
    // after check event in the form.
    protected override void OnAfterCheck(TreeViewEventArgs e) {
      if (!this.SuspendCheckEvents) {
        base.OnAfterCheck(e);
      }
    }
  }

  class SelectView : Form {
    readonly GoogleEmailUploaderModel googleEmailUploaderModel;

    Button nextButton;
    Button backButton;
    Label selectionInfoLabel;

    // Customized dialog
    Label selectEmailLabel;
    ArrayList addAnotherClientMailbox;
    GemuTreeView selectionTreeView;

    // Label dailog
    CheckBox folderLabelMapping;
    Label folderLabel;
    CheckBox archiveEverything;
    Label archiveEverythingLabel;

    SelectViewResult result;

    internal SelectView(GoogleEmailUploaderModel googleEmailUploaderModel) {
      this.StartPosition = FormStartPosition.CenterScreen;
      this.googleEmailUploaderModel = googleEmailUploaderModel;
      this.Text = Resources.GoogleEmailUploaderAppNameText;
      this.Icon = Resources.GMailIcon;
      this.MaximizeBox = false;
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.BackColor = Color.White;
      this.BackgroundImage = Resources.GoogleEmailUploaderBackgroundImage;
      this.Size = new Size(530, 370);
      this.InitializeComponent();

      if (this.googleEmailUploaderModel.TotalSelectedItemCount == 0) {
        this.nextButton.Enabled = false;
      } else {
        this.nextButton.Enabled = true;
      }

      this.result = SelectViewResult.Closed;
    }

    void InitializeComponent() {
      // Selection Tree View
      this.selectionTreeView = new GemuTreeView();
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
      foreach (TreeNodeModel treeNodeModel in
          this.googleEmailUploaderModel.ClientModels) {
        GemuTreeNode googleEmailUploaderTreeNode =
            new NormalGemuTreeNode(treeNodeModel);
        this.selectionTreeView.Nodes.Add(googleEmailUploaderTreeNode);
      }

      // Next Button
      this.nextButton = new Button();
      this.nextButton.Location = new Point(120, 287);
      this.nextButton.Size = new Size(72, 23);
      this.nextButton.Text = Resources.NextText;
      this.nextButton.Font = new Font("Arial", 9.25F);
      this.nextButton.BackColor = SystemColors.Control;
      this.nextButton.Enabled = false;
      this.nextButton.Click += new EventHandler(this.nextButtonInSelect_Click);

      // Back Button
      this.backButton = new Button();
      this.backButton.Location = new Point(30, 287);
      this.backButton.Size = new Size(72, 23);
      this.backButton.Text = Resources.BackText;
      this.backButton.Font = new Font("Arial", 9.25F);
      this.backButton.BackColor = SystemColors.Control;
      this.backButton.Click += new EventHandler(this.backButtonInSelect_Click);

      // SelectEmail Label.
      this.selectEmailLabel = new Label();
      this.selectEmailLabel.Location = new Point(25, 60);
      this.selectEmailLabel.Size = new Size(260, 15);
      this.selectEmailLabel.Font = new Font("Arial", 9.25F);
      this.selectEmailLabel.Text = Resources.SelectUploadFoldersText;

      this.selectionTreeView.AfterCheck +=
          new TreeViewEventHandler(this.selectionTreeView_AfterCheck);

      this.addAnotherClientMailbox = new ArrayList();
      int initialVerticalOffset = 180;
      foreach (ClientModel model in
          this.googleEmailUploaderModel.ClientModels) {
        if (model.Client.SupportsLoadingStore) {
          LinkLabel addStoreLinkLabel = new LinkLabel();
          addStoreLinkLabel.Text = string.Format(
              Resources.AddMailBoxTemplateText,
              model.Client.Name);
          addStoreLinkLabel.Location = new Point(320, initialVerticalOffset);
          addStoreLinkLabel.AutoSize = true;
          addStoreLinkLabel.Click +=
              new EventHandler(this.addAnotherMailbox_Click);
          addStoreLinkLabel.Enabled = true;
          addStoreLinkLabel.Font = new Font("Arial", 9.25F);
          addStoreLinkLabel.Name = model.Client.Name;

          initialVerticalOffset += 20;
          this.addAnotherClientMailbox.Add(addStoreLinkLabel);
        }
      }


      this.addControlsForNonCustomizedSelectDialog();

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    }

    void backButtonInSelect_Click(object sender, EventArgs e) {
      this.Close();
      this.result = SelectViewResult.Restart;
    }

    void addControlsForNonCustomizedSelectDialog() {
      Program.AddHeaderStrip(1, this.Controls);

      this.selectionInfoLabel = new Label();
      this.selectionInfoLabel.Location = new Point(25, 255);
      this.selectionInfoLabel.Font = new Font("Arial", 9.25F);
      this.selectionInfoLabel.ForeColor = Program.DisabledGreyColor;
      this.selectionInfoLabel.AutoSize = true;

      foreach (LinkLabel addMailBox in this.addAnotherClientMailbox) {
        this.Controls.Add(addMailBox);
      }
      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.selectionTreeView);
      this.Controls.Add(this.selectionInfoLabel);
      this.Controls.Add(this.selectEmailLabel);

      this.UpdateSelectionInfoLabel();

      this.ActiveControl = this.nextButton;
      this.AcceptButton = this.nextButton;
    } 

    void UpdateSelectionInfoLabel() {
      string templateMessage;
      if (this.googleEmailUploaderModel.SelectedContactCount == 1) {
        if (this.googleEmailUploaderModel.SelectedEmailCount == 1) {
          templateMessage = Resources.SelectionInfoSSTemplateText;
        } else {
          templateMessage = Resources.SelectionInfoSPTemplateText;
        }
      } else {
        if (this.googleEmailUploaderModel.SelectedEmailCount == 1) {
          templateMessage = Resources.SelectionInfoPSTemplateText;
        } else {
          templateMessage = Resources.SelectionInfoPPTemplateText;
        }
      }
      this.selectionInfoLabel.Text =
          string.Format(templateMessage,
                        this.googleEmailUploaderModel.SelectedContactCount,
                        this.googleEmailUploaderModel.SelectedEmailCount,
                        this.googleEmailUploaderModel.BallParkEstimate());
    }

    void nextButtonInSelect_Click(object sender, EventArgs e) {
      this.Controls.Clear();

      this.backButton.Click -= new EventHandler(this.backButtonInSelect_Click);
      this.backButton.Click +=
          new EventHandler(this.backButtonInLabelDialog_Click);

      this.nextButton.Click -= new EventHandler(this.nextButtonInSelect_Click);
      this.nextButton.Click +=
          new EventHandler(this.nextButtonInLabelDialog_Click);
      this.nextButton.Text = Resources.UploadText;

      this.folderLabelMapping = new CheckBox();
      this.folderLabelMapping.Checked =
          this.googleEmailUploaderModel.IsFolderToLabelMappingEnabled;
      this.folderLabelMapping.Location = new Point(35, 60);
      this.folderLabelMapping.Size = new Size(220, 15);
      this.folderLabelMapping.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.folderLabelMapping.Text = Resources.FolderLabelCheckBoxText;

      this.folderLabel = new Label();
      this.folderLabel.Location =
          new Point(53, this.folderLabelMapping.Bottom + 5);
      this.folderLabel.Size = new Size(220, 50);
      this.folderLabel.Font = new Font("Arial", 9.25F);
      this.folderLabel.Text = Resources.FolderInfoText;

      this.archiveEverything = new CheckBox();
      this.archiveEverything.Checked =
          this.googleEmailUploaderModel.IsArchiveEverything;
      this.archiveEverything.Location =
          new Point(35, this.folderLabel.Bottom + 10);
      this.archiveEverything.Size = new Size(220, 16);
      this.archiveEverything.Font = new Font("Arial", 9.25F, FontStyle.Bold);
      this.archiveEverything.Text = Resources.ArchiveEverythingText;

      this.archiveEverythingLabel = new Label();
      this.archiveEverythingLabel.Location =
          new Point(53, this.archiveEverything.Bottom + 5);
      this.archiveEverythingLabel.Size = new Size(220, 60);
      this.archiveEverythingLabel.Font = new Font("Arial", 9.25F);
      this.archiveEverythingLabel.Text = Resources.ArchiveEverythingInfoText;

      this.addControlsForLabelDialog();
    }

    void addControlsForLabelDialog() {
      Program.AddHeaderStrip(2, this.Controls);

      this.Controls.Add(this.nextButton);
      this.Controls.Add(this.backButton);
      this.Controls.Add(this.folderLabelMapping);
      this.Controls.Add(this.folderLabel);
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

      this.nextButton.Text = Resources.NextText;
      this.addControlsForNonCustomizedSelectDialog();
    }

    void nextButtonInLabelDialog_Click(object sender, EventArgs e) {
      this.googleEmailUploaderModel.SetFolderToLabelMapping(
          this.folderLabelMapping.Checked);
      this.googleEmailUploaderModel.SetArchiving(
          this.archiveEverything.Checked);
      this.Close();
      this.result = SelectViewResult.Upload;
    }

    void selectionTreeView_AfterCheck(object sender, TreeViewEventArgs e) {
      this.selectionTreeView.SuspendCheckEvents = true;
      GemuTreeNode node = (GemuTreeNode)e.Node;
      node.CustomChangeCheckedState(node.Checked);
      this.selectionTreeView.SuspendCheckEvents = false;
      this.googleEmailUploaderModel.ComputeEmailContactCounts();
      this.UpdateSelectionInfoLabel();
      if (this.googleEmailUploaderModel.TotalSelectedItemCount == 0) {
        this.nextButton.Enabled = false;
      } else {
        this.nextButton.Enabled = true;
      }
    }

    void addAnotherMailbox_Click(object sender, EventArgs e) {
      GemuTreeNode clientModelTreeNode = null;
      ClientModel clientModel = null;
      string senderName = ((LinkLabel)sender).Name;
      foreach (GemuTreeNode googleEmailUploaderTreeNodeIter in
          this.selectionTreeView.Nodes) {
        ClientModel tempClientModel =
            (ClientModel)googleEmailUploaderTreeNodeIter.TreeNodeModel;
        if (tempClientModel.Client.Name == senderName) {
          clientModel = tempClientModel;
          clientModelTreeNode = googleEmailUploaderTreeNodeIter;
          break;
        }
      }
      Debug.Assert(clientModelTreeNode != null && clientModel != null);
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Multiselect = false;
      openFileDialog.CheckFileExists = true;
      DialogResult dialogResult = openFileDialog.ShowDialog();
      if (dialogResult == DialogResult.OK) {
        StoreModel addedStoreModel =
            clientModel.OpenStore(openFileDialog.FileName);
        if (addedStoreModel != null) {
          GemuTreeNode addedStoreTreeNode =
              new NormalGemuTreeNode(addedStoreModel);
          clientModelTreeNode.AddStoreChild(addedStoreTreeNode);
          if (clientModelTreeNode.Checked) {
            addedStoreTreeNode.Checked = true;
          }
          this.googleEmailUploaderModel.BuildModelFlatList();
          this.UpdateSelectionInfoLabel();
          if (this.googleEmailUploaderModel.TotalSelectedItemCount == 0) {
            this.nextButton.Enabled = false;
          } else {
            this.nextButton.Enabled = true;
          }
        } else {
          MessageBox.Show(string.Format(Resources.CouldNotOpenStoreTemplateText,
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
