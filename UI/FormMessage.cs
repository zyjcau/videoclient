using System;
using System.Drawing;
using System.Windows.Forms;

namespace VideoClient.UI
{
    public partial class FormMessage : Form
    {
        private FlowLayoutPanel _panel;
        private Label _labelMessage;
        private PictureBox _iconPictureBox;

        int marginInElements = 16;

        public FormMessage()
        {
            InitializeComponent();

            // 设置透明无边居中窗体
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            Opacity = 1;
            TopMost = true;

            //--

            //--
            _labelMessage = new Label()
            {
                Name = "label_name",
                Font = new Font("宋体", 18F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134))),
                Location = new Point(0, 0),
                Margin = new Padding(0, 0, 0, 0),
                Size = new Size(640, 60),
                // AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Text = "loading"
            };
            // Controls.Add(_labelMessage);
            //--
            _iconPictureBox = new PictureBox()
            {
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(60, 60),
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            // Controls.Add(_iconPictureBox);
            _iconPictureBox.Load(GetIconDir() + "logo_lss.png");
            //--
            _panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(0, 0),
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(32, 16, 32, 16),
                Size = new Size(
                    _iconPictureBox.Width + _labelMessage.Width + marginInElements + 64,
                    _iconPictureBox.Height + 32),
                // BackColor = Color.Red
                // BackColor = Color.FromArgb(100, 0, 0, 0)
            };
            //--
            _panel.Controls.Add(_iconPictureBox);
            _panel.Controls.Add(_labelMessage);
            Controls.Add(_panel);

            _panel.Location = new Point(
                ClientRectangle.Width / 2 - _panel.Width / 2,
                ClientRectangle.Height / 2 - _panel.Height / 2
            );

            CenterToScreen();
        }

        private void SyncWidth()
        {
            // _panel.Size = new Size(
            //     _iconPictureBox.Width + _labelMessage.Width + marginInElements + 64,
            //     _iconPictureBox.Height + 32);
        }

        public void ShowWithParent(IWin32Window owner, string message)
        {
            RefreshMessage(message);
            Show(owner);
        }

        public void RefreshMessage(string message)
        {
            _labelMessage.Text = message;
            SyncWidth();
        }

        private string GetIconDir()
        {
            return $"{Application.StartupPath}\\wwwroot\\res\\img\\";
        }
    }
}