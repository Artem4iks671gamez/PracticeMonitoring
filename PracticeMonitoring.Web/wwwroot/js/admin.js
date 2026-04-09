document.addEventListener('DOMContentLoaded', function () {
    const buttons = document.querySelectorAll('.admin-tab-button');
    const tabs = document.querySelectorAll('.admin-console');

    buttons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = button.dataset.tab;

            buttons.forEach(x => x.classList.remove('active'));
            tabs.forEach(x => x.classList.remove('active'));

            button.classList.add('active');

            const target = document.getElementById(targetId);
            if (target) {
                target.classList.add('active');
            }
        });
    });
});
