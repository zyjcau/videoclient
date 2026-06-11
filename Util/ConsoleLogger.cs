using System;
using HTTPServerLib;

namespace VideoClient.Util
{
    class ConsoleLogger : ILogger
    {
        public void Log(object message)
        {
            Console.WriteLine(message);
        }
    }
}
