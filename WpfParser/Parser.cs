using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WpfParser
{
    internal class Parser
    {
        public string ParseUrl { get; set; }
        public int StartPage { private get; set; } = 1;
        public int EndPage { private get; set; }
        public string FileName { private get; set; }
        public bool Abort { private get; set; }

        private readonly HttpClient _client = new HttpClient();
        private StreamWriter _writer;

        public event Action<LogItem> OnLogResult;

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
                EndPage = pagesCount.Length != 0 ? int.Parse(pagesCount[pagesCount.Length - 1].InnerText) : 0;
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
            }
        }

        public async void StartParse()
        {
            using (_writer = File.CreateText(FileName))
            {
                for (var i = StartPage; i <= EndPage; i++)
                {
                    if (Abort)
                    {
                        OnLogResult?.Invoke(new LogItem { Status = "Warning", Result = "Сканирование прервано!" });
                        return;
                    }
                    var html = await GetSourceByPage(i);
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(html);
                    Parse(htmlDocument);
                    OnLogResult?.Invoke(new LogItem { Status = "OK", Result = $"Готова страница № {i} из {EndPage}" });
                }
            }
            OnLogResult?.Invoke(new LogItem { Status = "OK", Result = "Все страницы просканированы!"});
        }

        private async Task<string> GetSourceByPage(int page)
        {
            var currentUrl = ParseUrl + ";" + page;
            var html = await _client.GetStringAsync(currentUrl);
            return html;
        }

        private void Parse(HtmlDocument htmlDocument)
        {
            var items = htmlDocument
                .DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-gallery-tile__content")).ToArray();
            try
            {
                foreach (var item in items)
                {
                    _writer?.Write(item
                                       .Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "")
                                           .Equals("x-gallery-tile__name"))?.InnerText.Trim() + "\t", Encoding.UTF8);
                    _writer?.Write(item
                                       .Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "")
                                           .Contains("x-gallery-tile__price"))?.InnerText.Trim() + "\t", Encoding.UTF8);
                    _writer?.WriteLine(item
                                           .Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "")
                                               .Equals("x-gallery-tile__name"))?.GetAttributeValue("href", "")
                                       ?? throw new InvalidOperationException(), Encoding.UTF8);
                }
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
            }
        }

        public void RaiseOnResult(string status, string result)
        {
            OnLogResult?.Invoke(new LogItem { Status = status, Result = result });
        }
    }
}