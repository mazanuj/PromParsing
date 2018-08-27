using System;

namespace WpfParser
{
    public class LogItem
    {
        public string Status { get; set; }
        public string Result { get; set; }
        public string Date { get; } = DateTime.Now.ToString("HH:mm:ss");
    }
}