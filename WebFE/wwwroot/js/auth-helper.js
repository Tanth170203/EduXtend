/**
 * Authentication Helper - Automatic Token Refresh
 * Handles token refresh automatically when server returns 401
 */

// Global flag to prevent multiple simultaneous refresh attempts
let isRefreshing = false;
let refreshSubscribers = [];

/**
 * Subscribe to token refresh completion
 */
function subscribeTokenRefresh(callback) {
    refreshSubscribers.push(callback);
}

/**
 * Notify all subscribers that token has been refreshed
 */
function onTokenRefreshed() {
    refreshSubscribers.forEach(callback => callback());
    refreshSubscribers = [];
}

/**
 * Enhanced fetch wrapper with automatic token refresh
 * Usage: authFetch('/api/endpoint', { method: 'GET' })
 */
async function authFetch(url, options = {}) {
    // Ensure credentials are included to send cookies
    options.credentials = options.credentials || 'include';
    
    try {
        let response = await fetch(url, options);
        
        // Check if token was auto-refreshed by server
        if (response.headers.get('X-Token-Refreshed') === 'true') {
            console.log('✅ Token was automatically refreshed by server');
        }
        
        // If still 401 and we have refresh token, try manual refresh
        if (response.status === 401 && !isRefreshing) {
            console.log('🔄 Token expired, attempting refresh...');
            
            isRefreshing = true;
            
            try {
                // Call refresh endpoint
                const refreshResponse = await fetch('/api/auth/refresh', {
                    method: 'POST',
                    credentials: 'include'
                });
                
                if (refreshResponse.ok) {
                    console.log('✅ Token refreshed successfully');
                    isRefreshing = false;
                    onTokenRefreshed();
                    
                    // Retry original request
                    response = await fetch(url, options);
                } else {
                    console.log('❌ Token refresh failed, redirecting to login...');
                    isRefreshing = false;
                    window.location.href = '/Auth/Login';
                    throw new Error('Session expired. Please login again.');
                }
            } catch (error) {
                isRefreshing = false;
                throw error;
            }
        } else if (response.status === 401 && isRefreshing) {
            // Wait for ongoing refresh to complete
            await new Promise(resolve => subscribeTokenRefresh(resolve));
            // Retry original request
            response = await fetch(url, options);
        }
        
        return response;
    } catch (error) {
        console.error('Request failed:', error);
        throw error;
    }
}

/**
 * Helper to make authenticated API calls
 * Returns parsed JSON or throws error
 */
async function apiCall(url, options = {}) {
    const response = await authFetch(url, options);
    
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Request failed' }));
        throw new Error(error.message || `HTTP ${response.status}`);
    }
    
    return response.json();
}

/**
 * GET request helper
 */
async function apiGet(url) {
    return apiCall(url, { method: 'GET' });
}

/**
 * POST request helper
 */
async function apiPost(url, data) {
    return apiCall(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
}

/**
 * PUT request helper
 */
async function apiPut(url, data) {
    return apiCall(url, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
}

/**
 * DELETE request helper
 */
async function apiDelete(url) {
    return apiCall(url, { method: 'DELETE' });
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { authFetch, apiCall, apiGet, apiPost, apiPut, apiDelete };
}
