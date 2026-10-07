/**
 * MANAGEHUB — APP.JS
 * Theme, Language, Sidebar, OTP, Recovery, Control Panel
 */

'use strict';

/* ═══════════════════════════════════════
   STATE
═══════════════════════════════════════ */
const State = {
  theme: localStorage.getItem('mh-theme') || 'light',
  lang:  localStorage.getItem('mh-lang')  || 'ar',
  otpTimer: null
};

/* ═══════════════════════════════════════
       LANGUAGE
    ═══════════════════════════════════════ */
function applyLang(lang) {
    State.lang = lang;
    const html = document.documentElement;
    html.setAttribute('lang', lang);
    html.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');
    localStorage.setItem('mh-lang', lang);

};

/* ═══════════════════════════════════════
   THEME
═══════════════════════════════════════ */
function applyTheme(theme) {
  State.theme = theme;
  document.documentElement.setAttribute('data-theme', theme);
  localStorage.setItem('mh-theme', theme);

  // Update all theme icon buttons
  document.querySelectorAll('.js-theme-icon').forEach(el => {
    el.textContent = theme === 'dark' ? 'light_mode' : 'dark_mode';
  });
  document.querySelectorAll('.js-theme-label').forEach(el => {
    el.textContent = theme === 'dark'
      ? (State.lang === 'ar' ? 'فاتح' : 'Light')
      : (State.lang === 'ar' ? 'داكن' : 'Dark');
  });
}

function toggleTheme() {
  applyTheme(State.theme === 'light' ? 'dark' : 'light');
}


function updateSearchPlaceholders() {
  const ph = State.lang === 'ar' ? 'بحث...' : 'Search...';
  document.querySelectorAll('.js-search-input').forEach(el => {
    el.placeholder = ph;
  });
  const userPh = document.querySelector('.js-user-search');
  if (userPh) userPh.placeholder = State.lang === 'ar' ? 'بحث عن مستخدم...' : 'Search users...';
}

/* ═══════════════════════════════════════
   SIDEBAR
═══════════════════════════════════════ */
function openSidebar(sidebarId, overlayId) {
  const s = document.getElementById(sidebarId);
  const o = document.getElementById(overlayId);
  if (s) s.classList.add('sidebar-open');
  if (o) o.classList.add('visible');
}

function closeSidebar(sidebarId, overlayId) {
  const s = document.getElementById(sidebarId);
  const o = document.getElementById(overlayId);
  if (s) s.classList.remove('sidebar-open');
  if (o) o.classList.remove('visible');
}

function toggleSidebar(sidebarId, overlayId) {
  const s = document.getElementById(sidebarId);
  if (s && s.classList.contains('sidebar-open')) {
    closeSidebar(sidebarId, overlayId);
  } else {
    openSidebar(sidebarId, overlayId);
  }
}

/* ═══════════════════════════════════════
   PASSWORD VISIBILITY TOGGLE
═══════════════════════════════════════ */
function togglePass(inputId, btn) {
  const field = document.getElementById(inputId);
  if (!field) return;
  const isPass = field.type === 'password';
  field.type = isPass ? 'text' : 'password';
  btn.textContent = isPass ? 'visibility' : 'visibility_off';
}

/* ═══════════════════════════════════════
   PASSWORD STRENGTH
═══════════════════════════════════════ */
function checkPasswordStrength(value) {
  const bar   = document.getElementById('js-strength-bar');
  const label = document.getElementById('js-strength-label');
  const wrap  = document.getElementById('js-strength-wrap');
  if (!bar) return;

  wrap.style.display = value.length > 0 ? 'block' : 'none';

  let score = 0;
  if (value.length >= 8)          score++;
  if (/[A-Z]/.test(value))        score++;
  if (/[0-9]/.test(value))        score++;
  if (/[^A-Za-z0-9]/.test(value)) score++;

  const pct    = (score / 4) * 100;
  const colors = ['#ef4444', '#f59e0b', '#22c55e', '#14b8a6'];
  const labAr  = ['ضعيفة جداً', 'ضعيفة', 'جيدة', 'قوية'];
  const labEn  = ['Very Weak', 'Weak', 'Good', 'Strong'];
  const col    = colors[score - 1] || '#ef4444';

  bar.style.width      = pct + '%';
  bar.style.background = col;
  label.style.color    = col;
  label.style.fontSize = '.74rem';
  label.style.marginTop = '.3rem';
}

/* ═══════════════════════════════════════
   OTP
═══════════════════════════════════════ */
function handleOtpInput(input, index) {
  const inputs = document.querySelectorAll('.js-otp-input');
  input.classList.toggle('otp-filled', input.value.length > 0);
  if (input.value.length === 1 && index < inputs.length - 1) {
    inputs[index + 1].focus();
  }
}

function handleOtpKeydown(event, index) {
  const inputs = document.querySelectorAll('.js-otp-input');
  if (event.key === 'Backspace' && !event.target.value && index > 0) {
    inputs[index - 1].focus();
  }
}

