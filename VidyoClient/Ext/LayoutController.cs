using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NLog;
using VideoClient.Util;
using VidyoClient;
using VidyoClient.Ext;

namespace VideoClient.VidyoClient.Ext
{
    public class LayoutController :
        Connector.IRegisterRemoteCameraEventListener,
        Connector.IRegisterRemoteWindowShareEventListener,
        IOnLoudestParticipantChangedListener, IOnParticipantsChangedListener
    {
        private readonly Connector _connector;
        private readonly Panel _videoPanel;
        public readonly Object _Lock = new Object();

        private PictureBox _minorStreamState;

        public readonly FormVideoExt[] _forms = new FormVideoExt[3];

        public Dictionary<int, VideoRendererContainer> _rendererContainers =
            new Dictionary<int, VideoRendererContainer>();

        public int _extFormNumber;

        public ConcurrentDictionary<string, VideoSource>
            _videoSources = new ConcurrentDictionary<string, VideoSource>();

        public readonly LayoutControllerConfig config;

        private ParticipantListener _participantManager;
        private VideoSocketService _socketService;

        private LayoutMode _curLayoutMode = LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER;

        private LayoutMode _savedLayoutMode = LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER;

        //对应UserID的下划线后半部分，前半部分为用户类型，只保留后部分Id即可。
        private string _curLecturerId;
        private string _savedLecturerId;
        private Participant _lastSpeaker;
        public bool isMainCameraPin;

        public LayoutController(
            Panel videoPanel,
            LayoutControllerConfig layoutControllerConfig,
            Connector connector,
            VideoSocketService socketService,
            ParticipantListener participantManager)
        {
            _connector = connector;
            _videoPanel = videoPanel;
            config = layoutControllerConfig;
            _participantManager = participantManager;
            _socketService = socketService;
        }

        public void Init()
        {
            //变量根据配置赋值
            isMainCameraPin = config.EndpointModeCameraPin;
            //初始化状态相关UI元素
            InitStatusUi();
            //添加第一显示设备和初始化渲染容器
            InitDefaultRendererContainer();
            //识别其他显示设备，并初始化渲染容器
            if (config.EnableEndpointMode)
            {
                InitExtForm();
                //设置多窗口时，主窗口为仅主讲人布局
                if (GetWindowNumber() >= 2)
                {
                    GetDefaultRendererContainer()._curLayoutMode = LayoutMode.MODE_LECTURE_ONLY_LECTURER;
                }
            }

            //初始化语音激励渲染位置
            SetVoiceActivated(
                config.VoiceActivatedPositionOfScreens,
                config.VoiceActivatedPositionOfRenderers);
            //使用默认布局或自定义布局
            if (IsUseDefaultLayout())
            {
                _connector.AssignViewToCompositeRenderer(_videoPanel.Handle,
                    Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                    (uint)config.RemoteParticipants);
                _connector.ShowViewAt(_videoPanel.Handle, 0, 0, (uint)_videoPanel.Width, (uint)_videoPanel.Height);
                // _connector.SetRendererOptionsForViewId(_videoPanel.Handle, "{\"EnablePreviewMirroring\":false}");
                Log("Init use default layout.");
            }
            else
            {
                _connector.HideView(_videoPanel.Handle);
                _connector.AssignViewToCompositeRenderer(IntPtr.Zero,
                    Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                    (uint)config.RemoteParticipants);
                ChangeCustomizedLayoutMode(_curLayoutMode, _curLecturerId);
                Log("Init use custom layout.");
            }
        }

        private void InitStatusUi()
        {
            _minorStreamState = new PictureBox();
            _minorStreamState.Size = new Size(32, 32);
            _minorStreamState.Location = new Point(80, 0);
            _minorStreamState.Load($"{GetIconDir()}\\sharing.png");
            _minorStreamState.BackColor = Color.Transparent;
            _minorStreamState.Visible = false;

            _videoPanel.Parent.Controls.Add(_minorStreamState);
            _videoPanel.Parent.Controls.SetChildIndex(_minorStreamState, 0);

            RefreshStatusUiLocation();
        }

        private void RefreshStatusUiLocation()
        {
            _minorStreamState.Location = new Point( //使辅流状态显示在视频容器右下角
                _videoPanel.Left + _videoPanel.Width - _minorStreamState.Width - 16,
                _videoPanel.Top + _videoPanel.Height - _minorStreamState.Height - 16);
        }

        public void SetStatusUiVisible(bool visible)
        {
            if (config.EnableEndpointMode)
            {
                RunOnUIThread((Action)(() => { _minorStreamState.Visible = visible; }));
            }
        }

        private string GetIconDir()
        {
            return $"{System.Windows.Forms.Application.StartupPath}\\wwwroot\\res\\icon";
        }

        private void InitDefaultRendererContainer()
        {
            VideoRendererContainer mainContainer =
                new VideoRendererContainer(
                    _connector,
                    _videoPanel,
                    0,
                    (int)LayoutGridMode.GridTwentyFive,
                    config.DisplaySelfViewPip,
                    config.DisplayZoomBtnOnLocalRenderer,
                    config.DisplayZoomBtnOnRemoteRenderer,
                    config.DisplayZoomBtnOnLecturerRenderer,
                    config.DisplaySnapshotBtn,
                    false)
                {
                    _displaySelfViewInMeeting = config.DisplaySelfViewInMeeting
                };
            mainContainer.SetForceLayoutNum(_connector, config.ForceLayoutNum[0]);
            _rendererContainers.Add(0, mainContainer);
        }

        //callback
        private void InitExtForm()
        {
            Log("---------------InitExtForm begin---------------");
            _extFormNumber = (config.EndpointModeWindowNumber < 1
                ? Screen.AllScreens.Length
                : config.EndpointModeWindowNumber) - 1;

            if (_extFormNumber > 4) _extFormNumber = 4; //目前最大支持4个扩展屏

            if (_extFormNumber < 1) return;

            Screen appScreen = Screen.FromControl(_videoPanel);
            Log("Launch Screen -> {0}({1}) , {2}x{3}",
                appScreen.DeviceName,
                appScreen.Primary,
                appScreen.Bounds.Width,
                appScreen.Bounds.Height);

            //分配窗口到各屏幕
            List<Screen> extScreens = new List<Screen>();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen screen = Screen.AllScreens[i];
                if (!String.Equals(screen.DeviceName, appScreen.DeviceName))
                {
                    extScreens.Add(screen);
                }
            }

            for (int i = 0; i < _extFormNumber; i++)
            {
                if (null == _forms[i])
                {
                    int screenIndex = i + 1;

                    Screen allocScreen; //给扩展窗口分配显示器

                    if (extScreens.Count == 0)
                    {
                        allocScreen = appScreen;
                    }
                    else if (i < extScreens.Count)
                    {
                        allocScreen = extScreens[i];
                        Log("Allocate screen {0} for ext form {1}", extScreens[i].DeviceName, i);
                    }
                    else
                    {
                        //如果指定窗口数超出屏幕数，则分配最后一个屏幕
                        allocScreen = extScreens[extScreens.Count - 1];
                        Log("Ext Screen Not Enough ,Allocate screen {0} for ext form {1}", appScreen.DeviceName, i);
                    }

                    _forms[i] = new FormVideoExt(
                        screenIndex,
                        allocScreen,
                        String.Equals("fullscreen", config.EndpointModeWindowMode));

                    VideoRendererContainer rendererContainer =
                        new VideoRendererContainer(
                            _connector,
                            _forms[i].panel1,
                            screenIndex,
                            (int)LayoutGridMode.GridTwentyFive,
                            false,
                            config.DisplayZoomBtnOnLocalRenderer,
                            config.DisplayZoomBtnOnRemoteRenderer,
                            config.DisplayZoomBtnOnLecturerRenderer,
                            config.DisplaySnapshotBtn,
                            false);

                    if (screenIndex < config.ForceLayoutNum.Count)
                    {
                        rendererContainer.SetForceLayoutNum(_connector, config.ForceLayoutNum[screenIndex]);
                    }

                    _rendererContainers.Add(screenIndex, rendererContainer);
                    _forms[i]._videoRendererContainer = rendererContainer;
                    _forms[i].Show();
                }
            }

            Log("---------------InitExtForm end---------------");
        }

        public void SaveScreenForceLayoutNum(int screenIndex, int num)
        {
            if (screenIndex < 4)
                Config.SetStringConfig("Screen" + (screenIndex + 1) + "ForceLayoutNum", num.ToString());
        }

