
const defaultApiBase = 'https://localhost:7178'; // замените, если ваш API на другом порту
const apiBaseRaw = (window && window.apiBaseUrl) ? window.apiBaseUrl : '';
const apiBase = (apiBaseRaw || defaultApiBase).replace(/\/$/, '');

console.info('[site.js] apiBase =', apiBase);

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

        setGroupDisabled(true);
        setCourseValue('');

        specialtySelect.addEventListener('change', async () => {
            const val = specialtySelect.value;
            if (!val) {
                setGroupDisabled(true);
                setCourseValue('');
                return;
            }
            setGroupDisabled(true);
            await loadGroups(val);
        });
    } catch (err) {
        console.error('loadSpecialties error', err);
    }
}

async function loadGroups(specialtyId) {
    const groupSelect = document.getElementById('groupSelect');
    if (!groupSelect) return;

    groupSelect.innerHTML = '<option value="">-- загрузка групп --</option>';
    setCourseValue('');

    try {
        const res = await fetch(`${apiBase}/api/Helping/groups?specialtyId=${encodeURIComponent(specialtyId)}`);
        if (!res.ok) {
            console.error('groups fetch failed', res.status, res.statusText);
            groupSelect.innerHTML = '<option value="">-- нет групп --</option>';
            setGroupDisabled(true);
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
    }
}

function setGroupDisabled(disabled) {
    const groupSelect = document.getElementById('groupSelect');
    if (!groupSelect) return;
    groupSelect.disabled = !!disabled;
}

function setCourseValue(val) {
    const courseInput = document.getElementById('courseDisplay');
    if (!courseInput) return;
    courseInput.value = val ?? '';
    courseInput.disabled = true;
}

document.addEventListener("DOMContentLoaded", function () {
    setGroupDisabled(true);
    setCourseValue('');
    if (window.loadSpecialties) loadSpecialties();
});

window.loadSpecialties = loadSpecialties;
window.loadGroups = loadGroups;
