async function login() {
    let response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            email: document.getElementById('logEmail').value,
            password: document.getElementById('logPassword').value
        })
    });

    if (response.ok) {
        window.location.href = "dashboard.html";
    } else {
        alert("Přihlášení selhalo!");
    }
}

async function register() {
    let email = document.getElementById('regEmail').value;
    let password = document.getElementById('regPassword').value;
    let confirmPassword = document.getElementById('regConfirmPassword').value;

    if (password !== confirmPassword) {
        alert("Hesla se neshodují!");
        return;
    }

    let response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });

    if (response.ok) {
        alert("Registrace úspěšná!");
        document.getElementById('registerModal').style.display = "none";
    } else {
        alert("Registrace selhala!");
    }
}

// Otevření a zavření modalu
document.addEventListener("DOMContentLoaded", function () {
    const registerBtn = document.getElementById('registerBtn');
    const registerModal = document.getElementById('registerModal');
    const closeModal = document.getElementById('closeModal');

    registerBtn.addEventListener('click', function () {
        registerModal.style.display = "flex";
    });

    closeModal.addEventListener('click', function () {
        registerModal.style.display = "none";
    });

    window.addEventListener('click', function (event) {
        if (event.target === registerModal) {
            registerModal.style.display = "none";
        }
    });
});
