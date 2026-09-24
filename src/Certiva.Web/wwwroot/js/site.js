// Site-wide scripts live here. Exam-taking behavior is isolated in exams.js.

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-bs-toggle='collapse']").forEach(toggle => {
        const selector = toggle.getAttribute("data-bs-target");
        const panel = selector ? document.querySelector(selector) : null;
        if (!panel) return;

        toggle.addEventListener("click", () => {
            const expanded = toggle.getAttribute("aria-expanded") === "true";
            toggle.setAttribute("aria-expanded", String(!expanded));
            panel.classList.toggle("show", !expanded);
        });
    });
});
