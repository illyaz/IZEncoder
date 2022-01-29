namespace IZEncoder.UI.ViewModel
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Common;
    using Common.Helper;
    using Common.MessageBox;
    using View;

    public class QueueManagementViewModel : IZEScreen<QueueManagementView>
    {
        public QueueManagementViewModel(Global g)
        {
            G = g;
            UseConfig((Config.IWindowPosition) g.Config.QueueManagement);
            UseConfig((Config.IWindowSize) g.Config.QueueManagement);
            UseConfig((Config.IWindowState) g.Config.QueueManagement);
        }

        public Global G { get; }
        public EncodingQueue SelectedQueue { get; set; }
        public LoadingIndicator LoadingIndicator { get; set; }
        public string QueueId { get; set; }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            LoadingIndicator = new LoadingIndicator(View.Content as Grid, View);
            G.EncodingQueueProcessor.QueueCollection.CollectionChanged += QueueCollection_CollectionChanged;

            SelectedQueue = G.EncodingQueueProcessor.QueueCollection.LastOrDefault();
            if (SelectedQueue != null)
                View.QueueDataGrid.ScrollIntoView(SelectedQueue);
        }

        public override void CanClose(Action<bool> callback)
        {
            base.CanClose(callback);
            G.EncodingQueueProcessor.QueueCollection.CollectionChanged -= QueueCollection_CollectionChanged;
            callback(true);
        }

        private void QueueCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                View.QueueDataGrid.ScrollIntoView(SelectedQueue = (EncodingQueue) e.NewItems[0]);
        }

        public void OnSelectedQueueChanged()
        {
            QueueId = SelectedQueue?.Id?.ToString();
        }

        public void QueueIdPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (SelectedQueue == null || e.ClickCount != 2)
                return;

            var dir = new DirectoryInfo(SelectedQueue.GetWorkingPath());
            if (dir.Exists)
            {
                var files = dir.GetFiles();
                if (files.Length > 0)
                    Shell32Helper.OpenFolderAndSelectItem(dir.FullName, files[0].FullName);
                else
                    Shell32Helper.OpenFolder(dir.FullName);
            }
            else
            {
                G.ShowMessage("Queue Management", "Queue directory could not be found", this);
            }
        }

        public void Start()
        {
            if (SelectedQueue == null || !(SelectedQueue.Status == EncodingQueueStatus.Waiting ||
                                           SelectedQueue.Status == EncodingQueueStatus.Retry))
                return;

#pragma warning disable 4014
            G.MainWindowVm.StartQueue(SelectedQueue.CreatedOn);
#pragma warning restore 4014
        }

        public async void Delete()
        {
            var i = G.EncodingQueueProcessor.QueueCollection.IndexOf(SelectedQueue);
            if (i >= 0)
                try
                {
                    await LoadingIndicator.Run(() =>
                    {
                        var isDel = false;
                        var isRun = G.EncodingQueueProcessor.IsRunning &&
                                    SelectedQueue == G.EncodingQueueProcessor.CurrentQueue;

                        var lastSus = G.EncodingQueueProcessor.IsSuspended;

                        if (isRun)
                            if (!G.EncodingQueueProcessor.IsSuspended)
                                G.EncodingQueueProcessor.SuspendProcess();

                        if (G.ShowMessage("Queue Management",
                                "You want to delete queue ?", this,
                                ex => ex.ClearButton().AddButton(IZMessageBoxButton.YesNo).FocusButton("Yes")) == "Yes")
                        {
                            if (isRun)
                            {
                                LoadingIndicator.Set(-1, "Aborting ...");
                                G.EncodingQueueProcessor.ResumeProcess();
                                G.EncodingQueueProcessor.Abort();
                            }

                            isDel = true;
                            G.EncodingQueueProcessor.Delete(SelectedQueue);
                        }

                        if (isRun && !lastSus && G.EncodingQueueProcessor.IsRunning)
                            G.EncodingQueueProcessor.ResumeProcess();

                        if (isDel)
                            SelectedQueue = G.EncodingQueueProcessor.QueueCollection.ElementAtOrDefault(i) ??
                                            G.EncodingQueueProcessor.QueueCollection.LastOrDefault();
                    }, text: "Please wait ...");
                }
                catch (Exception e)
                {
                    G.ShowMessage(e, "Queue delete failed");
                }
        }
    }
}