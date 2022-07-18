const defaultSortBy = 'SortName';

const query = {
    StartIndex: 0,
    Limit: 100,
    IncludeItemTypes: 'Movie',
    HasQueryLimit: true,
    GroupBy: 'None',
    ReportView: 'ReportData',
    DisplayType: 'Screen'
};

function getTable(result, initial_state) {
    let html = '';
    //Report table
    html += '<table id="tblReport" data-role="table" data-mode="reflow" class="tblLibraryReport stripedTable ui-responsive table-stroke detailTable" style="display:table;">';
    html += '<thead>';

    //Report headers
    result.Headers.map(function (header) {
        let cellHtml = '<th class="detailTableHeaderCell" data-priority="' + 'persist' + '">';

        if (header.ShowHeaderLabel) {
            if (header.SortField) {
                cellHtml += '<button class="lnkColumnSort button-link" is="emby-button" data-sortfield="' + header.SortField + '" style="text-decoration:underline;">';
            }

            cellHtml += (header.Name || '&nbsp;');
            if (header.SortField) {
                cellHtml += '</button>';
                if (header.SortField === query.SortBy) {
                    if (query.SortOrder === 'Descending') {
                        cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;">&darr;</span>';
                    } else {
                        cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;">&uarr;</span>';
                    }
                }
            }
        }
        cellHtml += '</th>';
        return html += cellHtml;
    });

    html += '</thead>';
    //Report body
    html += '<tbody>';
    if (result.IsGrouped === false) {
        result.Rows.map( (row) => {
            return html += getRow(result.Headers, row);
        });
    } else {
        let row_count = 0;
        let current_state = 'table-row';
        let current_pointer = '&#x25BC;';
        if (initial_state == true) {
            current_state = 'none';
            current_pointer = '&#x25B6;';
        }
        result.Groups.map(function (group) {
            html += '<tr style="background-color: rgb(51, 51, 51); color: rgba(255,255,255,.87);">';
            html += '<th class="detailTableHeaderCell" scope="rowgroup" colspan="' + result.Headers.length + '">';
            html += '<a class="lnkShowHideRows" data-group_id="' + row_count + '" data-group_state="' + current_state + '" style="cursor: pointer;">' + current_pointer + '</a> ';
            html += (group.Name || '&nbsp;') + ' : ' + group.Rows.length;
            html += '</th>';
            html += '</tr>';
            group.Rows.map(function (row) {
                return html += getRow(result.Headers, row, row_count, current_state);
            });
            html += '<tr>';
            html += '<th class="detailTableHeaderCell row_id_' + row_count + '" scope="rowgroup" colspan="' + result.Headers.length + '" style="display:' + current_state + ';">&nbsp;</th>';
            html += '</tr>';
            row_count++;
            return html;
        });
    }

    html += '</tbody>';
    html += '</table>';
    return html;
}

function getRow(rHeaders, rRow, row_count, current_state) {
    let html = '';
    html += '<tr class="detailTableBodyRow detailTableBodyRow-shaded row_id_' + row_count + '" style="display:' + current_state + ';">';

    for (let j = 0; j < rHeaders.length; j++) {
        const rHeader = rHeaders[j];
        const rItem = rRow.Columns[j];
        html += getItem(rHeader, rRow, rItem);
    }
    html += '</tr>';
    return html;
}

