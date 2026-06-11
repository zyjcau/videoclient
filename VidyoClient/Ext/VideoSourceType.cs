using System;
using Newtonsoft.Json.Serialization;

namespace VidyoClient.Ext
{
    public class VideoSourceType
    {
        //视频源类型，记录在Renderer中
        public static readonly string TYPE_NONE = "none";
        public const string TYPE_LOCAL_CAMERA = "local_camera";
        public const string TYPE_LOCAL_CAMERA2 = "local_camera2";
        public const string TYPE_VIRTUAL_SOURCE = "local_virtualsource";
        public const string TYPE_REMOTE_CAMERA = "remote_camera";
        public const string TYPE_REMOTE_SHARE = "remote_share";

        public static readonly int TYPE_NONE_INT = 0;
        public const int TYPE_LOCAL_CAMERA_INT = 1;
        public const int TYPE_LOCAL_CAMERA2_INT = 2;
        public const int TYPE_VIRTUAL_SOURCE_INT = 3;
        public const int TYPE_LOCAL_MONITOR_INT = 4;
        public const int TYPE_LOCAL_WINDOW_INT = 5;
        public const int TYPE_REMOTE_CAMERA_INT = 6;
        public const int TYPE_REMOTE_SHARE_INT = 7;

        public static bool Is(String target, String type)
        {
            return String.Equals(target, type);
        }
    }
}