document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector('#addCardModal form');
    const expiryInput = document.getElementById("Expiry");
    const cvvInput = document.getElementById("CVV");

    // Ограничить ввод цифрами
    function allowOnlyDigits(input, maxLength) {
        input.addEventListener("keydown", (e) => {
            const allowed = ["Backspace", "Delete", "ArrowLeft", "ArrowRight", "Tab"];
            const isDigit = /^\d$/.test(e.key);
            if (!isDigit && !allowed.includes(e.key)) {
                e.preventDefault();
            }
        });

        input.addEventListener("input", (e) => {
            e.target.value = e.target.value.replace(/\D/g, "").slice(0, maxLength);
        });
    }

    allowOnlyDigits(cvvInput, 3);

    // Валидация срока действия
    expiryInput.addEventListener("input", (e) => {
        let value = e.target.value.replace(/\D/g, "");
        if (value.length >= 2) {
            value = value.slice(0, 2) + "/" + value.slice(2, 4);
        }
        expiryInput.value = value.slice(0, 5);
    });

    // Отправка формы с проверкой
    form.addEventListener("submit", (e) => {
        const cardParts = [
            document.getElementById("part1"),
            document.getElementById("part2"),
            document.getElementById("part3"),
            document.getElementById("part4")
        ];
        const expiry = expiryInput.value.trim();
        const cvv = cvvInput.value.trim();

        let valid = true;

        cardParts.forEach(part => {
            if (!/^\d{4}$/.test(part.value)) {
                part.classList.add("is-invalid");
                valid = false;
            } else {
                part.classList.remove("is-invalid");
            }
        });

        if (!/^\d{2}\/\d{2}$/.test(expiry)) {
            expiryInput.classList.add("is-invalid");
            valid = false;
        } else {
            expiryInput.classList.remove("is-invalid");
        }

        if (!/^\d{3}$/.test(cvv)) {
            cvvInput.classList.add("is-invalid");
            valid = false;
        } else {
            cvvInput.classList.remove("is-invalid");
        }

        if (!valid) {
            e.preventDefault();
            alert("Проверьте корректность всех полей");
        }
    });
});
