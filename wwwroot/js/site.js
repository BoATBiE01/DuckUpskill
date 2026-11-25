// DuckUpskill Site JavaScript

// Reusable Password Visibility Toggle
document.addEventListener('DOMContentLoaded', function () {
    document.addEventListener('click', function (e) {
        const toggleBtn = e.target.closest('.toggle-password-btn');
        if (!toggleBtn) return;

        e.preventDefault();
        const group = toggleBtn.closest('.input-group');
        if (!group) return;

        const input = group.querySelector('input');
        if (!input) return;

        const isPassword = input.type === 'password';
        input.type = isPassword ? 'text' : 'password';

        const eyeOpen = toggleBtn.querySelector('.eye-open');
        const eyeClosed = toggleBtn.querySelector('.eye-closed');

        if (eyeOpen && eyeClosed) {
            if (isPassword) {
                eyeOpen.classList.add('d-none');
                eyeClosed.classList.remove('d-none');
            } else {
                eyeOpen.classList.remove('d-none');
                eyeClosed.classList.add('d-none');
            }
        }

        input.focus();
    });
});