function getItem(rHeader, rRow, rItem) {
    let html = '';
    html += '<td class="detailTableBodyCell">';
    let id = rRow.Id;
    if (rItem.Id)
        id = rItem.Id;
    const serverId = rRow.ServerId || rItem.ServerId || ApiClient.serverId();

    switch (rHeader.ItemViewType) {
        case 'None':
            html += rItem.Name;
            break;
        case 'Detail':
            html += '<a is="emby-linkbutton" class="button-link" href="' + Emby.Page.getRouteUrl({ Id: id, ServerId: serverId }) + '">' + rItem.Name + '</a>';
            break;
        case 'Edit':
            html += '<a is="emby-button" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '">' + rItem.Name + '</a>';
            break;
        case 'List':
            html += '<a is="emby-button" class="button-link" href="itemlist.html?serverId=' + rItem.ServerId + '&id=' + rRow.Id + '">' + rItem.Name + '</a>';
            break;
        case 'ItemByNameDetails':
            html += '<a is="emby-button" class="button-link" href="' + Emby.Page.getRouteUrl({ Id: id, ServerId: serverId }) + '">' + rItem.Name + '</a>';
            break;
        case 'EmbeddedImage':
            if (rRow.HasEmbeddedImage) {
                html += '<i class="material-icons check"></i>';
            }
            break;
        case 'SubtitleImage':
            if (rRow.HasSubtitles) {
                html += '<i class="material-icons check"></i>';
            }
            break;
        case 'TrailersImage':
            if (rRow.HasLocalTrailer) {
                html += '<i class="material-icons check"></i>';
            }
            break;
        case 'SpecialsImage':
            if (rRow.HasSpecials) {
                html += '<i class="material-icons photo" title="Missing primary image." style="color:red;"></i>';
            }
            break;
        case 'LockDataImage':
            if (rRow.HasLockData) {
                html += '<i class="material-icons lock"></i>';
            }
            break;
        case 'TagsPrimaryImage':
            if (!rRow.HasImageTagsPrimary) {
                html += '<a is="emby-button" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '"><i class="material-icons photo" title="Missing primary image." style="color:red;"></i></a>';
            }
            break;
        case 'TagsBackdropImage':
            if (!rRow.HasImageTagsBackdrop) {
                if (rRow.RowType !== 'Episode' && rRow.RowType !== 'Season' && rRow.MediaType !== 'Audio' && rRow.RowType !== 'TvChannel' && rRow.RowType !== 'MusicAlbum') {
                    html += '<a is="emby-button" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '"><i class="material-icons photo" title="Missing backdrop image." style="color:orange;"></i></a>';
                }
            }
            break;
        case 'TagsLogoImage':
            if (!rRow.HasImageTagsLogo) {
                if (rRow.RowType === 'Movie' || rRow.RowType === 'Trailer' || rRow.RowType === 'Series' || rRow.RowType === 'MusicArtist' || rRow.RowType === 'BoxSet') {
                    html += '<a is="emby-button" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '"><i class="material-icons photo" title="Missing logo image."></i></a>';
                }
            }
            break;
        case 'UserPrimaryImage':
            if (rRow.UserId) {
                const userImage = ApiClient.getUserImageUrl(rRow.UserId, {
                    height: 24,
                    type: 'Primary'

                });
                if (userImage) {
                    html += '<img src="' + userImage + '" />';
                } else {
                    html += '';
                }
            }
            break;
        case 'StatusImage':
            if (rRow.HasLockData) {
                html += '<i class="material-icons lock"></i>';
            }

            if (!rRow.HasLocalTrailer && rRow.RowType === 'Movie') {
                html += '<i title="Missing local trailer." class="material-icons videocam"></i>';
            }

            if (!rRow.HasImageTagsPrimary) {
                html += '<i class="material-icons photo" title="Missing primary image." style="color:red;"></i>';
            }

            if (!rRow.HasImageTagsBackdrop) {
                if (rRow.RowType !== 'Episode' && rRow.RowType !== 'Season' && rRow.MediaType !== 'Audio' && rRow.RowType !== 'TvChannel' && rRow.RowType !== 'MusicAlbum') {
                    html += '<i class="material-icons photo" title="Missing backdrop image." style="color:orange;"></i>';
                }
            }

            if (!rRow.HasImageTagsLogo) {
                if (rRow.RowType === 'Movie' || rRow.RowType === 'Trailer' || rRow.RowType === 'Series' || rRow.RowType === 'MusicArtist' || rRow.RowType === 'BoxSet') {
                    html += '<i class="material-icons photo" title="Missing logo image."></i>';
                }
            }
            break;
        default:
            html += rItem.Name;
    }
    html += '</td>';
    return html;
}

