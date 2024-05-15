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
                Console.Write("┐");
                Console.ForegroundColor = color;
                Console.SetCursorPosition(x, y);
                Console.Write(s);
                Console.SetCursorPosition(x + s.Length, y);
                Console.ResetColor();
                Console.Write("┌");
                Console.SetCursorPosition(0, 0);
            }
            catch (ArgumentOutOfRangeException) { }
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
                AnsiConsole.Write(new Markup(character, Style.WithForeground(color)));
            }
            Console.SetCursorPosition(0, 0);
        }

        public static void GenerateAudioSpectrum(int x, int y, int width) =>
            GenerateSpectrum(x, y, width, new[] { "_", ".", ":", "-", "=", "#" }, Spectre.Console.Color.Lime);

        public static void GenerateAudioSpectrumInactive(int x, int y, int width) =>
            GenerateSpectrum(x, y, width, new[] { "_", ".", ":" }, Spectre.Console.Color.Grey27);

        private void DrawPanel(string title, string content, string color, Layout layout, string panelKey)
        {
            var panel = new Panel(Align.Center(new Markup(color + Markup.Escape(content ?? "Not Found") + "[/]")
                .Overflow(Overflow.Ellipsis), VerticalAlignment.Middle))
            {
                Expand = true,
                Header = new PanelHeader("┐" + color + title + "[/]" + "┌")
            };
            layout[panelKey].Update(panel);
        }

        internal void DrawPlaying(Song currentSong)
        {
            AnsiConsole.Clear();
            var textColor = "[Lime]";

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

            if (currentSong.img != null)
            {
                currentSong.img.MaxWidth(3);
                currentSong.img.BilinearResampler();
                var albumDataPanel = new Panel(Align.Center(currentSong.img, VerticalAlignment.Middle))
                {
                    Expand = true,
                    Header = new PanelHeader("┐" + textColor + "AlbumIMG[/]" + "┌")
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
            DrawStatus("[grey27]Nothing is playing[/]", "[grey27]Status[/]", Spectre.Console.Color.Grey27);
        }

        internal void DrawEmpty()
        {
            DrawStatus("[grey27]Waiting For Spotify[/]", "[grey27]Status[/]", Spectre.Console.Color.Grey27);
        }

        private void DrawStatus(string message, string header, Spectre.Console.Color color)
        {
            AnsiConsole.Clear();
            var panel = new Panel(Align.Center(new Markup(message).Overflow(Overflow.Ellipsis), VerticalAlignment.Middle))
            {
                Expand = true,
                Header = new PanelHeader("┐" + header + "┌")
            };
            var layout = new Layout("Root");
            layout["Root"].Update(panel);

            AnsiConsole.Background = Spectre.Console.Color.Black;
            AnsiConsole.Write(layout);
            GenerateAudioSpectrumInactive(1, 5, 95);
            Console.SetCursorPosition(0, 0);
        }
    }
}
