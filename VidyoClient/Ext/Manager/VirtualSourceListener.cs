using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using VideoClient.Manager;
using VideoClient.UI;
using VideoClient.Util;
using VideoClient.VidyoClient.Ext.Manager;

namespace VidyoClient
{
    public class VirtualSourceListener : Connector.IRegisterVirtualVideoSourceEventListener
    {
        public Dictionary<string, VirtualVideoSource> VirtualSources = new Dictionary<string, VirtualVideoSource>();

        public static String VirtualVideoSourceName = "Virtual Share";

        public VirtualVideoSource mVirtualCamera, mVirtualShare;

        private bool mVirtualCameraStarted;
        public bool mVirtualShareStarted;

        private int mVirtualCameraFrameInterval = 1000000000;
        private int mVirtualShareFrameInterval = 1000000000;

        CameraConfig _cameraConfig;

        private int s = 0, c = 0;

        public VirtualSourceListener()
        {
            _cameraConfig = new CameraConfig(
                2,
                Config.GetStringConfig("camera_3_name"),
                Config.GetIntegerConfig("camera_3_position_of_screen"),
                Config.GetIntegerConfig("camera_3_position_of_renderer"),
                "关闭",
                Config.GetStringConfig("camera_3_last_selected_device_name"),
                1920,
                1080,
                "1080p",
                30,
                (uint) Config.GetIntegerConfig("SetTargetBitRate_3"));
        }

        public string GetVirtualVideoSourceName()
        {
            return VirtualVideoSourceName;
        }

        public VirtualVideoSource.VirtualVideoSourceType GetVirtualVideoSourceType()
        {
            return VirtualVideoSource.VirtualVideoSourceType.VirtualvideosourcetypeSHARE;
        }

        public VirtualVideoSource GetVirtualVideoSource()
        {
            return VirtualSources[VirtualVideoSourceName];
        }

        public bool IsReady()
        {
            return null != mVirtualShare;
        }

        public bool IsStarted()
        {
            return mVirtualShareStarted;
        }

        public Boolean SendFrameWithExternalData(MediaFormat format, byte[] buffer, SizeT size, SizeT width,
            SizeT height)
        {
            return mVirtualShare.SendFrameWithExternalData(format, buffer, size, width, height);
        }

        public void OnVirtualVideoSourceAdded(VirtualVideoSource virtualVideoSource)
        {
            AddVirtualVideoSource(virtualVideoSource);
        }

        public void OnVirtualVideoSourceRemoved(VirtualVideoSource virtualVideoSource)
        {
            RemoveVirtualVideoSource(virtualVideoSource);
        }

        public void OnVirtualVideoSourceStateUpdated(VirtualVideoSource virtualVideoSource, Device.DeviceState state)
        {
            StateUpdetedVirtualVideoSource(virtualVideoSource, state);
        }

        public void OnVirtualVideoSourceExternalMediaBufferReleased(VirtualVideoSource virtualVideoSource,
            byte[] buffer, SizeT size)
        {
        }

        private void AddVirtualVideoSource(VirtualVideoSource virtualsource)
        {
            VirtualSources.Add(virtualsource.GetName(), virtualsource);
        }

        private void RemoveVirtualVideoSource(VirtualVideoSource virtualsource)
        {
            VirtualSources.Remove(virtualsource.GetName());
        }

