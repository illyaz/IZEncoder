namespace IZEncoder.UI.ViewModel
{
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using Caliburn.Micro;
    using Common.FFmpegEncoder;
    using Common.Helper;
    using FFmpegParamUI;
    using View;

    public sealed class TemplateVideoSettingsViewModel : IZEScreen<TemplateVideoSettingsView>
    {
        private bool _loaded;

        public TemplateVideoSettingsViewModel(Global g)
        {
            G = g;
            DisplayName = "Video Encoder";
        }

        public Global G { get; }

        public BindableCollection<FFmpegVideoParameter> FFmpegVideoParameters { get; set; } =
            new BindableCollection<FFmpegVideoParameter>();

        public FFmpegVideoParameter SelectedVideoParameter { get; set; }
        public bool IsEmpty { get; set; }
        public TemplateSettingsViewModel ParentVm { get; set; }

        public BindableCollection<IFFmpegParamUIViewModelBase> FFmpegParamUICollection { get; set; } =
            new BindableCollection<IFFmpegParamUIViewModelBase>();

        public void OnSelectedVideoParameterChanged()
        {
            var vguid = SelectedVideoParameter.Guid;
            if (vguid != ParentVm.Template.Video)
                ParentVm.SaveActions.Add(tpl =>
                {
                    tpl.Video = vguid;
                    tpl.VideoSettings.Clear();
                });

            foreach (var paramUi in FFmpegParamUICollection)
                paramUi.ValueChanged -= ParamUi_ValueChanged;

            FFmpegParamUICollection.Clear();
            FFmpegParamUICollection.AddRange(
                FFmpegParamUIHelper.GetViewModels(SelectedVideoParameter.Params.ToArray()));
            IsEmpty = FFmpegParamUICollection.Count == 0;
            foreach (var paramUi in FFmpegParamUICollection)
            {
                if (ParentVm.Template.VideoSettings.ContainsKey(paramUi.Param.Name))
                    paramUi.SetValue(0,
                        paramUi.Param.GetValue(ParentVm.Template.VideoSettings[paramUi.Param.Name]?.ToString()));
                else if (paramUi.Param.Default != null)
                    paramUi.SetValue(0, paramUi.Param.Default);

                paramUi.ValueChanged += ParamUi_ValueChanged;
            }

            ParentVm.VideoSettingsError = HasError();
        }

        private void ParamUi_ValueChanged(object sender, int e)
        {
            var ui = (IFFmpegParamUIViewModelBase) sender;

            ParentVm.SaveActions.Add(tpl =>
            {
                if (tpl.VideoSettings.ContainsKey(ui.Param.Name))
                    tpl.VideoSettings[ui.Param.Name] = ui.GetValue(e);
                else
                    tpl.VideoSettings.Add(ui.Param.Name, ui.GetValue(e));
            });

            ParentVm.VideoSettingsError = HasError();
        }

        private bool HasError()
        {
            var error = false;
            foreach (var paramVm in FFmpegParamUICollection)
            {
                if (error)
                    break;

                if (!(paramVm is IDataErrorInfo v))
                    continue;

                foreach (var propertyInfo in paramVm.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public))
                {
                    if (error)
                        break;

                    error = !string.IsNullOrEmpty(v[propertyInfo.Name]);
                }
            }

            return error;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            if (_loaded) return;

            _loaded = true;
            FFmpegVideoParameters.AddRange(G.FFmpegParameters.OfType<FFmpegVideoParameter>());

            SelectedVideoParameter = FFmpegVideoParameters.FirstOrDefault(x => x.Guid == ParentVm.Template.Video);
            ParentVm.VideoSettingsError = HasError();
        }
    }
}