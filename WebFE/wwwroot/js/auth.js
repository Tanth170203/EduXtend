// EduXtend Authentication Utilities - Clean & Optimized

const API_BASE_URL = window.location.hostname === 'localhost' 
    ? 'https://localhost:5001/api'     // Backend HTTPS
    : '/api'; // Production

/**
 * Get access token from localStorage
 */
function getAccessToken() {
    return localStorage.getItem('accessToken');
}

/**
 * Get refresh token from localStorage
 */
function getRefreshToken() {
    return localStorage.getItem('refreshToken');
}

/**
 * Get token expiration time from localStorage
 */
function getExpiresAt() {
    const expiresAt = localStorage.getItem('expiresAt');
    return expiresAt ? new Date(expiresAt) : null;
}

/**
 * Check if user is authenticated
 */
function isAuthenticated() {
    const accessToken = getAccessToken();
    const expiresAt = getExpiresAt();
    return accessToken && expiresAt && expiresAt > new Date();
}

/**
 * Get current user info from access token (decode JWT)
 */
function getCurrentUser() {
    const token = getAccessToken();
    if (!token) return null;

    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const payload = JSON.parse(jsonPayload);
        return {
            id: payload.sub,
            email: payload.email,
            name: payload.name,
            avatar: payload.picture,
            roles: payload.role ? (Array.isArray(payload.role) ? payload.role : [payload.role]) : []
        };
    } catch (e) {
        console.error("Error decoding token:", e);
        return null;
    }
}

/**
 * Refresh access token
 */
async function refreshAccessToken() {
    const refreshToken = getRefreshToken();
    if (!refreshToken) {
        console.log('No refresh token available.');
        logout();
        return false;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: refreshToken })
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to refresh token');
        }

        const data = await response.json();
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        localStorage.setItem('expiresAt', data.expiresAt);
        console.log('Token refreshed successfully.');
        return true;
    } catch (error) {
        console.error('Error refreshing token:', error);
        logout();
        return false;
    }
}

/**
 * Logout user
 */
async function logout() {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
        try {
            await fetch(`${API_BASE_URL}/auth/logout`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: refreshToken })
            });
            console.log('Logged out from server.');
        } catch (error) {
            console.error('Error during server logout:', error);
        }
    }
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    console.log('Logged out from client.');
    
    // Store logout message in sessionStorage for login page
    sessionStorage.setItem('logoutSuccess', JSON.stringify({
        title: 'Đã đăng xuất',
        message: 'Bạn đã đăng xuất thành công khỏi hệ thống',
        type: 'info'
    }));
    
    // Show logout notification
    if (typeof info === 'function') {
        info('Đã đăng xuất', 'Bạn đã đăng xuất thành công khỏi hệ thống.', 3000);
    }
    
    updateAuthUI();
    window.location.href = '/Login';
}

/**
 * Update UI based on authentication status
 */
function updateAuthUI() {
    const userInfo = document.getElementById('userInfo');
    const loginBtn = document.getElementById('loginBtn');

    if (isAuthenticated()) {
        const user = getCurrentUser();

        if (user) {
            // Show user info
            if (userInfo) {
                const userName = document.getElementById('userName');
                const userAvatar = document.getElementById('userAvatar');

                if (userName) userName.textContent = user.name || user.email;
                if (userAvatar) {
                    userAvatar.src = user.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name || user.email)}&background=003366&color=fff&size=128`;
                }

                userInfo.style.display = 'block';
            }

            // Hide login button
            if (loginBtn) loginBtn.style.display = 'none';

            // Reinitialize Lucide icons
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        } else {
            // No user data, show login button
            if (loginBtn) loginBtn.style.display = 'inline-block';
            if (userInfo) userInfo.style.display = 'none';
        }
    } else {
        // Not authenticated, show login button
        if (loginBtn) loginBtn.style.display = 'inline-block';

        // Hide user info
        if (userInfo) userInfo.style.display = 'none';
    }
}

/**
 * Make authenticated API request
 */
async function authenticatedFetch(url, options = {}) {
    let accessToken = getAccessToken();
    const expiresAt = getExpiresAt();

    // If token is expired or about to expire, try to refresh
    if (expiresAt && expiresAt < new Date(Date.now() + 60 * 1000)) { // Refresh if less than 1 minute left
        const refreshed = await refreshAccessToken();
        if (refreshed) {
            accessToken = getAccessToken();
        } else {
            throw new Error('Failed to refresh token, please log in again.');
        }
    }

    if (!accessToken) {
        logout();
        throw new Error('No access token available. Please log in.');
    }

    const headers = {
        ...options.headers,
        'Authorization': `Bearer ${accessToken}`
    };

    const response = await fetch(url, { ...options, headers });

    if (response.status === 401) { // Unauthorized, token might be invalid even after refresh
        logout();
        throw new Error('Unauthorized. Please log in again.');
    }

    return response;
}