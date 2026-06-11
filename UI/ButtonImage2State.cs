using System;
using System.Drawing;
using System.Windows.Forms;
using NLog;

namespace VideoClient.UI
{
    public class ButtonImage2State : PictureBox
    {
        private String state1IconPath, state2IconPath;

        private int height;

        public ButtonImage2State(int height, string state1IconPath, string state2IconPath, EventHandler handler)
        {
            this.Height = height;

            this.state1IconPath = state1IconPath;
            this.state2IconPath = state2IconPath;

            Click += handler;
            MouseHover += (sender, args) =>
            {
                BackColor = Color.FromArgb(48, 48, 48);
            };
            MouseLeave += (sender, args) =>
            {
                BackColor = Color.Transparent;
            };

            Size = new Size(height, height);
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            // Padding = new Padding(4);

            SizeMode = PictureBoxSizeMode.StretchImage;

            UseState1(true);
        }

        /**
         * 反之使用状态2
         */
        public void UseState1(bool isUseState1)
        {
            Load(GetIconDir() + (isUseState1 ? state1IconPath : state2IconPath));
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