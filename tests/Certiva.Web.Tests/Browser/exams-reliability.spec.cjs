const { test, expect } = require('@playwright/test');
const path = require('node:path');

const scriptPath = path.resolve(__dirname, '../../../src/Certiva.Web/wwwroot/js/exams.js');

async function openTake(page, { questions = 1, deadline = false, remaining = 0, answerType = 'short', storageUnavailable = false } = {}) {
    const answers = answerType === 'short'
        ? '<textarea></textarea>'
        : answerType === 'multi'
            ? '<input type="checkbox" data-attempt-choice-id="choice-a" /><input type="checkbox" data-attempt-choice-id="choice-b" />'
            : '<input type="radio" data-attempt-choice-id="choice-a" /><input type="radio" data-attempt-choice-id="choice-b" />';
    const panels = Array.from({ length: questions }, (_, i) => `
        <article class="question-panel ${i === 0 ? 'active' : ''}" data-attempt-question-id="question-${i + 1}">
            <p class="answer-save-status" data-state="saved" aria-live="polite"></p>
            ${answers}
            <input class="mark-review" type="checkbox" />
            <h2 class="exam-question-text" tabindex="-1">Question ${i + 1}</h2>
        </article>`).join('');

    const fixture = `<!doctype html><html><body>
        <section class="take-shell" data-attempt-id="attempt-1" data-save-url="/save">
            <form id="examForm" action="/submit" method="post">
                <input type="hidden" name="__RequestVerificationToken" value="token" />
                <input type="hidden" name="attemptId" value="attempt-1" />
                ${panels}
                <button type="button" id="prevQuestion">Prev</button>
                <button type="button" id="nextQuestion">Next</button>
                <button type="button" id="showQuestionList">Questions</button>
                <button type="submit" id="finishExam">Finish</button>
                <dialog id="submitDialog">
                    <p id="submitSaveError" role="alert" hidden></p>
                    <button type="button" id="cancelSubmit">Cancel</button>
                    <button type="button" id="confirmSubmit">Confirm</button>
                </dialog>
            </form>
        </section>
        <span id="questionCounter"></span><span id="progressPercentage"></span>
        <div><span id="progressBar"></span></div>
        <span id="questionsNavSummary"></span>
        <span id="submitAnsweredCount"></span><span id="submitUnansweredCount"></span><span id="submitReviewCount"></span>
        ${deadline ? `<div id="timer" data-has-deadline="True" data-remaining-seconds="${remaining}"><span id="timerValue"></span></div>` : ''}
        <script>window.examI18n = {
            saving: 'Saving', offline: 'Offline', expiredAnswer: 'Expired', saved: 'Saved', retry: 'Retrying',
            failed: 'Failed', expiring: 'Expiring', answerSaved: 'saved', savingAnswer: 'saving',
            answerFailed: 'failed', unanswered: 'unanswered', reviewMarked: 'review', markReview: 'mark review',
            confirmSubmit: 'Finish?', remaining: 'Remaining {0}:{1}', question: 'Question {0}: {1} {2}',
            reviewSuffix: 'review', of: 'of', savesFailed: 'Some answers could not be saved. Please try again.'
        };
        window.submitEvents = 0;
        window.submitPayloads = [];
        document.getElementById('examForm').addEventListener('submit', event => {
            window.submitEvents++;
            window.submitPayloads.push(Object.fromEntries(new FormData(event.target)));
            event.preventDefault();
        });</script>
    </body></html>`;
    const fixtureUrl = 'http://127.0.0.1:4189/fixture';
    await page.route(fixtureUrl, route => route.fulfill({ contentType: 'text/html', body: fixture }));
    await page.goto(fixtureUrl);
    await page.unroute(fixtureUrl);
    if (storageUnavailable) {
        await page.evaluate(() => Object.defineProperty(window, 'localStorage', {
            configurable: true,
            get() { throw new DOMException('blocked', 'SecurityError'); }
        }));
    }
    await page.addScriptTag({ path: scriptPath });
}

async function confirmFinish(page) {
    await page.locator('#finishExam').click();
    await expect(page.locator('#submitDialog')).toBeVisible();
    await page.locator('#confirmSubmit').click();
}

