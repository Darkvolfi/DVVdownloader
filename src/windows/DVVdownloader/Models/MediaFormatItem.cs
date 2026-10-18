using System;

namespace DVVdownloader.Models
{
    public class MediaFormatItem
    {
        public string FormatId { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public int Height { get; set; }
        public int Width { get; set; }
        public string VCodec { get; set; } = "none";
        public string ACodec { get; set; } = "none";
        public string Ext { get; set; } = "mp4";
        public long? Filesize { get; set; }
        public long? FilesizeApprox { get; set; }
        public string Note { get; set; } = string.Empty;
        public double? Fps { get; set; }
        public double? Tbr { get; set; } // Total bitrate in kbit/s
        public string DirectUrl { get; set; } = string.Empty;

        public bool IsAudioOnly
        {
            get
            {
                if (FormatId.Contains("audio", StringComparison.OrdinalIgnoreCase) || 
                    DirectUrl.Contains("_audio", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (VCodec == "none" || string.IsNullOrEmpty(VCodec))
                {
                    return ACodec != "none" || (Height <= 0 && Width <= 0);
                }
                return false;
            }
        }

        public bool IsVideoOnly => !IsAudioOnly && 
                                   (VCodec != "none" && !string.IsNullOrEmpty(VCodec)) && 
                                   (ACodec == "none" || string.IsNullOrEmpty(ACodec) || DirectUrl.Contains("_video", StringComparison.OrdinalIgnoreCase));

        public bool IsCombined => !IsAudioOnly && !IsVideoOnly &&
                                  ((VCodec != "none" && ACodec != "none" && !string.IsNullOrEmpty(ACodec)) ||
                                   (Ext.Equals("mp4", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(DirectUrl)));

        public string QualityLabel
        {
            get
            {
                if (!string.IsNullOrEmpty(Note))
                {
                    return Note;
                }

                if (IsAudioOnly)
                {
                    if (Tbr.HasValue) return $"Ses ({Math.Round(Tbr.Value)} kbps) - {Ext.ToUpper()}";
                    return $"Ses ({Ext.ToUpper()})";
                }

                if (Height > 0)
                {
                    string res = $"{Height}p";
                    if (Height >= 2160) res = "4K (2160p)";
                    else if (Height >= 1440) res = "2K (1440p)";
                    else if (Height >= 1080) res = "Full HD (1080p)";
                    else if (Height >= 720) res = "HD (720p)";
                    
                    if (Fps.HasValue && Fps.Value > 30) res += $" {Math.Round(Fps.Value)}fps";
                    return res;
                }

                if (Width > 0)
                {
                    return $"{Width}w ({Ext.ToUpper()})";
                }

                return !string.IsNullOrEmpty(Resolution) ? Resolution : "Orijinal Kalite";
            }
        }

        public string FormattedSize
        {
            get
            {
                long? size = Filesize ?? FilesizeApprox;
                if (!size.HasValue || size.Value <= 0) return "Tahmini boyut bilinmiyor";
                double mb = size.Value / (1024.0 * 1024.0);
                if (mb >= 1024)
                {
                    return $"{mb / 1024.0:F2} GB";
                }
                return $"{mb:F1} MB";
            }
        }
    }
}
