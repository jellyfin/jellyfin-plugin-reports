#nullable enable

using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Plugin.Reports.Api.Common;
using Jellyfin.Plugin.Reports.Api.Model;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Reports.Api
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "DefaultAuthorization")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ReportsController : ControllerBase
    {
        private readonly ReportsService _reportsService;

        public ReportsController(
            IUserManager userManager,
            ILibraryManager libraryManager,
            ILocalizationManager localizationManager,
            IActivityManager activityManager)
        {
            _reportsService = new ReportsService(userManager, libraryManager, localizationManager, activityManager);
        }

        /// <summary>
        /// Gets reports based on library items.
        /// </summary>
        [HttpGet("Items")]
        public ActionResult<ReportResult> GetItemReport(
            [FromQuery] string maxOfficialRating,
            [FromQuery] bool? hasThemeSong,
            [FromQuery] bool? hasThemeVideo,
            [FromQuery] bool? hasSubtitles,
            [FromQuery] bool? hasSpecialFeature,
            [FromQuery] bool? hasTrailer,
            [FromQuery] string? adjacentTo,
            [FromQuery] int? minIndexNumber,
            [FromQuery] int? parentIndexNumber,
            [FromQuery] bool? hasParentalRating,
            [FromQuery] bool? isHd,
            [FromQuery] string? locationTypes,
            [FromQuery] string? excludeLocationTypes,
            [FromQuery] bool? isMissing,
            [FromQuery] bool? isUnaried,
            [FromQuery] double? minCommunityRating,
            [FromQuery] double? minCriticRating,
            [FromQuery] int? airedDuringSeason,
            [FromQuery] string? minPremiereDate,
            [FromQuery] string? minDateLastSaved,
            [FromQuery] string? minDateLastSavedForUser,
            [FromQuery] string? maxPremiereDate,
            [FromQuery] bool? hasOverview,
            [FromQuery] bool? hasImdbId,
            [FromQuery] bool? hasTmdbId,
            [FromQuery] bool? hasTvdbId,
            [FromQuery] bool? isInBoxSet,
            [FromQuery] string? excludeItemIds,
            [FromQuery] bool? enableTotalRecordCount,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] bool? recursive,
            [FromQuery] string? sortOrder,
            [FromQuery] string? parentId,
            [FromQuery] string? fields,
            [FromQuery] string? excludeItemTypes,
            [FromQuery] string? includeItemTypes,
            [FromQuery] string? filters,
            [FromQuery] bool? isFavorite,
            [FromQuery] bool? isNotFavorite,
            [FromQuery] string? mediaTypes,
            [FromQuery] string? imageTypes,
            [FromQuery] string? sortBy,
            [FromQuery] bool? isPlayed,
            [FromQuery] string? genres,
            [FromQuery] string? genreIds,
            [FromQuery] string? officialRatings,
            [FromQuery] string? tags,
            [FromQuery] string? years,
            [FromQuery] bool? enableUserData,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string? enableImageTypes,
            [FromQuery] string? person,
            [FromQuery] string? personIds,
            [FromQuery] string? personTypes,
            [FromQuery] string? studios,
            [FromQuery] string? studioIds,
            [FromQuery] string? artists,
            [FromQuery] string? excludeArtistIds,
            [FromQuery] string? artistIds,
            [FromQuery] string? albums,
            [FromQuery] string? albumIds,
            [FromQuery] string? ids,
            [FromQuery] string? videoTypes,
            [FromQuery] string? userId,
            [FromQuery] string? minOfficialRating,
            [FromQuery] bool? isLocked,
            [FromQuery] bool? isPlaceHolder,
            [FromQuery] bool? hasOfficialRating,
            [FromQuery] bool? collapseBoxSetItems,
            [FromQuery] bool? is3D,
            [FromQuery] string? seriesStatus,
            [FromQuery] string? nameStartsWithOrGreater,
            [FromQuery] string? nameStartsWith,
            [FromQuery] string? nameLessThan,
            [FromQuery] string? reportView,
            [FromQuery] string? displayType,
            [FromQuery] bool? hasQueryLimit,
            [FromQuery] string? groupBy,
            [FromQuery] string? reportColumns,
            [FromQuery] bool enableImages = true)
        {
            var request = new GetItemReport
            {
                Albums = albums,
                AdjacentTo = adjacentTo,
                AiredDuringSeason = airedDuringSeason,
                AlbumIds = albumIds,
                ArtistIds = artistIds,
                Artists = artists,
                CollapseBoxSetItems = collapseBoxSetItems,
                DisplayType = displayType,
                EnableImages = enableImages,
                EnableImageTypes = enableImageTypes,
                Fields = fields,
                Filters = filters,
                Genres = genres,
                Ids = ids,
                Limit = limit,
                Person = person,
                Recursive = recursive ?? true,
                Studios = studios,
                Tags = tags,
                Years = years,
                GenreIds = genreIds,
                GroupBy = groupBy,
                HasOverview = hasOverview,
                HasSubtitles = hasSubtitles,
                HasTrailer = hasTrailer,
                ImageTypes = imageTypes,
                Is3D = is3D,
                IsFavorite = isFavorite ?? false,
                IsLocked = isLocked,
                IsMissing = isMissing,
                IsPlayed = isPlayed,
                IsUnaired = isUnaried,
                LocationTypes = locationTypes,
                MediaTypes = mediaTypes,
                OfficialRatings = officialRatings,
                ParentId = parentId,
                PersonIds = personIds,
                PersonTypes = personTypes,
                ReportColumns = reportColumns,
                ReportView = reportView,
                SeriesStatus = seriesStatus,
                SortBy = sortBy,
                SortOrder = sortOrder,
                StartIndex = startIndex,
                StudioIds = studioIds,
                UserId = userId,
                VideoTypes = videoTypes,
                EnableUserData = enableUserData,
                ExcludeArtistIds = excludeArtistIds,
                ExcludeItemIds = excludeItemIds,
                ExcludeItemTypes = excludeItemTypes,
                ExcludeLocationTypes = excludeLocationTypes,
                HasImdbId = hasImdbId,
                HasOfficialRating = hasOfficialRating,
                HasParentalRating = hasParentalRating,
                HasQueryLimit = hasQueryLimit ?? false,
                HasSpecialFeature = hasSpecialFeature,
                HasThemeSong = hasThemeSong,
                HasThemeVideo = hasThemeVideo,
                HasTmdbId = hasTmdbId,
                HasTvdbId = hasTvdbId,
                ImageTypeLimit = imageTypeLimit,
                IncludeItemTypes = includeItemTypes,
                IsHD = isHd,
                IsNotFavorite = isNotFavorite ?? false,
                IsPlaceHolder = isPlaceHolder,
                MaxOfficialRating = maxOfficialRating,
                MaxPremiereDate = maxPremiereDate,
                MinCommunityRating = minCommunityRating,
                MinCriticRating = minCriticRating,
                MinIndexNumber = minIndexNumber,
                MinOfficialRating = minOfficialRating,
                MinPremiereDate = minPremiereDate,
                NameLessThan = nameLessThan,
                NameStartsWith = nameStartsWith,
                ParentIndexNumber = parentIndexNumber,
                EnableTotalRecordCount = enableTotalRecordCount ?? true,
                IsInBoxSet = isInBoxSet,
                MinDateLastSaved = minDateLastSaved,
                NameStartsWithOrGreater = nameStartsWithOrGreater,
                MinDateLastSavedForUser = minDateLastSavedForUser
            };
            return Ok(_reportsService.Get(request));
        }
        
        /// <summary>
        /// Gets reports headers based on library items.
        /// </summary>
        /// <param name="reportView">The report view. Values (ReportData, ReportActivities).</param>
        /// <param name="displayType">The report display type. Values (None, Screen, Export, ScreenExport).</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.</param>
        /// <param name="reportColumns">Optional. The columns to show.</param>
        /// <returns></returns>
        [HttpGet("Headers")]
        public ActionResult GetReportHeaders(
            [FromQuery] string reportView,
            [FromQuery] string displayType,
            [FromQuery] string includeItemTypes,
            [FromQuery] string reportColumns)
        {
            var request = new GetReportHeaders
            {
                ReportView = reportView,
                DisplayType = displayType,
                IncludeItemTypes = includeItemTypes,
                ReportColumns = reportColumns
            };
            
            return Ok(_reportsService.Get(request));
        }

        /// <summary>
        /// Gets activities entries.
        /// </summary>
        [HttpGet("Activities")]
        public async Task<ActionResult> GetActivityLogs(
            [FromQuery] string? reportView,
            [FromQuery] string? displayType,
            [FromQuery] bool? hasQueryLimit,
            [FromQuery] string? groupBy,
            [FromQuery] string? reportColumns,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? minDate,
            [FromQuery] string? includeItemTypes)
        {
            var request = new GetActivityLogs
            {
                ReportView = reportView,
                Limit = limit,
                DisplayType = displayType,
                GroupBy = groupBy,
                MinDate = minDate,
                ReportColumns = reportColumns,
                StartIndex = startIndex,
                HasQueryLimit = hasQueryLimit ?? false,
                IncludeItemTypes = includeItemTypes
            };

            return Ok(await _reportsService.Get(request).ConfigureAwait(false));
        }
        
        /// <summary>
        /// Downloads report.
        /// </summary>
        [HttpGet("Items/Download")]
        public async Task<ActionResult<ReportResult>> GetReportDownload(
            [FromQuery] string maxOfficialRating,
            [FromQuery] bool? hasThemeSong,
            [FromQuery] bool? hasThemeVideo,
            [FromQuery] bool? hasSubtitles,
            [FromQuery] bool? hasSpecialFeature,
            [FromQuery] bool? hasTrailer,
            [FromQuery] string? adjacentTo,
            [FromQuery] int? minIndexNumber,
            [FromQuery] int? parentIndexNumber,
            [FromQuery] bool? hasParentalRating,
            [FromQuery] bool? isHd,
            [FromQuery] string? locationTypes,
            [FromQuery] string? excludeLocationTypes,
            [FromQuery] bool? isMissing,
            [FromQuery] bool? isUnaried,
            [FromQuery] double? minCommunityRating,
            [FromQuery] double? minCriticRating,
            [FromQuery] int? airedDuringSeason,
            [FromQuery] string? minPremiereDate,
            [FromQuery] string? minDateLastSaved,
            [FromQuery] string? minDateLastSavedForUser,
            [FromQuery] string? maxPremiereDate,
            [FromQuery] bool? hasOverview,
            [FromQuery] bool? hasImdbId,
            [FromQuery] bool? hasTmdbId,
            [FromQuery] bool? hasTvdbId,
            [FromQuery] bool? isInBoxSet,
            [FromQuery] string? excludeItemIds,
            [FromQuery] bool? enableTotalRecordCount,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] bool? recursive,
            [FromQuery] string? sortOrder,
            [FromQuery] string? parentId,
            [FromQuery] string? fields,
            [FromQuery] string? excludeItemTypes,
            [FromQuery] string? includeItemTypes,
            [FromQuery] string? filters,
            [FromQuery] bool? isFavorite,
            [FromQuery] bool? isNotFavorite,
            [FromQuery] string? mediaTypes,
            [FromQuery] string? imageTypes,
            [FromQuery] string? sortBy,
            [FromQuery] bool? isPlayed,
            [FromQuery] string? genres,
            [FromQuery] string? genreIds,
            [FromQuery] string? officialRatings,
            [FromQuery] string? tags,
            [FromQuery] string? years,
            [FromQuery] bool? enableUserData,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string? enableImageTypes,
            [FromQuery] string? person,
            [FromQuery] string? personIds,
            [FromQuery] string? personTypes,
            [FromQuery] string? studios,
            [FromQuery] string? studioIds,
            [FromQuery] string? artists,
            [FromQuery] string? excludeArtistIds,
            [FromQuery] string? artistIds,
            [FromQuery] string? albums,
            [FromQuery] string? albumIds,
            [FromQuery] string? ids,
            [FromQuery] string? videoTypes,
            [FromQuery] string? userId,
            [FromQuery] string? minOfficialRating,
            [FromQuery] bool? isLocked,
            [FromQuery] bool? isPlaceHolder,
            [FromQuery] bool? hasOfficialRating,
            [FromQuery] bool? collapseBoxSetItems,
            [FromQuery] bool? is3D,
            [FromQuery] string? seriesStatus,
            [FromQuery] string? nameStartsWithOrGreater,
            [FromQuery] string? nameStartsWith,
            [FromQuery] string? nameLessThan,
            [FromQuery] string? reportView,
            [FromQuery] string? displayType,
            [FromQuery] bool? hasQueryLimit,
            [FromQuery] string? groupBy,
            [FromQuery] string? reportColumns,
            [FromQuery] string? minDate,
            [FromQuery] ReportExportType exportType = ReportExportType.CSV,
            [FromQuery] bool enableImages = true)
        {
            var request = new GetReportDownload
            {
                Albums = albums,
                AdjacentTo = adjacentTo,
                AiredDuringSeason = airedDuringSeason,
                AlbumIds = albumIds,
                ArtistIds = artistIds,
                Artists = artists,
                CollapseBoxSetItems = collapseBoxSetItems,
                DisplayType = displayType,
                EnableImages = enableImages,
                EnableImageTypes = enableImageTypes,
                Fields = fields,
                Filters = filters,
                Genres = genres,
                Ids = ids,
                Limit = limit,
                Person = person,
                Recursive = recursive ?? true,
                Studios = studios,
                Tags = tags,
                Years = years,
                GenreIds = genreIds,
                GroupBy = groupBy,
                HasOverview = hasOverview,
                HasSubtitles = hasSubtitles,
                HasTrailer = hasTrailer,
                ImageTypes = imageTypes,
                Is3D = is3D,
                IsFavorite = isFavorite ?? false,
                IsLocked = isLocked,
                IsMissing = isMissing,
                IsPlayed = isPlayed,
                IsUnaired = isUnaried,
                LocationTypes = locationTypes,
                MediaTypes = mediaTypes,
                OfficialRatings = officialRatings,
                ParentId = parentId,
                PersonIds = personIds,
                PersonTypes = personTypes,
                ReportColumns = reportColumns,
                ReportView = reportView,
                SeriesStatus = seriesStatus,
                SortBy = sortBy,
                SortOrder = sortOrder,
                StartIndex = startIndex,
                StudioIds = studioIds,
                UserId = userId,
                VideoTypes = videoTypes,
                EnableUserData = enableUserData,
                ExcludeArtistIds = excludeArtistIds,
                ExcludeItemIds = excludeItemIds,
                ExcludeItemTypes = excludeItemTypes,
                ExcludeLocationTypes = excludeLocationTypes,
                HasImdbId = hasImdbId,
                HasOfficialRating = hasOfficialRating,
                HasParentalRating = hasParentalRating,
                HasQueryLimit = hasQueryLimit ?? false,
                HasSpecialFeature = hasSpecialFeature,
                HasThemeSong = hasThemeSong,
                HasThemeVideo = hasThemeVideo,
                HasTmdbId = hasTmdbId,
                HasTvdbId = hasTvdbId,
                ImageTypeLimit = imageTypeLimit,
                IncludeItemTypes = includeItemTypes,
                IsHD = isHd,
                IsNotFavorite = isNotFavorite ?? false,
                IsPlaceHolder = isPlaceHolder,
                MaxOfficialRating = maxOfficialRating,
                MaxPremiereDate = maxPremiereDate,
                MinCommunityRating = minCommunityRating,
                MinCriticRating = minCriticRating,
                MinIndexNumber = minIndexNumber,
                MinOfficialRating = minOfficialRating,
                MinPremiereDate = minPremiereDate,
                NameLessThan = nameLessThan,
                NameStartsWith = nameStartsWith,
                ParentIndexNumber = parentIndexNumber,
                EnableTotalRecordCount = enableTotalRecordCount ?? true,
                IsInBoxSet = isInBoxSet,
                MinDateLastSaved = minDateLastSaved,
                NameStartsWithOrGreater = nameStartsWithOrGreater,
                MinDateLastSavedForUser = minDateLastSavedForUser,
                ExportType = exportType,
                MinDate = minDate
            };
            var (content, contentType, headers) = await _reportsService.Get(request).ConfigureAwait(false);

            foreach (var (key, value) in headers)
            {
                Response.Headers.Add(key, value);
            }
            
            return Content(content, contentType);
        }
    }
}
