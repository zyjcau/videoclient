using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NLog;
using VideoClient.UI;
using VideoClient.Util;
using VidyoClient;
using VidyoClient.Ext;
using Application = System.Windows.Forms.Application;

namespace VideoClient.VidyoClient.Ext
{
    public class VideoRenderer : VideoFrameListener.IOnSnapshotSavedListener
    {
        public VideoRenderer(
            int id,
            int containerId,
            Panel panel
        )
        {
            this.id = id;
            this.containerId = containerId;
            _name = panel.Name;
            _panel = panel;
            _videoFrameListener = new VideoFrameListener(this);
            ClearVideoSource();
            CreateFuncPanelAtLeft();
            CreateFuncPanelAtRight();
        }

        private string GetIconDir()
        {
            return $"{Application.StartupPath}\\wwwroot\\res\\icon";
        }

        private void CreateFuncPanelAtRight()
        {
            funcPanelAtRight = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(_panel.Width - 96, 0),
                Size = new Size(96, funcPanelHeight),
                Margin = Padding.Empty, Padding = Padding.Empty,
                // BackColor = Color.Gray,
                Visible = true
            };
        }

        private void CreateFuncPanelAtLeft()
        {
            funcPanelAtLeft = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(0, 0),
                Size = new Size(64, funcPanelHeight),
                Margin = Padding.Empty, Padding = Padding.Empty,
                // BackColor = Color.Gray,
                Visible = true
            };
        }

        private void CreateSnapshotBtn()
        {
            snapshotBtn = new PictureBox()
            {
                Size = new Size(32, 32), Margin = Padding.Empty, Padding = Padding.Empty
            };
            snapshotBtn.Location = new Point(0, 0);
            snapshotBtn.Load($"{GetIconDir()}\\snapshot.png");
            snapshotBtn.Click += OnSnapshotClick;
        }

        private void CreateRecordBtn()
        {
            recordBtn = new PictureBox()
            {
                Size = new Size(32, 32), Margin = Padding.Empty, Padding = Padding.Empty
            };
            recordBtn.Location = new Point(40, 0);
            recordBtn.Load($"{GetIconDir()}\\record_start.png");
            recordBtn.Click += OnRecordClick;
        }

        private void CreatePinBtn()
        {
            pinBtn = new PictureBox()
            {
                Size = new Size(32, 32), Margin = Padding.Empty, Padding = Padding.Empty
            };
            pinBtn.Load($"{GetIconDir()}\\icon_pin.png");
            pinBtn.Click += OnPinClick;
        }

        private void CreateExchangeBtn()
        {
            exchangeBtn = new PictureBox()
            {
                Size = new Size(32, 32), Margin = Padding.Empty, Padding = Padding.Empty,
                // Visible = false
            };
            exchangeBtn.Load($"{GetIconDir()}\\exchange.png");
            exchangeBtn.Click += OnExchangeClick;
        }

        private void CreateMaximizeBtn()
        {
            maximizeBtn = new PictureBox
            {
                Size = new Size(32, 32), Margin = Padding.Empty, Padding = Padding.Empty
            };
            // maximizeBtn.Text = "全屏";
            // maximizeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // maximizeBtn.BackColor = Color.Aquamarine;
            maximizeBtn.Load($"{GetIconDir()}\\fullscreen_open.png");
            maximizeBtn.Click += OnMaximizeClick;
        }

        public void ShowTools()
        {
            if (null != recordBtn)
            {
                funcPanelAtLeft.Controls.Add(recordBtn);
            }

            if (
                null != snapshotBtn &&
                IsSupportListenFrame(_videoSource)
            )
            {
                funcPanelAtLeft.Controls.Add(snapshotBtn);
            }

            if (null != maximizeBtn)
            {
                funcPanelAtRight.Controls.Add(maximizeBtn);
            }

            if (null != pinBtn)
            {
                funcPanelAtRight.Controls.Add(pinBtn);
            }

            if (null != exchangeBtn)
            {
                funcPanelAtRight.Controls.Add(exchangeBtn);
            }

            _panel.Controls.Add(funcPanelAtLeft);
            _panel.Controls.SetChildIndex(funcPanelAtLeft, 0);
            _panel.Controls.Add(funcPanelAtRight);
            _panel.Controls.SetChildIndex(funcPanelAtRight, 0);
        }

        public void DismissTools()
        {
            if (null != snapshotBtn)
            {
                funcPanelAtLeft.Controls.Remove(snapshotBtn);
            }

            if (null != recordBtn)
            {
                funcPanelAtLeft.Controls.Remove(recordBtn);
            }

            if (null != pinBtn)
            {
                funcPanelAtRight.Controls.Remove(pinBtn);
            }

            if (null != exchangeBtn)
            {
                funcPanelAtRight.Controls.Remove(exchangeBtn);
            }

            if (null != maximizeBtn)
            {
                funcPanelAtRight.Controls.Remove(maximizeBtn);
            }

            _panel.Controls.Remove(funcPanelAtLeft);
            _panel.Controls.Remove(funcPanelAtRight);
        }

        private void ResetTools()
        {
            DismissTools();
            ShowTools();
        }

        public void SetMaximizedState(bool maximized)
        {
            _isMaximize = maximized;
            maximizeBtn.Load(
                _isMaximize
                    ? $"{GetIconDir()}\\fullscreen_close.png"
                    : $"{GetIconDir()}\\fullscreen_open.png");
        }

        public void SetPinState(bool pin)
        {
            _isPin = pin;
            pinBtn.Load(
                _isPin
                    ? $"{GetIconDir()}\\icon_pin_cancel.png"
                    : $"{GetIconDir()}\\icon_pin.png");
        }

        public void SetPinBtnVisible(bool visible)
        {
            if (null != pinBtn)
            {
                pinBtn.Visible = visible;
                pinBtn.Enabled = visible;
            }
        }

        public void SetExchangeBtnVisible(bool visible)
        {
            if (null != exchangeBtn)
            {
                exchangeBtn.Visible = visible;
                exchangeBtn.Enabled = visible;
            }
        }

        public readonly int id;
        public readonly int containerId;
        public readonly String _name;
        public Panel _panel;
        public FlowLayoutPanel funcPanelAtRight, funcPanelAtLeft;
        public readonly int funcPanelHeight = 32;
        public string _sourceKey; //空表示未渲染，用于从remoteCameras和share中取出Remote
        public VideoSource _videoSource;
        public IRendererMaximizeListener _maximizeListener;
        public IRendererPinListener _pinListener;
        public IRendererExchangeListener _exchangeListener;

        /**
         * remote_camera,remote_share，对应remoteCameras、remoteWindowshare两个字典
         * local_camera、local_virtualsource
         */
        public string _type;

        private AtomicBoolean inUsed = new AtomicBoolean(false);

        public bool _isVoiceActivated; //是否用于语音激励
        // public bool _isMirroring; //是否镜像画面

        public PictureBox maximizeBtn, snapshotBtn, recordBtn, pinBtn, exchangeBtn;

        // public bool _isShowMaximizeBtn;
        public bool _isMaximize, _isPin;
        private bool _isListenFrame;
        private bool _isStartedListenFrame;
        private VideoFrameListener _videoFrameListener;

        public string GetFingerPrint()
        {
            return $"{containerId}_{_panel.Name}";
        }

        public void EnableMaximizeBtn()
        {
            if (null == maximizeBtn) CreateMaximizeBtn();
        }

        public void EnableSnapshot()
        {
            _isListenFrame = true;
            if (null == snapshotBtn) CreateSnapshotBtn();
        }

        public void EnableRecord()
        {
            _isListenFrame = true;
            if (null == recordBtn) CreateRecordBtn();
        }

        public void EnablePinBtn()
        {
            if (null == pinBtn) CreatePinBtn();
        }

        public void EnableExchangeBtn()
        {
            if (null == exchangeBtn) CreateExchangeBtn();
        }

        private void OnMaximizeClick(object sender, EventArgs e)
        {
            _maximizeListener?.OnRequestRendererSizeChange(this, !_isMaximize);
        }

        private void OnSnapshotClick(object sender, EventArgs e)
        {
            if (_videoSource != null)
            {
                _videoFrameListener.TakeSnapshot(_videoSource.label);
            }
            else
            {
                Log("OnSnapshotClick failed, _videoSource is null");
            }
        }

        public void OnSnapshotSaved(string filePath)
        {
            RunOnUIThread((Action)(() => { new FormPictureView(filePath).Show(); }));
        }

        private void OnRecordClick(object sender, EventArgs e)
        {
            if (_videoSource != null)
            {
                bool recording = _videoFrameListener.ToggleRecord(_videoSource.label);
                recordBtn.Load(recording ? $"{GetIconDir()}\\record_stop.png" : $"{GetIconDir()}\\record_start.png");
            }
            else
            {
                Log("OnRecordClick failed, _videoSource is null");
            }
        }

        private void OnPinClick(object sender, EventArgs e)
        {
            _pinListener?.OnRequestPin(this, !_isPin);
        }

        private void OnExchangeClick(object sender, EventArgs e)
        {
            _exchangeListener?.OnExchangeClick(this);
        }

        private bool IsSupportListenFrame(VideoSource videoSource)
        {
            return null != videoSource &&
                   (
                       videoSource.IsLocalCamera() ||
                       videoSource.IsRemoteCamera() ||
                       videoSource.IsRemoteWindowShare()
                       // ||_videoSource.IsLocalCamera2()//发现 SDK 不支持同时监听1、2路
                   );
        }

        private void StartListenFrame(Connector connector)
        {
            if (!_isListenFrame || _videoSource == null || !IsSupportListenFrame(_videoSource)) return;

            bool isCompleted = _videoSource.RegisterFrameEventListener(connector, _videoFrameListener);

            _isStartedListenFrame = true;
        }

        private void StopListenFrame(Connector connector)
        {
            if (!_isStartedListenFrame) return;

            _videoFrameListener.StopRecord();

            _videoSource?.UnregisterFrameEventListener(connector);

            _isStartedListenFrame = false;
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            // Log("OnDoubleClick({0}-{1})", _screenIndex, _id);
            _maximizeListener?.OnRequestRendererSizeChange(this, !_isMaximize);
        }

        private Point _originPoint;
        private Size _originSize;

        public void SetSize(int x, int y, int width, int height)
        {
            if (_isMaximize)
            {
                _panel.Location = new Point(x, y);
                _panel.Size = new Size(width, height);
            }
            else
            {
                _originPoint = new Point(x, y);
                _originSize = new Size(width, height);
                _panel.Location = _originPoint;
                _panel.Size = _originSize;
            }

            RefreshFuncPanelSize();
        }

        private void RefreshFuncPanelSize()
        {
            //左侧
            funcPanelAtLeft.Size = new Size(funcPanelAtLeft.Controls.Count * 32, funcPanelHeight);
            //右侧，功能栏宽度随可见按钮数变化
            int visibleRightButtonCount = 0;
            foreach (Control control in funcPanelAtRight.Controls)
            {
                if (control.Enabled) //bug 有可能整个视图不可见，但同时在操作视图
                {
                    visibleRightButtonCount += 1;
                }
            }

            funcPanelAtRight.Size = new Size(visibleRightButtonCount * 32, funcPanelHeight);
            funcPanelAtRight.Location = new Point(_panel.Width - funcPanelAtRight.Width, 0);
        }

        public void DrawBorder(bool isSpeaking)
        {
            _panel.Invalidate();
            Graphics graphics = _panel.CreateGraphics();
            Pen pen = new Pen(
                isSpeaking ? Color.Brown : Color.Gray,
                1);
            graphics.DrawRectangle(
                pen,
                _panel.ClientRectangle.X,
                _panel.ClientRectangle.Y,
                _panel.ClientRectangle.X +
                _panel.ClientRectangle.Width,
                _panel.ClientRectangle.Y +
                _panel.ClientRectangle.Height
            );
        }

        public void PlaySpeakingAnim()
        {
            // DrawBorder(true);
            //todo
        }

        public VideoParticipant GetVideoSourceFrom()
        {
            return _videoSource?.from;
        }

        public bool TrySetVideoSource(VideoSource videoSource)
        {
            if (null != videoSource && videoSource.RequestUse())
            {
                _videoSource = videoSource;
                _sourceKey = videoSource.GetSourceKey();
                _type = videoSource.type;
                _videoSource.positionOfScreen = containerId;
                _videoSource.positionOfRendererContainer = id;
                return true;
            }
            else
            {
                ClearVideoSource();
                return false;
            }
        }

        public void ClearVideoSource()
        {
            _sourceKey = VideoSource.SOURCE_KEY_NO_DISPLAY;
            _type = VideoSourceType.TYPE_NONE;
            if (null != _videoSource)
            {
                _videoSource.Release();
                _videoSource.isRendering = false;
                _videoSource.positionOfScreen = -1;
                _videoSource.positionOfRendererContainer = -1;
            }

            _videoSource = null;
        }

        public bool IsInUse()
        {
            return inUsed.Get();
        }

        public bool RequestUse()
        {
            if (!IsInUse() && inUsed.CompareAndSet(false, true))
            {
                return true;
            }

            return false;
        }

        public void Release()
        {
            inUsed.Set(false);
        }

        public bool IsNone()
        {
            return VideoSourceType.TYPE_NONE.Equals(_type);
        }

        public void SetRendererOptions(Connector connector, string options)
        {
            connector.SetRendererOptionsForViewId(_panel.Handle, options);
        }

        public void SetRendererMirroring(Connector connector, bool mirroring)
        {
            // _isMirroring = mirroring;
            SetRendererOptions(connector, "{\"EnablePreviewMirroring\":" + (mirroring ? "true" : "false") + "}");
        }

        private void AssignViewToLocalCamera(Connector connector)
        {
            connector.AssignViewToLocalCamera(_panel.Handle, _videoSource.localCamera, false, true);
            SetRendererMirroring(connector, _videoSource.isMirroring);
            // connector.SetRendererOptionsForViewId(_panel.Handle, "{\"EnablePreviewMirroring\":false}");
            Log("AssignRender AssignViewToLocalCamera -> {0} , EnablePreviewMirroring => {1}",
                GetFingerPrint(),
                _videoSource.isMirroring);
        }

        private void AssignViewToVirtualVideoSource(Connector connector)
        {
            connector.AssignViewToVirtualVideoSource(_panel.Handle, _videoSource.virtualVideoSource, false, true);
            Log("AssignRender AssignViewToVirtualVideoSource " + GetFingerPrint());
        }

        private void AssignViewToRemoteCamera(Connector connector)
        {
            connector.AssignViewToRemoteCamera(_panel.Handle, _videoSource.remoteCamera, false, true);
            //test_code connector.SetRendererOptionsForViewId(_panel.Handle, "{\"EnablePreviewMirroring\":false}");
            Log("AssignRender AssignViewToRemoteCamera success.(" + GetFingerPrint() + "," +
                _videoSource.remoteCamera.GetName() + "," +
                _type + ")");
        }

        private void AssignViewToRemoteWindowShare(Connector connector)
        {
            connector.AssignViewToRemoteWindowShare(_panel.Handle, _videoSource.remoteWindowShare, false, true);
            Log("AssignRender AssignViewToRemoteWindowShare success.(" + GetFingerPrint() + "," +
                _videoSource.remoteWindowShare.GetName() + "," + _type + ")");
        }

        public void StopRendering(Connector connector)
        {
            if (inUsed && _isMaximize) _maximizeListener?.OnRequestRendererSizeChange(this, false); //取消最大化
            if (inUsed && _isPin) _pinListener?.OnRequestPin(this, false); //取消pin

            StopListenFrame(connector);

            _panel.Visible = false;
            inUsed.Set(false);

            connector.HideView(_panel.Handle);

            DismissTools();

            ClearVideoSource();
        }

        public bool StartRendering(Connector connector)
        {
            //-----
            if (IsNone())
            {
                StopRendering(connector);
                return true;
            }

            // if (IsInUse())
            // {
            //     Log("StartRendering ({0}) at {1},{2} failed,already rendering other video source.", _sourceKey, _screenIndex, _id);
            //     return false;
            // }

            if (null != _videoSource)
            {
                if (_videoSource.IsLocalCamera())
                {
                    AssignViewToLocalCamera(connector);
                }
                else if (_videoSource.IsLocalCamera2())
                {
                    AssignViewToLocalCamera(connector);
                }
                else if (_videoSource.IsVirtualSource())
                {
                    AssignViewToVirtualVideoSource(connector);
                }
                else if (_videoSource.IsRemoteCamera())
                {
                    AssignViewToRemoteCamera(connector);
                }
                else if (_videoSource.IsRemoteWindowShare())
                {
                    AssignViewToRemoteWindowShare(connector);
                }
                else
                {
                    Log("StartRendering Unknown.(" + _sourceKey + ")");
                    return false;
                }

                _panel.Visible = true;
                inUsed.Set(true);

                _videoSource.isRendering = true;

                StartListenFrame(connector);

                ShowTools();

                return true;
            }
            else
            {
                Log("StartRendering failed.videoSource is null");
                return false;
            }
        }

        public bool SwapStreams(Connector connector, VideoRenderer target)
        {
            if (target?._videoSource == null)
            {
                return false;
            }

            VideoSource vs1 = _videoSource;
            VideoSource vs2 = target._videoSource;

            vs1.Release();
            vs2.Release();

            TrySetVideoSource(vs2);
            target.TrySetVideoSource(vs1);

            ResetTools();
            target.ResetTools();

            StopListenFrame(connector);
            target.StopListenFrame(connector);

            StartListenFrame(connector);
            target.StartListenFrame(connector);

            bool swapResult = connector.SwapStreamsBetweenViews(_panel.Handle, target._panel.Handle);

            //set renderer mirror when video is local camera
            if (vs2.IsLocalCamera() || vs2.IsLocalCamera2())
            {
                this.SetRendererMirroring(connector, vs2.isMirroring);
            }

            if (vs1.IsLocalCamera() || vs1.IsLocalCamera2())
            {
                target.SetRendererMirroring(connector, vs1.isMirroring);
            }

            return swapResult;
        }

        public IAsyncResult RunOnUIThread(Delegate method)
        {
            return _panel?.BeginInvoke(method);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[VideoRenderer] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[VideoRenderer] " + content, args);
        }
    }

    public interface IRendererMaximizeListener
    {
        void OnRequestRendererSizeChange(VideoRenderer renderer, bool isMaximize);
    }

    public interface IRendererPinListener
    {
        void OnRequestPin(VideoRenderer renderer, bool isPin);
    }

    public interface IRendererExchangeListener
    {
        void OnExchangeClick(VideoRenderer renderer);
    }
}