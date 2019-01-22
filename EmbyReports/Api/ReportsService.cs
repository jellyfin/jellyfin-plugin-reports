using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using EmbyReports.Api.Common;
using EmbyReports.Api.Data;
using EmbyReports.Api.Model;
using EmbyReports.Api.Activities;
using MediaBrowser.Model.Services;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Dto;

namespace EmbyReports.Api
{
    /// <summary> The reports service. </summary>
    /// <seealso cref="T:MediaBrowser.Api.BaseApiService"/>
    public class ReportsService : IService, IRequiresRequest
    {
        public IRequest Request { get; set; }
        private IHttpResultFactory _resultFactory;

        #region [Constructors]

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportsService class. </summary>
        /// <param name="userManager"> Manager for user. </param>
        /// <param name="libraryManager"> Manager for library. </param>
        /// <param name="localization"> The localization. </param>
        /// <param name="activityManager"> Manager for activity. </param>
        public ReportsService(IUserManager userManager, ILibraryManager libraryManager, ILocalizationManager localization, IActivityManager activityManager, IHttpResultFactory resultFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _localization = localization;
            _activityManager = activityManager;
            _resultFactory = resultFactory;
        }

        #endregion

        #region [Private Fields]

        private readonly IActivityManager _activityManager; ///< Manager for activity

        /// <summary> Manager for library. </summary>
        private readonly ILibraryManager _libraryManager;   ///< Manager for library
                                                            /// <summary> The localization. </summary>

        private readonly ILocalizationManager _localization;    ///< The localization

        /// <summary> Manager for user. </summary>
        private readonly IUserManager _userManager; ///< Manager for user

        #endregion

