(function () {
    var KEY = "theme";
    var saved = null;
    try { saved = localStorage.getItem(KEY); } catch (e) { }

    var systemDark = window.matchMedia &&
        window.matchMedia("(prefers-color-scheme: dark)").matches;

    document.documentElement.setAttribute(
        "data-theme", saved || (systemDark ? "dark" : "light"));

    window.toggleTheme = function () {
        var next = document.documentElement.getAttribute("data-theme") === "dark"
            ? "light" : "dark";
        document.documentElement.setAttribute("data-theme", next);
        try { localStorage.setItem(KEY, next); } catch (e) { }
    };
})();