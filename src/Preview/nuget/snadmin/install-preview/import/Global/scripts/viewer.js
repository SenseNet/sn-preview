// using $skin/scripts/plugins/iscroll.js
// using $skin/scripts/jquery/plugins/jquery.ba-throttle-debounce.min.js
// using $skin/scripts/plugins/docviewer-2.0.js
// using $skin/scripts/plugins/excanvas.compiled.js
// using $skin/styles/plugins/snDocViewer.css

(function ($) {
    $.fn.extend({
        viewer: function (options) {
            // Prevent the plugin from running twice
            if (this.data('viewer'))
                return;

            var $pluginSubject = $(this[0]);

            var doctype = options.docType, createdby = options.createdby, modifiedby = options.modifiedby, ClientID = options.ClientId;
            var previewFolder = options.previews; 
            var filePath = options.filePath; 
            var fileVersion = options.fileVersion; 
            var actionArray;
            var isAdmin = options.canSave ? true : false;
            var noWatermark = options.canPreviewWithoutWatermark ? true : false; 
            var noRedaction = options.canPreviewWithoutRedaction ? true : false; 
            var shapes = options.shapes;
            var parentPath = document.URL;
            if (parentPath.indexOf('&back') > -1) {
                var parentPathArray = parentPath.split('back=');
                parentPathArray = parentPathArray[1].split('&');
                parentPath = decodeURIComponent(decodeURIComponent(parentPathArray[0]));
            }
            else {
                parentPath = SN.Context.currentContent.path.substr(SN.Context.currentContent.path.lastIndexOf('/') + 1) + '$';
            }

            var content = SN.Context.currentContent.name;
            var previewCount = 0;
            var wm = false;
            var loadingString = options.loadingString;
            var fileType = options.ext;
            fileType = fileType.replace('.', '');
            var fileSize = options.IsHeadOnly || options.IsPreviewOnly ? 0 : options.Size;
            var requests = [];
            var tempRequests = [];

            var mdate = options.mdate;
            var cdate = options.cdtae;

            modDate = SN.Util.setFriendlyLocalDateFromValue(mdate, options.CurrentCulture);
            creDate = SN.Util.setFriendlyLocalDateFromValue(cdate, options.CurrentCulture);

            var getPageCount = $.ajax({
                url: odata.dataRoot + odata.getItemUrl(filePath) + '/GetPageCount' + '?nocache=' + new Date().getTime(),
                dataType: "json",
                type: "POST",
                success: function (d) {
                    previewCount = d;
                }
            });


            function getFileSize(fileSize) {
                var i = -1;
                var byteUnits = ['kB', 'MB', 'GB'];
                do {
                    fileSize = fileSize / 1024;
                    i++;
                } while (fileSize > 1024);
                return Math.max(fileSize, 0.1).toFixed(1) + " " + byteUnits[i];
            }

            var touch = false;
            if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|BB10/i.test(navigator.userAgent))
                touch = true;

            var currentuser = odata.getItemUrl(SN.Context.currentUser.path);

            var noPreviewText = "No preview!";


            // Fetch the related reachable actions of the document
            var actionsReq = $.ajax({
                url: "/OData.svc" + odata.getItemUrl(filePath) + "/Actions?scenario=DocumentDetails&back=" + encodeURIComponent(document.URL)
            }).done(function (data) {
                actionArray = data.d.Actions;
            });

            // After the above AJAX requests complete
            //        $.when(previewsReq).done(function() {
            // Initialize the document viewer
            var $dv = $("#" + ClientID);
            var pcpromise, promisePC;
            $.when(getPageCount).done(function () {
                if (previewCount === 0) {
                    var errorString = options.emptyDocumentMessage;
                    var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                    $('.sn-docviewer-actions').after(errorDiv);
                }
                else if (previewCount === -2) {
                    var errorString = options.extensionFailureMessage;
                    var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                    $('.sn-docviewer-actions').after(errorDiv);
                }
                else if (previewCount === -3) {
                    var errorString = options.uploadFailureMessage;
                    var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                    $('.sn-docviewer-actions').after(errorDiv);
                }
                else if (previewCount === -4) {
                    var errorString = options.uploadFailureMessage;
                    var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                    $('.sn-docviewer-actions').after(errorDiv);
                }
                else if (previewCount === -5) {
                    var errorString = options.noPreviewProviderEnabledMessage;
                    var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                    $('.sn-docviewer-actions').after(errorDiv);
                }
                else if (previewCount === -1) {
                    var loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" />' + options.displayName + '<span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div></div></div>';
                    if (isAdmin)
                        loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" /><a href="' + filePath + '">' + options.displayName + '</a><span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div><a href="' + filePath + '"><span class="load-button">Download</span></a></div></div>';
                    $('.sn-docviewer-actions').after(loadingDiv);
                    pcpromise = new $.Deferred();
                    var path = "/OData.svc" + odata.getItemUrl(filePath) + "?$select=PageCount&metadata=no";

                    //getPageCount().done(function (data) {
                    //    pcpromise.resolve(data);
                    //    var pc = data.d.PageCount;
                    //    getExistingPreviewImages();
                    //    var c = 5;
                    //    if(previewCount < 5 && previewCount > 0)
                    //        c = previewCount;

                    //    getImage(c).done(function (data) {
                    //        $('.preview-loader').remove();
                    //        docViewerInit(pc);
                    //    });
                    //});

                    var getPrevs = getPC(path);
                    getPrevs.done(function (data) {
                        pcpromise.resolve(data);
                        previewCount = data.d.PageCount;

                        var c = 5;
                        if (previewCount < 5 && previewCount > 0)
                            c = previewCount;

                        getImage(c).done(function (data) {
                            $('.preview-loader').remove();
                            docViewerInit(previewCount);
                        });
                    });
                }
                else {
                    var loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" />' + options.displayName + '<span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div></div></div>';
                    if (isAdmin)
                        loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" /><a href="' + filePath + '">' + options.displayName + '</a><span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div><a href="' + filePath + '"><span class="load-button">Download</span></a></div></div>';
                    $('.sn-docviewer-actions').after(loadingDiv);
                    var c = 5;
                    if (previewCount < 5)
                        c = previewCount;

                    getImage(c).done(function (data) {
                        $('.preview-loader').remove();
                        docViewerInit(previewCount);
                    });
                }
            });
            function docViewerInit(previewCount) {

                var docViewer = $dv.documentViewer({
                    getImage: getImage,
                    getThumbnail: getThumbnail,
                    getExistingPreviewImages: getExistingPreviewImages,
                    showthumbnails: true,                           //show thumbnails, default false
                    metadata: true,                                 //show metadata, default false
                    showtoolbar: true,                              //show toolbar, default false
                    edittoolbar: true,                              //show shape edit toolbar, default false (its not visible if showtoolbar is false)
                    title: options.displayName,                     //content title, default empty string
                    containerWidth: function () {
                        var wi = $(window).width();
                        var h = $(window).height();

                        // Desktop
                        if (!touch) {
                            if (wi <= 1030) {
                                return $dv.width() * 0.70;
                            }
                            else {
                                return $dv.width() * 0.75;
                            }
                        }
                        else {
                            return $dv.width();
                        }
                    }, //container width
                    containerHeight: function () {
                        var wh = $(window).height();
                        return wh - 275;
                    }, //container height
                    reactToResize: true,                                                                   //react to resize or not
                    metadataHtml: '\
                                <ul class="docinfo">\
                                    <li><label>Document type:</label> <span>' + doctype + '</span></li>\
                                    <li><label>Created by:</label>' + createdby + '</span></li>\
                                    <li><label>Creation date:</label> <span>' + creDate + '</span></li>\
                                    <li><label>Modified by:</label> <span>' + modifiedby + '</span></li>\
                                    <li><label>Last modified:</label> <span>' + modDate + '</span></li>\
                                </ul>', //content metadata, feel free to add more
                    isAdmin: isAdmin,                               //current users save permission, default false
                    noWatermark: noWatermark,                       //current users noWatermark permission, default false
                    noRedaction: noRedaction,                       //current users noRedaction permission, default false  
                    showShapes: true,                               //shapes are showing by default, default true
                    shapes: options.shapes,             //shapes json, default empty string
                    pageAttributes: options.pageAttributes,             //shapes json, default empty string
                    SR: {
                        toolbarNotes: SN.Resources.DocViewer["DocViewer-toolbarNotes"],
                        toolbarHighlight: SN.Resources.DocViewer["DocViewer-toolbarHighlight"],
                        toolbarRedaction: SN.Resources.DocViewer["DocViewer-toolbarRedaction"],
                        toolbarFirstPage: SN.Resources.DocViewer["DocViewer-toolbarFirstPage"],
                        toolbarPreviousPage: SN.Resources.DocViewer["DocViewer-toolbarPreviousPage"],
                        toolbarNextPage: SN.Resources.DocViewer["DocViewer-toolbarNextPage"],
                        toolbarLastPage: SN.Resources.DocViewer["DocViewer-toolbarLastPage"],
                        toolbarFitWindow: SN.Resources.DocViewer["DocViewer-toolbarFitWindow"],
                        toolbarFitHeight: SN.Resources.DocViewer["DocViewer-toolbarFitHeight"],
                        toolbarFitWidth: SN.Resources.DocViewer["DocViewer-toolbarFitWidth"],
                        toolbarZoomOut: SN.Resources.DocViewer["DocViewer-toolbarZoomOut"],
                        toolbarZoomIn: SN.Resources.DocViewer["DocViewer-toolbarZoomIn"],
                        toolbarPrint: SN.Resources.DocViewer["DocViewer-toolbarPrint"],
                        toolbarRubberBandZoom: SN.Resources.DocViewer["DocViewer-toolbarRubberBandZoom"],
                        toolbarFullscreen: SN.Resources.DocViewer["DocViewer-toolbarFullscreen"],
                        toolbarExitFullscreen: SN.Resources.DocViewer["DocViewer-toolbarExitFullscreen"],
                        toolbarShowShapes: SN.Resources.DocViewer["DocViewer-toolbarShowShapes"],
                        toolbarHideShapes: SN.Resources.DocViewer["DocViewer-toolbarHideShapes"],
                        toolbarShowWatermark: SN.Resources.DocViewer["DocViewer-toolbarShowWatermark"],
                        toolbarHideWatermark: SN.Resources.DocViewer["DocViewer-toolbarHideWatermark"],
                        toolbarBurn: SN.Resources.DocViewer["DocViewer-toolbarBurn"],
                        toolbarRotatePageLeft: SN.Resources.DocViewer["DocViewer-toolbarRotatePageLeft"],
                        toolbarRotatePageRight: SN.Resources.DocViewer["DocViewer-toolbarRotatePageRight"],
                        toolbarRotateDocLeft: SN.Resources.DocViewer["DocViewer-toolbarRotateDocLeft"],
                        toolbarRotateDocRight: SN.Resources.DocViewer["DocViewer-toolbarRotateDocRight"],
                        annotationDefaultText: SN.Resources.DocViewer["DocViewer-annotationDefaultText"],
                        page: SN.Resources.DocViewer["DocViewer-page"],
                        showThumbnails: SN.Resources.DocViewer["sDocViewer-howThumbnails"],
                        deleteText: SN.Resources.DocViewer["DocViewer-deleteText"],
                        saveText: SN.Resources.DocViewer["DocViewer-saveText"],
                        cancelText: SN.Resources.DocViewer["DocViewer-cancelText"],
                        originalSizeText: SN.Resources.DocViewer["DocViewer-originalSize"],
                        downloadText: SN.Resources.DocViewer["DocViewer-downloadDocument"],
                        noPreview: noPreviewText
                    },
                    functions: {
                        print: {
                            action: printDocument,
                            title: SN.Resources.DocViewer["DocViewer-toolbarPrint"],
                            icon: '<span class="sn-icon sn-icon-print"></span>',
                            type: 'dataRelated',
                            touch: false
                        },
                        toggleWatermark: {
                            action: toggleWatermark,
                            title: SN.Resources.DocViewer["DocViewer-toolbarShowWatermark"],
                            icon: '<span class="sn-icon sn-icon-watermark"></span>',
                            type: 'dataRelated',
                            permission: noWatermark,
                            touch: false
                        },
                        save: {
                            action: Save,
                            title: SN.Resources.DocViewer["DocViewer-saveText"],
                            icon: '<span class="sn-icon sn-icon-save"></span>',
                            type: 'dataRelated',
                            permission: isAdmin,
                            touch: false
                        }
                    },
                    previewCount: previewCount,
                    getPC: getPC,
                    filePath: filePath,
                    getShapes: getShapes,
                    fitContainer: true,
                    addNoCachePostfix: true,
                    callbacks: {
                        viewerError: function (errorMessage) {
                            overlayManager.showMessage({
                                type: "error",
                                title: errorMessage,
                                text: errorMessage
                            });
                        },
                        viewerInfo: function (message) {
                            overlayManager.showMessage({
                                type: "info",
                                title: message,
                                text: message
                            });
                        },
                        documentOpened: resizeToolbar,
                        rotationStarted: function ($button) {
                            overlayManager.showLoader({});
                        },
                        rotationEnded: function ($button) {
                            overlayManager.hideLoader();
                        },
                        loadingStarted: function () {
                            overlayManager.showLoader({});
                        },
                        loadingEnded: function () {
                            overlayManager.hideLoader();
                        }
                    }
                }).data("snDocViewer");

                $viewer = $(".sn-docpreview-container").parent();
                viewer = $viewer.data('snDocViewer');

                appendPreviewPostfix = viewer.appendPreviewPostfix;
                var tempPreviewArray = [];

                if (touch) {
                    $('.sn-portalremotecontrol').remove();
                    $('.sn-zooming-tools').append('<span><span class="sn-icon sn-icon-menu"></span></span>');


                    $.when(actionsReq).done(function () {
                        $('.sn-docpreview-fullscreen-wrapper').append('<div class="sn-docviewer-actions"></div>');
                        $actionList = $('.sn-docviewer-actions');
                        $actionList.append('<span class="sn-action"><a href="' + parentPath + '"><span class="sn-icon sn-icon-back"></span>Back to the library</a></span>');
                        $.each(actionArray, function (i, item) {
                            if (item.Forbidden === false) {
                                var title = item.DisplayName;
                                var path = item.Url;
                                var icon = item.Icon;
                                if (path.charAt(0) === '/') {
                                    $actionList.append('<span class="sn-action"><a href="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</a></span>');

                                }
                                else {
                                    $actionList.append('<span class="sn-action"><span onClick="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</span></span>');
                                }
                            }
                        });
                    });
                    $('.sn-icon-menu').on('click', function () {
                        if (!$('.sn-docviewer-actions').hasClass('active'))
                            $('.sn-docviewer-actions').slideDown(200).addClass('active');
                        else
                            $('.sn-docviewer-actions').slideUp(200).removeClass('active');
                    });
                }

                //setInterval(function(){
                //    checkPromiseArray(docViewer.currentPage());
                //},5000);


                //refreshtest

                $('.refresh').on('click', function () {
                    docViewer.refreshViewer(true, true, true);
                });
                var windowWidth = window.innerWidth;
                $(window).resize(function () {

                    var newwindowWidth = window.innerWidth;
                    var direction = 'u';
                    if (windowWidth < newwindowWidth)
                        direction = 'd';
                    resizeToolbar(direction);
                    windowWidth = newwindowWidth;
                });

            }

            function resizeToolbar(d) {
                var $formerToolbar = $('.sn-zooming-tools');
                var toolbarItemWidth = 40;
                var toolbarWidth = $formerToolbar.width();

                var $menuIcon = $('<span class="sn-icon sn-icon-menu"></span>');

                var $ddownlist = $('<ul></ul>');
                if (d === 'u' || typeof d === 'undefined') {
                    if (windowSizeIsToSmall()) {
                        if ($('.sn-zooming-tools').find('ul').length === 0) {
                            $ddownlist.appendTo($formerToolbar);
                            $ddownlist.hide();

                            $ddownlist.before($menuIcon);
                            $('.sn-viewer-rotate').children().unwrap();
                            for (var i = $formerToolbar.find('.sn-icon').length - 2; i > getShowableItems(toolbarItemWidth, toolbarWidth) - 1; i--) {

                                var $element = $formerToolbar.children('span').eq(i).detach();
                                $ddownlist.prepend($element);
                            }
                        }
                        else {
                            for (var i = $formerToolbar.find('.sn-icon').length - 2; i > getShowableItems(toolbarItemWidth, toolbarWidth) - 1; i--) {
                                if (!$formerToolbar.children('span').eq(i).hasClass('sn-icon-menu')) {
                                    var $element = $formerToolbar.children('span').eq(i).detach();
                                    $ddownlist.prepend($element);

                                }
                            }
                        }

                        $menuIcon.on('click', function () {
                            var that = $(this);
                            if (!that.hasClass('active')) {
                                that.next('ul').slideDown(200);
                                that.addClass('active');
                            }
                            else {
                                that.next('ul').slideUp(200);
                                that.removeClass('active');
                            }
                        });
                    }
                    else {
                        $formerToolbar.find('ul').remove();
                        $formerToolbar.find('.sn-icon-menu').remove();
                    }

                    $ddownlist.children('span').each(function () {
                        var that = $(this);
                        var title = that.attr('title');
                        that.append('<span>' + title + '</span>');
                    });
                }
                else {
                    $ddownlist = $formerToolbar.find('ul');
                    $menuIcon = $formerToolbar.find('.sn-icon-menu');
                    if ($ddownlist.children().length === 1) {
                        for (var j = 0; j < getShowableItems(toolbarItemWidth, toolbarWidth) - $formerToolbar.children().length; j++) {
                            var $element = $ddownlist.children('span').eq(j).detach();
                            $menuIcon.before($element);

                            $ddownlist.remove();
                            $menuIcon.remove();
                        }

                    }
                    else {
                        for (var j = 0; j < getShowableItems(toolbarItemWidth, toolbarWidth) - ($formerToolbar.children().length - 2); j++) {

                            var $element = $ddownlist.children('span').eq(j).detach();
                            $menuIcon.before($element);
                        }
                    }

                    $formerToolbar.children('span').each(function () {
                        var that = $(this);
                        that.find('span:nth-child(2)').remove();
                    });
                }
            }

            function getShowableItems(itemWidth, toolbarWidth) {
                if (window.innerWidth > 1025)
                    return Math.floor(toolbarWidth / itemWidth) - 1;
                else if (window.innerWidth < 801) {
                    return 0;
                }
                else
                    return Math.floor(toolbarWidth / itemWidth) - 2;
            }

            function windowSizeIsToSmall() {
                var width = window.innerWidth;
                if (width < 1800)
                    return true;
                else
                    return false;
            }

            if (!touch) {
                $.when(actionsReq).done(function () {
                    $actionList = $('.sn-docviewer-actions');
                    $actionList.append('<span class="sn-action"><a href="' + parentPath + '"><span class="sn-icon sn-icon-back"></span>Back to the library</a></span>');
                    $.each(actionArray, function (i, item) {
                        if (item.Forbidden === false) {
                            var title = item.DisplayName;
                            var path = item.Url;
                            var icon = item.Icon;
                            if (path.charAt(0) === '/') {
                                $actionList.append('<span class="sn-action"><a href="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</a></span>');

                            }
                            else {
                                $actionList.append('<span class="sn-action"><span onClick="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</span></span>');
                            }
                        }
                    });
                });
            }


            function Save() {
                savable = viewer.saveShapes();
                savable.Path = filePath;
                var p = odata.saveContent({
                    contentItem: savable
                }).done(function () {
                    overlayManager.showMessage({
                        type: "success",
                        title: SN.Resources.DocViewer["MessageBox-Success"],
                        text: SN.Resources.DocViewer["DocViewer-burnSuccessful"]
                    });
                });
            }

            function printDocument() {
                // NOTE:
                // This feature works by creating a hidden iframe and calling the print method on its window object.
                // It might not work reliably accross all browsers and the viewer attempts to remedy that by firing a callback if it won't work.
                // ----------
                // Useful reading about the topic:
                // http://stackoverflow.com/questions/7570496/getting-the-document-object-of-an-iframe - getting document and window objects of an iframe
                // https://developer.mozilla.org/en-US/docs/Printing - explains printing and event handling in IE and Firefox
                // http://tjvantoll.com/2012/06/15/detecting-print-requests-with-javascript/ - explains a method for detecting printing in WebKit
                // ----------

                printable = viewer.saveShapes();

                rotatedPages = JSON.parse(printable.PageAttributes);

                // Remove previous print iframes
                $("#sn-docpreview-print-iframe").remove();
                overlayManager.showLoader({});
                //$('body').append('<div class="loading-print-view"><img src="/Root/Global/images/ajax-loader-white.gif" /><br />loading</div>');
                // Create HTML for the pictures
                var pics = '<style type="text/css">body{text-align: center;}</style>';

                var images = [];
                var previewsRequest = odata.customAction({
                    path: odata.getItemUrl(filePath),
                    action: "GetPreviewImages",
                    $select: ["Path", "Width", "Height", "Index"],
                    metadata: "no"
                }).done(function (data) {
                    if (!data || !data.d)
                        $.error('OData reply is incorrect for preview images request.');
                    images = data.d.results;
                });

                $.when(previewsRequest).done(function () {

                    if (rotatedPages.length > 0) {
                        var canvas = document.createElement('canvas');
                        var tempCanvas = document.createElement('canvas');
                        canvas.id = 'pageRotationCanvas';
                        tempCanvas.id = 'tempCanvas';
                        document.body.appendChild(canvas);
                        document.body.appendChild(tempCanvas);
                        var ctx = canvas.getContext("2d");
                        var tctx = tempCanvas.getContext("2d");

                        $canvas = $('#pageRotationCanvas');
                        $tempcanvas = $('#tempCanvas');
                        $canvas.hide();
                        $tempcanvas.hide();
                    }


                    var watermark = false;
                    if (typeof $('.sn-icon-nowatermark') !== 'undefined' && $('.sn-icon-nowatermark').length > 0)
                        watermark = true;

                    for (var i = 0; i < images.length; i++) {
                        var ind = pageIsRotated(images[i].Index);
                        if (ind !== -1 && typeof ind !== 'undefined') {
                            if (ind === -90)
                                ind = 270;
                            if (images[i].Width > images[i].Height && (ind === 180 || ind === 0)) {
                                if (watermark) {
                                    if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                        pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true" style="width: 90%" /></p>';
                                    else
                                        pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true" style="width: 100%" /></p>';
                                }
                                else {
                                    pics += '<p class="break"><img src="' + images[i].Path + '" style="width: 100%" /></p>';
                                }
                            }
                            else {
                                if (watermark) {
                                    if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                        pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true&rotation=' + ind + '" style="height: 90%" /></p>';
                                    else
                                        pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true&rotation=' + ind + '" style="height: 95%" /></p>';
                                }
                                else {
                                    if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                        pics += '<p class="break"><img src="' + images[i].Path + '?rotation=' + ind + '" style="width: 90%" /></p>';
                                    else
                                        pics += '<p class="break"><img src="' + images[i].Path + '?rotation=' + ind + '" style="width: 100%" /></p>';
                                }
                            }
                        }
                        else if ((typeof ind === 'undefined' || ind === -1) && images[i].Width > images[i].Height) {
                            if (watermark) {
                                if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                    pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true" style="width: 90%" /></p>';
                                else
                                    pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true" style="width: 100%" /></p>';
                            }
                            else {
                                if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                    pics += '<p class="break"><img src="' + images[i].Path + '?" style="width: 90%" /></p>';
                                else
                                    pics += '<p class="break"><img src="' + images[i].Path + '?" style="width: 100%" /></p>';
                            }
                        }
                        else {
                            if (watermark) {
                                if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                    pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true" style="height: 90%" /></p>';
                                else
                                    pics += '<p class="break"><img src="' + images[i].Path + '?watermark=true" style="height: 95%" /></p>';
                            }
                            else {
                                if (navigator.userAgent.toLowerCase().indexOf('msie') > -1)
                                    pics += '<p class="break"><img src="' + images[i].Path + '" style="height: 90%" /></p>';
                                else
                                    pics += '<p class="break"><img src="' + images[i].Path + '" style="height: 95%" /></p>';
                            }
                        }

                    }
                    // Create iframe element
                    var $iframe = $('<iframe id="sn-docpreview-print-iframe"></iframe>').css({
                        width: '100%',
                        height: '100%',
                        position: 'absolute',
                        top: 0,
                        left: 0,
                        'z-index': -1
                    });
                    // NOTE: browsers will not print() the contents of the iframe if it's not appended to the document
                    $iframe.appendTo($("body"));

                    // Find the DOM document inside the iframe
                    var doc = ($iframe[0].contentWindow) ? ($iframe[0].contentWindow.document) : (($iframe[0].contentDocument) ? (($iframe[0].contentDocument.document) ? $iframe[0].contentDocument.document : $iframe[0].contentDocument) : null);
                    doc.open();
                    doc.write(pics);
                    doc.close();

                    // Find the content window
                    var win, $win;
                    if ($iframe[0].contentWindow && typeof ($iframe[0].contentWindow.print) === "function") {
                        win = $iframe[0].contentWindow;
                    } else if ($iframe[0].contentDocument && typeof ($iframe[0].contentDocument.print) === "function") {
                        win = $iframe[0].contentDocument;
                    } else {
                        // There is no content window on the iframe or it doesn't support printing
                        $iframe.remove();
                        return;
                    }
                    $win = $(win);

                    // Print event handlers
                    var beforePrint = function (e) {
                        null;
                    };
                    var afterPrint = function (e) {
                        null;
                    };

                    // This works in WebKit, but the events are fired multiple times
                    if (win.matchMedia) {
                        var mediaQueryList = win.matchMedia('print');
                        mediaQueryList.addListener(function (mql) {
                            if (mql.matches) {
                                beforePrint();
                            } else {
                                afterPrint();
                            }
                        });
                    }

                    // This works in IE and Firefox
                    $win.on("beforeprint.snDocViewer", beforePrint);
                    $win.on("afterprint.snDocViewer", function () {
                        afterPrint();
                        $iframe.remove(); // Can't remove the element in Chrome in afterPrint() because then it crashes
                    });

                    // Call print
                    $iframe.load(function () {
                        win.focus();
                        win.print();
                        $('.loading-print-view').remove();
                        if (typeof canvas !== 'undefined') {
                            $('#pageRotationCanvas').remove();
                            $('#tempCanvas').remove();
                        }
                    });

                    overlayManager.hideLoader({});
                });
            }

            function pageIsRotated(p) {
                for (var i = 0; i < rotatedPages.length; i++) {
                    if (p === parseInt(rotatedPages[i].pageNum) && rotatedPages[i].options.degree !== 0)
                        return parseInt(rotatedPages[i].options.degree);
                    else if (p !== parseInt(rotatedPages[i].pageNum) && i === (rotatedPages.length - 1))
                        return -1;
                }
            }

            function rotateImg(canvas, tcanvas, ctx, tctx, p, d) {

                tcanvas.width = p.Width;
                tcanvas.height = p.Height;

                if (d === 180) {
                    canvas.width = p.Width;
                    canvas.height = p.Height;
                }
                else {
                    canvas.width = p.Height;
                    canvas.height = p.Width;
                }
                image = new Image();

                image.onload = function () {
                    tctx.drawImage(image, 0, 0);

                    if (d === -90) {
                        canvas.width = p.Height;
                        canvas.height = p.Width;
                        ctx.translate(0, canvas.height);
                        ctx.rotate(d * Math.PI / 180);
                    }
                    else if (d === 90) {
                        canvas.width = p.Height;
                        canvas.height = p.Width;
                        ctx.translate(canvas.width, 0);
                        ctx.rotate(d * Math.PI / 180);
                    }
                    else {
                        ctx.rotate(d * Math.PI / 180);
                    }

                    ctx.drawImage(tempCanvas, 0, 0);
                };

                image.src = p.Path;
                ctx.restore();
                pngUrl = canvas.toDataURL();
                return pngUrl;
            }

            function toggleWatermark() {
                var $this = $(this).children('span');
                if ($this.hasClass('sn-icon-watermark')) {
                    $this.removeClass('sn-icon-watermark').addClass('sn-icon-nowatermark').attr('title', SN.Resources.DocViewer["DocViewer-toolbarHideWatermark"]);
                    switchWatermark(true);
                }
                else {
                    $this.removeClass('sn-icon-nowatermark').addClass('sn-icon-watermark').attr('title', SN.Resources.DocViewer["DocViewer-toolbarShowWatermark"]);
                    switchWatermark(false);
                }
            }

            var Request = function (p, id, dead) {
                this.p = p;
                this.id = id;
                this.dead = dead;
            }

            function addPromiseToArray(p, id) {
                var req = new Request();
                requests.push(req);
                requests[requests.length - 1].p = p;
                requests[requests.length - 1].idx = id;
                requests[requests.length - 1].dead = false;
            }

            function checkPromiseArray(id) {
                for (var i = 0; i < requests.length; i++) {
                    if (requests[i].idx < id - 3 || requests[i].idx > id + 3) {
                        requests[i].p.resolve();
                        requests[i].dead = true;
                    }
                }
                removeDeadPromises();
            }

            function removeDeadPromises() {
                tempRequests = [];
                for (var i = 0; i < requests.length; i++) {
                    if (!requests[i].dead) {
                        tempRequests[tempRequests.length] = requests[i];
                    }
                }
                requests = [];
                for (var j = 0; j < tempRequests.length; j++) {
                    requests[j] = tempRequests[j];
                }
            }

            function getThumbnail(item) {
                var promise = new $.Deferred();
                previewExists(item).done(function (data) {
                    promise.resolve(data);
                });
                return promise;
            }

            var appendPreviewPostfix;

        function switchWatermark(enabled) {
            if (appendPreviewPostfix && typeof appendPreviewPostfix == "function") {
                wm = enabled;
                var $images = [];
                $images = $("img[data-loaded=true]", $('#docpreview'));
                var wmParam = '&watermark=true';
                // Iterate through all images
                $images.each(function (i) {
                    var $img = $($images[i]);
                    var oldsrc = $img.attr('src');
                    var rotationParam = '';
                    var rotation = $img.parent().attr('data-degree');
                    if (typeof rotation !== 'undefined' && rotation !== 0)
                        rotationParam = '&rotation=' + rotation;
                    
                    var path;
                    // Set the src parameter according to the watermark URL parameter
                    if (enabled) {
                        // trimright: remove last '?' if there is one, to avoid having duplicate question marks ('??')
                        //oldsrc = oldsrc.replace(new RegExp("[\?]+$"), "");
                        path = appendPreviewPostfix(oldsrc.substring(0, oldsrc.indexOf('?')), true, true, rotationParam);
                        $img.attr('src', path);
                        wm = true;
                    }
                    else {
                        path = appendPreviewPostfix(oldsrc.substring(0, oldsrc.indexOf('?')), false, true, rotationParam);
                        $img.attr('src', path);
                        wm = false;
                    }
                });
            }
        }

        function getImage(item) {
            var promise = new $.Deferred();
            addPromiseToArray(promise, item);
            previewExists(item).done(function(data) {
                promise.resolve(data);
            });
            return promise;
        }

        function getExistingPreviewImages(){
            var promise = new $.Deferred();
            existingPreviews().done(function(data) {
                promise.resolve(data);
            });
            return promise;
        }

        function previewExists(item) {
            var promise = new $.Deferred();

            odata.customAction({
                path: odata.getItemUrl(filePath),
                version: fileVersion,
                nocache: true,
                action: 'PreviewAvailable',
                params: {
                    page: item
                }
            }).done(function (data) {
                if (data.PreviewAvailable !== null) {
                    promise.resolve(data);
                }
                else {
                    setTimeout(function () {
                        previewExists(item).done(function(data){
                            promise.resolve(data);
                        }).fail(function() {
                            promise.reject();
                        });
                    }, 5000);
                }
                    
            }).fail(function() {
                promise.reject();
            });
            return promise;
        }

        function existingPreviews() {
            var promise = new $.Deferred();

            odata.customAction({
                path: odata.getItemUrl(filePath),
                version: fileVersion,
                action: 'GetExistingPreviewImages'
            }).done(function (data) {
                promise.resolve(data);
                    
            }).fail(function() {
                promise.reject();
            });
            return promise;
        }

        var timeout;

        function getPreviewCount(path) {
            var promise = new $.Deferred();
            var pcCount = $.ajax({
                url: path + '&nocache=' + new Date().getTime(),
                skipAjaxLoader: true
            }).done(function (data) {
                if (data.d.PageCount !== -1 && data.d.PageCount !== -4) {
                    pcpromise.resolve(data);
                } else {
                    timeout = setTimeout(function () {
                        getPreviewCount(path);
                    }, 3000);
                }
            }).fail(function () {
                pcpromise.reject();
            });
            return pcpromise;
        }

       
        function getPC(path) {
            var pcpromise = new $.Deferred();
            var path = "/OData.svc" + odata.getItemUrl(filePath) + "?$select=PageCount&metadata=no";

            getPreviewCount(path).done(function(data) {
                pcpromise.resolve(data);
            });
            return pcpromise;
        }


        function getShapes(path) {
            var shapepromise = new $.Deferred();
            var path = "/OData.svc" + odata.getItemUrl(path) + "?$select=Shapes&metadata=no";

            getDocShapes(path).done(function(data) {
                shapepromise.resolve(data);
            });
            return shapepromise;
        }

        function getDocShapes(path) {
            var shapepromise = new $.Deferred();
            var pcCount = $.ajax({
                url: path
            }).done(function (data) {
                
                if (typeof data.d == 'object') {
                    shapepromise.resolve(data);
                }
                else {
                    setTimeout(function () {
                        getDocShapes(path);
                    }, 3000);
                }
            }).fail(function() {
                shapepromise.reject();
            });
            return shapepromise;
        }
        

        function adminbutton(name, buttonAdditonalProperties) {
            this.name = name;
            this.additionalProps = buttonAdditonalProperties;
        }

            var dataObj = {}

            $pluginSubject.data('viewer', dataObj);
            // Maintain jQuery chainability
            return $pluginSubject;
        }
    });
})(jQuery);