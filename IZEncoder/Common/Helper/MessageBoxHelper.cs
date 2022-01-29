namespace IZEncoder.Common.Helper
{
    using System.Linq;
    using System.Windows;
    using MessageBox;
    using SubtitleAnalyzer;

    internal static class MessageBoxHelper
    {
        public static IZMessageBox BuildAnalysisResult(this IZMessageBox ex, SubtitleAnalysisResult result)
        {
            if (result.HasError)
            {
                foreach (var grouping in result.MissingFontStyles.GroupBy(x => x.FontName))
                    ex.AddErrorText("Missing font").AddErrorText($" '{grouping.Key}'",
                            fontWeight: FontWeights.Bold)
                        .AddLine()
                        .AddErrorText("Used in styles: ")
                        .AddErrorText(
                            "\n - " + string.Join("\n - ", grouping.Select(x => x.Name)),
                            fontWeight: FontWeights.Bold)
                        .AddLine(2);

                foreach (var grouping in result.MissingInlineFonts)
                    ex.AddErrorText("Missing inline font (fn)").AddErrorText($" '{grouping.Key}'",
                            fontWeight: FontWeights.Bold)
                        .AddLine()
                        .AddErrorText("Used in lines:")
                        .AddErrorText(
                            " \n - " + string.Join(", ", grouping.Value.Take(5)) + (grouping.Value.Count > 5 ? $" ({grouping.Value.Count} lines)" : ""),
                            fontWeight: FontWeights.Bold)
                        .AddLine(2);

                foreach (var kv in result.MissingStyles)
                {
                    ex.AddErrorText("Style ")
                        .AddErrorText($"'{kv.Key}'", fontWeight: FontWeights.Bold)
                        .AddErrorText(" does not exist").AddLine();

                    if (kv.Value.Count > 25)
                        ex.AddErrorText("Used in ").AddErrorText($"'{kv.Value.Count}'",
                            fontWeight: FontWeights.Bold).AddErrorText(" lines");
                    else
                        ex.AddErrorText("Used in lines: ").AddErrorText(
                            "\n - " + string.Join("\n - ", kv.Value),
                            fontWeight: FontWeights.Bold);

                    ex.AddLine(2);
                }
            }

            return ex;
        }
    }
}