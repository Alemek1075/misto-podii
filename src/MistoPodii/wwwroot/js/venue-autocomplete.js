$(function () {
    const input = $('#venue-search');
    if (!input.length) return;

    const idField = $('#VenueId');
    const list = $('#venue-suggestions');
    let timer;
    let request;
    let options = [];
    let activeIndex = -1;

    function close() {
        list.empty().prop('hidden', true);
        input.attr('aria-expanded', 'false').removeAttr('aria-activedescendant');
        options = [];
        activeIndex = -1;
    }

    function choose(option) {
        input.val(option.text);
        idField.val(option.id).trigger('change');
        close();
        input.trigger('focus');
    }

    function render(items) {
        close();
        options = items;
        if (!items.length) {
            list.append($('<li class="autocomplete-empty"></li>').text('Місць не знайдено.')).prop('hidden', false);
            input.attr('aria-expanded', 'true');
            return;
        }
        items.forEach(function (item, index) {
            const button = $('<button type="button" class="autocomplete-option" role="option"></button>')
                .attr('id', 'venue-option-' + index).text(item.text)
                .on('mousedown', function (event) { event.preventDefault(); choose(item); });
            list.append($('<li></li>').append(button));
        });
        list.prop('hidden', false);
        input.attr('aria-expanded', 'true');
    }

    input.on('input', function () {
        idField.val('0').trigger('change');
        clearTimeout(timer);
        if (request) request.abort();
        const term = input.val().trim();
        if (term.length < 3) { close(); return; }
        timer = setTimeout(function () {
            request = $.getJSON(input.data('url'), { q: term })
                .done(function (items) { if (input.val().trim() === term) render(items); })
                .fail(function (_xhr, status) { if (status !== 'abort') render([]); });
        }, 250);
    });

    input.on('keydown', function (event) {
        if (!options.length) return;
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            activeIndex = (activeIndex + (event.key === 'ArrowDown' ? 1 : -1) + options.length) % options.length;
            list.find('.autocomplete-option').attr('aria-selected', 'false').eq(activeIndex).attr('aria-selected', 'true');
            input.attr('aria-activedescendant', 'venue-option-' + activeIndex);
        } else if (event.key === 'Enter' && activeIndex >= 0) {
            event.preventDefault();
            choose(options[activeIndex]);
        } else if (event.key === 'Escape') {
            close();
        }
    });

    $(document).on('mousedown', function (event) {
        if (!$(event.target).closest('.autocomplete').length) close();
    });
});
