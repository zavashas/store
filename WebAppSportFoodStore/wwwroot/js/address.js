async function submitAddressForm() {
    const userId = document.body.dataset.userid;
    const form = document.getElementById('address-form');
    const formData = new FormData(form);

    const data = {
        userId: parseInt(userId),
        city: formData.get("city"),
        street: formData.get("street"),
        house: formData.get("house"),
        house: formData.get("apartament"),
        courierComment: formData.get("courierComment")
    };

    const response = await fetch('/api/UserAddresses', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });

    if (response.ok) {
        alert("Адрес успешно добавлен");
        loadUserAddresses();
        bootstrap.Modal.getInstance(document.getElementById("addAddressModal")).hide();
    } else {
        const error = await response.text();
        alert("Ошибка при добавлении адреса:\n" + error);
    }
}

async function loadUserAddresses() {
    const userId = document.body.dataset.userid;
    const res = await fetch(`/api/UserAddresses/user/${userId}`);
    const addresses = await res.json();

    const select = document.querySelector('select[name="addressId"]');
    select.innerHTML = "";

    if (!addresses.length) {
        select.innerHTML = `<option disabled selected>Нет доступных адресов</option>`;
    } else {
        for (const a of addresses) {
            const opt = document.createElement('option');
            opt.value = a.idAddress;
            opt.textContent = `г. ${a.city}, ул. ${a.street}, д. ${a.house}`;
            select.appendChild(opt);
        }
    }
}



