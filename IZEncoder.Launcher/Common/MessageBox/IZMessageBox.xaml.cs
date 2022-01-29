namespace IZEncoder.Launcher.Common.MessageBox
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Media;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using MahApps.Metro.Controls;
    using MahApps.Metro.IconPacks;

    /// <summary>
    ///     Interaction logic for IZMessageBox.xaml
    /// </summary>
    public partial class IZMessageBox : MetroWindow
    {
        private readonly Span _inline;
        private string _focusBtn;
        private SystemSound _sound;

        public IZMessageBox()
        {
            InitializeComponent();
            _inline = new Span();

            Loaded += IZMessageBox_Loaded;
        }

        public string Result { get; private set; }

        public string Title
        {
            get => Dispatcher.Invoke(() => PART_Title.Text);
            set => Dispatcher.Invoke(() => PART_Title.Text = value);
        }

        private void IZMessageBox_Loaded(object sender, RoutedEventArgs e)
        {
            MaxWidth = SystemParameters.WorkArea.Width * .8;
            MaxHeight = SystemParameters.WorkArea.Height * .8;
            PART_Root.PreviewMouseMove += PART_Root_PreviewMouseMove;

            FocusManager.SetFocusedElement(PART_Buttons,
                PART_Buttons.Children.OfType<Button>().FirstOrDefault(x => x.Content.ToString() == _focusBtn));

            _sound?.Play();
        }

        private void PART_Root_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed
                && PART_Root.IsMouseDirectlyOver)
                DragMove();
        }

        private void UpdateContent()
        {
            PART_ContentShadow.Inlines.Clear();
            PART_ContentShadow.Inlines.Add(_inline);
            PART_ContentShadow.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var mWidth = Math.Ceiling(PART_ContentShadow.DesiredSize.Width);
            var mHeight = Math.Ceiling(PART_ContentShadow.DesiredSize.Height);

            PART_Content.Document.PageWidth = mWidth + 10;
            PART_Content.Document.PageHeight = mHeight;

            ((Paragraph) PART_Content.Document.Blocks.FirstBlock).Inlines.Clear();
            ((Paragraph) PART_Content.Document.Blocks.FirstBlock).Inlines.Add(_inline);

            PART_Content.Document.PagePadding = new Thickness(0);
        }

        #region FluentAPI

        public string GetMessageText()
        {
            return Dispatcher.Invoke(() =>
                new TextRange(PART_Content.Document.ContentStart, PART_Content.Document.ContentEnd).Text);
        }

        public IZMessageBox SetIcon(PackIconMaterialKind kind)
        {
            Dispatcher.Invoke(() => PART_Icon.Kind = kind);
            return this;
        }

        public IZMessageBox SetSound(SystemSound sound)
        {
            _sound = sound;
            return this;
        }

        public IZMessageBox ClearButton()
        {
            Dispatcher.Invoke(() => PART_Buttons.Children.Clear());
            return this;
        }

        public IZMessageBox FocusButton(string name = null)
        {
            _focusBtn = name ?? Dispatcher.Invoke(() =>
                            PART_Buttons.Children.OfType<Button>().FirstOrDefault()?.Content.ToString());
            return this;
        }

        public IZMessageBox AddAccentButton(string text, Action<Button> extend = null,
            Func<RoutedEventArgs, bool> extendClick = null)
        {
            return AddButton(text, x =>
            {
                x.SetResourceReference(BackgroundProperty, "AccentColorBrush");
                extend?.Invoke(x);
            }, extendClick);
        }

        public IZMessageBox AddSuccessButton(string text, Action<Button> extend = null,
            Func<RoutedEventArgs, bool> extendClick = null)
        {
            return AddButton(text, x =>
            {
                x.Background = (SolidColorBrush) new BrushConverter().ConvertFrom("#CC60A917");
                extend?.Invoke(x);
            }, extendClick);
        }

        public IZMessageBox AddWarnButton(string text, Action<Button> extend = null,
            Func<RoutedEventArgs, bool> extendClick = null)
        {
            return AddButton(text, x =>
            {
                x.Background = (SolidColorBrush) new BrushConverter().ConvertFrom("#CCFA6800");
                extend?.Invoke(x);
            }, extendClick);
        }

        public IZMessageBox AddErrorButton(string text, Action<Button> extend = null,
            Func<RoutedEventArgs, bool> extendClick = null)
        {
            return AddButton(text, x =>
            {
                x.Background = (SolidColorBrush) new BrushConverter().ConvertFrom("#CCE51400");
                extend?.Invoke(x);
            }, extendClick);
        }

        public IZMessageBox AddButton(string text, Action<Button> extend = null,
            Func<RoutedEventArgs, bool> extendClick = null)
        {
            Dispatcher.Invoke(() =>
            {
                var btn = new Button {Content = text};
                btn.Click += (s, e) =>
                {
                    var stop = !extendClick?.Invoke(e) ?? false;

                    if (stop)
                        return;

                    Result = btn.Content.ToString();
                    Close();
                };
                extend?.Invoke(btn);
                PART_Buttons.Children.Insert(0, btn);
            });
            return this;
        }

        public IZMessageBox AddButton(IZMessageBoxButton button)
        {
            switch (button)
            {
                case IZMessageBoxButton.Ok:
                    return AddButton("Ok").FocusButton();
                case IZMessageBoxButton.OkCancel:
                    return AddButton("Cancel").AddButton("Ok").FocusButton();
                case IZMessageBoxButton.NoCancel:
                    return AddButton("Cancel").AddButton("No").FocusButton();
                case IZMessageBoxButton.YesNo:
                    return AddButton("No").FocusButton().AddButton("Yes");
                case IZMessageBoxButton.YesNoCancel:
                    return AddButton("Cancel").AddButton("No").FocusButton().AddButton("Yes");
                case IZMessageBoxButton.YesNoRetry:
                    return AddButton("No").FocusButton().AddButton("Yes").AddButton("Retry");
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }

        private Run CreateRun(string text, string color = null, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null)
        {
            var r = new Run(text);
            if (!string.IsNullOrEmpty(color))
                r.Foreground = new BrushConverter().ConvertFromString(color) as SolidColorBrush;

            if (!string.IsNullOrEmpty(font))
                r.FontFamily = new FontFamily(font);

            if (fontSize > 0)
                r.FontSize = fontSize;

            if (fontWeight != null)
                r.FontWeight = (FontWeight) fontWeight;
            return r;
        }

        public IZMessageBox ClearText()
        {
            Dispatcher.Invoke(() =>
            {
                _inline.Inlines.Clear();
                UpdateContent();
            });

            return this;
        }

        public IZMessageBox AddText(string text, string color = null, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null)
        {
            Dispatcher.Invoke(() =>
            {
                _inline.Inlines.Add(CreateRun(text, color, font, fontSize, fontWeight));
                UpdateContent();
            });

            return this;
        }

        public IZMessageBox AddSuccessText(string text, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null)
        {
            return AddText(text, "#CC60A917", font, fontSize, fontWeight);
        }

        public IZMessageBox AddWarnText(string text, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null)
        {
            return AddText(text, "#CCFA6800", font, fontSize, fontWeight);
        }

        public IZMessageBox AddErrorText(string text, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null)
        {
            return AddText(text, "#CCE51400", font, fontSize, fontWeight);
        }

        public IZMessageBox AddHyperLink(string text, Uri uri, string color = null, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            Dispatcher.Invoke(() =>
            {
                var r = new Hyperlink(CreateRun(text, color, font, fontSize, fontWeight)) {NavigateUri = uri};
                if (customHandler != null)
                    r.RequestNavigate += customHandler;
                else
                    r.RequestNavigate += (s, e) =>
                    {
                        Process.Start(e.Uri.AbsoluteUri);
                        e.Handled = true;
                    };

                r.ToolTip = uri.OriginalString;
                _inline.Inlines.Add(r);
                UpdateContent();
            });

            return this;
        }

        public IZMessageBox AddSuccessHyperLink(string text, string uri, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            return AddSuccessHyperLink(text, new Uri(uri), font, fontSize, fontWeight, customHandler);
        }

        public IZMessageBox AddSuccessHyperLink(string text, Uri uri, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            return AddHyperLink(text, uri, "#CC60A917", font, fontSize, fontWeight, customHandler);
        }

        public IZMessageBox AddWarnHyperLink(string text, string uri, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            return AddWarnHyperLink(text, new Uri(uri), font, fontSize, fontWeight, customHandler);
        }

        public IZMessageBox AddWarnHyperLink(string text, Uri uri, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            return AddHyperLink(text, uri, "#CCFA6800", font, fontSize, fontWeight, customHandler);
        }

        public IZMessageBox AddErrorHyperLink(string text, string uri, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            return AddSuccessHyperLink(text, new Uri(uri), font, fontSize, fontWeight, customHandler);
        }

        public IZMessageBox AddErrorHyperLink(string text, Uri uri, string font = null,
            int fontSize = -1, FontWeight? fontWeight = null, RequestNavigateEventHandler customHandler = null)
        {
            return AddHyperLink(text, uri, "#CCE51400", font, fontSize, fontWeight, customHandler);
        }


        //public IZMessageBox AddErrorShowInExplorer(string text, string uri, string color = null, string font = null,
        //    int fontSize = -1, FontWeight? fontWeight = null)
        //{
        //    return AddShowInExplorer(text, new Uri(uri), "#CCE51400", font, fontSize, fontWeight);
        //}

        //public IZMessageBox AddShowInExplorer(string text, string uri, string color = null, string font = null,
        //    int fontSize = -1, FontWeight? fontWeight = null)
        //{
        //    return AddShowInExplorer(text, new Uri(uri), color, font, fontSize, fontWeight);
        //}

        //public IZMessageBox AddShowInExplorer(string text, Uri uri, string color = null, string font = null,
        //    int fontSize = -1, FontWeight? fontWeight = null)
        //{
        //    return AddHyperLink(text, uri, color, font, fontSize, fontWeight, (s, e) =>
        //    {
        //        var fi = new FileInfo(uri.LocalPath);
        //        Shell32Helper.OpenFolderAndSelectItem(fi.DirectoryName, fi.Name);

        //        e.Handled = true;
        //    });
        //}

        public IZMessageBox TrimLines()
        {
            Dispatcher.Invoke(() =>
            {
                while (_inline.Inlines.LastInline is LineBreak lb)
                    _inline.Inlines.Remove(lb);

                //var tr = new TextRange(_inline.Inlines.LastInline.ContentStart, _inline.Inlines.LastInline.ContentEnd);
                //tr.Text = tr.Text.Trim();
            });
            return this;
        }

        public IZMessageBox AddLine(int count = 1)
        {
            Dispatcher.Invoke(() =>
            {
                for (var i = 0; i < count; i++)
                    _inline.Inlines.Add(new LineBreak());

                UpdateContent();
            });

            return this;
        }

        #endregion
    }

    public enum IZMessageBoxButton
    {
        Ok,
        OkCancel,
        NoCancel,
        YesNo,
        YesNoCancel,
        YesNoRetry
    }
}