function startOtpTimer() {
  let count = 60;
  const countEl  = document.getElementById('js-otp-count');
  const timerEl  = document.getElementById('js-otp-timer');
  const resendBtn = document.getElementById('js-resend-btn');

  if (!countEl) return;
  if (timerEl)  timerEl.style.display = '';
  if (resendBtn) resendBtn.disabled = true;
  countEl.textContent = count;

  if (State.otpTimer) clearInterval(State.otpTimer);
  State.otpTimer = setInterval(() => {
    count--;
    countEl.textContent = count;
    if (count <= 0) {
      clearInterval(State.otpTimer);
      if (timerEl)  timerEl.style.display = 'none';
      if (resendBtn) resendBtn.disabled = false;
    }
  }, 1000);
}

function resendOtp() {
  const timerEl = document.getElementById('js-otp-timer');
  if (timerEl) timerEl.style.display = '';
  startOtpTimer();
}

function showSubmitButtonProcessing(button) {
  if (!button) return true;
  const text = button.dataset.processingText || 'Processing';

  window.setTimeout(() => {
    button.disabled = true;
    button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span><span>' + text + '</span>';
  }, 0);

  return true;
}

function focusFirstOtpInput() {
  const firstInput = document.querySelector('#recovery-panel-2 .js-otp-input');
  if (!firstInput) return;

  window.setTimeout(() => firstInput.focus(), 0);
}

/* ═══════════════════════════════════════
   RECOVERY STEPS
═══════════════════════════════════════ */
function goToRecoveryStep(step) {
  for (let i = 1; i <= 3; i++) {
    const panel = document.getElementById('recovery-panel-' + i);
    if (panel) panel.style.display = i === step ? '' : 'none';

    const stepEl = document.getElementById('recovery-step-dot-' + i);
    if (stepEl) {
      stepEl.classList.remove('step-active', 'step-done');
      if (i < step)      stepEl.classList.add('step-done');
      else if (i === step) stepEl.classList.add('step-active');
    }
  }
  if (step === 2) {
    startOtpTimer();
    focusFirstOtpInput();
  }
}

function initializeRecoveryStep() {
  const codePanel = document.getElementById('recovery-panel-2');
  if (!codePanel) return;

  if (window.getComputedStyle(codePanel).display !== 'none') {
    startOtpTimer();
    focusFirstOtpInput();
  }
}

/* ═══════════════════════════════════════
   CONTROL PANEL TABS
═══════════════════════════════════════ */
function cpSetTab(tabName) {
  // Hide all tab panels
  document.querySelectorAll('.cp-tab-panel').forEach(el => el.style.display = 'none');
  // Deactivate all tabs
  document.querySelectorAll('.cp-tab').forEach(el => el.classList.remove('active'));
  // Deactivate sidebar items
  document.querySelectorAll('.cp-sidebar-nav .nav-item').forEach(el => el.classList.remove('active'));

  // Activate target panel
  const panel = document.getElementById('cp-panel-' + tabName);
  if (panel) panel.style.display = '';

  // Activate matching tab buttons
  document.querySelectorAll(`.cp-tab[data-tab="${tabName}"]`).forEach(el => el.classList.add('active'));

  // Activate matching sidebar link
  document.querySelectorAll(`.cp-sidebar-nav .nav-item[data-tab="${tabName}"]`)
    .forEach(el => el.classList.add('active'));
}

/* ═══════════════════════════════════════
   MINI BAR CHART HEIGHTS
═══════════════════════════════════════ */
function renderBarChart() {
  document.querySelectorAll('.bar-fill[data-h]').forEach(el => {
    el.style.height = el.dataset.h + '%';
  });
}

/* ═══════════════════════════════════════
   INIT
═══════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
  applyTheme(State.theme);
  applyLang(State.lang);
  renderBarChart();
  initializeRecoveryStep();

  // Wire all global buttons
  document.querySelectorAll('[data-action="toggle-theme"]').forEach(btn => {
    btn.addEventListener('click', toggleTheme);
  });


  // Close sidebar on mobile when nav-item is clicked
  document.querySelectorAll('.sidebar .nav-item[data-page]').forEach(item => {
    item.addEventListener('click', () => {
      closeSidebar('main-sidebar', 'main-overlay');
      closeSidebar('cp-sidebar', 'cp-overlay');
    });
  });

  // Close sidebar on mobile when menu links are clicked (Blazor navigation)
  document.querySelectorAll('.nav-menu-link').forEach(link => {
    link.addEventListener('click', () => {
      if (window.innerWidth <= 1024) {
        APP.closeMenu();
      }
    });
  });
});

// Handle Blazor navigation to ensure sidebar is closed on mobile
if (window.Blazor) {
  window.Blazor.addEventListener('enhanced:navigating', () => {
    if (window.innerWidth <= 1024) {
      APP.closeMenu();
    }
  });
}
