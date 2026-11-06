using Microsoft.Maui.Controls;

namespace clocklab
{
    public class ClockView : ContentView
    {
        private readonly GraphicsView _view;
        private readonly SevenSegmentDrawable _drawable = new();
        private readonly IDispatcherTimer _timer;

        public ClockView()
        {
            _view = new GraphicsView { Drawable = _drawable };
            Content = _view;

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) => UpdateTime();
            _timer.Start();

            UpdateTime();
        }

        private void UpdateTime()
        {
            _drawable.TimeText = DateTime.Now.ToString("HH:mm:ss");
            _drawable.Labels = Array.Empty<string>();
            _drawable.DigitWidth = 15f;
            _view.Invalidate();
        }
    }
}