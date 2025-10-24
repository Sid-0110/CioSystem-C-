/**
 * 載入優化器 - 關鍵路徑優化和資源預載入
 */

class LoadingOptimizer {
    constructor() {
        this.resourceCache = new Map();
        this.loadingQueue = [];
        this.isLoading = false;
        this.init();
    }

    init() {
        this.optimizeCriticalPath();
        this.setupResourcePreloading();
        this.setupProgressiveLoading();
        this.setupServiceWorker();
    }

    // 優化關鍵路徑
    optimizeCriticalPath() {
        // 預載入關鍵 CSS
        this.preloadCriticalCSS();

        // 預載入關鍵字體
        this.preloadCriticalFonts();

        // 優化 JavaScript 載入
        this.optimizeJavaScriptLoading();
    }

    // 預載入關鍵 CSS
    preloadCriticalCSS() {
        const criticalCSS = document.querySelector('link[rel="stylesheet"][data-critical]');
        if (criticalCSS) {
            const link = document.createElement('link');
            link.rel = 'preload';
            link.as = 'style';
            link.href = criticalCSS.href;
            link.onload = () => {
                link.rel = 'stylesheet';
            };
            document.head.insertBefore(link, criticalCSS);
        }
    }

    // 預載入關鍵字體
    preloadCriticalFonts() {
        const fontUrls = [
            'https://fonts.gstatic.com/s/inter/v12/UcCO3FwrK3iLTeHuS_fvQtMwCp50KnMw2boKoduKmMEVuLyfAZ9hiJ-Ek-_EeA.woff2',
            'https://fonts.gstatic.com/s/notosanskr/v36/PbykFmXiEBPT4ITbgNA5Cgm20xz64px_1H4w.woff2'
        ];

        fontUrls.forEach(url => {
            const link = document.createElement('link');
            link.rel = 'preload';
            link.as = 'font';
            link.type = 'font/woff2';
            link.crossOrigin = 'anonymous';
            link.href = url;
            document.head.appendChild(link);
        });
    }

    // 優化 JavaScript 載入
    optimizeJavaScriptLoading() {
        // 延遲載入非關鍵 JavaScript
        const nonCriticalScripts = document.querySelectorAll('script[data-defer]');
        nonCriticalScripts.forEach(script => {
            this.deferScript(script);
        });

        // 使用 requestIdleCallback 載入低優先級資源
        if ('requestIdleCallback' in window) {
            requestIdleCallback(() => {
                this.loadLowPriorityResources();
            });
        } else {
            setTimeout(() => {
                this.loadLowPriorityResources();
            }, 2000);
        }
    }

    // 延遲載入腳本
    deferScript(script) {
        const newScript = document.createElement('script');
        newScript.src = script.src;
        newScript.async = true;
        newScript.onload = () => {
            script.remove();
        };
        document.head.appendChild(newScript);
    }

    // 載入低優先級資源
    loadLowPriorityResources() {
        const lowPriorityResources = document.querySelectorAll('[data-low-priority]');
        lowPriorityResources.forEach(resource => {
            this.loadResource(resource);
        });
    }

    // 載入資源
    loadResource(resource) {
        const type = resource.tagName.toLowerCase();
        const src = resource.getAttribute('data-src') || resource.src;

        if (!src) return;

        switch (type) {
            case 'script':
                this.loadScript(src);
                break;
            case 'link':
                this.loadStylesheet(src);
                break;
            case 'img':
                this.loadImage(src);
                break;
        }
    }

    // 載入腳本
    loadScript(src) {
        return new Promise((resolve, reject) => {
            if (this.resourceCache.has(src)) {
                resolve(this.resourceCache.get(src));
                return;
            }

            const script = document.createElement('script');
            script.src = src;
            script.async = true;
            script.onload = () => {
                this.resourceCache.set(src, true);
                resolve(true);
            };
            script.onerror = () => {
                reject(new Error(`Failed to load script: ${src}`));
            };
            document.head.appendChild(script);
        });
    }

    // 載入樣式表
    loadStylesheet(href) {
        return new Promise((resolve, reject) => {
            if (this.resourceCache.has(href)) {
                resolve(this.resourceCache.get(href));
                return;
            }

            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.onload = () => {
                this.resourceCache.set(href, true);
                resolve(true);
            };
            link.onerror = () => {
                reject(new Error(`Failed to load stylesheet: ${href}`));
            };
            document.head.appendChild(link);
        });
    }

