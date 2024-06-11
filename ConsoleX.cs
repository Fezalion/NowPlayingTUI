using System;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Terminal.Gui;
using static NowPlayingTUI.StateManager;

namespace NowPlayingTUI
{
    public class ConsoleX
    {

        private ColorScheme _COLORSCHEME = new ColorScheme {
            Normal = Terminal.Gui.Attribute.Make(Color.BrightBlue, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black),
            Disabled = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black)
        };
        public static void WriteAt(string s, int x, int y)
        {
            if (string.IsNullOrEmpty(s))
                return;

            try
            {
                //TODO: Fix this
                return;
            }
            catch (ArgumentOutOfRangeException) { }
        }
        public static void WriteAtDown(string s, int x, int y) {
            if(string.IsNullOrEmpty(s))
                return;

            try {
                //TODO: Fix this
                return;
            }
            catch(ArgumentOutOfRangeException) { }
        }

        internal void DrawPlaying(Toplevel top,Song currentSong)
        {
            var CurrentSongPanel = new Window ("Song Name") {
                X = 0,
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill (),
                AutoSize = true,
                CanFocus = false,
                ColorScheme = _COLORSCHEME,
                LayoutStyle = LayoutStyle.Computed
            };

            var CurrentSongName = new TextView()
            {
                X = Pos.Percent(50) - currentSong.Title.Length / 2,
                Y = Pos.Percent(50),
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = false,
                ColorScheme = _COLORSCHEME,
                Text = currentSong.Title,
                LayoutStyle = LayoutStyle.Computed
            };

            CurrentSongPanel.Add(CurrentSongName);

            var CurrentAlbumPanel = new Window ("Album") {
                X = Pos.Percent(50),
                Y = 0,
                Width = Dim.Percent(50),
                Height = Dim.Fill (),
                AutoSize = true,
                CanFocus = false,
                ColorScheme = _COLORSCHEME,
                LayoutStyle = LayoutStyle.Computed
            };

            var CurrentAlbumName = new TextView()
            {
                X = Pos.Percent(50) - currentSong.Album.Length / 2,
                Y = Pos.Percent(50),
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = false,
                ColorScheme = _COLORSCHEME,
                Text = currentSong.Album,
                LayoutStyle = LayoutStyle.Computed
            };

            CurrentAlbumPanel.Add(CurrentAlbumName);
            top.RemoveAll();
            top.Add(CurrentSongPanel,CurrentAlbumPanel);
            Application.Refresh();
        }

        internal void DrawIdle(Toplevel top)
        {
            DrawStatus("Nothing is playing", "Status",top);
        }

        internal void DrawEmpty(Toplevel top)
        {
            DrawStatus("Waiting For Spotify", "Status", top);
        }

        private void DrawStatus(string message, string header, Toplevel top)
        {
            var win = new Window (header) {                
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                AutoSize = true,
                CanFocus = false,
                ColorScheme = _COLORSCHEME,
                LayoutStyle = LayoutStyle.Computed
            };

            var label = new Label(message)
            {
                X = Pos.Percent(50) - message.Length / 2,
                Y = Pos.Percent(50),
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = false,
                ColorScheme = _COLORSCHEME,
                LayoutStyle = LayoutStyle.Computed
            };
            top.RemoveAll();
            win.Add(label);
            top.Add(win);
            Application.Refresh();
        }
    }
}