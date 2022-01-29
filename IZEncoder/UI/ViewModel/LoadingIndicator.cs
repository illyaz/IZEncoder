namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Caliburn.Micro;
    using MahApps.Metro.Controls;
    using Action = System.Action;

    public class LoadingIndicator : PropertyChangedBase
    {
        private readonly Panel _panel;
        private readonly UIElement _elem;
        private Grid _grid;
        private bool _isShow;
        private ProgressBar _progressBar;
        private Rectangle _rectangle;
        private int _refCount;
        private StackPanel _stackPanel;
        private TextBlock _textBlock;
        private TransitioningContentControl _transitioningContentControl;

        public LoadingIndicator(Panel panel, UIElement win = null)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _elem = win;
            Init();
        }

        public double Value { get; set; }
        public bool IsIndeterminate { get; set; }
        public string Text { get; set; }

        public bool IsShow
        {
            get => _isShow;
            set
            {
                if (_isShow == value)
                    return;

                _isShow = value;

                if (value)
                    _grid.Dispatcher.Invoke(() =>
                    {
                        _grid.Visibility = Visibility.Visible;
                        _transitioningContentControl.ReloadTransition();
                    });
                else
                    _grid.Dispatcher.Invoke(() => _grid.Visibility = Visibility.Hidden);

                NotifyOfPropertyChange(() => IsShow);
            }
        }

        private void Init()
        {
            _grid = new Grid();
            _rectangle = new Rectangle();
            _stackPanel = new StackPanel();
            _textBlock = new TextBlock();
            _progressBar = new ProgressBar();

            _rectangle.Fill = Brushes.Black;
            _rectangle.Opacity = 0.75;

            _transitioningContentControl = new TransitioningContentControl
            {
                Transition = TransitionType.Default,
                Content = _grid
            };

            if (_elem != null)
                _elem.MouseMove += _win_PreviewMouseMove;

            _grid.Visibility = Visibility.Hidden;
            _grid.VerticalAlignment = VerticalAlignment.Stretch;
            _grid.HorizontalAlignment = HorizontalAlignment.Stretch;

            _stackPanel.VerticalAlignment = VerticalAlignment.Center;
            _stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            _stackPanel.Orientation = Orientation.Horizontal;
            _stackPanel.Children.Add(_progressBar);
            _stackPanel.Children.Add(_textBlock);

            _progressBar.Style = Application.Current.FindResource("MaterialDesignCircularProgressBar") as Style;
            _progressBar.SetBinding(RangeBase.ValueProperty, new Binding("Value"));
            _progressBar.SetBinding(ProgressBar.IsIndeterminateProperty, new Binding("IsIndeterminate"));
            _textBlock.Foreground = Brushes.White;
            _textBlock.FontWeight = FontWeights.Normal;
            _textBlock.FontSize = 12;
            _textBlock.Margin = new Thickness(10, 0, 0, 0);
            _textBlock.VerticalAlignment = VerticalAlignment.Center;
            _textBlock.SetBinding(TextBlock.TextProperty, new Binding("Text"));
            _grid.DataContext = this;
            _grid.Children.Add(_rectangle);
            _grid.Children.Add(_stackPanel);

            Panel.SetZIndex(_transitioningContentControl, 999999);
            _panel.Children.Add(_transitioningContentControl);
        }

        private void _win_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_elem is Window _win && _isShow && e.LeftButton == MouseButtonState.Pressed)
                _win.DragMove();
        }

        public void Set(double percent, string text = null)
        {
            if (text != null && percent >= 0)
                Text = string.Format(text, percent);
            else if (text == null)
                Text = null;
            else
                Text = text;

            if (!IsShow)
                IsShow = true;

            IsIndeterminate = !(percent >= 0);
            Value = percent;
        }

        public Task Wait(CancellationToken? cancellationToken = null)
        {
            return Task.Factory.StartNew(() =>
            {
                while (_refCount > 0)
                    Thread.Sleep(1);
            });
        }

        public Task Run(Func<bool> action, int percent = -1, string text = "Loading ...")
        {
            return Task.Factory.StartNew(() =>
            {
                var result = false;
                try
                {
                    Interlocked.Increment(ref _refCount);
                    Set(percent, text);
                    result = action();
                }
                finally
                {
                    if (Interlocked.Decrement(ref _refCount) <= 0)
                        if (!result)
                            Completed();
                }
            });
        }

        public Task Run(Action action, int percent = -1, string text = "Loading ...")
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    Interlocked.Increment(ref _refCount);
                    Set(percent, text);
                    action();
                }
                finally
                {
                    if (Interlocked.Decrement(ref _refCount) <= 0)
                        Completed();
                }
            });
        }

        public async Task Run(Func<Task> action, int percent = -1, string text = "Loading ...")
        {
            try
            {
                Interlocked.Increment(ref _refCount);
                Set(percent, text);
                await action();
            }
            finally
            {
                if (Interlocked.Decrement(ref _refCount) <= 0)
                    Completed();
            }
        }

        public void Completed()
        {
            IsShow = false;
        }
    }
}