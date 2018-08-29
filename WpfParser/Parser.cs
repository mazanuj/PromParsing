using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace WpfParser
{
    internal class Parser
    {
        public string ParseUrl { get; set; }
        public int EndPage { private get; set; }
        public string FileName { private get; set; }
        public bool Abort { private get; set; }
        public string Proxy { private get; set; }

        private HttpClient _client;
        private HtmlDocument _htmlDocument;
        private HttpClientHandler _handler;
        private FileStream _writer;
        private XSSFWorkbook _xssf;
        private ISheet _sheet;
        private int _indexRow;

        private readonly Regex _regexPhone = new Regex(@"\+\d{3} \(\d{2}\) \d{3}-\d{2}-\d{2}");

        public event Action<LogItem> OnLogResult;

        public async void StartParse()
        {
            try
            {
                if (Proxy != null)
                {
                    _handler = new HttpClientHandler
                    {
                        Proxy = new WebProxy(Proxy),
                        UseProxy = true,
                    };
                }
                _client = Proxy != null ? new HttpClient(_handler) : new HttpClient();
                _htmlDocument = new HtmlDocument();
                _htmlDocument.LoadHtml(await _client.GetStringAsync(ParseUrl));
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
                        row.CreateCell(4).SetCellValue("Phone");
                    }
                    for (var i = 1; i <= EndPage; i++)
                    {
                        if (Abort)
                        {
                            if (FileName.Contains("xlsx"))
                                _xssf.Write(_writer);
                            OnLogResult?.Invoke(new LogItem {Status = "Warning", Result = "Сканирование прервано!"});
                            EndPage = 1;
                            return;
                        }
                        _htmlDocument = new HtmlDocument();
                        _htmlDocument.LoadHtml(await _client.GetStringAsync(ParseUrl + ";" + i));
                        var pageUrls = _htmlDocument
                            .DocumentNode.Descendants("div")
                            .Where(node => node.GetAttributeValue("class", "")
                                .Equals("x-gallery-tile__content")).ToArray();
                        var j = 1;
                        foreach (var pageUrl in pageUrls)
                        {
                            if (Abort)
                            {
                                if (FileName.Contains("xlsx"))
                                    _xssf.Write(_writer);
                                OnLogResult?.Invoke(new LogItem
                                {
                                    Status = "Warning",
                                    Result = "Сканирование прервано!"
                                });
                                EndPage = 1;
                                return;
                            }
                            var itemUrl = pageUrl
                                .Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "")
                                    .Equals("x-gallery-tile__name"))?.GetAttributeValue("href", "");
                            _htmlDocument = new HtmlDocument();
                            _htmlDocument.LoadHtml(await _client.GetStringAsync(itemUrl));
                            var items = _htmlDocument
                                .DocumentNode.Descendants("div")
                                .Where(node => node.GetAttributeValue("class", "")
                                    .Equals("x-product-info__content")).ToArray();
                            foreach (var item in items)
                            {
                                var itemName = item
                                    .Descendants("h1").FirstOrDefault(node => node
                                        .GetAttributeValue("class", "")
                                        .Equals("x-title"))?.InnerText.Trim();
                                var itemAvailability = item
                                    .Descendants("div").FirstOrDefault(node => node
                                        .GetAttributeValue("class", "")
                                        .Contains("x-product-presence"))?.InnerText.Trim();
                                var itemPrice = item
                                    .Descendants("a").FirstOrDefault(node => node
                                        .GetAttributeValue("class", "")
                                        .Equals(
                                            "js-product-buy-button x-button x-button_width_full x-button_size_xl x-button_theme_purple"))
                                    ?.GetAttributeValue("data-product-price", "").Replace("&nbsp;", " ");
                                var itemCode = item
                                    .Descendants("div").FirstOrDefault(node => node
                                        .GetAttributeValue("class", "")
                                        .Contains("x-product-info__identity-item"))?.InnerText.Trim()
                                    .Replace("&nbsp;", " ");
                                var itemPhone = item
                                    .Descendants("span").FirstOrDefault(node => node
                                        .GetAttributeValue("class", "")
                                        .Equals(
                                            "js-product-ad-conv-action x-pseudo-link x-iconed-text__link"))
                                    ?.GetAttributeValue("data-pl-phones", "");
                                var matches = _regexPhone.Matches(itemPhone ?? throw new InvalidOperationException());
                                if (matches.Count > 0)
                                {
                                    itemPhone = "";
                                    foreach (Match match in matches)
                                    {
                                        itemPhone += match.Value + ' ';
                                    }
                                }
                                if (!FileName.Contains("xlsx"))
                                {
                                    var outputText = itemName + '\t' + itemAvailability + '\t' + itemPrice + '\t' +
                                                     itemCode + '\t' + itemPhone + '\n';
                                    var info = new UTF8Encoding(true).GetBytes(outputText);
                                    _writer?.Write(info, 0, info.Length);
                                }
                                else
                                {
                                    _indexRow++;
                                    var row = _sheet.CreateRow(_indexRow);
                                    row.CreateCell(0).SetCellValue(itemName);
                                    row.CreateCell(1).SetCellValue(itemAvailability);
                                    row.CreateCell(2).SetCellValue(itemPrice);
                                    row.CreateCell(3).SetCellValue(itemCode);
                                    row.CreateCell(4).SetCellValue(itemPhone);
                                    _sheet.AutoSizeColumn(0);
                                    _sheet.AutoSizeColumn(1);
                                    _sheet.AutoSizeColumn(2);
                                    _sheet.AutoSizeColumn(3);
                                    _sheet.AutoSizeColumn(4);
                                }
                            }
                            OnLogResult?.Invoke(new LogItem
                            {
                                Status = "OK",
                                Result = $"Готова товар № {j++} на странице {i} из {EndPage}"
                            });
                        }
                        OnLogResult?.Invoke(new LogItem
                        {
                            Status = "OK",
                            Result = $"Готова страница № {i} из {EndPage}"
                        });
                    }
                    if (FileName.Contains("xlsx"))
                        _xssf.Write(_writer);
                }
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem {Status = "Error", Result = exception.Message});
                return;
            }
            OnLogResult?.Invoke(new LogItem { Status = "OK", Result = "Все страницы просканированы!"});
        }

        public void RaiseOnResult(string status, string result)
        {
            OnLogResult?.Invoke(new LogItem { Status = status, Result = result });
        }
    }
}