namespace Hyperbyte_Patcher
{
    partial class FormPatcher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPatcher));
            this.panelMain = new System.Windows.Forms.Panel();
            this.buttonClosePatcher = new System.Windows.Forms.Button();
            this.buttonStartApp = new System.Windows.Forms.Button();
            this.checkBoxAutoStart = new System.Windows.Forms.CheckBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelNotice = new System.Windows.Forms.Label();
            this.textBoxNotice = new System.Windows.Forms.TextBox();
            this.panelNotice = new System.Windows.Forms.Panel();
            this.labelSpeed = new System.Windows.Forms.Label();
            this.panelMain.SuspendLayout();
            this.panelNotice.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.buttonClosePatcher);
            this.panelMain.Controls.Add(this.buttonStartApp);
            this.panelMain.Controls.Add(this.checkBoxAutoStart);
            this.panelMain.Controls.Add(this.labelStatus);
            this.panelMain.Controls.Add(this.progressBar);
            resources.ApplyResources(this.panelMain, "panelMain");
            this.panelMain.Name = "panelMain";
            // 
            // buttonClosePatcher
            // 
            resources.ApplyResources(this.buttonClosePatcher, "buttonClosePatcher");
            this.buttonClosePatcher.Name = "buttonClosePatcher";
            this.buttonClosePatcher.UseVisualStyleBackColor = true;
            this.buttonClosePatcher.Click += new System.EventHandler(this.buttonClosePatcher_Click);
            // 
            // buttonStartApp
            // 
            resources.ApplyResources(this.buttonStartApp, "buttonStartApp");
            this.buttonStartApp.Name = "buttonStartApp";
            this.buttonStartApp.UseVisualStyleBackColor = true;
            this.buttonStartApp.Click += new System.EventHandler(this.buttonStartApp_Click);
            // 
            // checkBoxAutoStart
            // 
            resources.ApplyResources(this.checkBoxAutoStart, "checkBoxAutoStart");
            this.checkBoxAutoStart.Name = "checkBoxAutoStart";
            this.checkBoxAutoStart.UseVisualStyleBackColor = true;
            // 
            // labelStatus
            // 
            resources.ApplyResources(this.labelStatus, "labelStatus");
            this.labelStatus.Name = "labelStatus";
            // 
            // progressBar
            // 
            resources.ApplyResources(this.progressBar, "progressBar");
            this.progressBar.Name = "progressBar";
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // labelNotice
            // 
            resources.ApplyResources(this.labelNotice, "labelNotice");
            this.labelNotice.Name = "labelNotice";
            // 
            // textBoxNotice
            // 
            resources.ApplyResources(this.textBoxNotice, "textBoxNotice");
            this.textBoxNotice.Name = "textBoxNotice";
            this.textBoxNotice.ReadOnly = true;
            this.textBoxNotice.TabStop = false;
            // 
            // panelNotice
            // 
            this.panelNotice.Controls.Add(this.labelSpeed);
            this.panelNotice.Controls.Add(this.textBoxNotice);
            this.panelNotice.Controls.Add(this.labelNotice);
            resources.ApplyResources(this.panelNotice, "panelNotice");
            this.panelNotice.Name = "panelNotice";
            // 
            // labelSpeed
            // 
            resources.ApplyResources(this.labelSpeed, "labelSpeed");
            this.labelSpeed.Name = "labelSpeed";
            // 
            // FormPatcher
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelNotice);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormPatcher";
            this.Shown += new System.EventHandler(this.FormPatcher_Shown);
            this.panelMain.ResumeLayout(false);
            this.panelNotice.ResumeLayout(false);
            this.panelNotice.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label labelNotice;
        private System.Windows.Forms.TextBox textBoxNotice;
        private System.Windows.Forms.Panel panelNotice;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.CheckBox checkBoxAutoStart;
        private System.Windows.Forms.Button buttonClosePatcher;
        private System.Windows.Forms.Button buttonStartApp;
        private System.Windows.Forms.Label labelSpeed;
    }
}

