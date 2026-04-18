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
            this.SuspendLayout();
            
            // btnAddRepo
            this.btnAddRepo.Location = new System.Drawing.Point(12, 12);
            this.btnAddRepo.Name = "btnAddRepo";
            this.btnAddRepo.Size = new System.Drawing.Size(150, 40);
            this.btnAddRepo.TabIndex = 0;
            this.btnAddRepo.Text = "+ Add Local Folder";
            this.btnAddRepo.UseVisualStyleBackColor = true;
            this.btnAddRepo.Click += new System.EventHandler(this.BtnAddRepo_Click);
            
            // btnPublish
            this.btnPublish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPublish.BackColor = System.Drawing.Color.MediumSeaGreen;
            this.btnPublish.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPublish.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnPublish.ForeColor = System.Drawing.Color.White;
            this.btnPublish.Location = new System.Drawing.Point(372, 12);
            this.btnPublish.Name = "btnPublish";
            this.btnPublish.Size = new System.Drawing.Size(150, 40);
            this.btnPublish.TabIndex = 1;
            this.btnPublish.Text = "Publish Selected";
            this.btnPublish.UseVisualStyleBackColor = false;
            this.btnPublish.Click += new System.EventHandler(this.BtnPublish_Click);
            
            // lblRepos
            this.lblRepos.AutoSize = true;
            this.lblRepos.Location = new System.Drawing.Point(12, 65);
            this.lblRepos.Name = "lblRepos";
            this.lblRepos.Size = new System.Drawing.Size(103, 15);
            this.lblRepos.TabIndex = 4;
            this.lblRepos.Text = "Saved Repositories";
            
            // lstRepos
            this.lstRepos.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstRepos.FormattingEnabled = true;
            this.lstRepos.ItemHeight = 15;
            this.lstRepos.Location = new System.Drawing.Point(12, 85);
            this.lstRepos.Name = "lstRepos";
            this.lstRepos.Size = new System.Drawing.Size(510, 154);
            this.lstRepos.TabIndex = 2;
            
            // lblLog
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(12, 255);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(76, 15);
            this.lblLog.TabIndex = 5;
            this.lblLog.Text = "Activity Log";
            
            // txtLog
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.ForeColor = System.Drawing.Color.LightGray;
            this.txtLog.Location = new System.Drawing.Point(12, 275);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(510, 160);
            this.txtLog.TabIndex = 3;
            this.txtLog.Text = "";
            
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 451);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.lblRepos);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lstRepos);
            this.Controls.Add(this.btnPublish);
            this.Controls.Add(this.btnAddRepo);
            this.MinimumSize = new System.Drawing.Size(400, 400);
            this.Name = "MainForm";
            this.Text = "SEAGit - Super-Easy And Accessible Git";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}