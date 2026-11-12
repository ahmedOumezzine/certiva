(function () {
    'use strict';
    const commands = [['undo','↶'],['redo','↷'],['bold','B'],['italic','I'],['underline','U'],['formatBlock','H3','H3'],['insertUnorderedList','•'],['insertOrderedList','1.'],['createLink','🔗'],['formatBlock','</>','pre'],['insertTable','▦'],['removeFormat','×']];
    const escapeHtml = value => { const node = document.createElement('div'); node.textContent = value || ''; return node.innerHTML.replace(/\n/g, '<br>'); };
    function sync(field) {
        const content = field.closest('.rich-editor')?.querySelector('.rich-editor__content');
        if (content) field.value = content.innerHTML;
    }
    function create(field) {
        if (field.dataset.richEditor === 'true') return;
        field.dataset.richEditor = 'true';
        const shell = document.createElement('div'); shell.className = 'rich-editor';
        const toolbar = document.createElement('div'); toolbar.className = 'rich-editor__toolbar';
        const content = document.createElement('div'); content.className = 'rich-editor__content'; content.contentEditable = 'true'; content.setAttribute('role','textbox'); content.setAttribute('aria-multiline','true'); content.innerHTML = /<\/?[a-z][\s\S]*>/i.test(field.value) ? field.value : escapeHtml(field.value);
        commands.forEach(([command, label, value]) => { const button = document.createElement('button'); button.type='button'; button.className='rich-editor__button'; button.textContent=label; button.title=command; button.setAttribute('aria-label',command); button.addEventListener('mousedown', e => e.preventDefault()); button.addEventListener('click', () => { content.focus(); if(command==='createLink'){ const url=window.prompt('URL'); if(url) document.execCommand(command,false,url); } else if(command==='insertTable') document.execCommand('insertHTML',false,'<table><tbody><tr><td>Cellule</td><td>Cellule</td></tr></tbody></table>'); else if(command==='formatBlock') document.execCommand(command,false,`<${value}>`); else document.execCommand(command,false,false); field.value=content.innerHTML; }); toolbar.appendChild(button); });
        field.hidden=true; field.setAttribute('aria-hidden','true'); field.parentNode.insertBefore(shell, field); shell.append(toolbar, content); shell.appendChild(field); content.addEventListener('input', () => sync(field)); content.addEventListener('blur', () => sync(field));
    }
    function initializeRichEditors(root=document) { root.querySelectorAll('#questionForm textarea').forEach(create); }
    window.initializeRichEditors=initializeRichEditors;
    document.addEventListener('DOMContentLoaded', () => {
        initializeRichEditors();
        const form=document.getElementById('questionForm');
        if (!form) return;
        form.addEventListener('submit', () => {
            form.querySelectorAll('textarea[data-rich-editor="true"]').forEach(sync);
        }, true);
        form.addEventListener('formdata', () => {
            form.querySelectorAll('textarea[data-rich-editor="true"]').forEach(sync);
        });
        new MutationObserver(() => initializeRichEditors(form)).observe(form,{childList:true,subtree:true});
    });
})();
