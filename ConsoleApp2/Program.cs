using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Windows.Forms;

namespace WebcamStream
{
    class Program
    {
        static void Main(string[] args)
        {
            Core.Initialize();

            // Create a new LibVLC instance
            using (var libVLC = new LibVLC())
            {
                // Create a new MediaPlayer
                using (var mediaPlayer = new MediaPlayer(libVLC))
                {
                    // Create a new VideoView
                    using (var videoView = new VideoView())
                    {
                        // Create a window to display the video
                        var form = new Form();
                        form.Text = "Webcam Stream";
                        form.Size = new Size(640, 480); // Set your desired size

                        // Add the VideoView to the form
                        form.Controls.Add(videoView);

                        // Show the form
                        form.Show();

                        // Create a VideoCapture object to capture frames from the webcam
                        using (var capture = new VideoCapture())
                        {
                            // Set the webcam resolution
                            capture.Set(CapProp.FrameWidth, 640);
                            capture.Set(CapProp.FrameHeight, 480);

                            // Start capturing frames
                            capture.Start();

                            // Event handler for updating the video stream
                            void UpdateVideoStream(object sender, EventArgs e)
                            {
                                // Capture a frame from the webcam
                                using (var frame = capture.QueryFrame())
                                {
                                    if (frame != null)
                                    {
                                        // Convert the frame to a byte array representing the RGB pixels
                                        byte[] imageData = ConvertFrameToByteArray(frame);

                                        // Create a Media from the RGB image data
                                        using (var media = new Media(libVLC, new byte[] { }, FormatCallback, CleanupCallback))
                                        {
                                            // Set the media options
                                            media.AddOption(":demux=rawvideo");
                                            media.AddOption(":rawvidwidth=640");
                                            media.AddOption(":rawvidheight=480");
                                            media.AddOption(":rawvidformat=RGB");

                                            // Set the media to the MediaPlayer
                                            mediaPlayer.Play(media);
                                        }
                                    }
                                }
                            }

                            // Hook up the event handler to update the video stream
                            Application.Idle += UpdateVideoStream;

                            // Set the VideoView as the render window for the MediaPlayer
                            mediaPlayer.SetRenderWindow(videoView);

                            // Start the playback
                            mediaPlayer.Play();

                            // Wait for the user to close the form
                            System.Windows.Forms.Application.Run(form);
                        }
                    }
                }
            }
        }

        // Convert Emgu.CV.Mat to byte array
        private static byte[] ConvertFrameToByteArray(Mat frame)
        {
            // Convert the frame to Bgr format
            var bgrFrame = frame.ToImage<Bgr, byte>();

            // Get the data pointer
            IntPtr ptr = bgrFrame.MIplImage.ImageData;

            // Get the image data as byte array
            byte[] imageData = new byte[bgrFrame.Width * bgrFrame.Height * bgrFrame.NumberOfChannels];
            System.Runtime.InteropServices.Marshal.Copy(ptr, imageData, 0, imageData.Length);

            return imageData;
        }

        // Callback function for LibVLC to provide the video data
        private static void FormatCallback(ref byte[] data, ref int width, ref int height, ref int pixelPitch)
        {
            // Set the output parameters
            data = GetBinaryRGBImageData();
            width = 640; // Set your image width
            height = 480; // Set your image height
            pixelPitch = 3; // Assuming RGB data with 3 bytes per pixel
        }

        // Callback function for LibVLC to cleanup resources
        private static void CleanupCallback(IntPtr opaque)
        {
            // Cleanup resources if needed
        }
    }
}
