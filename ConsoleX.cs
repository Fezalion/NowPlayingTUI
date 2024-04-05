using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NowPlayingTUI.StateManager;

namespace NowPlayingTUI {
    public class ConsoleX {
        private Table Table { get; set; }

        public ConsoleX() {
            Table = new Table();
        }
        
        public static void writeat(string s, int x, int y, Spectre.Console.Color color) {
            try {
                if(string.IsNullOrEmpty(s))
                    return;
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
            catch(ArgumentOutOfRangeException e) {

            }
        }
        public static void GenerateAudioSpectrum(int x, int y, int width) {

            string[] spectrumLevels = { "_" , ".", ":", "-", "=", "#" };

            // Generate random seed
            Random random = new Random();

            // Generate initial spectrum values
            int[] spectrum = new int[width];
            spectrum[0] = random.Next(spectrumLevels.Length - 1);
            for(int i = 1; i < width; i++) {
                int min = Math.Max(0, spectrum[i - 1] - 1); // Ensure next number is not less than 0
                int max = Math.Min(spectrumLevels.Length - 1, spectrum[i - 1] + 1); // Ensure next number is not greater than 8

                // Generate the next number
                spectrum[i] = random.Next(min, max + 1);
            }

            // Display the spectrum
            for(int i = 0; i < width; i++) {
                // Get the character representing the spectrum level
                string character = spectrumLevels[spectrum[i]];
                //string character = spectrum[i].ToString();

                // Output the character at the specified position
                Console.SetCursorPosition(x + i, y);
                AnsiConsole.Write(new Markup(character, Style.WithForeground(Spectre.Console.Color.Lime)));
            }

            Console.SetCursorPosition(0, 0);
        }

        public static void GenerateAudioSpectrumInactive(int x, int y, int width) {

            string[] spectrumLevels = { "_" , ".", ":" };

            // Generate random seed
            Random random = new Random();

            // Generate initial spectrum values
            int[] spectrum = new int[width];
            spectrum[0] = random.Next(spectrumLevels.Length - 1);
            for(int i = 1; i < width; i++) {
                int min = Math.Max(0, spectrum[i - 1] - 1); // Ensure next number is not less than 0
                int max = Math.Min(spectrumLevels.Length - 1, spectrum[i - 1] + 1); // Ensure next number is not greater than 8

                // Generate the next number
                spectrum[i] = random.Next(min, max + 1);
            }

            // Display the spectrum
            for(int i = 0; i < width; i++) {
                // Get the character representing the spectrum level
                string character = spectrumLevels[spectrum[i]];
                //string character = spectrum[i].ToString();

                // Output the character at the specified position
                Console.SetCursorPosition(x + i, y);
                AnsiConsole.Write(new Markup(character, Style.WithForeground(Spectre.Console.Color.Grey27)));
            }

            Console.SetCursorPosition(0, 0);
        }

        internal void DrawPlaying(Song currentSong) {
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

            var leftPanel =
                    new Panel(
                        Align.Center(
                            new Markup(textColor +Markup.Escape(currentSong.Title ?? "Not Found") + "[/]")
                                .Overflow(Overflow.Ellipsis)
                                    ,VerticalAlignment.Middle));
            leftPanel.Expand = true;
            leftPanel.Header = new PanelHeader("┐" + textColor + "Current Song[/]┌");

            var AlbumPanel = new Panel(Align.Center(new Markup(Markup.Escape(currentSong.Album ?? "Not Found")).Overflow(Overflow.Crop), VerticalAlignment.Middle));
            AlbumPanel.Expand = true;
            AlbumPanel.Header = new PanelHeader("┐" + textColor + "Album[/]┌");

            layout["left"].Update(leftPanel);

            layout["Album"].Update(AlbumPanel);

            if(currentSong.img != null) {
                currentSong.img.MaxWidth(3);
                currentSong.img.BilinearResampler();

                var AlbumDataPanel = new Panel(Align.Center(currentSong.img != null ? currentSong.img : new Markup("").Overflow(Overflow.Ellipsis), VerticalAlignment.Middle));
                AlbumDataPanel.Expand = true;
                AlbumDataPanel.Header = new PanelHeader("┐" + textColor + "AlbumIMG[/]┌");
                layout["AlbumCover"].Update(AlbumDataPanel);
            } else {
                layout["AlbumCover"].Invisible();
            }

            AnsiConsole.Background = Spectre.Console.Color.Black;

            // Render the layout
            AnsiConsole.Write(layout);            

            Console.SetCursorPosition(0, 0);
        }
        
        internal void DrawIdle() {
            AnsiConsole.Clear();
            var leftPanel =
                    new Panel(
                        Align.Center(
                            new Markup("[grey27]Nothing is playing[/]")
                                .Overflow(Overflow.Ellipsis)
                                    ,VerticalAlignment.Middle));
            leftPanel.Expand = true;
            leftPanel.Header = new PanelHeader("┐[grey27]Status[/]┌");
            var layout = new Layout("Root");
            layout["Root"].Update(leftPanel);

            AnsiConsole.Background = Spectre.Console.Color.Black;
            // Render the layout
            AnsiConsole.Write(layout);
            ConsoleX.GenerateAudioSpectrumInactive(1, 5, 95);
            Console.SetCursorPosition(0, 0);
        }

        internal void DrawEmpty() {
            AnsiConsole.Clear();
            var leftPanel =
                    new Panel(
                        Align.Center(
                            new Markup("[grey27]Waiting For Spotify[/]")
                                .Overflow(Overflow.Ellipsis)
                                    ,VerticalAlignment.Middle));
            leftPanel.Expand = true;
            leftPanel.Header = new PanelHeader("┐[grey27]Status[/]┌");
            var layout = new Layout("Root");
            layout["Root"].Update(leftPanel);

            AnsiConsole.Background = Spectre.Console.Color.Black;
            // Render the layout
            AnsiConsole.Write(layout);
            ConsoleX.GenerateAudioSpectrumInactive(1, 5, 95);
            Console.SetCursorPosition(0, 0);
        }
    }
}
