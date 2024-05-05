// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using Emgu.CV;
using Emgu.CV.Structure;
using System.IO.Pipelines;

namespace ImageSharpMjpegInput;

internal static class ProducerCV
{
    public static async Task Run(PipeWriter writer, CancellationToken token)
    {
        VideoCapture videoCapture = new(0);
        videoCapture.Set(Emgu.CV.CvEnum.CapProp.FrameWidth, 1920);
        videoCapture.Set(Emgu.CV.CvEnum.CapProp.FrameHeight, 1080);
        videoCapture.Start();
        using var jpegOutputMemoryStream = new MemoryStream();
        while (!token.IsCancellationRequested)
        {
            using var mat = new Image<Bgr, byte>(1920, 1080);
            var isOk = videoCapture.Read(mat);
            if (!isOk)
            {
                Thread.Sleep(10);
                continue;
            }
            var jpegData = mat.ToJpegData(100);
            jpegOutputMemoryStream.Write(jpegData, 0, jpegData.Length);
            var length = jpegOutputMemoryStream.Position;
            var memory = writer.GetMemory((int)length);
            jpegOutputMemoryStream.Position = 0;

            // Make the frame available to the reader
            writer.Advance(jpegOutputMemoryStream.Read(memory.Span));
            var flushResult = await writer.FlushAsync(token);

            jpegOutputMemoryStream.SetLength(0);

            if (flushResult.IsCompleted || flushResult.IsCanceled)
            {
                break;
            }
            Thread.Sleep(10);
        }

        await writer.CompleteAsync();
    }
}