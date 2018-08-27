using System;

namespace WpfParser
{
    public static class Informer
    {
        //public delegate void StateChanged();

        //public delegate void ProgressChanged(double result);

        //public delegate void InformMethod(LogItem result);

        public static event Action<LogItem> OnLogResult;
        //public static event StateChanged OnStateChanged;
        //public static event ProgressChanged OnProgressChangedAll;
        //public static event ProgressChanged OnProgressChangedCurrent;

        //public static void RaiseOnProgressChangedCurrent(double result)
        //{
        //    var handler = OnProgressChangedCurrent;
        //    handler?.Invoke(result);
        //}

        //public static void RaiseOnProgressChangedAll(double result)
        //{
        //    var handler = OnProgressChangedAll;
        //    handler?.Invoke(result);
        //}

        //public static void RaiseOnStateChanged()
        //{
        //    var handler = OnStateChanged;
        //    handler?.Invoke();
        //}

        public static void RaiseOnResult(string result)
        {
            OnLogResult?.Invoke(new LogItem { Result = result });
        }
    }
}