        #region [Public Methods]

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public object Get(GetActivityLogs request)
        {
            request.DisplayType = "Screen";
            ReportResult result = GetReportActivities(request);
            return result;
        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public object Get(GetReportHeaders request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;

            request.DisplayType = "Screen";
            ReportViewType reportViewType = ReportHelper.GetReportViewType(request.ReportView);

            List<ReportHeader> result = new List<ReportHeader>();
            switch (reportViewType)
            {
                case ReportViewType.ReportData:
                    ReportBuilder dataBuilder = new ReportBuilder(_libraryManager);
                    result = dataBuilder.GetHeaders(request);
                    break;
                case ReportViewType.ReportActivities:
                    ReportActivitiesBuilder activityBuilder = new ReportActivitiesBuilder(_libraryManager, _userManager);
                    result = activityBuilder.GetHeaders(request);
                    break;
            }

            return result;

        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public object Get(GetItemReport request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;

            request.DisplayType = "Screen";
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;
            var reportResult = GetReportResult(request, user);

            return reportResult;
        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public object Get(GetReportDownload request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;

            request.DisplayType = "Export";
            ReportViewType reportViewType = ReportHelper.GetReportViewType(request.ReportView);
            var headers = new Dictionary<string, string>();
            string fileExtension = "csv";
            string contentType = "text/plain;charset='utf-8'";

            switch (request.ExportType)
            {
                case ReportExportType.CSV:
                    break;
                case ReportExportType.Excel:
                    contentType = "application/vnd.ms-excel";
                    fileExtension = "xls";
                    break;
            }

            var filename = "ReportExport." + fileExtension;
            headers["Content-Disposition"] = string.Format("attachment; filename=\"{0}\"", filename);
            headers["Content-Encoding"] = "UTF-8";

            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;
            ReportResult result = null;
            switch (reportViewType)
            {
                case ReportViewType.ReportData:
                    ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
                    ReportBuilder dataBuilder = new ReportBuilder(_libraryManager);
                    QueryResult<BaseItem> queryResult = GetQueryResult(request, new DtoOptions(), user);
                    result = dataBuilder.GetResult(queryResult.Items, request);
                    result.TotalRecordCount = queryResult.TotalRecordCount;
                    break;
                case ReportViewType.ReportActivities:
                    result = GetReportActivities(request);
                    break;
            }

            string returnResult = string.Empty;
            switch (request.ExportType)
            {
                case ReportExportType.CSV:
                    returnResult = new ReportExport().ExportToCsv(result);
                    break;
                case ReportExportType.Excel:
                    returnResult = new ReportExport().ExportToExcel(result);
                    break;
            }

            return _resultFactory.GetResult(returnResult, contentType, headers);
        }

        #endregion

        private InternalItemsQuery GetItemsQuery(BaseReportRequest request, DtoOptions dtoOptions, User user)
        {
            var query = new InternalItemsQuery(user)
            {
                IsPlayed = request.IsPlayed,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                ExcludeItemTypes = request.GetExcludeItemTypes(),
                Recursive = request.Recursive,
                OrderBy = request.GetOrderBy(),

                IsFavorite = request.IsFavorite,
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                IsMissing = request.IsMissing,
                IsUnaired = request.IsUnaired,
                NameLessThan = request.NameLessThan,
                NameStartsWith = request.NameStartsWith,
                NameStartsWithOrGreater = request.NameStartsWithOrGreater,
                IsLocked = request.IsLocked,
                MinWidth = request.MinWidth,
                MinHeight = request.MinHeight,
                MaxWidth = request.MaxWidth,
                MaxHeight = request.MaxHeight,
                Is3D = request.Is3D,
                HasOverview = request.HasOverview,
                HasOfficialRating = request.HasOfficialRating,
                HasParentalRating = request.HasParentalRating,
                HasSubtitles = request.HasSubtitles,
                HasThemeSong = request.HasThemeSong,
                HasThemeVideo = request.HasThemeVideo,
                HasTrailer = request.HasTrailer,
                IsHD = request.IsHD,
                Is4K = request.Is4K,
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                ArtistIds = ParseIds(request.ArtistIds, _libraryManager),
                AlbumArtistIds = ParseIds(request.AlbumArtistIds, _libraryManager),
                ContributingArtistIds = ParseIds(request.ContributingArtistIds, _libraryManager),
                GenreIds = ParseIds(request.GenreIds, _libraryManager),
                StudioIds = ParseIds(request.StudioIds, _libraryManager),
                PersonIds = ParseIds(request.PersonIds, _libraryManager),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                ImageTypes = request.GetImageTypes(),
                AdjacentTo = request.AdjacentTo,
                ItemIds = ParseIds(request.Ids, _libraryManager),
                MinPlayers = request.MinPlayers,
                MaxPlayers = request.MaxPlayers,
                MinCommunityRating = request.MinCommunityRating,
                MinCriticRating = request.MinCriticRating,
                ParentIndexNumber = request.ParentIndexNumber,
                EnableTotalRecordCount = request.EnableTotalRecordCount,
                ExcludeItemIds = ParseIds(request.ExcludeItemIds, _libraryManager),
                DtoOptions = dtoOptions,
                SearchTerm = request.SearchTerm,
                IsMovie = request.IsMovie,
                IsSports = request.IsSports,
                IsKids = request.IsKids,
                IsNews = request.IsNews,
                IsSeries = request.IsSeries
            };

            var hasAnyProviderId = new List<string>();
            var missingAnyProviderId = new List<string>();

            if (request.HasImdbId.HasValue)
            {
                if (request.HasImdbId.Value)
                {
                    hasAnyProviderId.Add(MetadataProviders.Imdb.ToString());
                }
                else
                {
                    missingAnyProviderId.Add(MetadataProviders.Imdb.ToString());
                }
            }

            if (request.HasTvdbId.HasValue)
            {
                if (request.HasTvdbId.Value)
                {
                    hasAnyProviderId.Add(MetadataProviders.Tvdb.ToString());
                }
                else
                {
                    missingAnyProviderId.Add(MetadataProviders.Tvdb.ToString());
                }
            }

            if (request.HasTmdbId.HasValue)
            {
                if (request.HasTmdbId.Value)
                {
                    hasAnyProviderId.Add(MetadataProviders.Tmdb.ToString());
                }
                else
                {
                    missingAnyProviderId.Add(MetadataProviders.Tmdb.ToString());
                }
            }

            query.HasAnyProviderId = hasAnyProviderId;
            query.MissingAnyProviderId = missingAnyProviderId.ToArray();

            foreach (var filter in request.GetFilters())
            {
                switch (filter)
                {
                    case ItemFilter.Dislikes:
                        query.IsLiked = false;
                        break;
                    case ItemFilter.IsFavorite:
                        query.IsFavorite = true;
                        break;
                    case ItemFilter.IsFavoriteOrLikes:
                        query.IsFavorite = true;
                        break;
                    case ItemFilter.IsFolder:
                        query.IsFolder = true;
                        break;
                    case ItemFilter.IsNotFolder:
                        query.IsFolder = false;
                        break;
                    case ItemFilter.IsPlayed:
                        query.IsPlayed = true;
                        break;
                    case ItemFilter.IsResumable:
                        query.IsResumable = true;
                        break;
                    case ItemFilter.IsUnplayed:
                        query.IsPlayed = false;
                        break;
                    case ItemFilter.Likes:
                        query.IsLiked = true;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(request.MinDateLastSaved))
            {
                query.MinDateLastSaved = DateTime.Parse(request.MinDateLastSaved, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MinDateLastSavedForUser))
            {
                query.MinDateLastSavedForUser = DateTime.Parse(request.MinDateLastSavedForUser, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MinPremiereDate))
            {
                query.MinPremiereDate = DateTime.Parse(request.MinPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxPremiereDate))
            {
                query.MaxPremiereDate = DateTime.Parse(request.MaxPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            // Filter by Series Status
            if (!string.IsNullOrEmpty(request.SeriesStatus))
            {
                query.SeriesStatuses = request.SeriesStatus.Split(',').Select(d => (SeriesStatus)Enum.Parse(typeof(SeriesStatus), d, true)).ToArray();
            }

            // ExcludeLocationTypes
            if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
            {
                var excludeLocationTypes = request.ExcludeLocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray();
                if (excludeLocationTypes.Contains(LocationType.Virtual))
                {
                    query.IsVirtualItem = false;
                }
            }

            if (!string.IsNullOrEmpty(request.LocationTypes))
            {
                var requestedLocationTypes =
                    request.LocationTypes.Split(',');

                if (requestedLocationTypes.Length > 0 && requestedLocationTypes.Length < 4)
                {
                    query.IsVirtualItem = requestedLocationTypes.Contains(LocationType.Virtual.ToString());
                }
            }

            // Min official rating
            if (!string.IsNullOrWhiteSpace(request.MinOfficialRating))
            {
                query.MinParentalRating = _localization.GetRatingLevel(request.MinOfficialRating);
            }

            // Max official rating
            if (!string.IsNullOrWhiteSpace(request.MaxOfficialRating))
            {
                query.MaxParentalRating = _localization.GetRatingLevel(request.MaxOfficialRating);
            }

            // ExcludeArtistIds
            if (!string.IsNullOrWhiteSpace(request.ExcludeArtistIds))
            {
                query.ExcludeArtistIds = ParseIds(request.ExcludeArtistIds, _libraryManager);
            }

            if (!string.IsNullOrWhiteSpace(request.AlbumIds))
            {
                query.AlbumIds = ParseIds(request.AlbumIds, _libraryManager);
            }

            // Apply default sorting if none requested
            if (query.OrderBy.Length == 0)
            {
                // Albums by artist
                if ((query.ArtistIds.Length > 0) && query.IncludeItemTypes.Length == 1 && string.Equals(query.IncludeItemTypes[0], "MusicAlbum", StringComparison.OrdinalIgnoreCase))
                {
                    query.OrderBy = new[]
                    {
                        new ValueTuple<string, SortOrder>(ItemSortBy.ProductionYear, SortOrder.Descending),
                        new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending)
                    };
                }
            }

            return query;
        }

        private QueryResult<BaseItem> GetQueryResult(BaseReportRequest request, DtoOptions dtoOptions, User user)
        {
            var item = string.IsNullOrEmpty(request.ParentId) ?
                null :
                _libraryManager.GetItemById(request.ParentId);

            if (item == null)
            {
                item = string.IsNullOrEmpty(request.ParentId) ?
                    user == null ? _libraryManager.RootFolder : _libraryManager.GetUserRootFolder() :
                    _libraryManager.GetItemById(request.ParentId);
            }

            // Default list type = children

            var folder = item as Folder;
            if (folder == null)
            {
                folder = user == null ? _libraryManager.RootFolder : _libraryManager.GetUserRootFolder();
            }

            var hasCollectionType = folder as IHasCollectionType;
            var isPlaylistQuery = (hasCollectionType != null && string.Equals(hasCollectionType.CollectionType, CollectionType.Playlists, StringComparison.OrdinalIgnoreCase));

            if (isPlaylistQuery)
            {
                request.Recursive = true;
                request.IncludeItemTypes = "Playlist";
            }

            if (request.Recursive || !string.IsNullOrEmpty(request.Ids) || user == null)
            {
                return folder.GetItems(GetItemsQuery(request, dtoOptions, user));
            }

            var userRoot = item as UserRootFolder;

            if (userRoot == null)
            {
                return folder.GetItems(GetItemsQuery(request, dtoOptions, user));
            }

            var itemsArray = folder.GetChildren(user).ToArray();

            return new QueryResult<BaseItem>
            {
                Items = itemsArray,
                TotalRecordCount = itemsArray.Length
            };
        }

        protected Guid[] ParseIds(string value, ILibraryManager libraryManager)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Guid.Empty;
            }

            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(libraryManager.GetInternalId)
                .Where(i => i != 0)
                .ToArray();
        }

        #region [Private Methods]

        /// <summary> Gets report activities. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The report activities. </returns>
        private ReportResult GetReportActivities(IReportsDownload request)
        {
            DateTime? minDate = string.IsNullOrWhiteSpace(request.MinDate) ?
            (DateTime?)null :
            DateTime.Parse(request.MinDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

            QueryResult<ActivityLogEntry> queryResult;
            if (request.HasQueryLimit)
                queryResult = _activityManager.GetActivityLogEntries(minDate, request.StartIndex, request.Limit);
            else
                queryResult = _activityManager.GetActivityLogEntries(minDate, request.StartIndex, null);
            //var queryResult = _activityManager.GetActivityLogEntries(minDate, request.StartIndex, request.Limit);

            ReportActivitiesBuilder builder = new ReportActivitiesBuilder(_libraryManager, _userManager);
            var result = builder.GetResult(queryResult, request);
            result.TotalRecordCount = queryResult.TotalRecordCount;
            return result;
        }

        /// <summary> Gets report result. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The report result. </returns>
        private ReportResult GetReportResult(GetItemReport request, User user)
        {
            ReportBuilder reportBuilder = new ReportBuilder(_libraryManager);
            QueryResult<BaseItem> queryResult = GetQueryResult(request, new DtoOptions(), user);
            ReportResult reportResult = reportBuilder.GetResult(queryResult.Items, request);
            reportResult.TotalRecordCount = queryResult.TotalRecordCount;

            return reportResult;
        }

        #endregion

    }
}
