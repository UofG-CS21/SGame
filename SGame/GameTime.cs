using System;
using System.Diagnostics;

namespace SGame
{
    /// <summary>
    /// The mode for a `GameTime`.
    /// </summary>
    enum GameTimeMode
    {
        Stopwatch, ///< Use a real stopwatch.
        Manual, ///< Manually set the time.
    };

    /// <summary>
    /// Measures delta times for the game.
    /// </summary>
    class GameTime
    {
        /// <summary>
        /// Inits the GameTime given its mode.
        /// Starts the internal stopwatch.
        /// </summary>
        public GameTime(GameTimeMode mode = GameTimeMode.Stopwatch)
        {
            this.Mode = mode;
            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
        }

        /// <summary>
        /// The timer's mode.
        /// </summary>
        public GameTimeMode Mode
        {
            get; set;
        }

        private long manualMs;
        private Stopwatch stopwatch;

        /// <summary>
        /// The elapsed time in milliseconds (either stopwatch or manually-set time, depending on mode).
        /// </summary>
        public long ElapsedMilliseconds
        {
            get
            {
                switch (Mode)
                {
                    case GameTimeMode.Stopwatch:
                        return stopwatch.ElapsedMilliseconds;
                    default: // Manual
                        return manualMs;
                }
            }
        }

        /// <summary>
        /// Resets the elapsed time (either stopwatch or manually-set time, depending on mode).
        /// </summary>
        public void Reset()
        {
            switch (Mode)
            {
                case GameTimeMode.Stopwatch:
                    stopwatch.Reset();
                    break;
                default: // Manual
                    manualMs = 0;
                    break;
            }
        }

        /// <summary>
        /// Set the elapsed time manually. Switches to manual mode (if it wasn't already).
        /// </summary>
        public void SetElapsedMillisecondsManually(long ms)
        {
            Mode = GameTimeMode.Manual;
            manualMs = ms;
        }
    }
}
