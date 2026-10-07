APP = {

    rtlLanguage: function () {
        document.body.classList.add('ar');
        applyLang('ar');
    },

    ltrLanguage: function () {
        document.body.classList.remove('ar');
        applyLang('en');
    },

    toggleSidebar: function () {
        document.getElementById("main-sidebar").classList.toggle('show');
        document.getElementById("overlay").classList.toggle('show');
    },

    hideModal: function (modalId) {
        var modal = document.getElementById(modalId);
        var bootstrapModal = bootstrap.Modal.getInstance(modal);
        if (bootstrapModal) {
            bootstrapModal.hide();
        }
    },

    openSchoolModal: function () {
        var modal = document.getElementById("SchoolsModal");
        if (modal) {
            var bootstrapModal = new bootstrap.Modal(modal);
            bootstrapModal.show();
        }
    },

    showModal: function (modalId) {
        var modal = document.getElementById(modalId);
        if (modal) {
            var bootstrapModal = bootstrap.Modal.getInstance(modal) || new bootstrap.Modal(modal);
            bootstrapModal.show();
        }
    },

    setupSchoolModalEvents: function (dotnetRef) {
        var modal = document.getElementById("SchoolsModal");
        if (modal) {
            modal.addEventListener("show.bs.modal", async function () {
                // Load schools data before showing modal
                if (dotnetRef && dotnetRef.invokeMethodAsync) {
                    await dotnetRef.invokeMethodAsync("LoadSchools");
                }
            });
        }
    },

    openMenu: function () {
        if (window.innerWidth <= 1024) {
            const sidebar = document.getElementById("main-sidebar");
            const overlay = document.getElementById("overlay");
            if (sidebar) {
                sidebar.classList.add("show");
                sidebar.style.display = "block";
            }
            if (overlay) {
                overlay.classList.add("show");
                overlay.style.display = "block";
            }
        }
    },
    closeMenu: function () {
        if (window.innerWidth <= 1024) {
            const sidebar = document.getElementById("main-sidebar");
            const overlay = document.getElementById("overlay");
            if (sidebar) {
                sidebar.classList.remove("show");
                sidebar.style.display = "none";
            }
            if (overlay) {
                overlay.classList.remove("show");
                overlay.style.display = "none";
            }
        }
    },

    validateFloat: function (event) {
        const value = element.value;

        if (!/[0-9.]/.test(event.key) || (event.key === '.' && value.includes('.'))) {
            event.preventDefault();
        }
    },
    validateInt: function () {
        element.addEventListener("keypress", function (event) {
            if (!/[0-9]/.test(event.key)) {
                event.preventDefault();
            }
        });
    },

    printReport: function (fileName, pageOrientation, requestedPageMargin) {
        const originalTitle = document.title;
        const isStudentContract = document.getElementById("student-contract-report") !== null;
        const isReceipt = document.getElementById("payment-receipt") !== null;
        let pageStyle = null;

        if (fileName) {
            document.title = fileName;
        }

        if (isStudentContract) {
            document.documentElement.classList.add("student-contract-printing");
            document.body.classList.add("student-contract-printing");
        }

        if (isReceipt) {
            document.documentElement.classList.add("receipt-printing");
            document.body.classList.add("receipt-printing");
        }

        // Named CSS pages are not applied consistently by every browser print
        // preview. Add the report's orientation as the last @page rule so it
        // also applies when the user chooses "Save as PDF".
        if (pageOrientation === "portrait" || pageOrientation === "landscape") {
            pageStyle = document.createElement("style");
            pageStyle.media = "print";
            const pageMargin = requestedPageMargin || (isReceipt ? "8mm" : "0");
            pageStyle.textContent = `@page { size: A4 ${pageOrientation}; margin: ${pageMargin}; }`;
            document.head.appendChild(pageStyle);
        }

        const restorePrintState = function () {
            document.title = originalTitle;
            pageStyle?.remove();
            document.documentElement.classList.remove("student-contract-printing");
            document.body.classList.remove("student-contract-printing");
            document.documentElement.classList.remove("receipt-printing");
            document.body.classList.remove("receipt-printing");
        };

        window.addEventListener("afterprint", restorePrintState, { once: true });

        try {
            window.print();
        } catch (error) {
            window.removeEventListener("afterprint", restorePrintState);
            restorePrintState();
            throw error;
        }
    }

}
// Focus the popup's search input after the browser has displayed the dropdown.
window.focusDropdownSearch = function (popupId, searchId) {
    const initialFocus = document.activeElement;
    let attempts = 0;

    function focusSearch() {
        const popup = document.getElementById(popupId);
        const input = document.getElementById(searchId);
        if (!popup || !input || popup.style.display === "none" || popup.classList.contains("rz-close")) return;
        if (document.activeElement === input) return;

        // Do not take focus back if the user has already moved to another field.
        if (document.activeElement !== initialFocus && document.activeElement !== document.body) return;

        if (input.getClientRects().length && getComputedStyle(input).visibility === "visible") {
            input.focus({ preventScroll: true });
        }

        if (document.activeElement !== input && ++attempts < 10) {
            requestAnimationFrame(focusSearch);
        }
    }

    requestAnimationFrame(focusSearch);
};
