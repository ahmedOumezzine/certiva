(() => {
    'use strict';

    const pageSize = 10;
    const localization = document.querySelector('.admin-attempt-detail')?.dataset || {};
    const list = document.querySelector('[data-question-list]');

    if (list) {
        const items = Array.from(list.querySelectorAll('[data-question-item]'));
        const pagination = document.querySelector('[data-question-pagination]');
        const emptyState = document.querySelector('[data-question-filter-empty]');
        const filterButtons = Array.from(document.querySelectorAll('[data-question-filter]'));
        const expandButton = document.querySelector('[data-toggle-all-questions]');
        let activeFilter = 'all';
        let currentPage = 1;

        const filteredItems = () => items.filter(item => activeFilter === 'all' || item.dataset.questionResult === activeFilter);
        const visibleItems = () => filteredItems().slice((currentPage - 1) * pageSize, currentPage * pageSize);

        const updateExpandLabel = () => {
            const visible = visibleItems();
            expandButton.textContent = visible.some(item => !item.open) ? localization.expandAll : localization.collapseAll;
        };

        const renderPage = () => {
            const filtered = filteredItems();
            const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
            currentPage = Math.min(currentPage, pages);
            const visible = visibleItems();
            const visibleSet = new Set(visible);

            items.forEach(item => { item.hidden = !visibleSet.has(item); });
            emptyState.hidden = filtered.length > 0;
            pagination.hidden = pages <= 1;
            pagination.replaceChildren();

            if (pages > 1) {
                const addPageButton = (label, page, disabled, current = false) => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.textContent = label;
                    button.disabled = disabled;
                    if (current) button.setAttribute('aria-current', 'page');
                    button.addEventListener('click', () => {
                        currentPage = page;
                        renderPage();
                    });
                    pagination.append(button);
                };

                addPageButton(localization.previous, currentPage - 1, currentPage === 1);
                const pageSet = [...new Set([1, currentPage - 1, currentPage, currentPage + 1, pages])]
                    .filter(page => page >= 1 && page <= pages)
                    .sort((left, right) => left - right);
                let previousPage = 0;
                pageSet.forEach(page => {
                    if (page - previousPage > 1) {
                        const ellipsis = document.createElement('span');
                        ellipsis.textContent = '…';
                        ellipsis.setAttribute('aria-hidden', 'true');
                        pagination.append(ellipsis);
                    }
                    addPageButton(String(page), page, false, page === currentPage);
                    previousPage = page;
                });
                addPageButton(localization.next, currentPage + 1, currentPage === pages);
            }

            updateExpandLabel();
        };

        filterButtons.forEach(button => button.addEventListener('click', () => {
            activeFilter = button.dataset.questionFilter;
            currentPage = 1;
            filterButtons.forEach(filterButton => {
                const isActive = filterButton === button;
                filterButton.classList.toggle('is-active', isActive);
                filterButton.setAttribute('aria-pressed', String(isActive));
            });
            renderPage();
        }));

        expandButton.addEventListener('click', () => {
            const visible = visibleItems();
            const shouldExpand = visible.some(item => !item.open);
            visible.forEach(item => { item.open = shouldExpand; });
            updateExpandLabel();
        });

        list.addEventListener('toggle', updateExpandLabel, true);
        renderPage();
    }

    document.addEventListener('click', async event => {
        const button = event.target.closest('[data-copy-attempt-id]');
        if (!button) return;

        const feedback = document.querySelector('[data-attempt-copy-feedback]');
        try {
            await navigator.clipboard.writeText(button.dataset.copyAttemptId);
            feedback.textContent = localization.copySuccess;
        } catch {
            feedback.textContent = localization.copyFailure;
        }
    });
})();
