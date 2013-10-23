﻿; if (typeof window.dnn === "undefined" || window.dnn === null) { window.dnn = {}; }; //var dnn = dnn || {};

// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2013
// by DotNetNuke Corporation
// All Rights Reserved

(function ($, window, document, undefined) {
    "use strict";

    // undefined is used here as the undefined global variable in ECMAScript 3 is
    // mutable (ie. it can be changed by someone else). undefined isn't really being
    // passed in so we can ensure the value of it is truly undefined. In ES5, undefined
    // can no longer be modified.

    // window and document are passed through as local variables rather than globals
    // as this (slightly) quickens the resolution process and can be more efficiently
    // minified (especially when both are regularly referenced in your plugin).

    //
    // GettingStarted
    //
    var GettingStarted = this.GettingStarted = function (element, options) {
        this.element = element;
        this._options = options;

        this.init();
    };

    GettingStarted.prototype = {

        constructor: GettingStarted,

        init: function () {
            // Place initialization logic here
            // You already have access to the DOM element and the options via the instance, 
            // e.g., this.element and this._options
            this._options = $.extend({}, GettingStarted.defaults(), this._options);

            this._controller = new GettingStartedController();

            this.$this = $(this);

            if (this._options.showOnStartup) {
                this.show();
            }
        },

        _isEmailValid: function(email) {
            var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return re.test(email);
        },

        _ensureDialog: function() {
            if (!this.$element) {
                this.$element = this.element ? $(this.element) : this._createLayout();
                this._$checkBox = this.$element.find("." + this._options.footerLeftCss).find("input");
                this._$emailBox = this.$element.find("." + this._options.inputboxWrapperCss).find("input");
                var $iframe = this.$element.find("iframe");
                $iframe.attr("src", this._options.contentUrl);
                var $signUpButton = this.$element.find("." + this._options.headerLeftInputCss).find("a");
                $signUpButton.on("click", $.proxy(this._signUp, this));
                var $downloadManual = this.$element.find("." + this._options.headerRightCss).find("a");
                $downloadManual.attr("href", this._options.userManualLinkUrl);
            }
        },

        _createLayout: function () {
            var checkBoxId = dnn.uid("gs_");
            var signUpBoxId = dnn.uid("gs_");

            var layout = $("<div class='" + this._options.containerCss + "'/>")
                .append($("<div class='" + this._options.headerCss + "'/>")
                    .append($("<div class='" + this._options.headerLeftCss + "'/>")
                        .append($("<div/>")
                            .append($("<div class='" + this._options.headerLeftTextCss + "'/>")
                                .append($("<div/>")
                                    .append($("<label for='" + signUpBoxId + "'>" + this._options.signUpLabel + "</label>"))
                                    .append($("<span>" + this._options.signUpText + "</span>"))))
                            .append($("<div class='" + this._options.headerLeftInputCss + "'/>")
                                .append($("<div/>")
                                    .append($("<div class='" + this._options.inputboxWrapperCss + "'/>")
                                        .append($("<input type='text' id='" + signUpBoxId + "' maxlength='200' autocomplete='off'/>")))
                                    .append($("<a href='javascript:void(0);' title='" + this._options.signUpButton + "'>" + this._options.signUpButton + "</a>"))))))
                    .append($("<div class='" + this._options.headerRightCss + "'/>")
                        .append($("<div/>")
                            .append($("<a href='javascript:void(0);' title='" + this._options.downloadManualButton + "'><span>" + this._options.downloadManualButton + "</span></a>")))))
                .append($("<div class='" + this._options.contentCss + "'/>")
                    .append($("<div/>")
                        .append($("<iframe src='about:blank' scrolling='auto' frameborder='0' />"))))
                .append($("<div class='" + this._options.footerCss + "'/>")
                    .append($("<div class='" + this._options.footerBorderCss + "'/>")
                        .append($("<div/>")))
                    .append($("<div class='" + this._options.footerLeftCss + "'/>")
                        .append($("<input type='checkbox' id='" + checkBoxId + "' value='dontshow' name='ShowDialog' >"))
                        .append($("<label for='" + checkBoxId + "'>" + this._options.dontShowDialogLabel + "</label>")))
                    .append($("<div class='" + this._options.footerRightCss + "'/>")
                        .append($("<a href='//twitter.com/dnncorp' class='" + this._options.twitterLinkCss + "' title='" + this._options.twitterLinkTooltip + "'/>"))
                        .append($("<a href='//facebook.com/dotnetnuke' class='" + this._options.facebookLinkCss + "' title='" + this._options.facebookLinkTooltip + "'/>"))));

            return layout;
        },

        _onCloseDialog: function () {
            var notShowAgain = this._$checkBox.prop("checked");
            this._controller.closeDialog(notShowAgain);
            this._isShown = false;
        },

        _onGetSettings: function(settings) {
            this._$checkBox.prop("checked", settings.IsHidden);
            this._$emailBox.val(settings.EmailAddress);
        },

        _signUp: function () {
            var email = this._$emailBox.val().trim();
            var isValid = this._isEmailValid(email);
            var self = this;
            if (!isValid) {
                $.dnnAlert({ title: this._options.invalidEmailTitle, text: this._options.invalidEmailMessage, callback: function () { self._$emailBox.focus(); } });
                return;
            }
            this._controller.signUp(email, $.proxy(this._onSignUp, this));
        },

        _onSignUp: function () {
            $.dnnAlert({ title: this._options.signUpTitle, text: this._options.signUpMessage });
        },

        show: function () {
            if (this._isShown) {
                return;
            }
            this._isShown = true;
            this._ensureDialog();

            this._controller.getSettings($.proxy(this._onGetSettings, this));

            this.$element.dialog({
                modal: true,
                autoOpen: true,
                dialogClass: "dnnFormPopup " + this._options.dialogCss,
                title: this._options.title,
                resizable: false,
                width: 950,
                height: 640,
                close: $.proxy(this._onCloseDialog, this)
            });
        }

    };

    GettingStarted._defaults = {
        dialogCss: "gs-dialog",
        containerCss: "gs-container",
        headerCss: "gs-header",
        headerLeftCss: "gs-header-left-side",
        headerLeftTextCss: "gs-header-left-side-text",
        headerLeftInputCss: "gs-header-left-side-input",
        inputboxWrapperCss: "gs-header-left-side-inputbox-wrapper",
        headerRightCss: "gs-header-right-side",
        contentCss: "gs-content",
        footerCss: "gs-footer",
        footerBorderCss: "gs-footer-border",
        footerLeftCss: "gs-footer-left-side",
        footerRightCss: "gs-footer-right-side",
        twitterLinkCss: "gs-twitter-button",
        facebookLinkCss: "gs-facebook-button"
    };

    GettingStarted.defaults = function (settings) {
        if (typeof settings !== "undefined") {
            $.extend(GettingStarted._defaults, settings);
        }
        return GettingStarted._defaults;
    };


    //
    // GettingStarted Controller
    //
    var GettingStartedController = this.GettingStartedController = function (options) {
        this._options = options;
        this.init();
    };

    GettingStartedController.prototype = {
        constructor: GettingStartedController,

        init: function() {
            this._options = $.extend({}, GettingStartedController.defaults(), this._options);
            this._serviceUrl = $.dnnSF(this._options.moduleId).getServiceRoot(this._options.serviceRoot);
        },

        _callService: function (data, onLoadHandler, method, isGet) {
            var serviceSettings = {
                url: this._serviceUrl + method,
                beforeSend: $.dnnSF(this._options.moduleId).setModuleHeaders,
                dataType: "json",
                contentType: "application/json; charset=utf-8",
                data: isGet ? data : JSON.stringify(data),
                type: isGet ? "GET" : "POST",
                async: true,
                success: onLoadHandler,
                error: $.onAjaxError
            };
            $.ajax(serviceSettings);
        },

        closeDialog: function (notShowAgain, callback) {
            var onCloseDialogHandler = $.proxy(this._onCloseDialog, this, callback);
            this._callService({ IsHidden: notShowAgain }, onCloseDialogHandler, this._options.closeDialogMethod);
        },

        _onCloseDialog: function (callback, data, textStatus, jqXhr) {
            if (typeof callback === "function") {
                callback.apply(this, [data]);
            }
        },

        getSettings: function (callback) {
            var onGetSettingsHandler = $.proxy(this._onGetSettings, this, callback);
            this._callService({}, onGetSettingsHandler, this._options.getSettingsMethod, true);
        },

        _onGetSettings: function (callback, data, textStatus, jqXhr) {
            if (typeof callback === "function") {
                callback.apply(this, [data]);
            }
        },

        signUp: function (email, callback) {
            var onSignUpHandler = $.proxy(this._onSignUp, this, callback);
            this._callService({ Email: email }, onSignUpHandler, this._options.signUpMethod);
        },

        _onSignUp: function (callback, data, textStatus, jqXhr) {
            if (typeof callback === "function") {
                callback.apply(this, [data]);
            }
        }

    };

    GettingStartedController._defaults = {
        serviceRoot: "InternalServices",
        closeDialogMethod: "GettingStarted/CloseGettingStartedPage",
        getSettingsMethod: "GettingStarted/GetGettingStartedPageSettings",
        signUpMethod: "GettingStarted/SubscribeToNewsletter"
    };

    GettingStartedController.defaults = function (settings) {
        if (typeof settings !== "undefined") {
            $.extend(GettingStartedController._defaults, settings);
        }
        return GettingStartedController._defaults;
    };


    var GettingStartedDialog = this.GettingStartedDialog = dnn.singletonify(GettingStarted);


}).apply(dnn, [jQuery, window, document]);


dnn.createGettingStartedPage = function (options) {
    $(document).ready(function () {
        var instance = dnn.GettingStartedDialog.getInstance(null, options);
    });
};