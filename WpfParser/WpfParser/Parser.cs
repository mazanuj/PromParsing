using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfParser
{
    internal class Parser
    {
        public string ParseUrl { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public string FileName { get; set; }
        public bool Abort { get; set; } = true;

        private readonly HttpClient _client = new HttpClient();

        public event Action<string> PagesParseCompleted;
        public event Action<string> ParseLog;

        public async void ParsePages()
        {
            try
            {
                var html = await _client.GetStringAsync(ParseUrl);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var pagesCount = htmlDocument
                    .DocumentNode.Descendants("a")
                    .Where(node => node.GetAttributeValue("class", "")
                        .Equals("x-pager__item")).ToArray();

                PagesParseCompleted?.Invoke(pagesCount.Length != 0 ? pagesCount[pagesCount.Length - 1].InnerText : "0");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public async void StartParse()
        {
            if (StartPage > EndPage)
            {
                ParseLog?.Invoke("Error:     Начальная страница не может быть больше конечной!");
                return;
            }
            for (var i = StartPage; i <= EndPage; i++)
            {
                if (!Abort)
                {
                    ParseLog?.Invoke("Warning:     Сканирование прервано!");
                    return;
                }

                var html = await GetSourceByPage(i);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                Parse(htmlDocument);

                ParseLog?.Invoke($"OK:     Готова страница № {i}");
            }
            ParseLog?.Invoke("OK:     Все страницы просканированы!");
        }

        public async Task<string> GetSourceByPage(int page)
        {
            var currentUrl = ParseUrl + ";" + page;
            var html = await _client.GetStringAsync(currentUrl);

            return html;
        }

        public void Parse(HtmlDocument htmlDocument)
        {
            var items = htmlDocument
                .DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-gallery-tile__content")).ToArray();

            using (var writer = File.AppendText(FileName))
            {
                foreach (var item in items)
                {
                    writer.Write(item
                        .Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "")
                            .Equals("x-gallery-tile__name"))?.InnerText.Trim() + "\t", Encoding.UTF8);
                    writer.Write(item
                        .Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "")
                            .Contains("x-gallery-tile__price"))?.InnerText.Trim() + "\t", Encoding.UTF8);
                    writer.WriteLine(item
                        .Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "")
                            .Equals("x-gallery-tile__name"))?.GetAttributeValue("href", "") ?? throw new InvalidOperationException(), Encoding.UTF8);
                }
            }
        }
    }
}