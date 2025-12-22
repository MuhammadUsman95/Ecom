if (!window.layoutJsInitialized) {
    window.layoutJsInitialized = true;
    const userId = localStorage.getItem("userId") || "";

    async function initializeLayout(data) {
        const { userId } = data;

        if (!userId) {
            window.location.href = "/Login";
            return;
        }

        // Update UI
        document.querySelectorAll('a.js-acc-btn, a#userName, img[alt="Loading"]').forEach(el => {
            if (el.tagName === "IMG") {
                el.alt = userId;
            } else {
                el.textContent = userId;
            }
        });

        
        // Logout

        document.getElementById("logoutBtn")?.addEventListener("click", function (e) {
            e.preventDefault();

            fetch('/Login/Logout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(userId)  // userId send
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        // LocalStorage clear
                        localStorage.clear();
                        // Redirect to login
                        window.location.href = '/Login';
                    }
                })
                .catch(() => window.location.href = '/Login');
        });

    }

    fetch('/Login/LayoutLoad', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(userId) // <-- directly string
    }).then(res => res.json()).then(data => {
        if (data.loggedIn) {
            initializeLayout(data);
        } else {
            window.location.href = "/Login";
        }
    }).catch(() => window.location.href = "/Login");

}
