namespace IZEncoder.UI.ViewModel {
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using JetBrains.Annotations;

    public class ActionValidationRule : ValidationRule
    {
        private readonly Func<object, string> _func;

        public ActionValidationRule(Func<object, string> func)
        {
            _func = func;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var result = _func(value);
                return string.IsNullOrEmpty(result)
                    ? ValidationResult.ValidResult
                    : new ValidationResult(false, result);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, e.Message);
            }
        }
        
        public static void Attach([NotNull] FrameworkElement element, [NotNull] DependencyProperty dp, [NotNull] Func<object, string> func)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (dp == null) throw new ArgumentNullException(nameof(dp));
            if (func == null) throw new ArgumentNullException(nameof(func));

            var bindingExpression = element.GetBindingExpression(dp);
            if(bindingExpression?.ParentBinding == null)
                throw new InvalidOperationException("binding not found");

            bindingExpression.ParentBinding.ValidationRules.Add(new ActionValidationRule(func));
        }
    }
}