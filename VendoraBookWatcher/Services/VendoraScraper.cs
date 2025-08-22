using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VendoraBookWatcher.Models;
using HAP = HtmlAgilityPack; // alias για να μην μπερδεύεται με WinForms HtmlDocument

namespace VendoraBookWatcher.Services
{
    public class VendoraScraper
    {
        private readonly HttpClient _http;

        // Πάντα "Νεότερες"
        private const string Url = "https://vendora.gr/browse/4v377r/metaxeirismena-biblia.html?sort=recent";

        private static readonly Regex IdRx = new Regex(@"/items/([^/]+)/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PriceRx = new Regex(@"€\s*[\d\.\,]+", RegexOptions.Compiled);

        public VendoraScraper(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<BookItem>> FetchLatestAsync()
        {
            var html = await _http.GetStringAsync(Url);

            var doc = new HAP.HtmlDocument();
            doc.LoadHtml(html);

            var list = new List<BookItem>();

            // Πάρε όλα τα links που δείχνουν σε /items/<id>/...
            var links = doc.DocumentNode.SelectNodes("//a[contains(@href, '/items/')]");
            if (links == null) return list;

            var deDupe = new HashSet<string>(); // για διπλές κάρτες του ίδιου item

            foreach (var a in links)
            {
                var href = a.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href)) continue;

                var m = IdRx.Match(href);
                if (!m.Success) continue;

                var id = m.Groups[1].Value;
                if (!deDupe.Add(id)) continue; // κράτα μόνο 1 φορά το κάθε id

                // Τίτλος
                var title = HAP.HtmlEntity.DeEntitize(a.InnerText ?? string.Empty).Trim();
                title = Regex.Replace(title, @"\s+", " ");
                if (string.IsNullOrWhiteSpace(title)) title = "Χωρίς τίτλο";

                // Τιμή (πρώτα στο anchor, μετά στον γονέα)
                var priceText = a.InnerText ?? string.Empty;
                var priceMatch = PriceRx.Match(priceText);
                if (!priceMatch.Success)
                {
                    var parentText = a.ParentNode?.InnerText ?? "";
                    priceMatch = PriceRx.Match(parentText);
                }
                var price = priceMatch.Success ? priceMatch.Value.Trim() : "—";

                // Εικόνα (src / data-src / data-original)
                var img = a.Descendants("img").FirstOrDefault();
                string imgUrl = null;
                if (img != null)
                {
                    imgUrl = img.GetAttributeValue("src", null)
                          ?? img.GetAttributeValue("data-src", null)
                          ?? img.GetAttributeValue("data-original", null);
                    if (imgUrl != null && imgUrl.StartsWith("//")) imgUrl = "https:" + imgUrl;
                    if (imgUrl != null && imgUrl.StartsWith("/")) imgUrl = "https://vendora.gr" + imgUrl;
                }

                list.Add(new BookItem
                {
                    Id = id,                           // σταθερό μοναδικό id από το URL
                    Title = title,
                    Price = price,
                    ImageUrl = imgUrl ?? "https://via.placeholder.com/256x256.png?text=Book",
                    // Αν στο μοντέλο σου έχεις και Link/Url property, μπορείς να το βάλεις εδώ.
                    Link = href.StartsWith("http") ? href : "https://vendora.gr" + href
                });
            }

            return list;
        }
    }
}
