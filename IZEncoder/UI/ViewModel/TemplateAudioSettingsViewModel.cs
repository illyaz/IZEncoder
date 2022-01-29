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

    public sealed class TemplateAudioSettingsViewModel : IZEScreen<TemplateAudioSettingsView>
    {
        private bool _loaded;

        public TemplateAudioSettingsViewModel(Global g)
        {
            G = g;
            DisplayName = "Audio Encoder";
        }

        public Global G { get; }

        public BindableCollection<FFmpegAudioParameter> FFmpegAudioParameters { get; set; } =
            new BindableCollection<FFmpegAudioParameter>();

        public FFmpegAudioParameter SelectedAudioParameter { get; set; }
        public bool IsEmpty { get; set; }
        public TemplateSettingsViewModel ParentVm { get; set; }

        public BindableCollection<IFFmpegParamUIViewModelBase> FFmpegParamUICollection { get; set; } =
            new BindableCollection<IFFmpegParamUIViewModelBase>();

        public void OnSelectedAudioParameterChanged()
        {
            var vguid = SelectedAudioParameter.Guid;
            if (vguid != ParentVm.Template.Audio)
                ParentVm.SaveActions.Add(tpl =>
                {
                    tpl.Audio = vguid;
                    tpl.AudioSettings.Clear();
                });

            foreach (var paramUi in FFmpegParamUICollection)
                paramUi.ValueChanged -= ParamUi_ValueChanged;

            FFmpegParamUICollection.Clear();
            FFmpegParamUICollection.AddRange(
                FFmpegParamUIHelper.GetViewModels(SelectedAudioParameter.Params.ToArray()));

            IsEmpty = FFmpegParamUICollection.Count == 0;

            foreach (var paramUi in FFmpegParamUICollection)
            {
                if (ParentVm.Template.AudioSettings.ContainsKey(paramUi.Param.Name))
                    paramUi.SetValue(0,
                        paramUi.Param.GetValue(ParentVm.Template.AudioSettings[paramUi.Param.Name]?.ToString()));
                else if (paramUi.Param.Default != null)
                    paramUi.SetValue(0, paramUi.Param.Default);

                paramUi.ValueChanged += ParamUi_ValueChanged;
            }

            ParentVm.AudioSettingsError = HasError();
        }

        private void ParamUi_ValueChanged(object sender, int e)
        {
            var ui = (IFFmpegParamUIViewModelBase) sender;

            ParentVm.SaveActions.Add(tpl =>
            {
                if (tpl.AudioSettings.ContainsKey(ui.Param.Name))
                    tpl.AudioSettings[ui.Param.Name] = ui.GetValue(e);
                else
                    tpl.AudioSettings.Add(ui.Param.Name, ui.GetValue(e));
            });

            ParentVm.AudioSettingsError = HasError();
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
            FFmpegAudioParameters.AddRange(G.FFmpegParameters.OfType<FFmpegAudioParameter>());

            SelectedAudioParameter = FFmpegAudioParameters.FirstOrDefault(x => x.Guid == ParentVm.Template.Audio);
            ParentVm.AudioSettingsError = HasError();
        }
    }
}