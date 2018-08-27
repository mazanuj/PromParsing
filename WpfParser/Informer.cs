using System;

namespace WpfParser
{
    public static class Informer
    {
        public static event Action<LogItem> OnLogResult;

        public static void RaiseOnResult(string result)
        {
            OnLogResult?.Invoke(new LogItem { Result = result });
        }
    }
}