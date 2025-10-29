function filterTable() {
    const input = document.getElementById('searchInput');
    const filter = input.value.toLowerCase();
    const table = document.getElementById('dataTable');
    const tr = table.getElementsByTagName('tr');
    let visibleCount = 0;

    for (let i = 1; i < tr.length; i++) {
        const td = tr[i].getElementsByTagName('td');
        let found = false;

        for (let j = 0; j < td.length - 1; j++) { // -1 чтобы исключить колонку действий
            if (td[j]) {
                const txtValue = td[j].textContent || td[j].innerText;
                if (txtValue.toLowerCase().indexOf(filter) > -1) {
                    found = true;
                    break;
                }
            }
        }

        if (found) {
            tr[i].style.display = '';
            visibleCount++;
        } else {
            tr[i].style.display = 'none';
        }
    }

    document.getElementById('recordCount').textContent = `Найдено записей: ${visibleCount}`;
}