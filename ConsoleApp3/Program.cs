using ImageSharpMjpegInput;
using LibVLCSharp.Shared;
using System.IO.Pipelines;

Core.Initialize();

using var libVLC = new LibVLC();
var pipe = new Pipe();
using var mediaInput = new PipeMediaInput(pipe.Reader);
using var media = new Media(libVLC, mediaInput);

media.AddOption(":demux=rawvideo");
media.AddOption(":rawvid-width=" + 1920);
media.AddOption(":rawvid-height=" + 1080);
media.AddOption(":rawvid-format=BGR");
media.AddOption(":rawvid-fps=" + 90);
media.AddOption(":rawvid-chroma=" + "RV24");

using var mp = new MediaPlayer(media);

var form = new Form();

form.ClientSize = new Size(1280, 720);

form.Load += (s, e) =>
{
    mp.Hwnd = form.Handle;
    mp.Play();
};

form.Show();

var cancellationTokenSource = new CancellationTokenSource();

var producerTask = Task.Run(() =>
{
    //new ProducerCV(pipe.Writer, cancellationTokenSource.Token);
    new ProducerPylon(pipe.Writer, cancellationTokenSource.Token);
});

form.FormClosing += (s, e) =>
{
    mp.Stop();
    cancellationTokenSource.Cancel();
    Application.Exit();
};

Application.Run();