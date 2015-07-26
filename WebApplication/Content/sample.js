$('.myCustomCheckbox')
    .attr('checked', 'checked')
    .css('background-color', 'red')
    .addClass('selected-checkbox')
    .removeClass('boring-checkbox');

var obj = function () {
    var x = getData();
    sort(x);
    return x;
}();