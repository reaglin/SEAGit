namespace SEAGit
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnAddRepo;
        private System.Windows.Forms.Button btnPublish;
        private System.Windows.Forms.ListBox lstRepos;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Label lblRepos;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuCheckUpdates;
        private System.Windows.Forms.ToolStripMenuItem mnuAbout;
        private System.Windows.Forms.ToolStripMenuItem mnuHelpScreen;
        private System.Windows.Forms.ToolStripMenuItem mnuHelpWeb;
        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnChangeAddress;
        private System.Windows.Forms.Button btnRemove;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnAddRepo = new System.Windows.Forms.Button();
            this.btnPublish = new System.Windows.Forms.Button();
            this.lstRepos = new System.Windows.Forms.ListBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.lblRepos = new System.Windows.Forms.Label();
            this.lblLog = new System.Windows.Forms.Label();
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCheckUpdates = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelpScreen = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelpWeb = new System.Windows.Forms.ToolStripMenuItem();
            this.lblIntro = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnChangeAddress = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.menuMain.SuspendLayout();
            this.SuspendLayout();

            // menuMain
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.mnuHelp });
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Size = new System.Drawing.Size(550, 24);
            this.menuMain.TabIndex = 6;

            // mnuHelp
            // The standard Help menu of every Store program (usability checklist 6.3), in this order and with these words.
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { this.mnuHelpScreen, this.mnuHelpWeb, this.mnuCheckUpdates, this.mnuAbout });
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Text = "&Help";

            // mnuHelpScreen
            this.mnuHelpScreen.Name = "mnuHelpScreen";
            this.mnuHelpScreen.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.mnuHelpScreen.Text = "Help for this &screen";
            this.mnuHelpScreen.ToolTipText = "How to use this window: adding a folder, publishing it, and what the buttons do";
            this.mnuHelpScreen.Click += new System.EventHandler(this.MnuHelpScreen_Click);

            // mnuHelpWeb
            this.mnuHelpWeb.Name = "mnuHelpWeb";
            this.mnuHelpWeb.Text = "Help on the &web (softwareplus.ai/seagit)";
            this.mnuHelpWeb.ToolTipText = "Opens SEAGit's full help, with pictures, in your web browser";
            this.mnuHelpWeb.Click += new System.EventHandler(this.MnuHelpWeb_Click);

            // mnuCheckUpdates
            this.mnuCheckUpdates.Name = "mnuCheckUpdates";
            this.mnuCheckUpdates.Text = "Check for a &new version…";
            this.mnuCheckUpdates.ToolTipText = "Asks the Microsoft Store whether a newer version of SEAGit is out, and offers to install it";
            this.mnuCheckUpdates.Click += new System.EventHandler(this.MnuCheckUpdates_Click);

            // mnuAbout
            this.mnuAbout.Name = "mnuAbout";
            this.mnuAbout.Text = "&About SEAGit…";
            this.mnuAbout.ToolTipText = "Who made SEAGit, its version, and links to its web page, its Microsoft Store page and its privacy policy";
            this.mnuAbout.Click += new System.EventHandler(this.MnuAbout_Click);
            
            // lblIntro — what the window is for and what to do, for a first-time user
            this.lblIntro.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblIntro.Location = new System.Drawing.Point(12, 30);
            this.lblIntro.Name = "lblIntro";
            this.lblIntro.Size = new System.Drawing.Size(526, 64);
            this.lblIntro.TabIndex = 10;
            this.lblIntro.Text = "SEAGit copies a folder from your computer to GitHub.\n" +
                "1. Add a folder from your computer.   2. Select it in the list.   " +
                "3. Press Publish Selected to copy it to GitHub.\n" +
                "The first time, you will paste a GitHub repository address and sign in to GitHub in your browser.";

            // btnAddRepo
            this.btnAddRepo.Location = new System.Drawing.Point(12, 98);
            this.btnAddRepo.Name = "btnAddRepo";
            this.btnAddRepo.Size = new System.Drawing.Size(150, 36);
            this.btnAddRepo.TabIndex = 0;
            this.btnAddRepo.Text = "+ &Add Local Folder…";
            this.btnAddRepo.UseVisualStyleBackColor = true;
            this.btnAddRepo.Click += new System.EventHandler(this.BtnAddRepo_Click);

            // btnPublish
            this.btnPublish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPublish.BackColor = System.Drawing.Color.FromArgb(30, 113, 69); // dark green: white text on it is easy to read
            this.btnPublish.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPublish.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnPublish.ForeColor = System.Drawing.Color.White;
            this.btnPublish.Location = new System.Drawing.Point(388, 98);
            this.btnPublish.Name = "btnPublish";
            this.btnPublish.Size = new System.Drawing.Size(150, 36);
            this.btnPublish.TabIndex = 2;
            this.btnPublish.Text = "&Publish Selected";
            this.btnPublish.UseVisualStyleBackColor = false;
            this.btnPublish.Click += new System.EventHandler(this.BtnPublish_Click);

            // lblRepos
            this.lblRepos.AutoSize = true;
            this.lblRepos.Location = new System.Drawing.Point(12, 142);
            this.lblRepos.Name = "lblRepos";
            this.lblRepos.TabIndex = 11;
            this.lblRepos.Text = "Your folders";

            // lstRepos
            this.lstRepos.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstRepos.AccessibleName = "Your folders";
            this.lstRepos.FormattingEnabled = true;
            this.lstRepos.HorizontalScrollbar = true;
            this.lstRepos.IntegralHeight = false;
            this.lstRepos.Location = new System.Drawing.Point(12, 160);
            this.lstRepos.Name = "lstRepos";
            this.lstRepos.Size = new System.Drawing.Size(526, 120);
            this.lstRepos.TabIndex = 1;
            this.lstRepos.DoubleClick += new System.EventHandler(this.BtnOpenFolder_Click);

            // btnOpenFolder
            this.btnOpenFolder.Location = new System.Drawing.Point(12, 286);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(110, 28);
            this.btnOpenFolder.TabIndex = 3;
            this.btnOpenFolder.Text = "&Open Folder";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.BtnOpenFolder_Click);

            // btnChangeAddress
            this.btnChangeAddress.Location = new System.Drawing.Point(128, 286);
            this.btnChangeAddress.Name = "btnChangeAddress";
            this.btnChangeAddress.Size = new System.Drawing.Size(170, 28);
            this.btnChangeAddress.TabIndex = 4;
            this.btnChangeAddress.Text = "&Change GitHub Address…";
            this.btnChangeAddress.UseVisualStyleBackColor = true;
            this.btnChangeAddress.Click += new System.EventHandler(this.BtnChangeAddress_Click);

            // btnRemove
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(418, 286);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(120, 28);
            this.btnRemove.TabIndex = 5;
            this.btnRemove.Text = "&Remove from List";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.BtnRemove_Click);

            // lblStatus — what SEAGit is doing now (empty when idle)
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatus.Location = new System.Drawing.Point(12, 318);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(526, 34);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "";

            // lblLog
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(12, 354);
            this.lblLog.Name = "lblLog";
            this.lblLog.TabIndex = 13;
            this.lblLog.Text = "Activity Log (each step, with Git's own words when something goes wrong)";

            // txtLog
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.AccessibleName = "Activity Log";
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.ForeColor = System.Drawing.Color.LightGray;
            this.txtLog.Location = new System.Drawing.Point(12, 372);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(526, 116);
            this.txtLog.TabIndex = 6;
            this.txtLog.Text = "";

            // MainForm
            // No AutoScaleMode here: DpiAwareForm sets the 96-DPI baseline and scales in OnLoad. A
            // designer Font baseline as well scaled the window twice, and it opened 136 x 113.
            this.ClientSize = new System.Drawing.Size(550, 500);
            this.Controls.Add(this.lblIntro);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.btnChangeAddress);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.lblRepos);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lstRepos);
            this.Controls.Add(this.btnPublish);
            this.Controls.Add(this.btnAddRepo);
            this.Controls.Add(this.menuMain);
            this.MainMenuStrip = this.menuMain;
            this.MinimumSize = new System.Drawing.Size(566, 520);
            this.Name = "MainForm";
            this.Text = "SEAGit — Super-Easy and Accessible Git";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}