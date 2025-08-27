// Games PWA Service Worker with Dynamic Versioning and Update Detection
const CACHE_VERSION = '20250827-005226'; // Will be replaced during build
const CACHE_NAME = `games-pwa-${CACHE_VERSION}`;
const UPDATE_CHECK_INTERVAL = 60000; // Check for updates every minute
const staticUrlsToCache = [
'/',
'/index.html',
'/css/app.css',
'/Games.styles.css',
'/js/virtualjoystick.js',
'/js/gamepad.js',
'/lib/bootstrap/dist/css/bootstrap.min.css',
'/favicon.png',
'/icon-192.png',
'/manifest.json'
];
// Essential Blazor files that must be cached for offline functionality
const essentialBlazorFiles = [
'/_framework/blazor.webassembly.js',
'/_framework/blazor.boot.json',
'/_framework/dotnet.js',
// Core runtime files needed for offline startup
'/_framework/dotnet.native.50iqa8w3ys.js',
'/_framework/dotnet.runtime.ew19f13umk.js',
'/_framework/dotnet.native.pk43x8e436.wasm',
// Core assemblies needed for Blazor to start
'/_framework/System.Runtime.InteropServices.JavaScript.wt3af2xdkg.wasm',
'/_framework/System.Private.CoreLib.9ecf2cskpd.wasm',
// ICU data files for globalization
'/_framework/icudt_no_CJK.lfu7j35m59.dat',
'/_framework/icudt_EFIGS.tptq2av103.dat',
'/_framework/icudt_CJK.tjcz0u77k5.dat'
];
// Install event - cache essential files
self.addEventListener('install', event => {
console.log('Service Worker installing with version:', CACHE_VERSION);
event.waitUntil(
Promise.all([
// Cache static resources
caches.open(CACHE_NAME).then(cache => {
console.log('Caching static resources');
return cache.addAll(staticUrlsToCache);
}),
// Cache essential Blazor files
caches.open(CACHE_NAME).then(cache => {
console.log('Caching essential Blazor files');
return cache.addAll(essentialBlazorFiles);
}),
// Try to load and cache additional critical files from blazor.boot.json
caches.open(CACHE_NAME).then(cache => {
console.log('Attempting to cache additional critical files');
return fetch('/_framework/blazor.boot.json')
.then(response => response.json())
.then(bootData => {
const additionalCriticalFiles = [];
// Add core assembly files
if (bootData.resources && bootData.resources.coreAssembly) {
Object.keys(bootData.resources.coreAssembly).forEach(file => {
additionalCriticalFiles.push(`/_framework/${file}`);
});
}
// Add JS module files
if (bootData.resources && bootData.resources.jsModuleNative) {
Object.keys(bootData.resources.jsModuleNative).forEach(file => {
additionalCriticalFiles.push(`/_framework/${file}`);
});
}
if (bootData.resources && bootData.resources.jsModuleRuntime) {
Object.keys(bootData.resources.jsModuleRuntime).forEach(file => {
additionalCriticalFiles.push(`/_framework/${file}`);
});
}
// Add WASM native files
if (bootData.resources && bootData.resources.wasmNative) {
Object.keys(bootData.resources.wasmNative).forEach(file => {
additionalCriticalFiles.push(`/_framework/${file}`);
});
}
console.log('Additional critical files to cache:', additionalCriticalFiles);
// Cache each file individually to avoid failures breaking the whole install
const cachePromises = additionalCriticalFiles.map(file => {
return cache.add(file).catch(error => {
console.warn('Failed to cache critical file during install:', file, error);
});
});
return Promise.all(cachePromises);
})
.catch(error => {
console.warn('Could not load blazor.boot.json during install, using basic file list', error);
});
})
])
.then(() => {
console.log('All essential resources cached successfully');
// Skip waiting to activate immediately when first installed
return self.skipWaiting();
})
.catch(error => {
console.error('Error during service worker install:', error);
})
);
});
// Activate event - clean up old caches and notify clients of updates
self.addEventListener('activate', event => {
console.log('Service Worker activating with version:', CACHE_VERSION);
event.waitUntil(
caches.keys().then(cacheNames => {
const oldCaches = cacheNames.filter(cacheName =>
cacheName.startsWith('games-pwa-') && cacheName !== CACHE_NAME
);
if (oldCaches.length > 0) {
console.log('Found old caches, app has been updated!');
// Notify all clients about the update
notifyClientsOfUpdate();
}
return Promise.all([
// Delete old caches
...oldCaches.map(cacheName => {
console.log('Deleting old cache:', cacheName);
return caches.delete(cacheName);
}),
// Take control of all pages immediately
self.clients.claim()
]);
}).then(() => {
console.log('Service Worker activated and ready');
})
);
});
// Notify all clients that an update is available
function notifyClientsOfUpdate() {
self.clients.matchAll().then(clients => {
clients.forEach(client => {
client.postMessage({
type: 'APP_UPDATE_AVAILABLE',
version: CACHE_VERSION
});
});
});
}
// Fetch event - network-first for critical files, cache-first for static assets
self.addEventListener('fetch', event => {
const url = new URL(event.request.url);
// Skip non-GET requests
if (event.request.method !== 'GET') {
return;
}
// Handle app shell files (index.html, root) with network-first strategy for updates
if (url.pathname === '/' || url.pathname === '/index.html' ||
url.pathname.startsWith('/about') || url.pathname.startsWith('/tanks')) {
event.respondWith(
fetch(event.request)
.then(response => {
if (response.ok) {
// Cache the updated response
caches.open(CACHE_NAME).then(cache => {
cache.put(event.request, response.clone());
});
}
return response;
})
.catch(() => {
// If network fails, serve from cache
return caches.match(event.request).then(cachedResponse => {
if (cachedResponse) {
console.log('Serving from cache (offline):', url.pathname);
return cachedResponse;
}
// If no cache and offline, serve the main page for navigation
return caches.match('/');
});
})
);
return;
}
// Handle Blazor framework files with cache-first strategy but check for updates
if (url.pathname.startsWith('/_framework/')) {
event.respondWith(
caches.open(CACHE_NAME).then(cache => {
return cache.match(event.request).then(cachedResponse => {
// Serve from cache immediately
if (cachedResponse) {
console.log('Serving from cache:', url.pathname);
// Check for updates in the background
fetch(event.request).then(response => {
if (response.ok && response.headers.get('last-modified') !==
cachedResponse.headers.get('last-modified')) {
console.log('Updating cached framework file:', url.pathname);
cache.put(event.request, response.clone());
}
}).catch(() => {
// Ignore fetch errors for background updates
});
return cachedResponse;
}
// Not in cache, try to fetch and cache
return fetch(event.request)
.then(response => {
if (response.ok) {
console.log('Caching framework file:', url.pathname);
cache.put(event.request, response.clone());
}
return response;
})
.catch(error => {
console.error('Failed to fetch framework file:', url.pathname, error);
// For critical files, provide better error handling
if (url.pathname.includes('blazor.boot.json') ||
url.pathname.includes('dotnet.js') ||
url.pathname.includes('blazor.webassembly.js')) {
console.error('Critical Blazor file missing from cache:', url.pathname);
// Return a response that won't break Blazor startup completely
return new Response(JSON.stringify({error: 'Critical file not available offline'}), {
status: 503,
headers: { 'Content-Type': 'application/json' }
});
}
return new Response('Resource not available offline', {
status: 503,
headers: { 'Content-Type': 'text/plain' }
});
});
});
})
);
return;
}
// Handle all other requests with cache-first strategy
event.respondWith(
caches.match(event.request)
.then(response => {
// Return cached version if found
if (response) {
console.log('Serving from cache:', url.pathname);
return response;
}
// For uncached requests, try to fetch from network and cache if successful
return fetch(event.request)
.then(response => {
if (response.ok && response.status < 400) {
caches.open(CACHE_NAME).then(cache => {
cache.put(event.request, response.clone());
});
}
return response;
})
.catch(() => {
// If offline and no cache, return appropriate response
if (event.request.mode === 'navigate') {
console.log('Serving cached index for navigation request');
return caches.match('/') || new Response('App is offline', {
status: 503,
headers: { 'Content-Type': 'text/plain' }
});
}
throw new Error('Network request failed and no cache available');
});
})
);
});
// Listen for messages from the client
self.addEventListener('message', event => {
if (event.data && event.data.type === 'SKIP_WAITING') {
console.log('Client requested to skip waiting, activating new service worker');
self.skipWaiting();
}
});
