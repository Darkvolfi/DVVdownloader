using System;
using System.Collections.Generic;

namespace DVVdownloader.Models
{
    public class MediaMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Uploader { get; set; } = string.Empty;
        public double Duration { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string WebpageUrl { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public List<MediaFormatItem> Formats { get; set; } = new();

        public string FormattedDuration
        {
            get
            {
                if (Duration <= 0) return "--:--";
                var ts = TimeSpan.FromSeconds(Duration);
                if (ts.TotalHours >= 1)
                {
                    return ts.ToString(@"hh\:mm\:ss");
                }
                return ts.ToString(@"mm\:ss");
            }
        }
    }
}
