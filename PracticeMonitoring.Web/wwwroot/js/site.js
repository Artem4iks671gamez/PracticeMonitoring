const apiBase = (window.apiBaseUrl || '').replace(/\/$/, '');

console.info('[site.js] apiBase =', apiBase);

function getCustomSelect(selectId) {
    return document.querySelector(`.custom-select[data-target="${selectId}"]`);
}

function buildCustomSelect(selectId) {
    const nativeSelect = document.getElementById(selectId);
    const custom = getCustomSelect(selectId);

    if (!nativeSelect || !custom) return;

    const trigger = custom.querySelector('.custom-select-trigger');
    const menu = custom.querySelector('.custom-select-menu');

    if (!trigger || !menu) return;

    menu.innerHTML = '';

    const options = Array.from(nativeSelect.options);

    options.forEach(opt => {
        const item = document.createElement('div');
        item.className = 'custom-select-option';
        item.textContent = opt.textContent;
        item.dataset.value = opt.value;

        if (opt.selected) {
            item.classList.add('selected');
            trigger.textContent = opt.textContent;
        }

        item.addEventListener('click', () => {
            nativeSelect.value = opt.value;
            nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));

            menu.querySelectorAll('.custom-select-option').forEach(x => x.classList.remove('selected'));
            item.classList.add('selected');
            trigger.textContent = opt.textContent;

            custom.classList.remove('open');
        });

        menu.appendChild(item);
    });

    if (!nativeSelect.value && options.length > 0) {
        trigger.textContent = options[0].textContent;
    }

    custom.classList.toggle('disabled', nativeSelect.disabled);
}

function initCustomSelect(selectId) {
    const nativeSelect = document.getElementById(selectId);
    const custom = getCustomSelect(selectId);

    if (!nativeSelect || !custom) return;

    const trigger = custom.querySelector('.custom-select-trigger');
    if (!trigger) return;

    trigger.addEventListener('click', () => {
        if (nativeSelect.disabled) return;

        document.querySelectorAll('.custom-select.open').forEach(x => {
            if (x !== custom) x.classList.remove('open');
        });

        custom.classList.toggle('open');
    });

    nativeSelect.addEventListener('change', () => {
        buildCustomSelect(selectId);
    });

    buildCustomSelect(selectId);
}

document.addEventListener('click', (e) => {
    if (!e.target.closest('.custom-select')) {
        document.querySelectorAll('.custom-select.open').forEach(x => x.classList.remove('open'));
    }
});

async function loadSpecialties() {
    try {
        const res = await fetch(`${apiBase}/api/Helping/specialties`);
        if (!res.ok) {
            console.error('specialties fetch failed', res.status, res.statusText);
            return;
        }

        const list = await res.json();
        const specialtySelect = document.getElementById('specialtySelect');
        if (!specialtySelect) return;

        specialtySelect.innerHTML = '<option value="">-- выберите специальность --</option>';

        list.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.id;
            opt.textContent = `${s.code} — ${s.name}`;
            specialtySelect.appendChild(opt);
        });

        buildCustomSelect('specialtySelect');
        setGroupDisabled(true);
        setCourseValue('');

        specialtySelect.onchange = async function () {
            const val = specialtySelect.value;

            if (!val) {
                setGroupDisabled(true);
                setCourseValue('');

                const groupSelect = document.getElementById('groupSelect');
                if (groupSelect) {
                    groupSelect.innerHTML = '<option value="">-- выберите группу --</option>';
                    buildCustomSelect('groupSelect');
                }

                return;
            }

            setGroupDisabled(true);
            await loadGroups(val);
        };
    } catch (err) {
        console.error('loadSpecialties error', err);
    }
}

async function loadGroups(specialtyId) {
    const groupSelect = document.getElementById('groupSelect');
    if (!groupSelect) return;

    groupSelect.innerHTML = '<option value="">-- загрузка групп --</option>';
    buildCustomSelect('groupSelect');
    setCourseValue('');

    try {
        const res = await fetch(`${apiBase}/api/Helping/groups?specialtyId=${encodeURIComponent(specialtyId)}`);
        if (!res.ok) {
            console.error('groups fetch failed', res.status, res.statusText);
            groupSelect.innerHTML = '<option value="">-- нет групп --</option>';
            setGroupDisabled(true);
            buildCustomSelect('groupSelect');
            return;
        }

        const list = await res.json();

        groupSelect.innerHTML = '<option value="">-- выберите группу --</option>';

        list.forEach(g => {
            const opt = document.createElement('option');
            opt.value = g.id;
            opt.textContent = `${g.name} (курс ${g.course})`;
            opt.setAttribute('data-course', g.course);
            groupSelect.appendChild(opt);
        });

        setGroupDisabled(list.length === 0);
        buildCustomSelect('groupSelect');

        groupSelect.onchange = function () {
            const selected = groupSelect.options[groupSelect.selectedIndex];
            if (!selected || !selected.value) {
                setCourseValue('');
                return;
            }

            const course = selected.getAttribute('data-course');
            setCourseValue(course ?? '');
        };
    } catch (err) {
        console.error('loadGroups error', err);
        groupSelect.innerHTML = '<option value="">-- ошибка загрузки --</option>';
        setGroupDisabled(true);
        buildCustomSelect('groupSelect');
    }
}

function setGroupDisabled(disabled) {
    const groupSelect = document.getElementById('groupSelect');
    if (!groupSelect) return;

    groupSelect.disabled = !!disabled;
    buildCustomSelect('groupSelect');
}

function setCourseValue(val) {
    const courseInput = document.getElementById('courseDisplay');
    if (!courseInput) return;

    courseInput.value = val ?? '';
    courseInput.disabled = true;
}

document.addEventListener('DOMContentLoaded', function () {
    setGroupDisabled(true);
    setCourseValue('');

    initCustomSelect('specialtySelect');
    initCustomSelect('groupSelect');

    if (window.loadSpecialties) {
        loadSpecialties();
    }
});

window.loadSpecialties = loadSpecialties;
window.loadGroups = loadGroups;