using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NLog;
using VideoClient.Manager;

namespace VideoClient.UI
{
    public partial class FormPictureView : Form
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _filePath;

        public FormPictureView(string filePath)
        {
            _filePath = filePath;

            InitializeComponent();

            panelTop.Height = (int)(ClientRectangle.Height * 0.9);
            if (!IsWhiteBoardAvailable())
            {
                buttonStartCollab.Visible = false;
            }

            Resize += FormPictureView_Resize;

            _logger.Info("[FormPictureView] load image from " + _filePath);
            pictureBox.LoadAsync(_filePath);
        }

        private void FormPictureView_Resize(object sender, EventArgs e)
        {
            panelTop.Height = (int)(ClientRectangle.Height * 0.9);
        }


        private void buttonStartCollab_Click(object sender, EventArgs e)
        {
            if (!IsWhiteBoardAvailable())
            {
                MessageBox.Show("请联系管理员配置电子白板服务后，才可使用此功能！");
                return;
            }

            VideoManager.GetInstance().OpenTangoDrawWindowWithImage(_filePath);
        }

        private bool IsWhiteBoardAvailable()
        {
            return !string.IsNullOrEmpty(VideoManager.GetInstance().subsystemTangoDrawUrl);
        }

        private void buttonOpenFileDir_Click(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo nParent = Directory.GetParent(_filePath);
                if (null != nParent) OpenFolder(nParent.FullName);
            }
            catch (Exception nException)
            {
                _logger.Error(nException.ToString());
            }
        }

        private static void OpenFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            Process process = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("Explorer.exe")
            {
                Arguments = folderPath
            };
            process.StartInfo = psi;

            try
            {
                process.Start();
            }
            finally
            {
                process.Close();
            }
        }
    }
}