(function () {
    'use strict';

    const form = document.getElementById('examForm');
    if (!form) return;
    const text = window.examI18n || {};

    const shell = document.querySelector('.take-shell');
    const panels = Array.from(document.querySelectorAll('.question-panel'));
    const palette = Array.from(document.querySelectorAll('[data-jump]'));
    const counter = document.getElementById('questionCounter');
    const progress = document.getElementById('progressBar');
    const progressPercentage = document.getElementById('progressPercentage');
    const timer = document.getElementById('timer');
    const timerValue = document.getElementById('timerValue') || timer;
    const mobileNav = document.querySelector('.exam-question-nav-mobile');
    const submitDialog = document.getElementById('submitDialog');
    const finishButton = document.getElementById('finishExam');
    const storageKey = 'exam-session:' + (shell?.dataset.attemptId || 'unknown');
    const saveUrl = shell?.dataset.saveUrl;
    const attemptId = shell?.dataset.attemptId;
    const antiforgery = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const pending = {};
    const generations = new Map();
    const timers = new Map();
    const inFlightSaves = new Map();
    let index = 0;
    let remaining = Number(timer?.dataset.remainingSeconds || 0);
    let activeSaves = 0;
    let expirySubmitStarted = false;
    let submitConfirmed = false;
    let isFinalizing = false;
    let finalizationPromise = null;
    const hasDeadline = timer?.dataset.hasDeadline === 'True';
    const submitError = document.getElementById('submitSaveError');
    const finishButtons = [finishButton, document.getElementById('confirmSubmit')].filter(Boolean);

    function persistLocalCopy() {
        try {
            localStorage.setItem(storageKey, JSON.stringify({ index, pending }));
        } catch {
            // Local storage is a convenience; server autosave remains authoritative.
        }
    }

    function applyAnswer(panel, answer) {
        const selected = new Set(answer.selectedChoiceIds || []);
        panel.querySelectorAll('[data-attempt-choice-id]').forEach(field => {
            field.checked = selected.has(field.dataset.attemptChoiceId);
        });
        const textarea = panel.querySelector('textarea');
        if (textarea && typeof answer.textAnswer === 'string') textarea.value = answer.textAnswer;
    }

    function restoreLocalCopy() {
        try {
            const stored = JSON.parse(localStorage.getItem(storageKey) || '{}');
            index = Math.min(Math.max(Number(stored.index || 0), 0), Math.max(0, panels.length - 1));
            Object.assign(pending, stored.pending || {});
            panels.forEach(panel => {
                const questionId = panel.dataset.attemptQuestionId;
                if (questionId && pending[questionId]) applyAnswer(panel, pending[questionId]);
            });
        } catch {
            try { localStorage.removeItem(storageKey); } catch { /* Storage may be unavailable. */ }
        }
    }

    function collectAnswer(panel) {
        return {
            selectedChoiceIds: Array.from(panel.querySelectorAll('[data-attempt-choice-id]:checked'))
                .map(field => field.dataset.attemptChoiceId),
            textAnswer: panel.querySelector('textarea')?.value || ''
        };
    }

    function showFeedback(panel, message, state) {
        const status = panel.querySelector('.answer-save-status');
        if (!status) return;
        status.textContent = message;
        status.dataset.state = state;
        status.setAttribute('aria-live', 'polite');
        refreshPaletteStates();
    }

    function scheduleSave(panel, delay = 500) {
        if (isFinalizing) return;
        const questionId = panel.dataset.attemptQuestionId;
        if (!questionId) return;
        pending[questionId] = collectAnswer(panel);
        const generation = (generations.get(questionId) || 0) + 1;
        generations.set(questionId, generation);
        persistLocalCopy();
        showFeedback(panel, text.saving, 'saving');
        clearTimeout(timers.get(questionId));
        timers.set(questionId, setTimeout(() => {
            timers.delete(questionId);
            void saveAnswer(panel, questionId, generation);
        }, delay));
    }

    async function performSave(panel, questionId, generation) {
        for (let retries = 0; retries <= 3; retries += 1) {
            if (!navigator.onLine) {
                showFeedback(panel, text.offline, 'error');
                return false;
            }

            activeSaves += 1;
            const controller = new AbortController();
            const requestTimeout = setTimeout(() => controller.abort(), 5000);
            try {
                const answer = pending[questionId] || collectAnswer(panel);
                const response = await fetch(saveUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': antiforgery
                    },
                    body: JSON.stringify({
                        attemptId,
                        attemptQuestionId: questionId,
                        selectedChoiceIds: answer.selectedChoiceIds,
                        textAnswer: answer.textAnswer
                    }),
                    signal: controller.signal
                });

                if (response.status === 410) {
                    showFeedback(panel, text.expiredAnswer, 'error');
                    return false;
                }
                if (!response.ok) throw new Error(`Save failed: ${response.status}`);

                if (generations.get(questionId) === generation) {
                    delete pending[questionId];
                    persistLocalCopy();
                    showFeedback(panel, text.saved, 'saved');
                }
                return true;
            } catch {
                if (retries === 3) {
                    showFeedback(panel, text.failed, 'error');
                    return false;
                }
                showFeedback(panel, text.retry, 'saving');
            } finally {
                clearTimeout(requestTimeout);
                activeSaves = Math.max(0, activeSaves - 1);
            }

            await new Promise(resolve => setTimeout(resolve, 800 * (2 ** retries)));
        }
        return false;
    }

    function saveAnswer(panel, questionId, generation) {
        const existing = inFlightSaves.get(questionId);
        if (existing) {
            return existing.then(result => {
                if (pending[questionId] && generations.get(questionId) !== generation)
                    return saveAnswer(panel, questionId, generations.get(questionId));
                return result;
            });
        }

        const work = performSave(panel, questionId, generation).then(result =>
            result && pending[questionId] && generations.get(questionId) !== generation ? 'stale' : result);
        const tracked = work.finally(() => {
            if (inFlightSaves.get(questionId) === tracked) inFlightSaves.delete(questionId);
        });
        inFlightSaves.set(questionId, tracked);
        return tracked.then(result => result === 'stale'
            ? saveAnswer(panel, questionId, generations.get(questionId))
            : result);
    }

    function clearPendingSaveTimers() {
        timers.forEach(timerId => clearTimeout(timerId));
        timers.clear();
    }

    async function flushPendingSaves(timeoutMs = 20000) {
        const deadline = Date.now() + timeoutMs;
        clearPendingSaveTimers();
        const byQuestion = new Map(panels.map(panel => [panel.dataset.attemptQuestionId, panel]));

        while (Date.now() < deadline) {
            clearPendingSaveTimers();
            const jobs = Object.entries(pending).map(([questionId]) => {
                const panel = byQuestion.get(questionId);
                if (!panel) return Promise.resolve(false);
                return saveAnswer(panel, questionId, generations.get(questionId) || 0);
            });

            if (jobs.length) {
                const remaining = Math.max(1, deadline - Date.now());
                let timeoutId;
                const timeout = new Promise(resolve => { timeoutId = setTimeout(() => resolve('timeout'), remaining); });
                const result = await Promise.race([Promise.all(jobs), timeout]);
                clearTimeout(timeoutId);
                if (result === 'timeout') return false;
                if (result.some(saved => !saved)) return false;
            } else if (activeSaves > 0) {
                await new Promise(resolve => setTimeout(resolve, 25));
            } else {
                return true;
            }
        }
        return false;
    }

    function setFinalizing(busy) {
        form.querySelectorAll('input[type="radio"], input[type="checkbox"], textarea, button[type="button"], button[type="submit"]')
            .forEach(field => { field.disabled = busy; });
        finishButtons.forEach(button => { button.disabled = busy; });
    }

    async function finalizeSubmission(expired = false) {
        if (finalizationPromise) return finalizationPromise;
        isFinalizing = true;
        setFinalizing(true);
        if (submitError) submitError.hidden = true;
        finalizationPromise = (async () => {
            const saved = await flushPendingSaves(expired ? 3000 : 20000);
            if (!saved && !expired) {
                if (submitError) {
                    submitError.textContent = text.savesFailed;
                    submitError.hidden = false;
                } else window.alert(text.savesFailed);
                return false;
            }

            submitConfirmed = true;
            if (form.requestSubmit) form.requestSubmit();
            else form.submit();
            return true;
        })();

        const result = await finalizationPromise;
        if (!result) {
            isFinalizing = false;
            setFinalizing(false);
            finalizationPromise = null;
        }
        return result;
    }

    function submitOnExpiry() {
        if (expirySubmitStarted) return;
        expirySubmitStarted = true;
        panels.forEach(panel => showFeedback(panel, text.expiring, 'saving'));
        void finalizeSubmission(true);
    }

    function isAnswered(panel) {
        return Array.from(panel.querySelectorAll('input[type="radio"], input[type="checkbox"]')).some(field => field.checked)
            || Array.from(panel.querySelectorAll('textarea')).some(field => field.value.trim().length > 0);
    }

    function showQuestion(nextIndex) {
        if (!panels.length) return;
        index = Math.min(Math.max(nextIndex, 0), panels.length - 1);
        panels.forEach((panel, i) => panel.classList.toggle('active', i === index));
        refreshPaletteStates();
        if (counter) counter.textContent = String(index + 1);
        const percentage = Math.ceil(((index + 1) / panels.length) * 100);
        if (progress) progress.style.width = `${percentage}%`;
        const progressContainer = progress?.parentElement;
        progressContainer?.setAttribute('aria-valuenow', String(index + 1));
        if (progressPercentage) progressPercentage.textContent = `${percentage}%`;
        const navSummary = document.getElementById('questionsNavSummary');
        if (navSummary) navSummary.textContent = `${index + 1} ${text.of} ${panels.length}`;
        const previous = document.getElementById('prevQuestion');
        const next = document.getElementById('nextQuestion');
        if (previous) previous.disabled = index === 0;
        if (next) next.hidden = index === panels.length - 1;
        if (mobileNav && window.matchMedia('(max-width: 767px)').matches && document.activeElement?.matches('[data-jump]')) {
            mobileNav.removeAttribute('open');
            panels[index].querySelector('.exam-question-text')?.focus({ preventScroll: true });
        }
        persistLocalCopy();
    }

    function refreshPaletteStates() {
        palette.forEach((button, i) => {
            const panel = panels[i];
            const answered = isAnswered(panel);
            const review = panel.querySelector('.mark-review')?.checked || false;
            const questionId = panel.dataset.attemptQuestionId;
            const saveState = panel.querySelector('.answer-save-status')?.dataset.state;
            const isSaving = answered && saveState !== 'error' && (Boolean(pending[questionId]) || saveState === 'saving');
            const hasSaveError = answered && saveState === 'error';
            const isSaved = answered && !isSaving && !hasSaveError && saveState === 'saved';
            button.classList.toggle('active', i === index);
            button.classList.toggle('answered', isSaved);
            button.classList.toggle('saving', isSaving);
            button.classList.toggle('save-error', hasSaveError);
            button.classList.toggle('review', review);
            if (i === index) button.setAttribute('aria-current', 'step');
            else button.removeAttribute('aria-current');
            const answerLabel = isSaved ? text.answerSaved : isSaving ? text.savingAnswer : hasSaveError ? text.answerFailed : text.unanswered;
            button.setAttribute('aria-label', text.question.replace('{0}', i + 1).replace('{1}', answerLabel).replace('{2}', review ? text.reviewSuffix : ''));
            const reviewText = panel.querySelector('.review-button-text');
            if (reviewText) reviewText.textContent = review ? text.reviewMarked : text.markReview;
            panel.querySelector('.exam-review-button')?.classList.toggle('is-marked', review);
        });
    }

    function syncQuestionNav() {
        if (!mobileNav) return;
        if (window.matchMedia('(max-width: 767px)').matches) mobileNav.removeAttribute('open');
        else mobileNav.setAttribute('open', '');
    }

    function showSubmitConfirmation() {
        const answered = panels.filter(isAnswered).length;
        const review = panels.filter(panel => panel.querySelector('.mark-review')?.checked).length;
        const answeredCount = document.getElementById('submitAnsweredCount');
        const unansweredCount = document.getElementById('submitUnansweredCount');
        const reviewCount = document.getElementById('submitReviewCount');
        if (answeredCount) answeredCount.textContent = String(answered);
        if (unansweredCount) unansweredCount.textContent = String(panels.length - answered);
        if (reviewCount) reviewCount.textContent = String(review);
        if (submitDialog?.showModal) submitDialog.showModal();
        else if (window.confirm(text.confirmSubmit)) void finalizeSubmission();
    }

    function renderTimer() {
        if (!timer || !hasDeadline || !Number.isFinite(remaining)) return;
        const minutes = Math.floor(remaining / 60).toString().padStart(2, '0');
        const seconds = (remaining % 60).toString().padStart(2, '0');
        timerValue.textContent = `${minutes}:${seconds}`;
        timer.classList.toggle('is-warning', remaining < 600 && remaining >= 300);
        timer.classList.toggle('is-critical', remaining < 300);
        timer.setAttribute('aria-label', text.remaining.replace('{0}', minutes).replace('{1}', seconds));
        if (remaining <= 0) {
            submitOnExpiry();
        }
    }

    document.getElementById('prevQuestion')?.addEventListener('click', () => showQuestion(index - 1));
    document.getElementById('nextQuestion')?.addEventListener('click', () => showQuestion(index + 1));
    document.getElementById('showQuestionList')?.addEventListener('click', () => {
        mobileNav?.setAttribute('open', '');
        mobileNav?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        mobileNav?.querySelector('[data-jump]')?.focus({ preventScroll: true });
    });
    palette.forEach(button => button.addEventListener('click', () => showQuestion(Number(button.dataset.jump))));
    form.addEventListener('change', event => {
        const panel = event.target.closest('.question-panel');
        if (panel && event.target.matches('[data-attempt-choice-id]')) scheduleSave(panel);
        showQuestion(index);
    });
    form.addEventListener('input', event => {
        const panel = event.target.closest('.question-panel');
        if (panel && event.target.matches('textarea')) scheduleSave(panel);
    });
    form.addEventListener('submit', event => {
        if (submitConfirmed) return;
        event.preventDefault();
        showSubmitConfirmation();
    });
    document.getElementById('cancelSubmit')?.addEventListener('click', () => {
        if (!isFinalizing) submitDialog?.close();
    });
    document.getElementById('confirmSubmit')?.addEventListener('click', () => {
        void finalizeSubmission();
    });
    window.addEventListener('online', () => {
        panels.forEach(panel => {
            if (pending[panel.dataset.attemptQuestionId]) scheduleSave(panel, 0);
        });
    });

    restoreLocalCopy();
    syncQuestionNav();
    showQuestion(index);
    panels.forEach(panel => {
        if (pending[panel.dataset.attemptQuestionId]) scheduleSave(panel, 0);
    });
    renderTimer();
    window.addEventListener('resize', syncQuestionNav, { passive: true });

    setInterval(() => {
        if (hasDeadline && remaining > 0) remaining -= 1;
        renderTimer();
    }, 1000);
})();
