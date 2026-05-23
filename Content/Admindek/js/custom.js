/*
 * Admindek - Admin Panel Template
 * Author: Jhoon Granados
 */
function initLayout() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.admin-sidebar');
    const overlay = document.querySelector('.sidebar-overlay');

    if (sidebarToggle) {
        sidebarToggle.onclick = (e) => {
            e.preventDefault();
            sidebar.classList.toggle('show');
            if (window.innerWidth <= 992) {
                overlay.classList.toggle('show');
            }
        };
    }

    if (overlay) {
        overlay.onclick = () => {
            if (sidebar) sidebar.classList.remove('show');
            overlay.classList.remove('show');
        };
    }

    const sidebarCollapse = document.getElementById('sidebarCollapse');
    if (sidebarCollapse) {
        sidebarCollapse.onclick = (e) => {
            e.preventDefault();
            if (sidebar) sidebar.classList.toggle('collapsed');
            document.body.classList.toggle('sidebar-collapsed');
        };
    }

    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    if (typeof TomSelect !== 'undefined') {
        document.querySelectorAll('.form-select').forEach((el) => {
            if (!el.tomselect) {
                new TomSelect(el, {
                    plugins: ['dropdown_input'],
                    controlInput: '<input>',
                    sortField: {
                        field: "text",
                        direction: "asc"
                    }
                });
            }
        });
    }

    const fullscreenBtn = document.querySelector('[data-widget="fullscreen"]');
    if (fullscreenBtn) {
        const icon = fullscreenBtn.querySelector('i');

        fullscreenBtn.onclick = function (e) {
            e.preventDefault();
            if (!document.fullscreenElement) {
                document.documentElement.requestFullscreen();
            } else {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                }
            }
        };

        if (!window.fullscreenListenerAdded) {
            document.addEventListener('fullscreenchange', () => {
                if (document.fullscreenElement) {
                    if (icon) {
                        icon.classList.remove('bi-arrows-fullscreen');
                        icon.classList.add('bi-fullscreen-exit');
                    }
                } else {
                    if (icon) {
                        icon.classList.remove('bi-fullscreen-exit');
                        icon.classList.add('bi-arrows-fullscreen');
                    }
                }
            });
            window.fullscreenListenerAdded = true;
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    initLayout();

    document.addEventListener('click', (e) => {
        const link = e.target.closest('a');
        if (!link) return;

        const href = link.getAttribute('href');

        if (!href ||
            href.startsWith('#') ||
            href.startsWith('http') ||
            href.startsWith('mailto:') ||
            link.hasAttribute('data-bs-toggle') ||
            link.getAttribute('target') === '_blank') {
            return;
        }

        if (href.endsWith('.html') || href.endsWith('/')) {
            e.preventDefault();
            loadContent(href);
        }
    });

    window.addEventListener('popstate', (e) => {
        if (e.state && e.state.url) {
            loadContent(e.state.url, false);
        } else {
        }
    });
});

async function loadContent(url, push = true) {
    try {
        const response = await fetch(url);
        if (!response.ok) throw new Error('Network response was not ok');
        const text = await response.text();

        const parser = new DOMParser();
        const doc = parser.parseFromString(text, 'text/html');

        const newSidebar = doc.querySelector('.admin-sidebar');
        const newContent = doc.querySelector('.admin-content');
        const newOverlay = doc.querySelector('.sidebar-overlay');
        const newFooter = doc.querySelector('.admin-footer');
        const newHeader = doc.querySelector('.admin-header');

        const currentSidebar = document.querySelector('.admin-sidebar');
        const currentContent = document.querySelector('.admin-content');
        const currentFooter = document.querySelector('.admin-footer');
        const currentHeader = document.querySelector('.admin-header');

        if (newSidebar && currentSidebar) {
            currentSidebar.replaceWith(newSidebar);
        }

        if (newHeader && currentHeader) {
            currentHeader.replaceWith(newHeader);
        }

        if (newContent && currentContent) {
            currentContent.replaceWith(newContent);
        }

        if (newFooter && currentFooter) {
            currentFooter.replaceWith(newFooter);
        }

        document.title = doc.title;

        if (push) {
            window.history.pushState({ url: url }, doc.title, url);
        }

        initLayout();

    } catch (error) {
        window.location.href = url;
    }
}


document.body.addEventListener('click', function (e) {
    if (e.target.closest('.btn-delete') || e.target.classList.contains('btn-delete')) {
        e.preventDefault();
        const btn = e.target.closest('.btn-delete') || e.target;

        Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, delete it!',
            customClass: {
                confirmButton: 'btn btn-primary me-2',
                cancelButton: 'btn btn-danger'
            },
            buttonsStyling: false
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Deleted!',
                    text: 'Your file has been deleted.',
                    icon: 'success',
                    customClass: {
                        confirmButton: 'btn btn-primary'
                    },
                    buttonsStyling: false
                });
            }
        })
    }
});
