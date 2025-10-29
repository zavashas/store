document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.favorite-toggle-form').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const productId = this.dataset.productId;
            const token = this.querySelector('input[name="__RequestVerificationToken"]').value;
            const icon = this.querySelector('i');
            const isFavorited = icon.classList.contains('bi-heart-fill');

            const url = isFavorited ? `/Favorite/RemoveItem` : `/Favorite/Add`;

            const formData = new FormData();
            formData.append('productId', productId);

            try {
                const res = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token
                    },
                    body: formData
                });

                if (!res.ok) {
                    const errorText = await res.text();
                    alert("Ошибка при обновлении избранного:\n" + errorText);
                    return;
                }

                // Переключаем визуально иконку
                if (isFavorited) {
                    icon.classList.remove('bi-heart-fill', 'text-danger');
                    icon.classList.add('bi-heart');
                } else {
                    icon.classList.remove('bi-heart');
                    icon.classList.add('bi-heart-fill', 'text-danger');
                }

            } catch (err) {
                console.error(err);
                alert("Произошла ошибка при работе с избранным.");
            }
        });
    });
});
