namespace IZEncoder.UI.ViewModel.AvisynthParamUI
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using Common.AvisynthFilter;
    using Common.Helper;
    using MahApps.Metro.Controls;
    using View.AvisynthParamUI;

    public class IntPointAvisynthParamUIViewModel
        : AvisynthParamUIViewModelBase<IntPointAvisynthParamUI, IntPointAvisynthParamUIView>
    {
        private bool _nudxIsLeftDown;
        private Point _nudxLastPoint;

        private bool _nudyIsLeftDown;
        private Point _nudyLastPoint;

        public IntPointAvisynthParamUIViewModel(AvisynthParam param, IntPointAvisynthParamUI paramUI)
            : base(param, paramUI) { }

        public int? ValueX
        {
            get => Values[0] as int?;
            set
            {
                if (ValueX == value)
                    return;

                Values[0] = value;
                OnValueChanged(this, 0);
            }
        }

        public int? ValueY
        {
            get => Values[1] as int?;
            set
            {
                if (ValueY == value)
                    return;

                Values[1] = value;
                OnValueChanged(this, 1);
            }
        }


        public override string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(ValueX):
                        return Params[0].IsRequired && ValueX == null
                            ? "X Required"
                            : ParamUI.Validate(new IntPoint(ValueX, ValueY));
                    case nameof(ValueY):
                        return Params[1].IsRequired && ValueY == null
                            ? "Y Required"
                            : ParamUI.Validate(new IntPoint(ValueX, ValueY));
                    default:
                        return null;
                }
            }
        }

        protected override void OnValueChanged(object sender, int i)
        {
            base.OnValueChanged(sender, i);

            switch (i)
            {
                case 0:
                    NotifyOfPropertyChange(() => ValueX);
                    break;
                case 1:
                    NotifyOfPropertyChange(() => ValueY);
                    break;
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            View.nudx.Value = 0;
            View.nudx.SetBinding(NumericUpDown.ValueProperty,
                new Binding(nameof(ValueX))
                {
                    ValidatesOnDataErrors = true,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            View.nudx.PreviewMouseLeftButtonDown += NudX_PreviewMouseLeftButtonDown;
            View.nudx.PreviewMouseLeftButtonUp += NudX_PreviewMouseLeftButtonUp;
            View.nudx.PreviewMouseMove += NudX_PreviewMouseMove;

            View.nudy.Value = 0;
            View.nudy.SetBinding(NumericUpDown.ValueProperty,
                new Binding(nameof(ValueY))
                {
                    ValidatesOnDataErrors = true,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            View.nudy.PreviewMouseLeftButtonDown += NudY_PreviewMouseLeftButtonDown;
            View.nudy.PreviewMouseLeftButtonUp += NudY_PreviewMouseLeftButtonUp;
            View.nudy.PreviewMouseMove += NudY_PreviewMouseMove;
        }

        private void NudX_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _nudxIsLeftDown = true;
        }

        private void NudX_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _nudxIsLeftDown = false;
            Mouse.OverrideCursor = null;
        }

        private void NudX_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(sender as IInputElement);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (_nudxIsLeftDown && _nudxLastPoint != p)
                {
                    var delta = _nudxLastPoint - p;
                    var currentOrMin = ValueX ?? ParamUI.MinValueX;
                    var direction = _nudxLastPoint.GetMoveDirection(p);

                    switch (direction)
                    {
                        case MoveDirections.Left:
                        case MoveDirections.Right:
                            ValueX = (int) Math.Round(currentOrMin - delta.X)
                                .Clamp(ParamUI.MinValueX, ParamUI.MaxValueX);
                            break;
                        case MoveDirections.Up:
                        case MoveDirections.Down:
                            ValueX = (int) Math.Round(currentOrMin + delta.Y)
                                .Clamp(ParamUI.MinValueX, ParamUI.MaxValueX);
                            break;
                    }

                    Mouse.OverrideCursor = Cursors.SizeAll;
                }
            }
            else
            {
                _nudxIsLeftDown = false;
                Mouse.OverrideCursor = null;
            }

            _nudxLastPoint = p;
        }

        private void NudY_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _nudyIsLeftDown = true;
        }

        private void NudY_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _nudyIsLeftDown = false;
            Mouse.OverrideCursor = null;
        }

        private void NudY_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(sender as IInputElement);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (_nudyIsLeftDown && _nudyLastPoint != p)
                {
                    var delta = _nudyLastPoint - p;
                    var currentOrMin = ValueY ?? ParamUI.MinValueY;
                    var direction = _nudyLastPoint.GetMoveDirection(p);

                    switch (direction)
                    {
                        case MoveDirections.Left:
                        case MoveDirections.Right:
                            ValueY = (int) Math.Round(currentOrMin - delta.X)
                                .Clamp(ParamUI.MinValueY, ParamUI.MaxValueY);
                            break;
                        case MoveDirections.Up:
                        case MoveDirections.Down:
                            ValueY = (int) Math.Round(currentOrMin + delta.Y)
                                .Clamp(ParamUI.MinValueY, ParamUI.MaxValueY);
                            break;
                    }

                    Mouse.OverrideCursor = Cursors.SizeAll;
                }
            }
            else
            {
                _nudyIsLeftDown = false;
                Mouse.OverrideCursor = null;
            }

            _nudyLastPoint = p;
        }
    }
}