document.querySelectorAll('.quantity-change').forEach(btn => {
            btn.addEventListener('click', async () => {
                const row = btn.closest('tr');
                const cartId = row.dataset.id;
                const delta = parseInt(btn.dataset.delta);
                const currentQty = parseInt(row.dataset.quantity);

                // Если нажали "-" и количество == 1 → удаляем товар
                if (delta === -1 && currentQty === 1) {
                    const res = await fetch(`/Cart/RemoveItem?cartId=${cartId}`, {
                        method: 'POST'
                    });

                    if (res.ok) location.reload();
                    return;
                }

                // Если нажали "+" и достигли максимума → ничего не делаем
                const maxQty = parseInt(row.dataset.max);
                if (delta === 1 && currentQty >= maxQty) return;

                const res = await fetch(`/Cart/UpdateQuantity?cartId=${cartId}&delta=${delta}`, {
                    method: 'POST'
                });

                if (res.ok) location.reload();
            });
        });

        document.querySelectorAll('.remove-item').forEach(btn => {
            btn.addEventListener('click', async () => {
                const row = btn.closest('tr');
                const cartId = row.dataset.id;

                const res = await fetch(`/Cart/RemoveItem?cartId=${cartId}`, {
                    method: 'POST'
                });

                if (res.ok) location.reload();
            });
        });

        document.getElementById('clear-cart')?.addEventListener('click', async () => {
            const res = await fetch(`/Cart/Clear`, { method: 'POST' });
            if (res.ok) location.reload();
        });