/**
 * 圖片優化器 - 響應式圖片和延遲載入
 */

class ImageOptimizer {
    constructor() {
        this.observer = null;
        this.init();
    }

    init() {
        this.setupLazyLoading();
        this.setupResponsiveImages();
        this.setupImageCompression();
    }

    // 延遲載入設定
    setupLazyLoading() {
        if ('IntersectionObserver' in window) {
            this.observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        this.loadImage(entry.target);
                        this.observer.unobserve(entry.target);
                    }
                });
            }, {
                rootMargin: '50px 0px',
                threshold: 0.01
            });

            // 觀察所有需要延遲載入的圖片
            document.querySelectorAll('img[data-src]').forEach(img => {
                this.observer.observe(img);
            });
        }
    }

    // 載入圖片
    loadImage(img) {
        const src = img.getAttribute('data-src');
        if (!src) return;

        // 創建新的圖片元素
        const newImg = new Image();
        
        newImg.onload = () => {
            img.src = src;
            img.classList.add('loaded');
            img.removeAttribute('data-src');
        };

        newImg.onerror = () => {
            img.classList.add('error');
            console.error('圖片載入失敗:', src);
        };

        newImg.src = src;
    }

    // 響應式圖片設定
    setupResponsiveImages() {
        const images = document.querySelectorAll('img[data-responsive]');
        images.forEach(img => {
            this.createResponsiveImage(img);
        });
    }

    // 創建響應式圖片
    createResponsiveImage(img) {
        const src = img.src;
        const alt = img.alt;
        const sizes = img.getAttribute('data-sizes') || '100vw';
        
        // 生成不同尺寸的圖片URL
        const srcset = this.generateSrcSet(src);
        
        img.setAttribute('srcset', srcset);
        img.setAttribute('sizes', sizes);
        img.setAttribute('loading', 'lazy');
    }

    // 生成 srcset
    generateSrcSet(baseSrc) {
        const widths = [320, 640, 768, 1024, 1280, 1920];
        const srcset = widths.map(width => {
            const url = this.getOptimizedImageUrl(baseSrc, width);
            return `${url} ${width}w`;
        }).join(', ');
        
        return srcset;
    }

    // 獲取優化的圖片URL
    getOptimizedImageUrl(originalUrl, width) {
        // 這裡可以整合圖片CDN或優化服務
        // 例如：使用 Cloudinary、ImageKit 等
        const url = new URL(originalUrl);
        url.searchParams.set('w', width);
        url.searchParams.set('q', '80'); // 品質參數
        url.searchParams.set('f', 'auto'); // 格式自動選擇
        return url.toString();
    }

    // 圖片壓縮設定
    setupImageCompression() {
        const fileInputs = document.querySelectorAll('input[type="file"][accept*="image"]');
        fileInputs.forEach(input => {
            input.addEventListener('change', (e) => {
                this.compressImages(e.target.files);
            });
        });
    }

    // 壓縮圖片
    async compressImages(files) {
        const compressedFiles = [];
        
        for (let file of files) {
            try {
                const compressedFile = await this.compressImage(file);
                compressedFiles.push(compressedFile);
            } catch (error) {
                console.error('圖片壓縮失敗:', error);
                compressedFiles.push(file); // 使用原始檔案
            }
        }
        
        return compressedFiles;
    }

    // 壓縮單個圖片
    compressImage(file, maxWidth = 1920, quality = 0.8) {
        return new Promise((resolve) => {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            const img = new Image();
            
            img.onload = () => {
                // 計算新尺寸
                let { width, height } = img;
                if (width > maxWidth) {
                    height = (height * maxWidth) / width;
                    width = maxWidth;
                }
                
                // 設定畫布尺寸
                canvas.width = width;
                canvas.height = height;
                
                // 繪製壓縮後的圖片
                ctx.drawImage(img, 0, 0, width, height);
                
                // 轉換為 Blob
                canvas.toBlob((blob) => {
                    const compressedFile = new File([blob], file.name, {
                        type: 'image/jpeg',
                        lastModified: Date.now()
                    });
                    resolve(compressedFile);
                }, 'image/jpeg', quality);
            };
            
            img.src = URL.createObjectURL(file);
        });
    }

    // 預載入關鍵圖片
    preloadCriticalImages() {
        const criticalImages = document.querySelectorAll('img[data-critical]');
        criticalImages.forEach(img => {
            const link = document.createElement('link');
            link.rel = 'preload';
            link.as = 'image';
            link.href = img.src || img.getAttribute('data-src');
            document.head.appendChild(link);
        });
    }

    // 生成 WebP 格式（如果支援）
    generateWebP(src) {
        if (this.supportsWebP()) {
            return src.replace(/\.(jpg|jpeg|png)$/i, '.webp');
        }
        return src;
    }

    // 檢查 WebP 支援
    supportsWebP() {
        const canvas = document.createElement('canvas');
        canvas.width = 1;
        canvas.height = 1;
        return canvas.toDataURL('image/webp').indexOf('data:image/webp') === 0;
    }

    // 清理資源
    destroy() {
        if (this.observer) {
            this.observer.disconnect();
        }
    }
}

// 初始化圖片優化器
document.addEventListener('DOMContentLoaded', function() {
    window.imageOptimizer = new ImageOptimizer();
});

// 導出類別
window.ImageOptimizer = ImageOptimizer;