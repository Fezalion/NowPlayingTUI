using NowPlayingTUI.Enums;
using NowPlayingTUI.Helpers;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace NowPlayingTUI
{
    public class StateManager
    {
        private static string _scrobblerApiKey;
        private readonly ConsoleX _console;

        public delegate State StateChangeHandler(State oldState, State newState);
        public event StateChangeHandler StateChanged;

        public delegate Song SongChangeHandler(Song oldSong, Song newSong);
        public event SongChangeHandler SongChanged;

        public struct Song
        {
            public string Title;
            public string Album;
            public CanvasImage img;
            public SongTimer timer;
        }

        private Song _song;
        public Song CurrentSong
        {
            get => _song;
            set
            {
                var temp = _song;
                _song = value;
                SongChanged?.Invoke(temp, value);
            }
        }

        private State _state;
        public State CurrentState
        {
            get => _state;
            set
            {
                var temp = _state;
                _state = value;
                StateChanged?.Invoke(temp, _state);
            }
        }

        public StateManager()
        {
            CurrentState = State.NoSpotify;
            _console = new ConsoleX();
            StateChanged += OnStateChanged;
            SongChanged += OnSongChanged;
        }

        private Song OnSongChanged(Song oldSong, Song newSong)
        {
            oldSong.timer?.StopTimer();
            CurrentState = State.Playing;
            return newSong;
        }

        private State OnStateChanged(State oldState, State newState)
        {
            if (CurrentSong.timer != null)
                CurrentSong.timer.StopTimer();

            switch (newState)
            {
                case State.NoSpotify:
                    _console.DrawEmpty();
                    break;
                case State.Playing:
                    _console.DrawPlaying(CurrentSong);
                    break;
                case State.Idle:
                    _console.DrawIdle();
                    break;
                default:
                    _console.DrawEmpty();
                    break;
            }
            return newState;
        }

        public async Task Start()
        {
            LoadApiKey();

            string url = BuildTrackInfoUrl("cher", "believe");
            try
            {
                await FetchTrackInfoAsync(url);
            }
            catch
            {
                await HandleApiFailureAsync();
                return;
            }

            MonitorSpotify();
        }

        private void LoadApiKey()
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            _scrobblerApiKey = Environment.GetEnvironmentVariable("SCROBBLER_API_KEY") ?? PromptForApiKey(dotenv);
        }

        private string PromptForApiKey(string dotenv)
        {
            var apikey = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [lime]scrobbler api key[/]:")
                    .PromptStyle("red")
                    .Secret()
                    .Validate(key => key.Length > 0 ? ValidationResult.Success() : ValidationResult.Error("Please enter a valid scrobbler api key"))
            );

            DotEnv.Write(dotenv, apikey);
            return apikey;
        }

        private string BuildTrackInfoUrl(string artist, string title)
        {
            string url = "http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key={0}&autocorrect=1&artist={1}&track={2}";
            artist = HttpUtility.UrlEncode(artist);
            title = HttpUtility.UrlEncode(title);
            return string.Format(url, _scrobblerApiKey, artist, title);
        }

        private async Task FetchTrackInfoAsync(string url)
        {
            using WebClient client = new();
            string xml = await client.DownloadStringTaskAsync(url);
            XDocument xmlDoc = XDocument.Load(new StringReader(xml));
            var trackTitle = xmlDoc.XPathSelectElement("//track/name")?.Value;
        }

        private async Task HandleApiFailureAsync()
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Ascii)
                .SpinnerStyle(Style.Parse("bold red"))
                .StartAsync("Api does not respond, check api key or last.fm api status. (Application will quit in 15 seconds)", async ctx =>
                {
                    for (int i = 15; i > 0; i--)
                    {
                        ctx.Status($"Application will quit in {i} seconds");
                        await Task.Delay(1000);
                    }
                    Environment.Exit(0);
                });
        }

        private void MonitorSpotify()
        {
            Task.Run(async () =>
            {
                Process spotifyProcess = null;
                string spotifyTitle = string.Empty;

                while (true)
                {
                    if (spotifyProcess == null)
                    {
                        spotifyProcess = GetSpotifyProcess();
                        if (spotifyProcess == null)
                        {
                            CurrentState = State.NoSpotify;
                            await Task.Delay(1000);
                            continue;
                        }
                    }
                    else
                    {
                        spotifyProcess.Refresh();
                        if (spotifyProcess.HasExited)
                        {
                            spotifyProcess = null;
                            CurrentState = State.NoSpotify;
                            continue;
                        }

                        string title = spotifyProcess.MainWindowTitle;
                        CurrentState = title == "Spotify" ? State.Idle : CurrentState;
                        if (title != spotifyTitle)
                        {
                            spotifyTitle = title;
                            SetLabel(title);
                        }
                        await Task.Delay(1000);
                    }
                }
            });
        }

        private Process GetSpotifyProcess()
        {
            var processes = Process.GetProcessesByName("spotify");
            return processes.Length > 0 ? processes[0] : null;
        }

        public void SetLabel(string label)
        {
            var parts = label.Replace("Spotify - ", string.Empty).Split(" - ");
            if (parts.Length < 2) return;

            string artist = HttpUtility.UrlEncode(parts[0].Trim());
            string title = HttpUtility.UrlEncode(parts[1].Trim());
            string url = BuildTrackInfoUrl(artist, title);

            using WebClient client = new();
            string xml = client.DownloadString(url);
            XDocument xmlDoc = XDocument.Load(new StringReader(xml));
            var trackTitle = xmlDoc.XPathSelectElement("//track/name")?.Value ?? "Unknown Title";
            var artistName = xmlDoc.XPathSelectElement("//artist/name")?.Value ?? "Unknown Artist";
            var albumName = xmlDoc.XPathSelectElement("//album/title")?.Value ?? "Unknown Album";
            var imgUrl = xmlDoc.XPathSelectElement("//album/image[@size='small']")?.Value ?? null;
            int songDur = int.Parse(xmlDoc.XPathSelectElement("//track/duration")?.Value ?? "0");

            CurrentSong = new Song
            {
                Title = $"{artistName} - {trackTitle}",
                Album = albumName,
                img = !string.IsNullOrEmpty(imgUrl) ? new CanvasImage(new WebClient().DownloadData(imgUrl)) : null,
                timer = new SongTimer(songDur)
            };
        }

        public State GetState() => CurrentState;
    }
}
