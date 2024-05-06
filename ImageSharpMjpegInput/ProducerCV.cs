// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;

namespace ImageSharpMjpegInput;

internal class ProducerCV
{
    private readonly MemoryStream _jpegOutputMemoryStream;
    private readonly CancellationToken _token;
    private readonly PipeWriter _writer;
    private readonly ConcurrentQueue<Image<Bgr, byte>> frames = new();

    public ProducerCV(PipeWriter writer, CancellationToken token)
    {
        _writer = writer;
        _token = token;
        _jpegOutputMemoryStream = new MemoryStream();
        Task.Factory.StartNew(InitCam);
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
                    Thread.Sleep(10);
                    continue;
                }

                var isFrame = frames.TryDequeue(out var data);
                if (!isFrame)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (data == null)
                {
                    Thread.Sleep(10);
                    continue;
                }
                var jpegData = data.ToJpegData();
                await AddImageBufferAsync(jpegData);
                data.Dispose();
                Thread.Sleep(5);
            }
        });
    }

    private void InitCam()
    {
        VideoCapture videoCapture = new(0);
        videoCapture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 1920);
        videoCapture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 1080);
        videoCapture.Start();
        while (!_token.IsCancellationRequested)
        {
            var mat = new Image<Bgr, byte>(1920, 1080);
            var isOk = videoCapture.Read(mat);
            if (!isOk)
            {
                Thread.Sleep(10);
                continue;
            }
            frames.Enqueue(mat);

            Thread.Sleep(10);
        }
    }
}