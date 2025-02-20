namespace Jellyfin.Plugin.Reports.Api.Common
{
    public enum HeaderActivitiesMetadata
    {
        None,
        Name,
        Overview,
        ShortOverview,
        Type,
        Date,
        UserPrimaryImageTag,
        Severity,
        Item,
        User
    }

    public static class ReportHelper
    {
        public static void GetOption(HeaderMetadata header, ReportOption option)
        {
            switch (header)
            {
                case HeaderMetadata.FileSize:
                    option.Column = (i, r) =>
                    {
                        var sources = i.GetMediaSources(false);
                        long totalSize = sources != null && sources.Any() ? sources.Sum(src => src.Size) : 0;
                        return FormatFileSize(totalSize);
                    };
                    option.Header.HeaderFieldType = ReportFieldType.String;
                    option.Header.SortField = "Size,SortName";
                    break;
            }
        }

        private static string FormatFileSize(long size)
        {
            return size.ToString();
        }
    }
}
