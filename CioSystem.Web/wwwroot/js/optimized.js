/**
 * CioSystem 優化版 JavaScript - 模組化和效能優化版本
 * 提供增強的用戶體驗和最佳化效能
 */

// 模組化架構
const CioSystem = (function () {
    'use strict';

    // 私有變數
    let isLoading = false;
    let currentToastId = 0;
    let performanceObserver = null;
    let intersectionObserver = null;

    // 效能監控
    const PerformanceMonitor = {
        init() {
            if ('PerformanceObserver' in window) {
                this.observeLCP();
                this.observeFID();
                this.observeCLS();
            }
            this.showPerformanceIndicator();
        },

        observeLCP() {
            performanceObserver = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                const lastEntry = entries[entries.length - 1];
                console.log('LCP:', lastEntry.startTime);
            });
            performanceObserver.observe({ entryTypes: ['largest-contentful-paint'] });
        },

        observeFID() {
            performanceObserver = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                entries.forEach(entry => {
                    console.log('FID:', entry.processingStart - entry.startTime);
                });
            });
            performanceObserver.observe({ entryTypes: ['first-input'] });
        },

        observeCLS() {
            performanceObserver = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                entries.forEach(entry => {
                    if (!entry.hadRecentInput) {
                        console.log('CLS:', entry.value);
                    }
                });
            });
            performanceObserver.observe({ entryTypes: ['layout-shift'] });
        },

        showPerformanceIndicator() {
            const indicator = document.createElement('div');
            indicator.className = 'performance-indicator';
            indicator.innerHTML = '⚡ 優化版';
            document.body.appendChild(indicator);
        }
    };

    // 動畫管理器
    const AnimationManager = {
        init() {
            this.setupIntersectionObserver();
            this.animateStatistics();
        },

        setupIntersectionObserver() {
            if ('IntersectionObserver' in window) {
                intersectionObserver = new IntersectionObserver((entries) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            this.animateElement(entry.target);
                        }
                    });
                }, {
                    threshold: 0.1,
                    rootMargin: '0px 0px -50px 0px'
                });

                // 觀察所有需要動畫的元素
                document.querySelectorAll('.card, .stats-card, .fade-in').forEach(el => {
                    intersectionObserver.observe(el);
                });
            }
        },

        animateElement(element) {
            element.classList.add('fade-in');
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
        },

        animateStatistics() {
            const statsNumbers = document.querySelectorAll('.stats-number');
            statsNumbers.forEach(number => {
                const finalValue = parseInt(number.textContent.replace(/[^\d]/g, ''));
                if (!isNaN(finalValue) && finalValue > 0) {
                    this.animateNumber(number, 0, finalValue, 1500);
                }
            });
        },

        animateNumber(element, start, end, duration) {
            const startTime = performance.now();
            const animate = (currentTime) => {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const current = Math.floor(progress * (end - start) + start);

                element.textContent = current.toLocaleString();

                if (progress < 1) {
                    requestAnimationFrame(animate);
                }
            };
            requestAnimationFrame(animate);
        }
    };

    // 表單增強器
    const FormEnhancer = {
        init() {
            this.setupFormValidation();
            this.setupAutoSave();
            this.setupInputEnhancements();
        },

        setupFormValidation() {
            const forms = document.querySelectorAll('form[data-validate]');
            forms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    if (!this.validateForm(form)) {
                        e.preventDefault();
                    }
                });
            });
        },

        validateForm(form) {
            const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');
            let isValid = true;

            inputs.forEach(input => {
                if (!input.value.trim()) {
                    this.showFieldError(input, '此欄位為必填');
                    isValid = false;
                } else {
                    this.clearFieldError(input);
                }
            });

            return isValid;
        },

        showFieldError(input, message) {
            this.clearFieldError(input);
            input.classList.add('is-invalid');

            const errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            errorDiv.textContent = message;
            input.parentNode.appendChild(errorDiv);
        },

        clearFieldError(input) {
            input.classList.remove('is-invalid');
            const errorDiv = input.parentNode.querySelector('.invalid-feedback');
            if (errorDiv) {
                errorDiv.remove();
            }
        },

        setupAutoSave() {
            const forms = document.querySelectorAll('form[data-autosave]');
            forms.forEach(form => {
                const inputs = form.querySelectorAll('input, select, textarea');
                inputs.forEach(input => {
                    input.addEventListener('input', this.debounce(() => {
                        this.saveFormData(form);
                    }, 1000));
                });
            });
        },

        saveFormData(form) {
            const formData = new FormData(form);
            const data = Object.fromEntries(formData);
            localStorage.setItem(`form_${form.id}`, JSON.stringify(data));
        },

        setupInputEnhancements() {
            // 數字輸入格式化
            const numberInputs = document.querySelectorAll('input[type="number"]');
            numberInputs.forEach(input => {
                input.addEventListener('input', (e) => {
                    e.target.value = e.target.value.replace(/[^0-9]/g, '');
                });
            });

            // 搜尋輸入優化
            const searchInputs = document.querySelectorAll('input[type="search"]');
            searchInputs.forEach(input => {
                input.addEventListener('input', this.debounce((e) => {
                    this.performSearch(e.target.value);
                }, 300));
            });
        },

        performSearch(query) {
            // 實現搜尋邏輯
            console.log('搜尋:', query);
        },

        debounce(func, wait) {
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
    };

    // 通知管理器
    const NotificationManager = {
        init() {
            this.createNotificationContainer();
        },

        createNotificationContainer() {
            if (!document.getElementById('notification-container')) {
                const container = document.createElement('div');
                container.id = 'notification-container';
                container.style.cssText = `
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    z-index: 9999;
                    max-width: 400px;
                `;
                document.body.appendChild(container);
            }
        },

        show(message, type = 'info', duration = 5000) {
            const container = document.getElementById('notification-container');
            const toast = document.createElement('div');
            toast.className = `toast toast-${type}`;
            toast.innerHTML = `
                <div class="toast-content">
                    <span class="toast-message">${message}</span>
                    <button class="toast-close" onclick="this.parentElement.parentElement.remove()">×</button>
                </div>
            `;

            container.appendChild(toast);

            // 自動移除
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.remove();
                }
            }, duration);
        },

        success(message) {
            this.show(message, 'success');
        },

        error(message) {
            this.show(message, 'error');
        },

        warning(message) {
            this.show(message, 'warning');
        },

        info(message) {
            this.show(message, 'info');
        }
    };

    // 載入管理器
    const LoadingManager = {
        show(target = null) {
            if (isLoading) return;

            isLoading = true;
            const loader = document.createElement('div');
            loader.className = 'loading-overlay';
            loader.innerHTML = `
                <div class="loading-spinner"></div>
                <div class="loading-text">載入中...</div>
            `;

            if (target) {
                target.appendChild(loader);
            } else {
                document.body.appendChild(loader);
            }
        },

        hide() {
            isLoading = false;
            const loader = document.querySelector('.loading-overlay');
            if (loader) {
                loader.remove();
            }
        }
    };

    // 分頁管理器
    const PaginationManager = {
        init() {
            this.setupPagination();
        },

        setupPagination() {
            const paginationLinks = document.querySelectorAll('.pagination .page-link');
            paginationLinks.forEach(link => {
                link.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.loadPage(link.href);
                });
            });
        },

        loadPage(url) {
            LoadingManager.show();

            fetch(url)
                .then(response => response.text())
                .then(html => {
                    // 更新內容區域
                    const parser = new DOMParser();
                    const doc = parser.parseFromString(html, 'text/html');
                    const newContent = doc.querySelector('.main-content');
                    const currentContent = document.querySelector('.main-content');

                    if (newContent && currentContent) {
                        currentContent.innerHTML = newContent.innerHTML;
                        this.init();
                    }
                })
                .catch(error => {
                    console.error('載入頁面失敗:', error);
                    NotificationManager.error('載入頁面失敗，請稍後再試');
                })
                .finally(() => {
                    LoadingManager.hide();
                });
        }
    };

    // 快取管理器
    const CacheManager = {
        cache: new Map(),

        set(key, value, ttl = 300000) { // 5分鐘預設TTL
            const item = {
                value,
                expiry: Date.now() + ttl
            };
            this.cache.set(key, item);
        },

        get(key) {
            const item = this.cache.get(key);
            if (!item) return null;

            if (Date.now() > item.expiry) {
                this.cache.delete(key);
                return null;
            }

            return item.value;
        },

        clear() {
            this.cache.clear();
        }
    };

    // 公共 API
    return {
        init() {
            // 初始化所有模組
            PerformanceMonitor.init();
            AnimationManager.init();
            FormEnhancer.init();
            NotificationManager.init();
            PaginationManager.init();

            console.log('CioSystem 優化版已初始化');
        },

        // 公開方法
        showNotification: NotificationManager.show.bind(NotificationManager),
        showLoading: LoadingManager.show,
        hideLoading: LoadingManager.hide,
        cache: CacheManager
    };
})();

