using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VideoEditor.Views
{
    /// <summary>
    /// Animated splash screen shown while the main window is loading.
    /// Call BeginLoadSequence() after showing to start the animation sequence.
    /// The window closes itself after the progress bar completes.
    /// </summary>
    public partial class AppSplashScreen : Window
    {
        private readonly DispatcherTimer _statusTimer;
        private int _statusStep;
        private Action? _onFinished;

        private static readonly string[] StatusMessages =
        [
            "Initialising…",
            "Loading resources…",
            "Starting timeline engine…",
            "Preparing workspace…",
            "Almost ready…",
        ];

        public AppSplashScreen()
        {
            InitializeComponent();

            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(560)
            };
            _statusTimer.Tick += StatusTimer_Tick;
        }

        /// <summary>
        /// Starts all animations. Pass an optional callback to invoke when the splash closes.
        /// </summary>
        public void BeginLoadSequence(Action? onFinished = null)
        {
            _onFinished = onFinished;

            // Fade in
            var fadeIn = (Storyboard)Resources["FadeInStoryboard"];
            fadeIn.Begin();

            // Progress bar fill
            var progress = (Storyboard)Resources["ProgressStoryboard"];
            progress.Completed += Progress_Completed;
            progress.Begin();

            // Logo pulse
            var pulse = (Storyboard)Resources["LogoPulse"];
            pulse.Begin();

            // Status messages cycling
            _statusStep = 0;
            _statusTimer.Start();
        }

        // ── Private helpers ──────────────────────────────────────────────

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            _statusStep++;
            if (_statusStep < StatusMessages.Length)
            {
                StatusText.Text = StatusMessages[_statusStep];
            }
            else
            {
                _statusTimer.Stop();
                StatusText.Text = "Ready  ✓";
            }
        }

        private void Progress_Completed(object? sender, EventArgs e)
        {
            _statusTimer.Stop();
            StatusText.Text = "Ready  ✓";

            // Small pause, then fade out
            var pause = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            pause.Tick += (_, _) =>
            {
                pause.Stop();
                var fadeOut = (Storyboard)Resources["FadeOutStoryboard"];
                fadeOut.Begin();
            };
            pause.Start();
        }

        /// <summary>
        /// Called by the FadeOutStoryboard Completed event — defined in XAML.
        /// </summary>
        private void FadeOut_Completed(object sender, EventArgs e)
        {
            _onFinished?.Invoke();
            Close();
        }
    }
}
