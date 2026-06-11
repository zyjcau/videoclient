using NLog;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext;

namespace VidyoClient.Ext
{
    public class VideoSource
    {
        public static readonly string SOURCE_KEY_NO_DISPLAY = "0-no_display";

        public string key;
        public string type;
        public string label; //display label from p name
        public string name; //device name
        public VideoParticipant from;

        public bool isMirroring { get; set; }
        public int positionOfScreen { get; set; }

        public int positionOfRendererContainer { get; set; }

        public bool isRendering { get; set; }
        private AtomicBoolean inUsed = new AtomicBoolean(false);
        public int width { get; set; }
        public int height { get; set; }
        public int frameInterval { get; set; }

        public LocalCamera localCamera;
        public VirtualVideoSource virtualVideoSource;
        public RemoteCamera remoteCamera;
        public RemoteWindowShare remoteWindowShare;

        public VideoSource(LocalCamera localCamera, string label, bool isMainCamera, int positionOfScreen,
            int positionOfRendererContainer)
        {
            this.localCamera = localCamera;
            type = isMainCamera ? VideoSourceType.TYPE_LOCAL_CAMERA : VideoSourceType.TYPE_LOCAL_CAMERA2;
            key = GetSourceKey();
            this.label = label;
            name = localCamera.GetName();

            this.positionOfScreen = positionOfScreen;
            this.positionOfRendererContainer = positionOfRendererContainer;
        }

        public VideoSource(VirtualVideoSource virtualVideoSource, string label, int positionOfScreen,
            int positionOfRendererContainer)
        {
            this.virtualVideoSource = virtualVideoSource;
            type = VideoSourceType.TYPE_VIRTUAL_SOURCE;
            key = GetSourceKey();
            this.label = label;
            name = virtualVideoSource.GetName();

            this.positionOfScreen = positionOfScreen;
            this.positionOfRendererContainer = positionOfRendererContainer;
        }

        public VideoSource(RemoteCamera remoteCamera, int positionOfScreen,
            int positionOfRendererContainer)
        {
            this.remoteCamera = remoteCamera;
            type = VideoSourceType.TYPE_REMOTE_CAMERA;
            key = GetSourceKey();
            label = remoteCamera.GetName();
            name = remoteCamera.GetName();

            this.positionOfScreen = positionOfScreen;
            this.positionOfRendererContainer = positionOfRendererContainer;
        }

        public VideoSource(RemoteWindowShare remoteWindowShare, int positionOfScreen,
            int positionOfRendererContainer)
        {
            this.remoteWindowShare = remoteWindowShare;
            type = VideoSourceType.TYPE_REMOTE_SHARE;
            key = GetSourceKey();
            label = remoteWindowShare.GetName();
            name = remoteWindowShare.GetName();

            this.positionOfScreen = positionOfScreen;
            this.positionOfRendererContainer = positionOfRendererContainer;
        }

        public bool RequestUse()
        {
            if (!IsInUse() && inUsed.CompareAndSet(false, true))
            {
                return true;
            }

            return false;
        }

        public bool IsInUse()
        {
            return inUsed.Get();
        }

        public void Release()
        {
            inUsed.Set(false);
        }

        public static string GetSourceKey(LocalCamera camera, bool isMainCamera)
        {
            return camera.GetId() + "-" + camera.GetName() + "$" +
                   (isMainCamera ? VideoSourceType.TYPE_LOCAL_CAMERA : VideoSourceType.TYPE_LOCAL_CAMERA2);
        }

        public static string GetSourceKey(RemoteCamera camera)
        {
            return camera.GetId() + "-" + camera.GetName() + "$" + VideoSourceType.TYPE_REMOTE_CAMERA;
        }

        public static string GetSourceKey(RemoteWindowShare windowShare)
        {
            return windowShare.GetId() + "-" + windowShare.GetName() + "$" +
                   VideoSourceType.TYPE_REMOTE_SHARE;
        }

        public static string GetSourceKey(VirtualVideoSource virtualVideo)
        {
            return virtualVideo.GetId() + "-" + virtualVideo.GetName() +
                   "$" + VideoSourceType.TYPE_VIRTUAL_SOURCE;
        }

        public string GetSourceKey()
        {
            if (null != key)
            {
                return key;
            }

            if (null == type)
            {
                return "Unknown";
            }

            if (VideoSourceType.TYPE_LOCAL_CAMERA.Equals(type))
            {
                return GetSourceKey(localCamera, true);
            }

            if (VideoSourceType.TYPE_LOCAL_CAMERA2.Equals(type))
            {
                return GetSourceKey(localCamera, false);
            }

            if (VideoSourceType.TYPE_VIRTUAL_SOURCE.Equals(type))
            {
                return GetSourceKey(virtualVideoSource);
            }

            if (VideoSourceType.TYPE_REMOTE_CAMERA.Equals(type))
            {
                return GetSourceKey(remoteCamera);
            }

            if (VideoSourceType.TYPE_REMOTE_SHARE.Equals(type))
            {
                return GetSourceKey(remoteWindowShare);
            }
            else
            {
                return "Unknown";
            }
        }

        public void SetFrom(VideoParticipant videoParticipant)
        {
            this.from = videoParticipant;
            this.label = videoParticipant.name;
        }

        public bool IsLocalCamera()
        {
            return VideoSourceType.TYPE_LOCAL_CAMERA.Equals(type);
        }

        public bool IsLocalCamera2()
        {
            return VideoSourceType.TYPE_LOCAL_CAMERA2.Equals(type);
        }

        public bool IsVirtualSource()
        {
            return VideoSourceType.TYPE_VIRTUAL_SOURCE.Equals(type);
        }

        public bool IsRemoteCamera()
        {
            return VideoSourceType.TYPE_REMOTE_CAMERA.Equals(type);
        }

        public bool IsRemoteWindowShare()
        {
            return VideoSourceType.TYPE_REMOTE_SHARE.Equals(type);
        }

        public bool RegisterFrameEventListener(Connector connector, VideoFrameListener listener)
        {
            bool isCompleted = true;
            if (IsRemoteCamera())
            {
                remoteCamera.RegisterFrameEventListener(listener);
            }
            else if (IsRemoteWindowShare())
            {
                remoteWindowShare.RegisterFrameEventListener(listener);
            }
            else if (IsLocalCamera())
            {
                connector.RegisterLocalCameraFrameListener(
                    listener,
                    localCamera,
                    (uint) width, (uint) height, (ulong) frameInterval);
            }
            else if (IsLocalCamera2())
            {
                connector.RegisterLocalCameraFrameListener(
                    listener,
                    localCamera,
                    (uint) width, (uint) height, (ulong) frameInterval);
            }
            else
            {
                isCompleted = false;
            }

            Log($"RegisterFrameEventListener -> {type}, isCompleted: {isCompleted}");
            return isCompleted;
        }

        public bool UnregisterFrameEventListener(Connector connector)
        {
            bool isCompleted = true;
            if (IsRemoteCamera())
            {
                remoteCamera.UnregisterFrameEventListener();
            }
            else if (IsRemoteWindowShare())
            {
                remoteWindowShare.UnregisterFrameEventListener();
            }
            else if (IsLocalCamera())
            {
                connector.UnregisterLocalCameraFrameListener(localCamera);
            }
            else if (IsLocalCamera2())
            {
                connector.UnregisterLocalCameraFrameListener(localCamera);
            }
            else
            {
                isCompleted = false;
            }

            Log($"UnregisterFrameEventListener -> {type}, isCompleted: {isCompleted}");
            return isCompleted;
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[VideoSource] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[VideoSource] " + content, args);
        }
    }
}