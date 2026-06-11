using System.ComponentModel;
using System.Drawing;

namespace VidyoClient
{
    partial class FormVideoExt
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label_screen_index = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label_screen_index);
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(517, 284);
            this.panel1.TabIndex = 0;
            // 
            // label_screen_index
            // 
            this.label_screen_index.Font = new System.Drawing.Font("Microsoft Sans Serif", 60F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label_screen_index.Location = new System.Drawing.Point(190, 108);
            this.label_screen_index.Name = "label_screen_index";
            this.label_screen_index.Size = new System.Drawing.Size(109, 56);
            this.label_screen_index.TabIndex = 0;
            this.label_screen_index.Text = "label1";
            this.label_screen_index.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label_screen_index.Visible = false;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 300);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormVideoExt";
            this.Text = "Form2";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public System.Windows.Forms.Label label_screen_index;

        public System.Windows.Forms.Panel panel1;

        #endregion
    }
}