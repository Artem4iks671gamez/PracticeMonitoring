(function () {
    function getInitials(value) {
        const rawValue = String(value || '?').trim();
        if (rawValue && !/\s/.test(rawValue) && rawValue.length <= 3) {
            return rawValue.toUpperCase();
        }

        const initials = rawValue
            .split(/\s+/)
            .filter(Boolean)
            .slice(0, 2)
            .map(part => part[0])
            .join('')
            .toUpperCase();

        return initials || '?';
    }

    function replaceBrokenAvatar(image) {
        if (!image || image.dataset.avatarFallbackApplied === 'true') {
            return;
        }

        image.dataset.avatarFallbackApplied = 'true';

        const fallback = document.createElement('span');
        fallback.textContent = getInitials(image.dataset.avatarInitials || image.alt);

        image.replaceWith(fallback);
    }

    document.addEventListener('error', event => {
        const target = event.target;
        if (target instanceof HTMLImageElement && target.matches('img[data-avatar-fallback]')) {
            replaceBrokenAvatar(target);
        }
    }, true);

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('img[data-avatar-fallback]').forEach(image => {
            if (image.complete && image.naturalWidth === 0) {
                replaceBrokenAvatar(image);
            }
        });
    });
})();