// 延遲載入非關鍵功能
document.addEventListener('DOMContentLoaded', function () {
    // 立即初始化核心功能
    CioSystem.init();

    // 延遲載入非關鍵功能
    setTimeout(() => {
        loadNonCriticalFeatures();
    }, 100);
});

// 載入非關鍵功能
function loadNonCriticalFeatures() {
    // 載入工具提示
    if ('IntersectionObserver' in window) {
        loadTooltips();
    }

    // 載入圖表（如果需要）
    if (document.querySelector('.chart-container')) {
        loadCharts();
    }
}

// 工具提示載入
function loadTooltips() {
    const tooltipElements = document.querySelectorAll('[data-tooltip]');
    tooltipElements.forEach(element => {
        element.addEventListener('mouseenter', showTooltip);
        element.addEventListener('mouseleave', hideTooltip);
    });
}

function showTooltip(e) {
    const text = e.target.getAttribute('data-tooltip');
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = text;
    tooltip.style.cssText = `
        position: absolute;
        background: var(--text-primary);
        color: var(--text-white);
        padding: 0.5rem 0.75rem;
        border-radius: 6px;
        font-size: 0.875rem;
        z-index: 1000;
        pointer-events: none;
    `;

    document.body.appendChild(tooltip);

    const rect = e.target.getBoundingClientRect();
    tooltip.style.left = rect.left + rect.width / 2 - tooltip.offsetWidth / 2 + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';
}

function hideTooltip() {
    const tooltip = document.querySelector('.tooltip');
    if (tooltip) {
        tooltip.remove();
    }
}

// 圖表載入（如果需要）
function loadCharts() {
    // 這裡可以載入圖表庫
    console.log('載入圖表功能');
}

// 錯誤處理
window.addEventListener('error', function (e) {
    console.error('JavaScript 錯誤:', e.error);
    CioSystem.showNotification('發生錯誤，請重新整理頁面', 'error');
});

// 未處理的 Promise 拒絕
window.addEventListener('unhandledrejection', function (e) {
    console.error('未處理的 Promise 拒絕:', e.reason);
    CioSystem.showNotification('載入失敗，請稍後再試', 'error');
});

// 導出到全域
window.CioSystem = CioSystem;
