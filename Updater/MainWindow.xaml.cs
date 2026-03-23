using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Updater.Common;
using Updater.Core;
using Updater.Resources;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _backgroundWorker1;
        private readonly HttpClient _httpClient;

        public MainWindow()
        {
            InitializeComponent();

            _backgroundWorker1 = new BackgroundWorker();
            _backgroundWorker1.WorkerReportsProgress = true;
            _backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            _backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;

            var handler = new ProgressMessageHandler(new HttpClientHandler());
            handler.HttpReceiveProgress += ProgressMessageHandler_HttpReceiveProgress;
            _httpClient = new HttpClient(handler, true);
        }

        private async Task InitializeWebView2Async()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShaiyaLotus", "WebView2");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebView21.EnsureCoreWebView2Async(env);

                WebView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                WebView21.CoreWebView2.Settings.AreDevToolsEnabled = false;
                WebView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WebView21.CoreWebView2.Settings.IsZoomControlEnabled = false;

                NavigateToHome();
            }
            catch (Exception ex)
            {
                TextBox1.Text = $"WebView2: {ex.Message}";
            }
        }

        private void ProgressMessageHandler_HttpReceiveProgress(object? sender, HttpProgressEventArgs e)
        {
            if (sender == null)
                return;

            _backgroundWorker1.ReportProgress(e.ProgressPercentage, new ProgressReport("ProgressBar1"));
        }

        private void BackgroundWorker1_DoWork(object? sender, DoWorkEventArgs e)
        {
            Program.DoWork(_httpClient, _backgroundWorker1);
        }

        private void BackgroundWorker1_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string text)
            {
                TextBox1.Text = text;
            }

            if (e.UserState is ProgressReport progressReport)
            {
                if (progressReport.Value != null)
                {
                    if (progressReport.Value is string value)
                    {
                        if (value == ProgressBar1.Name)
                        {
                            ProgressBar1.Value = e.ProgressPercentage;
                        }

                        if (value == ProgressBar2.Name)
                        {
                            ProgressBar2.Value = e.ProgressPercentage;
                            if (e.ProgressPercentage >= 100)
                            {
                                StartPlayButtonBreathingAnimation();
                            }
                        }
                    }
                }
            }
        }

        private bool _isPlayButtonBreathing = false;

        private void StartPlayButtonBreathingAnimation()
        {
            if (_isPlayButtonBreathing) return;
            _isPlayButtonBreathing = true;

            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 1.08,
                Duration = new Duration(TimeSpan.FromSeconds(0.9)),
                AutoReverse = true,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                EasingFunction = new System.Windows.Media.Animation.SineEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            
            var transform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
            Button2.RenderTransformOrigin = new Point(0.5, 0.5);
            Button2.RenderTransform = transform;

            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
        }

        private void Window1_Initialized(object sender, EventArgs e)
        {
            if (DllImport.FindWindowW("GAME", "Shaiya") != IntPtr.Zero)
            {
                MessageBox.Show(Strings.GameWindow, Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown(0);
            }
        }

        private System.Windows.Threading.DispatcherTimer? _statusTimer;
        private System.Windows.Threading.DispatcherTimer? _bgTimer;
        private int _currentBgIndex = 0;
        private readonly string[] _backgroundImages = new string[] 
        { 
            "pack://application:,,,/Resources/bg_lotus_1.png",
            "pack://application:,,,/Resources/bg_lotus_2.png",
            "pack://application:,,,/Resources/bg_lotus_3.png"
        };

        private async void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView2Async();
            _backgroundWorker1.RunWorkerAsync();

            _statusTimer = new System.Windows.Threading.DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(20);
            _statusTimer.Tick += async (s, ev) => await CheckServerStatusAsync();
            _statusTimer.Start();
            
            await CheckServerStatusAsync();
            StartDynamicBackground();
        }

        private async Task CheckServerStatusAsync()
        {
            try
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                var host = new Uri(Constants.Source).Host;
                var connectTask = tcpClient.ConnectAsync(host, 30800); // Puerto por defecto del login Shaiya
                var timeoutTask = Task.Delay(2500);

                if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                {
                    StatusEllipse.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 120));
                    StatusText.Text = "SERVIDOR ONLINE";
                    StatusText.Foreground = StatusEllipse.Fill;
                }
                else
                {
                    StatusEllipse.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 60, 60));
                    StatusText.Text = "SERVIDOR OFFLINE";
                    StatusText.Foreground = StatusEllipse.Fill;
                }
            }
            catch
            {
                StatusEllipse.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 60, 60));
                StatusText.Text = "SERVIDOR OFFLINE";
                StatusText.Foreground = StatusEllipse.Fill;
            }
        }

        private void StartDynamicBackground()
        {
            _bgTimer = new System.Windows.Threading.DispatcherTimer();
            _bgTimer.Interval = TimeSpan.FromSeconds(10);
            _bgTimer.Tick += (s, ev) => 
            {
                if (_backgroundImages.Length > 1)
                {
                    _currentBgIndex = (_currentBgIndex + 1) % _backgroundImages.Length;
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0.85, To = 0.0,
                        Duration = new Duration(TimeSpan.FromSeconds(0.6))
                    };
                    fadeOut.Completed += (sender, args) =>
                    {
                        BgImageBrush.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(_backgroundImages[_currentBgIndex]));
                        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 0.0, To = 0.85,
                            Duration = new Duration(TimeSpan.FromSeconds(0.8))
                        };
                        BgImageBrush.BeginAnimation(System.Windows.Media.Brush.OpacityProperty, fadeIn);
                    };
                    BgImageBrush.BeginAnimation(System.Windows.Media.Brush.OpacityProperty, fadeOut);
                }
            };
            _bgTimer.Start();
        }

        private void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker1.IsBusy)
                return;

            Application.Current.Shutdown(0);
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker1.IsBusy)
                return;

            try
            {
                var fileName = Path.Combine(Directory.GetCurrentDirectory(), "game.exe");
                Process.Start(fileName, "start game");

                var currentProcess = Process.GetCurrentProcess();
                currentProcess.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(ex.HResult);
            }
        }

        private void NavigateToHome()
        {
            if (WebView21.CoreWebView2 == null)
                return;

            WebView21.CoreWebView2.Navigate("about:blank");
        }

        private void NavigateWebView(string url)
        {
            if (WebView21.CoreWebView2 != null)
                WebView21.CoreWebView2.Navigate(url);
        }

        private void SidebarHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateToHome();
        }

        private void SidebarNews_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://shaiyalotus.com/news-events?tab=news",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                TextBox1.Text = $"News: {ex.Message}";
            }
        }

        private void SidebarDownloads_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://shaiyalotus.com/download",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                TextBox1.Text = $"Downloads: {ex.Message}";
            }
        }

        private void SidebarDiscord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Constants.DiscordSource,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                TextBox1.Text = $"Discord: {ex.Message}";
            }
        }
    }
}
