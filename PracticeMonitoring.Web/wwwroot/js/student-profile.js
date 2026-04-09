document.addEventListener('DOMContentLoaded', function () {
    const modalBackdrop = document.getElementById('profileModalBackdrop');
    const openButton = document.getElementById('openProfileSettingsButton');
    const closeButton = document.getElementById('closeProfileSettingsButton');
    const cancelButton = document.getElementById('cancelProfileSettingsButton');
    const avatarUpload = document.getElementById('avatarUpload');
    const modalAvatarPreview = document.getElementById('modalAvatarPreview');
    const headerAvatarPreview = document.getElementById('studentAvatarPreview');
    const settingsForm = document.getElementById('profileSettingsForm');
    const themeSelect = document.getElementById('editTheme');
    const initialTheme = document.documentElement.getAttribute('data-theme') || 'light';

    function openModal() {
        if (modalBackdrop) {
            modalBackdrop.classList.add('open');
        }
    }

    function closeModal() {
        if (modalBackdrop) {
            modalBackdrop.classList.remove('open');
            if (themeSelect) {
                document.documentElement.setAttribute('data-theme', themeSelect.dataset.savedTheme || initialTheme);
            }
        }
    }

    if (themeSelect) {
        themeSelect.dataset.savedTheme = initialTheme;

        themeSelect.addEventListener('change', function () {
            document.documentElement.setAttribute('data-theme', themeSelect.value);
        });
    }

    if (openButton) {
        openButton.addEventListener('click', openModal);
    }

    if (closeButton) {
        closeButton.addEventListener('click', closeModal);
    }

    if (cancelButton) {
        cancelButton.addEventListener('click', closeModal);
    }

    if (modalBackdrop) {
        modalBackdrop.addEventListener('click', function (e) {
            if (e.target === modalBackdrop) {
                closeModal();
            }
        });
    }

    if (avatarUpload) {
        avatarUpload.addEventListener('change', function () {
            const file = avatarUpload.files && avatarUpload.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = function (event) {
                const imageHtml = `<img src="${event.target.result}" alt="Аватар">`;

                if (modalAvatarPreview) {
                    modalAvatarPreview.innerHTML = imageHtml;
                }

                if (headerAvatarPreview) {
                    headerAvatarPreview.innerHTML = imageHtml;
                }
            };

            reader.readAsDataURL(file);
        });
    }

    if (settingsForm) {
        settingsForm.addEventListener('submit', function (e) {
            const confirmed = window.confirm(
                'Вы уверены что хотите изменить данные профиля? Администратор системы получит сообщение о смене данных.'
            );

            if (!confirmed) {
                e.preventDefault();
                return;
            }
        });
    }
});