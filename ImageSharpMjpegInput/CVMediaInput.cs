using Emgu.CV;
using Emgu.CV.Structure;
using LibVLCSharp.Shared;
using System.Collections.Concurrent;

namespace ImageSharpMjpegInput
{
    internal class CVMediaInput : MediaInput
    {
        private readonly VideoCapture camera = new(1);
        private readonly ConcurrentQueue<byte[]> frames = new();
        private readonly ManualResetEvent ManualResetEvent = new(false);

        private int bytesRead;

        public CVMediaInput()
        {
            camera.Start();
            camera.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 1920);
            camera.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 1080);
            StartCapture();
        }

        public override void Close()
        {
            camera.Stop();
        }

        public override bool Open(out ulong size)
        {
            size = ulong.MaxValue;

            return true;
        }

        public override int Read(IntPtr buf, uint len)
        {
            ManualResetEvent.WaitOne();
            var isOk = frames.TryDequeue(out var capturedFrame);
            if (!isOk)
            {
                return -1;
            }

            if (capturedFrame == null)
            {
                return -1;
            }

            // Copy captured frame to buffer
            if (capturedFrame.Length <= len)
            {
                System.Runtime.InteropServices.Marshal.Copy(capturedFrame, 0, buf, capturedFrame.Length);
                ManualResetEvent.Reset();
                return capturedFrame.Length;
            }

            return capturedFrame.Length;
        }

        public override bool Seek(ulong offset)
        {
            return false;
        }

        private void StartCapture()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var img = new Image<Bgr, byte>(1920, 1080);
                    var isOk = camera.Read(img);
                    if (!isOk)
                    {
                        img.Dispose();
                        Thread.Sleep(5);
                        continue;
                    }
                    frames.Enqueue(img.Bytes);
                    img.Dispose();
                    ManualResetEvent.Set();
                    Thread.Sleep(5);
                }
            });
        }
    }
}