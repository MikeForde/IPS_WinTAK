using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;

namespace ipswintakplugin.AnimatedQr
{
    public partial class AnimatedQrScanWindow : Window, INotifyPropertyChanged
    {
        private VideoCaptureDevice _device;
        private readonly BarcodeReader _reader;
        private readonly AnimatedQrAssembler _assembler = new AnimatedQrAssembler();

        public event PropertyChangedEventHandler PropertyChanged;

        private int _progress;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }

        private string _statusLine = "Starting camera…";
        public string StatusLine { get => _statusLine; set { _statusLine = value; OnPropertyChanged(); } }

        private string _lastPacketInfo = "";
        public string LastPacketInfo { get => _lastPacketInfo; set { _lastPacketInfo = value; OnPropertyChanged(); } }

        internal DecodeResult Result { get; private set; }

        public AnimatedQrScanWindow()
        {
            InitializeComponent();
            DataContext = this;

            _reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            Loaded += (_, __) => StartCamera();
            Closing += (_, __) => StopCamera();
        }

        private void StartCamera()
        {
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (devices.Count == 0)
            {
                StatusLine = "No camera devices found.";
                return;
            }

            // pick first camera (you can add a dropdown later)
            _device = new VideoCaptureDevice(devices[0].MonikerString);
            _device.NewFrame += Device_NewFrame;
            _device.Start();

            StatusLine = "Scanning… (point camera at animated QR)";
        }

        private void StopCamera()
        {
            try
            {
                if (_device != null)
                {
                    _device.NewFrame -= Device_NewFrame;
                    if (_device.IsRunning) _device.SignalToStop();
                    _device = null;
                }
            }
            catch { }
        }

        private void Device_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (Result != null) return;

            Bitmap frame = null;
            try
            {
                frame = (Bitmap)eventArgs.Frame.Clone();

                // show preview
                var bmp = ToBitmapImage(frame);
                bmp.Freeze();
                Dispatcher.BeginInvoke(new Action(() => PreviewImage.Source = bmp));

                // decode
                var zres = _reader.Decode(frame);
                if (zres?.Text == null) return;

                // packet handling
                if (_assembler.TryAddPacket(zres.Text, out var reconstructed, out var info))
                {
                    var decoded = IpsEnvelopeDecoder.Decode(reconstructed);
                    Result = decoded;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        StatusLine = decoded.DecodeError == null ? "Scan complete." : "Scan complete (decode issues).";
                        Progress = 100;
                        Close();
                    }));
                    return;
                }
                else
                {
                    // update progress-ish
                    var total = _assembler.Total;
                    var received = _assembler.ReceivedCount;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LastPacketInfo = info ?? "";
                        if (total.HasValue && total.Value > 0)
                        {
                            Progress = (int)Math.Floor((received / (double)total.Value) * 100.0);
                            StatusLine = $"Scanning… received {received}/{total.Value}";
                        }
                    }));
                }
            }
            catch
            {
                // ignore per-frame errors
            }
            finally
            {
                frame?.Dispose();
            }
        }

        private static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
