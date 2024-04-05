using Spectre.Console;
using Spectre;
using Spectre.Console.Rendering;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime;
using System.Text;
using CommandLine;

namespace NowPlayingTUI {
    class Program {

        // Define constants for modifying window styles
        const int GWL_STYLE = -16;
        const uint WS_VISIBLE = 0x10000000;
        const uint WS_BORDER = 0x00800000;
        const uint WS_CAPTION = 0x00C00000;
        const uint WS_MINIMIZEBOX = 0x00020000;
        const uint WS_MAXIMIZEBOX = 0x00010000;
        const uint WS_THICKFRAME = 0x00040000;
        const uint WS_SYSMENU = 0x00080000;
        const uint WS_SIZEBOX = WS_THICKFRAME;
        const uint WS_VSCROLL = 0x00200000;
        const uint WS_HSCROLL = 0x00100000;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


        public static SongTimer _songTimer;

        public StateManager _stateManager;

        private static void Main(string[] args) => new Program().Run(args).GetAwaiter().GetResult();

        class Options {

            [Option('x',"xcoord", Required = false, Default = -790)]
            public int x { get; set; }

            [Option('y', "ycoord", Required = false, Default = 0)]
            public int y { get; set; }
        }

        private async Task Run(string[] args) {

            int x = 0,y = 0;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => {
                    x = o.x;
                    y = o.y;
                });

            Console.Title = "Now Playing TUI";
            Console.CursorVisible = false;
            Console.InputEncoding = Encoding.Unicode;
            IntPtr consoleWindow = GetConsoleWindow();
            _stateManager = new StateManager();

            if(consoleWindow != IntPtr.Zero) {
                // Get the current window style
                uint style = GetWindowLong(consoleWindow, GWL_STYLE);

                // Remove unwanted styles (e.g., border, title bar, maximize box, minimize box)
                style &= ~(WS_VSCROLL | WS_SYSMENU | WS_BORDER | WS_CAPTION | WS_HSCROLL | WS_THICKFRAME | WS_SIZEBOX);

                // Add or modify styles for squared border
                style |= WS_BORDER;

                // Apply the new window style
                SetWindowLong(consoleWindow, GWL_STYLE, style);

                // Resize and reposition the window to remove the title bar's space
                SetWindowPos(consoleWindow, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002); // SWP_NOMOVE | SWP_NOSIZE

                // Set the new position and size (example: x=100, y=100, width=800, height=600)
                MoveWindow(consoleWindow, x, y, 810, 220, true);

                // Make the console window visible
                SetWindowLong(consoleWindow, GWL_STYLE, style | WS_VISIBLE);

            }
            else {
                Console.WriteLine("Failed to get the console window handle.");
            }

            Console.BufferHeight = Console.WindowHeight = 7;
            Console.BufferWidth = Console.WindowWidth;
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;

            await _stateManager.Start();

            await Task.Delay(-1);
        }
    }
}
