using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        private WebClient _client;
        private HtmlDocument _htmlDocument;
        private FileStream _writer;
        private readonly XSSFWorkbook _xssf = new XSSFWorkbook();
        private ISheet _sheet;
        private int _indexRow;
        private string _url;

        private readonly Regex _regexPhone = new Regex(@"\+\d{3} \(\d{2}\) \d{3}-\d{2}-\d{2}");

        public event Action<LogItem> OnLogResult;

        public void RaiseOnResult(string status, string result)
        {
            OnLogResult?.Invoke(new LogItem { Status = status, Result = result });
        }

        public void Start()
        {
            try
            {
                _client = Proxy != null
                    ? new WebClient { Proxy = new WebProxy(Proxy), Encoding = Encoding.UTF8 }
                    : new WebClient { Encoding = Encoding.UTF8 };
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
                return;
            }
            CheckCategory(ParseUrl);
        }

        private void CheckCategory(string categoryUrl)
        {
            if (Abort) return;
            _htmlDocument = new HtmlDocument();
            try
            {
                Thread.Sleep(1000);
                _url = _client.DownloadString(categoryUrl);
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
                return;
            }
            _htmlDocument.LoadHtml(_url);
            var categories = _htmlDocument
                .DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-category-tile__content")).ToArray();
            if (categories.Length == 0) StartParse(categoryUrl);
            else
            {
                foreach (var category in categories)
                {
                    var nextCategoryUrl = category
                        .Descendants("a").FirstOrDefault(node => node
                            .GetAttributeValue("class", "")
                            .Equals("x-category-tile__title"))?.GetAttributeValue("href", "");
                    CheckCategory("https://prom.ua" + nextCategoryUrl);
                }
            }
        }


        private void StartParse(string currentUrl)
        {
            if (Abort) return;
            ParsePagesCount(currentUrl);
            using (_writer = File.Create(FileName))
            {
                if (FileName.Contains("xlsx"))
                    InitXlsxDoc();
                for (var i = 1; i <= EndPage; i++)
                {
                    ParsePageLinks(ref i, currentUrl);
                }
                if (Abort) return;
                if (FileName.Contains("xlsx"))
                    _xssf.Write(_writer);
            }
            OnLogResult?.Invoke(new LogItem { Status = "OK", Result = "Все страницы просканированы!"});
        }

        private void ParsePagesCount(string currentUrl)
        {
            _htmlDocument = new HtmlDocument();
            try
            {
                Thread.Sleep(1000);
                _url = _client.DownloadString(currentUrl);
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
                return;
            }
            _htmlDocument.LoadHtml(_url);
            var pagesCount = _htmlDocument
                .DocumentNode.Descendants("a")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-pager__item")).ToArray();
            EndPage = pagesCount.Length != 0 ? int.Parse(pagesCount[pagesCount.Length - 1].InnerText) : 1;
        }

        private void InitXlsxDoc()
        {
            _sheet = _xssf.CreateSheet("Prom UA");
            var row = _sheet.CreateRow(_indexRow);
            row.CreateCell(0).SetCellValue("Name");
            row.CreateCell(1).SetCellValue("Availability");
            row.CreateCell(2).SetCellValue("Price");
            row.CreateCell(3).SetCellValue("Code");
            row.CreateCell(4).SetCellValue("Phone");
        }

        private void AbortParser()
        {
            if (FileName.Contains("xlsx"))
                _xssf.Write(_writer);
            OnLogResult?.Invoke(new LogItem { Status = "Warning", Result = "Сканирование прервано!" });
            EndPage = 1;
        }

        private void ParsePageLinks(ref int i, string currentUrl)
        {
            _htmlDocument = new HtmlDocument();
            try
            {
                Thread.Sleep(1000);
                _url = _client.DownloadString(currentUrl + ";" + i);
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
                return;
            }
            _htmlDocument.LoadHtml(_url);
            var pageUrls = _htmlDocument
                .DocumentNode.Descendants("a")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-gallery-tile__name")).ToArray();
            var j = 1;
            foreach (var pageUrl in pageUrls)
            {
                if (Abort)
                {
                    AbortParser();
                    return;
                }
                var itemUrl = pageUrl.GetAttributeValue("href", "");
                ParseItem(ref itemUrl, ref i, ref j);
            }
            OnLogResult?.Invoke(new LogItem { Status = "OK", Result = $"Готова страница № {i} из {EndPage}" });
        }

        private void ParseItem(ref string itemUrl, ref int i, ref int j)
        {
            _htmlDocument = new HtmlDocument();
            try
            {
                Thread.Sleep(1000);
                _url = _client.DownloadString(itemUrl);
            }
            catch (Exception exception)
            {
                OnLogResult?.Invoke(new LogItem { Status = "Error", Result = exception.Message });
                return;
            }
            _htmlDocument.LoadHtml(_url);
            var items = _htmlDocument
                .DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("x-product-page")).ToArray();
            foreach (var item in items)
            {
                var itemName = item
                    .Descendants("h1").FirstOrDefault(node => node
                        .GetAttributeValue("class", "")
                        .Equals("x-title"))?.InnerText.Trim().Replace("&#34;", "\"").Replace("&amp;", "&");
                var itemAvailability = item
                    .Descendants("div").FirstOrDefault(node => node
                        .GetAttributeValue("class", "")
                        .Contains("x-product-presence"))?.InnerText.Trim();
                var itemPrice = item
                    .Descendants("div").FirstOrDefault(node => node
                        .GetAttributeValue("class", "")
                        .Equals("x-product-sticky__price"))?.InnerText.Trim();
                var itemCode = item
                    .Descendants("div").FirstOrDefault(node => node
                        .GetAttributeValue("class", "")
                        .Contains("x-product-info__identity-item"))?.InnerText.Trim().Replace("&nbsp;", " ");
                var itemPhone = item
                    .Descendants("span").FirstOrDefault(node => node
                        .GetAttributeValue("class", "")
                        .Contains(
                            "js-product-ad-conv-action x-pseudo-link"))
                    ?.GetAttributeValue("data-pl-phones", "");
                WrieteResult(ref itemName, ref itemAvailability, ref itemPrice, ref itemCode, ref itemPhone);
            }
            OnLogResult?.Invoke(new LogItem { Status = "OK", Result = $"Готова товар № {j++} на странице {i} из {EndPage}" });
        }

        private void WrieteResult(ref string itemName, ref string itemAvailability, 
            ref string itemPrice, ref string itemCode, ref string itemPhone)
        {
            if (itemPhone != null)
            {
                var matches = _regexPhone.Matches(itemPhone);
                if (matches.Count > 0)
                {
                    itemPhone = "";
                    foreach (Match match in matches)
                    {
                        itemPhone += match.Value + ' ';
                    }
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
    }
}