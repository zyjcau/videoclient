using System.ComponentModel;
using NLog;
using NLog.Fluent;

namespace VideoClient.Util
{
    public class XLog
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // public static void Info([Localizable(false)] string message)
        // {
        //     _logger.Info(message);
        // }
        // public static void Info([Localizable(false)] string message)
        // {
        //     _logger.Info(message);
        // }
    }
}