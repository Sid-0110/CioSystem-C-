/**
 * CioSystem - 韓系現代化 UI 互動功能
 * 提供增強的用戶體驗和視覺效果
 */

// 全域變數
let isLoading = false;
let currentToastId = 0;

// 初始化函數
document.addEventListener('DOMContentLoaded', function () {
    initializeAnimations();
    initializeFormEnhancements();
    initializeInteractiveElements();
    initializeTooltips();
    console.log('CioSystem UI 已初始化');
});

/**
 * 初始化動畫效果
 */
function initializeAnimations() {
    // 為所有卡片添加淡入動畫
    const cards = document.querySelectorAll('.card');
    cards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';

        setTimeout(() => {
            card.style.transition = 'all 0.6s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });

    // 為統計數字添加計數動畫
    animateStatistics();
}

/**
 * 統計數字動畫
 */
function animateStatistics() {
    const statsNumbers = document.querySelectorAll('.stats-number');
    statsNumbers.forEach(number => {
        const finalValue = parseInt(number.textContent.replace(/[^\d]/g, ''));
        if (!isNaN(finalValue) && finalValue > 0) {
            animateNumber(number, 0, finalValue, 1500);
        }
    });
}

/**
 * 數字動畫函數
 */
function animateNumber(element, start, end, duration) {
    const startTime = performance.now();
    const isCurrency = element.textContent.includes('$') || element.textContent.includes('¥') || element.textContent.includes('€');

    function updateNumber(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);

        // 使用 easeOut 緩動函數
        const easeProgress = 1 - Math.pow(1 - progress, 3);
        const current = Math.floor(start + (end - start) * easeProgress);

        if (isCurrency) {
            element.textContent = '$' + current.toLocaleString();
        } else {
            element.textContent = current.toLocaleString();
        }

        if (progress < 1) {
            requestAnimationFrame(updateNumber);
        }
    }

    requestAnimationFrame(updateNumber);
}

/**
 * 初始化表單增強功能
 */
function initializeFormEnhancements() {
    // 為所有表單輸入添加聚焦效果
    const inputs = document.querySelectorAll('.form-control, .form-select');
    inputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.classList.add('focused');
        });

        input.addEventListener('blur', function () {
            this.parentElement.classList.remove('focused');
        });
    });

    // 表單提交動畫
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function (e) {
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn && !isLoading) {
                isLoading = true;
                const originalText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>處理中...';
                submitBtn.disabled = true;

                // 5秒後恢復按鈕狀態（防止卡住）
                setTimeout(() => {
                    isLoading = false;
                    submitBtn.innerHTML = originalText;
                    submitBtn.disabled = false;
                }, 5000);
            }
        });
    });
}

/**
 * 初始化互動元素
 */
function initializeInteractiveElements() {
    // 卡片懸停效果
    document.querySelectorAll('.card').forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-8px) scale(1.02)';
            this.style.boxShadow = '0 24px 64px rgba(108, 92, 231, 0.2)';
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0) scale(1)';
            this.style.boxShadow = '0 16px 48px rgba(108, 92, 231, 0.15)';
        });
    });

    // 按鈕懸停效果
    document.querySelectorAll('.btn').forEach(btn => {
        btn.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-2px)';
        });

        btn.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });

    // 表格行懸停效果
    document.querySelectorAll('tbody tr').forEach(row => {
        row.addEventListener('mouseenter', function () {
            this.style.backgroundColor = 'rgba(108, 92, 231, 0.05)';
            this.style.transform = 'scale(1.01)';
        });

        row.addEventListener('mouseleave', function () {
            this.style.backgroundColor = '';
            this.style.transform = 'scale(1)';
        });
    });
}

/**
 * 初始化工具提示
 */
function initializeTooltips() {
    // 為所有帶有 title 屬性的元素添加增強的工具提示
    document.querySelectorAll('[title]').forEach(element => {
        element.addEventListener('mouseenter', function () {
            showTooltip(this, this.title);
        });

        element.addEventListener('mouseleave', function () {
            hideTooltip();
        });
    });
}

