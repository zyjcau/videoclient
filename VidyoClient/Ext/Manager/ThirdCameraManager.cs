using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge.Video;
using AForge.Video.DirectShow;
using NLog;
using VideoClient.Manager;
using VideoClient.Util;
using VidyoClient;
using VidyoClient.Ext;

namespace VideoClient.VidyoClient.Ext.Manager
{
    public class ThirdCameraManager
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        delegate void StringArgReturningVoidDelegate(string text);

        private Connector _connector;
        private VideoManager _videoManager;
        private VirtualSourceListener _virtualSourceListener;

        private LayoutController _layoutController;

        //-------------------------第三路摄像头，基于AForge-------------------------------
        public CameraConfig _cameraConfig;

        // private VideoSourcePlayer _videoSourcePlayer;
        FilterInfoCollection _videoDevices;

        // public Dictionary<string, FilterInfo> third_cameras = new Dictionary<string, FilterInfo>();
        VideoCaptureDevice _videoCapturer; //捕获设备源
        public static bool useThirdCameraShare; //设为false则发送测试图像

        public Dictionary<string, uint[]> resolutions = new Dictionary<string, uint[]>();
        public Dictionary<string, uint> framerates = new Dictionary<string, uint>();

        byte[] newFrame = null;
        long streamLen = 0;

        private bool showSharePreview = true;
        private bool _flip;

        private const string DEVICE_NONE = "关闭";
        private const string DEVICE_AUTO = "auto";

        public ThirdCameraManager(VideoManager videoManager)
        {
            _videoManager = videoManager;
            _connector = _videoManager.connector;
            _virtualSourceListener = _videoManager.virtualSourceListener;
            // _videoSourcePlayer = videoSourcePlayer;

            _flip = Config.GetBooleanConfig("camera_3_flip");

            _cameraConfig = new CameraConfig(
                2,
                Config.GetStringConfig("camera_3_name"),
                Config.GetIntegerConfig("camera_3_position_of_screen"),
                Config.GetIntegerConfig("camera_3_position_of_renderer"),
                DEVICE_NONE,
                Config.GetStringConfig("camera_3_last_selected_device_name"),
                Config.GetStringConfig("camera_3_resolution"),
                (uint)Config.GetIntegerConfig("camera_3_framerate"),
                (uint)Config.GetIntegerConfig("SetTargetBitRate_3"));

            framerates.Add("5", 5);
            framerates.Add("10", 10);
            framerates.Add("15", 15);
            framerates.Add("25", 25);
            framerates.Add("30", 30);
            // framerates.Add("40", 40);
            framerates.Add("50", 50);
            framerates.Add("60", 60);

            resolutions.Add("auto", new uint[] { 1280, 720 });
            resolutions.Add("480p", new uint[] { 720, 480 });
            resolutions.Add("540p", new uint[] { 960, 540 });
            resolutions.Add("720p", new uint[] { 1280, 720 });
            resolutions.Add("1080p", new uint[] { 1920, 1080 });
            resolutions.Add("2K", new uint[] { 2560, 1440 });
            resolutions.Add("4K", new uint[] { 3840, 2160 });

            _connector.CreateVirtualVideoSource(
                _virtualSourceListener.GetVirtualVideoSourceType(),
                "camera_3",
                _virtualSourceListener.GetVirtualVideoSourceName());

            // InitCameras();
        }

        public LayoutController GetLayoutController()
        {
            if (_layoutController == null)
            {
                _layoutController = _videoManager.layoutController;
            }

            return _layoutController;
        }

