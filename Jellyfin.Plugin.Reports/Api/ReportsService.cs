using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Plugin.Reports.Api.Activities;
using Jellyfin.Plugin.Reports.Api.Common;
using Jellyfin.Plugin.Reports.Api.Data;
using Jellyfin.Plugin.Reports.Api.Model;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using User = Jellyfin.Data.Entities.User;

namespace Jellyfin.Plugin.Reports.Api
{
    /// <summary> The reports service. </summary>
    /// <seealso cref="T:MediaBrowser.Api.BaseApiService"/>
    public class ReportsService
    {
        #region [Constructors]

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportsService class. </summary>
        /// <param name="userManager"> Manager for user. </param>
        /// <param name="libraryManager"> Manager for library. </param>
        /// <param name="localization"> The localization. </param>
        /// <param name="activityManager"> Manager for activity. </param>
        public ReportsService(IUserManager userManager, ILibraryManager libraryManager, ILocalizationManager localization, IActivityManager activityManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _localization = localization;
            _activityManager = activityManager;
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
        public async Task<object> Get(GetActivityLogs request)
        {
            request.DisplayType = "Screen";
            ReportResult result = await GetReportActivities(request).ConfigureAwait(false);
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
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(new Guid(request.UserId)) : null;
            var reportResult = GetReportResult(request, user);

            return reportResult;
        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public async Task<(string content, string contentType, Dictionary<string,string> headers)> Get(GetReportDownload request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return (null, null, null);

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

            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(new Guid(request.UserId)) : null;
            ReportResult result = null;
            switch (reportViewType)
            {
                case ReportViewType.ReportData:
                    ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
                    ReportBuilder dataBuilder = new ReportBuilder(_libraryManager);
                    QueryResult<BaseItem> queryResult = GetQueryResult(request, user);
                    result = dataBuilder.GetResult(queryResult.Items, request);
                    result.TotalRecordCount = queryResult.TotalRecordCount;
                    break;
                case ReportViewType.ReportActivities:
                    result = await GetReportActivities(request).ConfigureAwait(false);
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

            return (returnResult, contentType, headers);
        }

        #endregion

        private InternalItemsQuery GetItemsQuery(BaseReportRequest request, User user)
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
                StartIndex = request.StartIndex,
                IsMissing = request.IsMissing,
                IsUnaired = request.IsUnaired,
                CollapseBoxSetItems = request.CollapseBoxSetItems,
                NameLessThan = request.NameLessThan,
                NameStartsWith = request.NameStartsWith,
                NameStartsWithOrGreater = request.NameStartsWithOrGreater,
                HasImdbId = request.HasImdbId,
                IsPlaceHolder = request.IsPlaceHolder,
                IsLocked = request.IsLocked,
                IsHD = request.IsHD,
                Is3D = request.Is3D,
                HasTvdbId = request.HasTvdbId,
                HasTmdbId = request.HasTmdbId,
                HasOverview = request.HasOverview,
                HasOfficialRating = request.HasOfficialRating,
                HasParentalRating = request.HasParentalRating,
                HasSpecialFeature = request.HasSpecialFeature,
                HasSubtitles = request.HasSubtitles,
                HasThemeSong = request.HasThemeSong,
                HasThemeVideo = request.HasThemeVideo,
                HasTrailer = request.HasTrailer,
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                GenreIds = request.GetGenreIds(),
                StudioIds = request.GetStudioIds(),
                Person = request.Person,
                PersonIds = request.GetPersonIds(),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                ImageTypes = request.GetImageTypes().ToArray(),
                VideoTypes = request.GetVideoTypes().ToArray(),
                AdjacentTo = request.AdjacentTo,
                ItemIds = request.GetItemIds(),
                MinCommunityRating = request.MinCommunityRating,
                MinCriticRating = request.MinCriticRating,
                ParentId = string.IsNullOrWhiteSpace(request.ParentId) ? Guid.Empty : new Guid(request.ParentId),
                ParentIndexNumber = request.ParentIndexNumber,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            };

            if (request.Limit == -1)
            {
                query.Limit = null;
            }

            if (!string.IsNullOrWhiteSpace(request.Ids))
            {
                query.CollapseBoxSetItems = false;
            }

            query.IsFavorite = null;
            if(request.IsFavorite == true)
            {
                query.IsFavorite = true;
            }
            else if (request.IsNotFavorite == true)
            {
                query.IsFavorite = false;
            }

            foreach (var filter in request.GetFilters())
            {
                switch (filter)
                {
                    case ItemFilter.Dislikes:
                        query.IsLiked = false;
                        break;
                    case ItemFilter.IsFavoriteOrLikes:
                        query.IsFavoriteOrLiked = true;
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

            query.CollapseBoxSetItems = false;

            if (request.Limit > -1 && request.Limit < int.MaxValue)
            {
                query.Limit = request.Limit;
            }

            return query;
        }

        private QueryResult<BaseItem> GetQueryResult(BaseReportRequest request, User user)
        {
            // all report queries currently need this because it's not being specified
            request.Recursive = true;

            BaseItem item = null;

            if (!string.IsNullOrEmpty(request.ParentId))
            {
                item = _libraryManager.GetItemById(request.ParentId);
            }

            if (string.Equals(request.IncludeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase))
            {
                //item = user == null ? _libraryManager.RootFolder : user.RootFolder;
            }
            else if (string.Equals(request.IncludeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase))
            {
                item = _libraryManager.GetUserRootFolder();
            }

            // Default list type = children

            Folder folder = item as Folder;
            if (folder is null)
            {
                folder = _libraryManager.GetUserRootFolder();
            }

            if (!string.IsNullOrEmpty(request.Ids))
            {
                request.Recursive = true;
                var query = GetItemsQuery(request, user);
                var result = folder.GetItems(query);

                if (string.IsNullOrWhiteSpace(request.SortBy))
                {
                    var ids = query.ItemIds.ToList();

                    // Try to preserve order
                    result.Items = result.Items.OrderBy(i => ids.IndexOf(i.Id)).ToArray();
                }

                return result;
            }

            if (request.Recursive)
            {
                return folder.GetItems(GetItemsQuery(request, user));
            }

            if (user == null)
            {
                return folder.GetItems(GetItemsQuery(request, null));
            }

            var userRoot = item as UserRootFolder;

            if (userRoot == null)
            {
                return folder.GetItems(GetItemsQuery(request, user));
            }

            IEnumerable<BaseItem> items = folder.GetChildren(user, true);

            var itemsArray = items.ToArray();

            return new QueryResult<BaseItem>
            {
                Items = itemsArray,
                TotalRecordCount = itemsArray.Length
            };
        }

        #region [Private Methods]

        /// <summary> Gets report activities. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The report activities. </returns>
        private async Task<ReportResult> GetReportActivities(IReportsDownload request)
        {
            var activityLogQuery = new ActivityLogQuery
            {
                Skip = request.StartIndex,
                Limit = request.HasQueryLimit ? request.Limit : null
            };

            var queryResult = await _activityManager.GetPagedResultAsync(activityLogQuery).ConfigureAwait(false);
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
            QueryResult<BaseItem> queryResult = GetQueryResult(request, user);
            ReportResult reportResult = reportBuilder.GetResult(queryResult.Items, request);
            reportResult.TotalRecordCount = queryResult.TotalRecordCount;

            return reportResult;
        }

        #endregion

    }
}
