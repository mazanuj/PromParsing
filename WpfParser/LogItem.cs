using System;

namespace WpfParser
{
    public class LogItem
    {
        public string Result { get; set; }
        public string Date { get; } = DateTime.Now.ToString("HH:mm:ss");
    }
}
