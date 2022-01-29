namespace IZEncoder.Common.Project
{
    using Caliburn.Micro;

    public class MediaInfomation : PropertyChangedBaseJson
    {
        public BindableCollection<VideoTrackInfomation> VideoTracks { get; set; } =
            new BindableCollection<VideoTrackInfomation>();

        public BindableCollection<AudioTrackInfomation> AudioTracks { get; set; } =
            new BindableCollection<AudioTrackInfomation>();

        public string Filename { get; set; }
        public long Filesize { get; set; }
        public string FileExtension { get; set; }
    }
}