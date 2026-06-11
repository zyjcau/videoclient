using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using NLog;
using VideoClient.Util;
using VidyoClient;
using Application = System.Windows.Forms.Application;

// using AForge.Video.FFMPEG;

namespace VideoClient.VidyoClient.Ext
{
    public class VideoFrameListener :
        RemoteCamera.IRegisterFrameEventListener,
        RemoteWindowShare.IRegisterFrameEventListener,
        Connector.IRegisterLocalCameraFrameListener
    {
        public static readonly string _savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        //截图
        private bool _inProgressTakeSnapshot;

        private string _fileName = "unknown";

        //录制
        private bool _recordStarted, _recording;

        private string _recordFileName = "unknown", _recordFilePath;
        // private VideoFileWriter _videoFileWriter;

        private IOnSnapshotSavedListener _onSnapshotSavedListener;

        public VideoFrameListener(IOnSnapshotSavedListener onSnapshotSavedListener)
        {
            this._onSnapshotSavedListener = onSnapshotSavedListener;
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }

        public void OnLocalCameraFrame(LocalCamera localCamera, VideoFrame videoFrame)
        {
            // Log("OnLocalCameraFrame p->{0} , v->{1}", "local_camera", videoFrame.GetFormat());
            Take(videoFrame);
            videoFrame.Dispose();
        }

        public void OnRemoteCameraFrame(Participant participant, VideoFrame videoFrame)
        {
            // Log("OnRemoteCameraFrame p->{0} , v->{1}", participant.GetName(), videoFrame.GetFormat());
            Take(videoFrame);
            videoFrame.Dispose();
        }

        public void OnRemoteWindowShareFrame(Participant participant, VideoFrame videoFrame)
        {
            // Log("OnRemoteWindowShareFrame p->{0} , v->{1}", participant.GetName(), videoFrame.GetFormat());
            Take(videoFrame);
            videoFrame.Dispose();
        }

        private void Take(VideoFrame videoFrame)
        {
            if (_inProgressTakeSnapshot)
            {
                Log("Take snapshot started.");
                Save(videoFrame);
                _inProgressTakeSnapshot = false;
                Log("Take snapshot completed.");
            }

            // if (_recordStarted)
            // {
            //     if (null != _videoFileWriter)
            //     {
            //         if (!_videoFileWriter.IsOpen)
            //         {
            //             _recordFilePath =
            //                 $"{_savePath}\\{_recordFileName}_{videoFrame.GetWidth()}x{videoFrame.GetHeight()}";
            //             Log($"_recordFilePath -> {_recordFilePath}");
            //             _videoFileWriter.Open(_recordFilePath, (int) videoFrame.GetWidth(),
            //                 (int) videoFrame.GetHeight());
            //         }
            //
            //         Bitmap bitmap = VideoFrame2Bitmap(videoFrame);
            //         if (null != bitmap) _videoFileWriter.WriteVideoFrame(bitmap);
            //         _recording = true;
            //     }
            // }
            // else
            // {
            //     if (_recording) //need stop
            //     {
            //         _videoFileWriter.Close();
            //         File.Move(_recordFilePath, $"{_recordFilePath}.mp4");
            //         _recording = false;
            //     }
            // }
        }

        public void TakeSnapshot(string fileName)
        {
            _fileName = fileName;
            _inProgressTakeSnapshot = true;
        }

        public bool ToggleRecord(string fileName)
        {
            if (!_recordStarted)
            {
                try
                {
                    // StartRecord(fileName);
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                    throw;
                }
            }
            else
            {
                try
                {
                    StopRecord();
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                    throw;
                }
            }

            return _recordStarted;
        }

        // public void StartRecord(string fileName)
        // {
        //     _recordFileName = fileName;
        //
        //     if (null == _videoFileWriter)
        //     {
        //         _videoFileWriter = new VideoFileWriter();
        //     }
        //
        //     _recordStarted = true;
        //
        //     Log("StartRecord {0}", fileName);
        // }

        public void StopRecord()
        {
            _recordStarted = false;
        }

        private Bitmap VideoFrame2Bitmap(VideoFrame videoFrame)
        {
            byte[] yuv = GetYuv(videoFrame);
            byte[] rgb = new byte[3 * videoFrame.GetWidth() * videoFrame.GetHeight()];
            Bitmap bitmap = ConvertYUV2RGB(yuv, rgb, (int)videoFrame.GetWidth(), (int)videoFrame.GetHeight());

            // Bitmap bmp = new Bitmap((int) videoFrame.GetWidth(), (int) videoFrame.GetHeight(),
            //     PixelFormat.Format24bppRgb);
            //
            // int r = 0;
            // int g = 0;
            // int b = 0;
            //
            // for (int i = 0; i < rgb.Length; i++)
            // {
            //     if (i % 3 == 0)
            //     {
            //         r = i;
            //     }
            //     else if (i % 3 == 1)
            //     {
            //         g = i;
            //     }
            //     else if (i % 3 == 2)
            //     {
            //         b = i;
            //     }
            //
            //     if (i % 3 == 2)
            //     {
            //         int x = (i / 3 - 2) % (int) videoFrame.GetWidth();
            //         int y = (i / 3 - 2) / (int) videoFrame.GetWidth();
            //         Log("trans bitmap x:{0},y:{1},r:{2},g:{3},b:{4}", x, y, r, g, b);
            //         // bmp.SetPixel(x, y, Color.FromArgb(rgb[r], rgb[g], rgb[b]));
            //     }
            // }

            return bitmap;
        }

        private AtomicBoolean _isSaving = new AtomicBoolean(false);

        private void Save(VideoFrame videoFrame)
        {
            if (!_isSaving.CompareAndSet(false, true))
            {
                return;
            }

            string fileType = "bmp";
            string name =
                $"{_savePath}\\{_fileName}_{videoFrame.GetWidth()}x{videoFrame.GetHeight()}_{DateTimeOffset.Now.ToUnixTimeSeconds()}.{fileType}";

            Log("-----------------------------------------------");
            Log("| IsRunOnMain         -> {0}",
                Thread.CurrentThread.ManagedThreadId == Program.mainThreadId);
            Log("| Format         -> {0}", videoFrame.GetFormat());
            Log("| Width x Height -> {0}x{1}", videoFrame.GetWidth(), videoFrame.GetHeight());
            Log("| Size           -> {0}", videoFrame.GetSize());
            Log("| Y              -> {0},{1}x{2},{3},{4}",
                videoFrame.GetSizeY(),
                videoFrame.GetWidthY(), videoFrame.GetHeightY(),
                videoFrame.GetOffsetY(), videoFrame.GetPitchY());
            Log("| Cb             -> {0},{1}x{2},{3},{4}",
                videoFrame.GetSizeCb(),
                videoFrame.GetWidthCb(), videoFrame.GetHeightCb(),
                videoFrame.GetOffsetCb(), videoFrame.GetPitchCb());
            Log("| Cr             -> {0},{1}x{2},{3},{4}",
                videoFrame.GetSizeCr(),
                videoFrame.GetWidthCr(), videoFrame.GetHeightCr(),
                videoFrame.GetOffsetCr(), videoFrame.GetPitchCr());
            Log("-----------------------------------------------");

            byte[] yuv = GetYuv(videoFrame);
            byte[] rgb = new byte[3 * videoFrame.GetWidth() * videoFrame.GetHeight()];
            Bitmap bitmap = ConvertYUV2RGB(yuv, rgb, (int)videoFrame.GetWidth(), (int)videoFrame.GetHeight());
            bitmap.Save(name, ImageFormat.Bmp);
            _onSnapshotSavedListener?.OnSnapshotSaved(name);

            _isSaving.Set(false);
        }

        private byte[] GetYuv(VideoFrame v)
        {
            //parse Y
            byte[] yData = GetData(v.GetDataY(),
                v.GetWidthY(), v.GetHeightY(),
                v.GetOffsetY(), v.GetPitchY()
            );
            //parse U
            byte[] uData = GetData(
                v.GetDataCb(),
                v.GetWidthCb(), v.GetHeightCb(),
                v.GetOffsetCb(), v.GetPitchCb()
            );
            //parse V
            byte[] vData = GetData(
                v.GetDataCr(),
                v.GetWidthCr(), v.GetHeightCr(),
                v.GetOffsetCr(), v.GetPitchCr()
            );
            byte[] yuv = new byte[yData.Length + uData.Length + vData.Length];
            Array.Copy(
                yData, 0,
                yuv, 0, yData.Length);
            Array.Copy(
                uData, 0,
                yuv, yData.Length, uData.Length);
            Array.Copy(
                vData, 0,
                yuv, yData.Length + uData.Length, vData.Length);
            return yuv;
        }

        private byte[] GetData(
            byte[] data,
            uint width, uint height,
            uint offset,
            uint pitch
        )
        {
            uint fi = offset;
            byte[] box = new byte[width * height];
            for (int i = 0; i < height; i++)
            {
                Array.Copy(
                    data, fi,
                    box, i * width, width);
                // bw.Write(box, 0, box.Length);
                fi += pitch;
            }

            return box;
        }

        private void WriteBMP(byte[] rgbFrame, int width, int height, string bmpFile)
        {
            // 写 BMP 图像文件。
            int yu = width * 3 % 4;
            int bytePerLine = 0;
            yu = yu != 0 ? 4 - yu : yu;
            bytePerLine = width * 3 + yu;

            using (FileStream fs = File.Open(bmpFile, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write('B');
                    bw.Write('M');
                    bw.Write(bytePerLine * height + 54);
                    bw.Write(0);
                    bw.Write(54);
                    bw.Write(40);
                    bw.Write(width);
                    bw.Write(height);
                    bw.Write((ushort)1);
                    bw.Write((ushort)24);
                    bw.Write(0);
                    bw.Write(bytePerLine * height);
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);

                    byte[] data = new byte[bytePerLine * height];
                    int gIndex = width * height;
                    int bIndex = gIndex * 2;

                    for (int y = height - 1, j = 0; y >= 0; y--, j++)
                    {
                        for (int x = 0, i = 0; x < width; x++)
                        {
                            data[y * bytePerLine + i++] = rgbFrame[bIndex + j * width + x]; // B
                            data[y * bytePerLine + i++] = rgbFrame[gIndex + j * width + x]; // G
                            data[y * bytePerLine + i++] = rgbFrame[j * width + x]; // R
                        }
                    }

                    bw.Write(data, 0, data.Length);
                    bw.Flush();
                    bw.Close();
                }
            }
        }

