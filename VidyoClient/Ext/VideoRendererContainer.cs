using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web.Globalization;
using System.Windows.Forms;
using CefSharp.Structs;
using NLog;
using NLog.Layouts;
using VidyoClient;
using VidyoClient.Ext;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace VideoClient.VidyoClient.Ext
{
    public class VideoRendererContainer : IRendererMaximizeListener, IRendererPinListener, IRendererExchangeListener
    {
        public Connector _connector;
        public Panel _container;
        private readonly int id;

        // public int curAvailableRenderersNumber;//当前最大可用一般渲染器数，根据不同模式数量不同

        private int rendererNumber;
        private int rendererNumberInLectureMode = 6;
        public int curFixedLayoutNumber; //强制分屏数

        private bool _displaySelfViewPIP;
        public bool _displaySelfViewInMeeting;
        private bool _isMajorBiggerInPIP;

        public VideoRenderer pipRenderer;
        public List<VideoRenderer> _lecturerRenderers = new List<VideoRenderer>(); //用于渲染主讲人的视频源
        public List<VideoRenderer> _renderers = new List<VideoRenderer>(); //用于渲染其他参会者的视频源

        public LayoutMode _curLayoutMode = LayoutMode.MODE_NORMAL; //仅第一渲染容器支持
        private float lectureModeLayoutHeightRatio = 7f / 9f; //主讲人渲染区高度屏幕占比

        //网格布局的行列配置（从4分屏开始才需要）
        private Dictionary<LayoutGridMode, int[]> gridLayoutConfig = new Dictionary<LayoutGridMode, int[]>()
        {
            [LayoutGridMode.GridFour] = new[] { 2, 2 }, //{行数、列数}
            [LayoutGridMode.GridSix] = new[] { 2, 3 },
            [LayoutGridMode.GridEight] = new[] { 2, 4 },
            [LayoutGridMode.GridNine] = new[] { 3, 3 },
            [LayoutGridMode.GridTwelve] = new[] { 4, 3 },
            [LayoutGridMode.GridSixteen] = new[] { 4, 4 },
            [LayoutGridMode.GridTwenty] = new[] { 4, 5 },
            [LayoutGridMode.GridTwentyFive] = new[] { 5, 5 },
        };

        public VideoRendererContainer(
            Connector connector,
            Panel container,
            int id,
            int renderersNumber,
            bool displaySelfViewPIP,
            bool displayZoomBtnOnLocal,
            bool displayZoomBtnOnRemote,
            bool displayZoomBtnOnLecturer,
            bool displaySnapshotBtn,
            bool displayRecordBtn)
        {
            _connector = connector;
            _container = container;
            _container.BackColor = Color.Black;

            this.id = id;

            this.rendererNumber = renderersNumber;

            _displaySelfViewPIP = displaySelfViewPIP;

            for (int i = 0; i < 4; i++)
            {
                Panel renderPanel = new Panel();
                renderPanel.Name = "lecturer_" + i;
                renderPanel.Visible = false;
                renderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                renderPanel.BorderStyle = BorderStyle.FixedSingle;
                _container.Controls.Add(renderPanel);

                VideoRenderer r = new VideoRenderer(
                    i,
                    id,
                    renderPanel);
                r._maximizeListener = this;
                r._pinListener = this;
                r._exchangeListener = this;
                if (displayZoomBtnOnLecturer) r.EnableMaximizeBtn();
                r.EnablePinBtn();
                r.EnableExchangeBtn();
                r.SetExchangeBtnVisible(false);
                if (displaySnapshotBtn) r.EnableSnapshot();
                if (displayRecordBtn) r.EnableRecord();
                _lecturerRenderers.Add(r);
            }

            for (int i = 0;
                 i < (rendererNumberInLectureMode > rendererNumber
                     ? rendererNumberInLectureMode
                     : rendererNumber); //按多的一种模式的渲染器数量去构建
                 i++)
            {
                Panel renderPanel = new Panel();
                renderPanel.Name = i.ToString();
                renderPanel.Visible = false;
                renderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                renderPanel.BorderStyle = BorderStyle.FixedSingle;
                _container.Controls.Add(renderPanel);

                VideoRenderer r = new VideoRenderer(i, id, renderPanel);
                r._maximizeListener = this;
                if (displayZoomBtnOnRemote) r.EnableMaximizeBtn();
                if (displaySnapshotBtn) r.EnableSnapshot();
                if (displayRecordBtn) r.EnableRecord();
                _renderers.Add(r);
            }

            if (_displaySelfViewPIP)
            {
                Panel renderPanel = new Panel();
                renderPanel.Name = "local_camera";
                renderPanel.Visible = false;
                renderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                pipRenderer = new VideoRenderer(1000, 0, renderPanel);
                pipRenderer._maximizeListener = this;
                if (displayZoomBtnOnLocal) pipRenderer.EnableMaximizeBtn();
                _container.Controls.Add(renderPanel);
            }
        }

        public int[] GetSupportedGridLayoutNumber()
        {
            Array values = Enum.GetValues(typeof(LayoutGridMode));
            int[] result = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = (int)values.GetValue(i);
            }

            return result;
        }

        private string supportedGridLayoutNumberJson;

        public string GetSupportedGridLayoutNumberJson()
        {
            if (String.IsNullOrEmpty(supportedGridLayoutNumberJson))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                int[] ints = GetSupportedGridLayoutNumber();
                for (int i = 0; i < ints.Length; i++)
                {
                    sb.Append(ints[i]);
                    if (i != (ints.Length - 1)) sb.Append(",");
                }

                sb.Append("]");
                supportedGridLayoutNumberJson = sb.ToString();
            }

            return supportedGridLayoutNumberJson;
        }

        public int GetAvailableRendererNumber()
        {
            switch (_curLayoutMode)
            {
                case LayoutMode.MODE_NORMAL:
                    return rendererNumber;
                case LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER:
                case LayoutMode.MODE_LECTURE_FIXED_LECTURER:
                    return rendererNumberInLectureMode;
                default:
                    return 0;
            }
        }

        public bool HasSourceKey(string sourceKey)
        {
            foreach (VideoRenderer renderer in _renderers)
            {
                if (String.Equals(sourceKey, renderer._sourceKey))
                {
                    return true;
                }
            }

            return false;
        }

        public VideoRenderer GetSelfViewPipRenderer()
        {
            return pipRenderer;
        }

        public bool IsSelfViewPipVisible()
        {
            return null != pipRenderer && pipRenderer._panel.Visible;
        }

        public void SetSelfViewPipVisible(bool visible)
        {
            if (null != pipRenderer)
            {
                pipRenderer._panel.Visible = visible;
            }
        }

        public VideoRenderer FindRenderer(string sourceKey)
        {
            foreach (VideoRenderer renderer in _renderers)
            {
                if (String.Equals(sourceKey, renderer._sourceKey))
                {
                    return renderer;
                }
            }

            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (String.Equals(sourceKey, renderer._sourceKey))
                {
                    return renderer;
                }
            }

            return null;
        }

        public VideoRenderer FindMajorRendererByParticipant(Participant participant)
        {
            foreach (VideoRenderer renderer in GetRenderingList().Concat(GetRenderingLecturerList()))
            {
                VideoSource videoSource = renderer._videoSource;
                VideoParticipant videoParticipant = renderer.GetVideoSourceFrom();
                if (null != videoSource && videoSource.IsRemoteCamera() && null != videoParticipant)
                {
                    if (String.Equals(participant.GetUserId(), videoParticipant.GetUserId()))
                    {
                        return renderer;
                    }
                }
            }

            // foreach (VideoRenderer renderer in GetRenderingLecturerList())
            // {
            //     VideoParticipant videoParticipant = renderer.GetVideoSourceFrom();
            //     if (null != videoParticipant)
            //     {
            //         if (String.Equals(participant.GetUserId(), videoParticipant.GetUserId()))
            //         {
            //             return renderer;
            //         }
            //     }
            // }

            return null;
        }

        public bool HasMaximizeRenderer()
        {
            foreach (VideoRenderer renderer in _renderers)
            {
                if (renderer._isMaximize)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasFreeRenderer()
        {
            switch (_curLayoutMode)
            {
                case LayoutMode.MODE_LECTURE_ONLY_LECTURER:
                    return false;
                default:
                    foreach (VideoRenderer renderer in _renderers)
                    {
                        if (!renderer.IsInUse())
                        {
                            return true;
                        }
                    }

                    return false;
            }
        }

        public VideoRenderer FindFreeRenderer()
        {
            Log("FindFreeRenderer begin ->  containerId:{0} , LayoutMode:{1}",
                id,
                Enum.GetName(typeof(LayoutMode), _curLayoutMode));
            switch (_curLayoutMode)
            {
                case LayoutMode.MODE_NORMAL:
                    if (curFixedLayoutNumber > 0) //强制分屏数
                    {
                        if (InRenderingCount() >= curFixedLayoutNumber)
                        {
                            Log("FindFreeRenderer result -> renderer not enough...(enabled fixed num of layout)");
                            return null;
                        }
                    }
                    else //自动分屏数
                    {
                        if (InRenderingCount() >= rendererNumber)
                        {
                            Log("FindFreeRenderer result -> renderer not enough...");
                            return null;
                        }
                    }

                    break;
                case LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER:
                case LayoutMode.MODE_LECTURE_FIXED_LECTURER:
                    if (InRenderingCount() >= rendererNumberInLectureMode)
                    {
                        Log("FindFreeRenderer result -> renderer not enough...");
                        return null;
                    }

                    break;
                case LayoutMode.MODE_LECTURE_ONLY_LECTURER:
                    Log("FindFreeRenderer result -> renderer not enough...(because current mode is only lecturer)");
                    return null;
            }

            foreach (VideoRenderer renderer in _renderers)
            {
                if (renderer.RequestUse())
                {
                    Log("FindFreeRenderer result -> found free renderer : {0}.", renderer.GetFingerPrint());
                    return renderer;
                }
            }

            Log("FindFreeRenderer result -> return null...");
            return null;
        }

        public VideoRenderer FindFreeLecturerRenderer()
        {
            Log("FindFreeLecturerRenderer begin -> containerId:{0} , LayoutMode:{1}",
                id,
                Enum.GetName(typeof(LayoutMode), _curLayoutMode));
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (renderer.RequestUse())
                {
                    Log("FindFreeLecturerRenderer result -> found free renderer:{0}", renderer.GetFingerPrint());
                    return renderer;
                }
            }

            Log("FindFreeLecturerRenderer result -> return null...");
            return null;
        }

        public bool IsAllLecturerRendererIdle()
        {
            return InRenderingCountLecturer() == 0;
        }

        private int InRenderingCount()
        {
            int count = 0;
            foreach (VideoRenderer renderer in _renderers)
            {
                if (renderer.IsInUse())
                {
                    count += 1;
                }
            }

            return count;
        }

        private int InRenderingCountLecturer()
        {
            int count = 0;
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (renderer.IsInUse())
                {
                    count += 1;
                }
            }

            return count;
        }

        private List<VideoRenderer> GetRenderingList()
        {
            List<VideoRenderer> result = new List<VideoRenderer>();
            foreach (VideoRenderer renderer in _renderers)
            {
                if (renderer.IsInUse())
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        public List<VideoRenderer> GetNotRenderingList()
        {
            List<VideoRenderer> result = new List<VideoRenderer>();
            foreach (VideoRenderer renderer in _renderers)
            {
                if (!renderer.IsInUse())
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        private List<VideoRenderer> GetRenderingLecturerList()
        {
            List<VideoRenderer> result = new List<VideoRenderer>();
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (renderer.IsInUse())
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        public List<VideoRenderer> GetNotRenderingLecturerList()
        {
            List<VideoRenderer> result = new List<VideoRenderer>();
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (!renderer.IsInUse())
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        public VideoRenderer FindRendererById(int id)
        {
            foreach (VideoRenderer renderer in _renderers)
            {
                if (id == renderer.id)
                {
                    return renderer;
                }
            }

            return null;
        }

        public void SetLayoutMode(Connector connector, LayoutMode layoutMode)
        {
            _curLayoutMode = layoutMode;

            // Log("SetLayoutMode({0})", layoutMode);

            RefreshLayout(connector);
        }

        public void SetForceLayoutNum(Connector connector, int num)
        {
            curFixedLayoutNumber = num > rendererNumber ? rendererNumber : num;

            RefreshLayout(connector);
        }

        public void RefreshLayout()
        {
            lock (_container)
            {
                RefreshLayout(_connector);
            }
        }

        private void RefreshLayout(Connector connector)
        {
            Log("↓");
            Log("------------------------RefreshLayout Start------------------------");
            Log("| Container Id: {0}, layout: {1}", id, _curLayoutMode);
            switch (_curLayoutMode)
            {
                case LayoutMode.MODE_NORMAL:
                    LayOutNormal(connector);
                    foreach (VideoRenderer renderer in _lecturerRenderers)
                    {
                        renderer._panel.Visible = false;
                    }

                    break;
                case LayoutMode.MODE_LECTURE_DYNAMIC_LECTURER:
                case LayoutMode.MODE_LECTURE_FIXED_LECTURER:
                    LayOutLecture(connector, false);
                    break;
                case LayoutMode.MODE_LECTURE_ONLY_LECTURER:
                    LayOutLecture(connector, true);
                    break;
            }

            Log("------------------------RefreshLayout   End------------------------");
            Log("↑");
        }

        private void LayOutNormal(Connector connector)
        {
            Rectangle bounds = _container.ClientRectangle;
            Log("| Start lay out by normal , Bounds -> {0} x {1}",
                bounds.Width, bounds.Height);

            int inRenderingCount = InRenderingCount();
            Log("| Rendering count -> {0}", inRenderingCount);

            int layoutNum;
            List<VideoRenderer> renderers;
            if (curFixedLayoutNumber > 0)
            {
                Log("| Fixed tiles -> {0}", curFixedLayoutNumber);
                layoutNum = curFixedLayoutNumber;
                renderers = _renderers;
                //强制布局下，需要将所有panel显示，用来显示边线
                foreach (VideoRenderer renderer in renderers)
                {
                    renderer._panel.Visible = true;
                }

                //如果当前已渲染画面数大于强制分屏数，需要把多余得画面停止
                if (renderers.Count > curFixedLayoutNumber)
                {
                    for (int i = curFixedLayoutNumber; i < renderers.Count; i++)
                    {
                        VideoRenderer videoRenderer = renderers[i];
                        videoRenderer.StopRendering(connector);
                    }
                }
            }
            else
            {
                Log("| Fixed tiles -> Auto");
                layoutNum = inRenderingCount;
                renderers = GetRenderingList();
                //如果之前因为强制布局使所有panel显示，则需要在切换回自动布局时，隐藏之前显示得panel
                foreach (VideoRenderer renderer in GetNotRenderingList())
                {
                    renderer._panel.Visible = false;
                }
            }

            SetSameSizeLayout(renderers, bounds, layoutNum);
            SyncViewPointSize(connector, renderers);

            //设置自视样式
            if (_displaySelfViewPIP)
            {
                if (inRenderingCount == 0)
                {
                    // pipRenderer._panel.Location = new Point(bounds.X, bounds.Y);
                    // pipRenderer._panel.Size = new Size(bounds.Width, bounds.Height);
                    SetRenderPointSize(pipRenderer, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    SetSelfViewPipVisible(true);
                    Log("| Current self view mode is pip, remote is empty, set self view fullscreen.");
                }
                else
                {
                    // pipRenderer._panel.Location =
                    //     new Point(
                    //         bounds.Width - (bounds.Width / 6) - 15,
                    //         bounds.Height - (bounds.Height / 6) - 15);
                    // pipRenderer._panel.Size = new Size(bounds.Width / 6, bounds.Height / 6);
                    SetRenderPointSize(pipRenderer,
                        bounds.Width - (bounds.Width / 6) - 15,
                        bounds.Height - (bounds.Height / 6) - 15,
                        bounds.Width / 6,
                        bounds.Height / 6);
                    Log("| Current self view mode is pip, remote is not empty, " +
                        (_displaySelfViewInMeeting ? "set self view small." : "set self view invisible."));
                    SetSelfViewPipVisible(_displaySelfViewInMeeting);
                }

                connector.ShowViewAt(pipRenderer._panel.Handle, 0, 0,
                    (uint)pipRenderer._panel.Width,
                    (uint)pipRenderer._panel.Height);
                pipRenderer._panel.BringToFront();
                pipRenderer.ShowTools();
                Log("| ShowViewAtPoints (PIP self view)");
            }
        }

        /**
         * @isOnlyLecturer 是否lecturer占满容器，否表示下部分留给显示参会人用。
         */
        private void LayOutLecture(Connector connector, bool isOnlyLecturer)
        {
            //显示区域
            Rectangle bounds = _container.ClientRectangle;
            Log("| Start lay out by Lecture , Bounds -> {0} x {1}",
                bounds.Width, bounds.Height);

            //渲染主讲人画面
            Rectangle lecturerBounds = isOnlyLecturer ? bounds : GenerateLecturerRectangle(bounds);
            LogBounds(
                "| Lecturer Bounds -> [{0},{1}] {2}x{3}",
                lecturerBounds.X,
                lecturerBounds.Y,
                lecturerBounds.Width,
                lecturerBounds.Height);

            int layoutNum; //主讲人分屏数
            List<VideoRenderer> lecturerRenderers;

            //当指定强制分屏数时
            if (curFixedLayoutNumber > 0)
            {
                Log("| Fixed tiles -> {0}", curFixedLayoutNumber);
                layoutNum = curFixedLayoutNumber;
                lecturerRenderers = _lecturerRenderers;

                foreach (VideoRenderer renderer in lecturerRenderers)
                {
                    renderer._panel.Visible = true;
                }

                //如果当前已渲染画面数大于强制分屏数，需要把多余得画面停止
                if (lecturerRenderers.Count > curFixedLayoutNumber)
                {
                    for (int i = curFixedLayoutNumber; i < lecturerRenderers.Count; i++)
                    {
                        VideoRenderer videoRenderer = lecturerRenderers[i];
                        videoRenderer.StopRendering(connector);
                    }
                }
            }
            else
            {
                Log("| Fixed tiles -> Auto");
                layoutNum = InRenderingCountLecturer();
                lecturerRenderers = GetRenderingLecturerList();

                //如果之前因为强制布局使所有panel显示，则需要在切换回自动布局时，隐藏之前显示得panel
                foreach (VideoRenderer renderer in GetNotRenderingLecturerList())
                {
                    renderer._panel.Visible = false;
                }

                // foreach (VideoRenderer renderer in GetRenderingLecturerList())
                // {
                // Log("| Panel visible -> {0}", renderer._panel.Visible);
                // Log("| Btn {0} -> {1}x{2};{3}x{4}",
                //     renderer.maximizeBtn?.Visible,
                //     renderer.maximizeBtn?.Left,
                //     renderer.maximizeBtn?.Top,
                //     renderer.maximizeBtn?.Width, renderer.maximizeBtn?.Height);
                // renderer.maximizeBtn?.BringToFront();
                // renderer._panel.Visible = true;
                // }
            }

            //当主持人 一路摄像头 一路辅流 时，应采取pip布局
            if (IsNeedLecturerPip(lecturerRenderers))
            {
                Log("| Lecturer area mode -> PIP");
                //隐藏pin按钮、复原pin状态
                foreach (VideoRenderer renderer in lecturerRenderers)
                {
                    renderer.SetPinState(false);
                    renderer.SetPinBtnVisible(false);
                    renderer.SetExchangeBtnVisible(false);
                }

                //布局pip
                VideoRenderer smallerRenderer = _isMajorBiggerInPIP
                    ? FindLecturerWindowShareRenderer(lecturerRenderers)
                    : FindLecturerCameraRenderer(lecturerRenderers);
                VideoRenderer biggerRenderer = _isMajorBiggerInPIP
                    ? FindLecturerCameraRenderer(lecturerRenderers)
                    : FindLecturerWindowShareRenderer(lecturerRenderers);
                //设置共享为大画面
                SetRenderPointSize(biggerRenderer, lecturerBounds.X, lecturerBounds.Y, lecturerBounds.Width,
                    lecturerBounds.Height);
                //设置摄像头为小画面
                Point cameraLocation = new Point(
                    lecturerBounds.Width - (lecturerBounds.Width / 6) - 15,
                    lecturerBounds.Height - (lecturerBounds.Height / 6) - 15);
                Size cameraSize = new Size(lecturerBounds.Width / 6, lecturerBounds.Height / 6);
                SetRenderPointSize(smallerRenderer,
                    cameraLocation.X, cameraLocation.Y,
                    cameraSize.Width, cameraSize.Height);
                if (!IsFullScreenRendererExist())
                {
                    smallerRenderer.SetExchangeBtnVisible(true);
                    smallerRenderer._panel.BringToFront();
                }
            }
            else if (IsPinRendererExist())
            {
                Log("| Lecturer area mode -> Pin layout");
                //按pin布局排列
                VideoRenderer pinRenderer = null;
                List<VideoRenderer> otherRenderers = new List<VideoRenderer>();
                foreach (VideoRenderer r in lecturerRenderers)
                {
                    r.SetPinBtnVisible(true);
                    r.SetExchangeBtnVisible(false);
                    if (r._isPin)
                    {
                        pinRenderer = r;
                    }
                    else
                    {
                        otherRenderers.Add(r);
                    }
                }

                Rectangle pinBounds = new Rectangle();
                pinBounds.X = lecturerBounds.X;
                pinBounds.Y = lecturerBounds.Y;
                pinBounds.Width = lecturerBounds.Width / 6 * 5;
                pinBounds.Height = lecturerBounds.Height;
                if (null != pinRenderer)
                    SetRenderPointSize(pinRenderer, pinBounds.X, pinBounds.Y, pinBounds.Width, pinBounds.Height);

                Rectangle otherBounds = new Rectangle();
                otherBounds.X = pinBounds.Width;
                otherBounds.Y = lecturerBounds.Y;
                otherBounds.Width = lecturerBounds.Width / 6;
                otherBounds.Height = lecturerBounds.Height;
                LayOutLinearVertical(otherBounds, otherRenderers, 3);
            }
            else
            {
                Log("| Lecturer area mode -> Grid layout");
                if (lecturerRenderers.Count == 1)
                {
                    foreach (VideoRenderer renderer in lecturerRenderers)
                    {
                        renderer.SetExchangeBtnVisible(false);
                    }
                }

                //多于两路主讲人画面时，显示局部放大按钮
                if (lecturerRenderers.Count > 2)
                {
                    foreach (VideoRenderer renderer in lecturerRenderers)
                    {
                        renderer.SetPinBtnVisible(true);
                        renderer.SetExchangeBtnVisible(false);
                    }
                }

                SetSameSizeLayout(lecturerRenderers, lecturerBounds, layoutNum);
            }

            SyncViewPointSize(connector, lecturerRenderers);

            //渲染参会人画面
            if (!isOnlyLecturer)
            {
                Log("| Is only lecturer mode -> No");

                Rectangle participantsBounds = GenerateParticipantsRectangle(bounds, lecturerBounds);
                LogBounds(
                    "| Participants area bounds -> [{0},{1}] {2}x{3}",
                    participantsBounds.X,
                    participantsBounds.Y,
                    participantsBounds.Width,
                    participantsBounds.Height);

                List<VideoRenderer> participantsRenderingList = _renderers;
                LayOutLinearHorizontal(participantsBounds, participantsRenderingList, rendererNumberInLectureMode);

                SyncViewPointSize(connector, participantsRenderingList);
            }
            else
            {
                Log("| Is only lecturer mode -> Yes");
            }

            //隐藏本地自视
            SetSelfViewPipVisible(false);
        }

        private void SetSameSizeLayout(List<VideoRenderer> renderers, Rectangle bounds, int layoutNum)
        {
            if (layoutNum < 0)
            {
                Log("| SetSameSizeLayout error !!! layoutNum < 0");
                return;
            }

            //如果非枚举数，应归类到对应的分屏
            int[] ints = GetSupportedGridLayoutNumber();

            int targetModeInt = 0;

            if (layoutNum > 25)
            {
                targetModeInt = 25;
            }
            else
            {
                for (int i = 0; i < ints.Length; i++)
                {
                    //找到对应的
                    if (ints[i] == layoutNum)
                    {
                        targetModeInt = ints[i];
                        break;
                    }
                }

                if (targetModeInt == 0)
                {
                    for (int i = 0; i < ints.Length; i++)
                    {
                        //找到所属区间
                        if (layoutNum < ints[i])
                        {
                            targetModeInt = ints[i];
                            break;
                        }
                    }
                }
            }

            LayoutGridMode mode = (LayoutGridMode)targetModeInt;
            Log("| GridMode -> {0} (tile num : {1})",
                Enum.GetName(typeof(LayoutGridMode), mode),
                layoutNum);


            switch (mode)
            {
                case 0:
                    //TODO selfview full screen
                    break;
                case LayoutGridMode.GridSingle:
                    SetRenderPointSize(renderers, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    break;
                case LayoutGridMode.GridTwo:
                    SetRenderPointSize(renderers, 0, bounds.X, bounds.Y, bounds.Width / 2, bounds.Height);
                    SetRenderPointSize(renderers, 1, bounds.Width / 2, bounds.Y, bounds.Width / 2, bounds.Height);
                    break;
                case LayoutGridMode.GridThree:
                    SetRenderPointSize(renderers, 0, bounds.Width / 4, bounds.Y, bounds.Width / 2, bounds.Height / 2);
                    SetRenderPointSize(renderers, 1, bounds.X, bounds.Height / 2, bounds.Width / 2, bounds.Height / 2);
                    SetRenderPointSize(renderers, 2, bounds.Width / 2, bounds.Height / 2, bounds.Width / 2,
                        bounds.Height / 2);
                    break;
                case LayoutGridMode.GridFour:
                case LayoutGridMode.GridSix:
                case LayoutGridMode.GridEight:
                case LayoutGridMode.GridNine:
                case LayoutGridMode.GridTwelve:
                case LayoutGridMode.GridSixteen:
                case LayoutGridMode.GridTwenty:
                case LayoutGridMode.GridTwentyFive:

                    int columnCount = gridLayoutConfig[mode][1];
                    int rowCount = gridLayoutConfig[mode][0];

                    int width = bounds.Width / columnCount; //宽度除以列数，得到每个渲染器宽度
                    int height = bounds.Height / rowCount; //高度除以行数，得到每个渲染器高度

                    for (int i = 0; i < rowCount; i++)
                    {
                        for (int j = 0; j < columnCount; j++)
                        {
                            int index = i * columnCount + j;
                            int x = j * width;
                            int y = i * height;
                            SetRenderPointSize(renderers, index, x, y, width, height);
                        }
                    }

                    break;
            }
        }

        private void LayOutLinearHorizontal(Rectangle rectangle, List<VideoRenderer> renderers, int itemCount)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                int x = rectangle.Width / itemCount * i;
                int width = rectangle.Width / itemCount;
                SetRenderPointSize(
                    renderers,
                    i,
                    x,
                    rectangle.Y,
                    width,
                    rectangle.Height
                );
                renderers[i]._panel.Visible = true;
            }
        }

        private void LayOutLinearVertical(Rectangle rectangle, List<VideoRenderer> renderers, int itemCount)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                int y = rectangle.Height / itemCount * i;
                int height = rectangle.Height / itemCount;
                SetRenderPointSize(
                    renderers,
                    i,
                    rectangle.X,
                    y,
                    rectangle.Width,
                    height
                );
                renderers[i]._panel.Visible = true;
            }
        }

        private void SetRenderPointSize(
            List<VideoRenderer> renderers,
            int rendererIndex,
            int x,
            int y,
            int width,
            int height
        )
        {
            if (rendererIndex >= renderers.Count)
            {
                Log("| SetRenderPointSize(index:" + rendererIndex + ") failed,renderers count:" +
                    renderers.Count);
                return;
            }

            SetRenderPointSize(renderers[rendererIndex], x, y, width, height);
        }

        private void SetRenderPointSize(
            VideoRenderer renderer,
            int x,
            int y,
            int width,
            int height)
        {
            //设置尺寸位置
            if (renderer._isMaximize)
            {
                x = 0;
                y = 0;
                width = _container.Width;
                height = _container.Height;
            }

            renderer.SetSize(x, y, width, height);
            //绘制边框
            // renderers[rendererIndex].DrawBorder(false);
        }

        private void SyncViewPointSize(Connector connector, List<VideoRenderer> renderers)
        {
            foreach (VideoRenderer renderer in renderers)
            {
                if (renderer.IsInUse())
                {
                    connector.ShowViewAt(renderer._panel.Handle, 0, 0,
                        (uint)renderer._panel.Width,
                        (uint)renderer._panel.Height);
                    renderer.ShowTools();
                    Log("| {0} -> {1}",
                        renderer.GetFingerPrint(),
                        renderer._videoSource?.key);
                }
            }
        }

        private Rectangle GenerateLecturerRectangle(Rectangle bounds)
        {
            Rectangle lecturerBounds = new Rectangle();
            lecturerBounds.X = bounds.X;
            lecturerBounds.Y = bounds.Y;
            lecturerBounds.Width = bounds.Width;
            lecturerBounds.Height = (int)(bounds.Height * lectureModeLayoutHeightRatio);
            return lecturerBounds;
        }

        private Rectangle GenerateParticipantsRectangle(Rectangle bounds, Rectangle lecturerBounds)
        {
            Rectangle participantsBounds = new Rectangle();
            participantsBounds.X = bounds.X;
            participantsBounds.Y = lecturerBounds.Height;
            participantsBounds.Width = bounds.Width;
            participantsBounds.Height = (int)(bounds.Height * (1 - lectureModeLayoutHeightRatio));
            return participantsBounds;
        }

        /**
         * 判断主讲人区域是否需要PIP布局
         * 满足条件：只有两个画面 且 一主一辅
         */
        private bool IsNeedLecturerPip(List<VideoRenderer> renderers)
        {
            int sourceCount = 0;
            bool isMajorFounded = false;
            bool isMinorFounded = false;

            for (int i = 0; i < renderers.Count; i++)
            {
                VideoRenderer r = renderers[i];
                if (r.IsInUse())
                {
                    sourceCount += 1;
                    //判断主流是否存在
                    if (
                        null != r._videoSource &&
                        (
                            r._videoSource.IsRemoteCamera()
                            ||
                            r._videoSource.IsLocalCamera()
                        )
                    )
                    {
                        isMajorFounded = true;
                    }
                    //判断辅流是否存在
                    else if (
                        null != r._videoSource &&
                        (
                            r._videoSource.IsRemoteWindowShare()
                            ||
                            r._videoSource.IsLocalCamera2()
                            ||
                            r._videoSource.IsVirtualSource()
                        )
                    )
                    {
                        isMinorFounded = true;
                    }
                }
            }

            //当我们判定：有且仅有两个源且主辅流都存在时，说明需要PIP布局展示。
            return sourceCount == 2 && isMajorFounded && isMinorFounded;
        }

        private VideoRenderer FindLecturerCameraRenderer(List<VideoRenderer> renderers)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                VideoRenderer r = renderers[i];
                if (r.IsInUse())
                {
                    if (
                        r._videoSource.IsRemoteCamera()
                        ||
                        r._videoSource.IsLocalCamera()
                    )
                    {
                        return r;
                    }
                }
            }

            return null;
        }

        private VideoRenderer FindLecturerWindowShareRenderer(List<VideoRenderer> renderers)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                VideoRenderer r = renderers[i];
                if (r.IsInUse())
                {
                    if (
                        r._videoSource.IsRemoteWindowShare()
                        ||
                        r._videoSource.IsLocalCamera2()
                        ||
                        r._videoSource.IsVirtualSource()
                    )
                    {
                        return r;
                    }
                }
            }

            return null;
        }

        private bool IsFullScreenRendererExist()
        {
            foreach (VideoRenderer renderer in _renderers)
            {
                if (renderer._isMaximize)
                {
                    return true;
                }
            }

            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (renderer._isMaximize)
                {
                    return true;
                }
            }

            return null != pipRenderer && pipRenderer._isMaximize;
        }

        private bool IsPinRendererExist()
        {
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (renderer._isPin)
                {
                    return true;
                }
            }

            return false;
        }

        public void OnRequestRendererSizeChange(VideoRenderer renderer, bool isMaximize)
        {
            Log("OnRequestRendererSizeChange(container Id:{0} , renderer Id:{1} , is Maximize:{2})",
                id,
                renderer.id,
                isMaximize);
            renderer.SetMaximizedState(isMaximize);
            if (isMaximize) renderer._panel.BringToFront();
            RefreshLayout(_connector);
        }

        public void OnRequestPin(VideoRenderer renderer, bool isPin)
        {
            Log("OnRequestPin(container Id:{0} , renderer Id:{1} , is Pin:{2})",
                id,
                renderer.id,
                isPin);
            foreach (VideoRenderer r in _lecturerRenderers) //复原其他pin渲染器
            {
                r.SetPinState(false);
            }

            renderer.SetPinState(isPin); //设置用户指定pin渲染器
            RefreshLayout(_connector);
        }

        /**
         * 只有在pip布局时，会显示交换按钮
         */
        public void OnExchangeClick(VideoRenderer renderer)
        {
            if (IsNeedLecturerPip(_lecturerRenderers))
            {
                _isMajorBiggerInPIP = !_isMajorBiggerInPIP;
                RefreshLayout();
                // VideoRenderer cameraRenderer = FindLecturerCameraRenderer(_lecturerRenderers);
                // VideoRenderer shareRenderer = FindLecturerWindowShareRenderer(_lecturerRenderers);
                // cameraRenderer.SwapStreams(_connector, shareRenderer);
            }
        }

        public void StopRenderingAll(Connector connector)
        {
            //stop participant's renderer
            foreach (VideoRenderer renderer in _renderers)
            {
                renderer.StopRendering(connector);
            }

            //stop lecturer's renderer
            StopRenderingLecturerAll(connector);

            //stop pip renderer
            if (null != pipRenderer && pipRenderer.IsInUse())
            {
                pipRenderer.StopRendering(connector);
            }
        }

        public void StopRenderingLecturerAll(Connector connector)
        {
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                renderer.StopRendering(connector);
            }
        }

        public void StopRenderingLecturerMinorStream(Connector connector)
        {
            foreach (VideoRenderer renderer in _lecturerRenderers)
            {
                if (null != renderer && renderer.IsInUse())
                {
                    VideoSource videoSource = renderer._videoSource;
                    if (null != videoSource)
                    {
                        if (!videoSource.IsRemoteCamera()) renderer.StopRendering(connector);
                    }
                    else
                    {
                        Log("Error! renderer is in used,but video source is null.");
                    }
                }
            }
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[VideoRendererContainer] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[VideoRendererContainer] " + content, args);
        }

        private bool ENABLE_BOUNDS_LOG = false;

        private void LogBounds(string content, params object[] args)
        {
            if (ENABLE_BOUNDS_LOG) Log(content, args);
        }
    }
}