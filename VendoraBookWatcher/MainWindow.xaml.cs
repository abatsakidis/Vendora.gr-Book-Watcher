using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms; // alias για το NotifyIcon
using VendoraBookWatcher.Services;
using VendoraBookWatcher.Models;

namespace VendoraBookWatcher
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly VendoraScraper _scraper;
        private readonly SeenStore _store;
        private readonly HttpClient _http;
        private readonly WinForms.NotifyIcon _tray;

        public MainWindow()
        {
            InitializeComponent();

            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) VendoraBookWatcher/1.0");
            _scraper = new VendoraScraper(_http);
            _store = new SeenStore();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick += async (_, __) => await PollAsync();
            _timer.Start();

            // tray icon (WinForms)
            _tray = new WinForms.NotifyIcon();
            _tray.Icon = new System.Drawing.Icon("vendora_icon.ico");
            _tray.Text = "Vendora Book Watcher";
            _tray.Visible = true;
            _tray.ContextMenuStrip = new WinForms.ContextMenuStrip();
            _tray.ContextMenuStrip.Items.Add("Άνοιγμα", null, (_, __) => this.ShowAndActivate());
            _tray.ContextMenuStrip.Items.Add("Έλεγχος τώρα", null, async (_, __) => await PollAsync(forcePopupWhenNone: false));
            _tray.ContextMenuStrip.Items.Add("Έξοδος", null, (_, __) =>
            {
                _tray.Visible = false;
                _http.Dispose();
                System.Windows.Application.Current.Shutdown();
            });
        }

        private async Task PollAsync(bool forcePopupWhenNone = false)
        {
            try
            {
                StatusText.Text = "Έλεγχος...";
                var books = await _scraper.FetchLatestAsync();

                var newOnes = new List<BookItem>();
                foreach (var b in books)
                {
                    if (!_store.HasSeen(b.Id))
                        newOnes.Add(b);
                }

                if (newOnes.Count > 0 || forcePopupWhenNone)
                {
                    if (newOnes.Count == 0)
                    {
                        // show sample demo if requested
                        newOnes.Add(new BookItem
                        {
                            Id = "demo",
                            Title = "Δοκιμαστικό Βιβλίο",
                            Price = "€10",
                            ImageUrl = "https://via.placeholder.com/256x256.png?text=Book"
                        });
                    }

                    var popup = new PopupWindow(newOnes);
                    popup.Closed += (_, __) =>
                    {
                        foreach (var n in newOnes)
                            _store.MarkSeen(n.Id);
                        _store.Save();
                    };
                    popup.Show();
                }

                StatusText.Text = $"Τελευταίος έλεγχος: {DateTime.Now:HH:mm:ss} — Νέα: {newOnes.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Σφάλμα: " + ex.Message;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalBox.Text, out int seconds) && seconds >= 10)
            {
                _timer.Interval = TimeSpan.FromSeconds(seconds);
                StatusText.Text = $"Το διάστημα ορίστηκε σε {seconds} δευτερόλεπτα";
            }
            else
            {
                StatusText.Text = "Δώσε έγκυρο αριθμό >= 10";
            }
        }

        private async void ShowPopupBtn_Click(object sender, RoutedEventArgs e)
        {
            await PollAsync(forcePopupWhenNone: true);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // Μην κλείνεις το app, απλά κρύψε το παράθυρο
            e.Cancel = true;
            this.Hide();

            // Balloon notification
            _tray.BalloonTipTitle = "Vendora Book Watcher";
            _tray.BalloonTipText = "Η εφαρμογή συνεχίζει να τρέχει στο tray.";
            _tray.ShowBalloonTip(2000);
        }

        private void ShowAndActivate()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
    }
}
