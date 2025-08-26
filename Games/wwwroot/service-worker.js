// Games PWA Service Worker
const CACHE_NAME = 'games-pwa-v4';
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
  '/_framework/dotnet.js'
];

// Install event - cache essential files
self.addEventListener('install', event => {
  console.log('Service Worker installing...');
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
      })
    ])
    .then(() => {
      console.log('All essential resources cached successfully');
      // Skip waiting to activate immediately
      return self.skipWaiting();
    })
    .catch(error => {
      console.error('Error during service worker install:', error);
    })
  );
});

// Fetch event - smart caching strategy
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);
  
  // Handle Blazor framework files with cache-first strategy
  if (url.pathname.startsWith('/_framework/')) {
    event.respondWith(
      caches.open(CACHE_NAME).then(cache => {
        return cache.match(event.request).then(cachedResponse => {
          if (cachedResponse) {
            console.log('Serving from cache:', url.pathname);
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
              // Return a basic error response for offline scenarios
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
        
        // For uncached requests, try to fetch from network
        return fetch(event.request)
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

// Activate event - clean up old caches
self.addEventListener('activate', event => {
  console.log('Service Worker activating...');
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheName !== CACHE_NAME) {
            console.log('Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    }).then(() => {
      console.log('Service Worker activated and ready');
      // Take control of all pages immediately
      return self.clients.claim();
    })
  );
});