using System.Collections.Generic;

namespace VidyoClient
{
    public class LayoutControllerConfig
    {
        public bool UseDefaultLayout { get; set; }
        public bool EnableEndpointMode { get; set; } //是否启用终端模式
        public bool EnableGuestMode { get; set; } //是否启用来宾模式
        public string EndpointModeWindowMode { get; set; } //normal、fullscreen
        public int EndpointModeWindowNumber { get; set; } //0表示匹配显示器数量、或指定数量
        public bool EndpointModeCameraPin { get; set; } //是否启用
        public int EndpointModeCameraPinWindowIndex { get; set; } //固定主摄像头到指定屏幕，0表示第一个

        public bool DisplayGuestVideo { get; set; } //是否显示来宾用户画面
        public bool DisplayGatewayVideo { get; set; } //是否显示第三方终端画面
        public bool DisplaySelfViewInMeeting { get; set; } //是否在会中时显示自视画面
        public bool DisplaySelfViewPip { get; set; } //是否显示自视
        public bool DisplayZoomBtnOnLocalRenderer { get; set; } //是否在本地渲染器显示缩放按钮
        public bool DisplayZoomBtnOnRemoteRenderer { get; set; } //是否在普通渲染器显示缩放按钮
        public bool DisplayZoomBtnOnLecturerRenderer { get; set; } //是否在主讲人渲染器显示缩放按钮
        public bool DisplaySnapshotBtn { get; set; } //
        public bool AllowCameraCopyRendering { get; set; } //
        public int RemoteParticipants { get; set; } //

        public bool AutoAssignRenderer { get; set; } //自动指定远端源到空闲显示器

        public List<int> ForceLayoutNum = new List<int>();

        // public int Screen1ForceLayoutNum { get; set; }
        // public int Screen2ForceLayoutNum { get; set; }
        // public int Screen3ForceLayoutNum { get; set; }
        // public int Screen4ForceLayoutNum { get; set; }
        public int VoiceActivatedPositionOfScreens { get; set; } //
        public int VoiceActivatedPositionOfRenderers { get; set; } //
    }
}