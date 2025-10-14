// Admin Dashboard JavaScript

// Sidebar state management
let sidebarOpen = false;

// Toggle sidebar on mobile
function toggleSidebar() {
    const sidebar = document.getElementById('adminSidebar');
    const overlay = document.getElementById('sidebarOverlay');
    
    if (sidebar && overlay) {
        sidebar.classList.toggle('show');
        overlay.classList.toggle('show');
        sidebarOpen = !sidebarOpen;
    }
}

// Toggle sidebar collapse on desktop
function toggleSidebarCollapse() {
    document.body.classList.toggle('sidebar-collapsed');
    const isCollapsed = document.body.classList.contains('sidebar-collapsed');
    localStorage.setItem('sidebarCollapsed', isCollapsed);
    
    // Re-initialize icons after toggle
    setTimeout(() => {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }, 350);
}

// Restore sidebar state from localStorage
function restoreSidebarState() {
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (isCollapsed) {
        document.body.classList.add('sidebar-collapsed');
    }
}

// Initialize dashboard
document.addEventListener('DOMContentLoaded', function() {
    // Restore sidebar state
    restoreSidebarState();
    
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Auto-close sidebar on mobile when clicking a link
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    sidebarLinks.forEach(link => {
        link.addEventListener('click', function() {
            if (window.innerWidth < 992) {
                toggleSidebar();
            }
        });
    });
    
    // Close sidebar when resizing from mobile to desktop
    let resizeTimer;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function() {
            if (window.innerWidth >= 992) {
                const sidebar = document.getElementById('adminSidebar');
                const overlay = document.getElementById('sidebarOverlay');
                if (sidebar && overlay) {
                    sidebar.classList.remove('show');
                    overlay.classList.remove('show');
                    sidebarOpen = false;
                }
            }
        }, 250);
    });
});

// Update active nav link based on current page
function updateActiveNavLink() {
    const currentPath = window.location.pathname;
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    
    sidebarLinks.forEach(link => {
        const linkPath = new URL(link.href).pathname;
        if (currentPath === linkPath || currentPath.startsWith(linkPath + '/')) {
            link.classList.add('active');
        } else {
            link.classList.remove('active');
        }
    });
}

// Call on page load
document.addEventListener('DOMContentLoaded', updateActiveNavLink);

// Chart helpers (for future dashboard charts)
function createLineChart(canvasId, data, labels, label) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    // TODO: Implement chart
}

function createBarChart(canvasId, data, labels, label) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    // TODO: Implement chart
}

function createPieChart(canvasId, data, labels) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    // TODO: Implement chart
}

// Format number with thousand separators
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// Format percentage
function formatPercent(num, decimals = 1) {
    return num.toFixed(decimals) + '%';
}

// Calculate percentage change
function calculatePercentChange(current, previous) {
    if (previous === 0) return 0;
    return ((current - previous) / previous) * 100;
}

// Export data helpers
function exportToCSV(data, filename) {
    const csv = convertToCSV(data);
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function convertToCSV(data) {
    if (!data || !data.length) return '';
    
    const headers = Object.keys(data[0]);
    const csvHeaders = headers.join(',');
    
    const csvRows = data.map(row => {
        return headers.map(header => {
            const value = row[header];
            return typeof value === 'string' && value.includes(',') 
                ? `"${value}"` 
                : value;
        }).join(',');
    });
    
    return [csvHeaders, ...csvRows].join('\n');
}

// Print current page
function printPage() {
    window.print();
}

// Refresh page data
function refreshData() {
    location.reload();
}

// Show loading state
function showLoading(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
                <p class="mt-3 text-muted">Đang tải dữ liệu...</p>
            </div>
        `;
    }
}

// Hide loading state
function hideLoading(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = '';
    }
}

// Debounce function for search
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

// Search handler
const handleSearch = debounce(function(query) {
    // TODO: Implement search logic
}, 300);

// ===== API HELPERS WITH AUTHENTICATION (Cookie-based) =====

/**
 * Make authenticated API request (now using httpOnly cookies)
 * @param {string} url - API endpoint URL
 * @param {object} options - Fetch options
 * @returns {Promise} - Response data
 */
async function apiRequest(url, options = {}) {
    try {
        // Set default headers
        const headers = options.headers || {};
        
        if (!headers['Content-Type'] && (options.method === 'POST' || options.method === 'PUT')) {
            headers['Content-Type'] = 'application/json';
        }
        
        const response = await fetch(url, {
            ...options,
            headers,
            credentials: 'include' // Include cookies in request
        });
        
        // Handle unauthorized (token expired or invalid)
        if (response.status === 401) {
            // Try to refresh token
            const refreshed = await refreshAccessToken();
            if (refreshed) {
                // Retry the original request with new token
                return apiRequest(url, options);
            }
            
            // Redirect to login
            sessionStorage.setItem('redirectAfterLogin', window.location.pathname);
            sessionStorage.setItem('loginRequired', JSON.stringify({
                title: 'Phiên đăng nhập hết hạn',
                message: 'Vui lòng đăng nhập lại để tiếp tục'
            }));
            window.location.href = '/Auth/Login';
            throw new Error('Unauthorized');
        }
        
        // Handle forbidden (not admin)
        if (response.status === 403) {
            const data = await response.json().catch(() => ({}));
            
            if (data.requiresAdmin) {
                sessionStorage.setItem('accessDenied', JSON.stringify({
                    title: 'Truy cập bị từ chối',
                    message: 'Bạn không có quyền truy cập trang này'
                }));
                window.location.href = '/';
                throw new Error('Forbidden');
            }
        }
        
        // Handle other errors
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    } catch (error) {
        console.error('API request error:', error);
        throw error;
    }
}

/**
 * Refresh access token (now using httpOnly cookies)
 */
async function refreshAccessToken() {
    try {
        const apiUrl = window.location.hostname === 'localhost' 
            ? 'https://localhost:5001/api/auth/refresh'
            : '/api/auth/refresh';
        
        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include' // Include cookies for refresh token
        });
        
        return response.ok;
    } catch (error) {
        console.error('Error refreshing token:', error);
        return false;
    }
}

/**
 * API methods for common operations
 */
const api = {
    get: (url) => apiRequest(url, { method: 'GET' }),
    post: (url, data) => apiRequest(url, { 
        method: 'POST', 
        body: JSON.stringify(data) 
    }),
    put: (url, data) => apiRequest(url, { 
        method: 'PUT', 
        body: JSON.stringify(data) 
    }),
    delete: (url) => apiRequest(url, { method: 'DELETE' })
};

// Make api object globally available
window.api = api;

