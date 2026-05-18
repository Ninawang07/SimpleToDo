using System;
using System.Threading;
using System.Windows.Forms;

namespace SimpleTodo
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool createdNew;
            using (var mutex = new Mutex(true, "SimpleTodo_Widget_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    // Another instance running — bring it to foreground
                    var hWnd = NativeMethods.FindWindow(null, "SimpleTodo");
                    if (hWnd != IntPtr.Zero)
                    {
                        if (!NativeMethods.IsWindowVisible(hWnd))
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
                        NativeMethods.SetForegroundWindow(hWnd);
                    }
                    return;
                }

                Application.Run(new MainForm());
            }
        }
    }
}
