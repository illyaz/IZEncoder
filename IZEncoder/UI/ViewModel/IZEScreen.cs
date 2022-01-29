namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using Caliburn.Micro;
    using Common.Helper;

    // ReSharper disable once InconsistentNaming
    public abstract class IZEScreen<T> : Screen
        where T : FrameworkElement
    {
        private Config.IWindowPosition _windowPositionConfig;
        private Config.IWindowSize _windowSizeConfig;
        private Config.IWindowState _windowStateConfig;

        protected virtual T View => GetView() as T;
        protected virtual Window Window => GetView() as Window;
        protected event EventHandler OnWindowClosed;

        public void BringToFront()
        {
            if (!typeof(Window).IsAssignableFrom(typeof(T)))
                return;

            OnUIThread(() =>
            {
                if (Window.WindowState == WindowState.Minimized)
                    Window.WindowState = WindowState.Normal;

                var top = Window.Topmost;
                if(!top)
                    Window.Topmost = true;
                Window.Activate();
                if(!top)
                    Window.Topmost = false;
            });
        }

        public async Task WaitClosed()
        {
            if (!typeof(Window).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException();

            while (Application.Current.Dispatcher.Invoke(Window.IsWindowOpen))
                await Task.Delay(1);
        }

        protected void UseConfig(Config.IWindowPosition windowPositionConfig)
        {
            _windowPositionConfig = windowPositionConfig;
        }

        protected void UseConfig(Config.IWindowSize windowSizeConfig)
        {
            _windowSizeConfig = windowSizeConfig;
        }

        protected void UseConfig(Config.IWindowState windowStateConfig)
        {
            _windowStateConfig = windowStateConfig;
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

            if (Window != null)
            {
                Window.Closed += IZEScreen_OnWindowClosed;

                if (_windowPositionConfig?.WindowPosition != null)
                {
                    Window.Left = _windowPositionConfig.WindowPosition.Value.X;
                    Window.Top = _windowPositionConfig.WindowPosition.Value.Y;
                }

                if (_windowSizeConfig?.WindowSize != null)
                {
                    Window.Width = _windowSizeConfig.WindowSize.Value.Width;
                    Window.Height = _windowSizeConfig.WindowSize.Value.Height;
                }

                if (_windowStateConfig?.WindowState != null)
                    Window.WindowState = _windowStateConfig.WindowState.Value;
            }
        }

        private void IZEScreen_OnWindowClosed(object sender, EventArgs e)
        {
            var w = (Window) sender;
            OnWindowClosed?.Invoke(this, e);

            if (_windowPositionConfig != null)
                _windowPositionConfig.WindowPosition = new Point(w.Left, w.Top);


            if (_windowSizeConfig != null)
                _windowSizeConfig.WindowSize = new Size(w.Width, w.Height);


            if (_windowStateConfig != null)
                _windowStateConfig.WindowState = w.WindowState;
        }

        public virtual bool IsClosed()
        {
            return Window == null ? true : !Window.IsWindowOpen();
        }
    }

    public abstract class IZEPopup<T> : IZEScreen<T>
        where T : FrameworkElement
    {
        protected virtual T View => Popup?.Child as T;
        protected Popup Popup => GetView() as Popup;
    }
}