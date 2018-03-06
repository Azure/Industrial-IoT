(function ($) {
    "use strict";

    $.fn.dataTable.moment = function (format, locale) {
        var types = $.fn.dataTable.ext.type;

        // Add type detection
        types.detect.unshift(function (d, x) {
            // Null and empty values are acceptable
            if (d === '' || d === null) {
                return 'moment-' + format;
            }

            if (d === "Pending") {
                d = moment().format(format);
            }

            return moment(d.replace ? d.replace(/<.*?>/g, '').trim() : d, format, locale, true).isValid() ?
                'moment-' + format :
                null;
        });

        // Add sorting method - use an integer for the sorting
        types.order['moment-' + format + '-pre'] = function (d, x) {
            if (!moment(d.replace ? d.replace(/<.*?>/g, '').trim() : d, format, locale, true).isValid()) {
                return Infinity;
            }

            return d === '' || d === null ?
                -Infinity :
                parseInt(moment(d.replace ? d.replace(/<.*?>/g, '').trim() : d, format, locale, true).format('x'), 10);
        };
    };

}(jQuery));