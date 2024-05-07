// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using Basler.Pylon;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;

namespace ImageSharpMjpegInput;

internal class ProducerPylon
{
    private readonly MemoryStream _jpegOutputMemoryStream;
    private readonly CancellationToken _token;
    private readonly PipeWriter _writer;
    private readonly ConcurrentQueue<byte[]> frames = new();

    public ProducerPylon(PipeWriter writer, CancellationToken token)
    {
        _writer = writer;
        _token = token;
        _jpegOutputMemoryStream = new MemoryStream();
        InitCam();
        Consumer();
    }

    private async Task AddImageBufferAsync(byte[] jpegData)
    {
        _jpegOutputMemoryStream.Write(jpegData, 0, jpegData.Length);
        var length = _jpegOutputMemoryStream.Position;
        var memory = _writer.GetMemory((int)length);
        _jpegOutputMemoryStream.Position = 0;

        // Make the frame available to the reader
        _writer.Advance(_jpegOutputMemoryStream.Read(memory.Span));
        var flushResult = await _writer.FlushAsync(_token);

        _jpegOutputMemoryStream.SetLength(0);

        if (flushResult.IsCompleted || flushResult.IsCanceled)
        {
            Debug.WriteLine("Stop writer");
        }
    }

    private void Consumer()
    {
        Task.Factory.StartNew(async () =>
        {
            while (!_token.IsCancellationRequested)
            {
                if (frames.IsEmpty)
                {
                    Thread.Sleep(5);
                    continue;
                }

                var isFrame = frames.TryDequeue(out var data);
                if (!isFrame)
                {
                    Thread.Sleep(5);
                    continue;
                }
                if (data == null)
                {
                    Thread.Sleep(5);
                    continue;
                }
                await AddImageBufferAsync(data);
                Thread.Sleep(5);
            }
        });
    }

    private void InitCam()
    {
        Task.Factory.StartNew(() =>
        {
            var camera = new Camera();
            camera.CameraOpened += Configuration.AcquireContinuous;
            camera.Open();
            camera.Parameters[PLCamera.PixelFormat].SetValue("BGR8Packed");
            camera.Parameters[PLCamera.Width].SetValue(1920);
            camera.Parameters[PLCamera.Height].SetValue(1080);
            camera.Parameters[PLCamera.AcquisitionFrameRateEnable].SetValue(true);
            camera.Parameters[PLCamera.AcquisitionFrameRate].SetValue(90);
            camera.StreamGrabber.Start();

            while (true)
            {
                using var grabResult = camera.StreamGrabber.RetrieveResult(1000, TimeoutHandling.Return);
                if (grabResult == null || !grabResult.IsValid)
                {
                    Thread.Sleep(5);
                    continue;
                }
                frames.Enqueue(grabResult.PixelData as byte[]);
                Thread.Sleep(5);
            }
        });
    }
}