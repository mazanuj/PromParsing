using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace WpfParser
{
    public partial class MainWindow
    {
        private readonly Parser _parser = new Parser();

        private readonly SaveFileDialog _dlg =
            new SaveFileDialog {DefaultExt = ".txt", Filter = "Text documents (.txt)|*.txt"};

        public MainWindow()
        {
            InitializeComponent();
            var dataItemsLog = new ObservableCollection<LogItem>();
            DataGridLog.ItemsSource = dataItemsLog;
            _parser.OnLogResult += async result =>
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => dataItemsLog.Insert(0, result)));
                if (result.Result == "Все страницы просканированы!")
                {
                    StartParseButton.IsEnabled = true;
                    StartParsePagesButton.IsEnabled = true;
                }
            };
            _parser.PagesParseCompleted += PagesParseCompleted;
        }

        private void PagesParseCompleted(int pagesCount)
        {
            switch (pagesCount)
            {
                case 0:
                    PagesCountLabel.Content = "Данный раздел содержит одну страницу";
                    _parser.EndPage = 1;
                    break;
                default:
                    PagesCountLabel.Content = $"Данный раздел содержит {pagesCount} страниц.";
                    _parser.EndPage = pagesCount;
                    break;
            }
            PagesCountLabel.Visibility = Visibility.Visible;
            StartParseButton.IsEnabled = true;
            AbortButton.IsEnabled = true;
            _parser.RaiseOnResult("OK", "Анализ количества страниц окончен.");
        }

        private void UrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            PagesCountLabel.Visibility = Visibility.Hidden;
            StartParseButton.IsEnabled = false;
            AbortButton.IsEnabled = false;
        }

        private void StartParsePagesButton_OnClick(object sender, RoutedEventArgs e)
        {
            _parser.RaiseOnResult("OK", "Начинаю анализ количества страниц.");
            try
            {
                if (!UrlTextBox.Text.Contains("prom.ua")) throw new Exception("Парсер предназначен только для сайта Prom.ua");
                _parser.ParseUrl = new UriBuilder(UrlTextBox.Text).Uri.AbsoluteUri;
                _parser.ParsePages();
            }
            catch (Exception exception)
            {
                _parser.RaiseOnResult("Error", exception.Message);
            }
        }

        private void StartParseButton_OnClick(object sender, RoutedEventArgs e)
        {
            _dlg.FileName = string.Empty;
            if (_dlg.ShowDialog() != true) return;
            _parser.FileName = _dlg.FileName;
            _parser.RaiseOnResult("OK", "Начинаю сканирование.");
            _parser.Abort = false;
            StartParseButton.IsEnabled = false;
            StartParsePagesButton.IsEnabled = false;
            _parser.StartParse();
        }

        private void AbortButton_OnClick(object sender, RoutedEventArgs e)
        {
            _parser.Abort = true;
            StartParseButton.IsEnabled = true;
            StartParsePagesButton.IsEnabled = true;
        }

        private void LaunchOnlineTranslationsOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/PromParsing");
        }
    }
}