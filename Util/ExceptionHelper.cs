using System;
using System.Windows.Forms;

namespace VideoClient.Util
{
    public class ExceptionHelper
    {
        public static event Action<int, Exception> ExceptionOver;

        public static void AddGlobalObserve()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException); //处理处理未捕获的异常 
            Application.ThreadException +=
                new System.Threading.ThreadExceptionEventHandler(Application_ThreadException); //处理UI线程异常  
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException); //处理非UI线程异常

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(0, (System.Exception) e.ExceptionObject);
        }

        //处理UI线程异常
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(1, e.Exception);
        }

        private static void HandleException(int crashType, System.Exception ex)
        {
            ExceptionOver(crashType, ex);
        }
    }
}