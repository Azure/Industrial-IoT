(function ($) {
    "use strict";
    
    //----------------------------------------//
    // Variable
    //----------------------------------------//
    var variable = {
        width : 0,
        height : 0,
        selector : '.item-point',
        styleSelector : 'circle',
        animationSelector : '',
        animationPopoverIn : '',
        animationPopoverOut : '', 
        onInit : null,
        getSelectorElement : null,
        getValueRemove : null
    }

    //----------------------------------------//
    // Scaling
    //----------------------------------------//
    var scaling = {
        settings : null,
        //----------------------------------------//
        // Initialize
        //----------------------------------------//
        init: function(el, options){
            this.settings = $.extend(variable, options);
            this.event(el);            

            scaling.layout(el);
            $(window).on('load', function(){
                scaling.layout(el);
            });
            $(el).find('.target').on('load', function(){
                scaling.layout(el);
            });
            $(window).on('resize', function(){
                setTimeout(function () {
                    scaling.layout(el);
                }, 20);
            });
        },

        //----------------------------------------//
        // Event
        //----------------------------------------//
        event : function(elem){
            // Set Style Selector
            if ( this.settings.styleSelector ) {
                $(this.settings.selector).addClass( this.settings.styleSelector );
            }

            // Set Animation
            if ( this.settings.animationSelector ) {
                if( this.settings.animationSelector == 'marker' ){
                    $(this.settings.selector).addClass( this.settings.animationSelector );
                    $(this.settings.selector).append('<div class="pin"></div>')
                    $(this.settings.selector).append('<div class="pulse"></div>')
                }else{
                    $(this.settings.selector).addClass( this.settings.animationSelector );
                }
            }

            // Event On Initialize
            if ( $.isFunction( this.settings.onInit ) ) {
                this.settings.onInit();
            }

            // Content add class animated element
            $(elem).find('.content').addClass('animated');

            // Wrapper selector
            $(this.settings.selector).wrapAll( "<div class='wrap-selector' />");

            // Event Selector
            $(this.settings.selector).each(function(){
                
                // Toggle
                $('.toggle', this).on('click', function(e){
                    e.preventDefault();
                    $(this).closest(scaling.settings.selector).toggleClass('active');

                    // Selector Click
                    var content = $(this).closest(scaling.settings.selector).data('popover'),
                        id = $(content);

                    if($(this).closest(scaling.settings.selector).hasClass('active') && !$(this).closest(scaling.settings.selector).hasClass('disabled')){
                        if ( $.isFunction( scaling.settings.getSelectorElement ) ) {
                            scaling.settings.getSelectorElement($(this).closest(scaling.settings.selector));
                        }
                        id.fadeIn();
                        scaling.layout(elem);
                        id.removeClass(scaling.settings.animationPopoverOut);
                        id.addClass(scaling.settings.animationPopoverIn);
                    }else{
                        if ($.isFunction( scaling.settings.getValueRemove )){
                            scaling.settings.getValueRemove($(this).closest(scaling.settings.selector));
                        }
                        id.removeClass(scaling.settings.animationPopoverIn);
                        id.addClass(scaling.settings.animationPopoverOut);
                        id.delay(500).fadeOut();
                    }
                });

                // Select
                $('.select', this).on('click', function (e) {
                });
                $('.select', this).on('mouseover', function (e) {
                    $(this).closest(scaling.settings.selector).css("opacity", 0.7);
                });
                $('.select', this).on('mouseout', function (e) {
                    $(this).closest(scaling.settings.selector).css("opacity", 1);
                });

                // Exit
                var target = $(this).data('popover'),
                    idTarget = $(target);
                idTarget.find('.exit').on('click', function(e){
                    e.preventDefault();
                    // selector.removeClass('active');
                    $('[data-popover="'+ target +'"]').removeClass('active');
                    idTarget.removeClass(scaling.settings.animationPopoverIn);
                    idTarget.addClass(scaling.settings.animationPopoverOut);
                    idTarget.delay(500).fadeOut();
                });
            });
        },

        //----------------------------------------//
        // Layout
        //----------------------------------------//
        layout : function(elem){

            // Get Original Image
            var image = new Image();
            image.src = elem.find('.target').attr("src");

            // Variable
            var width = image.naturalWidth,
                height = image.naturalHeight,
                getWidthLess = $(elem).width(),
                getHeightLess = $(elem).height(),
                setPercenHeight = getHeightLess / height * 100,
                setPercenWidth = getWidthLess / width * 100;

            // Set Position Selector
            $(this.settings.selector).each(function () {
                var getTop = $(this).data("top") * setPercenHeight / 100,
                    getLeft = $(this).data("left") * setPercenWidth / 100;

                $(this).css("top", getTop + "px");
                $(this).css("left", getLeft + "px");

                // Target Position
                var target = $(this).data('popover'),
                    allSize = $(target).find('.head').outerHeight() + $(target).find('.body').outerHeight() + $(target).find('.footer').outerHeight();
                $(target).css("left", getLeft + "px");
                $(target).css("height", allSize + "px");
                
                if($(target).hasClass('bottom')){
                    var getHeight = $(target).outerHeight(),
                        getTopBottom = getTop - getHeight;
                    $(target).css("top", getTopBottom + "px");
                }else if($(target).hasClass('center')){
                    var getHeight = $(target).outerHeight() * 0.50,
                        getTopBottom = getTop - getHeight;
                    $(target).css("top", getTopBottom + "px");
                }else{
                    $(target).css("top", getTop + "px");
                }

                $('.select', this).css('width', $(this).outerWidth());
                $('.select', this).css('height', $(this).outerHeight());

                $('.toggle', this).css('width', $(this).outerWidth());
                $('.toggle', this).css('height', $(this).outerHeight());
                
                // Toggle Size
                if($(this).find('.pin')){
                    var widthThis = $('.pin', this).outerWidth(),
                        heightThis = $('.pin', this).outerHeight();
                    $('.toggle', this).css('width', widthThis);
                    $('.toggle', this).css('height', heightThis);                    
                }
            });
        }
        
    };

    //----------------------------------------//
    // Scalize Plugin
    //----------------------------------------//
    $.fn.scalize = function(options){
        return scaling.init(this, options);
    };

}(jQuery));