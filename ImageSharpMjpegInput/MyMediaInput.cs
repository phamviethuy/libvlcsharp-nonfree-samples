using Basler.Pylon;
using LibVLCSharp.Shared;
using System;
using System.Collections.Concurrent;

namespace ImageSharpMjpegInput
{
    internal class MyMediaInput : MediaInput
    {
        private readonly Camera camera = new();
        private readonly ConcurrentQueue<IntPtr> frames = new();
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
                    frames.Enqueue(grabResult.PixelDataPointer);
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

        public unsafe override int Read(IntPtr buf, uint len)
        {
            ManualResetEvent.WaitOne();
            var isOk = frames.TryDequeue(out var intPt);
            if (!isOk)
            {
                return (int)(len);
            }
            var capturedFrame = new Span<byte>(intPt.ToPointer(), (int)len);
            var buffer = (capturedFrame.Length > len) ? capturedFrame[..(int)len] : capturedFrame;
            var outputBuffer = new Span<byte>(buf.ToPointer(), (int)len);

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

    }
}