function ExportReport() {
    query.UserId = Dashboard.getCurrentUserId();
    query.HasQueryLimit = false;
    query.api_key = ApiClient.accessToken();
    const url = ApiClient.getUrl('Reports/Items/Download', query);

    if (url) {
        window.location.href = url;
    }
}

function loadGroupByFilters(page) {
    query.UserId = Dashboard.getCurrentUserId();
    let url = '';

    url = ApiClient.getUrl('Reports/Headers', query);
    ApiClient.getJSON(url).then(function (result) {
        let selected = 'None';

        const selectReportGroup = page.querySelector('#selectReportGroup');
        for (const elem of selectReportGroup.querySelectorAll('option')) {
            const parent = elem.parentNode;
            parent.removeChild(elem);
        }
        selectReportGroup.insertAdjacentHTML('beforeend', '<option value="None">None</option>');
        result.map(function (header) {
            if ((header.DisplayType === 'Screen' || header.DisplayType === 'ScreenExport') && header.CanGroup) {
                if (header.FieldName.length > 0) {
                    const option = '<option value="' + header.FieldName + '">' + header.Name + '</option>';
                    selectReportGroup.insertAdjacentHTML('beforeend', option);
                    if (query.GroupBy === header.FieldName)
                        selected = header.FieldName;
                }
            }
        });
        page.querySelector('#selectPageSize').value = selected;
    });
}

function getQueryPagingHtml(options) {
    const startIndex = options.startIndex;
    const limit = options.limit;
    const totalRecordCount = options.totalRecordCount;
    let html = '';
    const recordsEnd = Math.min(startIndex + limit, totalRecordCount);
    const showControls = limit < totalRecordCount;

    html += '<div class="listPaging">';
    if (showControls) {
        html += '<span style="vertical-align:middle;">';
        const startAtDisplay = totalRecordCount ? startIndex + 1 : 0;
        html += startAtDisplay + '-' + recordsEnd + ' of ' + totalRecordCount;
        html += '</span>';
        html += '<div style="display:inline-block;">';
        html += '<button is="paper-icon-button-light" class="btnPreviousPage autoSize" ' + (startIndex ? '' : 'disabled') + '><span class="material-icons arrow_back"></span></button>';
        html += '<button is="paper-icon-button-light" class="btnNextPage autoSize" ' + (startIndex + limit >= totalRecordCount ? 'disabled' : '') + '><span class="material-icons arrow_forward"></span></button>';
        html += '</div>';
    }

    html += '</div>';

    return html;
}

