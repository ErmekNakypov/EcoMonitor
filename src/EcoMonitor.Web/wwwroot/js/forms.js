(function () {
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.dataset.noLoading) return;

        var submitBtn = form.querySelector('button[type="submit"]');
        if (!submitBtn) return;

        if (submitBtn.dataset.busy === '1') {
            e.preventDefault();
            return;
        }

        submitBtn.dataset.busy = '1';
        submitBtn.dataset.originalHtml = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Working…';

        window.addEventListener('pageshow', function () {
            if (submitBtn.dataset.busy === '1') {
                submitBtn.disabled = false;
                submitBtn.innerHTML = submitBtn.dataset.originalHtml;
                submitBtn.dataset.busy = '0';
            }
        });
    }, true);
})();
