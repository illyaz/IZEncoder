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

    public sealed class TemplateContainerSettingsViewModel : IZEScreen<TemplateContainerSettingsView>
    {
        private bool _loaded;

        public TemplateContainerSettingsViewModel(Global g)
        {
            G = g;
            DisplayName = "Container";
        }

        public Global G { get; }

        public BindableCollection<FFmpegContainerParameter> FFmpegContainerParameters { get; set; } =
            new BindableCollection<FFmpegContainerParameter>();

        public FFmpegContainerParameter SelectedContainerParameter { get; set; }
        public bool IsEmpty { get; set; }
        public TemplateSettingsViewModel ParentVm { get; set; }

        public BindableCollection<IFFmpegParamUIViewModelBase> FFmpegParamUICollection { get; set; } =
            new BindableCollection<IFFmpegParamUIViewModelBase>();

        public void OnSelectedContainerParameterChanged()
        {
            var vguid = SelectedContainerParameter.Guid;
            if (vguid != ParentVm.Template.Container)
                ParentVm.SaveActions.Add(tpl =>
                {
                    tpl.Container = vguid;
                    tpl.ContainerSettings.Clear();
                });

            foreach (var paramUi in FFmpegParamUICollection)
                paramUi.ValueChanged -= ParamUi_ValueChanged;

            FFmpegParamUICollection.Clear();
            FFmpegParamUICollection.AddRange(
                FFmpegParamUIHelper.GetViewModels(SelectedContainerParameter.Params.ToArray()));

            IsEmpty = FFmpegParamUICollection.Count == 0;

            foreach (var paramUi in FFmpegParamUICollection)
            {
                if (ParentVm.Template.ContainerSettings.ContainsKey(paramUi.Param.Name))
                    paramUi.SetValue(0,
                        paramUi.Param.GetValue(ParentVm.Template.ContainerSettings[paramUi.Param.Name]?.ToString()));
                else if (paramUi.Param.Default != null)
                    paramUi.SetValue(0, paramUi.Param.Default);

                paramUi.ValueChanged += ParamUi_ValueChanged;
            }

            ParentVm.ContainerSettingsError = HasError();
        }

        private void ParamUi_ValueChanged(object sender, int e)
        {
            var ui = (IFFmpegParamUIViewModelBase) sender;

            ParentVm.SaveActions.Add(tpl =>
            {
                if (tpl.ContainerSettings.ContainsKey(ui.Param.Name))
                    tpl.ContainerSettings[ui.Param.Name] = ui.GetValue(e);
                else
                    tpl.ContainerSettings.Add(ui.Param.Name, ui.GetValue(e));
            });

            ParentVm.ContainerSettingsError = HasError();
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
            FFmpegContainerParameters.AddRange(G.FFmpegParameters.OfType<FFmpegContainerParameter>());

            SelectedContainerParameter =
                FFmpegContainerParameters.FirstOrDefault(x => x.Guid == ParentVm.Template.Container);
            ParentVm.ContainerSettingsError = HasError();
        }
    }
}