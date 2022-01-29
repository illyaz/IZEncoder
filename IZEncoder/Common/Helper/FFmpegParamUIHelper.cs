namespace IZEncoder.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FFmpegEncoder;
    using UI.ViewModel.FFmpegParamUI;

    public static class FFmpegParamUIHelper
    {
        public static IEnumerable<IFFmpegParamUIViewModelBase> GetViewModels(params FFmpegParam[] @params)
        {
            var uivms = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IFFmpegParamUIViewModelBase).IsAssignableFrom(p)).ToList();

            foreach (var avisynthParam in @params)
            {
                if (avisynthParam.UI == null)
                    continue;

                var ui = uivms.FirstOrDefault(x => x.Name.Equals(avisynthParam.UI.GetType().Name + "ViewModel"));

                if (ui == null)
                    continue;

                yield return (IFFmpegParamUIViewModelBase) Activator.CreateInstance(ui, avisynthParam,
                    avisynthParam.UI);
            }
        }
    }
}