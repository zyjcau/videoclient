using System.Windows.Forms;

namespace VideoClient.UI
{
    partial class FormMain
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
            this.panel_video = new System.Windows.Forms.Panel();
            this.panel_cef = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panel_video
            // 
            this.panel_video.Location = new System.Drawing.Point(0, 0);
            this.panel_video.Margin = new System.Windows.Forms.Padding(0);
            this.panel_video.Name = "panel_video";
            this.panel_video.Size = new System.Drawing.Size(100, 100);
            this.panel_video.TabIndex = 2;
            // 
            // panel_cef
            // 
            this.panel_cef.Location = new System.Drawing.Point(110, 0);
            this.panel_cef.Margin = new System.Windows.Forms.Padding(0);
            this.panel_cef.Name = "panel_cef";
            this.panel_cef.Size = new System.Drawing.Size(100, 100);
            this.panel_cef.TabIndex = 3;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1828, 944);
            this.Controls.Add(this.panel_cef);
            this.Controls.Add(this.panel_video);
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormMain";
            this.Text = "视频窗口";
            this.Load += new System.EventHandler(this.FormVideo_Load);
            this.ResumeLayout(false);
        }

        public System.Windows.Forms.Panel panel_video;
        public System.Windows.Forms.Panel panel_cef;

        #endregion
    }
}