        // public void InitCameras()
        // {
        //     Log("InitCameras for VirtualSource...");
        //     _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        //
        //     third_cameras.Clear();
        //
        //     third_cameras.Add(DEVICE_NONE, null);
        //
        //     for (int d = 0; d < _videoDevices.Count; d++)
        //     {
        //         FilterInfo info = _videoDevices[d];
        //
        //         if (info.MonikerString.IndexOf("#") > 0)
        //         {
        //             int len = info.MonikerString.LastIndexOf("#") - info.MonikerString.IndexOf("#");
        //             string camId = info.MonikerString.Substring(info.MonikerString.IndexOf("#"), len);
        //             third_cameras.Add(info.Name + CameraCustomIdSeparator + camId, info);
        //         }
        //         else if (info.MonikerString.IndexOf("{") > 0)
        //         {
        //             int len = info.MonikerString.IndexOf("}") - info.MonikerString.IndexOf("{");
        //             string camId = info.MonikerString.Substring(info.MonikerString.IndexOf("{"), len);
        //             third_cameras.Add(info.Name + CameraCustomIdSeparator + camId, info);
        //         }
        //     }
        // }

        public bool SetCam3Framerate(string framerateKey)
        {
            _cameraConfig.framerate = framerates[framerateKey];
            _logger.Info("select 3 camera framerate " + _cameraConfig.framerate);
            return true;
        }

        public bool SetCam3Resolution(string resolutionKey)
        {
            _cameraConfig.resolution = resolutionKey;
            _cameraConfig.width = resolutions[resolutionKey][0];
            _cameraConfig.height = resolutions[resolutionKey][1];
            _logger.Info("select camera 3 resolution " + _cameraConfig.width + "x" + _cameraConfig.height);
            return true;
        }

        public void SaveLastSelectedCamera(string cameraName)
        {
            _cameraConfig.lastSelectedDeviceName = cameraName;
        }

        public void SaveCam3Resolution(string resolutionKey)
        {
            Config.SetStringConfig("camera_3_resolution", resolutionKey);
        }

        public void SaveCam3Framerate(string framerateKey)
        {
            Config.SetStringConfig("camera_3_framerate", framerateKey);
        }

