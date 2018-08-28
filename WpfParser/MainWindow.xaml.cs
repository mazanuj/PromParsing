using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;

namespace WpfParser
{
    public partial class MainWindow
    {
        private readonly Parser _parser = new Parser();

        private readonly SaveFileDialog _dlg = new SaveFileDialog();

        public MainWindow()
        {
            InitializeComponent();
            var dataItemsLog = new ObservableCollection<LogItem>();
            DataGridLog.ItemsSource = dataItemsLog;
            _parser.OnLogResult += async result =>
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => dataItemsLog.Insert(0, result)));
                if (result.Result != "Все страницы просканированы!") return;
                UrlTextBox.IsEnabled = true;
                StartParseButton.IsEnabled = true;
                AbortButton.IsEnabled = false;
                _parser.EndPage = 1;
            };
        }

        private void StartParseButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _parser.ParseUrl = new UriBuilder(UrlTextBox.Text).Uri.AbsoluteUri;
                if (!UrlTextBox.Text.Contains("prom.ua")) throw new Exception("Парсер предназначен только для сайта Prom.ua");
                _parser.ParsePages();
                _dlg.FileName = _parser.ParseUrl.Substring(_parser.ParseUrl.IndexOf("prom.ua/", StringComparison.Ordinal) + 8, 
                    _parser.ParseUrl.Length - _parser.ParseUrl.IndexOf("prom.ua/", StringComparison.Ordinal) - 8);
                if (TxtRadioButton.IsChecked != null && (bool) TxtRadioButton.IsChecked)
                {
                    _dlg.DefaultExt = ".txt";
                    _dlg.Filter = "Text documents (.txt)|*.txt";
                }
                else
                {
                    _dlg.DefaultExt = ".xlsx";
                    _dlg.Filter = "Text documents (.xlsx)|*.xlsx";
                }
                if (_dlg.ShowDialog() != true) return;
                _parser.FileName = _dlg.FileName;
                _parser.RaiseOnResult("OK", "Начинаю сканирование.");
                _parser.Abort = false;
                AbortButton.IsEnabled = true;
                StartParseButton.IsEnabled = false;
                UrlTextBox.IsEnabled = false;
                _parser.StartParse();
            }
            catch (Exception exception)
            {
                _parser.RaiseOnResult("Error", exception.Message);
            }
        }

        private void AbortButton_OnClick(object sender, RoutedEventArgs e)
        {
            _parser.Abort = true;
            StartParseButton.IsEnabled = true;
            UrlTextBox.IsEnabled = true;
            AbortButton.IsEnabled = false;
        }

        private void LaunchPromParserOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/PromParsing");
        }
    }
}