/**
 * 顯示自定義工具提示
 */
function showTooltip(element, text) {
    const tooltip = document.createElement('div');
    tooltip.className = 'custom-tooltip';
    tooltip.textContent = text;
    tooltip.style.cssText = `
        position: absolute;
        background: rgba(45, 52, 54, 0.9);
        color: white;
        padding: 8px 12px;
        border-radius: 8px;
        font-size: 12px;
        z-index: 1000;
        pointer-events: none;
        backdrop-filter: blur(10px);
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
        animation: fadeIn 0.3s ease;
    `;

    document.body.appendChild(tooltip);

    const rect = element.getBoundingClientRect();
    tooltip.style.left = rect.left + rect.width / 2 - tooltip.offsetWidth / 2 + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';

    element._tooltip = tooltip;
}

/**
 * 隱藏工具提示
 */
function hideTooltip() {
    const tooltip = document.querySelector('.custom-tooltip');
    if (tooltip) {
        tooltip.remove();
    }
}

/**
 * 顯示 Toast 通知
 */
function showToast(message, type = 'info', title = '通知', duration = 5000) {
    const toastContainer = document.getElementById('toast-container') || createToastContainer();

    const toastId = 'toast-' + (++currentToastId);
    const iconMap = {
        'success': 'fas fa-check-circle',
        'error': 'fas fa-exclamation-circle',
        'warning': 'fas fa-exclamation-triangle',
        'info': 'fas fa-info-circle'
    };

    const typeColors = {
        'success': 'var(--success-color)',
        'error': 'var(--danger-color)',
        'warning': 'var(--warning-color)',
        'info': 'var(--primary-color)'
    };

    const toastHtml = `
        <div class="toast fade-in" id="${toastId}" role="alert" aria-live="assertive" aria-atomic="true" style="margin-bottom: 1rem;">
            <div class="toast-header" style="background: ${typeColors[type]}; color: white; border-radius: 12px 12px 0 0;">
                <i class="${iconMap[type]} me-2"></i>
                <strong class="me-auto">${title}</strong>
                <button type="button" class="btn-close btn-close-white" onclick="closeToast('${toastId}')"></button>
            </div>
            <div class="toast-body" style="background: white; border-radius: 0 0 12px 12px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);">
                ${message}
            </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML('beforeend', toastHtml);

    // 自動移除 toast
    setTimeout(() => {
        closeToast(toastId);
    }, duration);

    return toastId;
}

/**
 * 關閉 Toast
 */
function closeToast(toastId) {
    const toast = document.getElementById(toastId);
    if (toast) {
        toast.style.animation = 'fadeOut 0.3s ease';
        setTimeout(() => {
            toast.remove();
        }, 300);
    }
}

/**
 * 創建 Toast 容器
 */
function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.className = 'toast-container position-fixed top-0 end-0 p-3';
    container.style.zIndex = '1055';
    container.style.maxWidth = '400px';
    document.body.appendChild(container);
    return container;
}

/**
 * 顯示載入動畫
 */
function showLoading(element, text = '載入中...') {
    if (!element) return;

    const originalContent = element.innerHTML;
    element._originalContent = originalContent;
    element.innerHTML = `
        <div class="d-flex align-items-center justify-content-center">
            <div class="loading-spinner me-2"></div>
            <span>${text}</span>
        </div>
    `;
    element.disabled = true;
}

/**
 * 隱藏載入動畫
 */
function hideLoading(element) {
    if (!element || !element._originalContent) return;

    element.innerHTML = element._originalContent;
    element.disabled = false;
}

/**
 * AJAX 請求輔助函數
 */
function ajaxRequest(url, options = {}) {
    const defaultOptions = {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        }
    };

    const finalOptions = { ...defaultOptions, ...options };

    return fetch(url, finalOptions)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .catch(error => {
            console.error('AJAX 請求錯誤:', error);
            showToast('請求失敗，請稍後再試', 'error', '網路錯誤');
            throw error;
        });
}

/**
 * 表單驗證增強
 */
function validateForm(form) {
    let isValid = true;
    const errors = [];

    // 檢查必填欄位
    const requiredFields = form.querySelectorAll('[required]');
    requiredFields.forEach(field => {
        if (!field.value.trim()) {
            field.classList.add('is-invalid');
            errors.push(`${field.previousElementSibling?.textContent || '欄位'} 為必填項目`);
            isValid = false;
        } else {
            field.classList.remove('is-invalid');
        }
    });

    // 檢查電子郵件格式
    const emailFields = form.querySelectorAll('input[type="email"]');
    emailFields.forEach(field => {
        if (field.value && !isValidEmail(field.value)) {
            field.classList.add('is-invalid');
            errors.push('請輸入有效的電子郵件地址');
            isValid = false;
        }
    });

    // 檢查數字欄位
    const numberFields = form.querySelectorAll('input[type="number"]');
    numberFields.forEach(field => {
        if (field.value && isNaN(field.value)) {
            field.classList.add('is-invalid');
            errors.push(`${field.previousElementSibling?.textContent || '數字欄位'} 必須為有效數字`);
            isValid = false;
        }
    });

    if (!isValid) {
        showToast(errors.join('<br>'), 'error', '表單驗證錯誤');
    }

    return isValid;
}

/**
 * 驗證電子郵件格式
 */
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

/**
 * 格式化數字
 */
function formatNumber(number, decimals = 0) {
    return new Intl.NumberFormat('zh-TW', {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    }).format(number);
}

/**
 * 格式化貨幣
 */
function formatCurrency(amount, currency = 'TWD') {
    return new Intl.NumberFormat('zh-TW', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

/**
 * 格式化日期
 */
function formatDate(date, options = {}) {
    const defaultOptions = {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
    };

    const finalOptions = { ...defaultOptions, ...options };
    return new Intl.DateTimeFormat('zh-TW', finalOptions).format(new Date(date));
}

/**
 * 複製到剪貼板
 */
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        showToast('已複製到剪貼板', 'success', '複製成功');
    } catch (err) {
        console.error('複製失敗:', err);
        showToast('複製失敗', 'error', '複製錯誤');
    }
}

/**
 * 防抖函數
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * 節流函數
 */
function throttle(func, limit) {
    let inThrottle;
    return function () {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

/**
 * 滾動到頂部
 */
function scrollToTop(duration = 1000) {
    const start = window.pageYOffset;
    const distance = -start;
    let startTime = null;

    function animation(currentTime) {
        if (startTime === null) startTime = currentTime;
        const timeElapsed = currentTime - startTime;
        const run = easeInOutQuad(timeElapsed, start, distance, duration);
        window.scrollTo(0, run);
        if (timeElapsed < duration) requestAnimationFrame(animation);
    }

    requestAnimationFrame(animation);
}

/**
 * 緩動函數
 */
function easeInOutQuad(t, b, c, d) {
    t /= d / 2;
    if (t < 1) return c / 2 * t * t + b;
    t--;
    return -c / 2 * (t * (t - 2) - 1) + b;
}

/**
 * 全域錯誤處理
 */
window.addEventListener('error', function (e) {
    console.error('JavaScript 錯誤:', e.error);
    showToast('發生未預期的錯誤', 'error', '系統錯誤');
});

/**
 * 網路狀態監聽
 */
window.addEventListener('online', function () {
    showToast('網路連線已恢復', 'success', '連線狀態');
});

window.addEventListener('offline', function () {
    showToast('網路連線已中斷', 'warning', '連線狀態');
});

// 導出全域函數
window.CioSystem = {
    showToast,
    closeToast,
    showLoading,
    hideLoading,
    ajaxRequest,
    validateForm,
    formatNumber,
    formatCurrency,
    formatDate,
    copyToClipboard,
    scrollToTop,
    debounce,
    throttle
};