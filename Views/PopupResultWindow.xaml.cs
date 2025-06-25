using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace WordPopupApp.Views
{
    public partial class PopupResultWindow : Window
    {
        // P/Invoke for getting mouse position
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        };

        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        public PopupResultWindow()
        {
            InitializeComponent();
            // 订阅 Deactivated 事件,当窗口失去焦点时（比如用户点击了其他地方），这个事件就会被触发
            Deactivated += PopupResultWindow_Deactivated;
        }
        private void PopupResultWindow_Deactivated(object sender, EventArgs e)
        {
            // 当窗口失去焦点时，隐藏它
            // 我们加一个 IsVisible 的判断，防止不必要的重复隐藏操作
            if (this.IsVisible)
            {
                this.Hide();
            }
        }
        // 拦截用户点 × 或 Alt+F4
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;  // 取消真正关闭
            Hide();           // 只做隐藏
        }

        public void SetPositionAndShow()
        {
            ValidatePosition();
            Show();
            Activate();
        }

        private void ValidatePosition()
        {
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;

            if (Left + Width > screenWidth)
            {
                Left = screenWidth - Width - 15;
            }
            if (Top + Height > screenHeight)
            {
                Top = screenHeight - Height - 15;
            }
        }



    }
}