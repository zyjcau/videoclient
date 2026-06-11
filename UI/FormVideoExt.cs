using System;
using System.Drawing;
using System.Windows.Forms;
using NLog;
using NLog.Fluent;
using VideoClient.Manager;
using VideoClient.UI;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;

namespace VidyoClient
{
    public partial class FormVideoExt : Form
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        //1表示第二屏，依次往后
        public int _screenIndex;

        private Screen _screen;
        public bool _isFullScreen;

        public VideoRendererContainer _videoRendererContainer;

        // public Connector _connector;

        delegate void BooleanArgReturningVoidDelegate(bool b);

        public FormVideoExt(int index, Screen screen, bool isFullScreen)
        {
            _screenIndex = index;
            _screen = screen;
            _isFullScreen = isFullScreen;

            InitializeComponent();

            InitView();
            SetScreenIndexVisible();

            Shown += OnShown;
            Resize += SyncFormSize;
            FormClosing += Form_Close;
        }

        private void OnShown(object sender, EventArgs e)
        {
            if (_isFullScreen)
            {
                SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                Util.AssignFormFullScreenToScreen(this, _screen);
            }
            else
            {
                Left = _screen.Bounds.X;
                Top = _screen.Bounds.Y;
                WindowState = FormWindowState.Maximized;
            }

            // // this.WindowState = FormWindowState.Maximized;

            // this.Left = Screen.AllScreens[_screenIndex - 1].Bounds.Width;
            // this.Top = 0;
            // this.Size = new System.Drawing.Size(Screen.AllScreens[_screenIndex].Bounds.Width, Screen.AllScreens[_screenIndex].Bounds.Height);

            // Log("The {0} screen bounds is ([{1},{2}] {3}x{4}). device name is {5}",
            //     _screenIndex,
            //     DesktopBounds.X,
            //     DesktopBounds.Y,
            //     DesktopBounds.Width,
            //     DesktopBounds.Height,
            //     Screen.AllScreens[_screenIndex].DeviceName);
        }

        private void SyncFormSize(object sender, EventArgs e)
        {
            Log("SyncFormSize " + Width + " x " + Height + " , " + ClientRectangle.Width +
                "x" + ClientRectangle.Height);
            panel1.Margin = new Padding(0, 0, 0, 0);
            panel1.Location = new Point(0, 0);
            panel1.Size = new Size(ClientRectangle.Width, ClientRectangle.Height);
            if (IsCameraPreviewed())
            {
                _videoRendererContainer?._connector.ShowViewAt(panel1.Handle, 0, 0,
                    (uint)panel1.Width,
                    (uint)panel1.Height);
            }

            label_screen_index.Size = new Size(450, 200);
            label_screen_index.Location =
                new Point(ClientRectangle.Width / 2 - label_screen_index.Width / 2,
                    ClientRectangle.Height / 2 - label_screen_index.Height / 2);

            _videoRendererContainer?.RefreshLayout();
        }

