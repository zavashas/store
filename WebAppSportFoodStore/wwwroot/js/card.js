function restrictToDigits(inputElement, maxLength) {
    if (!inputElement) return;

    inputElement.addEventListener("keydown", (e) => {
        const allowedKeys = ["Backspace", "Delete", "ArrowLeft", "ArrowRight", "Tab"];
        const isNumber = /^\d$/.test(e.key);
        if (!isNumber && !allowedKeys.includes(e.key)) {
            e.preventDefault();
        }
    });

    inputElement.addEventListener("input", (e) => {
        e.target.value = e.target.value.replace(/\D/g, "").slice(0, maxLength);
    });
}

function initCardFieldValidation() {
    const part1 = document.getElementById("part1");
    const part2 = document.getElementById("part2");
    const part3 = document.getElementById("part3");
    const part4 = document.getElementById("part4");
    const expiryInput = document.getElementById("Expiry");
    const cvvInput = document.getElementById("CVV");

    restrictToDigits(part1, 4);
    restrictToDigits(part2, 4);
    restrictToDigits(part3, 4);
    restrictToDigits(part4, 4);
    restrictToDigits(cvvInput, 3);

    if (expiryInput) {
        expiryInput.addEventListener("keydown", (e) => {
            const allowedKeys = ["Backspace", "Delete", "ArrowLeft", "ArrowRight", "Tab"];
            const isNumber = /^\d$/.test(e.key);
            if (!isNumber && !allowedKeys.includes(e.key)) {
                e.preventDefault();
            }
        });

        expiryInput.addEventListener("input", (e) => {
            let value = e.target.value.replace(/\D/g, "");

            if (value.length === 0) return;
            if (value.length === 1 && parseInt(value[0]) > 1) value = "1";
            if (value.length === 2) {
                const month = parseInt(value);
                if (month < 1 || month > 12) value = value[0];
            }

            if (value.length > 2) {
                value = value.slice(0, 2) + "/" + value.slice(2, 4);
            }

            expiryInput.value = value.slice(0, 5);

            if (value.length >= 4) {
                const [mm, yy] = expiryInput.value.split("/");
                const month = parseInt(mm);
                const year = 2000 + parseInt(yy);

                const today = new Date();
                const currentMonth = today.getMonth() + 1;
                const currentYear = today.getFullYear();

                const isValid = !(year < currentYear || (year === currentYear && month < currentMonth));
                expiryInput.classList.toggle("is-invalid", !isValid);
                expiryInput.setCustomValidity(isValid ? "" : "Срок действия карты истёк");
            } else {
                expiryInput.classList.remove("is-invalid");
                expiryInput.setCustomValidity("");
            }
        });
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const modal = document.getElementById("addCardModal");
    if (modal) {
        modal.addEventListener("shown.bs.modal", initCardFieldValidation);
    } else {
        initCardFieldValidation(); 
    }
});
