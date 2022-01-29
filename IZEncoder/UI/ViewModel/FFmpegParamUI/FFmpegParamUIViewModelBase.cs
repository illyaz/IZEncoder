namespace IZEncoder.UI.ViewModel.FFmpegParamUI
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Common.FFmpegEncoder;

    public interface IFFmpegParamUIViewModelBase
    {
        FFmpegParam Param { get; }
        IFFmpegParamUI ParamUI { get; }
        FFmpegParam[] Params { get; }
        object[] Values { get; }
        void SetParamExtented(int i, FFmpegParam param);
        object GetValue(int i);
        void SetValue(int i, object value);
        event EventHandler<int> ValueChanged;
    }

    public abstract class FFmpegParamUIViewModelBase<TParamUI, TView> : FFmpegParamUIViewModelBase<TView>
        where TParamUI : IFFmpegParamUI
        where TView : FrameworkElement
    {
        protected FFmpegParamUIViewModelBase(FFmpegParam param, TParamUI paramUI)
            : base(param, paramUI)
        {
            ParamUI = paramUI;
        }

        public new TParamUI ParamUI { get; }
    }

    public abstract class FFmpegParamUIViewModelBase<TView> : IZEScreen<TView>, IFFmpegParamUIViewModelBase,
        IDataErrorInfo
        where TView : FrameworkElement
    {
        protected FFmpegParamUIViewModelBase(FFmpegParam param, IFFmpegParamUI paramUI)
        {
            Param = param;
            ParamUI = paramUI;

            if (ParamUI is IFFmpegParamUIExtented ext)
            {
                Params = new FFmpegParam[1 + ext.ExtentedParamNames.Length];
                Values = new object[1 + ext.ExtentedParamNames.Length];
            }
            else
            {
                Params = new FFmpegParam[1];
                Values = new object[1];
            }

            Params[0] = param;
        }

        public virtual string this[string columnName] => null;

        public virtual string Error { get; }

        public FFmpegParam[] Params { get; }
        public object[] Values { get; }

        public virtual void SetParamExtented(int i, FFmpegParam param)
        {
            Params[i + 1] = param;
        }

        public virtual object GetValue(int i)
        {
            return Values[i];
        }

        public virtual void SetValue(int i, object value)
        {
            Values[i] = value;
            OnValueChanged(this, i);
        }

        public event EventHandler<int> ValueChanged;

        public FFmpegParam Param { get; }
        public IFFmpegParamUI ParamUI { get; }

        protected virtual void OnValueChanged(object sender, int i)
        {
            ValueChanged?.Invoke(sender, i);
        }
    }
}