(function () {
    function attach() {
        document.querySelectorAll('tr.clickable-row').forEach(function (row) {
            if (row.dataset.clickableBound === '1') return;
            row.dataset.clickableBound = '1';
            row.addEventListener('click', function (e) {
                if (e.target.closest('a, button, input, label, select, textarea')) return;
                var href = row.dataset.href;
                if (href) window.location = href;
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', attach);
    } else {
        attach();
    }
})();
