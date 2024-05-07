using Basler.Pylon;
using LibVLCSharp.Shared;
using System.Collections.Concurrent;

namespace ImageSharpMjpegInput
{
    internal class MyMediaInput : MediaInput
    {
        private readonly Camera camera = new();
        private readonly ConcurrentQueue<byte[]> frames = new();
        private byte[]? currentFrame;
        private readonly ManualResetEvent ManualResetEvent = new(false);

        public MyMediaInput()
        {
            camera.CameraOpened += Configuration.AcquireContinuous;
            camera.Open();
            camera.Parameters[PLCamera.PixelFormat].SetValue("BGR8Packed");
            camera.Parameters[PLCamera.AcquisitionFrameRateEnable].SetValue(true);
            camera.Parameters[PLCamera.AcquisitionFrameRateAbs].SetValue(30.0);
            camera.StreamGrabber.Start();
            StartCapture();
        }

        private void StartCapture()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    using var grabResult = camera.StreamGrabber.RetrieveResult(1000, TimeoutHandling.Return);
                    if (grabResult == null || !grabResult.IsValid)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    frames.Enqueue(grabResult.PixelData as byte[]);
                    ManualResetEvent.Set();
                    Thread.Sleep(1);
                }
            });
        }

        public override void Close()
        {
            camera.StreamGrabber.Stop();
        }

        public override bool Open(out ulong size)
        {
            size = ulong.MaxValue;

            return true;
        }

        public override int Read(IntPtr buf, uint len)
        {
            ManualResetEvent.WaitOne();
            if (currentFrame != null)
            {
                System.Runtime.InteropServices.Marshal.Copy(currentFrame, 0, buf, currentFrame.Length);
                ManualResetEvent.Reset();
                var length = currentFrame.Length;
                currentFrame = null;
                return length;
            }

            var isOk = frames.TryDequeue(out var capturedFrame);
            if (!isOk)
            {
                return -1;
            }

            if (capturedFrame == null)
            {
                return -1;
            }

            if (capturedFrame.Length > len)
            {
                int remainLenght = capturedFrame.Length - (int)len;
                currentFrame = new byte[remainLenght];

                System.Runtime.InteropServices.Marshal.Copy(capturedFrame, 0, buf, (int)len);
                Array.Copy(capturedFrame, (int)len, currentFrame, 0, remainLenght);
                return capturedFrame.Length;
            }
            // Copy captured frame to buffer
            if (capturedFrame.Length <= len)
            {
                System.Runtime.InteropServices.Marshal.Copy(capturedFrame, 0, buf, capturedFrame.Length);
                return capturedFrame.Length;
            }

            return capturedFrame.Length;
        }

        public override bool Seek(ulong offset)
        {
            return false;
        }
    }
}