document.addEventListener('click', function (event) {
    const toggle = event.target.closest('.password-toggle');
    if (!toggle) return;

    const targetId = toggle.dataset.target;
    if (!targetId) return;

    const input = document.getElementById(targetId);
    const icon = toggle.querySelector('[data-icon-for="' + targetId + '"]');
    if (!input || !icon) return;

    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('bi-eye');
        icon.classList.add('bi-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('bi-eye-slash');
        icon.classList.add('bi-eye');
    }
});
