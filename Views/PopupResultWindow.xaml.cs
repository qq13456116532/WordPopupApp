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
        }

        public void SetPositionAndShow()
        {
            var mousePosition = GetMousePosition();
            this.Left = mousePosition.X + 15; // 在鼠标右侧显示
            this.Top = mousePosition.Y + 15;  // 在鼠标下方显示

            // 确保窗口不会超出屏幕边界
            ValidatePosition();

            this.Show();
            this.Activate();
        }

        private void ValidatePosition()
        {
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;

            if (this.Left + this.Width > screenWidth)
            {
                this.Left = screenWidth - this.Width - 15;
            }
            if (this.Top + this.Height > screenHeight)
            {
                this.Top = screenHeight - this.Height - 15;
            }
        }


        // 窗口失去焦点时自动关闭
        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            this.Close();
        }

        // 鼠标离开窗口时自动关闭（提供多一种关闭方式）
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Close();
        }
    }
}