function renderItems(page, result) {
    window.scrollTo(0, 0);
    let html = '';

    if (query.ReportView === 'ReportData') {
        page.querySelector('#selectIncludeItemTypesBox').classList.remove('hide');
        page.querySelector('#tabFilter').classList.remove('hide');
    } else {
        page.querySelector('#selectIncludeItemTypesBox').classList.add('hide');
        page.querySelector('#tabFilterBox').classList.add('hide');
        page.querySelector('#tabFilter').classList.add('hide');
    }

    let pagingHtml = 'Total : ' + result.TotalRecordCount;
    if (query.Limit != -1) {
        pagingHtml = getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            updatePageSizeSetting: false,
            viewButton: true,
            showLimit: false
        });
    }

    if (query.ReportView === 'ReportData' || query.ReportView === 'ReportActivities') {
        for (const paging of page.querySelectorAll('.paging')) {
            paging.innerHTML = pagingHtml;
            paging.dispatchEvent(new Event('create'));
        }

        for (const btnNextPage of page.querySelectorAll('.btnNextPage')) {
            btnNextPage.addEventListener('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });
        }

        for (const btnPreviousPage of page.querySelectorAll('.btnPreviousPage')) {
            btnPreviousPage.addEventListener('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });
        }

        const initial_state = page.querySelector('#chkStartCollapsed').checked;
        html += getTable(result, initial_state);

        const reporContainer = page.querySelector('.reporContainer');
        reporContainer.innerHTML = html;
        reporContainer.dispatchEvent(new Event('create'));

        for (const elem of page.querySelectorAll('.lnkShowHideRows')) {
            elem.addEventListener('click', function () {
                const row_id = this.getAttribute('data-group_id');
                const row_id_index = 'row_id_' + row_id;
                const row_group_state = this.getAttribute('data-group_state');
                //alert(this.getAttribute("data-group_state"));
                if (row_group_state == 'table-row') {
                    this.setAttribute('data-group_state', 'none');
                    for (const elems of page.querySelectorAll('.' + row_id_index)) {
                        elems.style.display = 'none';
                    }
                    this.innerHTML = '&#x25B6;';
                } else {
                    this.setAttribute('data-group_state', 'table-row');
                    for (const elems of page.querySelectorAll('.' + row_id_index)) {
                        elems.style.display = 'table-row';
                    }
                    this.innerHTML = '&#x25BC;';
                }
            });
        }

        for (const elem of page.querySelectorAll('.lnkColumnSort')) {
            elem.addEventListener('click', function () {
                const order = this.getAttribute('data-sortfield');

                if (query.SortBy === order) {
                    if (query.SortOrder === 'Descending') {
                        query.SortOrder = 'Ascending';
                        query.SortBy = defaultSortBy;
                    } else {
                        query.SortOrder = 'Descending';
                        query.SortBy = order;
                    }
                } else {
                    query.SortOrder = 'Ascending';
                    query.SortBy = order;
                }

                query.StartIndex = 0;

                reloadItems(page);
            });
        }
    }

    page.querySelector('#GroupStatus').classList.add('hide');
    page.querySelector('#GroupAirDays').classList.add('hide');
    page.querySelector('#GroupEpisodes').classList.add('hide');

    switch (query.IncludeItemTypes) {
        case 'Series':
        case 'Season':
            page.querySelector('#GroupStatus').classList.remove('hide');
            page.querySelector('#GroupAirDays').classList.remove('hide');
            break;
        case 'Episode':
            page.querySelector('#GroupStatus').classList.remove('hide');
            page.querySelector('#GroupAirDays').classList.remove('hide');
            page.querySelector('#GroupEpisodes').classList.remove('hide');
            break;
    }
}

function reloadItems(page) {
    Loading.show();

    query.UserId = Dashboard.getCurrentUserId();
    let url = '';

    switch (query.ReportView) {
        case 'ReportData':
            query.HasQueryLimit = true;
            url = ApiClient.getUrl('Reports/Items', query);
            break;
        case 'ReportActivities':
            query.HasQueryLimit = true;
            url = ApiClient.getUrl('Reports/Activities', query);
            break;
    }

    ApiClient.getJSON(url).then(function (result) {
        updateFilterControls(page);
        renderItems(page, result);
    });

    Loading.hide();
}

function updateFilterControls(context) {
    for (const elem of context.querySelectorAll('.chkStandardFilter')) {
        const filters = ',' + (query.Filters || '');
        const filterName = elem.getAttribute('data-filter');

        elem.checked = filters.indexOf(',' + filterName) != -1;
    }

    for (const elem of context.querySelectorAll('.chkVideoTypeFilter')) {
        const filters = ',' + (query.VideoTypes || '');
        const filterName = elem.getAttribute('data-filter');

        elem.checked = filters.indexOf(',' + filterName) != -1;
    }
    for (const elem of context.querySelectorAll('.chkStatus')) {
        const filters = ',' + (query.SeriesStatus || '');
        const filterName = elem.getAttribute('data-filter');

        elem.checked = filters.indexOf(',' + filterName) != -1;
    }
    for (const elem of context.querySelectorAll('.chkAirDays')) {
        const filters = ',' + (query.AirDays || '');
        const filterName = elem.getAttribute('data-filter');

        elem.checked = filters.indexOf(',' + filterName) != -1;
    }

    context.querySelector('#chk3D').checked = query.Is3D == true;
    context.querySelector('#chkHD').checked = query.IsHD == true;
    context.querySelector('#chkSD').checked = query.IsHD == false;

    context.querySelector('#chkSubtitle').checked = query.HasSubtitles == true;
    context.querySelector('#chkTrailer').checked = query.HasTrailer == true;
    context.querySelector('#chkMissingTrailer').checked = query.HasTrailer == false;
    context.querySelector('#chkSpecialFeature').checked = query.HasSpecialFeature == true;
    context.querySelector('#chkThemeSong').checked = query.HasThemeSong == true;
    context.querySelector('#chkThemeVideo').checked = query.HasThemeVideo == true;

    context.querySelector('#selectPageSize').value = query.Limit;

    //Management
    context.querySelector('#chkMissingRating').checked = query.HasOfficialRating == false;
    context.querySelector('#chkMissingOverview').checked = query.HasOverview == false;
    context.querySelector('#chkIsLocked').checked = query.IsLocked == true;
    context.querySelector('#chkMissingImdbId').checked = query.HasImdbId == false;
    context.querySelector('#chkMissingTmdbId').checked = query.HasTmdbId == false;
    context.querySelector('#chkMissingTvdbId').checked = query.HasTvdbId == false;

    //Episodes
    context.querySelector('#chkSpecialEpisode').checked = query.ParentIndexNumber == 0;
    context.querySelector('#chkMissingEpisode').checked = query.IsMissing == true;
    context.querySelector('#chkFutureEpisode').checked = query.IsUnaired == true;

    context.querySelector('#selectIncludeItemTypes').value = query.IncludeItemTypes;

    // isfavorite
    context.querySelector('#chkIsFavorite').checked = query.IsFavorite == true;
    context.querySelector('#chkIsNotFavorite').checked = query.IsNotFavorite == true;
}

let filtersLoaded;
function reloadFiltersIfNeeded(page) {
    if (!filtersLoaded) {
        filtersLoaded = true;

        QueryReportFilters.loadFilters(page, Dashboard.getCurrentUserId(), query, function () {
            reloadItems(page);
        });

        QueryReportColumns.loadColumns(page, Dashboard.getCurrentUserId(), query, function () {
            reloadItems(page);
        });
    }
}

function renderOptions(context, selector, cssClass, items) {
    const elem = context.querySelector(selector);

    if (items.length) {
        elem.classList.remove('hide');
    } else {
        elem.classList.add('hide');
    }

    let html = '';

    //  style="margin: -.2em -.8em;"
    html += '<div data-role="controlgroup">';

    let index = 0;
    const idPrefix = 'chk' + selector.substring(1);

    html += items.map(function (filter) {
        let itemHtml = '';

        const id = idPrefix + index;
        let label = filter;
        let value = filter;
        let checked = false;
        if (filter.FieldName) {
            label = filter.Name;
            value = filter.FieldName;
            checked = filter.Visible;
        }

        itemHtml += '<input id="' + id + '" type="checkbox" data-filter="' + value + '" class="' + cssClass + '"';
        if (checked)
            itemHtml += ' checked="checked" ';
        itemHtml += '/> ';
        itemHtml += '<label for="' + id + '">' + label + '</label>';
        itemHtml += '<br/>';

        index++;

        return itemHtml;
    }).join('');

    html += '</div>';

    elem.querySelector('.filterOptions').innerHTML = html;
    elem.dispatchEvent(new Event('create'));
}

function renderFilters(context, result) {
    if (result.Tags) {
        result.Tags.length = Math.min(result.Tags.length, 50);
    }

    renderOptions(context, '.genreFilters', 'chkGenreFilter', result.Genres);
    renderOptions(context, '.officialRatingFilters', 'chkOfficialRatingFilter', result.OfficialRatings);
    renderOptions(context, '.tagFilters', 'chkTagFilter', result.Tags);
    renderOptions(context, '.yearFilters', 'chkYearFilter', result.Years);
}

function renderColumnss(context, result) {
    if (result.Tags) {
        result.Tags.length = Math.min(result.Tags.length, 50);
    }

    renderOptions(context, '.reportsColumns', 'chkReportColumns', result);
}

function onFiltersLoaded(context, query, reloadItemsFn) {
    for (const elem of context.querySelectorAll('.chkGenreFilter')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.Genres || '';
            const delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Genres = filters;

            reloadItemsFn();
        });
    }

    for (const elem of context.querySelectorAll('.chkTagFilter')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.Tags || '';
            const delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Tags = filters;

            reloadItemsFn();
        });
    }

    for (const elem of context.querySelectorAll('.chkYearFilter')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.Years || '';
            const delimiter = ',';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Years = filters;

            reloadItemsFn();
        });
    }

    for (const elem of context.querySelectorAll('.chkOfficialRatingFilter')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.OfficialRatings || '';
            const delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.OfficialRatings = filters;

            reloadItemsFn();
        });
    }
}