        public void ShowExtWindowTips()
        {
            if (!config.EnableEndpointMode) return;
            foreach (FormVideoExt form2 in _forms)
            {
                form2?.SetScreenIndexDisplay(true);
            }
        }

        public void DismissExtWindowTips()
        {
            if (!config.EnableEndpointMode) return;
            foreach (FormVideoExt form2 in _forms)
            {
                form2?.SetScreenIndexDisplay(false);
            }
        }

        public int GetWindowNumber()
        {
            return _rendererContainers.Count;
        }

        public bool IsEndpointMultiWindow()
        {
            return config.EnableEndpointMode && GetWindowNumber() >= 2;
        }

        public void Restore()
        {
            StopRenderingAllOnMain();
            ClearVideoSource();
            _curLayoutMode = LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER;
            SetLecturerNull();
            _savedLecturerId = "";
            _lastSpeaker = null;
        }

        #region 布局

        public bool IsUseDefaultLayout()
        {
            return config.UseDefaultLayout;
        }

        public void UseDefaultLayout(bool use)
        {
            RunOnUIThread((Action)(() =>
            {
                config.UseDefaultLayout = use;

                if (IsUseDefaultLayout())
                {
                    _connector.AssignViewToCompositeRenderer(_videoPanel.Handle,
                        Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                        (uint)config.RemoteParticipants);
                    _connector.ShowViewAt(_videoPanel.Handle, 0, 0, (uint)_videoPanel.Width,
                        (uint)_videoPanel.Height);
                    Log("Use default layout.");
                }
                else
                {
                    _connector.HideView(_videoPanel.Handle);
                    _connector.AssignViewToCompositeRenderer(IntPtr.Zero,
                        Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                        (uint)config.RemoteParticipants);
                    Log("Use custom layout.");
                }
            }));
        }

