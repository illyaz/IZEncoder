namespace IZEncoder.UI.ViewModel.AvisynthParamUI
{
    using Common.AvisynthFilter;
    using View.AvisynthParamUI;

    public class BooleanAvisynthParamUIViewModel
        : AvisynthParamUIViewModelBase<BooleanAvisynthParamUI, BooleanAvisynthParamUIView>
    {
        public BooleanAvisynthParamUIViewModel(AvisynthParam param, BooleanAvisynthParamUI paramUI)
            : base(param, paramUI) { }

        public bool? Value
        {
            get => Values[0] as bool?;
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
    }
}