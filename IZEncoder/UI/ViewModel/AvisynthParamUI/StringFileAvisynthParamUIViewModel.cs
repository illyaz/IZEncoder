namespace IZEncoder.UI.ViewModel.AvisynthParamUI
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Common.AvisynthFilter;
    using Microsoft.Win32;
    using View.AvisynthParamUI;

    public class StringFileAvisynthParamUIViewModel
        : AvisynthParamUIViewModelBase<StringFileAvisynthParamUI, StringFileAvisynthParamUIView>
    {
        public StringFileAvisynthParamUIViewModel(AvisynthParam param, StringFileAvisynthParamUI paramUI)
            : base(param, paramUI) { }

        public string Value
        {
            get => Values[0] as string;
            set
            {
                if (Value == value)
                    return;

                Values[0] = value;
                OnValueChanged(this, 0);
            }
        }

        public override string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Value))
                    return Param.IsRequired && Value == null
                        ? "Required"
                        : ParamUI.Validate(Value);

                return null;
            }
        }

        protected override void OnValueChanged(object sender, int i)
        {
            base.OnValueChanged(sender, i);
            if (i == 0)
                NotifyOfPropertyChange(() => Value);
        }

        public void PreviewDragOver(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.None;

            if (ParamUI.Filters != null)
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] datas
                    && datas.Length == 1)
                {
                    var ext = new FileInfo(datas[0]).Extension.TrimStart('.');
                    if (!ParamUI.Filters.Keys.Where(x => x != "_ALL_")
                        .Any(x => x == "*" || x.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                        return;
                }
                else
                {
                    return;
                }
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        public void PreviewDrop(DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] datas
                && datas.Length == 1)
                Value = datas[0];
        }

        public void PreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            var ofd = new OpenFileDialog();
            if (ParamUI.Filters != null)
                ofd.Filter = string.Join("|", ParamUI.Filters.GroupBy(x => x.Value)
                    .Select(x =>
                        $"{x.Key}|" + string.Join(";", x.Select(v =>
                            v.Key == "_ALL_"
                                ? string.Join(";",
                                    ParamUI.Filters.Where(w => w.Key != "_ALL_").Select(_ => $" *.{_.Key}"))
                                : $"*.{v.Key}"
                        ))));

            if (ofd.ShowDialog() is bool b && b)
                Value = ofd.FileName;
        }
    }
}