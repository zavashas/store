document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.add-to-cart-form').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const productId = this.dataset.productId;
            const userId = document.body.dataset.userid;
            if (!userId) {
                alert("Вы не авторизованы");
                return;
            }

            const response = await fetch('/Cart/AddToCart?productId=' + productId, {
                method: 'POST'
            });

            if (response.ok) {
                this.innerHTML = `
                    <button type="button" class="btn btn-success btn-sm w-100" disabled>
                        <i class="bi bi-cart-check-fill"></i> В корзине
                    </button>
                `;
            } else {
                const errorText = await response.text();
                alert("Ошибка при добавлении в корзину:\n" + errorText);
            }
        });
    });
});