        public void AssignLastSelectedCamera()
        {
            if (String.IsNullOrEmpty(_cameraConfig.lastSelectedDeviceName)) return;

            if (String.Equals(DEVICE_AUTO, _cameraConfig.lastSelectedDeviceName))
            {
                foreach (KeyValuePair<string, LocalCamera> pair in GetCameras())
                {
                    string cameraName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, cameraName))
                    {
                        AssignThirdCamera(cameraName);
                        return;
                    }
                }
            }
            else if (String.Equals(DEVICE_NONE, _cameraConfig.lastSelectedDeviceName))
            {
                return;
            }
            else if (GetCameras().ContainsKey(_cameraConfig.lastSelectedDeviceName))
            {
                AssignThirdCamera(_cameraConfig.lastSelectedDeviceName);
            }
            else
            {
                foreach (KeyValuePair<string, LocalCamera> pair in GetCameras())
                {
                    string cameraName = pair.Key;
                    if (!String.Equals(DEVICE_NONE, cameraName))
                    {
                        AssignThirdCamera(cameraName);
                        return;
                    }
                }
            }
        }

        private Dictionary<string, LocalCamera> GetCameras()
        {
            return _videoManager.cameraManager.cameras;
        }

        // private bool _UseAforgeRendering = false; //实验性功能，默认关闭

        public void AssignThirdCamera(string cameraName)
        {
            _videoManager.RunOnUIThread((Action)(() =>
            {
                useThirdCameraShare = true;

                ShutThirdCamera();

                //connector.SelectVideoContentShare(cameras[cameraItem.Text]);
                if (DEVICE_NONE.Equals(cameraName) && _videoManager.isConnected)
                {
                    // if (_UseAforgeRendering)
                    // {
                    //     _videoSourcePlayer?.Stop();
                    // }
                    // else
                    // {
                    _videoCapturer?.SignalToStop();
                    _videoCapturer?.WaitForStop();

                    if (!GetLayoutController().IsUseDefaultLayout())
                    {
                        GetLayoutController()
                            .StopRenderingLocalVirtualSource(_virtualSourceListener.GetVirtualVideoSource());
                    }

                    _connector.SelectVirtualSourceWindowShare(null);
                    // }

                    _cameraConfig.inUse = cameraName;

                    _logger.Info("close camera 3");
                    return;
                }

                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (null == videoDevices) return;
                for (int i = 0; i < videoDevices.Count; i++)
                {
                    FilterInfo info = videoDevices[i];

                    String[] split = cameraName.Split(new char[] { CameraListener.CameraCustomIdSeparator });
                    if (split.Length > 1 &&
                        info.MonikerString.Contains(split[1]))
                    {
                        _videoCapturer = new VideoCaptureDevice(info.MonikerString);
                        //log("support list : ");

                        for (int j = 0; j < _videoCapturer.VideoCapabilities.Length; j++)
                        {
                            Size size = _videoCapturer.VideoCapabilities[j].FrameSize;
                            //log(size.Width+" x "+ size.Height);
                            if (size.Width == _cameraConfig.width && size.Height == _cameraConfig.height)
                            {
                                _videoCapturer.VideoResolution = _videoCapturer.VideoCapabilities[j];
                                _logger.Info("found camera 3 resolution -> " + _cameraConfig.width + " x " +
                                             _cameraConfig.height);
                            }
                        }

                        if (_videoCapturer.VideoResolution == null)
                        {
                            _videoCapturer.VideoResolution = _videoCapturer.VideoCapabilities[0];
                            _logger.Info("not found camera 3 resolution " + _cameraConfig.height +
                                         "p , then set to default.");
                        }

                        // if (_UseAforgeRendering)
                        // {
                        //     _videoSourcePlayer.VideoSource = _videoCapturer;
                        //     _videoSourcePlayer.Location = new Point(10, 0);
                        //     _videoSourcePlayer.Size = new Size(640, 360);
                        //     _videoSourcePlayer.BringToFront();
                        //     _videoSourcePlayer.Start();
                        // }
                        // else
                        // {
                        _videoCapturer.NewFrame += ThirdCameraNewFrame;
                        _videoCapturer.Start();
                        // }

                        // _cameraConfig.isClosed = false;
                        _cameraConfig.inUse = cameraName;

                        // if (!_UseAforgeRendering && _videoManager.isConnected)
                        // if (!_UseAforgeRendering)
                        // {
                        VirtualVideoSource virtualSource = _virtualSourceListener.GetVirtualVideoSource();
                        virtualSource.SetLowLatencyProfile(true);
                        virtualSource
                            .SetPreviewLabel(_cameraConfig.previewLabel);
                        _connector.SelectVirtualSourceWindowShare(
                            virtualSource);

                        if (!GetLayoutController().IsUseDefaultLayout())
                        {
                            _logger.Info("start AssignLocalVirtualSource");
                            GetLayoutController().StartRenderingLocalVirtualSource(
                                virtualSource,
                                _cameraConfig.previewLabel,
                                _cameraConfig.positionOfScreen,
                                _cameraConfig.positionOfRenderer);
                        }
                        else
                        {
                            _connector.ShowWindowSharePreview(showSharePreview);
                        }

                        _logger.Info("start camera 3 success");
                        // }
                        // else
                        // {
                        //     _cameraConfig.inUse = DEVICE_NONE;
                        //     _logger.Info("start camera 3 cannot success,because has not in meeting");
                        // }
                    }
                }
            }));
            // if (_videoSourcePlayer.InvokeRequired)
            // {
            //     StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(AssignThirdCamera);
            //     _videoSourcePlayer.Invoke(d, new object[] { cameraName });
            // }
            // else
            // {
            // }
        }

        public void Close()
        {
            if (IsOpened()) AssignThirdCamera(DEVICE_NONE);
        }

        public bool IsOpened()
        {
            return !String.Equals(_cameraConfig.inUse, DEVICE_NONE);
        }

        private void ShutThirdCamera()
        {
            // if (_UseAforgeRendering)
            // {
            //     if (_videoSourcePlayer.VideoSource != null)
            //     {
            //         _videoSourcePlayer.SignalToStop();
            //         _videoSourcePlayer.WaitForStop();
            //         _videoSourcePlayer.VideoSource = null;
            //     }
            // }
            // else
            // {
            _videoCapturer?.SignalToStop();
            _videoCapturer?.WaitForStop();

            if (!GetLayoutController().IsUseDefaultLayout())
            {
                GetLayoutController()
                    .StopRenderingLocalVirtualSource(_virtualSourceListener.GetVirtualVideoSource());
            }

            _connector.SelectVirtualSourceWindowShare(null);
            // }

            // _cameraConfig.isClosed = true;
        }

        long lastFrameTime = 0;

        private void ThirdCameraNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (Util.Util.GetTimeStamp() - lastFrameTime < (1000 / _cameraConfig.framerate))
            {
                return;
            }

            lastFrameTime = Util.Util.GetTimeStamp();

            ulong width = (ulong)eventArgs.Frame.Width;
            ulong height = (ulong)eventArgs.Frame.Height;

            if (_videoManager.isConnected &&
                _virtualSourceListener.IsReady() &&
                _virtualSourceListener.IsStarted())
            {
                // Log("bmp: pixelformat->{0},imageformat->{1}", eventArgs.Frame.PixelFormat, eventArgs.Frame.RawFormat);
                // Bitmap bitmap = AForge.Imaging.Image.Clone(eventArgs.Frame, PixelFormat.Format);//ImageFormat : memoryBMP , PixelFormat : Format24bppRgb

                // byte[] bs = Bitmap2Byte(eventArgs.Frame);
                // bitmap.Dispose();

                if (_flip)
                {
                    eventArgs.Frame.RotateFlip(RotateFlipType.RotateNoneFlipY); //耗时，会造成一定的延迟
                    byte[] bs = Bitmap2Byte(eventArgs.Frame);
                    // byte[] bs = flipY(newFrame, eventArgs.Frame.Width * 3);

                    _virtualSourceListener.SendFrameWithExternalData(
                        MediaFormat.Mediaformat24BG,
                        bs,
                        (ulong)bs.Length,
                        width,
                        height);
                }
                else
                {
                    byte[] bs = Bitmap2Byte(eventArgs.Frame);
                    // byte[] bs = Bitmap2Byte(bitmap);
                    _virtualSourceListener.SendFrameWithExternalData(
                        MediaFormat.MediaformatJPEG,
                        bs,
                        (ulong)bs.Length,
                        width,
                        height);
                }

                eventArgs.Frame.Dispose();
            }
        }

        private byte[] Bitmap2Byte(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                // if (streamLen != stream.Length || null == newFrame)
                // {
                //     newFrame = new byte[streamLen = stream.Length];
                // }
                //
                // stream.Seek(0, SeekOrigin.Begin);
                // stream.Read(newFrame, 0, Convert.ToInt32(stream.Length));
                // return newFrame;
                byte[] buffer = stream.GetBuffer();
                stream.Close();
                return buffer;
            }
        }

        public byte[] flipY(byte[] raw, int width)
        {
            int headLen = 54;

            byte[] flippedBits = new byte[raw.Length];

            for (int i = 0; i < headLen; ++i)
            {
                flippedBits[i] = raw[i];
            }

            for (int i = headLen, j = raw.Length - width; i < raw.Length - headLen; i += width, j -= width)
            {
                for (int k = 0; k < width; ++k)
                {
                    flippedBits[i + k] = raw[j + k];
                }
            }

            return flippedBits;
        }

        private void Log(string content)
        {
            _logger.Info("[ThirdCameraManager] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info("[ThirdCameraManager] " + content, args);
        }
    }
}