        private void Form_Close(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult dlgRes = MessageBox.Show("如果需要退出程序，请关闭主窗口。", "知道了",
                    MessageBoxButtons.OK);
                if (dlgRes == DialogResult.OK)
                {
                    e.Cancel = true;
                }
            }
        }

        private void InitView()
        {
            Text = GetWindowsName();

            int formWidth = ClientRectangle.Width;
            int formHeight = ClientRectangle.Height;

            panel1.Margin = new Padding(0, 0, 0, 0);
            panel1.Location = new Point(0, 0);
            panel1.Size = new Size(formWidth, formHeight);

            label_screen_index.Size = new Size(450, 200);
            label_screen_index.Location =
                new Point(formWidth / 2 - label_screen_index.Width / 2,
                    formHeight / 2 - label_screen_index.Height / 2);
            label_screen_index.Text = GetWindowsName();
            label_screen_index.Font = new Font("Segoe UI", 18F, FontStyle.Regular,
                GraphicsUnit.Point, ((byte)(0)));
            label_screen_index.ForeColor = Color.White;
            label_screen_index.BackColor = Color.Black;
        }

        private string GetWindowsName()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                return $"显示屏{_screenIndex + 1}";
            }
            else
            {
                return $"窗口{_screenIndex + 1}";
            }
        }

        public void SetScreenIndexDisplay(bool display)
        {
            RunOnUIThread((Action)(() =>
            {
                if (display)
                {
                    SetScreenIndexVisible();
                }
                else
                {
                    SetScreenIndexGone();
                }
            }));
        }

        private void SetScreenIndexVisible()
        {
            SetScreenIndexGone();
            panel1.Controls.Add(label_screen_index);
            label_screen_index.Visible = true;
        }

        private void SetScreenIndexGone()
        {
            if (panel1.Controls.Contains(label_screen_index))
            {
                label_screen_index.Visible = false;
                panel1.Controls.Remove(label_screen_index);
            }
        }

        private Panel _previewCameraPanel; //空表示停止状态，有值则表示预览中

        public void StartPreviewLastSelectedCamera(VideoManager videoManager)
        {
            if (!videoManager.isConnected) //只能在没有入会的情况下预览
            {
                // LocalCamera localCamera;

                String cameraName = videoManager.cameraManager.AssignLastSelectedCamera();

                if (String.Equals(CameraListener.DEVICE_NONE, cameraName))
                {
                    Log("StartPreviewCamera None success.");
                    return;
                }

                StartPreviewCamera(videoManager, cameraName);
            }
        }

        private void StartPreviewCamera(VideoManager videoManager, String cameraName)
        {
            LocalCamera localCamera = videoManager.cameraManager.cameras[cameraName];
            if (null != localCamera)
            {
                videoManager.connector.AssignViewToLocalCamera(panel1.Handle, localCamera, false, true);
                
                //根据配置设置主摄是否镜像展示
                if (
                    videoManager.cameraManager?._cameraConfigs != null &&
                    videoManager.cameraManager._cameraConfigs.Count > 0)
                {
                    bool isMirroring = videoManager.cameraManager._cameraConfigs[0].isMirroring;
                    SetRendererMirroring(videoManager.connector, isMirroring);
                }

                videoManager.connector.ShowViewAt(panel1.Handle, 0, 0,
                    (uint)panel1.Width,
                    (uint)panel1.Height);
                _previewCameraPanel = panel1;
                SetScreenIndexGone();
                Log("StartPreviewCamera success. -> ", cameraName);
            }
            else
            {
                _previewCameraPanel = null;
                SetScreenIndexVisible();
                Log("StartPreviewCamera failed. -> ", cameraName);
            }
        }

        private void SetRendererMirroring(Connector connector, bool mirroring)
        {
            SetRendererOptions(connector, "{\"EnablePreviewMirroring\":" + (mirroring ? "true" : "false") + "}");
        }

        private void SetRendererOptions(Connector connector, string options)
        {
            connector.SetRendererOptionsForViewId(panel1.Handle, options);
        }

        public void StopPreviewCamera(VideoManager videoManager)
        {
            if (IsCameraPreviewed())
            {
                videoManager.connector.HideView(panel1.Handle);
                _previewCameraPanel = null;
                Log("StopPreviewCamera success.");
            }
            else
            {
                Log("StopPreviewCamera failed, not previewing.");
            }
        }

        public void ChangePreviewCamera(VideoManager videoManager, String cameraName)
        {
            if (String.Equals(CameraListener.DEVICE_NONE, cameraName))
            {
                StopPreviewCamera(videoManager);
            }
            else
            {
                if (IsCameraPreviewed())
                {
                    StopPreviewCamera(videoManager);
                }

                StartPreviewCamera(videoManager, cameraName);
            }

            Log("ChangePreviewCamera({})", cameraName);
        }

        private bool IsCameraPreviewed()
        {
            return _previewCameraPanel != null;
        }

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return BeginInvoke(method);
        }

        private void Log(string content)
        {
            _logger.Info("[FormVideoExt] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[FormVideoExt] " + content, args);
        }
    }
}