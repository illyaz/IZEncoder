namespace IZEncoder.Common.Helper
{
    using System;
    using System.Windows.Forms;

    // Message Only Window
    internal sealed class GlobalWin32Message : NativeWindow
    {
        public delegate void MessageHandlerDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        public GlobalWin32Message(string caption = null)
        {
            Caption = caption ?? Guid.NewGuid().ToString().Replace("-", "");
            CreateHandle(new CreateParams {Caption = Caption});
        }

        public string Caption { get; set; }
        public event MessageHandlerDelegate MessageHandler;

        protected override void WndProc(ref Message m)
        {
            MessageHandler?.Invoke(m.HWnd, m.Msg, m.WParam, m.LParam);
            base.WndProc(ref m);
        }
    }
}