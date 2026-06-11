using System;
using System.Drawing;
using System.Windows.Forms;
using NLog;

namespace VideoClient.UI
{
    public class ButtonImage3State : PictureBox
    {
        private String state1IconPath, state2IconPath, state3IconPath;

        private int height;

        public ButtonImage3State(
            int height,
            string state1IconPath,
            string state2IconPath,
            string state3IconPath,
            EventHandler handler)
        {
            this.Height = height;

            this.state1IconPath = state1IconPath;
            this.state2IconPath = state2IconPath;
            this.state3IconPath = state3IconPath;

            Click += handler;
            MouseHover += (sender, args) => { BackColor = Color.FromArgb(48, 48, 48); };
            MouseLeave += (sender, args) => { BackColor = Color.Transparent; };

            Size = new Size(height, height);
            Margin = Padding.Empty;
            Padding = Padding.Empty;

            SizeMode = PictureBoxSizeMode.StretchImage;

            UseState1();
        }

        public void UseState1()
        {
            Load(GetIconDir() + state1IconPath);
        }

        public void UseState2()
        {
            Load(GetIconDir() + state2IconPath);
        }

        public void UseState3()
        {
            Load(GetIconDir() + state3IconPath);
        }

        private string GetIconDir()
        {
            return $"{Application.StartupPath}\\wwwroot\\res\\icon\\";
        }

        #region 通用函数

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return BeginInvoke(method);
        }

        private void Log(string content)
        {
            _logger.Info("[ButtonImage2State] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[ButtonImage2State] " + content, args);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}