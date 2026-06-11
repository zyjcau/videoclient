using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace VideoClient.UI
{
    partial class FormPictureView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPictureView));
            this.panelTop = new System.Windows.Forms.Panel();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.buttonOpenFileDir = new System.Windows.Forms.Button();
            this.buttonStartCollab = new System.Windows.Forms.Button();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.pictureBox);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1258, 588);
            this.panelTop.TabIndex = 0;
            // 
            // pictureBox
            // 
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(1258, 588);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.buttonOpenFileDir);
            this.panelBottom.Controls.Add(this.buttonStartCollab);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBottom.Location = new System.Drawing.Point(0, 588);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Padding = new System.Windows.Forms.Padding(12);
            this.panelBottom.Size = new System.Drawing.Size(1258, 76);
            this.panelBottom.TabIndex = 1;
            // 
            // buttonOpenFileDir
            // 
            this.buttonOpenFileDir.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonOpenFileDir.Location = new System.Drawing.Point(926, 12);
            this.buttonOpenFileDir.Margin = new System.Windows.Forms.Padding(0);
            this.buttonOpenFileDir.Name = "buttonOpenFileDir";
            this.buttonOpenFileDir.Size = new System.Drawing.Size(160, 52);
            this.buttonOpenFileDir.TabIndex = 1;
            this.buttonOpenFileDir.Text = "打开文件所在目录";
            this.buttonOpenFileDir.UseVisualStyleBackColor = true;
            this.buttonOpenFileDir.Click += new System.EventHandler(this.buttonOpenFileDir_Click);
            // 
            // buttonStartCollab
            // 
            this.buttonStartCollab.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonStartCollab.Location = new System.Drawing.Point(1086, 12);
            this.buttonStartCollab.Margin = new System.Windows.Forms.Padding(0);
            this.buttonStartCollab.Name = "buttonStartCollab";
            this.buttonStartCollab.Size = new System.Drawing.Size(160, 52);
            this.buttonStartCollab.TabIndex = 0;
            this.buttonStartCollab.Text = "发起白板讨论";
            this.buttonStartCollab.UseVisualStyleBackColor = true;
            this.buttonStartCollab.Click += new System.EventHandler(this.buttonStartCollab_Click);
            // 
            // FormPictureView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1258, 664);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormPictureView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "截取图像";
            this.panelTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button buttonOpenFileDir;

        private System.Windows.Forms.Button buttonStartCollab;

        private System.Windows.Forms.PictureBox pictureBox;

        private System.Windows.Forms.Panel panelBottom;

        private System.Windows.Forms.Panel panelTop;

        #endregion
    }
}