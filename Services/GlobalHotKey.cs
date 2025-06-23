// 需要引用 System.Windows.Forms
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Clipboard = System.Windows.Clipboard;

namespace WordPopupApp.Services
{
    public class GlobalHotKey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 使用 SendKeys 模拟 Ctrl+C
        public static void SimulateCtrlC()
        {
            Thread.Sleep(100); // 等待一下，确保焦点在选中的文本上
            SendKeys.SendWait("^c");
            Thread.Sleep(100); // 等待剪贴板内容更新
        }

        public static string GetTextFromClipboard()
        {
            for (int i = 0; i < 5; i++) // 尝试几次以防万一
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        return Clipboard.GetText();
                    }
                }
                catch (COMException)
                {
                    // 剪贴板正被其他进程占用，稍等后重试
                    Thread.Sleep(50);
                }
            }
            return string.Empty;
        }


        private HwndSource _source;
        private readonly int _id;
        private readonly Window _window;

        public event Action HotKeyPressed;

        public GlobalHotKey(Window window, uint modifier, uint key)
        {
            _window = window;
            _id = GetHashCode();

            var helper = new WindowInteropHelper(_window);
            helper.EnsureHandle();
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            if (!RegisterHotKey(_source.Handle, _id, modifier, key))
            {
                throw new InvalidOperationException("无法注册热键。");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
            {
                HotKeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_source.Handle, _id);
        }
    }

    // 定义修饰键常量
    public static class HotKeyModifiers
    {
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
    }
}