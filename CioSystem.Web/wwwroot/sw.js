
/**
 * CioSystem Service Worker - 快取策略和離線支援
 */

const CACHE_NAME = 'ciosystem-v1.0.0';
const STATIC_CACHE = 'ciosystem-static-v1.0.0';
const DYNAMIC_CACHE = 'ciosystem-dynamic-v1.0.0';
const API_CACHE = 'ciosystem-api-v1.0.0';

// 需要快取的靜態資源
const STATIC_ASSETS = [
    '/',
    '/css/optimized.css',
    '/js/optimized.js',
    '/js/image-optimizer.js',
    '/js/loading-optimizer.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/favicon.ico'
];

// 需要快取的 API 端點
const API_ENDPOINTS = [
    '/api/products',
    '/api/inventory',
    '/api/sales',
    '/api/purchases'
];

// 安裝事件
self.addEventListener('install', event => {
    console.log('Service Worker 安裝中...');

    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then(cache => {
                console.log('快取靜態資源...');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                console.log('靜態資源快取完成');
                return self.skipWaiting();
            })
            .catch(error => {
                console.error('靜態資源快取失敗:', error);
            })
    );
});

// 啟用事件
self.addEventListener('activate', event => {
    console.log('Service Worker 啟用中...');

    event.waitUntil(
        caches.keys()
            .then(cacheNames => {
                return Promise.all(
                    cacheNames.map(cacheName => {
                        if (cacheName !== STATIC_CACHE &&
                            cacheName !== DYNAMIC_CACHE &&
                            cacheName !== API_CACHE) {
                            console.log('刪除舊快取:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
            .then(() => {
                console.log('Service Worker 啟用完成');
                return self.clients.claim();
            })
    );
});

// 攔截請求
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // 只處理 GET 請求
    if (request.method !== 'GET') {
        return;
    }

    // 處理不同類型的請求
    if (isStaticAsset(request.url)) {
        event.respondWith(handleStaticAsset(request));
    } else if (isApiRequest(request.url)) {
        event.respondWith(handleApiRequest(request));
    } else if (isPageRequest(request.url)) {
        event.respondWith(handlePageRequest(request));
    } else {
        event.respondWith(handleOtherRequest(request));
    }
});

// 處理靜態資源
async function handleStaticAsset(request) {
    try {
        // 先嘗試從快取取得
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        // 快取未命中，從網路取得
        const networkResponse = await fetch(request);

        // 快取響應
        if (networkResponse.ok) {
            const cache = await caches.open(STATIC_CACHE);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.error('靜態資源載入失敗:', error);
        return new Response('資源載入失敗', { status: 404 });
    }
}

// 處理 API 請求
async function handleApiRequest(request) {
    try {
        // 先嘗試從快取取得
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            // 檢查快取是否過期
            const cacheTime = cachedResponse.headers.get('sw-cache-time');
            if (cacheTime && Date.now() - parseInt(cacheTime) < 300000) { // 5分鐘
                return cachedResponse;
            }
        }

        // 從網路取得最新資料
        const networkResponse = await fetch(request);

        if (networkResponse.ok) {
            // 快取 API 響應
            const cache = await caches.open(API_CACHE);
            const responseToCache = networkResponse.clone();
            responseToCache.headers.set('sw-cache-time', Date.now().toString());
            cache.put(request, responseToCache);
        }

        return networkResponse;
    } catch (error) {
        console.error('API 請求失敗:', error);

        // 網路錯誤時返回快取資料
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        return new Response('API 請求失敗', { status: 503 });
    }
}

// 處理頁面請求
async function handlePageRequest(request) {
    try {
        // 先嘗試從網路取得
        const networkResponse = await fetch(request);

        if (networkResponse.ok) {
            // 快取頁面
            const cache = await caches.open(DYNAMIC_CACHE);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.error('頁面載入失敗:', error);

        // 網路錯誤時返回快取頁面
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        // 返回離線頁面
        return caches.match('/offline.html') ||
            new Response('頁面載入失敗', { status: 503 });
    }
}

// 處理其他請求
async function handleOtherRequest(request) {
    try {
        return await fetch(request);
    } catch (error) {
        console.error('請求失敗:', error);
        return new Response('請求失敗', { status: 503 });
    }
}

// 檢查是否為靜態資源
function isStaticAsset(url) {
    return url.includes('/css/') ||
        url.includes('/js/') ||
        url.includes('/lib/') ||
        url.includes('/images/') ||
        url.includes('.ico') ||
        url.includes('.png') ||
        url.includes('.jpg') ||
        url.includes('.jpeg') ||
        url.includes('.gif') ||
        url.includes('.svg');
}

// 檢查是否為 API 請求
function isApiRequest(url) {
    return url.includes('/api/');
}

// 檢查是否為頁面請求
function isPageRequest(url) {
    const urlObj = new URL(url);
    return urlObj.pathname === '/' ||
        urlObj.pathname.startsWith('/Products') ||
        urlObj.pathname.startsWith('/Inventory') ||
        urlObj.pathname.startsWith('/Sales') ||
        urlObj.pathname.startsWith('/Purchases') ||
        urlObj.pathname.startsWith('/Dashboard');
}

// 背景同步
self.addEventListener('sync', event => {
    if (event.tag === 'background-sync') {
        event.waitUntil(doBackgroundSync());
    }
});

// 執行背景同步
async function doBackgroundSync() {
    try {
        // 同步離線資料
        console.log('執行背景同步...');

        // 這裡可以實現離線資料同步邏輯
        // 例如：同步表單資料、上傳檔案等

    } catch (error) {
        console.error('背景同步失敗:', error);
    }
}

// 推送通知
self.addEventListener('push', event => {
    if (event.data) {
        const data = event.data.json();
        const options = {
            body: data.body,
            icon: '/favicon.ico',
            badge: '/favicon.ico',
            tag: 'ciosystem-notification',
            requireInteraction: true,
            actions: [
                {
                    action: 'view',
                    title: '查看'
                },
                {
                    action: 'close',
                    title: '關閉'
                }
            ]
        };

        event.waitUntil(
            self.registration.showNotification(data.title, options)
        );
    }
});

// 通知點擊事件
self.addEventListener('notificationclick', event => {
    event.notification.close();

    if (event.action === 'view') {
        event.waitUntil(
            clients.openWindow('/')
        );
    }
});

// 快取管理
async function manageCache() {
    const cacheNames = await caches.keys();
    const maxCacheSize = 50 * 1024 * 1024; // 50MB

    for (const cacheName of cacheNames) {
        const cache = await caches.open(cacheName);
        const keys = await cache.keys();

        if (keys.length > 100) { // 限制快取項目數量
            const keysToDelete = keys.slice(0, keys.length - 100);
            await Promise.all(keysToDelete.map(key => cache.delete(key)));
        }
    }
}

// 定期清理快取
setInterval(manageCache, 60000); // 每分鐘檢查一次