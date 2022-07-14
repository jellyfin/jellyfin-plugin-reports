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

            string productionYear = item.ProductionYear.ToString();
            var series = item as Series;
            if (series == null)
            {
                if (item.ProductionYear == null || item.ProductionYear == 0)
                    return string.Empty;
                return productionYear;
            }

            if (series.Status == SeriesStatus.Continuing)
                return productionYear += "-Present";

            if (series.EndDate != null && series.EndDate.Value.Year != series.ProductionYear)
                return productionYear += "-" + series.EndDate.Value.Year;

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
                        stream.Height != null ? stream.Height.ToString() : "-");

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