        private LayoutMode ChangeCustomizedLayoutMode(LayoutMode layoutMode, string lecturerId)
        {
            Log("------------------------SetCustomLayoutMode start------------------------");
            Log("| layoutMode: {0}, lecturerId: {1}", layoutMode, lecturerId);

            if (IsUseDefaultLayout())
            {
                Log("| ChangeCustomizedLayoutMode failed. current layout way is default layout..");
                Log("------------------------SetCustomLayoutMode   End------------------------");
                return LayoutMode.Mode_UNKNOWN;
            }

            if (layoutMode == _curLayoutMode && layoutMode == LayoutMode.MODE_NORMAL)
            {
                Log("| SetCustomLayoutMode failed,because already in {0} layout mode.", _curLayoutMode);
                Log("------------------------SetCustomLayoutMode   End------------------------");
                SendLayoutStatusEvent(false);
                return LayoutMode.Mode_UNKNOWN;
            }

            _curLayoutMode = layoutMode;
            //根据不同的布局模式，给主讲人id赋值
            switch (layoutMode)
            {
                case LayoutMode.MODE_NORMAL:
                    SetLecturerNull();
                    break;
                case LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER:
                    if (_lastSpeaker != null)
                    {
                        SetLecturer(VideoParticipant.ParseId(_lastSpeaker));
                        Log("| Change layout mode to dynamic lecturer, set last speaker to lecturer -> {0}",
                            _curLecturerId);
                    }
                    else
                    {
                        SetLecturerNull();
                        Log("| Change layout mode to dynamic lecturer, have not last speaker,set lecturer to null");
                    }

                    break;
                case LayoutMode.MODE_LECTURE_FIXED_LECTURER:
                    SetLecturer(lecturerId);
                    break;
            }

            //
            StopRenderingAll();

            //单屏模式时，才需要变换布局模式
            if (GetWindowNumber() == 1)
            {
                Log("| SetLayoutMode for default container (Because window num is one).");
                GetDefaultRendererContainer().SetLayoutMode(_connector, layoutMode);
            }

            //区分最后讲话者主画面和其他画面
            Dictionary<string, VideoSource> sources = new Dictionary<string, VideoSource>();
            VideoSource speakerCameraSource = null;
            bool isSpeakerHasMinorStream = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsRemoteCamera() &&
                    String.Equals(_lastSpeaker?.GetUserId(), pair.Value.@from?.GetUserId()))
                {
                    speakerCameraSource = pair.Value;
                }
                else
                {
                    sources[pair.Key] = pair.Value;
                    if (String.Equals(_lastSpeaker?.GetUserId(), pair.Value.@from?.GetUserId()))
                    {
                        isSpeakerHasMinorStream = true;
                    }
                }
            }

            Log("| The Last Speaker has camera : {0} , has minor stream : {1}",
                null != speakerCameraSource,
                isSpeakerHasMinorStream);

            //渲染最后讲话人画面
            if (null != speakerCameraSource)
            {
                if (
                    // GetWindowNumber() == 1 &&
                    layoutMode == LayoutMode.MODE_LECTURE_FIXED_LECTURER &&
                    isSpeakerHasMinorStream
                )
                {
                    Log("| Assign Last Speaker camera to lecturer_view.");
                    StartRenderingForLecturer(speakerCameraSource);
                }
                else
                {
                    VideoRenderer voiceActivatedRenderer = FindRendererForVoiceActivated();
                    Log("| Assign Last Speaker camera to voice_view({0})",
                        null != voiceActivatedRenderer ? voiceActivatedRenderer.GetFingerPrint() : "");
                    StartRendering(voiceActivatedRenderer, speakerCameraSource);
                }
            }

            //找出是否包含其他人辅流（便于判断是否要固定自己画面）
            bool isOtherHasMinorStream = false;
            foreach (KeyValuePair<string, VideoSource> pair in sources)
            {
                if (pair.Value.IsRemoteWindowShare())
                {
                    isOtherHasMinorStream = true;
                }
            }

            //渲染主讲以外其他画面
            foreach (KeyValuePair<string, VideoSource> pair in sources)
            {
                //渲染主讲人其他辅流
                if (
                    IsLectureLayoutMode() &&
                    IsFromLecturer(pair.Value))
                {
                    StartRenderingForLecturer(pair.Value);
                }
                //渲染主摄
                else if (pair.Value.IsLocalCamera())
                {
                    //主摄到固定位，如果开启的话
                    if (
                        isMainCameraPin &&
                        IsLectureLayoutMode() &&
                        !isSpeakerHasMinorStream &&
                        !isOtherHasMinorStream
                    )
                    {
                        Log(
                            "| Assign LocalCamera To Lecturer Force (enabled pin main camera & other minor stream is null)");
                        AssignLocalCameraToLecturerForce();
                    }
                    //pip
                    else if (config.DisplaySelfViewPip && _curLayoutMode == LayoutMode.MODE_NORMAL)
                    {
                        Log("| Assign LocalCamera To Pip (enabled DisplaySelfViewPip)");
                        StartRendering(GetDefaultRendererContainer().GetSelfViewPipRenderer(), pair.Value);
                    }
                    //
                    else if (IsLectureLayoutMode() && IsLecturerNull())
                    {
                        Log("| Assign LocalCamera To LecturerForce (is lecture layout mode & lecturer is null)");
                        AssignLocalCameraToLecturerForce();
                    }
                    else
                    {
                        Log("| Assign LocalCamera To Auto");
                        StartRendering(pair.Value.GetSourceKey());
                    }
                }
                //渲染其他源
                else
                {
                    StartRendering(pair.Value.GetSourceKey());
                }
            }

            SendLayoutStatusEvent(true);

            Log("------------------------SetCustomLayoutMode   End------------------------");
            return layoutMode;
        }

        private IAsyncResult ChangeCustomizedLayoutModeOnMain(LayoutMode layoutMode, string lecturerId)
        {
            return RunOnUIThread((Action)(() => { ChangeCustomizedLayoutMode(layoutMode, lecturerId); }));
        }

        public void ChangeCustomizedLayoutModeOnMainWait(LayoutMode layoutMode, string lecturerId)
        {
            IAsyncResult asyncResult = ChangeCustomizedLayoutModeOnMain(layoutMode, lecturerId);
            while (!asyncResult.IsCompleted)
            {
            }
        }

        public void SaveLayoutMode(LayoutMode layoutMode)
        {
            _savedLayoutMode = layoutMode;
            Log("SaveLayoutMode {}", layoutMode);
        }

        public void SaveLecturerId(string lecturerId)
        {
            _savedLecturerId = lecturerId;
            Log("SaveLecturerId {}", lecturerId);
        }

        private bool IsAutoAssignRenderer()
        {
            return _curLayoutMode == LayoutMode.MODE_LECTURE_FIXED_LECTURER || config.AutoAssignRenderer;
        }

        public void SetAutoAssignRenderer(bool auto)
        {
            config.AutoAssignRenderer = auto;
            Config.SetStringConfig("AutoAssignRenderer", auto ? "true" : "false");
            Log("SetAutoAssignRenderer(auto:{0})", auto);

            bool isNeedRestoreAutoLayout = auto && GetWindowNumber() == 1;
            if (isNeedRestoreAutoLayout)
            {
                SaveScreenForceLayoutNum(0, 0);
                SetRendererContainerForceLayoutNum(0, 0);
            }
            else
            {
                lock (_Lock)
                {
                    //重新布局
                    IAsyncResult asyncResult = RunOnUIThread((Action)(() => { TryStartRenderingFreeVideoSources(); }));
                    while (!asyncResult.IsCompleted)
                    {
                    }
                }
            }
        }

        bool IsLectureLayoutMode()
        {
            return _curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER ||
                   _curLayoutMode == LayoutMode.MODE_LECTURE_FIXED_LECTURER;
        }

        //interface
        //显示屏幕编号
        public void SetExtFormIndexDisplay(bool display)
        {
            for (int i = 0; i < _extFormNumber; i++)
            {
                FormVideoExt temp = _forms[i];
                temp?.SetScreenIndexDisplay(display);
            }
        }

        private void SendLayoutStatusEvent(bool isLayoutExecuted)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"curLayoutMode\":").Append((int)_curLayoutMode).Append(",");
            sb.Append("\"remoteWindowShareCount\":").Append(GetVideoSourceCount(VideoSourceType.TYPE_REMOTE_SHARE))
                .Append(",");
            sb.Append("\"isLayoutExecuted\":").Append(isLayoutExecuted ? "true" : "false");
            sb.Append("}");
            String json = "{\"code\":" + 0 + ",\"data\":" + sb + "}";
            _socketService.SendLayoutStatusEventToAll(json);
        }

        #endregion

        #region 控制渲染器

        private bool StopRenderingAndLayOut(VideoRenderer r)
        {
            if (IsUseDefaultLayout())
            {
                Log("> StopRenderingAndLayOut failed. current layout way is default layout..");
                return false;
            }

            if (r?._panel == null)
            {
                Log("> StopRenderingAndLayOut failed. video render panel is null.");
                return false;
            }

            try
            {
                r.StopRendering(_connector);
            }
            catch (Exception e)
            {
                Log("> StopRenderingAndLayOut exception,{0} -> {1}", r.GetFingerPrint(), e.ToString());
                throw;
            }

            VideoRendererContainer container = _rendererContainers[r.containerId];
            if (null != container)
            {
                container.RefreshLayout();
                _socketService.SendVideoSourcesUpdatedEventToAll();
                return true;
            }
            else
            {
                Log("> StopRenderingAndLayOut failed. refresh layout failed.");
                return false;
            }
        }

        private bool StartRenderingAndLayOut(VideoRenderer r)
        {
            if (IsUseDefaultLayout())
            {
                Log("> StartRenderingAndLayOut failed. current layout way is default layout..");
                return false;
            }

            if (r?._panel == null)
            {
                Log("> StartRenderingAndLayOut failed. video render panel is null.");
                return false;
            }

            bool execCompleted = false;
            try
            {
                execCompleted = r.StartRendering(_connector);
            }
            catch (Exception e)
            {
                Log("> StartRenderingAndLayOut exception,{0} -> {1}", r.GetFingerPrint(), e.ToString());
                throw;
            }

            VideoRendererContainer container = _rendererContainers[r.containerId];
            if (null != container)
            {
                container.RefreshLayout();
                _socketService.SendVideoSourcesUpdatedEventToAll();
                return true;
            }
            else
            {
                Log("> StartRenderingAndLayOut failed. refresh layout failed.");
                return false;
            }
        }

        private bool SwapStreamsBetweenRenderers(VideoRenderer r1, VideoRenderer r2)
        {
            if (IsUseDefaultLayout())
            {
                Log("Assign Renderer failed. current layout way is default layout..");
                return false;
            }

            if (r1?._panel == null || r2?._panel == null)
            {
                Log("SwapStreamsBetweenRenderers failed. video render panel is null.");
                return false;
            }

            if (r1.GetFingerPrint().Equals(r2.GetFingerPrint()))
            {
                Log("SwapStreamsBetweenRenderers failed (r1:{0} ,r2: {1}). r1 and r2 is the same one.",
                    r1.GetFingerPrint(), r2.GetFingerPrint());
                return false;
            }

            try
            {
                r1.SwapStreams(_connector, r2);
            }
            catch (Exception e)
            {
                Log("{0} swap to {1} exception, -> {2}", r1.GetFingerPrint(), r2.GetFingerPrint(), e.ToString());
                throw;
            }

            _socketService.SendVideoSourcesUpdatedEventToAll();

            Log("SwapStreamsBetweenRenderers {0} swap with {1} completed.", r1.GetFingerPrint(), r2.GetFingerPrint());

            return true;
        }

        private void StartRenderingForLecturer(VideoSource videoSource)
        {
            VideoRenderer renderer = GetDefaultRendererContainer().FindFreeLecturerRenderer();
            if (null != renderer)
            {
                StartRendering(renderer, videoSource);
            }
            else
            {
                Log("Start RenderingForLecturer failed,there is not have lecturer_renderer enough.");
            }
        }

        private void StartRendering(VideoRenderer r, VideoSource videoSource)
        {
            if (r == null)
            {
                Log("> Start Rendering start failed. renderer is null.");
                return;
            }

            if (videoSource == null)
            {
                Log("> Start Rendering start failed. videoSource is null");
                return;
            }

            Log("↓");
            Log("> Start Rendering start. ({0}_{1} -> {2}、 {3})",
                videoSource.type,
                videoSource.label,
                r.GetFingerPrint(),
                videoSource.key);

            bool usable = r.TrySetVideoSource(videoSource);
            if (usable)
            {
                StartRenderingAndLayOut(r);
                Log("> Start Rendering ok. ({0}_{1} -> {2}、 {3})",
                    videoSource.type,
                    videoSource.label,
                    r.GetFingerPrint(),
                    videoSource.key);
                Log("↑");
            }
            else
            {
                r.Release(); //查找空闲渲染器时，已经标记渲染器被使用，未渲染成功时需要释放渲染器
                Log("> Start Rendering failed,VideoSource already in used. (VideoSource:{0}_{1}、{2})",
                    videoSource.key,
                    videoSource.label,
                    r.GetFingerPrint());
            }
        }

        private bool StartRendering(string sourceKey)
        {
            return StartRendering(sourceKey, -1);
        }

        private bool StartRendering(string sourceKey, int screenIndex)
        {
            VideoSource videoSource = FindVideoSource(sourceKey);

            if (config.AutoAssignRenderer &&
                IsLectureLayoutMode() &&
                IsFromLecturer(videoSource)
               )
            {
                StartRenderingForLecturer(videoSource);
                return true;
            }

            VideoRenderer renderer = FindFreeRenderer(screenIndex);

            //多窗口手动模式时，允许分配到主屏
            if (GetWindowNumber() >= 2 &&
                !config.AutoAssignRenderer &&
                screenIndex == 0 &&
                null == renderer)
            {
                renderer = GetDefaultRendererContainer().FindFreeLecturerRenderer();
            }

            if (null != renderer)
            {
                StartRendering(renderer, videoSource);
                return true;
            }

            _socketService.SendVideoSourcesUpdatedEventToAll();
            Log("> Start Rendering failed. current screen have renderer not enough.");
            return false;
        }

        private bool StartRendering(string sourceKey, int screenIndex, int rendererIndex)
        {
            VideoSource videoSource = FindVideoSource(sourceKey);

            Log("> Start Rendering " + sourceKey + "," + screenIndex + " , " + rendererIndex);

            if (!_rendererContainers.ContainsKey(screenIndex))
            {
                Log("> Start Rendering failed,screeIndex not exist.");
                return false;
            }

            VideoRendererContainer container = _rendererContainers[screenIndex];
            if (rendererIndex >= container._renderers.Count)
            {
                Log("> Start Rendering failed,rendererIndex not exist.");
                return false;
            }

            VideoRenderer renderer = container._renderers[rendererIndex];

            if (null != renderer)
            {
                if (renderer.IsInUse())
                {
                    renderer.StopRendering(_connector);
                }

                StartRendering(renderer, videoSource);
                return true;
            }
            else
            {
                Log("> Start Rendering failed. renderer is null.");
                return false;
            }
        }


        private void StopRendering(VideoRenderer renderer)
        {
            string sourceKey = renderer._sourceKey;
            string label = renderer._videoSource?.label;
            Log("> Stop Rendering start. ({0}_{1}、{2})", sourceKey, label, renderer.GetFingerPrint());

            StopRenderingAndLayOut(renderer);

            Log("> Stop Rendering ok. ({0}_{1})、{2}", sourceKey, label, renderer.GetFingerPrint());
        }

        private void StopRendering(string sourceKey)
        {
            VideoRenderer assignedRenderer = FindRenderer(sourceKey);
            if (null != assignedRenderer)
            {
                StopRendering(assignedRenderer);
            }
        }


        private void StopRenderingAll()
        {
            if (IsUseDefaultLayout())
            {
                Log("Stop RenderingAll failed. current layout way is default layout..");
                return;
            }

            Log("> Stop RenderingAll");
            foreach (KeyValuePair<int, VideoRendererContainer> pair in _rendererContainers)
            {
                pair.Value.StopRenderingAll(_connector);
            }

            _socketService.SendVideoSourcesUpdatedEventToAll();
        }

        public void StartRenderingLocalCamera(LocalCamera camera, CameraConfig cameraConfig)
        {
            lock (_Lock)
            {
                IAsyncResult asyncResult = StartRenderingLocalCamera(
                    camera,
                    cameraConfig.previewLabel,
                    cameraConfig.cameraIndex == 0,
                    cameraConfig.positionOfScreen,
                    cameraConfig.positionOfRenderer,
                    (int)cameraConfig.width,
                    (int)cameraConfig.height,
                    (int)cameraConfig.framerate,
                    cameraConfig.isMirroring);
                // while (!asyncResult.IsCompleted) // 导致问题
                // {
                // }
            }
        }

        private IAsyncResult StartRenderingLocalCamera(
            LocalCamera camera,
            string cameraName,
            bool isMainCamera,
            int positionOfScreen,
            int positionOfRendererContainer,
            int width,
            int height,
            int frameInterval,
            bool isMirroring)
        {
            return RunOnUIThread((Action)(() =>
            {
                if (IsUseDefaultLayout())
                {
                    Log("Assign LocalCamera failed. current layout way is default layout..");
                    return;
                }

                if (null == camera)
                {
                    Log("Assign LocalCamera failed. camera is null");
                    return;
                }

                VideoSource videoSource = FindVideoSource(VideoSource.GetSourceKey(camera, isMainCamera));

                if (null == videoSource)
                {
                    videoSource =
                        new VideoSource(
                            camera,
                            cameraName,
                            isMainCamera,
                            positionOfScreen,
                            positionOfRendererContainer)
                        {
                            width = width, height = height, frameInterval = frameInterval, isMirroring = isMirroring,
                            from = _participantManager._virtualParticipantMe
                        };
                    AddVideoSource(videoSource);
                    _participantManager.AddVideoSource(videoSource);
                }

                //停止渲染摄像头如果已被渲染（当启用允许复制渲染时，则不需要停止）
                if (!config.AllowCameraCopyRendering) //todo 目前sourceKey编码方式导致此控制未实现，第一路第二路仍可同时渲染
                {
                    VideoRenderer oldRenderer = FindRenderer(videoSource.GetSourceKey());
                    if (null != oldRenderer && oldRenderer.IsInUse())
                    {
                        oldRenderer.StopRendering(_connector);
                    }
                }

                //为摄像头分配渲染器
                VideoRendererContainer container = _rendererContainers[positionOfScreen];
                VideoRenderer selfViewRenderer =
                    FindLocalCameraRenderer(isMainCamera, container, positionOfRendererContainer);

                //停止目标渲染器，并记录目标渲染器视频源，以分配空闲位置供其渲染
                string employSourceKey = null;
                if (selfViewRenderer.IsInUse())
                {
                    employSourceKey = selfViewRenderer._sourceKey;
                    selfViewRenderer.StopRendering(_connector);
                }

                if (IsAutoAssignRenderer())
                {
                    //渲染第二路时，当单屏有他人共享时且处于网格布局，则需要变换布局模式
                    if (!isMainCamera && (_curLayoutMode == LayoutMode.MODE_NORMAL ||
                                          _curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER))
                    {
                        ChangeCustomizedLayoutMode(LayoutMode.MODE_LECTURE_FIXED_LECTURER,
                            VideoParticipant.ParseId(_participantManager._virtualParticipantMe.participant));
                    }
                    else
                    {
                        //渲染本地摄像头画面
                        StartRendering(selfViewRenderer, videoSource);
                        //给被占用的源找新的空闲位置渲染
                        if (!String.IsNullOrEmpty(employSourceKey) &&
                            !String.Equals(VideoSource.SOURCE_KEY_NO_DISPLAY, employSourceKey))
                        {
                            StartRendering(employSourceKey);
                        }
                    }
                }
                else
                {
                    Log(
                        $"Because AutoAssignRenderer is false,so just generate {(isMainCamera ? "camera" : "content")} video source and not rendering");
                    return;
                }

                Log("Assign LocalCamera success.");
            }));
        }

        public bool SetMainCameraPin(bool pin)
        {
            lock (_Lock)
            {
                isMainCameraPin = pin;

                //显示到主屏,当没有固定主讲人时
                if (_curLayoutMode != LayoutMode.MODE_LECTURE_FIXED_LECTURER)
                {
                    if (isMainCameraPin)
                    {
                        //执行固定
                        // RunOnUIThread((Action)(AssignLocalCameraToLecturerForce));
                        AssignLocalCameraToLecturerForce();
                    }
                    else
                    {
                        //取消固定时，需要将上个讲话人显示在主屏
                        if (null != _lastSpeaker)
                        {
                            //当没有主讲人辅流时，挪动主摄到其他空闲位，主位用来显示语音激励者
                            // if(lecturerviews.contains )
                            VideoRenderer renderer = FindMajorRendererByParticipant(_lastSpeaker);
                            if (null != renderer)
                            {
                                //todo 取消固定时，需要将上个讲话人显示在主屏
                            }
                        }
                    }
                }

                Config.SetStringConfig("endpointModeCameraPin", isMainCameraPin ? "true" : "false");
            }

            return isMainCameraPin;
        }

        private void AssignLocalCameraToLecturerForce()
        {
            Log("Assign local camera to lecturer_view force");

            VideoSource cameraVideoSource = FindLocalCameraVideoSource();
            if (null != cameraVideoSource)
            {
                VideoRenderer origin = FindRenderer(cameraVideoSource.GetSourceKey());
                VideoRenderer
                    dest = GetDefaultRendererContainer()._lecturerRenderers[0];
                if (dest.IsInUse())
                {
                    if (null != origin)
                    {
                        Log("Assign local camera to lecturer_view by swap (lecturer_view is busy).");
                        SwapStreamsBetweenRenderers(origin, dest);
                    }
                    else
                    {
                        //让出主位给主摄
                        Log(
                            "Assign local camera to lecturer_view by force (lecturer_view is busy & local camera free).");
                        string employSourceKey = dest._sourceKey;
                        dest.StopRendering(_connector);
                        StartRendering(dest, cameraVideoSource);
                        StartRendering(employSourceKey);
                    }
                    //todo 优化：还需要把主讲人显示到第二屏第一位置
                }
                else
                {
                    Log("Assign local camera to lecturer_view directly (lecturer_view is free).");
                    if (null != origin) StopRendering(origin);
                    StartRendering(dest, cameraVideoSource);
                }
            }
            else
            {
                Log("Assign LocalCamera To Lecturer Force failed, camera source is null...");
            }
        }

        /// <summary>
        /// 为本地摄像头分配渲染器
        /// </summary>
        /// <param name="isMainCamera"></param>
        /// <param name="container">默认容器</param>
        /// <param name="defaultRendererId">默认渲染器Id</param>
        /// <returns></returns>
        private VideoRenderer FindLocalCameraRenderer(
            bool isMainCamera,
            VideoRendererContainer container,
            int defaultRendererId)
        {
            Log("FindLocalCameraRenderer (isMain:{0}) begin -> defaultRendererId : {1}",
                isMainCamera,
                defaultRendererId);

            //当开启PIP时
            if (config.DisplaySelfViewPip && isMainCamera && container._curLayoutMode == LayoutMode.MODE_NORMAL)
            {
                return container.GetSelfViewPipRenderer();
            }

            //当自己为主讲人时
            if (IsLecturerMe())
            {
                Log("FindLocalCameraRenderer process -> For now,I'm the lecturer.");
                return container.FindFreeLecturerRenderer();
            }

            //当屏幕数大于1时
            if (GetWindowNumber() >= 2)
            {
                //当开启主摄Pin且无固定主讲人时，占用主讲人位置
                if (isMainCamera && isMainCameraPin && _curLayoutMode != LayoutMode.MODE_LECTURE_FIXED_LECTURER)
                {
                    Log(
                        "FindLocalCameraRenderer result -> use lecturer renderer(multiple window & pin main camera & not fixed lecturer)");
                    return container._lecturerRenderers[0];
                }

                //会里没人时，分配到主讲人屏幕 （因开启了ReportLocalOnJoined，参会人空以1做条件判断）
                if (_participantManager._participants.Count <= 1)
                {
                    Log("FindLocalCameraRenderer process -> multiple window & just me alone");
                    return container.FindFreeLecturerRenderer();
                }

                //有人则分配到第二屏
                Log(
                    "FindLocalCameraRenderer result -> use renderer of ext window(multiple window & not pin main camera");
                return _rendererContainers[1]._renderers[defaultRendererId];
            }

            //当单屏模式，剧场模式的主讲人位置空闲，则显示本地画面
            if (
                _curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER &&
                container.IsAllLecturerRendererIdle()
            )
            {
                Log("FindLocalCameraRenderer result -> use lecturer renderer (single window & lecturer is nobody");
                return container._lecturerRenderers[0];
            }

            //默认情况
            Log("FindLocalCameraRenderer result -> use default renderer");
            return container._renderers[defaultRendererId];
        }

        public void StopRenderingLocalCamera(LocalCamera localCamera, bool isMainCamera, CameraConfig cameraConfig)
        {
            string sourceKey = VideoSource.GetSourceKey(localCamera, isMainCamera);
            VideoRenderer renderer = FindRenderer(sourceKey);
            _participantManager.RemoveMyVideoSource(sourceKey);
            RemoveVideoSource(sourceKey);

            if (null != renderer)
            {
                lock (_Lock)
                {
                    IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                    {
                        StopRendering(renderer);

                        //源于主讲人且主讲人无辅流后，则需要变换布局到用户设定的布局，固定主摄时例外
                        if (!isMainCamera &&
                            IsLecturerMe() &&
                            !IsHasVirtualSource())
                        {
                            ChangeCustomizedLayoutMode(_savedLayoutMode, _savedLecturerId);
                        }
                    }));
                    // while (!asyncResult.IsCompleted)
                    // {
                    // }
                }
            }
        }

        public void StartRenderingLocalVirtualSource(
            VirtualVideoSource virtualVideoSource,
            string deviceName,
            int positionOfScreen,
            int positionOfRendererContainer)
        {
            lock (_Lock)
            {
                IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                {
                    if (IsUseDefaultLayout())
                    {
                        Log("Assign LocalVirtualSource failed. current layout way is default layout..");
                        return;
                    }

                    if (null == virtualVideoSource)
                    {
                        Log("Assign LocalVirtualSource failed. virtualVideoSource is null");
                        return;
                    }

                    VideoSource videoSource =
                        new VideoSource(virtualVideoSource, deviceName, positionOfScreen, positionOfRendererContainer)
                        {
                            from = _participantManager._virtualParticipantMe
                        };
                    AddVideoSource(videoSource);
                    _participantManager.AddVideoSource(videoSource);

                    //停止渲染摄像头
                    VideoRenderer oldRenderer = FindRenderer(videoSource.GetSourceKey());
                    if (null != oldRenderer && oldRenderer.IsInUse())
                    {
                        oldRenderer.StopRendering(_connector);
                    }

                    //停止渲染新位置视频源
                    // VideoRendererContainer container = _rendererContainers[positionOfScreen];
                    // VideoRenderer newRenderer = container._renderers[positionOfRendererContainer];
                    // if (newRenderer.IsInUse())
                    // {
                    //     newRenderer.StopRendering(_connector);
                    // }

                    VideoRenderer newRenderer = IsLecturerMe()
                        ? GetDefaultRendererContainer().FindFreeLecturerRenderer()
                        : FindFreeRenderer();

                    if (IsAutoAssignRenderer())
                    {
                        //当单屏下有人发出共享时且如果处于网格布局，则需要变换布局模式
                        if ((_curLayoutMode == LayoutMode.MODE_NORMAL ||
                             _curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER))
                        {
                            ChangeCustomizedLayoutMode(LayoutMode.MODE_LECTURE_FIXED_LECTURER,
                                VideoParticipant.ParseId(_participantManager._virtualParticipantMe.participant));
                        }
                        else
                        {
                            StartRendering(newRenderer, videoSource);
                        }
                    }
                    else
                    {
                        //释放未被使用的渲染器
                        if (null != newRenderer && newRenderer.IsInUse())
                        {
                            newRenderer.StopRendering(_connector);
                        }

                        Log(
                            "because AutoAssignRenderer is false,so just generate virtual video source and not rendering");
                    }

                    Log("Assign LocalVirtualSource success.");
                }));
                // while (!asyncResult.IsCompleted)
                // {
                // }
            }
        }

        public void StopRenderingLocalVirtualSource(VirtualVideoSource virtualVideoSource)
        {
            string sourceKey = VideoSource.GetSourceKey(virtualVideoSource);
            VideoRenderer renderer = FindRenderer(sourceKey);
            _participantManager.RemoveMyVideoSource(sourceKey);
            RemoveVideoSource(sourceKey);

            if (null != renderer)
            {
                lock (_Lock)
                {
                    IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                    {
                        StopRendering(renderer);

                        //当单屏下，源于主讲人且主讲人无辅流后，则需要变换布局到用户设定的布局
                        if (
                            IsLecturerMe() &&
                            !IsHasLocalCamera2()
                        )
                        {
                            ChangeCustomizedLayoutMode(_savedLayoutMode, _savedLecturerId);
                        }
                    }));
                    // while (!asyncResult.IsCompleted)
                    // {
                    // }
                }
            }
        }

        private bool TryStartRenderingFreeVideoSources()
        {
            bool isExistFree = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (!pair.Value.IsInUse())
                {
                    //渲染主屏固定
                    if (isMainCameraPin && pair.Value.IsLocalCamera())
                    {
                        AssignLocalCameraToLecturerForce();
                    }
                    else
                    {
                        StartRendering(pair.Value.GetSourceKey());
                    }

                    isExistFree = true;
                }
            }

            Log("Try Start RenderingFreeVideoSources isExistFree -> {0}", isExistFree);

            return isExistFree;
        }

        private bool TryMoveForwardRenderer()
        {
            //todo 前移算法

            return false;
        }

        #endregion

        #region 渲染器主线程调用

        public IAsyncResult StartRenderingOnMain(string sourceKey)
        {
            IAsyncResult result = RunOnUIThread((Action)(() => { StartRendering(sourceKey); }));
            return result;
        }

        public IAsyncResult StartRenderingOnMain(string sourceKey, int screenIndex)
        {
            IAsyncResult result = RunOnUIThread((Action)(() => { StartRendering(sourceKey, screenIndex); }));
            return result;
        }

        public IAsyncResult StartRenderingOnMain(string sourceKey, int screenIndex, int rendererIndex)
        {
            IAsyncResult result = RunOnUIThread((Action)(() =>
            {
                StartRendering(sourceKey, screenIndex, rendererIndex);
            }));
            return result;
        }

        public IAsyncResult StopRenderingOnMain(string sourceKey)
        {
            IAsyncResult result = RunOnUIThread((Action)(() => { StopRendering(sourceKey); }));
            return result;
        }

        public void StopRenderingAllOnMain()
        {
            RunOnUIThread((Action)(StopRenderingAll));
        }

        public bool SwapStreamsBetweenRenderersOnMain(VideoRenderer r1, VideoRenderer r2)
        {
            IAsyncResult result = RunOnUIThread((Action)(() => { SwapStreamsBetweenRenderers(r1, r2); }));
            return true;
        }

        #endregion

        #region 配置渲染器

        public VideoRendererContainer GetDefaultRendererContainer()
        {
            return _rendererContainers[0];
        }

        public void SetRendererContainerSize(int x, int y, int width, int height)
        {
            _videoPanel.Margin = new Padding(0, 0, 0, 0);
            _videoPanel.Location = new Point(x, y);
            _videoPanel.Size = new Size(width, height);
            // _videoPanel.BringToFront();

            if (!IsUseDefaultLayout())
            {
                for (int i = 0; i < _rendererContainers.Count; i++)
                {
                    VideoRendererContainer container = _rendererContainers[i];
                    container.RefreshLayout();
                }
            }
            else
            {
                _connector.ShowViewAt(_videoPanel.Handle,
                    0,
                    0,
                    (uint)_videoPanel.Width,
                    (uint)_videoPanel.Height);
            }
        }

        public bool SetRendererContainerForceLayoutNum(int containerIndex, int num)
        {
            if (containerIndex >= _rendererContainers.Count)
            {
                Log("SetRendererContainerForceRenderingNum failed. containerIndex already bigger than count.");
                return false;
            }

            VideoRendererContainer container = _rendererContainers[containerIndex];

            lock (_Lock)
            {
                if (num > container.GetAvailableRendererNumber())
                {
                    Log("SetRendererContainerForceRenderingNum failed. num already bigger than max.");
                    return false;
                }

                IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                {
                    container.SetForceLayoutNum(_connector, num);
                    _socketService.SendVideoSourcesUpdatedEventToAll();
                    if (IsAutoAssignRenderer()) TryStartRenderingFreeVideoSources();
                }));
                while (!asyncResult.IsCompleted)
                {
                }
            }

            Log("SetRendererContainerForceRenderingNum success.");

            return true;
        }

        public int GetRendererContainerForceLayoutNum(int containerIndex)
        {
            if (containerIndex >= _rendererContainers.Count)
            {
                return -1;
            }

            return _rendererContainers[containerIndex].curFixedLayoutNumber;
        }

        public int GetRendererContainerMaxLayoutNum(int containerIndex)
        {
            if (containerIndex >= _rendererContainers.Count)
            {
                return 0;
            }

            return _rendererContainers[containerIndex].GetAvailableRendererNumber();
        }

        public void SetVoiceActivatedDisabled()
        {
            SetVoiceActivated(-1, -1);
        }

        public void SetVoiceActivated(int positionOfScreens, int positionOfRenderers)
        {
            if (positionOfScreens < 0 || positionOfScreens >= _rendererContainers.Count)
            {
                Log("SetVoiceActivated failed,VoiceActivatedPositionOfScreens overflow");
                return;
            }

            config.VoiceActivatedPositionOfScreens = positionOfScreens;
            config.VoiceActivatedPositionOfRenderers = positionOfRenderers;

            VideoRendererContainer container = _rendererContainers[positionOfScreens];
            VideoRenderer renderer = container.FindRendererById(positionOfRenderers);
            if (null == renderer)
            {
                Log("SetVoiceActivated failed,VoiceActivatedPositionOfRenderers not found");
                return;
            }

            renderer._isVoiceActivated = true;
        }

        public bool IsVoiceActivatedEnabled()
        {
            return config.VoiceActivatedPositionOfScreens >= 0 &&
                   config.VoiceActivatedPositionOfRenderers >= 0;
        }

        public bool IsVoiceActivatedRenderer(VideoRenderer renderer)
        {
            if (null != renderer)
            {
                if (renderer.containerId == config.VoiceActivatedPositionOfScreens &&
                    renderer.id == config.VoiceActivatedPositionOfRenderers)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 查找渲染器

        // private VideoRenderer FindRenderer(int screenIndex, int id)
        // {
        //     if (screenIndex >= _rendererContainers.Count)
        //     {
        //         return null;
        //     }
        //
        //     VideoRendererContainer container = _rendererContainers[screenIndex];
        //     return container.FindRendererById(id);
        // }

        private VideoRenderer FindRenderer(string sourceKey)
        {
            foreach (KeyValuePair<int, VideoRendererContainer> pair in _rendererContainers)
            {
                VideoRenderer renderer = pair.Value?.FindRenderer(sourceKey);
                if (null != renderer)
                {
                    return renderer;
                }
            }

            return null;
        }

        private VideoRenderer FindMajorRendererByParticipant(Participant participant)
        {
            foreach (KeyValuePair<int, VideoRendererContainer> pair in _rendererContainers)
            {
                VideoRenderer renderer = pair.Value?.FindMajorRendererByParticipant(participant);
                if (null != renderer)
                {
                    return renderer;
                }
            }

            return null;
        }

        private VideoRenderer FindRendererForVoiceActivated()
        {
            //多屏
            if (GetWindowNumber() >= 2)
            {
                //有固定主讲人时，用第二屏第一渲染器作为语音激励
                if (_curLayoutMode == LayoutMode.MODE_LECTURE_FIXED_LECTURER)
                {
                    return _rendererContainers[1]._renderers[0];
                }

                //非固定主讲人，且固定本地主摄时，语音激励调至第二屏
                if (isMainCameraPin)
                {
                    return _rendererContainers[1]._renderers[0];
                }

                return GetDefaultRendererContainer()._lecturerRenderers[0];

                // switch (_curLayoutMode)
                // {
                //     case LayoutMode.MODE_LECTURE_FIXED_LECTURER:
                //         return _rendererContainers[1]._renderers[0]; //选用第二屏第一渲染器作为语音激励
                //     default:
                //         return GetDefaultRendererContainer()._lecturerRenderers[0];
                // }
            }

            //单屏
            switch (_curLayoutMode)
            {
                case LayoutMode.MODE_NORMAL:
                    return GetDefaultRendererContainer()._renderers[0];
                case LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER:
                    if (isMainCameraPin)
                    {
                        return GetDefaultRendererContainer()._renderers[0];
                    }

                    return GetDefaultRendererContainer()._lecturerRenderers[0];
                case LayoutMode.MODE_LECTURE_FIXED_LECTURER:
                    return GetDefaultRendererContainer()._renderers[0];
                default:
                    return GetDefaultRendererContainer()._renderers[0];
            }
        }

        private VideoRenderer FindFreeRenderer(int screenIndex = -1)
        {
            VideoRendererContainer container = null;
            if (screenIndex < 0)
            {
                for (int i = 0; i < _rendererContainers.Count; i++)
                {
                    if (_rendererContainers[i].HasFreeRenderer())
                    {
                        container = _rendererContainers[i];
                        break;
                    }
                }
            }
            else
            {
                container = _rendererContainers[screenIndex];
            }

            return container?.FindFreeRenderer();
        }

        #endregion

        #region 视频源管理

        public string GetLecturerId()
        {
            return _curLecturerId;
        }

        private void SetLecturer(string videoParticipantId)
        {
            _curLecturerId = videoParticipantId;
            Log("| -----> On Lecturer Set -> {0}", videoParticipantId);
        }

        private void SetLecturerNull()
        {
            _curLecturerId = "";
            Log("| -----> On Lecturer Set -> Null");
        }

        private bool IsLecturer(Participant participant)
        {
            return VideoParticipant.IsLecturer(participant, _curLecturerId);
        }

        private bool IsLecturerMe()
        {
            return IsLecturer(_participantManager._virtualParticipantMe.participant);
        }

        private bool IsLecturerNull()
        {
            return _curLecturerId == "";
        }

        private bool IsFromLecturer(VideoSource videoSource)
        {
            if (videoSource?.@from == null) return false;
            Participant participant = videoSource?.from?.participant;
            return null != participant && IsLecturer(participant);
        }

        /**
         * 获取可用的视频源
         * 当开启pip时，pip会显示本地第一路，则第一路不可指定到其他渲染器
         */
        public ConcurrentDictionary<string, VideoSource> GetAvailableVideoSources()
        {
            if (!config.DisplaySelfViewInMeeting)
            {
                return _videoSources;
            }

            ConcurrentDictionary<string, VideoSource> videoSources = new ConcurrentDictionary<string, VideoSource>();
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (VideoSourceType.TYPE_LOCAL_CAMERA.Equals(pair.Value.type))
                {
                    break;
                }

                videoSources.TryAdd(pair.Key, pair.Value);
            }

            return videoSources;
        }

        private void AddVideoSource(VideoSource videoSource)
        {
            if (!_videoSources.ContainsKey(videoSource.GetSourceKey()))
            {
                _videoSources.TryAdd(videoSource.GetSourceKey(), videoSource);
            }

            _socketService.SendVideoSourcesUpdatedEventToAll();
        }

        private void RemoveVideoSource(string sourceKey)
        {
            if (_videoSources.ContainsKey(sourceKey))
            {
                _videoSources.TryRemove(sourceKey, out _);
                _socketService.SendVideoSourcesUpdatedEventToAll();
            }
        }

        public void ClearVideoSource()
        {
            _videoSources.Clear();
            _socketService.SendVideoSourcesUpdatedEventToAll();
        }

        public int GetVideoSourceCount(String videoSourceType)
        {
            int count = 0;
            foreach (VideoSource source in _videoSources.Values)
            {
                if (String.Equals(videoSourceType, source.type))
                {
                    count += 1;
                }
            }

            return count;
        }

        public bool IsHasRemoteWindowShare()
        {
            bool has = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsRemoteWindowShare()) has = true;
            }

            return has;
        }

        public bool IsHasRemoteWindowShare(Participant participant)
        {
            bool has = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsRemoteWindowShare() &&
                    String.Equals(pair.Value.@from?.GetUserId(), participant?.GetUserId()))
                {
                    has = true;
                }
            }

            return has;
        }

        private bool IsHasLocalCamera()
        {
            bool has = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsLocalCamera())
                {
                    has = true;
                }
            }

            return has;
        }

        private VideoSource FindLocalCameraVideoSource()
        {
            VideoSource videoSource = null;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsLocalCamera())
                {
                    videoSource = pair.Value;
                }
            }

            return videoSource;
        }

        private bool IsHasLocalCamera2()
        {
            bool has = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsLocalCamera2())
                {
                    has = true;
                }
            }

            return has;
        }

        private bool IsHasVirtualSource()
        {
            bool has = false;
            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                if (pair.Value.IsVirtualSource())
                {
                    has = true;
                }
            }

            return has;
        }

        public VideoSource FindVideoSource(string sourceKey)
        {
            if (_videoSources.ContainsKey(sourceKey))
            {
                return _videoSources[sourceKey];
            }

            return null;
        }

        public VideoSource FindVideoSourceForMajorStream(Participant participant)
        {
            List<VideoSource> results = new List<VideoSource>();

            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                VideoParticipant from = pair.Value.from;
                if (
                    null != from &&
                    String.Equals(from.GetUserId(), participant.GetUserId()) &&
                    pair.Value.IsRemoteCamera()
                )
                {
                    return pair.Value;
                }
            }

            return null;
        }

        public List<VideoSource> FindVideoSourcesForMinorStream(Participant participant)
        {
            List<VideoSource> results = new List<VideoSource>();

            foreach (KeyValuePair<string, VideoSource> pair in _videoSources)
            {
                VideoParticipant from = pair.Value.from;
                if (
                    null != from &&
                    String.Equals(from.GetUserId(), participant.GetUserId()) &&
                    !pair.Value.IsRemoteCamera()
                )
                {
                    results.Add(pair.Value);
                }
            }

            return results;
        }

        //--------------------Remote Camera--------------------
        public void OnRemoteCameraAdded(RemoteCamera remoteCamera, Participant participant)
        {
            if (participant.GetUserId().StartsWith("Guest")) //guest policy
            {
                switch (participant.GetApplicationType())
                {
                    case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_None:
                        if (!config.DisplayGuestVideo)
                        {
                            Log(
                                "-----> On Remote Camera Added found guest_user and displayGuestVideo is false,so not to display guest_user.");
                            return;
                        }

                        break;
                    case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_Gateway:
                        if (!config.DisplayGatewayVideo)
                        {
                            Log(
                                "-----> On Remote Camera Added found guest_gateway and displayGatewayVideo is false,so not to display guest_gateway.");
                            return;
                        }

                        break;
                }
            }

            lock (_Lock)
            {
                VideoSource videoSource = new VideoSource(
                    remoteCamera,
                    -1,
                    -1);
                videoSource.SetFrom(new VideoParticipant(participant));
                AddVideoSource(videoSource);
                _participantManager.AddVideoSource(videoSource);

                Log(
                    $"-----> On Remote Camera Added ({videoSource.@from?.userId} , {videoSource.name} , {videoSource.@from?.name})");

                if (IsAutoAssignRenderer())
                {
                    IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                    {
                        //当此画面为演讲者的主画面时，且主讲位有之前讲话人图像时，需要先将之前的演讲者画面转移
                        if (IsFromLecturer(videoSource))
                        {
                            VideoSource curLecturerRendererSource =
                                GetDefaultRendererContainer()._lecturerRenderers[0]?._videoSource;
                            if (null != curLecturerRendererSource && !IsFromLecturer(curLecturerRendererSource))
                            {
                                StopRendering(curLecturerRendererSource.GetSourceKey());
                                StartRendering(curLecturerRendererSource.GetSourceKey());
                                Log(
                                    "-----| Found the lecturer renderer is rendering last lecturer video,so need transfer it to another renderer.");
                            }
                        }

                        StartRendering(videoSource.GetSourceKey());
                    }));
                    while (!asyncResult.IsCompleted)
                    {
                    }
                    //todo 需要处理固定主摄时的情况
                }
            }
        }

        public void OnRemoteCameraRemoved(RemoteCamera remoteCamera, Participant participant)
        {
            if (participant.GetUserId().StartsWith("Guest")) //guest policy
            {
                switch (participant.GetApplicationType())
                {
                    case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_None:
                        if (!config.DisplayGuestVideo)
                        {
                            Log(
                                "-----> OnRemoteCameraRemoved found guest_user and displayGuestVideo is false,so do not nothing.");
                            return;
                        }

                        break;
                    case Participant.ParticipantApplicationType.ParticipantAPPLICATIONTYPE_Gateway:
                        if (!config.DisplayGatewayVideo)
                        {
                            Log(
                                "-----> OnRemoteCameraRemoved found guest_gateway and displayGatewayVideo is false,so do not nothing.");
                            return;
                        }

                        break;
                }
            }

            lock (_Lock)
            {
                Log(
                    $"-----> OnRemoteCameraRemoved({participant.GetUserId()} , {remoteCamera.GetName()} , {participant.GetName()})");

                String sourceKey = VideoSource.GetSourceKey(remoteCamera);
                _participantManager.RemoveVideoSource(participant.GetUserId(), sourceKey);
                RemoveVideoSource(sourceKey);

                IAsyncResult asyncResult = StopRenderingOnMain(sourceKey);
                while (!asyncResult.IsCompleted)
                {
                }
            }
        }

        public void OnRemoteCameraStateUpdated(RemoteCamera remoteCamera, Participant participant,
            Device.DeviceState state)
        {
        }


        //--------------------Remote Window Share--------------------
        public void OnRemoteWindowShareAdded(RemoteWindowShare remoteWindowShare, Participant participant)
        {
            lock (_Lock)
            {
                VideoSource videoSource = new VideoSource(
                    remoteWindowShare,
                    -1,
                    -1);
                videoSource.SetFrom(new VideoParticipant(participant));
                AddVideoSource(videoSource);
                _participantManager.AddVideoSource(videoSource);

                Log(
                    $"-----> OnRemoteWindowShareAdded ({videoSource.@from?.userId} , {videoSource.name} , {videoSource.@from?.name})");

                //当有某人发送辅流后，不再允许本端发送辅流（屏蔽UI按钮）
                _socketService.SendFunctionAvailableEventToAll(false);

                if (config.AutoAssignRenderer)
                {
                    //当单屏下有人发出共享时且如果处于网格布局，则需要变换布局模式
                    if ((_curLayoutMode == LayoutMode.MODE_NORMAL ||
                         _curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER))
                    {
                        Log("-----| change customized layout to fixed lecturer.");
                        ChangeCustomizedLayoutModeOnMainWait(
                            LayoutMode.MODE_LECTURE_FIXED_LECTURER,
                            VideoParticipant.ParseId(participant));
                    }
                    else
                    {
                        Log("-----| current layout is fixed mode, so rendering directly.");
                        IAsyncResult asyncResult = StartRenderingOnMain(videoSource.GetSourceKey());
                        while (!asyncResult.IsCompleted)
                        {
                        }
                    }
                }
                else
                {
                    //当手动布局时，需要记录辅流发起者为主讲人
                    if ((_curLayoutMode == LayoutMode.MODE_NORMAL ||
                         _curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER))
                    {
                        _curLayoutMode = LayoutMode.MODE_LECTURE_FIXED_LECTURER;
                        // _curLecturerId = _savedLecturerId = VideoParticipant.ParseId(participant);
                        string lecturerId = VideoParticipant.ParseId(participant);
                        SetLecturer(lecturerId);
                        SaveLecturerId(lecturerId);
                        Log(
                            "-----| It is not auto assign renderer mode,So recording the lecturer id when remote window share added.");
                    }
                }
            }
        }

        public void OnRemoteWindowShareRemoved(RemoteWindowShare share, Participant participant)
        {
            lock (_Lock)
            {
                Log(
                    $"-----> OnRemoteWindowShareRemoved ({participant.GetUserId()} , {share.GetName()} , {participant.GetName()})");

                string sourceKey = VideoSource.GetSourceKey(share);
                _participantManager.RemoveVideoSource(participant.GetUserId(), sourceKey);

                RemoveVideoSource(sourceKey);

                IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                {
                    StopRendering(sourceKey);

                    //主讲人无辅流后，则需要变换布局到用户设定的布局
                    if (IsLecturer(participant) && !IsHasRemoteWindowShare(participant))
                    {
                        //当有某人停止所有辅流后，将允许其他人发送辅流
                        _socketService.SendFunctionAvailableEventToAll(true);

                        ChangeCustomizedLayoutMode(_savedLayoutMode, _savedLecturerId);
                    }

                    //当多窗口且开启固定主摄时，他人无辅流后，需要把固定画面显示回来
                    if (isMainCameraPin && !IsHasRemoteWindowShare(participant))
                    {
                        AssignLocalCameraToLecturerForce();
                    }
                }));
                while (!asyncResult.IsCompleted)
                {
                }
            }
        }

        public void OnRemoteWindowShareStateUpdated(RemoteWindowShare remoteWindowShare, Participant participant,
            Device.DeviceState state)
        {
        }

        #endregion

        #region 语音激励

        public void OnLoudestParticipantChanged(Participant speaker, bool audioOnly)
        {
            lock (_Lock)
            {
                Log("-----> On Loudest Participant Changed ({0} , {1})", speaker.GetName(), audioOnly);

                _lastSpeaker = speaker;

                //在讲话人画面绘制颜色边框
                VideoRenderer speakerMajorRenderer = FindMajorRendererByParticipant(speaker);
                RunOnUIThread((Action)(() => { speakerMajorRenderer?.PlaySpeakingAnim(); }));

                //主讲人说话不需要激励
                if (_curLayoutMode == LayoutMode.MODE_LECTURE_FIXED_LECTURER &&
                    VideoParticipant.IsLecturer(speaker, _curLecturerId))
                {
                    return;
                }

                //语音激励布局
                // if (IsVoiceActivatedEnabled() && !HasMaximizeRenderer())
                if (IsVoiceActivatedEnabled() && IsAutoAssignRenderer())
                {
                    IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                    {
                        VideoRenderer voiceActivatedRenderer = FindRendererForVoiceActivated();
                        if (null == voiceActivatedRenderer)
                        {
                            Log("-----| Assign Renderer To LoudestParticipant failed,voiceActivatedRenderer is null.");
                            return;
                        }

                        if (null == speakerMajorRenderer)
                        {
                            //当说话人未渲染时，显示到指定 语音激励的 渲染器
                            VideoSource videoSource = FindVideoSourceForMajorStream(speaker);

                            if (null != videoSource)
                            {
                                Log("-----| Assign Speaker camera view to voice_renderer (has not rendering before).");
                                StopRendering(voiceActivatedRenderer);
                                StartRendering(voiceActivatedRenderer, videoSource);
                            }
                            else
                            {
                                Log("-----| Assign Speaker camera view to nothing (speaker_camera_view not exist).");
                            }
                        }
                        else
                        {
                            //当说话人已经渲染在语音激励渲染器时，do nothing
                            if (
                                _curLayoutMode != LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER &&
                                IsVoiceActivatedRenderer(speakerMajorRenderer)
                            ) return;
                            //当说话人已渲染时，和指定语音激励渲染器互换显示位置
                            if (voiceActivatedRenderer.IsInUse())
                            {
                                Log(
                                    "-----| Swap Speaker major renderer with voice_renderer (voice_renderer in used by other).");
                                SwapStreamsBetweenRenderers(speakerMajorRenderer, voiceActivatedRenderer);
                            }
                            else
                            {
                                Log("-----| Assign Speaker camera to voice_renderer.");
                                VideoSource videoSource = FindVideoSource(speakerMajorRenderer._sourceKey);
                                StopRendering(speakerMajorRenderer);
                                StartRendering(voiceActivatedRenderer, videoSource);
                            }
                        }

                        if (_curLayoutMode == LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER)
                        {
                            String participantId = VideoParticipant.ParseId(speaker);

                            //渲染主讲人的辅流,如果有的话
                            //变换主讲人时，应停止渲染上个主讲人的辅流，但固定主摄画面时例外
                            if (!isMainCameraPin && !IsLecturer(speaker))
                            {
                                GetDefaultRendererContainer().StopRenderingLecturerMinorStream(_connector);
                                Log("-----| Stop last lecturer minor stream.");
                            }

                            List<VideoSource> sources = FindVideoSourcesForMinorStream(speaker);
                            foreach (VideoSource source in sources)
                            {
                                StartRenderingForLecturer(source);
                            }

                            SetLecturer(participantId);
                        }
                    }));
                    while (!asyncResult.IsCompleted)
                    {
                    }
                }
            }
        }

        private bool HasMaximizeRenderer()
        {
            if (config.VoiceActivatedPositionOfScreens < _rendererContainers.Count)
            {
                VideoRendererContainer container = _rendererContainers[config.VoiceActivatedPositionOfScreens];
                return container.HasMaximizeRenderer();
            }

            return false;
        }

        #endregion

        public void RegisterParticipantsChangedListener()
        {
            _participantManager.participantsChangedListener = this;
        }

        public void UnregisterParticipantsChangedListener()
        {
            _participantManager.participantsChangedListener = null;
        }

        public void OnParticipantsChanged(
            bool isJoinedEvent,
            Participant participant,
            List<Participant> participants,
            bool isJustMeAlone)
        {
            lock (_Lock)
            {
                Log("-----> On Participants {0} : isJustMe->{1} , p count->{2}",
                    isJoinedEvent ? "Joined" : " Left",
                    isJustMeAlone,
                    participants?.Count);
                Log("-----| Participant : ({0},{1},{2})",
                    participant.GetUserId(),
                    participant.GetApplicationType(),
                    participant.GetId());

                bool isLeftEvent = !isJoinedEvent;
                if (isLeftEvent)
                {
                    //清空lastSpeaker，当lastSpeaker离场时。
                    if (!_participantManager.IsParticipantExist(_lastSpeaker))
                    {
                        _lastSpeaker = null;
                        Log("-----| The last speaker is left from this room, set cache to null.");
                    }

                    //清空主讲人（若为自己则不需要），当主讲人离场时
                    if (!_participantManager.IsLecturerExist(_curLecturerId))
                    {
                        SetLecturerNull();
                        Log($"-----| The lecturer ({_curLecturerId}) is left from this room, set cache to null.");
                    }
                    else
                    {
                        Log($"-----| The lecturer ({_curLecturerId}) is in the room...");
                    }
                }

                //调整主摄的位置，在满足一定条件的情况下。
                if (IsAutoAssignRenderer() &&
                    GetWindowNumber() >= 2 &&
                    !IsLecturerMe() &&
                    IsHasLocalCamera())
                {
                    VideoSource cameraVideoSource = FindLocalCameraVideoSource();
                    bool inLecturerRendererContainer = cameraVideoSource?.positionOfScreen == 0;

                    VideoRenderer origin = null;
                    VideoRenderer destination = null;

                    //移动主摄至主屏，当房间内仅自己后且主摄像不在主屏时。
                    if (null != cameraVideoSource && isJustMeAlone && !inLecturerRendererContainer)
                    {
                        origin = FindRenderer(cameraVideoSource.GetSourceKey());
                        destination = GetDefaultRendererContainer().FindFreeLecturerRenderer();
                        Log("-----| move the camera to the main window.");
                    }

                    //移动主摄至副屏，当有新参会人后且在主屏 且 未开启主摄Pin时。
                    if (!isMainCameraPin && null != cameraVideoSource && !isJustMeAlone && inLecturerRendererContainer)
                    {
                        origin = FindRenderer(cameraVideoSource.GetSourceKey());
                        destination = FindFreeRenderer(1);
                        Log("-----| move the camera to the other window.");
                    }

                    //执行移动
                    if (null != origin && null != destination)
                    {
                        IAsyncResult asyncResult = RunOnUIThread((Action)(() =>
                        {
                            StopRendering(origin);
                            StartRendering(destination, cameraVideoSource);
                        }));
                        while (!asyncResult.IsCompleted)
                        {
                        }
                    }
                    else
                    {
                        destination?.Release();
                    }
                }
            }
        }

        #region 通用函数

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return _videoPanel.BeginInvoke(method);
        }

        private void Log(string content)
        {
            _logger.Info("[LayoutController] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[LayoutController] " + content, args);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}