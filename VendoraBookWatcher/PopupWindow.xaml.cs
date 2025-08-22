using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using VendoraBookWatcher.Models;

namespace VendoraBookWatcher
{
    public partial class PopupWindow : Window
    {
        private readonly List<BookItem> _items;
        private int _index = 0;

        public PopupWindow(List<BookItem> items)
        {
            InitializeComponent();
            _items = items;
            PositionBottomRight();
            Render();
        }

        private void PositionBottomRight()
        {
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            var area = SystemParameters.WorkArea;
            this.Left = area.Right - this.Width - 16;
            this.Top = area.Bottom - this.Height - 16;
        }

        private async void Render()
        {
            var it = _items[_index];
            TitleText.Text = it.Title;
            PriceText.Text = string.IsNullOrWhiteSpace(it.Price) ? "—" : it.Price;
            CounterText.Text = $"{_index + 1} / {_items.Count}";

            PrevBtn.IsEnabled = _index > 0;
            NextBtn.IsEnabled = _index < _items.Count - 1;

            BookImage.Source = null;

            if (!string.IsNullOrWhiteSpace(it.ImageUrl))
            {
                try
                {
                    string imgUrl = it.ImageUrl;

                    if (imgUrl.StartsWith("//")) imgUrl = "https:" + imgUrl;
                    else if (imgUrl.StartsWith("/")) imgUrl = "https://vendora.gr" + imgUrl;

                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imgUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.Default;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bitmap.EndInit();

                    // Επιβεβαιώνουμε ότι UI thread δέχεται assignment
                    BookImage.Source = bitmap;
                }
                catch
                {
                    BookImage.Source = null;
                }
            }
        }



        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_index > 0) { _index--; Render(); }
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_index < _items.Count - 1) { _index++; Render(); }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            var it = _items[_index];
            if (!string.IsNullOrWhiteSpace(it.Link))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(it.Link) { UseShellExecute = true });
                }
                catch { }
            }
        }

    }
}