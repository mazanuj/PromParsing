using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace WpfParser
{
    internal class Parser
    {
        public string ParseUrl { get; set; }
        public int EndPage { private get; set; } = 1;
        public string FileName { private get; set; }
        public bool Abort { private get; set; }

        private readonly HttpClient _client = new HttpClient();
        private FileStream _writer;
        private XSSFWorkbook _xssf;
        private ISheet _sheet;
        private int _indexRow;

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
            using (_writer = File.Create(FileName))
            {
                if (FileName.Contains("xlsx"))
                {
                    _xssf = new XSSFWorkbook();
                    _sheet = _xssf.CreateSheet("Prom UA");
                    _indexRow = 0;
                    var row = _sheet.CreateRow(_indexRow);
                    row.CreateCell(0).SetCellValue("Name");
                    row.CreateCell(1).SetCellValue("Availability");
                    row.CreateCell(2).SetCellValue("Price");
                    //row.CreateCell(3).SetCellValue("Code");
                }
                for (var i = 1; i <= EndPage; i++)
                {
                    if (Abort)
                    {
                        if (FileName.Contains("xlsx"))
                            _xssf.Write(_writer);
                        OnLogResult?.Invoke(new LogItem { Status = "Warning", Result = "Сканирование прервано!" });
                        EndPage = 1;
                        return;
                    }
                    var html = await GetSourceByPage(i);
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(html);
                    Parse(htmlDocument);
                    OnLogResult?.Invoke(new LogItem { Status = "OK", Result = $"Готова страница № {i} из {EndPage}" });
                }
                if(FileName.Contains("xlsx"))
                    _xssf.Write(_writer);
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
            if (!FileName.Contains("xlsx"))
            {
                foreach (var item in items)
                {
                    var text = item
                                   .Descendants("a").FirstOrDefault(node => node
                                       .GetAttributeValue("class", "")
                                       .Equals("x-gallery-tile__name"))?.InnerText.Trim() + "\t" +
                               item
                                   .Descendants("div").FirstOrDefault(node => node
                                       .GetAttributeValue("class", "")
                                       .Contains("x-product-presence"))?.InnerText.Trim() + "\t" +
                               item
                                   .Descendants("div").FirstOrDefault(node => node
                                       .GetAttributeValue("class", "")
                                       .Contains("x-gallery-tile__price"))?.InnerText.Trim() + "\n";
                    var info = new UTF8Encoding(true).GetBytes(text);
                    _writer?.Write(info, 0, info.Length);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    _indexRow++;
                    var row = _sheet.CreateRow(_indexRow);
                    row.CreateCell(0)
                        .SetCellValue(item
                            .Descendants("a").FirstOrDefault(node => node
                                .GetAttributeValue("class", "")
                                .Equals("x-gallery-tile__name"))?.InnerText.Trim());
                    row.CreateCell(1)
                        .SetCellValue(item
                            .Descendants("div").FirstOrDefault(node => node
                                .GetAttributeValue("class", "")
                                .Contains("x-product-presence"))?.InnerText.Trim());
                    row.CreateCell(2)
                        .SetCellValue(item
                            .Descendants("div").FirstOrDefault(node => node
                                .GetAttributeValue("class", "")
                                .Contains("x-gallery-tile__price"))?.InnerText.Trim());
                }
            }
        }

        public void RaiseOnResult(string status, string result)
        {
            OnLogResult?.Invoke(new LogItem { Status = status, Result = result });
        }
    }
}