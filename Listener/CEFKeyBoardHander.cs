using System;
using System.Windows.Forms;
using CefSharp;
using VideoClient.UI;

namespace VidyoClient
{
    public class CEFKeyBoardHander : IKeyboardHandler
    {
        private CefForm _formCef;

        private IOnFastKeyClick _onFastKeyClick;

        public CEFKeyBoardHander()
        {
        }

        public CEFKeyBoardHander(CefForm formCef)
        {
            _formCef = formCef;
        }

        public CEFKeyBoardHander(IOnFastKeyClick onFastKeyClick)
        {
            _onFastKeyClick = onFastKeyClick;
        }

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type,
            int windowsKeyCode,
            int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            return false;
        }

        public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode,
            int nativeKeyCode,
            CefEventFlags modifiers, bool isSystemKey)
        {
            if (type == KeyType.KeyUp && Enum.IsDefined(typeof(Keys), windowsKeyCode))
            {
                var key = (Keys)windowsKeyCode;
                switch (key)
                {
                    case Keys.F5:
                        if (modifiers == CefEventFlags.ControlDown)
                        {
                            browser.Reload(true); //强制忽略缓存
                        }
                        else
                        {
                            browser.Reload();
                        }

                        break;
                    case Keys.F10:
                    case Keys.F11:
                        if (modifiers == CefEventFlags.ControlDown)
                        {
                            _onFastKeyClick?.OnWindowsKeyClick(windowsKeyCode);
                        }

                        break;
                    case Keys.F12:
                        if (modifiers == CefEventFlags.ControlDown) browser.ShowDevTools();
                        break;
                    case Keys.PageDown:
                    case Keys.PageUp:
                        return true;
                }
            }

            return false;
        }

        private IAsyncResult RunOnUIThread(Delegate method)
        {
            return _formCef?.BeginInvoke(method);
        }

        public interface IOnFastKeyClick
        {
            void OnWindowsKeyClick(int windowsKeyCode);
        }
    }
}