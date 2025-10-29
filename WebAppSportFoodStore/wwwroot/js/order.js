document.addEventListener('DOMContentLoaded', () => {
    const deliveryDateInput = document.getElementById('deliveryDate');
    if (deliveryDateInput) {
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1); // Завтра
        const minDate = tomorrow.toISOString().split('T')[0]; // YYYY-MM-DD
        deliveryDateInput.min = minDate;

        // Установка текущей даты как значения по умолчанию (если нужно)
        deliveryDateInput.value = minDate;
    }

    const submitBtn = document.getElementById('submit-order');
    if (!submitBtn) return;

    submitBtn.addEventListener('click', async (e) => {
        e.preventDefault();

        const addressId = parseInt(document.querySelector('select[name="addressId"]').value);
        const cardId = parseInt(document.querySelector('select[name="userCardId"]').value);
        const slotId = parseInt(document.querySelector('select[name="slotId"]').value);
        const deliveryDate = document.querySelector('input[name="deliveryDate"]').value;
        const userId = parseInt(document.body.dataset.userid);

        if (!addressId || !cardId || !slotId || !userId || !deliveryDate) {
            alert("Пожалуйста, заполните все поля.");
            return;
        }

        const selectedDate = new Date(deliveryDate);
        const tomorrow = new Date();
        tomorrow.setHours(0, 0, 0, 0);
        tomorrow.setDate(tomorrow.getDate() + 1);

        if (selectedDate < tomorrow) {
            alert("Выберите дату не раньше завтрашнего дня.");
            return;
        }

        const url = `http://localhost:5254/api/Orders/from-cart/${userId}`;

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ addressId, cardId, slotId, deliveryDate })
            });

            if (response.ok) {
                alert("Заказ успешно оформлен!");
                window.location.href = "/Catalog";
            } else {
                const error = await response.text();
                alert("Ошибка оформления заказа:\n" + error);
            }
        } catch (err) {
            alert("Сетевая ошибка: " + err.message);
        }
    });
});
