window.BudgetTracker = window.BudgetTracker || {};

window.BudgetTracker.getCookie = function (name) {
    const nameEQ = name + "=";
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) === ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
};

window.BudgetTracker.storeCsrfToken = function (token) {
    localStorage.setItem('X-CSRF-TOKEN', token);
};

window.BudgetTracker.getCsrfToken = function () {
    return localStorage.getItem('X-CSRF-TOKEN');
};
