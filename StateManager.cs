using NowPlayingTUI.Enums;
using NowPlayingTUI.Helpers;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using static NowPlayingTUI.Program;

namespace NowPlayingTUI {
    public class StateManager {

        private static string _scrobblerApiKey;
        private ConsoleX _console;
        private string defaultImgUrl = "https://placehold.co/100";


        public delegate State StateChangeHandler(State oldState, State newState);
        public event StateChangeHandler StateChanged;

        public delegate Song SongChangeHandler(Song oldSong, Song newSong);
        public event SongChangeHandler SongChanged;


        public struct Song {
            public string Title;
            public string Album;
            public CanvasImage img;
            public SongTimer timer;
        }

        private Song _song;
        public Song CurrentSong {
            get {
                return _song;
            }
            set {
                var temp = _song;
                _song = value;
                SongChanged?.Invoke(temp, value);
            }
        }

        private State _state;
        public State CurrentState {
            get {
                return _state;
            }
            set {
                var temp = _state;
                _state = value;
                StateChanged?.Invoke(temp, _state);
            }
        }

        public StateManager() {
            CurrentState = State.NoSpotify;
            _console = new ConsoleX();
            StateChanged += OnStateChanged;
            SongChanged += OnSongChaged;
        }
        public Song OnSongChaged(Song oldSong, Song newSong) {
            //clean old song timer
            if(!EqualityComparer<Song>.Default.Equals(oldSong, default(Song))) {
                oldSong.timer.stopTimer();
            }
            CurrentState = State.Playing;
            return newSong;
        }
        public State OnStateChanged(State oldState, State newState) {
            switch(newState) {
                case State.NoSpotify:
                    if(!EqualityComparer<Song>.Default.Equals(CurrentSong, default(Song))) {
                        CurrentSong.timer.stopTimer();
                    }
                    _console.DrawEmpty();
                    break;
                case State.Playing:
                    _console.DrawPlaying(CurrentSong);
                    break;
                case State.Idle:
                    if(!EqualityComparer<Song>.Default.Equals(CurrentSong, default(Song))) {
                        CurrentSong.timer.stopTimer();
                    }
                    _console.DrawIdle();
                    break;
                default:
                    if(!EqualityComparer<Song>.Default.Equals(CurrentSong, default(Song))) {
                        CurrentSong.timer.stopTimer();
                    }
                    _console.DrawEmpty();
                    break;
            }
            return newState;
        }


        public Task Start() {

            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            _scrobblerApiKey = Environment.GetEnvironmentVariable("SCROBBLER_API_KEY");

            if(_scrobblerApiKey == null) {
                AnsiConsole.MarkupLine("[bold red]scrobbler api key is missing from .env file[/]");

                string apikey = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter [lime]scrobbler api key[/]:")
                        .PromptStyle("red")
                        .Secret()
                        .Validate(key => {
                            return key switch {
                                string k when k.Length > 0 => ValidationResult.Success(),
                                _ => ValidationResult.Error("Please enter a valid scrobbler api key")
                            };
                        })
                );

                DotEnv.Write(dotenv, apikey);

                _scrobblerApiKey = apikey;
            }

            string url = "http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key={0}&autocorrect=1&artist={1}&track={2}";
            var artist = HttpUtility.UrlEncode("cher");
            var title = HttpUtility.UrlEncode("believe");
            url = string.Format(url, _scrobblerApiKey, artist, title);

            using(WebClient client = new WebClient()) {
                try {
                    string xml = client.DownloadString(url);
                    XDocument xmlDoc = XDocument.Load(new StringReader(xml));
                    var track = from trackData in xmlDoc.XPathSelectElements("//track/name")
                                select trackData.Value;
                    string trackTitle = track.FirstOrDefault();
                }
                catch {
                    AnsiConsole.Status().Spinner(Spinner.Known.Ascii).SpinnerStyle(Style.Parse("bold red"))
                        .Start("s", async ctx => {
                            int count = 15;
                            while((count--) > 0) {
                                // Update the status and spinner
                                ctx.Status($"Api does not respond, check api key or last.fm api status.\n (Application will quit in {count} seconds)");                                
                                ctx.Spinner(Spinner.Known.Ascii);
                                ctx.SpinnerStyle(Style.Parse("bold red"));
                                Thread.Sleep(1000);
                            }
                            Environment.Exit(0);
                        });
                    return Task.CompletedTask;
                }
            }

