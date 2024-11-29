using Spectre.Console;
using System;
using System.Linq;
using System.Text;
using static NowPlayingTUI.StateManager;

namespace NowPlayingTUI
{
    public class ConsoleX
    {
        private Table Table { get; set; }

        public ConsoleX()
        {
            Table = new Table();
        }

        public static void WriteAt(string s, int x, int y, Spectre.Console.Color color)
        {
            if (string.IsNullOrEmpty(s))
                return;

            try
            {
                Console.SetCursorPosition(x - 1, y);
                AnsiConsole.Write(new Markup("[grey39]┐[/]"));
                Console.SetCursorPosition(x, y);
                AnsiConsole.Write(new Markup(s, new Style(foreground: color)).Overflow(Overflow.Ellipsis));
                Console.SetCursorPosition(x + s.Length, y);
                AnsiConsole.Write(new Markup("[grey39]┌[/]"));
                Console.SetCursorPosition(0, 0);
            }
            catch (ArgumentOutOfRangeException) { }
        }
        public static void WriteAtDown(string s, int x, int y, Spectre.Console.Color color) {
            if(string.IsNullOrEmpty(s))
                return;

            try {
                Console.SetCursorPosition(x - 1, y);
                AnsiConsole.Write(new Markup("[grey39]┐[/]"));
                Console.SetCursorPosition(x, y);
                AnsiConsole.Write(new Markup(s, new Style(foreground: color)).Overflow(Overflow.Ellipsis));
                Console.SetCursorPosition(x + s.Length, y);
                AnsiConsole.Write(new Markup("[grey39]┌[/]"));
                Console.SetCursorPosition(0, 0);
            }
            catch(ArgumentOutOfRangeException) { }
        }

        private static void GenerateSpectrum(int x, int y, int width, string[] spectrumLevels, Spectre.Console.Color color)
        {
            Random random = new Random();
            int[] spectrum = new int[width];
            spectrum[0] = random.Next(spectrumLevels.Length - 1);
            for (int i = 1; i < width; i++)
            {
                int min = Math.Max(0, spectrum[i - 1] - 1);
                int max = Math.Min(spectrumLevels.Length - 1, spectrum[i - 1] + 1);
                spectrum[i] = random.Next(min, max + 1);
            }

            for (int i = 0; i < width; i++)
            {
                string character = spectrumLevels[spectrum[i]];
                Console.SetCursorPosition(x + i, y);
                AnsiConsole.Write(new Markup(character, new Style(foreground: color)));
            }
            Console.SetCursorPosition(0, 0);
        }

        public static void GenerateAudioSpectrum(int x, int y, int width) =>
            GenerateSpectrum(x, y, width, new[] { "_", ".", ":", "-", "=", "#" }, Spectre.Console.Color.Lime);

        public static void GenerateAudioSpectrumInactive(int x, int y, int width) =>
            GenerateSpectrum(x, y, width, new[] { "_", ".", ":" }, Spectre.Console.Color.RoyalBlue1);
        public static void GenerateTimeDisplay(int x, int y, Spectre.Console.Color clr) {            
            var time = DateTime.Now.ToString("HH:mm:ss - dd/MMM/yy - dddd");
            WriteAtDown(time, x, y, clr);
        }
        private void DrawPanel(string title, string content, string color, Layout layout, string panelKey)
        {
            var panel = new Panel(Align.Center(new Markup(color + Markup.Escape(content ?? "Not Found") + "[/]")
                .Overflow(Overflow.Ellipsis), VerticalAlignment.Middle))
            {
                Expand = true,
                Header = new PanelHeader("[grey39]┐[/]" + color + title + "[/]" + "[grey39]┌[/]"),
                BorderStyle = new Style(foreground: Spectre.Console.Color.Grey39)
            };
            layout[panelKey].Update(panel);
        }

        internal void DrawPlaying(Song currentSong)
        {
            var textColor = "[orange1]";

            var layout = new Layout("Root")
                .SplitColumns(
                    new Layout("Left"),
                    new Layout("Right")
                        .SplitColumns(
                            new Layout("Album"),
                            new Layout("AlbumCover")
                        ));

            DrawPanel("Current Song", currentSong.Title, textColor, layout, "Left");
            DrawPanel("Album", currentSong.Album, textColor, layout, "Album");

            if(currentSong.Album.ToLower() == "unknown album")
                layout["Right"].Invisible();

            if (currentSong.img != null)
            {
                currentSong.img.MaxWidth(3);
                currentSong.img.BilinearResampler();
                var albumDataPanel = new Panel(Align.Center(currentSong.img, VerticalAlignment.Middle))
                {
                    Expand = true,
                    Header = new PanelHeader("[grey39]┐[/]" + textColor + "AlbumIMG[/]" + "[grey39]┌[/]"),
                    BorderStyle = new Style(foreground: Spectre.Console.Color.Grey39)
                };
                layout["AlbumCover"].Update(albumDataPanel);
            }
            else
            {
                layout["AlbumCover"].Invisible();
            }

            AnsiConsole.Background = Spectre.Console.Color.Black;
            AnsiConsole.Write(layout);
            Console.SetCursorPosition(0, 0);
        }

        internal void DrawIdle()
        {
            DrawStatus("[orange1]Nothing is playing[/]", "[orange1]Status[/]", Spectre.Console.Color.Orange1);
        }

        internal void DrawEmpty()
        {
            DrawStatus("[orange1]Waiting For Spotify[/]", "[orange1]Status[/]", Spectre.Console.Color.Orange1);
        }

        private void DrawStatus(string message, string header, Spectre.Console.Color color)
        {
            var panel = new Panel(Align.Center(new Markup(message).Overflow(Overflow.Ellipsis), VerticalAlignment.Middle))
            {
                Expand = true,
                Header = new PanelHeader("[grey39]┐[/]" + header + "[grey39]┌[/]"),
                BorderStyle = new Style(foreground: Spectre.Console.Color.Grey39)
            };
            var layout = new Layout("Root");
            layout["Root"].Update(panel);

            AnsiConsole.Background = Spectre.Console.Color.Black;
            AnsiConsole.Write(layout);
            Console.SetCursorPosition(0, 0);
        }
    }
}
