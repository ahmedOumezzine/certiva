(() => {
    const filters = [...document.querySelectorAll("[data-result-filter]")];
    const questions = [...document.querySelectorAll("[data-result-question]")];
    const empty = document.querySelector("[data-result-empty]");

    if (!filters.length || !questions.length) return;

    filters.forEach((filter) => {
        filter.addEventListener("click", () => {
            const selected = filter.dataset.resultFilter;
            let visibleCount = 0;

            filters.forEach((item) => {
                const active = item === filter;
                item.classList.toggle("is-active", active);
                item.setAttribute("aria-pressed", String(active));
            });

            questions.forEach((question) => {
                const visible = selected === "all" || question.dataset.result === selected;
                question.hidden = !visible;
                if (visible) visibleCount++;
            });

            if (empty) empty.hidden = visibleCount > 0;
        });
    });
})();
