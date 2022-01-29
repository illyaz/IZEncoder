﻿namespace IZEncoder.UI.ViewModel.AvisynthParamUI
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using Common.AvisynthFilter;
    using Common.Helper;
    using MahApps.Metro.Controls;
    using View.AvisynthParamUI;

    public class IntAvisynthParamUIViewModel
        : AvisynthParamUIViewModelBase<IntAvisynthParamUI, IntAvisynthParamUIView>
    {
        private bool _nudIsLeftDown;
        private Point _nudLastPoint;

        public IntAvisynthParamUIViewModel(AvisynthParam param, IntAvisynthParamUI paramUI)
            : base(param, paramUI) { }

        public int? Value
        {
            get => Values[0] as int?;
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

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            View.nud.Value = 0;
            View.nud.SetBinding(NumericUpDown.ValueProperty,
                new Binding(nameof(Value))
                {
                    ValidatesOnDataErrors = true,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            View.nud.PreviewMouseLeftButtonDown += Nud_PreviewMouseLeftButtonDown;
            View.nud.PreviewMouseLeftButtonUp += Nud_PreviewMouseLeftButtonUp;
            View.nud.PreviewMouseMove += Nud_PreviewMouseMove;
        }

        private void Nud_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _nudIsLeftDown = true;
        }

        private void Nud_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _nudIsLeftDown = false;
            Mouse.OverrideCursor = null;
        }

        private void Nud_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(sender as IInputElement);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (_nudIsLeftDown && _nudLastPoint != p)
                {
                    var delta = _nudLastPoint - p;
                    var currentOrMin = Value ?? ParamUI.MinValue;
                    var direction = _nudLastPoint.GetMoveDirection(p);

                    switch (direction)
                    {
                        case MoveDirections.Left:
                        case MoveDirections.Right:
                            Value = (int) Math.Round(currentOrMin - delta.X).Clamp(ParamUI.MinValue, ParamUI.MaxValue);
                            break;
                        case MoveDirections.Up:
                        case MoveDirections.Down:
                            Value = (int) Math.Round(currentOrMin + delta.Y).Clamp(ParamUI.MinValue, ParamUI.MaxValue);
                            break;
                    }

                    Mouse.OverrideCursor = Cursors.SizeAll;
                }
            }
            else
            {
                _nudIsLeftDown = false;
                Mouse.OverrideCursor = null;
            }

            _nudLastPoint = p;
        }
    }
}