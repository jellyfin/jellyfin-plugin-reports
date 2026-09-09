#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Jellyfin.Plugin.Reports.Api.Data;
using Jellyfin.Plugin.Reports.Api.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Reports.Api.Common
{
    /// <summary> A report builder base. </summary>
    public abstract class ReportBuilderBase
    {
        /// <summary> Manager for library. </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportBuilderBase class. </summary>
        /// <param name="libraryManager"> Manager for library. </param>
        public ReportBuilderBase(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        protected Func<bool, string> GetBoolString => s => s == true ? "x" : string.Empty;

        /// <summary> Gets the headers. </summary>
        /// <typeparam name="T"> Type of the header. </typeparam>
        /// <param name="request"> The request. </param>
        /// <returns> The headers. </returns>
        protected internal abstract List<ReportHeader> GetHeaders<T>(T request) where T : IReportsHeader;

        /// <summary> Gets active headers. </summary>
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="options"> Options for controlling the operation. </param>
        /// <returns> The active headers. </returns>
        protected List<ReportHeader> GetActiveHeaders<T>(List<ReportOptions<T>> options, ReportDisplayType displayType)
            => options.Where(x => this.DisplayTypeVisible(x.Header.DisplayType, displayType)).Select(x => x.Header).ToList();

        /// <summary> Gets audio stream. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The audio stream. </returns>
        protected string GetAudioStream(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Audio);
            if (stream == null)
            {
                return string.Empty;
            }

            return string.Equals(stream.Codec, "DCA", StringComparison.OrdinalIgnoreCase)
                ? stream.Profile
                : stream.Codec.ToUpperInvariant();
        }

        /// <summary> Gets an episode. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The episode. </returns>
        protected string GetEpisode(BaseItem item)
        {
            if (string.Equals(item.GetClientTypeName(), ChannelMediaContentType.Episode.ToString(), StringComparison.Ordinal)
                && item.ParentIndexNumber != null)
            {
                return "Season " + item.ParentIndexNumber;
            }

            return item.Name;
        }

        /// <summary> Gets a genre. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The genre. </returns>
        protected Genre GetGenre(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return _libraryManager.GetGenre(name);
        }

        /// <summary> Gets genre identifier. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The genre identifier. </returns>
        protected string GetGenreID(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return GetGenre(name).Id.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary> Gets the headers. </summary>
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="options"> Options for controlling the operation. </param>
        /// <returns> The headers. </returns>
        protected List<ReportHeader> GetHeaders<T>(List<ReportOptions<T>> options)
            => options.ConvertAll(x => x.Header);

        /// <summary> Gets the headers. </summary>
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="request"> The request. </param>
        /// <param name="getHeadersMetadata"> The get headers metadata. </param>
        /// <param name="getOptions"> Options for controlling the get. </param>
        /// <returns> The headers. </returns>
        protected List<ReportHeader> GetHeaders<T>(IReportsHeader request, Func<List<HeaderMetadata>> getHeadersMetadata, Func<HeaderMetadata, ReportOptions<T>> getOptions)
        {
            List<ReportOptions<T>> options = this.GetReportOptions(request, getHeadersMetadata, getOptions);
            return this.GetHeaders(options);
        }

        /// <summary> Gets list as string. </summary>
        /// <param name="items"> The items. </param>
        /// <returns> The list as string. </returns>
        protected string GetListAsString(List<string> items)
        {
            return string.Join("; ", items);
        }

        /// <summary> Gets localized header. </summary>
        /// <param name="internalHeader"> The internal header. </param>
        /// <returns> The localized header. </returns>
        protected static string GetLocalizedHeader(HeaderMetadata internalHeader)
        {
            if (internalHeader == HeaderMetadata.EpisodeNumber)
            {
                return "Episode";
            }

            string headerName = string.Empty;
            if (internalHeader != HeaderMetadata.None)
            {
                string localHeader = internalHeader.ToString();
                headerName = ReportHelper.GetCoreLocalizedString(localHeader);
            }
            return headerName;
        }

        /// <summary> Gets media source information. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The media source information. </returns>
        protected MediaSourceInfo GetMediaSourceInfo(BaseItem item)
        {
            if (item is IHasMediaSources mediaSource)
                return mediaSource.GetMediaSources(false).FirstOrDefault(n => n.Type == MediaSourceType.Default);

            return null;
        }

        /// <summary> Gets an object. </summary>
        /// <typeparam name="TItem"> Generic type parameter. </typeparam>
        /// <typeparam name="TReturn"> Type of the r. </typeparam>
        /// <param name="item"> The item. </param>
        /// <param name="function"> The function. </param>
        /// <param name="defaultValue"> The default value. </param>
        /// <returns> The object. </returns>
        protected TReturn GetObject<TItem, TReturn>(BaseItem item, Func<TItem, TReturn> function, TReturn defaultValue = default)
            where TItem : class
        {
            if (item is TItem value && function != null)
                return function(value);
            else
                return defaultValue;
        }

        /// <summary> Gets a person. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The person. </returns>
        protected Person GetPerson(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return _libraryManager.GetPerson(name);
        }

        /// <summary> Gets person identifier. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The person identifier. </returns>
        protected string GetPersonID(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return GetPerson(name).Id.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary> Gets report options. </summary>
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="request"> The request. </param>
        /// <param name="getHeadersMetadata"> The get headers metadata. </param>
        /// <param name="getOptions"> Options for controlling the get. </param>
        /// <returns> The report options. </returns>
        protected List<ReportOptions<T>> GetReportOptions<T>(IReportsHeader request, Func<List<HeaderMetadata>> getHeadersMetadata, Func<HeaderMetadata, ReportOptions<T>> getOptions)
        {
            List<HeaderMetadata> headersMetadata = getHeadersMetadata();
            List<ReportOptions<T>> options = new List<ReportOptions<T>>();
            ReportDisplayType displayType = ReportHelper.GetReportDisplayType(request.DisplayType);
            foreach (HeaderMetadata header in headersMetadata)
            {
                ReportOptions<T> headerOptions = getOptions(header);
                if (this.DisplayTypeVisible(headerOptions.Header.DisplayType, displayType))
                    options.Add(headerOptions);
            }

            if (request != null && !string.IsNullOrEmpty(request.ReportColumns))
            {
                List<HeaderMetadata> headersMetadataFiltered = ReportHelper.GetFilteredReportHeaderMetadata(request.ReportColumns, () => headersMetadata);
                foreach (ReportHeader header in options.Select(x => x.Header))
                {

                    if ((!DisplayTypeVisible(header.DisplayType, displayType)) || (!headersMetadataFiltered.Contains(header.FieldName) && header.DisplayType != ReportDisplayType.Export)
                        || (!headersMetadataFiltered.Contains(HeaderMetadata.Status) && header.DisplayType == ReportDisplayType.Export))
                    {
                        header.DisplayType = ReportDisplayType.None;
                    }
                }
            }

            return options;
        }

        /// <summary> Gets runtime date time. </summary>
        /// <param name="runtime"> The runtime. </param>
        /// <returns> The runtime date time. </returns>
        protected double? GetRuntimeDateTime(long? runtime)
        {
            if (runtime.HasValue)
                return Math.Ceiling(new TimeSpan(runtime.Value).TotalMinutes);
            return null;
        }

        /// <summary> Gets series production year. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The series production year. </returns>
        protected string GetSeriesProductionYear(BaseItem item)
        {

            string productionYear = item.ProductionYear?.ToString(CultureInfo.InvariantCulture);
            if (item is not Series series)
            {
                if (item.ProductionYear == null || item.ProductionYear == 0)
                    return string.Empty;
                return productionYear;
            }

            if (series.Status == SeriesStatus.Continuing)
                return productionYear + "-Present";

            if (series.EndDate != null && series.EndDate.Value.Year != series.ProductionYear)
                return productionYear + "-" + series.EndDate.Value.Year;

            return productionYear;
        }

        /// <summary> Gets a stream. </summary>
        /// <param name="item"> The item. </param>
        /// <param name="streamType"> Type of the stream. </param>
        /// <returns> The stream. </returns>
        protected MediaStream GetStream(BaseItem item, MediaStreamType streamType)
        {
            var itemInfo = GetMediaSourceInfo(item);
            if (itemInfo != null)
                return itemInfo.MediaStreams.FirstOrDefault(n => n.Type == streamType);

            return null;
        }

        /// <summary> Gets a studio. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The studio. </returns>
        protected Studio GetStudio(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return _libraryManager.GetStudio(name);
        }

        /// <summary> Gets studio identifier. </summary>
        /// <param name="name"> The name. </param>
        /// <returns> The studio identifier. </returns>
        protected string GetStudioID(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return GetStudio(name).Id.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary> Gets video resolution. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The video resolution. </returns>
        protected string GetVideoResolution(BaseItem item)
        {
            var stream = GetStream(item,
                    MediaStreamType.Video);
            if (stream != null && stream.Width != null)
                return string.Format(CultureInfo.InvariantCulture, "{0} * {1}",
                    stream.Width,
                    stream.Height?.ToString(CultureInfo.InvariantCulture) ?? "-");

            return string.Empty;
        }

        /// <summary> Gets video stream. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The video stream. </returns>
        protected string GetVideoStream(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Video);
            if (stream != null)
                return stream.Codec.ToUpperInvariant();

            return string.Empty;
        }

        /// <summary> Gets video resolution X (width). </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The video width in pixels. </returns>
        protected int? GetVideoResolutionX(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Video);
            return stream?.Width;
        }

        /// <summary> Gets video resolution Y (height). </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The video height in pixels. </returns>
        protected int? GetVideoResolutionY(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Video);
            return stream?.Height;
        }

        /// <summary> Gets formatted file size. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The formatted file size (e.g., "4.2 GB", "350 MB"). </returns>
        protected string GetFormattedFileSize(BaseItem item)
        {
            if (item.Size == null || item.Size == 0)
                return string.Empty;

            long bytes = item.Size.Value;
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", size, sizes[order]);
        }

        /// <summary> Gets aspect ratio. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The aspect ratio (e.g., "16:9", "4:3", "21:9"). </returns>
        protected string GetAspectRatio(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Video);
            if (stream?.Width == null || stream?.Height == null || stream.Height == 0)
                return string.Empty;

            int width = stream.Width.Value;
            int height = stream.Height.Value;
            double actualRatio = (double)width / height;

            // Check against common aspect ratios with 3% tolerance
            if (IsCloseToRatio(actualRatio, 16.0 / 9.0)) return "16:9";
            if (IsCloseToRatio(actualRatio, 4.0 / 3.0)) return "4:3";
            if (IsCloseToRatio(actualRatio, 21.0 / 9.0)) return "21:9";
            if (IsCloseToRatio(actualRatio, 2.39)) return "2.39:1";  // Anamorphic widescreen
            if (IsCloseToRatio(actualRatio, 2.35)) return "2.35:1";  // CinemaScope
            if (IsCloseToRatio(actualRatio, 1.85)) return "1.85:1";  // VistaVision
            if (IsCloseToRatio(actualRatio, 16.0 / 10.0)) return "16:10";
            if (IsCloseToRatio(actualRatio, 5.0 / 4.0)) return "5:4";
            if (IsCloseToRatio(actualRatio, 1.0)) return "1:1";

            // Return calculated ratio in reduced form for non-standard ratios
            int gcd = CalculateGCD(width, height);
            int ratioWidth = width / gcd;
            int ratioHeight = height / gcd;

            // If reduced ratio is too complex, show decimal instead
            if (ratioWidth > 100 || ratioHeight > 100)
                return string.Format(CultureInfo.InvariantCulture, "{0:0.##}:1", actualRatio);

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", ratioWidth, ratioHeight);
        }

        /// <summary> Calculates greatest common divisor. </summary>
        private int CalculateGCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary> Checks if actual ratio is close to a standard ratio. </summary>
        private bool IsCloseToRatio(double actualRatio, double standardRatio)
        {
            // 3% tolerance for aspect ratio variations
            double tolerance = standardRatio * 0.03;
            return Math.Abs(actualRatio - standardRatio) <= tolerance;
        }

        /// <summary> Gets audio bitrate. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The audio bitrate formatted (e.g., "320 kbps", "1.5 Mbps"). </returns>
        protected string GetAudioBitrate(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Audio);
            if (stream?.BitRate == null || stream.BitRate == 0)
                return string.Empty;

            int bitrate = stream.BitRate.Value;

            if (bitrate >= 1000000)
                return string.Format(CultureInfo.InvariantCulture, "{0:0.#} Mbps", bitrate / 1000000.0);
            else
                return string.Format(CultureInfo.InvariantCulture, "{0} kbps", bitrate / 1000);
        }

        /// <summary> Gets video bitrate. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The video bitrate formatted (e.g., "8.5 Mbps", "2500 kbps"). </returns>
        protected string GetVideoBitrate(BaseItem item)
        {
            var stream = GetStream(item, MediaStreamType.Video);
            if (stream?.BitRate == null || stream.BitRate == 0)
                return string.Empty;

            int bitrate = stream.BitRate.Value;

            if (bitrate >= 1000000)
                return string.Format(CultureInfo.InvariantCulture, "{0:0.#} Mbps", bitrate / 1000000.0);
            else
                return string.Format(CultureInfo.InvariantCulture, "{0} kbps", bitrate / 1000);
        }

        /// <summary> Gets container format. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The container format (e.g., "MKV", "MP4", "AVI"). </returns>
        protected string GetContainer(BaseItem item)
        {
            var mediaSource = GetMediaSourceInfo(item);
            if (mediaSource == null || string.IsNullOrEmpty(mediaSource.Container))
                return string.Empty;

            return mediaSource.Container.ToUpperInvariant();
        }

        /// <summary> Displays a type visible. </summary>
        /// <param name="headerDisplayType"> Type of the header display. </param>
        /// <param name="displayType"> Type of the display. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        protected bool DisplayTypeVisible(ReportDisplayType headerDisplayType, ReportDisplayType displayType)
        {
            if (headerDisplayType == ReportDisplayType.None)
                return false;

            bool rval = headerDisplayType == displayType || headerDisplayType == ReportDisplayType.ScreenExport && (displayType == ReportDisplayType.Screen || displayType == ReportDisplayType.Export);
            return rval;
        }
    }
}