        public void StateUpdetedVirtualVideoSource(VirtualVideoSource virtualsource, Device.DeviceState state)
        {
            switch (state)
            {
                case Device.DeviceState.DevicestateStarted:
                    if (virtualsource.GetType() ==
                        VirtualVideoSource.VirtualVideoSourceType.VirtualvideosourcetypeCAMERA)
                    {
                        mVirtualCameraStarted = true;
                        mVirtualCamera = virtualsource;
                        mVirtualCameraFrameInterval = (int) virtualsource.GetCurrentEncodeFrameInterval();
                        if (!ThirdCameraManager.useThirdCameraShare) Task.Run(() => { FeedingCameraFrames(); });
                    }
                    else
                    {
                        mVirtualShareStarted = true;
                        mVirtualShare = virtualsource;

                        //mVirtualShare.SetMinFrameInterval(1000/thirdFramerate);
                        mVirtualShare.SetLowLatencyProfile(true);

                        mVirtualShare.SetBoundsConstraints(
                            1000000000 / _cameraConfig.framerate,
                            1000000000 / _cameraConfig.framerate,
                            _cameraConfig.width, _cameraConfig.width,
                            _cameraConfig.height, _cameraConfig.height);

                        mVirtualShareFrameInterval = (int) virtualsource.GetCurrentEncodeFrameInterval();
                        if (!ThirdCameraManager.useThirdCameraShare) Task.Run(() => { FeedingShareFrames(); });
                    }

                    Log("Started Type:{0}", virtualsource.GetType());
                    break;
                case Device.DeviceState.DevicestateStopped:
                    if (virtualsource.GetType() ==
                        VirtualVideoSource.VirtualVideoSourceType.VirtualvideosourcetypeCAMERA)
                    {
                        mVirtualCameraStarted = false;
                        mVirtualCamera = null;
                    }
                    else
                    {
                        mVirtualShareStarted = false;
                        mVirtualShare = null;
                    }

                    Log("Stopped Type:{0}", virtualsource.GetType());
                    break;
                case Device.DeviceState.DevicestateConfigurationChanged:
                    if (virtualsource.GetType() ==
                        VirtualVideoSource.VirtualVideoSourceType.VirtualvideosourcetypeCAMERA)
                    {
                        mVirtualCameraFrameInterval = (int) virtualsource.GetCurrentEncodeFrameInterval();
                    }
                    else
                    {
                        mVirtualShareFrameInterval = (int) virtualsource.GetCurrentEncodeFrameInterval();
                    }

                    Log("ConfigurationChanged Type:{0} , FrameInterval:{1}", virtualsource.GetType(),
                        mVirtualShareFrameInterval);
                    break;
                default:
                    break;
            }
        }

        private void FillBuffer(byte[] buffer, int width, int height, bool isShare)
        {
            int i = isShare ? s : c;
            int d = isShare ? 1 : 3;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    buffer[y * width + x] = (byte) (x * d + y + i);
                }
            }

            for (int y = 0; y < height / 2; y++)
            {
                for (int x = 0; x < width / 2; x++)
                {
                    buffer[y * width / 2 + x + height * width] = (byte) (64 + y + i * 3);
                    buffer[y * width / 2 + x + height * width] = (byte) (128 + x * d + i * 5);
                }
            }

            if (isShare)
                s++;
            else
                c++;
        }

        private void FeedingCameraFrames()
        {
            while (mVirtualCameraStarted)
            {
                //Thread.Sleep(mVirtualCameraFrameInterval / 1000000);
                Thread.Sleep(1000 / 15);
                if (mVirtualCamera == null)
                    continue;
                int width = 640;
                int height = 360;
                int size = (int) (width * height * 1.5);
                byte[] buffer = new byte[size];

                FillBuffer(buffer, width, height, false);
                if (null != mVirtualCamera)
                    mVirtualCamera.SendFrameWithExternalData(MediaFormat.MediaformatI420, buffer, (ulong) buffer.Length,
                        (ulong) width, (ulong) height);
            }
        }

        private void FeedingShareFrames()
        {
            while (mVirtualShareStarted)
            {
                //Thread.Sleep(mVirtualCameraFrameInterval / 15000000);
                Thread.Sleep(1000 / 15);
                if (mVirtualShare == null)
                    continue;
                int width = 640;
                int height = 360;
                int size = (int) (width * height * 1.5);
                byte[] buffer = new byte[size];

                FillBuffer(buffer, width, height, true);
                mVirtualShare.SendFrameWithExternalData(MediaFormat.MediaformatI420, buffer, (ulong) buffer.Length,
                    (ulong) width, (ulong) height);
            }
        }

        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[VirtualSourceListener] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[VirtualSourceListener] " + content, args);
        }
    }
}