    // 載入圖片
    loadImage(src) {
        return new Promise((resolve, reject) => {
            if (this.resourceCache.has(src)) {
                resolve(this.resourceCache.get(src));
                return;
            }

            const img = new Image();
            img.onload = () => {
                this.resourceCache.set(src, true);
                resolve(true);
            };
            img.onerror = () => {
                reject(new Error(`Failed to load image: ${src}`));
            };
            img.src = src;
        });
    }

    // 設定資源預載入
    setupResourcePreloading() {
        // 預載入下一頁可能需要的資源
        this.preloadNextPageResources();

        // 預載入用戶可能點擊的資源
        this.preloadHoverResources();
    }

    // 預載入下一頁資源
    preloadNextPageResources() {
        const links = document.querySelectorAll('a[href]');
        const nextPageResources = new Set();

        links.forEach(link => {
            const href = link.getAttribute('href');
            if (this.isInternalLink(href)) {
                nextPageResources.add(href);
            }
        });

        // 限制預載入數量
        const limitedResources = Array.from(nextPageResources).slice(0, 3);
        limitedResources.forEach(href => {
            this.preloadPage(href);
        });
    }

    // 預載入頁面
    preloadPage(href) {
        const link = document.createElement('link');
        link.rel = 'prefetch';
        link.href = href;
        document.head.appendChild(link);
    }

    // 預載入懸停資源
    preloadHoverResources() {
        const hoverElements = document.querySelectorAll('[data-preload-on-hover]');
        hoverElements.forEach(element => {
            element.addEventListener('mouseenter', () => {
                const resource = element.getAttribute('data-preload-on-hover');
                this.loadResource({ tagName: 'script', src: resource });
            }, { once: true });
        });
    }

    // 設定漸進式載入
    setupProgressiveLoading() {
        // 優先載入可見內容
        this.loadVisibleContent();

        // 延遲載入不可見內容
        this.deferInvisibleContent();
    }

    // 載入可見內容
    loadVisibleContent() {
        const visibleElements = document.querySelectorAll('[data-visible]');
        visibleElements.forEach(element => {
            this.loadElementResources(element);
        });
    }

    // 延遲載入不可見內容
    deferInvisibleContent() {
        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        this.loadElementResources(entry.target);
                        observer.unobserve(entry.target);
                    }
                });
            });

            const invisibleElements = document.querySelectorAll('[data-invisible]');
            invisibleElements.forEach(element => {
                observer.observe(element);
            });
        }
    }

    // 載入元素資源
    loadElementResources(element) {
        const scripts = element.querySelectorAll('script[data-src]');
        const styles = element.querySelectorAll('link[data-href]');
        const images = element.querySelectorAll('img[data-src]');

        [...scripts, ...styles, ...images].forEach(resource => {
            this.loadResource(resource);
        });
    }

    // 設定 Service Worker
    setupServiceWorker() {
        if ('serviceWorker' in navigator) {
            window.addEventListener('load', () => {
                navigator.serviceWorker.register('/sw.js')
                    .then(registration => {
                        console.log('Service Worker 註冊成功:', registration);
                    })
                    .catch(error => {
                        console.log('Service Worker 註冊失敗:', error);
                    });
            });
        }
    }

    // 檢查是否為內部連結
    isInternalLink(href) {
        try {
            const url = new URL(href, window.location.origin);
            return url.origin === window.location.origin;
        } catch {
            return false;
        }
    }

    // 批量載入資源
    async loadResourcesBatch(resources) {
        const promises = resources.map(resource => this.loadResource(resource));
        return Promise.allSettled(promises);
    }

    // 清理資源
    cleanup() {
        this.resourceCache.clear();
        this.loadingQueue = [];
    }

    // 獲取載入統計
    getLoadingStats() {
        return {
            cachedResources: this.resourceCache.size,
            loadingQueue: this.loadingQueue.length,
            isLoading: this.isLoading
        };
    }
}

// 初始化載入優化器
document.addEventListener('DOMContentLoaded', function () {
    window.loadingOptimizer = new LoadingOptimizer();
});

// 導出類別
window.LoadingOptimizer = LoadingOptimizer;
