using System;
using System.Drawing;
using System.Windows.Forms;
using VideoClient.Manager;

namespace VideoClient.UI
{
    /// <summary>
    /// 小窗模式控制器
    /// </summary>
    public class SmallWindowController
    {
        public const int HeaderHeight = 48;
        public const int VideoHeight = 480;
        public const int FooterHeight = 48;

        ///正方形窗口
        public const int WindowHeight = HeaderHeight + VideoHeight + FooterHeight;

        public const int WindowWidth = WindowHeight;

        public readonly Panel panelHeader;
        private readonly Panel _panelVideo;
        public readonly Panel panelFooter;

        //
        private FormWindowState _lastWindowState = FormWindowState.Normal;
        private Size _lastWindowSize, _lastVideoSize;
        private Point _lastWindowLocation, _lastVideoLocation;

        public SmallWindowController(Panel panelHeader, Panel panelVideo, Panel panelFooter)
        {
            this.panelHeader = panelHeader;
            _panelVideo = panelVideo;
            this.panelFooter = panelFooter;
            //
            Init();
        }

        private ButtonImage2State stopShareButton, muteMicButton, muteCameraButton;

        private void Init()
        {
            //头
            panelHeader.Location = new Point(0, 0);
            panelHeader.Size = new Size(WindowWidth, HeaderHeight);
            //脚
            panelFooter.Location = new Point(0, HeaderHeight + VideoHeight);
            panelFooter.Size = new Size(WindowWidth, FooterHeight);
            //

            //-- header buttons
            ButtonImage2State restoreButton =
                new ButtonImage2State(40,
                    "fullscreen.png", "fullscreen.png",
                    OnRestoreClick);
            panelHeader.Controls.Add(restoreButton);
            restoreButton.Location = new Point(8, 4);
            //-
            stopShareButton =
                new ButtonImage2State(40,
                    "icon_stop_share.png", "icon_stop_share.png",
                    OnStopShareClick);
            panelHeader.Controls.Add(stopShareButton);
            stopShareButton.Location = new Point(WindowWidth - stopShareButton.Width - 4, 4);

            //-- footer buttons
            ButtonImage2State leaveButton =
                new ButtonImage2State(40,
                    "callEnd.png", "callEnd.png",
                    OnLeaveClick);
            panelFooter.Controls.Add(leaveButton);
            leaveButton.Location = new Point(8, 4);
            //-
            muteMicButton =
                new ButtonImage2State(38,
                    "icon_mic_mute.png", "icon_mic_open.png",
                    OnMuteMicClick);
            panelFooter.Controls.Add(muteMicButton);
            muteMicButton.Location = new Point(WindowWidth / 2 - muteMicButton.Width - 4, 5);
            // muteMicButton.UseState1(VideoManager.GetInstance().micPrivacyOnAppLaunch);
            //-
            muteCameraButton =
                new ButtonImage2State(38,
                    "icon_camera_mute.png", "icon_camera_open.png",
                    OnMuteCameraClick);
            panelFooter.Controls.Add(muteCameraButton);
            muteCameraButton.Location = new Point(WindowWidth / 2 + 4, 5);
            // muteCameraButton.UseState1(VideoManager.GetInstance().cameraPrivacyOnAppLaunch);
        }

        private void OnRestoreClick(object sender, EventArgs e)
        {
            Restore();
        }

        private void OnStopShareClick(object sender, EventArgs e)
        {
            Restore();
            ((FormMain)_panelVideo.FindForm())?.SetTopMost(false);
            VideoManager.GetInstance().StopShare();
        }

        private void OnLeaveClick(object sender, EventArgs e)
        {
            Restore();
            VideoManager.GetInstance().Disconnect();
        }

        private void OnMuteMicClick(object sender, EventArgs e)
        {
            // if (null != _videoManager) SetMuteMicButtonState(_videoManager.ToggleMicrophonePrivacy());
            bool privacy = VideoManager.GetInstance().ToggleMicrophonePrivacy();
            muteMicButton.UseState1(privacy);
        }

