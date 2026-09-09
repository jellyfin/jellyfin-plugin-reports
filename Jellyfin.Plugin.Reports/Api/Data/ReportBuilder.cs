#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.Reports.Api.Common;
using Jellyfin.Plugin.Reports.Api.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Reports.Api.Data
{
    /// <summary> A report builder. </summary>
    /// <seealso cref="ReportBuilderBase"/>
    public class ReportBuilder : ReportBuilderBase
    {
        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportBuilder class. </summary>
        /// <param name="libraryManager"> Manager for library. </param>
        public ReportBuilder(ILibraryManager libraryManager)
            : base(libraryManager)
        {
        }

        /// <summary> Gets report result. </summary>
        /// <param name="items"> The items. </param>
        /// <param name="request"> The request. </param>
        /// <returns> The report result. </returns>
        public ReportResult GetResult(IReadOnlyList<BaseItem> items, IReportsQuery request)
        {
            ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
            ReportDisplayType displayType = ReportHelper.GetReportDisplayType(request.DisplayType);

            List<ReportOptions<BaseItem>> options = this.GetReportOptions<BaseItem>(request,
                () => this.GetDefaultHeaderMetadata(reportRowType),
                (hm) => this.GetOption(hm)).Where(x => this.DisplayTypeVisible(x.Header.DisplayType, displayType)).ToList();

            var headers = GetHeaders<BaseItem>(options);
            var rows = GetReportRows(items, options);

            ReportResult result = new ReportResult { Headers = headers };
            HeaderMetadata groupBy = ReportHelper.GetHeaderMetadataType(request.GroupBy);
            int i = headers.FindIndex(x => x.FieldName == groupBy);
            if (groupBy != HeaderMetadata.None && i >= 0)
            {
                var rowsGroup = rows.SelectMany(x => x.Columns[i].Name.Split(';'), (x, g) => new { Group = g.Trim(), Rows = x })
                    .GroupBy(x => x.Group)
                    .OrderBy(x => x.Key)
                    .Select(x => new ReportGroup(x.Key, x.Select(r => r.Rows).ToList()));

                result.Groups = rowsGroup.ToList();
                result.IsGrouped = true;
            }
            else
            {
                result.Rows = rows;
                result.IsGrouped = false;
            }

            return result;
        }

        /// <summary> Applies custom sorting to report results. </summary>
        /// <param name="result"> The report result. </param>
        /// <param name="sortBy"> The field to sort by. </param>
        /// <param name="sortOrder"> The sort order (Ascending/Descending). </param>
        /// <returns> The sorted report result. </returns>
        public ReportResult ApplySorting(ReportResult result, string sortBy, string sortOrder)
        {
            if (result == null || result.IsGrouped || result.Rows == null || result.Rows.Count == 0)
                return result;

            if (string.IsNullOrWhiteSpace(sortBy))
                return result;

            // Find the header index for the sort field
            // The UI sends SortField value (e.g., "Width", "Size"), so we need to match by:
            // 1. SortField (primary - what UI sends)
            // 2. FieldName (secondary - fallback)
            // 3. Name (tertiary - display name)
            int sortColumnIndex = result.Headers.FindIndex(h =>
                (!string.IsNullOrEmpty(h.SortField) && h.SortField.Split(',')[0].Trim().Equals(sortBy, StringComparison.OrdinalIgnoreCase)) ||
                h.FieldName.ToString().Equals(sortBy, StringComparison.OrdinalIgnoreCase) ||
                h.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

            if (sortColumnIndex < 0)
                return result; // Sort field not found in headers

            bool isDescending = !string.IsNullOrWhiteSpace(sortOrder) &&
                               sortOrder.Equals("Descending", StringComparison.OrdinalIgnoreCase);

            // Sort rows by the specified column
            var sortedRows = isDescending
                ? result.Rows.OrderByDescending(row => GetSortableValue(row, sortColumnIndex)).ToList()
                : result.Rows.OrderBy(row => GetSortableValue(row, sortColumnIndex)).ToList();

            result.Rows = sortedRows;

            return result;
        }

        /// <summary> Gets a sortable value from a report row column. </summary>
        /// <param name="row"> The report row. </param>
        /// <param name="columnIndex"> The column index. </param>
        /// <returns> A sortable string value. </returns>
        private string GetSortableValue(ReportRow row, int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= row.Columns.Count)
                return string.Empty;

            var column = row.Columns[columnIndex];
            if (column == null || string.IsNullOrEmpty(column.Name))
                return string.Empty;

            var value = column.Name.Trim();

            // Extract numeric part if present
            var numericPart = string.Empty;
            foreach (char c in value)
            {
                if (char.IsDigit(c) || c == '.')
                    numericPart += c;
                else if (!string.IsNullOrEmpty(numericPart))
                    break;
            }

            // If we found a numeric part, convert to proper sort value
            if (!string.IsNullOrEmpty(numericPart))
            {
                if (double.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double numValue))
                {
                    // Convert to base units for proper sorting
                    if (value.Contains("TB", StringComparison.OrdinalIgnoreCase))
                        numValue *= 1024.0 * 1024.0 * 1024.0 * 1024.0;
                    else if (value.Contains("GB", StringComparison.OrdinalIgnoreCase))
                        numValue *= 1024.0 * 1024.0 * 1024.0;
                    else if (value.Contains("MB", StringComparison.OrdinalIgnoreCase))
                        numValue *= 1024.0 * 1024.0;
                    else if (value.Contains("KB", StringComparison.OrdinalIgnoreCase))
                        numValue *= 1024.0;
                    else if (value.Contains("Mbps", StringComparison.OrdinalIgnoreCase))
                        numValue *= 1000.0 * 1000.0;
                    else if (value.Contains("kbps", StringComparison.OrdinalIgnoreCase))
                        numValue *= 1000.0;

                    // Return as padded numeric string for proper sorting
                    return numValue.ToString("000000000000000.00", CultureInfo.InvariantCulture);
                }
            }

            // Return original value for non-numeric fields (codecs, aspect ratios)
            return value;
        }

        /// <summary> Gets the headers. </summary>
        /// <typeparam name="T"> Type of the header. </typeparam>
        /// <param name="request"> The request. </param>
        /// <returns> The headers. </returns>
        /// <seealso cref="ReportBuilderBase.GetHeaders"/>
        protected internal override List<ReportHeader> GetHeaders<T>(T request)
        {
            ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
            return this.GetHeaders<BaseItem>(request, () => this.GetDefaultHeaderMetadata(reportRowType), (hm) => this.GetOption(hm));
        }

        /// <summary> Gets default report header metadata. </summary>
        /// <param name="reportIncludeItemTypes"> Type of the report row. </param>
        /// <returns> The default report header metadata. </returns>
        private List<HeaderMetadata> GetDefaultHeaderMetadata(ReportIncludeItemTypes reportIncludeItemTypes)
        {
            switch (reportIncludeItemTypes)
            {
                case ReportIncludeItemTypes.Season:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Series,
                        HeaderMetadata.Season,
                        HeaderMetadata.SeasonNumber,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres
                    };

                case ReportIncludeItemTypes.Series:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.Network,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating,
                        HeaderMetadata.ImdbId,
                        HeaderMetadata.Runtime,
                        HeaderMetadata.Trailers,
                        HeaderMetadata.Specials
                    };

                case ReportIncludeItemTypes.MusicAlbum:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.AlbumArtist,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Tracks,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres
                    };

                case ReportIncludeItemTypes.MusicArtist:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.MusicArtist,
                        HeaderMetadata.Countries,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres
                    };

                case ReportIncludeItemTypes.Movie:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating,
                        HeaderMetadata.ImdbId,
                        HeaderMetadata.Runtime,
                        HeaderMetadata.Container,
                        HeaderMetadata.Video,
                        HeaderMetadata.Audio,
                        HeaderMetadata.ResolutionX,
                        HeaderMetadata.ResolutionY,
                        HeaderMetadata.AspectRatio,
                        HeaderMetadata.VideoBitrate,
                        HeaderMetadata.AudioBitrate,
                        HeaderMetadata.FileSize,
                        HeaderMetadata.Subtitles,
                        HeaderMetadata.Trailers,
                        HeaderMetadata.Specials,
                        HeaderMetadata.Path
                    };

                case ReportIncludeItemTypes.Book:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating
                    };

                case ReportIncludeItemTypes.BoxSet:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating,
                        HeaderMetadata.Trailers
                    };

                case ReportIncludeItemTypes.Audio:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.AudioAlbumArtist,
                        HeaderMetadata.AudioAlbum,
                        HeaderMetadata.Disc,
                        HeaderMetadata.Track,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating,
                        HeaderMetadata.Runtime,
                        HeaderMetadata.Audio,
                        HeaderMetadata.AudioBitrate,
                        HeaderMetadata.FileSize,
                        HeaderMetadata.Container
                    };

                case ReportIncludeItemTypes.Episode:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.EpisodeSeries,
                        HeaderMetadata.Season,
                        HeaderMetadata.EpisodeNumber,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating,
                        HeaderMetadata.ImdbId,
                        HeaderMetadata.Runtime,
                        HeaderMetadata.Container,
                        HeaderMetadata.Video,
                        HeaderMetadata.Audio,
                        HeaderMetadata.ResolutionX,
                        HeaderMetadata.ResolutionY,
                        HeaderMetadata.AspectRatio,
                        HeaderMetadata.VideoBitrate,
                        HeaderMetadata.AudioBitrate,
                        HeaderMetadata.FileSize,
                        HeaderMetadata.Subtitles,
                        HeaderMetadata.Trailers,
                        HeaderMetadata.Specials,
                        HeaderMetadata.Path
                    };

                case ReportIncludeItemTypes.Video:
                case ReportIncludeItemTypes.MusicVideo:
                case ReportIncludeItemTypes.Trailer:
                case ReportIncludeItemTypes.BaseItem:
                default:
                    return new List<HeaderMetadata>
                    {
                        HeaderMetadata.Status,
                        HeaderMetadata.Locked,
                        HeaderMetadata.ImagePrimary,
                        HeaderMetadata.ImageBackdrop,
                        HeaderMetadata.ImageLogo,
                        HeaderMetadata.Name,
                        HeaderMetadata.DateAdded,
                        HeaderMetadata.ReleaseDate,
                        HeaderMetadata.Year,
                        HeaderMetadata.Genres,
                        HeaderMetadata.ParentalRating,
                        HeaderMetadata.CommunityRating,
                        HeaderMetadata.Runtime,
                        HeaderMetadata.Container,
                        HeaderMetadata.Video,
                        HeaderMetadata.Audio,
                        HeaderMetadata.ResolutionX,
                        HeaderMetadata.ResolutionY,
                        HeaderMetadata.AspectRatio,
                        HeaderMetadata.VideoBitrate,
                        HeaderMetadata.AudioBitrate,
                        HeaderMetadata.FileSize,
                        HeaderMetadata.Subtitles,
                        HeaderMetadata.Trailers,
                        HeaderMetadata.Specials
                    };

            }

        }

        /// <summary> Gets report option. </summary>
        /// <param name="header"> The header. </param>
        /// <param name="sortField"> The sort field. </param>
        /// <returns> The report option. </returns>
        private ReportOptions<BaseItem> GetOption(HeaderMetadata header, string sortField = "")
        {
            HeaderMetadata internalHeader = header;

            ReportOptions<BaseItem> option = new ReportOptions<BaseItem>()
            {
                Header = new ReportHeader
                {
                    HeaderFieldType = ReportFieldType.String,
                    SortField = sortField,
                    Type = "",
                    ItemViewType = ItemViewType.None
                }
            };

            switch (header)
            {
                case HeaderMetadata.Status:
                    option.Header.ItemViewType = ItemViewType.StatusImage;
                    internalHeader = HeaderMetadata.Status;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Screen;
                    break;
                case HeaderMetadata.Locked:
                    option.Column = (i, r) => this.GetBoolString(r.HasLockData);
                    option.Header.ItemViewType = ItemViewType.LockDataImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;
                case HeaderMetadata.ImagePrimary:
                    option.Column = (i, r) => this.GetBoolString(r.HasImageTagsPrimary);
                    option.Header.ItemViewType = ItemViewType.TagsPrimaryImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;
                case HeaderMetadata.ImageBackdrop:
                    option.Column = (i, r) => this.GetBoolString(r.HasImageTagsBackdrop);
                    option.Header.ItemViewType = ItemViewType.TagsBackdropImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;
                case HeaderMetadata.ImageLogo:
                    option.Column = (i, r) => this.GetBoolString(r.HasImageTagsLogo);
                    option.Header.ItemViewType = ItemViewType.TagsLogoImage;
                    option.Header.CanGroup = false;
                    option.Header.DisplayType = ReportDisplayType.Export;
                    break;

                case HeaderMetadata.Path:
                    option.Column = (i, r) => i.Path;
                    option.Header.SortField = "Path,SortName";
                    break;

                case HeaderMetadata.Name:
                    option.Column = (i, r) => i.Name;
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "SortName";
                    break;

                case HeaderMetadata.DateAdded:
                    option.Column = (i, r) => i.DateCreated;
                    option.Header.SortField = "DateCreated,SortName";
                    option.Header.HeaderFieldType = ReportFieldType.DateTime;
                    option.Header.Type = "";
                    break;

                case HeaderMetadata.PremiereDate:
                case HeaderMetadata.ReleaseDate:
                    option.Column = (i, r) => i.PremiereDate;
                    option.Header.HeaderFieldType = ReportFieldType.DateTime;
                    option.Header.SortField = "ProductionYear,PremiereDate,SortName";
                    break;

                case HeaderMetadata.Runtime:
                    option.Column = (i, r) => this.GetRuntimeDateTime(i.RunTimeTicks);
                    option.Header.HeaderFieldType = ReportFieldType.Minutes;
                    option.Header.SortField = "Runtime,SortName";
                    option.Header.CanGroup = false;
                    break;

                case HeaderMetadata.PlayCount:
                    option.Header.HeaderFieldType = ReportFieldType.Int;
                    break;

                case HeaderMetadata.Season:
                    option.Column = (i, r) => this.GetEpisode(i);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "SortName";
                    break;

                case HeaderMetadata.SeasonNumber:
                    option.Column = (i, r) => this.GetObject<Season, string>(i, (x) => x.IndexNumber == null ? "" : x.IndexNumber?.ToString(CultureInfo.InvariantCulture));
                    option.Header.SortField = "IndexNumber";
                    option.Header.HeaderFieldType = ReportFieldType.Int;
                    break;

                case HeaderMetadata.Series:
                    option.Column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "SeriesSortName,SortName";
                    break;

                case HeaderMetadata.EpisodeSeries:
                    option.Column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.ItemID = (i) =>
                    {
                        Series series = this.GetObject<Episode, Series>(i, (x) => x.Series);
                        if (series == null)
                            return string.Empty;
                        return series.Id;
                    };
                    option.Header.SortField = "SeriesSortName,SortName";
                    internalHeader = HeaderMetadata.Series;
                    break;

                case HeaderMetadata.EpisodeSeason:
                    option.Column = (i, r) => this.GetObject<IHasSeries, string>(i, (x) => x.SeriesName);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.ItemID = (i) =>
                    {
                        Season season = this.GetObject<Episode, Season>(i, (x) => x.Season);
                        if (season == null)
                            return string.Empty;
                        return season.Id;
                    };
                    option.Header.SortField = "SortName";
                    internalHeader = HeaderMetadata.Season;
                    break;

                case HeaderMetadata.EpisodeNumber:
                    option.Column = (i, r) => this.GetObject<BaseItem, string>(i, (x) => x.IndexNumber == null ? "" : x.IndexNumber?.ToString(CultureInfo.InvariantCulture));
                    //option.Header.SortField = "IndexNumber";
                    //option.Header.HeaderFieldType = ReportFieldType.Int;
                    break;

                case HeaderMetadata.Network:
                    option.Column = (i, r) => this.GetListAsString(i.Studios.ToList());
                    option.ItemID = (i) => this.GetStudioID(i.Studios.FirstOrDefault());
                    option.Header.ItemViewType = ItemViewType.ItemByNameDetails;
                    option.Header.SortField = "Studio,SortName";
                    break;

                case HeaderMetadata.Year:
                    option.Column = (i, r) => this.GetSeriesProductionYear(i);
                    option.Header.SortField = "ProductionYear,PremiereDate,SortName";
                    break;

                case HeaderMetadata.ParentalRating:
                    option.Column = (i, r) => i.OfficialRating;
                    option.Header.SortField = "OfficialRating,SortName";
                    break;

                case HeaderMetadata.CommunityRating:
                    option.Column = (i, r) => i.CommunityRating;
                    option.Header.SortField = "CommunityRating,SortName";
                    break;

                case HeaderMetadata.Trailers:
                    option.Column = (i, r) => this.GetBoolString(r.HasLocalTrailer);
                    option.Header.ItemViewType = ItemViewType.TrailersImage;
                    break;

                case HeaderMetadata.Specials:
                    option.Column = (i, r) => this.GetBoolString(r.HasSpecials);
                    option.Header.ItemViewType = ItemViewType.SpecialsImage;
                    break;

                case HeaderMetadata.AlbumArtist:
                    option.Column = (i, r) => this.GetObject<MusicAlbum, string>(i, (x) => x.AlbumArtist);
                    option.ItemID = (i) => this.GetPersonID(this.GetObject<MusicAlbum, string>(i, (x) => x.AlbumArtist));
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "AlbumArtist,Album,SortName";

                    break;
                case HeaderMetadata.MusicArtist:
                    option.Column = (i, r) => this.GetObject<MusicArtist, string>(i, (x) => x.GetLookupInfo().Name);
                    option.Header.ItemViewType = ItemViewType.Detail;
                    option.Header.SortField = "AlbumArtist,Album,SortName";
                    internalHeader = HeaderMetadata.AlbumArtist;
                    break;
                case HeaderMetadata.AudioAlbumArtist:
                    option.Column = (i, r) => this.GetListAsString(this.GetObject<Audio, List<string>>(i, (x) => x.AlbumArtists.ToList()));
                    option.Header.SortField = "AlbumArtist,Album,SortName";
                    internalHeader = HeaderMetadata.AlbumArtist;
                    break;

                case HeaderMetadata.AudioAlbum:
                    option.Column = (i, r) => this.GetObject<Audio, string>(i, (x) => x.Album);
                    option.Header.SortField = "Album,SortName";
                    internalHeader = HeaderMetadata.Album;
                    break;

                case HeaderMetadata.Disc:
                    option.Column = (i, r) => i.ParentIndexNumber;
                    break;

                case HeaderMetadata.Track:
                    option.Column = (i, r) => i.IndexNumber;
                    break;

                case HeaderMetadata.Tracks:
                    option.Column = (i, r) => this.GetObject<MusicAlbum, List<Audio>>(i, (x) => x.Tracks.ToList(), new List<Audio>()).Count;
                    break;

                case HeaderMetadata.Audio:
                    option.Column = (i, r) => this.GetAudioStream(i);
                    break;

                case HeaderMetadata.EmbeddedImage:
                    break;

                case HeaderMetadata.Video:
                    option.Column = (i, r) => this.GetVideoStream(i);
                    break;

                case HeaderMetadata.Resolution:
                    option.Column = (i, r) => this.GetVideoResolution(i);
                    break;

                case HeaderMetadata.Subtitles:
                    option.Column = (i, r) => this.GetBoolString(r.HasSubtitles);
                    option.Header.ItemViewType = ItemViewType.SubtitleImage;
                    break;

                case HeaderMetadata.ResolutionX:
                    option.Column = (i, r) => this.GetVideoResolutionX(i);
                    option.Header.HeaderFieldType = ReportFieldType.Int;
                    option.Header.SortField = "Width";
                    option.Header.CanGroup = false;
                    internalHeader = HeaderMetadata.ResolutionX;
                    break;

                case HeaderMetadata.ResolutionY:
                    option.Column = (i, r) => this.GetVideoResolutionY(i);
                    option.Header.HeaderFieldType = ReportFieldType.Int;
                    option.Header.SortField = "Height";
                    option.Header.CanGroup = false;
                    internalHeader = HeaderMetadata.ResolutionY;
                    break;

                case HeaderMetadata.FileSize:
                    option.Column = (i, r) => this.GetFormattedFileSize(i);
                    option.Header.HeaderFieldType = ReportFieldType.String;
                    option.Header.SortField = "Size";
                    option.Header.CanGroup = false;
                    break;

                case HeaderMetadata.AspectRatio:
                    option.Column = (i, r) => this.GetAspectRatio(i);
                    option.Header.HeaderFieldType = ReportFieldType.String;
                    option.Header.CanGroup = true;
                    break;

                case HeaderMetadata.AudioBitrate:
                    option.Column = (i, r) => this.GetAudioBitrate(i);
                    option.Header.HeaderFieldType = ReportFieldType.String;
                    option.Header.CanGroup = false;
                    break;

                case HeaderMetadata.VideoBitrate:
                    option.Column = (i, r) => this.GetVideoBitrate(i);
                    option.Header.HeaderFieldType = ReportFieldType.String;
                    option.Header.CanGroup = false;
                    break;

                case HeaderMetadata.Container:
                    option.Column = (i, r) => this.GetContainer(i);
                    option.Header.HeaderFieldType = ReportFieldType.String;
                    option.Header.SortField = "Container";
                    option.Header.CanGroup = true;
                    break;

                case HeaderMetadata.Genres:
                    option.Column = (i, r) => this.GetListAsString(i.Genres.ToList());
                    break;

                case HeaderMetadata.ImdbId:
                    option.Column = (i, r) => i.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Imdb) ?? string.Empty;
                    option.Header.SortField = "";  // Non-sortable
                    option.Header.CanGroup = false;
                    break;

            }

            option.Header.Name = GetLocalizedHeader(internalHeader);
            option.Header.FieldName = header;

            return option;
        }

        /// <summary> Gets report rows. </summary>
        /// <param name="items"> The items. </param>
        /// <param name="options"> Options for controlling the operation. </param>
        /// <returns> The report rows. </returns>
        private List<ReportRow> GetReportRows(IEnumerable<BaseItem> items, List<ReportOptions<BaseItem>> options)
        {
            var rows = new List<ReportRow>();

            foreach (BaseItem item in items)
            {
                ReportRow rRow = GetRow(item);
                foreach (ReportOptions<BaseItem> option in options)
                {
                    object itemColumn = option.Column != null ? option.Column(item, rRow) : "";
                    object itemId = option.ItemID != null ? option.ItemID(item) : "";
                    ReportItem rItem = new ReportItem
                    {
                        Name = ReportHelper.ConvertToString(itemColumn, option.Header.HeaderFieldType),
                        Id = ReportHelper.ConvertToString(itemId, ReportFieldType.Object)
                    };
                    rRow.Columns.Add(rItem);
                }

                rows.Add(rRow);
            }

            return rows;
        }

        /// <summary> Gets a row. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The row. </returns>
        private ReportRow GetRow(BaseItem item)
        {
            return new ReportRow
            {
                Id = item.Id.ToString("N"),
                HasLockData = item.IsLocked,
                HasLocalTrailer = item.GetExtras(new[] { ExtraType.Trailer }).Any(),
                HasImageTagsPrimary = item.ImageInfos != null && item.ImageInfos.Any(n => n.Type == ImageType.Primary),
                HasImageTagsBackdrop = item.ImageInfos != null && item.ImageInfos.Any(n => n.Type == ImageType.Backdrop),
                HasImageTagsLogo = item.ImageInfos != null && item.ImageInfos.Any(n => n.Type == ImageType.Logo),
                HasSpecials = item.GetExtras(BaseItem.DisplayExtraTypes).Any(),
                HasSubtitles = item is Video video && video.HasSubtitles,
                RowType = ReportHelper.GetRowType(item.GetClientTypeName())
            };
        }
    }
}
