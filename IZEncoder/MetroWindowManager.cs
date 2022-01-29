namespace IZEncoder
{
    using System.Windows;

    using Caliburn.Micro;

    using MahApps.Metro.Controls;

    public class MetroWindowManager : WindowManager 
    {
        protected override Window EnsureWindow(object model, object view, bool isDialog)
        {
            if (view is Window window)
            {
                var owner = InferOwnerOf(window);
                if (owner != null && isDialog)
                {
                    window.Owner = owner;
                }
            }
            else
            {
                window = new MetroWindow
                {
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight
                };

                window.SetValue(View.IsGeneratedProperty, true);

                var owner = InferOwnerOf(window);
                if (owner != null)
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = owner;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }

            return window;
        }
    }
}