function onColumnsLoaded(context, query, reloadItemsFn) {
    for (const elem of context.querySelectorAll('.chkReportColumns')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.ReportColumns || '';
            const delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.ReportColumns = filters;

            reloadItemsFn();
        });
    }
}

function loadFilters(context, userId, itemQuery, reloadItemsFn) {
    return ApiClient.getJSON(ApiClient.getUrl('Items/Filters', {

        UserId: userId,
        ParentId: itemQuery.ParentId,
        IncludeItemTypes: itemQuery.IncludeItemTypes,
        ReportView: itemQuery.ReportView

    })).then(function (result) {
        renderFilters(context, result);

        onFiltersLoaded(context, itemQuery, reloadItemsFn);
    });
}

function loadColumns(context, userId, itemQuery, reloadItemsFn) {
    return ApiClient.getJSON(ApiClient.getUrl('Reports/Headers', {

        UserId: userId,
        IncludeItemTypes: itemQuery.IncludeItemTypes,
        ReportView: itemQuery.ReportView

    })).then(function (result) {
        renderColumnss(context, result);
        let filters = '';
        const delimiter = '|';
        result.map(function (item) {
            if ((item.DisplayType === 'Screen' || item.DisplayType === 'ScreenExport'))
                filters = filters ? (filters + delimiter + item.FieldName) : item.FieldName;
        });
        if (!itemQuery.ReportColumns)
            itemQuery.ReportColumns = filters;
        onColumnsLoaded(context, itemQuery, reloadItemsFn);
    });
}

