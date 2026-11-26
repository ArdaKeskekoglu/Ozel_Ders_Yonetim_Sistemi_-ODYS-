function closeInfoPanel(toggle) {
    const panel = document.getElementById(toggle.getAttribute('aria-controls'));
    if (!panel) return;
    panel.hidden = true;
    toggle.setAttribute('aria-expanded', 'false');
}

document.addEventListener('click', function (event) {
    const target = event.target;
    if (!(target instanceof Element)) return;

    const clickedToggle = target.closest('[data-odys-info-toggle]');
    const clickedClose = target.closest('[data-odys-info-close]');

    if (clickedClose) {
        const toggle = clickedClose.closest('.odys-info')?.querySelector('[data-odys-info-toggle]');
        if (toggle) {
            closeInfoPanel(toggle);
            toggle.focus();
        }
        return;
    }

    document.querySelectorAll('[data-odys-info-toggle][aria-expanded="true"]').forEach(toggle => {
        if (toggle !== clickedToggle && !toggle.closest('.odys-info')?.contains(target)) {
            closeInfoPanel(toggle);
        }
    });

    if (clickedToggle) {
        const panel = document.getElementById(clickedToggle.getAttribute('aria-controls'));
        if (!panel) return;
        panel.hidden = !panel.hidden;
        clickedToggle.setAttribute('aria-expanded', String(!panel.hidden));
    }
});

document.addEventListener('keydown', function (event) {
    if (event.key !== 'Escape') return;
    const openToggle = document.querySelector('[data-odys-info-toggle][aria-expanded="true"]');
    if (openToggle) {
        closeInfoPanel(openToggle);
        openToggle.focus();
    }
});