test('manual submit flushes a debounced answer before submitting', async ({ page }) => {
    const order = [];
    await page.route('**/save', async route => {
        order.push('save');
        await route.fulfill({ status: 200, body: '{}' });
    });
    await openTake(page);
    await page.locator('textarea').fill('latest answer');
    await confirmFinish(page);
    await expect.poll(() => page.evaluate(() => window.submitEvents)).toBe(2);
    expect(order).toEqual(['save']);
});

test('manual submit waits for an already active save and several pending saves', async ({ page }) => {
    const saved = [];
    await page.route('**/save', async route => {
        const body = route.request().postDataJSON();
        await new Promise(resolve => setTimeout(resolve, 120));
        saved.push(body.attemptQuestionId);
        await route.fulfill({ status: 200, body: '{}' });
    });
    await openTake(page, { questions: 2 });
    await page.locator('textarea').nth(0).fill('first');
    await page.locator('textarea').nth(1).fill('second');
    await page.waitForTimeout(550);
    await confirmFinish(page);
    await expect.poll(() => page.evaluate(() => window.submitEvents)).toBe(2);
    expect(saved.sort()).toEqual(['question-1', 'question-2']);
});

test('failed save keeps the dialog open, shows localized feedback, and can be retried', async ({ page }) => {
    await page.route('**/save', route => route.fulfill({ status: 200, body: '{}' }));
    await page.addInitScript(() => Object.defineProperty(navigator, 'onLine', { configurable: true, get: () => window.testOnline }));
    await openTake(page);
    await page.evaluate(() => { window.testOnline = false; });
    await page.locator('textarea').fill('answer');
    await confirmFinish(page);
    await expect(page.locator('#submitSaveError')).toHaveText('Some answers could not be saved. Please try again.');
    await expect.poll(() => page.evaluate(() => window.submitEvents)).toBe(1);

    await page.evaluate(() => { window.testOnline = true; window.dispatchEvent(new Event('online')); });
    await page.locator('#confirmSubmit').click();
    await expect.poll(() => page.evaluate(() => window.submitEvents)).toBe(2);
});

test('single choice, true/false, multi choice, and short answers retain autosave behavior', async ({ page }) => {
    const payloads = [];
    await page.route('**/save', async route => {
        payloads.push(route.request().postDataJSON());
        await route.fulfill({ status: 200, body: '{}' });
    });
    for (const answerType of ['single', 'single', 'multi', 'short']) {
        await openTake(page, { answerType });
        if (answerType === 'short') {
            await page.locator('textarea').fill('short answer');
        } else {
            await page.locator('input[type="' + (answerType === 'multi' ? 'checkbox' : 'radio') + '"]').first().check();
            if (answerType === 'multi') await page.locator('input[type="checkbox"]').nth(1).check();
        }
        await confirmFinish(page);
        await expect.poll(() => page.evaluate(() => window.submitEvents)).toBe(2);
        expect(payloads.at(-1).textAnswer).toBe(answerType === 'short' ? 'short answer' : '');
        expect(payloads.at(-1).selectedChoiceIds.length).toBe(answerType === 'multi' ? 2 : answerType === 'short' ? 0 : 1);
    }
});

test('expiration waits for an active save, then submits once', async ({ page }) => {
    await page.route('**/save', async route => {
        await new Promise(resolve => setTimeout(resolve, 1400));
        await route.fulfill({ status: 200, body: '{}' });
    });
    await openTake(page, { deadline: true, remaining: 1 });
    await page.locator('textarea').fill('answer before expiry');
    await page.waitForTimeout(550);
    await expect.poll(() => page.evaluate(() => window.submitEvents), { timeout: 5000 }).toBe(1);
    expect(await page.evaluate(() => window.submitPayloads[0])).toMatchObject({
        attemptId: 'attempt-1',
        __RequestVerificationToken: 'token'
    });
});

test('double confirm click starts only one finalization and storage failure does not stop server autosave', async ({ page }) => {
    let saveCount = 0;
    await page.route('**/save', async route => {
        saveCount++;
        await new Promise(resolve => setTimeout(resolve, 150));
        await route.fulfill({ status: 200, body: '{}' });
    });
    await openTake(page, { storageUnavailable: true });
    await page.locator('textarea').fill('works without local storage');
    await page.locator('#finishExam').click();
    await page.locator('#confirmSubmit').dispatchEvent('click');
    await page.locator('#confirmSubmit').dispatchEvent('click');
    await expect.poll(() => page.evaluate(() => window.submitEvents)).toBe(2);
    expect(saveCount).toBe(1);
});
