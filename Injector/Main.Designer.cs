
namespace Injector
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBoxProcessIcon = new System.Windows.Forms.PictureBox();
            this.lblProcessDetails = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtboxRuntimeVer = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtboxTypename = new System.Windows.Forms.TextBox();
            this.txtboxEntrypointMethod = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnInject = new System.Windows.Forms.Button();
            this.GameDirWorker = new System.ComponentModel.BackgroundWorker();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.ProcessPollWorker = new System.ComponentModel.BackgroundWorker();
            this.txtboxInjectDLLPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxProcessIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(366, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuToolStripMenuItem
            // 
            this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            this.menuToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
            this.menuToolStripMenuItem.Text = "Menu";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // pictureBoxProcessIcon
            // 
            this.pictureBoxProcessIcon.Location = new System.Drawing.Point(12, 27);
            this.pictureBoxProcessIcon.Name = "pictureBoxProcessIcon";
            this.pictureBoxProcessIcon.Size = new System.Drawing.Size(36, 36);
            this.pictureBoxProcessIcon.TabIndex = 1;
            this.pictureBoxProcessIcon.TabStop = false;
            // 
            // lblProcessDetails
            // 
            this.lblProcessDetails.AutoSize = true;
            this.lblProcessDetails.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblProcessDetails.Location = new System.Drawing.Point(54, 27);
            this.lblProcessDetails.Name = "lblProcessDetails";
            this.lblProcessDetails.Size = new System.Drawing.Size(96, 13);
            this.lblProcessDetails.TabIndex = 2;
            this.lblProcessDetails.Text = "Waiting for game...";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "NET Runtime Version:";
            // 
            // txtboxRuntimeVer
            // 
            this.txtboxRuntimeVer.Location = new System.Drawing.Point(127, 75);
            this.txtboxRuntimeVer.Name = "txtboxRuntimeVer";
            this.txtboxRuntimeVer.Size = new System.Drawing.Size(124, 20);
            this.txtboxRuntimeVer.TabIndex = 6;
            this.txtboxRuntimeVer.Text = "v4.0.30319";
            this.txtboxRuntimeVer.Leave += new System.EventHandler(this.txtboxRuntimeVer_Leave);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 104);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Full Typename:";
            // 
            // txtboxTypename
            // 
            this.txtboxTypename.Location = new System.Drawing.Point(127, 101);
            this.txtboxTypename.Name = "txtboxTypename";
            this.txtboxTypename.Size = new System.Drawing.Size(124, 20);
            this.txtboxTypename.TabIndex = 8;
            this.txtboxTypename.Leave += new System.EventHandler(this.txtboxTypename_Leave);
            // 
            // txtboxEntrypointMethod
            // 
            this.txtboxEntrypointMethod.Location = new System.Drawing.Point(127, 127);
            this.txtboxEntrypointMethod.Name = "txtboxEntrypointMethod";
            this.txtboxEntrypointMethod.Size = new System.Drawing.Size(124, 20);
            this.txtboxEntrypointMethod.TabIndex = 9;
            this.txtboxEntrypointMethod.Leave += new System.EventHandler(this.txtboxEntrypointMethod_Leave);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 130);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Entrypoint Method:";
            // 
            // btnInject
            // 
            this.btnInject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInject.Enabled = false;
            this.btnInject.Location = new System.Drawing.Point(12, 153);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(342, 37);
            this.btnInject.TabIndex = 11;
            this.btnInject.Text = "Inject";
            this.btnInject.UseVisualStyleBackColor = true;
            this.btnInject.Click += new System.EventHandler(this.btnInject_Click);
            // 
            // GameDirWorker
            // 
            this.GameDirWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.GameDirWorker_DoWork);
            this.GameDirWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.GameDirWorker_RunWorkerCompleted);
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.Description = "Locate Nin Online Install Location...";
            this.folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.folderBrowserDialog1.ShowNewFolderButton = false;
            // 
            // ProcessPollWorker
            // 
            this.ProcessPollWorker.WorkerSupportsCancellation = true;
            this.ProcessPollWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.ProcessPollWorker_DoWork);
            this.ProcessPollWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.ProcessPollWorker_RunWorkerCompleted);
            // 
            // txtboxInjectDLLPath
            // 
            this.txtboxInjectDLLPath.Location = new System.Drawing.Point(127, 43);
            this.txtboxInjectDLLPath.Name = "txtboxInjectDLLPath";
            this.txtboxInjectDLLPath.ReadOnly = true;
            this.txtboxInjectDLLPath.Size = new System.Drawing.Size(227, 20);
            this.txtboxInjectDLLPath.TabIndex = 12;
            this.txtboxInjectDLLPath.Click += new System.EventHandler(this.txtboxInjectDLLPath_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(54, 46);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Inject DLL:";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "Inject DLL";
            this.openFileDialog1.Filter = "DLL files|*.dll";
            this.openFileDialog1.Title = "Select Inject DLL ...";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(366, 202);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtboxInjectDLLPath);
            this.Controls.Add(this.btnInject);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtboxEntrypointMethod);
            this.Controls.Add(this.txtboxTypename);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtboxRuntimeVer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblProcessDetails);
            this.Controls.Add(this.pictureBoxProcessIcon);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Main";
            this.Text = "NET Injector";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxProcessIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBoxProcessIcon;
        private System.Windows.Forms.Label lblProcessDetails;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtboxRuntimeVer;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtboxTypename;
        private System.Windows.Forms.TextBox txtboxEntrypointMethod;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnInject;
        private System.ComponentModel.BackgroundWorker GameDirWorker;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.ComponentModel.BackgroundWorker ProcessPollWorker;
        private System.Windows.Forms.TextBox txtboxInjectDLLPath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

