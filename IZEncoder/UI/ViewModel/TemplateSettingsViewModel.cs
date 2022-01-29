namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;
    using Caliburn.Micro;
    using Common;
    using View;

    public class TemplateSettingsViewModel : IZEScreen<TemplateSettingsView>
    {
        public TemplateSettingsViewModel(Global g)
        {
            G = g;
        }

        public Global G { get; }
        public MainWindowViewModel ParentVm { get; set; }
        public BindableCollection<Screen> Navigator { get; set; } = new BindableCollection<Screen>();
        public Screen ActiveItem { get; set; }
        public List<Action<EncoderTemplate>> SaveActions { get; set; } = new List<Action<EncoderTemplate>>();
        public EncoderTemplate Template { get; set; }

        public bool VideoSettingsError { get; set; }
        public bool AudioSettingsError { get; set; }
        public bool ContainerSettingsError { get; set; }
        public bool AvisynthSettingsError { get; set; }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            Template = ParentVm.SelectedTemplate;

            var tvs = IoC.Get<TemplateVideoSettingsViewModel>();
            tvs.ParentVm = this;
            Navigator.Add(tvs);
            var tas = IoC.Get<TemplateAudioSettingsViewModel>();
            tas.ParentVm = this;
            Navigator.Add(tas);
            var cnr = IoC.Get<TemplateContainerSettingsViewModel>();
            cnr.ParentVm = this;
            Navigator.Add(cnr);

            View.Navigator.SelectedItem = ActiveItem = Navigator.FirstOrDefault();
        }

        public override void CanClose(Action<bool> callback)
        {
            if (VideoSettingsError)
            {
                View.Navigator.SelectedItem = ActiveItem = Navigator[0];
            }
            else if (AudioSettingsError)
            {
                View.Navigator.SelectedItem = ActiveItem = Navigator[1];
            }
            else if (ContainerSettingsError)
            {
                View.Navigator.SelectedItem = ActiveItem = Navigator[2];
            }
            else if (AvisynthSettingsError)
            {
                View.Navigator.SelectedItem = ActiveItem = Navigator[3];
            }
            else if (SaveActions.Any())
            {
                switch (G.ShowMessage("Template settings", "Do you want to save changes template ?", this,
                    ex => ex.ClearButton().AddButton("Cancel").AddButton("Don't save").AddButton("Save")))
                {
                    case "Save":
                        foreach (var saveAction in SaveActions)
                            saveAction(Template);

                        Template.Save(Template.Filepath);

                        callback(true);
                        return;
                    case "Don't save":
                        callback(true);
                        return;
                    default:
                        callback(false);
                        return;
                }
            }
            else
            {
                callback(true);
                return;
            }

            callback(false);
        }

        public void PreventNavigatorListBoxDeselection(object sender, SelectionChangedEventArgs e)
        {
            if (View.Navigator.SelectedItem == null)
            {
                if (e.RemovedItems.Count > 0)
                    View.Navigator.SelectedItem = ActiveItem = e.RemovedItems[0] as Screen;
            }
            else
            {
                ActiveItem = View.Navigator.SelectedItem as Screen;
            }
        }
    }
}