namespace IZEncoder.UI.ViewModel.AvisynthParamUI
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Common.AvisynthFilter;

    public interface IAvisynthParamUIViewModelBase
    {
        AvisynthParam Param { get; }
        IAvisynthParamUI ParamUI { get; }
        AvisynthParam[] Params { get; }
        object[] Values { get; }
        void SetParamExtented(int i, AvisynthParam param);
        object GetValue(int i);
        void SetValue(int i, object value);
        event EventHandler<int> ValueChanged;
    }

    public abstract class AvisynthParamUIViewModelBase<TParamUI, TView> : AvisynthParamUIViewModelBase<TView>
        where TParamUI : IAvisynthParamUI
        where TView : FrameworkElement
    {
        protected AvisynthParamUIViewModelBase(AvisynthParam param, TParamUI paramUI)
            : base(param, paramUI)
        {
            ParamUI = paramUI;
        }

        public new TParamUI ParamUI { get; }
    }

    public abstract class AvisynthParamUIViewModelBase<TView> : IZEScreen<TView>, IAvisynthParamUIViewModelBase,
        IDataErrorInfo
        where TView : FrameworkElement
    {
        protected AvisynthParamUIViewModelBase(AvisynthParam param, IAvisynthParamUI paramUI)
        {
            Param = param;
            ParamUI = paramUI;

            if (ParamUI is IAvisynthParamUIExtented ext)
            {
                Params = new AvisynthParam[1 + ext.ExtentedParamNames.Length];
                Values = new object[1 + ext.ExtentedParamNames.Length];
            }
            else
            {
                Params = new AvisynthParam[1];
                Values = new object[1];
            }

            Params[0] = param;
        }

        public AvisynthParam[] Params { get; }
        public object[] Values { get; }

        public virtual void SetParamExtented(int i, AvisynthParam param)
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
            ValueChanged?.Invoke(this, i);
        }

        public event EventHandler<int> ValueChanged;

        public AvisynthParam Param { get; }
        public IAvisynthParamUI ParamUI { get; }

        public virtual string this[string columnName] => null;

        public virtual string Error { get; }

        protected virtual void OnValueChanged(object sender, int i)
        {
            ValueChanged?.Invoke(sender, i);
        }
    }
}