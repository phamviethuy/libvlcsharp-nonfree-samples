// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using FlashCap;
using SixLabors.ImageSharp.Drawing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Pipelines;

namespace ImageSharpMjpegInput;

internal class ProducerFlashCap
{
    private readonly PipeWriter _writer;
    private readonly CancellationToken _token;
    private readonly MemoryStream _jpegOutputMemoryStream;
    private readonly ConcurrentQueue<byte[]> frames = new();

    public ProducerFlashCap(PipeWriter writer, CancellationToken token)
    {
        _writer = writer;
        _token = token;
        _jpegOutputMemoryStream = new MemoryStream();
        InitFlasCapAsync();
        Consumer();
    }

    private async Task InitFlasCapAsync()
    {
        var devices = new CaptureDevices();

        var devicesDes = new List<CaptureDeviceDescriptor>();

        foreach (var descriptor in devices.EnumerateDescriptors().
            Where(d => d.Characteristics.Length >= 1))             // One or more valid video characteristics.
        {
            devicesDes.Add(descriptor);
        }

        var device = devicesDes[0];
        var characteristics = device.Characteristics.FirstOrDefault();

        // Open capture device:
        var captureDevice = await device.OpenAsync(characteristics, OnPixelBufferArrivedAsync);

        captureDevice?.StartAsync();
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

    private void OnPixelBufferArrivedAsync(PixelBufferScope bufferScope)
    {
        var bytes = bufferScope.Buffer.ExtractImage();
        frames.Enqueue(bytes);

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

                await AddImageBufferAsync(data);
                Thread.Sleep(5);
            }
        });

    }
}