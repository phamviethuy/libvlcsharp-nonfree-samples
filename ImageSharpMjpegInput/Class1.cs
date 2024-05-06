
//using LibVLCSharp.Shared;
//using System.IO.Pipelines;
//using ImageSharpMjpegInput;

//Core.Initialize();

//using var libVLC = new LibVLC();
//var pipe = new Pipe();
////using var mediaInput = new PipeMediaInput(pipe.Reader);
//using var media = new Media(libVLC, "dshow://", FromType.FromLocation, $":dshow-vdev=ManyCam Virtual Webcam", ":dshow-size=1920x1080");
//using var mp = new MediaPlayer(media);

//var form = new Form();

//form.ClientSize = new Size(1280, 720);

//form.Load += (s, e) =>
//{
//    mp.Hwnd = form.Handle;
//    mp.Play();
//};

//form.Show();

//var cancellationTokenSource = new CancellationTokenSource();

//var producerTask = Task.Run(() =>
//{

//    new ProducerPylon(pipe.Writer, cancellationTokenSource.Token);

//});

//form.FormClosing += (s, e) =>
//{
//    mp.Stop();
//    cancellationTokenSource.Cancel();
//    Application.Exit();
//};

//Application.Run();