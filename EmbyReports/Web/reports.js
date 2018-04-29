define(['jQuery', 'libraryBrowser', 'loading', 'appRouter', 'fnchecked', 'emby-linkbutton', 'paper-icon-button-light', 'jqmwidget', 'detailtablecss'], function ($, libraryBrowser, loading, appRouter) {
    'use strict';

    (function ($, undefined) {
        var props = {
            "animation": {},
            "transition": {}
        },
            testElement = document.createElement("a"),
            vendorPrefixes = ["", "webkit-", "moz-", "o-"];

        $.each(["animation", "transition"], function (i, test) {

            // Get correct name for test
            var testName = (i === 0) ? test + "-" + "name" : test;

            $.each(vendorPrefixes, function (j, prefix) {
                if (testElement.style[$.camelCase(prefix + testName)] !== undefined) {
                    props[test]["prefix"] = prefix;
                    return false;
                }
            });

            // Set event and duration names for later use
            props[test]["duration"] =
                $.camelCase(props[test]["prefix"] + test + "-" + "duration");
            props[test]["event"] =
                $.camelCase(props[test]["prefix"] + test + "-" + "end");

            // All lower case if not a vendor prop
            if (props[test]["prefix"] === "") {
                props[test]["event"] = props[test]["event"].toLowerCase();
            }
        });

        // Remove the testElement
        $(testElement).remove();

        // Animation complete callback
        $.fn.animationComplete = function (callback, type, fallbackTime) {
            var timer, duration,
                that = this,
                eventBinding = function () {

                    // Clear the timer so we don't call callback twice
                    clearTimeout(timer);
                    callback.apply(this, arguments);
                },
                animationType = (!type || type === "animation") ? "animation" : "transition";

            // If a fallback time was not passed set one
            if (fallbackTime === undefined) {

                // Make sure the was not bound to document before checking .css
                if ($(this).context !== document) {

                    // Parse the durration since its in second multiple by 1000 for milliseconds
                    // Multiply by 3 to make sure we give the animation plenty of time.
                    duration = parseFloat(
                        $(this).css(props[animationType].duration)
                    ) * 3000;
                }

                // If we could not read a duration use the default
                if (duration === 0 || duration === undefined || isNaN(duration)) {
                    duration = $.fn.animationComplete.defaultDuration;
                }
            }

            // Sets up the fallback if event never comes
            timer = setTimeout(function () {
                $(that).off(props[animationType].event, eventBinding);
                callback.apply(that);
            }, duration);

            // Bind the event
            return $(this).one(props[animationType].event, eventBinding);
        };

        // Allow default callback to be configured on mobileInit
        $.fn.animationComplete.defaultDuration = 1000;
    })(jQuery);

    (function ($, undefined) {

        $.widget("mobile.panel", {
            options: {
                animate: true,
                theme: null,
                position: "left",
                dismissible: true,
                display: "overlay", //accepts reveal, push, overlay
                swipeClose: true,
                positionFixed: true
            },

            _parentPage: null,
            _page: null,
            _modal: null,
            _panelInner: null,
            _wrapper: null,

            _create: function () {
                var el = this.element,
                    parentPage = el.closest(".ui-page, [data-role='page']");

                // expose some private props to other methods
                $.extend(this, {
                    _parentPage: (parentPage.length > 0) ? parentPage : false,
                    _openedPage: null,
                    _page: this._getPage,
                    _panelInner: this._getPanelInner()
                });
                if (this.options.display !== "overlay") {
                    this._getWrapper();
                }
                this._addPanelClasses();

                // if animating, add the class to do so
                if (!!this.options.animate) {
                    this.element.addClass("ui-panel-animate");
                }
                this.element.addClass("dialog");

                this._bindUpdateLayout();
                this._bindCloseEvents();
                this._bindLinkListeners();
                this._bindPageEvents();

                if (!!this.options.dismissible) {
                    this._createModal();
                }

                this._bindSwipeEvents();
            },

            _getPanelInner: function () {
                var panelInner = this.element[0].querySelector("." + "ui-panel-inner");
                if (!panelInner) {
                    panelInner = this.element.children().wrapAll("<div class='" + "ui-panel-inner" + "' />").parent();
                } else {
                    panelInner = $(panelInner);
                }

                return panelInner;
            },

            _createModal: function () {
                var self = this,
                    target = self._parentPage ? self._parentPage.parent() : self.element.parent();

                self._modal = $("<div class='" + "ui-panel-dismiss" + "'></div>")
                    .on("mousedown", function () {
                        self.close();
                    })
                    .appendTo(target);
            },

            _getPage: function () {
                var page = this._openedPage || this._parentPage || $(".ui-page-active");

                return page;
            },

            _getWrapper: function () {
                var wrapper = this._page().find("." + "ui-panel-wrapper");
                if (wrapper.length === 0) {
                    wrapper = this._page().children(".ui-header:not(.ui-header-fixed), .ui-content:not(.ui-popup), .ui-footer:not(.ui-footer-fixed)")
                        .wrapAll("<div class='" + "ui-panel-wrapper" + "'></div>")
                        .parent();
                }

                this._wrapper = wrapper;
            },

            _getPosDisplayClasses: function (prefix) {
                return prefix + "-position-right " + prefix + "-display-" + this.options.display;
            },

            _getPanelClasses: function () {
                var panelClasses = "ui-panel" +
                    " " + this._getPosDisplayClasses("ui-panel") +
                    " " + "ui-panel-closed" +
                    " " + "ui-body-" + (this.options.theme ? this.options.theme : "inherit");

                if (!!this.options.positionFixed) {
                    panelClasses += " " + "ui-panel-fixed";
                }

                return panelClasses;
            },

            _addPanelClasses: function () {
                this.element.addClass(this._getPanelClasses());
            },

            _handleCloseClick: function (event) {
                if (!event.isDefaultPrevented()) {
                    this.close();
                }
            },

            _bindCloseEvents: function () {
            },

            _positionPanel: function (scrollToTop) {
                var self = this,
                    panelInnerHeight = self._panelInner.outerHeight(),
                    expand = panelInnerHeight > (window.innerHeight || $(window).height());

                if (expand || !self.options.positionFixed) {
                    if (expand) {
                        self._unfixPanel();
                    }
                    if (scrollToTop) {
                        this.window[0].scrollTo(0, $.mobile.defaultHomeScroll);
                    }
                } else {
                    self._fixPanel();
                }
            },

            _bindFixListener: function () {
                this._on($(window), { "resize": "_positionPanel" });
            },

            _unbindFixListener: function () {
                this._off($(window), "resize");
            },

            _unfixPanel: function () {
                if (!!this.options.positionFixed) {
                    this.element.removeClass("ui-panel-fixed");
                }
            },

            _fixPanel: function () {
                if (!!this.options.positionFixed) {
                    this.element.addClass("ui-panel-fixed");
                }
            },

            _bindUpdateLayout: function () {
                var self = this;

                self.element.on("updatelayout", function (/* e */) {
                    if (self._open) {
                        self._positionPanel();
                    }
                });
            },

            _bindLinkListeners: function () {
                this._on("body", {
                    "click a": "_handleClick"
                });

            },

            _handleClick: function (e) {
                var link,
                    panelId = this.element.attr("id");

                if (e.currentTarget.href.split("#")[1] === panelId && panelId !== undefined) {

                    e.preventDefault();
                    link = $(e.target);
                    if (link.hasClass("ui-btn")) {
                        link.addClass($.mobile.activeBtnClass);
                        this.element.one("panelopen panelclose", function () {
                            link.removeClass($.mobile.activeBtnClass);
                        });
                    }
                    this.toggle();
                }
            },

            _bindSwipeEvents: function () {
                var self = this,
                    area = self._modal ? self.element.add(self._modal) : self.element;

                // on swipe, close the panel
                if (!!self.options.swipeClose) {
                    if (self.options.position === "left") {
                        area.on("swipeleft.panel", function (/* e */) {
                            self.close();
                        });
                    } else {
                        area.on("swiperight.panel", function (/* e */) {
                            self.close();
                        });
                    }
                }
            },

            _bindPageEvents: function () {
                var self = this;

                this.document
                    // Close the panel if another panel on the page opens
                    .on("panelbeforeopen", function (e) {
                        if (self._open && e.target !== self.element[0]) {
                            self.close();
                        }
                    })
                    // On escape, close? might need to have a target check too...
                    .on("keyup.panel", function (e) {
                        if (e.keyCode === 27 && self._open) {
                            self.close();
                        }
                    });
                if (!this._parentPage && this.options.display !== "overlay") {
                    this._on(this.document, {
                        "pageshow": function () {
                            this._openedPage = null;
                            this._getWrapper();
                        }
                    });
                }
                // Clean up open panels after page hide
                if (self._parentPage) {
                    this.document.on("pagehide", "[data-role='page']", function () {
                        if (self._open) {
                            self.close(true);
                        }
                    });
                } else {
                    this.document.on("pagebeforehide", function () {
                        if (self._open) {
                            self.close(true);
                        }
                    });
                }
            },

            // state storage of open or closed
            _open: false,
            _pageContentOpenClasses: null,
            _modalOpenClasses: null,

            open: function (immediate) {
                if (!this._open) {
                    var self = this,
                        o = self.options,

                        _openPanel = function () {
                            self._off(self.document, "panelclose");
                            self._page().data("panel", "open");

                            if (!!o.animate && o.display !== "overlay") {
                                self._wrapper.addClass("ui-panel-animate");
                            }

                            if (!immediate && !!o.animate) {
                                (self._wrapper || self.element)
                                    .animationComplete(complete, "transition");
                            } else {
                                setTimeout(complete, 0);
                            }

                            if (o.theme && o.display !== "overlay") {
                                self._page().parent()
                                    .addClass("ui-panel-page-container" + "-themed " + "ui-panel-page-container" + "-" + o.theme);
                            }

                            self.element
                                .removeClass("ui-panel-closed")
                                .addClass("ui-panel-open");

                            self._positionPanel(true);

                            self._pageContentOpenClasses = self._getPosDisplayClasses("ui-panel-page-content");

                            if (o.display !== "overlay") {
                                self._page().parent().addClass("ui-panel-page-container");
                                self._wrapper.addClass(self._pageContentOpenClasses);
                            }

                            self._modalOpenClasses = self._getPosDisplayClasses("ui-panel-dismiss") + " " + "ui-panel-dismiss-open";
                            if (self._modal) {
                                self._modal
                                    .addClass(self._modalOpenClasses)
                                    .height(Math.max(self._modal.height(), self.document.height()));
                            }
                        },
                        complete = function () {

                            // Bail if the panel was closed before the opening animation has completed
                            if (!self._open) {
                                return;
                            }

                            if (o.display !== "overlay") {
                                self._wrapper.addClass("ui-panel-page-content" + "-open");
                            }

                            self._bindFixListener();

                            self._trigger("open");

                            self._openedPage = self._page();
                        };

                    self._trigger("beforeopen");

                    if (self._page().data("panel") === "open") {
                        self._on(self.document, {
                            "panelclose": _openPanel
                        });
                    } else {
                        _openPanel();
                    }

                    self._open = true;
                }
            },

            close: function (immediate) {
                if (this._open) {
                    var self = this,
                        o = this.options,

                        _closePanel = function () {

                            self.element.removeClass("ui-panel-open");

                            if (o.display !== "overlay") {
                                self._wrapper.removeClass(self._pageContentOpenClasses);
                            }

                            if (!immediate && !!o.animate) {
                                (self._wrapper || self.element)
                                    .animationComplete(complete, "transition");
                            } else {
                                setTimeout(complete, 0);
                            }

                            if (self._modal) {
                                self._modal
                                    .removeClass(self._modalOpenClasses)
                                    .height("");
                            }
                        },
                        complete = function () {
                            if (o.theme && o.display !== "overlay") {
                                self._page().parent().removeClass("ui-panel-page-container" + "-themed " + "ui-panel-page-container" + "-" + o.theme);
                            }

                            self.element.addClass("ui-panel-closed");

                            if (o.display !== "overlay") {
                                self._page().parent().removeClass("ui-panel-page-container");
                                self._wrapper.removeClass("ui-panel-page-content" + "-open");
                            }

                            if (!!o.animate && o.display !== "overlay") {
                                self._wrapper.removeClass("ui-panel-animate");
                            }

                            self._fixPanel();
                            self._unbindFixListener();

                            self._page().removeData("panel");

                            self._trigger("close");

                            self._openedPage = null;
                        };

                    self._trigger("beforeclose");

                    _closePanel();

                    self._open = false;
                }
            },

            toggle: function () {
                this[this._open ? "close" : "open"]();
            },

            _destroy: function () {
                var otherPanels,
                    o = this.options,
                    multiplePanels = ($("body > :mobile-panel").length + $.mobile.activePage.find(":mobile-panel").length) > 1;

                if (o.display !== "overlay") {

                    //  remove the wrapper if not in use by another panel
                    otherPanels = $("body > :mobile-panel").add($.mobile.activePage.find(":mobile-panel"));
                    if (otherPanels.not(".ui-panel-display-overlay").not(this.element).length === 0) {
                        this._wrapper.children().unwrap();
                    }

                    if (this._open) {

                        this._page().parent().removeClass("ui-panel-page-container");

                        if (o.theme) {
                            this._page().parent().removeClass("ui-panel-page-container" + "-themed " + "ui-panel-page-container" + "-" + o.theme);
                        }
                    }
                }

                if (!multiplePanels) {

                    this.document.off("panelopen panelclose");

                }

                if (this._open) {
                    this._page().removeData("panel");
                }

                this._panelInner.children().unwrap();

                this.element
                    .removeClass([this._getPanelClasses(), "ui-panel-open", "ui-panel-animate"].join(" "))
                    .off("swipeleft.panel swiperight.panel")
                    .off("panelbeforeopen")
                    .off("panelhide")
                    .off("keyup.panel")
                    .off("updatelayout");

                if (this._modal) {
                    this._modal.remove();
                }
            }
        });

    })(jQuery);

    var defaultSortBy = "SortName";
    var topItems = 5;

    var query = {
        StartIndex: 0,
        Limit: 100,
        IncludeItemTypes: "Movie",
        HasQueryLimit: true,
        GroupBy: "None",
        ReportView: "ReportData",
        DisplayType: "Screen",
    };

    function getTable(result) {
        var html = '';
        //Report table
        html += '<table id="tblReport" data-role="table" data-mode="reflow" class="tblLibraryReport stripedTable ui-responsive table-stroke detailTable" style="display:table;">';
        html += '<thead>';

        //Report headers
        result.Headers.map(function (header) {
            var cellHtml = '<th class="detailTableHeaderCell" data-priority="' + 'persist' + '">';

            if (header.ShowHeaderLabel) {
                if (header.SortField) {
                    cellHtml += '<a class="lnkColumnSort button-link" is="emby-linkbutton" href="#" data-sortfield="' + header.SortField + '" style="text-decoration:underline;">';
                }

                cellHtml += (header.Name || '&nbsp;');
                if (header.SortField) {
                    cellHtml += '</a>';
                    if (header.SortField === defaultSortBy) {

                        if (query.SortOrder === "Descending") {
                            cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;">&darr;</span>';
                        } else {
                            cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;">&uarr;</span>';
                        }
                    }
                }
            }
            cellHtml += '</th>';
            html += cellHtml;
        });

        html += '</thead>';
        //Report body
        html += '<tbody>';
        if (result.IsGrouped === false) {

            result.Rows.map(function (row) {
                html += getRow(result.Headers, row);
            });
        }
        else {

            result.Groups.map(function (group) {
                html += '<tr style="background-color: rgb(51, 51, 51);">';
                html += '<th class="detailTableHeaderCell" scope="rowgroup" colspan="' + result.Headers.length + '">' + (group.Name || '&nbsp;') + '</th>';
                html += '</tr>';
                group.Rows.map(function (row) {
                    html += getRow(result.Headers, row);
                });
                html += '<tr>';
                html += '<th class="detailTableHeaderCell" scope="rowgroup" colspan="' + result.Headers.length + '">' + '&nbsp;' + '</th>';
                html += '</tr>';
            });
        }

        html += '</tbody>';
        html += '</table>';
        return html;
    }

    function getRow(rHeaders, rRow) {
        var html = '';
        html += '<tr class="detailTableBodyRow detailTableBodyRow-shaded">';

        for (var j = 0; j < rHeaders.length; j++) {
            var rHeader = rHeaders[j];
            var rItem = rRow.Columns[j];
            html += getItem(rHeader, rRow, rItem);
        }
        html += '</tr>';
        return html;
    }

    function getItem(rHeader, rRow, rItem) {
        var html = '';
        html += '<td class="detailTableBodyCell">';
        var id = rRow.Id;
        if (rItem.Id)
            id = rItem.Id;
        var serverId = rRow.ServerId || rItem.ServerId || ApiClient.serverId();

        switch (rHeader.ItemViewType) {
            case "None":
                html += rItem.Name;
                break;
            case "Detail":
                html += '<a is="emby-linkbutton" class="button-link" href="' + appRouter.getRouteUrl({ Id: id, ServerId: serverId }) + '">' + rItem.Name + '</a>';
                break;
            case "Edit":
                html += '<a is="emby-linkbutton" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '">' + rItem.Name + '</a>';
                break;
            case "List":
                html += '<a is="emby-linkbutton" class="button-link" href="itemlist.html?serverId=' + rItem.ServerId + '&id=' + rRow.Id + '">' + rItem.Name + '</a>';
                break;
            case "ItemByNameDetails":
                html += '<a is="emby-linkbutton" class="button-link" href="' + appRouter.getRouteUrl({ Id: id, ServerId: serverId }) + '">' + rItem.Name + '</a>';
                break;
            case "EmbeddedImage":
                if (rRow.HasEmbeddedImage) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "SubtitleImage":
                if (rRow.HasSubtitles) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "TrailersImage":
                if (rRow.HasLocalTrailer) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "SpecialsImage":
                if (rRow.HasSpecials) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "LockDataImage":
                if (rRow.HasLockData) {
                    html += '<i class="md-icon">lock</i>';
                }
                break;
            case "TagsPrimaryImage":
                if (!rRow.HasImageTagsPrimary) {
                    html += '<a is="emby-linkbutton" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '"><img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." style="width:18px"/></a>';
                }
                break;
            case "TagsBackdropImage":
                if (!rRow.HasImageTagsBackdrop) {
                    if (rRow.RowType !== "Episode" && rRow.RowType !== "Season" && rRow.MediaType !== "Audio" && rRow.RowType !== "TvChannel" && rRow.RowType !== "MusicAlbum") {
                        html += '<a is="emby-linkbutton" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '"><img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." style="width:18px"/></a>';
                    }
                }
                break;
            case "TagsLogoImage":
                if (!rRow.HasImageTagsLogo) {
                    if (rRow.RowType === "Movie" || rRow.RowType === "Trailer" || rRow.RowType === "Series" || rRow.RowType === "MusicArtist" || rRow.RowType === "BoxSet") {
                        html += '<a is="emby-linkbutton" class="button-link" href="edititemmetadata.html?id=' + rRow.Id + '"><img src="css/images/editor/missinglogo.png" title="Missing logo image." style="width:18px"/></a>';
                    }
                }
                break;
            case "UserPrimaryImage":
                if (rRow.UserId) {
                    var userImage = ApiClient.getUserImageUrl(rRow.UserId, {
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
            case "StatusImage":
                if (rRow.HasLockData) {
                    html += '<i class="md-icon">lock</i>';
                }

                if (!rRow.HasLocalTrailer && rRow.RowType === "Movie") {
                    html += '<i title="Missing local trailer." class="md-icon">videocam</i>';
                }

                if (!rRow.HasImageTagsPrimary) {
                    html += '<img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." style="width:18px"/>';
                }

                if (!rRow.HasImageTagsBackdrop) {
                    if (rRow.RowType !== "Episode" && rRow.RowType !== "Season" && rRow.MediaType !== "Audio" && rRow.RowType !== "TvChannel" && rRow.RowType !== "MusicAlbum") {
                        html += '<img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." style="width:18px"/>';
                    }
                }

                if (!rRow.HasImageTagsLogo) {
                    if (rRow.RowType === "Movie" || rRow.RowType === "Trailer" || rRow.RowType === "Series" || rRow.RowType === "MusicArtist" || rRow.RowType === "BoxSet") {
                        html += '<img src="css/images/editor/missinglogo.png" title="Missing logo image." style="width:18px"/>';
                    }
                }
                break;
            default:
                html += rItem.Name;
        }
        html += '</td>';
        return html;
    }

    function ExportReport(page, e) {

        query.UserId = Dashboard.getCurrentUserId();
        query.HasQueryLimit = false;
        var url = ApiClient.getUrl("Reports/Items/Download", query);

        if (url) {
            window.location.href = url;
        }
    }

    function loadGroupByFilters(page) {

        query.UserId = Dashboard.getCurrentUserId();
        var url = "";

        url = ApiClient.getUrl("Reports/Headers", query);
        ApiClient.getJSON(url).then(function (result) {
            var selected = "None";

            $('#selectReportGroup', page).find('option').remove().end();
            $('#selectReportGroup', page).append('<option value="None"></option>');

            result.map(function (header) {
                if ((header.DisplayType === "Screen" || header.DisplayType === "ScreenExport") && header.CanGroup) {
                    if (header.FieldName.length > 0) {
                        var option = '<option value="' + header.FieldName + '">' + header.Name + '</option>';
                        $('#selectReportGroup', page).append(option);
                        if (query.GroupBy === header.FieldName)
                            selected = header.FieldName;
                    }
                }
            });
            $('#selectPageSize', page).val(selected);

        });
    }

    function renderItems(page, result) {

        window.scrollTo(0, 0);
        var html = '';

        if (query.ReportView === "ReportData") {
            $('#selectIncludeItemTypesBox', page).show();
            $('#tabFilter', page).show();
        }
        else {
            $('#selectIncludeItemTypesBox', page).hide();
            $('#tabFilterBox', page).hide();
            $('#tabFilter', page).hide();
        }

        var pagingHtml = libraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            updatePageSizeSetting: false,
            viewButton: true,
            showLimit: false
        });

        if (query.ReportView === "ReportData" || query.ReportView === "ReportActivities") {


            $('.listTopPaging', page).html(pagingHtml).trigger('create');
            // page.querySelector('.listTopPaging').innerHTML = pagingHtml;
            $('.listTopPaging', page).show();

            $('.listBottomPaging', page).html(pagingHtml).trigger('create');
            $('.listBottomPaging', page).show();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });
            $('.btnNextPage', page).show();

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });
            $('.btnPreviousPage', page).show();

            $('#btnReportExport', page).show();
            $('#selectPageSizeBox', page).show();
            $('#selectReportGroupingBox', page).show();
            $('#grpReportsColumns', page).show();

            html += getTable(result);

            $('.reporContainer', page).html(html).trigger('create');

            $('.lnkColumnSort', page).on('click', function () {

                var order = this.getAttribute('data-sortfield');

                if (query.SortBy === order) {

                    if (query.SortOrder === "Descending") {

                        query.SortOrder = "Ascending";
                        query.SortBy = defaultSortBy;

                    } else {

                        query.SortOrder = "Descending";
                        query.SortBy = order;
                    }

                } else {

                    query.SortOrder = "Ascending";
                    query.SortBy = order;
                }

                query.StartIndex = 0;

                reloadItems(page);
            });
        }

        $('#GroupStatus', page).hide();
        $('#GroupAirDays', page).hide();
        $('#GroupEpisodes', page).hide();
        switch (query.IncludeItemTypes) {
            case "Series":
            case "Season":
                $('#GroupStatus', page).show();
                $('#GroupAirDays', page).show();
                break;
            case "Episode":
                $('#GroupStatus', page).show();
                $('#GroupAirDays', page).show();
                $('#GroupEpisodes', page).show();
                break;
        }
        $('.viewPanel', page).refresh;
    }

    function reloadItems(page) {
        loading.show();

        query.UserId = Dashboard.getCurrentUserId();
        var url = "";

        switch (query.ReportView) {
            case "ReportData":
                query.HasQueryLimit = true;
                url = ApiClient.getUrl("Reports/Items", query);
                break;
            case "ReportActivities":
                query.HasQueryLimit = true;
                url = ApiClient.getUrl("Reports/Activities", query);
                break;
        }

        ApiClient.getJSON(url).then(function (result) {
            updateFilterControls(page);
            renderItems(page, result);
        });


        loading.hide();
    }

    function updateFilterControls(page) {
        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });


        $('.chkVideoTypeFilter', page).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.chkStatus', page).each(function () {

            var filters = "," + (query.SeriesStatus || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.chkAirDays', page).each(function () {

            var filters = "," + (query.AirDays || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('#chk3D', page).checked(query.Is3D == true);
        $('#chkHD', page).checked(query.IsHD == true);
        $('#chkSD', page).checked(query.IsHD == false);

        $('#chkSubtitle', page).checked(query.HasSubtitles == true);
        $('#chkTrailer', page).checked(query.HasTrailer == true);
        $('#chkMissingTrailer', page).checked(query.HasTrailer == false);
        $('#chkSpecialFeature', page).checked(query.HasSpecialFeature == true);
        $('#chkThemeSong', page).checked(query.HasThemeSong == true);
        $('#chkThemeVideo', page).checked(query.HasThemeVideo == true);

        $('#selectPageSize', page).val(query.Limit);

        //Management
        $('#chkMissingRating', page).checked(query.HasOfficialRating == false);
        $('#chkMissingOverview', page).checked(query.HasOverview == false);
        $('#chkIsLocked', page).checked(query.IsLocked == true);
        $('#chkMissingImdbId', page).checked(query.HasImdbId == false);
        $('#chkMissingTmdbId', page).checked(query.HasTmdbId == false);
        $('#chkMissingTvdbId', page).checked(query.HasTvdbId == false);

        //Episodes
        $('#chkSpecialEpisode', page).checked(query.ParentIndexNumber == 0);
        $('#chkMissingEpisode', page).checked(query.IsMissing == true);
        $('#chkFutureEpisode', page).checked(query.IsUnaired == true);

        $('#selectIncludeItemTypes').val(query.IncludeItemTypes);

        // isfavorite
        if (query.IsFavorite == true) {
            $('#isFavorite').val("true");
        }
        else if (query.IsFavorite == false) {
            $('#isFavorite').val("false");
        }
        else {
            $('#isFavorite').val("-");
        }


    }

    var filtersLoaded;
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

    function renderOptions(page, selector, cssClass, items) {

        var elem;

        if (items.length) {

            elem = $(selector, page).show();

        } else {
            elem = $(selector, page).hide();
        }

        var html = '';

        //  style="margin: -.2em -.8em;"
        html += '<div data-role="controlgroup">';

        var index = 0;
        var idPrefix = 'chk' + selector.substring(1);

        html += items.map(function (filter) {

            var itemHtml = '';

            var id = idPrefix + index;
            var label = filter;
            var value = filter;
            var checked = false;
            if (filter.FieldName) {
                label = filter.Name;
                value = filter.FieldName;
                checked = filter.Visible;
            }
            itemHtml += '<label for="' + id + '">' + label + '</label>';
            itemHtml += '<input id="' + id + '" type="checkbox" data-filter="' + value + '" class="' + cssClass + '"';
            if (checked)
                itemHtml += ' checked="checked" ';
            itemHtml += '/>';

            index++;

            return itemHtml;

        }).join('');

        html += '</div>';

        $('.filterOptions', elem).html(html).trigger('create');
    }

    function renderFilters(page, result) {


        if (result.Tags) {
            result.Tags.length = Math.min(result.Tags.length, 50);
        }

        renderOptions(page, '.genreFilters', 'chkGenreFilter', result.Genres);
        renderOptions(page, '.officialRatingFilters', 'chkOfficialRatingFilter', result.OfficialRatings);
        renderOptions(page, '.tagFilters', 'chkTagFilter', result.Tags);
        renderOptions(page, '.yearFilters', 'chkYearFilter', result.Years);

    }

    function renderColumnss(page, result) {


        if (result.Tags) {
            result.Tags.length = Math.min(result.Tags.length, 50);
        }

        renderOptions(page, '.reportsColumns', 'chkReportColumns', result);
    }

    function onFiltersLoaded(page, query, reloadItemsFn) {

        $('.chkGenreFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Genres || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Genres = filters;

            reloadItemsFn();
        });
        $('.chkTagFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Tags || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Tags = filters;

            reloadItemsFn();
        });
        $('.chkYearFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Years || "";
            var delimiter = ',';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Years = filters;

            reloadItemsFn();
        });
        $('.chkOfficialRatingFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.OfficialRatings || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.OfficialRatings = filters;

            reloadItemsFn();
        });
    }

    function onColumnsLoaded(page, query, reloadItemsFn) {

        $('.chkReportColumns', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.ReportColumns || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.ReportColumns = filters;

            reloadItemsFn();
        });
    }

    function loadFilters(page, userId, itemQuery, reloadItemsFn) {

        return ApiClient.getJSON(ApiClient.getUrl('Items/Filters', {

            UserId: userId,
            ParentId: itemQuery.ParentId,
            IncludeItemTypes: itemQuery.IncludeItemTypes,
            ReportView: itemQuery.ReportView


        })).then(function (result) {

            renderFilters(page, result);

            onFiltersLoaded(page, itemQuery, reloadItemsFn);
        });
    }

    function loadColumns(page, userId, itemQuery, reloadItemsFn) {

        return ApiClient.getJSON(ApiClient.getUrl('Reports/Headers', {

            UserId: userId,
            IncludeItemTypes: itemQuery.IncludeItemTypes,
            ReportView: itemQuery.ReportView

        })).then(function (result) {

            renderColumnss(page, result);
            var filters = "";
            var delimiter = '|';
            result.map(function (item) {

                if ((item.DisplayType === "Screen" || item.DisplayType === "ScreenExport"))
                    filters = filters ? (filters + delimiter + item.FieldName) : item.FieldName;
            });
            if (!itemQuery.ReportColumns)
                itemQuery.ReportColumns = filters;
            onColumnsLoaded(page, itemQuery, reloadItemsFn);
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

    return function (page, params) {

        $(page).trigger('create');

        $('#selectIncludeItemTypes', page).on('change', function () {

            query.StartIndex = 0;
            query.ReportView = $('#selectViewType', page).val();
            query.IncludeItemTypes = this.value;
            query.SortOrder = "Ascending";
            query.ReportColumns = null;
            $('.btnReportExport', page).hide();
            filtersLoaded = false;
            loadGroupByFilters(page);
            reloadFiltersIfNeeded(page);
            reloadItems(page);
        });

        $('#selectViewType', page).on('change', function () {

            query.StartIndex = 0;
            query.ReportView = this.value;
            query.IncludeItemTypes = $('#selectIncludeItemTypes', page).val();
            query.SortOrder = "Ascending";
            filtersLoaded = false;
            query.ReportColumns = null;
            loadGroupByFilters(page);
            reloadFiltersIfNeeded(page);
            reloadItems(page);
        });

        $('#selectReportGroup', page).on('change', function () {
            query.GroupBy = this.value;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('#btnReportExportCsv', page).on('click', function (e) {

            query.ExportType = "CSV";
            ExportReport(page, e);
        });

        $('#btnReportExportExcel', page).on('click', function (e) {

            query.ExportType = "Excel";
            ExportReport(page, e);
        });

        $('#btnResetReportColumns', page).on('click', function (e) {

            query.ReportColumns = null;
            query.StartIndex = 0;
            filtersLoaded = false;
            reloadFiltersIfNeeded(page);
            reloadItems(page);
        });

        $('.viewPanel', page).on('panelopen', function () {
            reloadFiltersIfNeeded(page);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('#isFavorite', page).on('change', function () {

            if (this.value == "true") {
                query.IsFavorite = true;
            }
            else if (this.value == "false") {
                query.IsFavorite = false;
            }
            else {
                query.IsFavorite = null;
            }
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkStandardFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(page);
        });

        $('.chkVideoTypeFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(page);
        });

        $('#chk3D', page).on('change', function () {

            query.StartIndex = 0;
            query.Is3D = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkHD', page).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkSD', page).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkSubtitle', page).on('change', function () {

            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkTrailer', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkMissingTrailer', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkSpecialFeature', page).on('change', function () {

            query.StartIndex = 0;
            query.HasSpecialFeature = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeSong', page).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeVideo', page).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            reloadItems(page);
        });

        $('#radioBasicFilters', page).on('change', function () {

            if (this.checked) {
                $('.basicFilters', page).show();
                $('.advancedFilters', page).hide();
            } else {
                $('.basicFilters', page).hide();
            }
        });

        $('#radioAdvancedFilters', page).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', page).show();
                $('.basicFilters', page).hide();
            } else {
                $('.advancedFilters', page).hide();
            }
        });

        //Management
        $('#chkIsLocked', page).on('change', function () {

            query.StartIndex = 0;
            query.IsLocked = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkMissingOverview', page).on('change', function () {

            query.StartIndex = 0;
            query.HasOverview = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingEpisode', page).on('change', function () {

            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            reloadItems(page);
        });

        $('#chkMissingRating', page).on('change', function () {

            query.StartIndex = 0;
            query.HasOfficialRating = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingImdbId', page).on('change', function () {

            query.StartIndex = 0;
            query.HasImdbId = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingTmdbId', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTmdbId = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingTvdbId', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTvdbId = this.checked ? false : null;

            reloadItems(page);
        });

        //Episodes
        $('#chkMissingEpisode', page).on('change', function () {

            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            reloadItems(page);
        });

        $('#chkFutureEpisode', page).on('change', function () {

            query.StartIndex = 0;

            if (this.checked) {
                query.IsUnaired = true;
                query.IsVirtualUnaired = null;
            } else {
                query.IsUnaired = null;
                query.IsVirtualUnaired = false;
            }


            reloadItems(page);
        });

        $('#chkSpecialEpisode', page).on('change', function () {

            query.ParentIndexNumber = this.checked ? 0 : null;

            reloadItems(page);
        });

        $('.chkAirDays', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.AirDays || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkStatus', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.SeriesStatus || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $(page.getElementsByClassName('viewTabButton')).on('click', function () {

            var parent = $(this).parents('.viewPanel');
            $('.viewTabButton', parent).removeClass('ui-btn-active');
            this.classList.add('ui-btn-active');

            $('.viewTab', parent).addClass('hide');
            $('.' + this.getAttribute('data-tab'), parent).removeClass('hide');
        });

        page.addEventListener('viewshow', function () {

            query.UserId = Dashboard.getCurrentUserId();
            var page = this;
            query.SortOrder = "Ascending";

            QueryReportFilters.onPageShow(page, query);
            QueryReportColumns.onPageShow(page, query);
            $('#selectIncludeItemTypes', page).val(query.IncludeItemTypes).trigger('change');

            updateFilterControls(page);

            filtersLoaded = false;
            updateFilterControls(this);
        });
    };
});