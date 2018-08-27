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

            StartParseButton.Visibility = Visibility.Hidden;
            StartPageBox.Visibility = Visibility.Hidden;
            EndPageBox.Visibility = Visibility.Hidden;
            StartLabel.Visibility = Visibility.Hidden;
            EndLabel.Visibility = Visibility.Hidden;
            AbortButton.Visibility = Visibility.Hidden;

            _parser.PagesParseCompleted += PagesParseCompleted;
            _parser.ParseLog += ParseLog;

            _dlg.DefaultExt = ".txt";
            _dlg.Filter = "Text documents (.txt)|*.txt";
        }

        private void PagesParseCompleted(string pagesCount)
        {
            var n = int.Parse(pagesCount);
            if (n == 0)
            {
                PagesCountLabel.Content = "Данный раздел содержит одну страницу";
                StartParseButton.Visibility = Visibility.Visible;
                _parser.StartPage = 1;
                _parser.EndPage = 1;
            }
            else
            {
                for (var i = 1; i <= n; i++)
                {
                    StartPageBox.Items.Add(i);
                    EndPageBox.Items.Add(i);
                }

                StartPageBox.Visibility = Visibility.Visible;
                EndPageBox.Visibility = Visibility.Visible;
                StartParseButton.Visibility = Visibility.Visible;
                StartLabel.Visibility = Visibility.Visible;
                EndLabel.Visibility = Visibility.Visible;
                AbortButton.Visibility = Visibility.Visible;
                PagesCountLabel.Content = $"Данный раздел содержит {n} страниц. С какой по какую страницу вы хотите сделать анализ?";
            }
        }

        private void ParseLog(string log)
        {
            LogTextBox.Text += log + '\n';
        }

        private void StartParsePage_Click(object sender, RoutedEventArgs e)
        {
            if(UrlTextBox.Text.Contains("http://") || UrlTextBox.Text.Contains("https://"))
            {
                _parser.ParseUrl = UrlTextBox.Text;
                _parser.ParsePages();
            }
            else
            {
                _parser.ParseUrl = "http://" + UrlTextBox.Text;
                _parser.ParsePages();
            }
        }

        private void StartParseButton_Click(object sender, RoutedEventArgs e)
        {
            _dlg.FileName = "Document";
            if (_dlg.ShowDialog() != true) return;
            _parser.FileName = _dlg.FileName;
            if(StartPageBox.Visibility == Visibility.Visible)
            {
                _parser.StartPage = (int) StartPageBox.SelectedItem;
                _parser.EndPage = (int) EndPageBox.SelectedItem;
            }
            _parser.StartParse();
        }

        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            _parser.Abort = false;
        }

        private void LogTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LogTextBox.ScrollToEnd();
        }
    }
}
