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
using System.Windows.Media;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker _backgroundWorker1;
        private readonly HttpClient _httpClient;
        private System.Windows.Forms.NotifyIcon? _trayIcon;

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

            // System Tray Icon Setup
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Icon", "IconGroup164.ico");
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Text = "Shaiya Lotus";
            if (File.Exists(iconPath))
                _trayIcon.Icon = new System.Drawing.Icon(iconPath);
            else
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            _trayIcon.Visible = false;
            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();
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
            Button2.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            Button2.RenderTransform = transform;

            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
        }

        private void Window1_Initialized(object sender, EventArgs e)
        {
            // Game detection is now handled dynamically via CheckGameProcess() timer.
            // The Play button will show "JUEGO ACTIVO" if game.exe is already running.
        }

        private System.Windows.Threading.DispatcherTimer? _statusTimer;
        private System.Windows.Threading.DispatcherTimer? _bgTimer;
        private System.Windows.Threading.DispatcherTimer? _gameTimer;
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

            _gameTimer = new System.Windows.Threading.DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(5);
            _gameTimer.Tick += (s, ev) => CheckGameProcess();
            _gameTimer.Start();
            
            await CheckServerStatusAsync();
            CheckGameProcess();
            await LoadPvpRankingAsync();
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

        private void CheckGameProcess()
        {
            var gameRunning = Process.GetProcessesByName("game").Length > 0;
            if (gameRunning)
            {
                Button2.IsEnabled = false;
                Button2.Content = "JUEGO ACTIVO";
                // Stop breathing animation when game is running
                if (Button2.RenderTransform is System.Windows.Media.ScaleTransform t)
                {
                    t.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
                    t.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
                }
                _isPlayButtonBreathing = false;
            }
            else
            {
                Button2.IsEnabled = true;
                Button2.Content = "JUGAR";
            }
        }

        private async Task LoadPvpRankingAsync()
        {
            var rankings = await DatabaseManager.GetDailyTop5PvpAsync();

            PvpRankItems.Children.Clear();

            if (rankings.Count == 0)
            {
                PvpRankItems.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = "Sin datos hoy",
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666")),
                    FontSize = 10,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                });
                return;
            }

            string[] medals = { "🥇", "🥈", "🥉", "4.", "5." };
            string[] colors = { "#FFD700", "#C0C0C0", "#CD7F32", "#AAAAAA", "#AAAAAA" };

            for (int i = 0; i < rankings.Count && i < 5; i++)
            {
                var row = new System.Windows.Controls.Grid();
                row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(28) });
                row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 2, 0, 2);

                var medal = new System.Windows.Controls.TextBlock
                {
                    Text = medals[i],
                    FontSize = i < 3 ? 14 : 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                System.Windows.Controls.Grid.SetColumn(medal, 0);

                var name = new System.Windows.Controls.TextBlock
                {
                    Text = rankings[i].CharName,
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colors[i])),
                    FontSize = 11,
                    FontWeight = i == 0 ? FontWeights.Bold : FontWeights.Normal,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                System.Windows.Controls.Grid.SetColumn(name, 1);

                var kills = new System.Windows.Controls.TextBlock
                {
                    Text = rankings[i].Kills.ToString() + " kills",
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888")),
                    FontSize = 10,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                System.Windows.Controls.Grid.SetColumn(kills, 2);

                row.Children.Add(medal);
                row.Children.Add(name);
                row.Children.Add(kills);

                PvpRankItems.Children.Add(row);
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

        private void HideToTray()
        {
            this.Hide();
            if (_trayIcon != null)
                _trayIcon.Visible = true;
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            if (_trayIcon != null)
                _trayIcon.Visible = false;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker1.IsBusy)
                return;

            // Limpiar el icono de la bandeja antes de cerrar
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            System.Windows.Application.Current.Shutdown(0);
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker1.IsBusy)
                return;

            // Safety check: abort if game is already running (anti-multiclient)
            if (Process.GetProcessesByName("game").Length > 0)
            {
                Button2.IsEnabled = false;
                Button2.Content = "JUEGO ACTIVO";
                return;
            }

            try
            {
                var fileName = Path.Combine(Directory.GetCurrentDirectory(), "game.exe");
                Process.Start(fileName, "start game");
                
                // Enviar el Launcher a la bandeja del sistema (System Tray)
                HideToTray();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
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
