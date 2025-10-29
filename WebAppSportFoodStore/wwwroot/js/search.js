const searchInput = document.getElementById("searchInput");
const sortSelect = document.getElementById("sortSelect");
const categorySelect = document.getElementById("categorySelect");
const filterForm = document.getElementById("filterForm");

let timer;

if (searchInput) {
    searchInput.addEventListener("input", () => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            filterForm.submit();
        }, 500); // Задержка для live-поиска
    });
}

if (sortSelect) {
    sortSelect.addEventListener("change", () => {
        filterForm.submit();
    });
}

if (categorySelect) {
    categorySelect.addEventListener("change", () => {
        filterForm.submit();
    });
}
