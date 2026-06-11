using System.ComponentModel;
using System.Windows.Forms;

namespace VideoClient.UI
{
    partial class FormSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSettings));
            this.panel_cef = new System.Windows.Forms.Panel();
            this.panel_video = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panel_cef
            // 
            this.panel_cef.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_cef.Location = new System.Drawing.Point(0, 0);
            this.panel_cef.Margin = new System.Windows.Forms.Padding(2);
            this.panel_cef.Name = "panel_cef";
            this.panel_cef.Size = new System.Drawing.Size(1578, 844);
            this.panel_cef.TabIndex = 0;
            // 
            // panel_video
            // 
            this.panel_video.Location = new System.Drawing.Point(110, 267);
            this.panel_video.Margin = new System.Windows.Forms.Padding(2);
            this.panel_video.Name = "panel_video";
            this.panel_video.Size = new System.Drawing.Size(397, 38);
            this.panel_video.TabIndex = 1;
            this.panel_video.Visible = false;
            // 
            // FormSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1578, 844);
            this.Controls.Add(this.panel_video);
            this.Controls.Add(this.panel_cef);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormSettings";
            this.Text = "摄像头设置";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panel_video;

        private System.Windows.Forms.Panel panel_cef;

        #endregion
    }
}