using System;
using System.Collections.Generic;

namespace VidyoClient
{
    public class CameraConfig
    {
        public int cameraIndex;
        public string previewLabel = "本地视频源";
        public string inUse = "关闭";
        public string lastSelectedDeviceName;
        public uint width = 1920;
        public uint height = 1080;
        public string resolution = "1080p";
        public uint framerate = 30;

        public uint bitrate = 8 * 1024 * 1024;

        // public bool isClosed = true;
        public int positionOfScreen;
        public int positionOfRenderer;

        public int resolutionProfile =
            Convert.ToInt32(LocalCamera.LocalCameraTradeOffProfile.LocalcameratradeoffprofileHigh);

        public int framerateProfile =
            Convert.ToInt32(LocalCamera.LocalCameraTradeOffProfile.LocalcameratradeoffprofileHigh);

        public bool isMirroring;

        private Dictionary<string, uint[]> _resolutions = new Dictionary<string, uint[]>()
        {
            { "auto", new uint[] { 1280, 720 } },
            { "480p", new uint[] { 720, 480 } },
            { "540p", new uint[] { 960, 540 } },
            { "720p", new uint[] { 1280, 720 } },
            { "1080p", new uint[] { 1920, 1080 } },
            { "2K", new uint[] { 2560, 1440 } },
            { "4K", new uint[] { 3840, 2160 } }
        };

        public CameraConfig(
            int cameraIndex,
            string previewLabel,
            int positionOfScreen,
            int positionOfRenderer,
            string inUse,
            string lastSelectedDeviceName,
            uint width,
            uint height,
            string resolution,
            uint framerate,
            uint bitrate)
        {
            this.cameraIndex = cameraIndex;
            this.previewLabel = previewLabel;
            this.positionOfScreen = positionOfScreen;
            this.positionOfRenderer = positionOfRenderer;
            this.inUse = inUse;
            this.lastSelectedDeviceName = lastSelectedDeviceName;
            this.width = width;
            this.height = height;
            this.resolution = resolution;
            this.framerate = framerate;
            this.bitrate = bitrate;
        }

        public CameraConfig(
            int cameraIndex,
            string previewLabel,
            int positionOfScreen,
            int positionOfRenderer,
            string inUse,
            string lastSelectedDeviceName,
            string resolutionKey,
            uint framerate,
            uint bitrate)
        {
            this.cameraIndex = cameraIndex;
            this.previewLabel = previewLabel;
            this.positionOfScreen = positionOfScreen;
            this.positionOfRenderer = positionOfRenderer;
            this.inUse = inUse;
            this.lastSelectedDeviceName = lastSelectedDeviceName;

            uint[] res = ParseResolution(resolutionKey);
            this.width = res[0];
            this.height = res[1];
            this.resolution = resolutionKey;
            this.framerate = framerate;
            this.bitrate = bitrate;
        }

        public uint[] ParseResolution(String resolutionKey)
        {
            if (_resolutions.ContainsKey(resolutionKey))
            {
                return new[] { _resolutions[resolutionKey][0], _resolutions[resolutionKey][1] };
            }

            return new uint[] { 1280, 720 };
        }
    }
}