function onPageShow(page, query) {
    query.Genres = null;
    query.Years = null;
    query.OfficialRatings = null;
    query.Tags = null;
}

function onPageReportColumnsShow(page, query) {
    query.ReportColumns = null;
}

window.QueryReportFilters = {
    loadFilters: loadFilters,
    onPageShow: onPageShow
};

window.QueryReportColumns = {
    loadColumns: loadColumns,
    onPageShow: onPageReportColumnsShow
};

export default function (view) {
    view.dispatchEvent(new CustomEvent('create'));

    view.querySelector('#selectIncludeItemTypes').addEventListener('change', function () {
        query.StartIndex = 0;
        query.ReportView = view.querySelector('#selectViewType').value;
        query.IncludeItemTypes = this.value;
        query.SortOrder = 'Ascending';
        query.ReportColumns = null;
        filtersLoaded = false;
        loadGroupByFilters(view);
        reloadFiltersIfNeeded(view);
        reloadItems(view);
    });

    view.querySelector('#selectViewType').addEventListener('change', function () {
        query.StartIndex = 0;
        query.ReportView = this.value;
        query.IncludeItemTypes = view.querySelector('#selectIncludeItemTypes').value;
        query.SortOrder = 'Ascending';
        filtersLoaded = false;
        query.ReportColumns = null;
        loadGroupByFilters(view);
        reloadFiltersIfNeeded(view);
        reloadItems(view);
    });

    view.querySelector('#selectReportGroup').addEventListener('change', function () {
        query.GroupBy = this.value;
        query.StartIndex = 0;
        reloadItems(view);
    });

    view.querySelector('#chkStartCollapsed').addEventListener('change', function () {
        reloadItems(view);
    });

    view.querySelector('#btnReportExportCsv').addEventListener('click', function (e) {
        query.ExportType = 'CSV';
        ExportReport(view, e);
    });

    view.querySelector('#btnReportExportHtml').addEventListener('click', function (e) {
        query.ExportType = 'HTML';
        ExportReport(view, e);
    });

    view.querySelector('#btnResetReportColumns').addEventListener('click', function () {
        query.ReportColumns = null;
        query.StartIndex = 0;
        filtersLoaded = false;
        reloadFiltersIfNeeded(view);
        reloadItems(view);
    });

    view.querySelector('#selectPageSize').addEventListener('change', function () {
        query.Limit = parseInt(this.value);
        query.StartIndex = 0;
        reloadItems(view);
    });

    const chkIsFavorite = view.querySelector('#chkIsFavorite');
    chkIsFavorite.addEventListener('change', () => {
        if (chkIsFavorite.checked) {
            query.IsFavorite = true;
        } else {
            query.IsFavorite = false;
        }
        reloadItems(view);
    });
    const chkIsNotFavorite = view.querySelector('#chkIsNotFavorite');
    chkIsNotFavorite.addEventListener('change', () => {
        if (chkIsNotFavorite.checked) {
            query.IsNotFavorite = true;
        } else {
            query.IsNotFavorite = false;
        }
        reloadItems(view);
    });
    for (const elem of view.querySelectorAll('.chkStandardFilter')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.Filters || '';

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(view);
        });
    }

    for (const elem of view.querySelectorAll('.chkVideoTypeFilter')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.VideoTypes || '';

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(view);
        });
    }

    const chk3D = view.querySelector('#chk3D');
    chk3D.addEventListener('change', () => {
        query.StartIndex = 0;
        query.Is3D = chk3D.checked ? true : null;

        reloadItems(view);
    });

    const chkHD = view.querySelector('#chkHD');
    chkHD.addEventListener('change', () => {
        query.StartIndex = 0;
        query.IsHD = chkHD.checked ? true : null;

        reloadItems(view);
    });

    const chkSD = view.querySelector('#chkSD');
    chkSD.addEventListener('change', () => {
        query.StartIndex = 0;
        query.IsHD = chkSD.checked ? false : null;

        reloadItems(view);
    });

    const chkSubtitle = view.querySelector('#chkSubtitle');
    chkSubtitle.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasSubtitles = chkSubtitle.checked ? true : null;

        reloadItems(view);
    });

    const chkTrailer = view.querySelector('#chkTrailer');
    chkTrailer.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasTrailer = chkTrailer.checked ? true : null;

        reloadItems(view);
    });

    const chkMissingTrailer = view.querySelector('#chkMissingTrailer');
    chkMissingTrailer.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasTrailer = chkMissingTrailer.checked ? false : null;

        reloadItems(view);
    });

    const chkSpecialFeature = view.querySelector('#chkSpecialFeature');
    chkSpecialFeature.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasSpecialFeature = chkSpecialFeature.checked ? true : null;

        reloadItems(view);
    });

    const chkThemeSong = view.querySelector('#chkThemeSong');
    chkThemeSong.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasThemeSong = chkThemeSong.checked ? true : null;

        reloadItems(view);
    });

    const chkThemeVideo = view.querySelector('#chkThemeVideo');
    chkThemeVideo.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasThemeVideo = chkThemeVideo.checked ? true : null;

        reloadItems(view);
    });

    //Management
    const chkIsLocked = view.querySelector('#chkIsLocked');
    chkIsLocked.addEventListener('change', () => {
        query.StartIndex = 0;
        query.IsLocked = chkIsLocked.checked ? true : null;

        reloadItems(view);
    });

    const chkMissingOverview = view.querySelector('#chkMissingOverview');
    chkMissingOverview.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasOverview = chkMissingOverview.checked ? false : null;

        reloadItems(view);
    });

    const chkMissingRating = view.querySelector('#chkMissingRating');
    chkMissingRating.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasOfficialRating = chkMissingRating.checked ? false : null;

        reloadItems(view);
    });

    const chkMissingImdbId = view.querySelector('#chkMissingImdbId');
    chkMissingImdbId.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasImdbId = chkMissingImdbId.checked ? false : null;

        reloadItems(view);
    });

    const chkMissingTmdbId = view.querySelector('#chkMissingTmdbId');
    chkMissingTmdbId.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasTmdbId = chkMissingTmdbId.checked ? false : null;

        reloadItems(view);
    });

    const chkMissingTvdbId = view.querySelector('#chkMissingTvdbId');
    chkMissingTvdbId.addEventListener('change', () => {
        query.StartIndex = 0;
        query.HasTvdbId = chkMissingTvdbId.checked ? false : null;

        reloadItems(view);
    });

    //Episodes
    const chkMissingEpisode = view.querySelector('#chkMissingEpisode');
    chkMissingEpisode.addEventListener('change', () => {
        query.StartIndex = 0;
        query.IsMissing = chkMissingEpisode.checked ? true : false;

        reloadItems(view);
    });

    const chkFutureEpisode = view.querySelector('#chkFutureEpisode');
    chkFutureEpisode.addEventListener('change', () => {
        query.StartIndex = 0;

        if (chkFutureEpisode.checked) {
            query.IsUnaired = true;
            query.IsVirtualUnaired = null;
        } else {
            query.IsUnaired = null;
            query.IsVirtualUnaired = false;
        }

        reloadItems(view);
    });

    const chkSpecialEpisode = view.querySelector('#chkSpecialEpisode');
    chkSpecialEpisode.addEventListener('change', () => {
        query.ParentIndexNumber = chkSpecialEpisode.checked ? 0 : null;

        reloadItems(view);
    });

    for (const elem of view.querySelectorAll('.chkAirDays')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.AirDays || '';

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            reloadItems(view);
        });
    }

    for (const elem of view.querySelectorAll('.chkStatus')) {
        elem.addEventListener('change', function () {
            const filterName = elem.getAttribute('data-filter');
            let filters = query.SeriesStatus || '';

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (elem.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            reloadItems(view);
        });
    }

    view.querySelector('.btnPanelOpen').addEventListener('click', function () {
        const viewPanel = view.querySelector('.viewPanel');
        viewPanel.classList.add('ui-panel-open');
        viewPanel.classList.remove('ui-panel-closed');
        reloadFiltersIfNeeded(view);
    });

    view.querySelector('.btnPanelClose').addEventListener('click', () => {
        const viewPanel = view.querySelector('.viewPanel');
        viewPanel.classList.add('ui-panel-closed');
        viewPanel.classList.remove('ui-panel-open');
    });

    const openTabs = ({ target }) => {
        const { dataset: { tab = '' }} = target;
        view.querySelectorAll('.viewTabButton').forEach(t =>
            t.classList.remove('ui-btn-active')
        );
        target.classList.add('ui-btn-active');
        view.querySelectorAll('.viewTab').forEach(t =>
            t.classList.add('hide')
        );
        view.querySelector(`.${tab}`).classList.remove('hide');
    };

    view.querySelectorAll('.viewTabButton').forEach(tab => {
        tab.addEventListener('click', openTabs);
    });

    view.addEventListener('viewshow', function () {
        query.UserId = Dashboard.getCurrentUserId();
        const page = this;
        query.SortOrder = 'Ascending';

        QueryReportFilters.onPageShow(page, query);
        QueryReportColumns.onPageShow(page, query);
        const selectIncludeItemTypes = page.querySelector('#selectIncludeItemTypes');
        selectIncludeItemTypes.value = query.IncludeItemTypes;
        selectIncludeItemTypes.dispatchEvent(new Event('change'));

        updateFilterControls(page);

        filtersLoaded = false;
        updateFilterControls(this);
    });
}