        private double[,] YUV2RGB_CONVERT_MATRIX =
            new double[3, 3] { { 1, 0, 1.4022 }, { 1, -0.3456, -0.7145 }, { 1, 1.771, 0 } };

        /// <summary>
        /// 将一桢 YUV 格式的图像转换为一桢 RGB 格式图像。
        /// </summary>
        /// <param name="yuvFrame">YUV 格式图像数据。</param>
        /// <param name="rgbFrame">RGB 格式图像数据。</param>
        /// <param name="width">图像宽（单位：像素）。</param>
        /// <param name="height">图像高（单位：像素）。</param>
        private Bitmap ConvertYUV2RGB(byte[] yuvFrame, byte[] rgbFrame, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            int uIndex = width * height;
            int vIndex = uIndex + ((width * height) >> 2);
            int gIndex = width * height;
            int bIndex = gIndex * 2;

            int temp = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // R分量
                    temp = (int)(yuvFrame[y * width + x] + (yuvFrame[vIndex + (y / 2) * (width / 2) + x / 2] - 128) *
                        YUV2RGB_CONVERT_MATRIX[0, 2]);
                    rgbFrame[y * width + x] = (byte)(temp < 0 ? 0 : (temp > 255 ? 255 : temp));

                    // G分量
                    temp = (int)(yuvFrame[y * width + x] +
                                 (yuvFrame[uIndex + (y / 2) * (width / 2) + x / 2] - 128) *
                                 YUV2RGB_CONVERT_MATRIX[1, 1] +
                                 (yuvFrame[vIndex + (y / 2) * (width / 2) + x / 2] - 128) *
                                 YUV2RGB_CONVERT_MATRIX[1, 2]);
                    rgbFrame[gIndex + y * width + x] = (byte)(temp < 0 ? 0 : (temp > 255 ? 255 : temp));

                    // B分量
                    temp = (int)(yuvFrame[y * width + x] + (yuvFrame[uIndex + (y / 2) * (width / 2) + x / 2] - 128) *
                        YUV2RGB_CONVERT_MATRIX[2, 1]);
                    rgbFrame[bIndex + y * width + x] = (byte)(temp < 0 ? 0 : (temp > 255 ? 255 : temp));

                    //
                    bitmap.SetPixel(x, y, Color.FromArgb(
                        rgbFrame[y * width + x],
                        rgbFrame[gIndex + y * width + x],
                        rgbFrame[bIndex + y * width + x]));
                }
            }

            return bitmap;
        }

        private static int R = 0;
        private static int G = 1;
        private static int B = 2;

        public static int[] I420ToRGB(byte[] src, int width, int height)
        {
            int numOfPixel = width * height;
            int positionOfV = numOfPixel;
            int positionOfU = numOfPixel / 4 + numOfPixel;
            int[] rgb = new int[numOfPixel * 3];

            for (int i = 0; i < height; i++)
            {
                int startY = i * width;
                int step = (i / 2) * (width / 2);
                int startV = positionOfV + step;
                int startU = positionOfU + step;
                for (int j = 0; j < width; j++)
                {
                    int Y = startY + j;
                    int V = startV + j / 2;
                    int U = startU + j / 2;
                    int index = Y * 3;
                    rgb[index + B] = (int)((src[Y] & 0xff) + 1.4075 * ((src[V] & 0xff) - 128));
                    rgb[index + G] = (int)((src[Y] & 0xff) - 0.3455 * ((src[U] & 0xff) - 128) -
                                           0.7169 * ((src[V] & 0xff) - 128));
                    rgb[index + R] = (int)((src[Y] & 0xff) + 1.779 * ((src[U] & 0xff) - 128));
                }
            }

            return rgb;
        }


        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private void Log(string content)
        {
            _logger.Info("[VideoFrameListener] " + content);
        }

        private void Log(string content, params object[] args)
        {
            _logger.Info($"[VideoFrameListener] {content}", args);
        }

        public interface IOnSnapshotSavedListener
        {
            void OnSnapshotSaved(string filePath);
        }
    }
}