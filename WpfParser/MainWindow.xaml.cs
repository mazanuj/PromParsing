using System;
using System.Collections.ObjectModel;
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

        public ObservableCollection<LogItem> DataItemsLog { get; } = new ObservableCollection<LogItem>();

        public MainWindow()
        {
            InitializeComponent();
            Hidden();
            _parser.PagesParseCompleted += PagesParseCompleted;

            DataGridLog.ItemsSource = DataItemsLog;

            Informer.OnLogResult += async result =>
            {
                await Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => DataItemsLog.Insert(0, result)));
            };
        }

        private void Hidden()
        {
            PagesCountLabel.Visibility = Visibility.Hidden;
            StartParseButton.Visibility = Visibility.Hidden;
            AbortButton.Visibility = Visibility.Hidden;
        }

        private void PagesParseCompleted(int pagesCount)
        {
            switch (pagesCount)
            {
                case 0:
                    PagesCountLabel.Content = "Данный раздел содержит одну страницу";
                    Informer.RaiseOnResult("Анализ количества страниц окончен.");
                    PagesCountLabel.Visibility = Visibility.Visible;
                    StartParseButton.Visibility = Visibility.Visible;
                    _parser.StartPage = 1;
                    _parser.EndPage = 1;
                    break;
                default:
                    _parser.StartPage = 1;
                    _parser.EndPage = pagesCount;
                    PagesCountLabel.Content = $"Данный раздел содержит {pagesCount} страниц.";
                    Informer.RaiseOnResult("Анализ количества страниц окончен.");
                    PagesCountLabel.Visibility = Visibility.Visible;
                    StartParseButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ParsePages()
        {
            try
            {
                _parser.ParseUrl = new UriBuilder(UrlTextBox.Text).Uri.AbsoluteUri;
                _parser.ParsePages();
            }
            catch (Exception)
            {
                Informer.RaiseOnResult("Невозможно выполнить разбор имени хоста!");
            }
        }

        private void Parse()
        {
            _dlg.FileName = string.Empty;
            if (_dlg.ShowDialog() != true) return;
            _parser.FileName = _dlg.FileName;

            _parser.Abort = false;
            AbortButton.Visibility = Visibility.Visible;
            _parser.StartParse();
        }

        private void UrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => Hidden();

        private void StartParsePagesButton_OnClick(object sender, RoutedEventArgs e)
        {
            Informer.RaiseOnResult("Начинаю анализ количества страниц.");
            ParsePages();
        }

        private void StartParseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Informer.RaiseOnResult("Начинаю сканирование указанных страниц.");
            Parse();
        }

        private void AbortButton_OnClick(object sender, RoutedEventArgs e) => _parser.Abort = true;
    }
}