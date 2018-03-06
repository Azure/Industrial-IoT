jQuery.fn.dataTable.Api.register('page.jumpToRow()', function (arrayPosition) {
    "use strict";

    var pos = -1;

    for (var i = 0, rows = this.rows() ; i < rows[0].length; i++) {
        if (rows[0][i] === arrayPosition) {
            pos = i;
            break;
        }
    }

    if (pos >= 0) {
        var page = Math.floor(pos / this.page.info().length);
        this.page(page).draw(false);
    }

    return this;
});