            /*Task.Run(() => {
                while(true) {
                    Thread.Sleep(1000);
                    if(CurrentState == State.Playing) {
                        ConsoleX.GenerateAudioSpectrum(1, 5, 47);
                        if(CurrentSong.img != null) {
                            ConsoleX.GenerateAudioSpectrum(50, 5, 22);
                            ConsoleX.GenerateAudioSpectrum(74, 5, 23);
                        } else {
                            ConsoleX.GenerateAudioSpectrum(50, 5, 40);
                        }
                    }
                }

            });*/

            var t = Task.Run(() => {
                Thread.Sleep(1000);
                Process spotifyProcess = null;
                string spotifyTitle = string.Empty;
                while(true) {
                    if(spotifyProcess == null) {
                        var processes = Process.GetProcessesByName("spotify");
                        if(processes.Length < 1) {
                            CurrentState = State.NoSpotify;
                            Thread.Sleep(1000);
                            continue;
                        }

                        spotifyProcess = processes[0];
                    }
                    else {
                        spotifyProcess.Refresh();
                        if(spotifyProcess.HasExited) {
                            spotifyProcess = null;
                            CurrentState = State.NoSpotify;
                            continue;
                        }

                        string title = spotifyProcess.MainWindowTitle;

                        if(title == "Spotify") {
                            CurrentState = State.Idle;
                        }

                        if(title != spotifyTitle) {
                            spotifyTitle = title;
                            SetLabel(title);
                        }
                        Thread.Sleep(1000);
                    }
                }
            });

            return t;
        }

        public void SetLabel(string label) {
            string noSpotify = label.Replace("Spotify - ", string.Empty);
            var parts = noSpotify.Split(" - ");
            if(parts.Length < 2) {
                //this should never happen
                return;
            }
            string artist = parts[0].Trim();
            string title = parts[1].Trim();
            string url = "http://ws.audioscrobbler.com/2.0/?method=track.getinfo&api_key={0}&autocorrect=1&artist={1}&track={2}";
            artist = HttpUtility.UrlEncode(artist);
            title = HttpUtility.UrlEncode(title);
            url = string.Format(url, _scrobblerApiKey, artist, title);

            using(WebClient client = new WebClient()) {
                // Download the audioscrobbler XML for the given artist and song
                string xml = client.DownloadString(url);
                XDocument xmlDoc = XDocument.Load(new StringReader(xml));
                var imgUrls = from img in xmlDoc.XPathSelectElements("//album/image")
                              where img.Attribute("size").Value == "small"
                              select img.Value;
                string imgUrl = imgUrls.FirstOrDefault();

                var track = from trackData in xmlDoc.XPathSelectElements("//track/name")
                            select trackData.Value;
                string trackTitle = track.FirstOrDefault();
                var artistd = from artistData in xmlDoc.XPathSelectElements("//artist/name")
                              select artistData.Value;
                string artistName = artistd.FirstOrDefault();

                var album = from albumData in xmlDoc.XPathSelectElements("//album/title")
                            select albumData.Value;
                string albumName = album.FirstOrDefault();

                var songDuration = from duration in xmlDoc.XPathSelectElements("//track/duration")
                                   select duration.Value;
                int songDur = int.Parse(songDuration.FirstOrDefault() ?? "0");

                // If we can't find an image URL, just use the default cover
                artistName = artistName ?? "Unknown Artist";
                trackTitle = trackTitle ?? "Unknown Title";
                albumName = albumName ?? "Unknown Album";
                imgUrl = imgUrl ?? null;

                //Create Song object
                if(!string.IsNullOrEmpty(imgUrl)) {
                    CurrentSong = new Song() {
                        Title = artistName + " " + trackTitle,
                        Album = albumName,
                        img = new CanvasImage(new WebClient().DownloadData(imgUrl)),
                        timer = new SongTimer(songDur)
                    };
                }
                else {
                    CurrentSong = new Song() {
                        Title = trackTitle,
                        Album = albumName,
                        img = null,
                        timer = new SongTimer(songDur)
                    };
                }
            }
        }

        public State GetState() {
            return CurrentState;
        }
    }
}
