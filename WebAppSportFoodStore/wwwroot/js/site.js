// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function getRequestVerificationToken() {
    const f = document.getElementById('__af');
    const input = f && f.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : null;
}

document.addEventListener('click', async (e) => {
    const btn = e.target.closest('.js-fav-toggle');
    if (!btn) return;

    e.preventDefault();
    e.stopPropagation();

    const productId = btn.dataset.productId;
    if (!productId) return;

    const form = new FormData();
    form.append('productId', productId);

    // анти-CSRF
    const token = getRequestVerificationToken();
    if (token) form.append('__RequestVerificationToken', token);

    let resp;
    try {
        resp = await fetch('/Favorites/Toggle', {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: form
        });
    } catch {
        alert('Сетевая ошибка при изменении избранного');
        return;
    }

    if (resp.status === 401) { window.location.href = '/Account/Authorization'; return; }
    if (!resp.ok) {
        const msg = await resp.text();
        alert(msg || 'Не удалось изменить избранное');
        return;
    }

    // Не все бэки выставляют правильный content-type. Попробуем аккуратно прочесть JSON:
    let data = null;
    const ct = resp.headers.get('content-type') || '';
    if (ct.includes('application/json')) {
        data = await resp.json().catch(() => null);
    } else {
        const text = await resp.text().catch(() => '');
        try { data = JSON.parse(text); } catch { data = null; }
    }

    // Если JSON не распарсился, всё равно считаем успехом (DB уже обновлена)
    const active = data && typeof data.inFavorites === 'boolean'
        ? data.inFavorites
        : !btn.classList.contains('active');

    btn.classList.toggle('active', active);
    btn.setAttribute('aria-pressed', active ? 'true' : 'false');
});

document.addEventListener('click', async (e) => {
    const btn = e.target.closest('.favorite-toggle');
    if (!btn) return;
    e.preventDefault();         // чтобы не открывалась ссылка
    e.stopPropagation();        // чтобы клик не ушёл на карточку/ссылку

    const id = btn.dataset.id;
    const resp = await fetch('/Favorites/Toggle', {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: new URLSearchParams({ productId: id })
    });
    if (resp.status === 401) { window.location.href = '/Account/Authorization'; return; }
    const data = await resp.json();
    const active = !!data.inFavorites;
    btn.classList.toggle('active', active);
    btn.setAttribute('aria-pressed', active ? 'true' : 'false');
});

    document.addEventListener('keydown', function (e) {
    const key = e.key.toLowerCase();
    const ctrl = e.ctrlKey || e.metaKey;
    const alt  = e.altKey;

    // === Универсальные ===
    if (ctrl && key === 'f') {               // Поиск
        e.preventDefault();
    const input = document.getElementById('searchInput');
    if (input) {input.focus(); input.select(); }
    }

    if (key === 'escape') {                  // Очистка поиска
        const input = document.getElementById('searchInput');
    if (input && input.value) {
        input.value = '';
    if (typeof filterTable === 'function') filterTable();
        }
    }

    // === Таблица ===
    if (ctrl && key === '') {               // Новая запись
        e.preventDefault();
    const addBtn = document.querySelector('.action-bar .btn-primary');
    if (addBtn) addBtn.click();
    }

    if (key === 'delete') {                  // Удаление
        const focusedRow = document.activeElement.closest('tr');
    const delBtn = focusedRow?.querySelector('form button.btn-danger');
    if (delBtn && confirm('Удалить выбранную запись?')) delBtn.click();
    }

    if (key === 'enter') {                   // Редактирование
        const focusedRow = document.activeElement.closest('tr');
    const editBtn = focusedRow?.querySelector('a.btn-primary');
    if (editBtn) editBtn.click();
    }

    // === Дашборд ===
    if (ctrl && !alt && !e.shiftKey && ['1','2','3','4','5'].includes(key)) {
        e.preventDefault();
    const links = document.querySelectorAll('.admin-grid .admin-card');
    const index = parseInt(key) - 1;
    if (links[index]) links[index].click();
    }
});
