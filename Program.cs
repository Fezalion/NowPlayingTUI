using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace NowPlayingTUI
{
    class Program
    {       

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
        private async Task Run(string[] args)
        {    
            Console.Title = "Now Playing TUI";
            Console.CursorVisible = false;
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;

            Application.UseSystemConsole = true;
            Application.IsMouseDisabled = true;
            Application.Init();

            var top = Application.Top;
            top.CanFocus = false;
            Application.MainLoop.Invoke(async () => {
                _stateManager = new StateManager(top);
                await _stateManager.Start();
            });

            Application.Run();
            await Task.Delay(-1);
        }
    }
}
