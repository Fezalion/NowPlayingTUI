using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NowPlayingTUI {
    public class SongTimer {
        public int End { get; set; }
        private int ElapsedTime { get; set; }
        public System.Timers.Timer Timer { get; set; }

        public SongTimer(int duration) {
            Start(duration);
        }

        public void Start(int end) {
            ElapsedTime = 0;
            try {
                if(Timer != null)
                    Timer.Stop();
            }
            catch(NullReferenceException e) {

            }

            End = end;
            Timer = new System.Timers.Timer(1000);
            Timer.Elapsed += (sender, e) => {
                if(End <= ElapsedTime) {
                    Timer.Stop();
                }

                string formattedElapsedTime = MillisecondsToFormattedString(ElapsedTime);
                string formattedEnd = MillisecondsToFormattedString(End);

                string formattedString = $"{formattedElapsedTime}/{formattedEnd}";
                if(end == 0) {
                    formattedString = "";
                }

                ConsoleX.writeat(formattedString, 35, 0, Spectre.Console.Color.Lime);
                ElapsedTime += 1000;
            };
            Timer.Start();
        }

        public System.Timers.Timer GetTimer() {
            return Timer;
        }

        public void stopTimer() {
            try {
                if(Timer != null)
                    Timer.Stop();
            }
            catch(NullReferenceException e) {

            }
        }

        static string MillisecondsToFormattedString(int milliseconds) {
            // Convert milliseconds to seconds
            int seconds = milliseconds / 1000;

            // Calculate minutes and remaining seconds
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;

            // Format minutes and seconds with leading zeros
            string formattedMinutes = minutes.ToString("00");
            string formattedSeconds = remainingSeconds.ToString("00");

            // Combine formatted minutes and seconds with ":" separator
            string formattedTime = $"{formattedMinutes}:{formattedSeconds}";

            return formattedTime;
        }
    }
}
