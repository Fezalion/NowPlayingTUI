using NowPlayingTUI.Enums;
using NowPlayingTUI.Helpers;
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
using Terminal.Gui;

namespace NowPlayingTUI {
    public class StateManager {
        private static string _scrobblerApiKey;
        private readonly ConsoleX _console;

        public delegate State StateChangeHandler(State oldState, State newState);
        public event StateChangeHandler StateChanged;

        public delegate Song SongChangeHandler(Song oldSong, Song newSong);
        public event SongChangeHandler SongChanged;

        public struct Song {
            public string Title;
            public string Album;
        }

        private Song _song;
        public Song CurrentSong {
            get => _song;
            set {
                var temp = _song;
                _song = value;
                SongChanged?.Invoke(temp, value);
            }
        }

        private State _state;
        public State CurrentState {
            get => _state;
            set {
                var temp = _state;
                _state = value;
                StateChanged?.Invoke(temp, _state);
            }
        }

        private Toplevel _top;
        public Toplevel Top {
            get => _top;
            set => _top = value;
        }

        public StateManager(Toplevel top) {
            CurrentState = State.NoSpotify;
            _console = new ConsoleX();
            StateChanged += OnStateChanged;
            SongChanged += OnSongChanged;
            Top = top;
        }

        private Song OnSongChanged(Song oldSong, Song newSong) {
            CurrentState = State.Playing;
            return newSong;
        }

        private State OnStateChanged(State oldState, State newState) {

           // if(oldState == newState)
           //     return newState;

            switch(newState) {
                case State.NoSpotify:
                    _console.DrawEmpty(Top);
                    break;
                case State.Playing:
                    _console.DrawPlaying(Top, CurrentSong);
                    break;
                case State.Idle:
                    _console.DrawIdle(Top);
                    break;
                default:
                    _console.DrawEmpty(Top);
                    break;
            }
            return newState;
        }

        public async Task Start() {
            LoadApiKey();
            MonitorSpotify();
            string url = BuildTrackInfoUrl("cher", "believe");
            try {
                await FetchTrackInfoAsync(url);
            }
            catch {
                await HandleApiFailureAsync();
                return;
            }
        }

        private void LoadApiKey() {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            _scrobblerApiKey = Environment.GetEnvironmentVariable("SCROBBLER_API_KEY") ?? PromptForApiKey(dotenv);
        }

        private static string PromptForApiKey(string dotenv) {
            var dialog = new Dialog("Enter API Key", 60, 20);

            var label = new Label("Enter scrobbler API key:")
            {
                X = Pos.Center(),
                Y = 1
            };
            dialog.Add(label);

            var textField = new TextField("")
            {
                Secret = true,
                X = Pos.Center(),
                Y = Pos.Bottom(label) + 1,
                Width = 40
            };
            dialog.Add(textField);

            string apiKey = null;
            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.Bottom(textField) + 2,
                IsDefault = true
            };
            okButton.Clicked += () => {
                if(ValidateApiKey(textField.Text.ToString())) {
                    apiKey = textField.Text.ToString();
                    Application.RequestStop();
                }
                else {
                    MessageBox.ErrorQuery("Validation Error", "Please enter a valid scrobbler API key.", "OK");
                }
            };
            dialog.Add(okButton);

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 10,
                Y = Pos.Bottom(textField) + 2
            };
            cancelButton.Clicked += () => {
                apiKey = null;
                Application.RequestStop();
            };
            dialog.Add(cancelButton);

            Application.Run(dialog);

            if(apiKey != null) {
                DotEnv.Write(dotenv, apiKey);
            }
            return apiKey;
        }

        static bool ValidateApiKey(string key) {
            return !string.IsNullOrEmpty(key);
        }

        private static async Task HandleApiFailureAsync() {
            var dialog = new Dialog("API Failure", 60, 8, new Button("Ok"));
            var messageLabel = new Label("Api does not respond, check api key or last.fm api status.")
            {
                X = Pos.Center(),
                Y = 1
            };
            dialog.Add(messageLabel);

            var countdownLabel = new Label("Application will quit in 15 seconds")
            {
                X = Pos.Center(),
                Y = Pos.Bottom(messageLabel) + 1
            };
            dialog.Add(countdownLabel);

            Application.Run(dialog); // Show dialog

            for(int i = 15; i > 0; i--) {
                countdownLabel.Text = $"Application will quit in {i} seconds";
                Application.Refresh();
                await Task.Delay(1000);
            }

            Environment.Exit(0);
        }

        private string BuildTrackInfoUrl(string artist, string title) {
            string url = "http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key={0}&autocorrect=1&artist={1}&track={2}";
            artist = HttpUtility.UrlEncode(artist);
            title = HttpUtility.UrlEncode(title);
            return string.Format(url, _scrobblerApiKey, artist, title);
        }

        private async Task FetchTrackInfoAsync(string url) {
            using WebClient client = new();
            string xml = await client.DownloadStringTaskAsync(url);
            XDocument xmlDoc = XDocument.Load(new StringReader(xml));
            var trackTitle = xmlDoc.XPathSelectElement("//track/name")?.Value;
        }

        private void MonitorSpotify() {
            Task.Run(async () => {
                Process spotifyProcess = null;
                string spotifyTitle = string.Empty;

                while(true) {
                    if(spotifyProcess == null) {
                        spotifyProcess = GetSpotifyProcess();
                        if(spotifyProcess == null) {
                            CurrentState = State.NoSpotify;
                            await Task.Delay(1000);
                            continue;
                        }
                    }
                    else {
                        spotifyProcess.Refresh();
                        if(spotifyProcess.HasExited) {
                            spotifyProcess = null;
                            CurrentState = State.NoSpotify;
                            continue;
                        }

                        string title = spotifyProcess.MainWindowTitle;
                        switch(title) {
                            case "Spotify":
                            case "Spotify Free":
                            case "Spotify Premium":
                                CurrentState = State.Idle;
                                break;
                            default:
                                CurrentState = CurrentState;
                                break;
                        }
                        if(title != spotifyTitle) {
                            spotifyTitle = title;
                            SetCurrentSong(title);
                        }
                        await Task.Delay(1000);
                    }
                }
            });
        }

        private Process GetSpotifyProcess() {
            var processes = Process.GetProcessesByName("spotify");
            return processes.Length > 0 ? processes[0] : null;
        }

        public void SetCurrentSong(string label) {
            var parts = label.Replace("Spotify - ", string.Empty).Split(" - ");
            if(parts.Length < 2)
                return;

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

            CurrentSong = new Song {
                Title = $"{artistName} - {trackTitle}",
                Album = albumName,
                // img = !string.IsNullOrEmpty(imgUrl) ? new CanvasImage(new WebClient().DownloadData(imgUrl)) : null
            };
        }

        public State GetState() => CurrentState;
    }
}
