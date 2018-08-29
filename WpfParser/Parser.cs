using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        private readonly HtmlDocument _htmlDocument = new HtmlDocument();
        private FileStream _writer;
        private XSSFWorkbook _xssf;
        private ISheet _sheet;
        private int _indexRow;

        public event Action<LogItem> OnLogResult;

        public async void StartParse()
        {
            try
            {
                _htmlDocument.LoadHtml(await _client.GetStringAsync(ParseUrl));
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
                return;
            }
            var pagesCount = _htmlDocument
                .DocumentNode.Descendants("a")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-pager__item")).ToArray();
            EndPage = pagesCount.Length != 0 ? int.Parse(pagesCount[pagesCount.Length - 1].InnerText) : 1;
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
                    row.CreateCell(3).SetCellValue("Code");
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
                    _htmlDocument.LoadHtml(await _client.GetStringAsync(ParseUrl + ";" + i));
                    var pageUrls = _htmlDocument
                        .DocumentNode.Descendants("div")
                        .Where(node => node.GetAttributeValue("class", "")
                            .Equals("x-gallery-tile__content")).ToArray();

                    foreach (var pageUrl in pageUrls)
                    {
                        var itemUrl = pageUrl
                            .Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "")
                                .Equals("x-gallery-tile__name"))?.GetAttributeValue("href", "");
                        _htmlDocument.LoadHtml(await _client.GetStringAsync(itemUrl));
                        var items = _htmlDocument
                            .DocumentNode.Descendants("div")
                            .Where(node => node.GetAttributeValue("class", "")
                                .Equals("x-product-info__content")).ToArray();
                        if (!FileName.Contains("xlsx"))
                        {
                            foreach (var item in items)
                            {
                                var text = item
                                               .Descendants("h1").FirstOrDefault(node => node
                                                   .GetAttributeValue("class", "")
                                                   .Equals("x-title"))?.InnerText.Trim() + '\t' +
                                           item
                                               .Descendants("div").FirstOrDefault(node => node
                                                   .GetAttributeValue("class", "")
                                                   .Contains("x-product-presence"))?.InnerText.Trim() + '\t' +
                                           item
                                               .Descendants("div").FirstOrDefault(node => node
                                                   .GetAttributeValue("class", "")
                                                   .Contains("x-product-price__value"))?.InnerText.Trim() + '\t' +
                                           item
                                               .Descendants("div").FirstOrDefault(node => node
                                                   .GetAttributeValue("class", "")
                                                   .Contains("x-product-info__identity-item"))?.InnerText.Trim() + '\n';
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
                                        .Descendants("h1").FirstOrDefault(node => node
                                            .GetAttributeValue("class", "")
                                            .Equals("x-title"))?.InnerText.Trim());
                                row.CreateCell(1)
                                    .SetCellValue(item
                                        .Descendants("div").FirstOrDefault(node => node
                                            .GetAttributeValue("class", "")
                                            .Contains("x-product-presence"))?.InnerText.Trim());
                                row.CreateCell(2)
                                    .SetCellValue(item
                                        .Descendants("div").FirstOrDefault(node => node
                                            .GetAttributeValue("class", "")
                                            .Contains("x-product-price__value"))?.InnerText.Trim());
                                row.CreateCell(3)
                                    .SetCellValue(item
                                        .Descendants("div").FirstOrDefault(node => node
                                            .GetAttributeValue("class", "")
                                            .Contains("x-product-info__identity-item"))?.InnerText.Trim());
                            }
                        }
                    }
                    OnLogResult?.Invoke(new LogItem { Status = "OK", Result = $"Готова страница № {i} из {EndPage}" });
                }
                if(FileName.Contains("xlsx"))
                    _xssf.Write(_writer);
            }
            OnLogResult?.Invoke(new LogItem { Status = "OK", Result = "Все страницы просканированы!"});
        }

        public void RaiseOnResult(string status, string result)
        {
            OnLogResult?.Invoke(new LogItem { Status = status, Result = result });
        }
    }
}