        private void OnMuteCameraClick(object sender, EventArgs e)
        {
            // if (null != _videoManager) SetMuteCameraButtonState(_videoManager.ToggleCameraPrivacy());
            bool privacy = VideoManager.GetInstance().ToggleCameraPrivacy();
            muteCameraButton.UseState1(privacy);
        }

        public bool IsApplied()
        {
            return panelHeader.Visible;
        }

        public void Apply()
        {
            if (IsApplied()) return;

            Form form = _panelVideo.FindForm();
            SaveState(form);
            ApplyLayout();
            ApplyWindowState(form);
            ApplyWindowSizeLocation(form);
            SyncButtonState();
            // //test code
            // if (!VideoManager.GetInstance().isConnected)
            // {
            //     VideoManager.GetInstance().SetVideoVisible(true);
            // }
        }

        void ApplyLayout()
        {
            panelHeader.Visible = true;
            panelFooter.Visible = true;
            // //头
            // panelHeader.Location = new Point(0, 0);
            // panelHeader.Size = new Size(WindowWidth, HeaderHeight);
            //视频
            // _panelVideo.Location = new Point(0, HeaderHeight);
            // _panelVideo.Size = new Size(WindowWidth, VideoHeight);
            VideoManager.GetInstance().SetVideoLocationSize(0, HeaderHeight, WindowWidth, VideoHeight);
            // //脚
            // panelFooter.Location = new Point(0, HeaderHeight + VideoHeight);
            // panelFooter.Size = new Size(WindowWidth, FooterHeight);
        }

        void ApplyWindowState(Form form)
        {
            form.WindowState = FormWindowState.Normal;
            form.FormBorderStyle = FormBorderStyle.Fixed3D;
            form.MaximizeBox = false;
        }

        void ApplyWindowSizeLocation(Form form)
        {
            // 设置窗口尺寸
            form.ClientSize = new Size(WindowWidth, WindowHeight); // 替换窗口宽度和窗口高度为实际值
            // 获取屏幕工作区域尺寸（不包括任务栏）
            Rectangle workingArea = Screen.GetWorkingArea(form);
            // 设置窗口位置
            form.Location = new Point(workingArea.Right - form.Size.Width - 16,
                workingArea.Bottom - form.Size.Height - 16);
        }

        void SyncButtonState()
        {
            stopShareButton.Visible = VideoManager.GetInstance().IsShareStarted();
            muteMicButton.UseState1(VideoManager.GetInstance().micPrivacyOnAppLaunch);
            muteCameraButton.UseState1(VideoManager.GetInstance().cameraPrivacyOnAppLaunch);
        }

        public void Restore()
        {
            Form form = _panelVideo.FindForm();
            RestoreLayout();
            RestoreWindowState(form);
            RestoreWindowSizeLocation(form);
            //test code
            if (!VideoManager.GetInstance().isConnected)
            {
                VideoManager.GetInstance().SetVideoVisible(false);
            }
        }

        public void RestoreLayout()
        {
            panelHeader.Visible = false;
            panelFooter.Visible = false;
            //
            VideoManager.GetInstance().SetVideoLocationSize(
                _lastVideoLocation.X, _lastVideoLocation.Y,
                _lastVideoSize.Width, _lastVideoSize.Height);
        }

        public void RestoreWindowState(Form form)
        {
            form.WindowState = _lastWindowState;
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
        }

        public void RestoreWindowSizeLocation(Form form)
        {
            if (null != _lastWindowSize && null != _lastWindowLocation)
            {
                // 设置窗口尺寸
                form.Size = new Size(_lastWindowSize.Width, _lastWindowSize.Height); // 替换窗口宽度和窗口高度为实际值
                // 设置窗口位置
                form.Location = new Point(_lastWindowLocation.X, _lastWindowLocation.Y);
            }
        }

        private void SaveState(Form form)
        {
            _lastWindowState = form.WindowState;
            _lastWindowSize = new Size(form.Size.Width, form.Size.Height);
            _lastWindowLocation = new Point(form.Location.X, form.Location.Y);
            _lastVideoSize = new Size(_panelVideo.Size.Width, _panelVideo.Size.Height);
            _lastVideoLocation = new Point(_panelVideo.Location.X, _panelVideo.Location.Y);
        }
    }
}