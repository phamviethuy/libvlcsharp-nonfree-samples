using LibVLCSharp.Shared;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private MemoryStream _mediaStream;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();

            _libVLC = new LibVLC("--no-osd");
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;
        }

        // Method to update the video frame
        public void UpdateFrame(byte[] frameData, int width, int height)
        {
            if (_mediaStream == null)
            {
                _mediaStream = new MemoryStream();
                _mediaPlayer.Media = new Media(_libVLC, _mediaStream, ":demux=rawvideo", $":rawvid-fps=60/1", $":rawvid-width={width}", $":rawvid-height={height}", ":rawvid-chroma=BGR");
            }
            else
            {
                _mediaStream.Seek(0, SeekOrigin.Begin);
                _mediaStream.SetLength(0);
            }

            _mediaStream.Write(frameData, 0, frameData.Length);
            _mediaStream.Flush();

            _mediaPlayer.Play();
        }
    }
}