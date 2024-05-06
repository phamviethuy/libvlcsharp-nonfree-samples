// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using ImageSharpMjpegInput;
using LibVLCSharp.Shared;
using System.IO.Pipelines;

Core.Initialize();

using var libVLC = new LibVLC();
var pipe = new Pipe();
using var mediaInput = new MyMediaInput();
using var media = new Media(libVLC, mediaInput);

// Set video parameters
media.AddOption(":demux=rawvideo");
media.AddOption(":rawvid-width=" + 1024);
media.AddOption(":rawvid-height=" + 1040);
media.AddOption(":rawvid-format=RGB");
media.AddOption(":rawvid-fps=" + 30);
media.AddOption(":rawvid-chroma=" + "RV24");

using var mp = new MediaPlayer(media);

var form = new Form
{
    ClientSize = new Size(1280, 720)
};

form.Load += (s, e) =>
{
    mp.Hwnd = form.Handle;
    mp.Play();
};

form.Show();

var cancellationTokenSource = new CancellationTokenSource();

//var producerTask = Task.Run(() =>
//{
//    new ProducerPylon(pipe.Writer, cancellationTokenSource.Token);
//    //new ProducerFlashCap(pipe.Writer, cancellationTokenSource.Token);
//    // ProducerCV.Run(pipe.Writer, cancellationTokenSource.Token);
//});

form.FormClosing += (s, e) =>
{
    mp.Stop();
    cancellationTokenSource.Cancel();
    Application.Exit();
};

Application.Run();