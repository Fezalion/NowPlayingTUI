using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace NowPlayingTUI
{
    public class SongTimer
    {
        public int End { get; private set; }
        private int ElapsedTime { get; set; }
        public Timer Timer { get; private set; }

        public SongTimer(int duration)
        {
            Start(duration);
        }

        public void Start(int end)
        {
            ElapsedTime = 0;
            StopTimer();

            End = end;
            Timer = new Timer(1000);
            Timer.Elapsed += TimerElapsed;
            Timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (End <= ElapsedTime)
            {
                Timer.Stop();
                return;
            }

            string formattedElapsedTime = MillisecondsToFormattedString(ElapsedTime);
            string formattedEnd = MillisecondsToFormattedString(End);
            string formattedString = $"{formattedElapsedTime}/{formattedEnd}";

            if (End == 0)
            {
                formattedString = string.Empty;
            }

            ConsoleX.WriteAt(formattedString, 35, 0, Spectre.Console.Color.Lime);
            ElapsedTime += 1000;
        }

        public void StopTimer()
        {
            Timer?.Stop();
        }

        private static string MillisecondsToFormattedString(int milliseconds)
        {
            int seconds = milliseconds / 1000;
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
