using Spectre.Console;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace NowPlayingTUI
{
    class Program
    {
        // Define constants for modifying window styles
        private const int GWL_STYLE = -16;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_SIZEBOX = WS_THICKFRAME;
        private const uint WS_VSCROLL = 0x00200000;
        private const uint WS_HSCROLL = 0x00100000;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private StateManager _stateManager;

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException((Exception)e.ExceptionObject);
            new Program().Run(args).GetAwaiter().GetResult();
        }

        private static void LogException(Exception ex)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"[{DateTime.Now}] Exception occurred:");
                writer.WriteLine($"Message: {ex.Message}");
                writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                writer.WriteLine("--------------------------------------------------");
            }
            Console.WriteLine($"Exception logged to {logFilePath}");
        }

        private class Options
        {
            [Option('x', "xcoord", Required = false, Default = -790)]
            public int X { get; set; }

            [Option('y', "ycoord", Required = false, Default = 0)]
            public int Y { get; set; }
        }

        private async Task Run(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                AdjustConsoleWindow(o.X, o.Y);
            });

            Console.Title = "Now Playing TUI";
            Console.CursorVisible = false;
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            Console.BufferHeight = Console.WindowHeight = 7;
            Console.BufferWidth = Console.WindowWidth;

            _stateManager = new StateManager();
            await _stateManager.Start();
            await Task.Delay(-1);
        }

        private void AdjustConsoleWindow(int x, int y)
        {
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow == IntPtr.Zero)
            {
                Console.WriteLine("Failed to get the console window handle.");
                return;
            }

            uint style = GetWindowLong(consoleWindow, GWL_STYLE);
            style &= ~(WS_VSCROLL | WS_SYSMENU | WS_BORDER | WS_CAPTION | WS_HSCROLL | WS_THICKFRAME | WS_SIZEBOX);
            //style |= WS_BORDER;

            SetWindowLong(consoleWindow, GWL_STYLE, style);
            SetWindowPos(consoleWindow, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002);
            MoveWindow(consoleWindow, x, y, 810, 220, true);
            SetWindowLong(consoleWindow, GWL_STYLE, style | WS_VISIBLE);
        }
    }
}
