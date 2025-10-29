document.addEventListener('keydown', e => {
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

    // Сочетания
    const k = e.key.toLowerCase();
    const ctrl = e.ctrlKey || e.metaKey;
    const shift = e.shiftKey;
    const alt = e.altKey;

    if (ctrl && k === 's' && !shift) { e.preventDefault(); globalHotkeySave(); }
    else if (ctrl && shift && k === 's') { e.preventDefault(); saveAll(); }
    else if (ctrl && k === 'f') { e.preventDefault(); focusSearch(); }
    else if (ctrl && k === 'q') { e.preventDefault(); createNew(); }
    else if (ctrl && k === 'e') { e.preventDefault(); toggleEdit(); }
    else if (ctrl && k === 'p') { e.preventDefault(); printCurrent(); }
    else if (ctrl && k === 'delete') { e.preventDefault(); deleteSelected(); }
    else if (ctrl && alt && k === 'l') { e.preventDefault(); toggleLogPanel(); }
    else if (k === 'escape') { closeModals(); }
});


document.addEventListener('keydown', function (e) {
    const key = e.key.toLowerCase();
    const ctrl = e.ctrlKey || e.metaKey;
    const shift = e.shiftKey;

    // === Ctrl+Shift+S — сохранить ===
    if (ctrl && shift && key === 's') {
        e.preventDefault(); // отключаем стандартное сохранение страницы

        const active = document.activeElement;
        let form = active && active.closest('form');
        if (!form) form = document.querySelector('form');

        if (form) {
            const btn = form.querySelector('[type="submit"], .save-btn');
            if (btn) {
                btn.click();
                showToast?.('Сохранение (Ctrl + Shift + S)…', 'info');
            } else {
                form.requestSubmit?.();
                showToast?.('Форма отправлена (Ctrl + Shift + S)', 'info');
            }
        } else {
            showToast?.('Форма не найдена', 'danger');
        }
    }
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
    if (ctrl && key === 'q') {               // Новая запись
        e.preventDefault();
    const addBtn = document.querySelector('.action-bar .btn-primary');
    if (addBtn) addBtn.click();
    }

    // === Дашборд ===
    if (ctrl && !alt && !e.shiftKey && ['1','2','3','4','5'].includes(key)) {
        e.preventDefault();
    const links = document.querySelectorAll('.admin-grid .admin-card');
    const index = parseInt(key) - 1;
    if (links[index]) links[index].click();
    }
});
