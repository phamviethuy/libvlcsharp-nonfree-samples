// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using Basler.Pylon;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using BitMiracle.LibJpeg;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

    private static byte[] EncodeBgrToJpeg(byte[] bgrData, int width, int height)
    {
        // Create a MemoryStream to hold the JPEG bytes
        using MemoryStream memoryStream = new();
        // Initialize JPEG compressor
        using (JpegImage jpegImage = new(GetSampleRows(bgrData, width, height), Colorspace.RGB)) // Provide sample data and specify colorspace
        {
            // Encode to JPEG
            jpegImage.WriteJpeg(memoryStream);
        }

        // Return the JPEG bytes
        return memoryStream.ToArray();
    }


    // Helper function to create sample rows from BGR data
    static SampleRow[] GetSampleRows(byte[] bgrData, int width, int height)
    {
        SampleRow[] sampleRows = new SampleRow[height];
        int rowSize = width * 3; // 3 components (BGR)

        for (int y = 0; y < height; y++)
        {
            byte[] rowData = new byte[rowSize];
            Array.Copy(bgrData, y * rowSize, rowData, 0, rowSize);
            sampleRows[y] = new SampleRow(rowData, width, 8, 3); // 8 bits per component, 3 components per sample (BGR)
        }

        return sampleRows;
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
                var jpegData = EncodeBgrToJpeg(data, 1024, 1040);
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
            camera.StreamGrabber.Start();

            while (true)
            {
                using var grabResult = camera.StreamGrabber.RetrieveResult(100, TimeoutHandling.Return);
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