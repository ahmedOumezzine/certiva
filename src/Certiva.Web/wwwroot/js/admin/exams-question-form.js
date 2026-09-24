(function () {
    'use strict';

    function initQuestionTypeForm() {
        const form = document.getElementById('questionForm');
        const typeSelect = document.getElementById('QuestionType');
        if (!form || !typeSelect) return;

        const originalType = typeSelect.value;
        let confirmedType = originalType;

        typeSelect.addEventListener('change', function () {
            if (!confirmTypeChangeIfExistingAnswers(form, confirmedType, typeSelect.value)) {
                typeSelect.value = confirmedType;
                return;
            }
            confirmedType = typeSelect.value;
            onQuestionTypeChanged(form, typeSelect);
        });

        form.addEventListener('click', function (event) {
            const target = event.target;
            if (target.matches('[data-add-choice]')) addChoice(target.closest('[data-type-panel]'));
            if (target.matches('[data-remove-row]')) removeChoice(target);
            if (target.matches('[data-add-pair]')) addDragDropPair(form);
            if (target.matches('[data-add-grouped]')) addGroupedChoice(form);
        });

        form.addEventListener('change', function (event) {
            if (event.target.matches('[data-choice-correct-radio]')) syncSingleChoiceCorrect(event.target.closest('[data-choice-list]'));
            if (event.target.matches('input[name="trueFalseCorrect"]')) syncTrueFalseCorrect(form);
        });

        form.addEventListener('submit', function (event) {
            if (!validateBeforeSubmit(form, typeSelect.value)) {
                event.preventDefault();
                event.stopPropagation();
            }
        });

        onQuestionTypeChanged(form, typeSelect);
    }

    function currentTypeName(typeValue) {
        const map = {
            '0': 'SingleChoice',
            '1': 'MultiChoice',
            '2': 'TrueFalse',
            '3': 'ShortAnswer',
            '4': 'LongAnswer',
            '5': 'FillInBlank',
            '6': 'DragAndDrop',
            '7': 'DragAndDrop2'
        };
        return map[typeValue] || typeValue;
    }

    function displayTypeName(typeValue, form) {
        const labels = {
            '0': form.dataset.typeSingle,
            '1': form.dataset.typeMultiple,
            '2': form.dataset.typeTrueFalse,
            '3': form.dataset.typeShort,
            '4': form.dataset.typeLong,
            '5': form.dataset.typeBlank,
            '6': form.dataset.typeMatch,
            '7': form.dataset.typeGroup
        };
        return labels[typeValue] || currentTypeName(typeValue);
    }

    function onQuestionTypeChanged(form, typeSelect) {
        const activeType = currentTypeName(typeSelect.value);
        document.getElementById('questionTypeBadge').textContent = displayTypeName(typeSelect.value, form);

        form.querySelectorAll('[data-type-panel]').forEach((panel) => {
            const isActive = panel.dataset.typePanel === activeType;
            panel.hidden = !isActive;
            panel.querySelectorAll('input, textarea, select, button').forEach((field) => {
                if (field === typeSelect) return;
                field.disabled = !isActive;
            });
        });

        const activePanel = form.querySelector(`[data-type-panel="${activeType}"]`);
        renderChoiceEditor(activePanel);
        syncTrueFalseCorrect(form);
    }

    function renderChoiceEditor(panel) {
        if (!panel) return;
        const list = panel.querySelector('[data-choice-list]');
        if (!list) return;
        reindexRows(list, 'Choices');
        if (list.dataset.correctMode === 'single') syncSingleChoiceCorrect(list);
    }

    function addChoice(panel) {
        const list = panel?.querySelector('[data-choice-list]');
        if (!list) return;
        const index = list.querySelectorAll('.choice-row-modern').length;
        const isSingle = list.dataset.correctMode === 'single';
        const row = document.createElement('div');
        row.className = 'choice-row-modern';
        row.innerHTML = isSingle
            ? `<input type="hidden" name="Choices[${index}].Id" value="00000000-0000-0000-0000-000000000000" />
               <label>${form.dataset.answerFr} <input name="Choices[${index}].ChoiceText" placeholder="${form.dataset.answerPlaceholderFr}" /></label>
               <label>${form.dataset.answerEn} <input name="Choices[${index}].EnglishChoiceText" lang="en" placeholder="${form.dataset.answerPlaceholderEn}" /></label>
               <label class="radio-card"><input type="radio" name="singleCorrectChoice" data-choice-correct-radio /> ${form.dataset.correct}</label>
               <input type="hidden" name="Choices[${index}].IsCorrect" value="false" data-choice-correct />
               <button type="button" class="remove-choice" data-remove-row>${form.dataset.remove}</button>`
            : `<input type="hidden" name="Choices[${index}].Id" value="00000000-0000-0000-0000-000000000000" />
               <label>${form.dataset.answerFr} <input name="Choices[${index}].ChoiceText" placeholder="${form.dataset.answerPlaceholderFr}" /></label>
               <label>${form.dataset.answerEn} <input name="Choices[${index}].EnglishChoiceText" lang="en" placeholder="${form.dataset.answerPlaceholderEn}" /></label>
               <label class="checkline"><input type="checkbox" name="Choices[${index}].IsCorrect" value="true" /> ${form.dataset.correct}</label>
               <input type="hidden" name="Choices[${index}].IsCorrect" value="false" />
               <button type="button" class="remove-choice" data-remove-row>${form.dataset.remove}</button>`;
        list.appendChild(row);
        renderChoiceEditor(panel);
    }

    function removeChoice(button) {
        const row = button.closest('.choice-row-modern, .pair-row');
        const list = row?.parentElement;
        row?.remove();
        if (!list) return;
        if (list.matches('[data-choice-list]')) reindexRows(list, 'Choices');
        if (list.matches('[data-pair-list]')) reindexRows(list, 'DragDropPairs');
        if (list.matches('[data-grouped-list]')) reindexRows(list, 'GroupedChoices');
    }

    function addDragDropPair(form) {
        const list = form.querySelector('[data-pair-list]');
        const index = list.querySelectorAll('.pair-row').length;
        const row = document.createElement('div');
        row.className = 'pair-row';
        row.innerHTML = `<input type="hidden" name="DragDropPairs[${index}].Id" value="00000000-0000-0000-0000-000000000000" />
            <label>${form.dataset.source} <input name="DragDropPairs[${index}].Source" placeholder="${form.dataset.sourceExample}" /></label>
            <label>${form.dataset.destination} <input name="DragDropPairs[${index}].Target" placeholder="${form.dataset.destinationExample}" /></label>
            <label>${form.dataset.source} (${form.dataset.languageEn}) <input name="DragDropPairs[${index}].EnglishSource" lang="en" /></label>
            <label>${form.dataset.destination} (${form.dataset.languageEn}) <input name="DragDropPairs[${index}].EnglishTarget" lang="en" /></label>
            <button type="button" class="remove-choice" data-remove-row>${form.dataset.remove}</button>`;
        list.appendChild(row);
    }

    function removeDragDropPair(button) {
        removeChoice(button);
    }

    function addGroupedChoice(form) {
        const list = form.querySelector('[data-grouped-list]');
        const index = list.querySelectorAll('.pair-row').length;
        const row = document.createElement('div');
        row.className = 'pair-row';
        row.innerHTML = `<input type="hidden" name="GroupedChoices[${index}].Id" value="00000000-0000-0000-0000-000000000000" />
            <label>${form.dataset.item} <input name="GroupedChoices[${index}].Item" placeholder="ASP.NET Core" /></label>
            <label>${form.dataset.group} <input name="GroupedChoices[${index}].Group" placeholder="${form.dataset.groupExample}" /></label>
            <label>${form.dataset.item} (${form.dataset.languageEn}) <input name="GroupedChoices[${index}].EnglishItem" lang="en" /></label>
            <label>${form.dataset.group} (${form.dataset.languageEn}) <input name="GroupedChoices[${index}].EnglishGroup" lang="en" /></label>
            <button type="button" class="remove-choice" data-remove-row>${form.dataset.remove}</button>`;
        list.appendChild(row);
    }

    function removeGroupedChoice(button) {
        removeChoice(button);
    }

    function confirmTypeChangeIfExistingAnswers(form, previousType, nextType) {
        if (previousType === nextType) return true;
        const previousPanel = form.querySelector(`[data-type-panel="${currentTypeName(previousType)}"]`);
        if (!previousPanel) return true;
        const hasData = Array.from(previousPanel.querySelectorAll('input:not([type="hidden"]), textarea'))
            .some((field) => field.type === 'checkbox' || field.type === 'radio' ? field.checked : field.value.trim().length > 0);
        if (!hasData) return true;
        return confirm(form.dataset.confirmTypeChange);
    }

    function validateBeforeSubmit(form, typeValue) {
        clearClientErrors(form);
        const type = currentTypeName(typeValue);
        const panel = form.querySelector(`[data-type-panel="${type}"]`);
        let ok = true;

        function fail(message) {
            ok = false;
            const box = document.createElement('div');
            box.className = 'client-validation-error';
            box.textContent = message;
            panel?.prepend(box);
        }

        if (type === 'SingleChoice') {
            const rows = answeredRows(panel, '.choice-row-modern');
            if (rows.length < 2) fail(form.dataset.validationAddTwo);
            if (rows.filter(r => r.querySelector('[data-choice-correct]')?.value === 'true').length !== 1) fail(form.dataset.validationExactlyOne);
        }

        if (type === 'MultiChoice') {
            const rows = answeredRows(panel, '.choice-row-modern');
            if (rows.length < 2) fail(form.dataset.validationAddTwo);
            if (!rows.some(r => r.querySelector('input[type="checkbox"]')?.checked)) fail(form.dataset.validationSelectOne);
        }

        if (type === 'ShortAnswer' && !form.querySelector('[name="ExpectedAnswer"]:not(:disabled)')?.value.trim()) fail(form.dataset.validationAnswerRequired);
        if (type === 'FillInBlank' && !form.querySelector('[name="FillInBlank"]:not(:disabled)')?.value.trim()) fail(form.dataset.validationBlankRequired);

        if (type === 'DragAndDrop' && answeredRows(panel, '.pair-row').length < 2) fail(form.dataset.validationTwoPairs);
        if (type === 'DragAndDrop2') {
            const rows = answeredRows(panel, '.pair-row');
            const groups = new Set(rows.map(r => r.querySelector('[name$=".Group"]')?.value.trim()).filter(Boolean));
            if (rows.length < 2) fail(form.dataset.validationTwoItems);
            if (groups.size < 2) fail(form.dataset.validationTwoGroups);
        }

        return ok;
    }

    function answeredRows(panel, selector) {
        return Array.from(panel?.querySelectorAll(selector) || []).filter((row) => {
            return Array.from(row.querySelectorAll('input:not([type="hidden"]), textarea')).some(input => input.value.trim().length > 0 || input.checked);
        });
    }

    function clearClientErrors(form) {
        form.querySelectorAll('.client-validation-error').forEach(e => e.remove());
    }

    function syncSingleChoiceCorrect(list) {
        if (!list) return;
        list.querySelectorAll('.choice-row-modern').forEach((row) => {
            const radio = row.querySelector('[data-choice-correct-radio]');
            const hidden = row.querySelector('[data-choice-correct]');
            if (hidden) hidden.value = radio?.checked ? 'true' : 'false';
        });
    }

    function syncTrueFalseCorrect(form) {
        const selected = form.querySelector('input[name="trueFalseCorrect"]:checked')?.value || 'True';
        form.querySelectorAll('[data-tf-correct]').forEach(input => {
            input.value = input.dataset.tfCorrect === selected ? 'true' : 'false';
        });
    }

    function reindexRows(list, prefix) {
        const rows = list.querySelectorAll('.choice-row-modern, .pair-row');
        rows.forEach((row, index) => {
            row.querySelectorAll('input, textarea, select').forEach((input) => {
                if (!input.name || input.name === 'singleCorrectChoice') return;
                input.name = input.name.replace(new RegExp(`${prefix}\\[\\d+\\]`), `${prefix}[${index}]`);
            });
        });
    }

    window.initQuestionTypeForm = initQuestionTypeForm;
    window.onQuestionTypeChanged = onQuestionTypeChanged;
    window.renderChoiceEditor = renderChoiceEditor;
    window.addChoice = addChoice;
    window.removeChoice = removeChoice;
    window.validateBeforeSubmit = validateBeforeSubmit;
    window.addDragDropPair = addDragDropPair;
    window.removeDragDropPair = removeDragDropPair;
    window.addGroupedChoice = addGroupedChoice;
    window.removeGroupedChoice = removeGroupedChoice;
    window.confirmTypeChangeIfExistingAnswers = confirmTypeChangeIfExistingAnswers;

    document.addEventListener('DOMContentLoaded', initQuestionTypeForm);
})();
