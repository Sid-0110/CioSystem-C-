/**
 * CioSystem 前端效能配置
 * 集中管理前端優化設定
 */

const FrontendConfig = {
    // 效能設定
    performance: {
        // 啟用效能監控
        enableMonitoring: true,

        // 載入時間警告閾值 (毫秒)
        loadTimeWarning: 3000,

        // 啟用 Web Vitals 監控
        enableWebVitals: true,

        // 啟用資源載入監控
        enableResourceMonitoring: true
    },

    // 快取設定
    cache: {
        // 啟用 Service Worker
        enableServiceWorker: true,

        // 靜態資源快取時間 (毫秒)
        staticCacheTTL: 7 * 24 * 60 * 60 * 1000, // 7天

        // API 快取時間 (毫秒)
        apiCacheTTL: 5 * 60 * 1000, // 5分鐘

        // 頁面快取時間 (毫秒)
        pageCacheTTL: 60 * 60 * 1000, // 1小時

        // 最大快取大小 (MB)
        maxCacheSize: 50
    },

    // 圖片優化設定
    images: {
        // 啟用延遲載入
        enableLazyLoading: true,

        // 啟用響應式圖片
        enableResponsiveImages: true,

        // 啟用圖片壓縮
        enableCompression: true,

        // 預設圖片品質 (0-1)
        defaultQuality: 0.8,

        // 最大圖片寬度
        maxWidth: 1920,

        // 支援的圖片格式
        supportedFormats: ['webp', 'jpeg', 'png', 'gif'],

        // 預載入關鍵圖片
        preloadCriticalImages: true
    },

    // 載入優化設定
    loading: {
        // 啟用關鍵路徑優化
        enableCriticalPathOptimization: true,

        // 啟用資源預載入
        enablePreloading: true,

        // 啟用漸進式載入
        enableProgressiveLoading: true,

        // 延遲載入非關鍵資源
        deferNonCriticalResources: true,

        // 延遲載入時間 (毫秒)
        deferDelay: 100,

        // 預載入下一頁資源數量
        preloadNextPageCount: 3
    },

    // 動畫設定
    animations: {
        // 啟用動畫
        enableAnimations: true,

        // 動畫持續時間 (毫秒)
        duration: 300,

        // 啟用減少動畫 (尊重用戶偏好)
        respectReducedMotion: true,

        // 啟用交點觀察器
        enableIntersectionObserver: true,

        // 動畫閾值
        animationThreshold: 0.1
    },

    // 表單設定
    forms: {
        // 啟用自動儲存
        enableAutoSave: true,

        // 自動儲存延遲 (毫秒)
        autoSaveDelay: 1000,

        // 啟用即時驗證
        enableRealTimeValidation: true,

        // 啟用輸入增強
        enableInputEnhancements: true
    },

    // 通知設定
    notifications: {
        // 啟用通知
        enableNotifications: true,

        // 預設顯示時間 (毫秒)
        defaultDuration: 5000,

        // 最大通知數量
        maxNotifications: 5,

        // 啟用推送通知
        enablePushNotifications: true
    },

    // 錯誤處理設定
    errorHandling: {
        // 啟用全域錯誤處理
        enableGlobalErrorHandling: true,

        // 啟用錯誤報告
        enableErrorReporting: true,

        // 錯誤報告端點
        errorReportingEndpoint: '/api/errors',

        // 啟用離線錯誤處理
        enableOfflineErrorHandling: true
    },

    // 網路設定
    network: {
        // 啟用離線支援
        enableOfflineSupport: true,

        // 離線頁面路徑
        offlinePagePath: '/offline.html',

        // 啟用背景同步
        enableBackgroundSync: true,

        // 重試次數
        maxRetries: 3,

        // 重試延遲 (毫秒)
        retryDelay: 1000
    },

    // 開發設定
    development: {
        // 啟用開發模式
        enableDevMode: false,

        // 啟用詳細日誌
        enableVerboseLogging: false,

        // 啟用效能分析
        enableProfiling: false,

        // 啟用熱重載
        enableHotReload: false
    },

    // 初始化方法
    init() {
        this.applySettings();
        this.setupEventListeners();
        this.initializeModules();
    },

    // 應用設定
    applySettings() {
        // 應用效能設定
        if (this.performance.enableMonitoring) {
            this.setupPerformanceMonitoring();
        }

        // 應用快取設定
        if (this.cache.enableServiceWorker) {
            this.setupServiceWorker();
        }

        // 應用圖片優化設定
        if (this.images.enableLazyLoading) {
            this.setupImageOptimization();
        }

        // 應用載入優化設定
        if (this.loading.enableCriticalPathOptimization) {
            this.setupLoadingOptimization();
        }
    },

    // 設定事件監聽器
    setupEventListeners() {
        // 網路狀態變化
        window.addEventListener('online', () => {
            this.handleOnline();
        });

        window.addEventListener('offline', () => {
            this.handleOffline();
        });

        // 頁面可見性變化
        document.addEventListener('visibilitychange', () => {
            this.handleVisibilityChange();
        });

        // 頁面卸載
        window.addEventListener('beforeunload', () => {
            this.handleBeforeUnload();
        });
    },

    // 初始化模組
    initializeModules() {
        // 初始化效能監控
        if (this.performance.enableMonitoring) {
            this.initializePerformanceMonitoring();
        }

        // 初始化快取管理
        if (this.cache.enableServiceWorker) {
            this.initializeCacheManagement();
        }

        // 初始化圖片優化
        if (this.images.enableLazyLoading) {
            this.initializeImageOptimization();
        }
    },

    // 效能監控設定
    setupPerformanceMonitoring() {
        if (this.performance.enableWebVitals && 'PerformanceObserver' in window) {
            this.observeWebVitals();
        }
    },

    // 觀察 Web Vitals
    observeWebVitals() {
        // LCP 監控
        new PerformanceObserver((list) => {
            const entries = list.getEntries();
            const lastEntry = entries[entries.length - 1];
            console.log('LCP:', lastEntry.startTime);
        }).observe({ entryTypes: ['largest-contentful-paint'] });

        // FID 監控
        new PerformanceObserver((list) => {
            const entries = list.getEntries();
            entries.forEach(entry => {
                console.log('FID:', entry.processingStart - entry.startTime);
            });
        }).observe({ entryTypes: ['first-input'] });

        // CLS 監控
        new PerformanceObserver((list) => {
            const entries = list.getEntries();
            entries.forEach(entry => {
                if (!entry.hadRecentInput) {
                    console.log('CLS:', entry.value);
                }
            });
        }).observe({ entryTypes: ['layout-shift'] });
    },

    // Service Worker 設定
    setupServiceWorker() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.register('/sw.js')
                .then(registration => {
                    console.log('Service Worker 註冊成功:', registration);
                })
                .catch(error => {
                    console.log('Service Worker 註冊失敗:', error);
                });
        }
    },

    // 圖片優化設定
    setupImageOptimization() {
        if (window.ImageOptimizer) {
            window.imageOptimizer = new window.ImageOptimizer();
        }
    },

    // 載入優化設定
    setupLoadingOptimization() {
        if (window.LoadingOptimizer) {
            window.loadingOptimizer = new window.LoadingOptimizer();
        }
    },

    // 處理線上狀態
    handleOnline() {
        console.log('網路已連接');
        if (window.CioSystem && window.CioSystem.showNotification) {
            window.CioSystem.showNotification('網路已連接', 'success');
        }
    },

    // 處理離線狀態
    handleOffline() {
        console.log('網路已斷開');
        if (window.CioSystem && window.CioSystem.showNotification) {
            window.CioSystem.showNotification('網路已斷開，部分功能可能受限', 'warning');
        }
    },

    // 處理頁面可見性變化
    handleVisibilityChange() {
        if (document.hidden) {
            console.log('頁面隱藏');
        } else {
            console.log('頁面顯示');
        }
    },

    // 處理頁面卸載
    handleBeforeUnload() {
        // 清理資源
        this.cleanup();
    },

    // 清理資源
    cleanup() {
        // 清理快取
        if (window.CioSystem && window.CioSystem.cache) {
            window.CioSystem.cache.clear();
        }

        // 清理觀察器
        if (window.imageOptimizer) {
            window.imageOptimizer.destroy();
        }

        if (window.loadingOptimizer) {
            window.loadingOptimizer.cleanup();
        }
    },

    // 獲取配置
    getConfig(section) {
        return this[section] || null;
    },

    // 更新配置
    updateConfig(section, key, value) {
        if (this[section] && this[section][key] !== undefined) {
            this[section][key] = value;
        }
    },

    // 重置配置
    resetConfig() {
        // 重新載入頁面以重置所有配置
        window.location.reload();
    }
};

// 自動初始化
document.addEventListener('DOMContentLoaded', function () {
    FrontendConfig.init();
});

// 導出到全域
window.FrontendConfig = FrontendConfig;