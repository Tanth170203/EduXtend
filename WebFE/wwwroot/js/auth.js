// ==========================
// EduXtend Auth Utility
// ==========================

const API_BASE_URL = "https://localhost:5001";

// --------------------------
// Lấy thông tin user (tự refresh nếu token hết hạn)
// --------------------------
async function getUser() {
    try {
        const res = await fetch(`${API_BASE_URL}/api/auth/me`, {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (res.status === 401) {
            const refreshRes = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (refreshRes.ok) {
                await new Promise(resolve => setTimeout(resolve, 100));

                const retry = await fetch(`${API_BASE_URL}/api/auth/me`, {
                    method: 'GET',
                    credentials: 'include',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                return retry.ok ? await retry.json() : null;
            }
            return null;
        }

        return res.ok ? await res.json() : null;

    } catch (err) {
        console.error("[Auth] Error fetching user:", err);
        return null;
    }
}

// --------------------------
// Cập nhật giao diện người dùng FE
// --------------------------
async function updateAuthUI() {
    const user = await getUser();
    const loginBtn = document.getElementById('loginBtn');
    const userInfo = document.getElementById('userInfo');

    console.log('[Auth Debug] User data:', user);
    console.log('[Auth Debug] Avatar URL:', user?.avatar);

    if (user && user.id && user.roles && user.roles.length > 0) {
        loginBtn.style.display = 'none';
        userInfo.style.display = 'block';

        const displayName = user.name && user.name.trim() !== '' ? user.name : user.email;
        document.getElementById('userName').textContent = displayName;
        
        const avatarUrl = user.avatar || '/images/default-avatar.png';
        console.log('[Auth Debug] Setting avatar to:', avatarUrl);
        const avatarImg = document.getElementById('userAvatar');
        avatarImg.src = avatarUrl;
        avatarImg.onerror = function() {
            console.error('[Auth Debug] Failed to load avatar from:', avatarUrl);
            this.src = '/images/default-avatar.png';
        };
        avatarImg.onload = function() {
            console.log('[Auth Debug] Avatar loaded successfully!');
        };
    } else {
        loginBtn.style.display = 'inline-block';
        userInfo.style.display = 'none';
    }
}

// --------------------------
// Cập nhật giao diện Admin Dashboard
// --------------------------
async function updateAdminAuthUI() {
    const user = await getUser();

    if (user && user.id && user.roles && user.roles.length > 0) {
        const nameEl = document.getElementById('adminUserName');
        const avatarEl = document.getElementById('adminUserAvatar');

        // Use name, fallback to email if name is null/empty
        const displayName = user.name && user.name.trim() !== '' ? user.name : user.email;
        
        if (nameEl) nameEl.textContent = displayName;
        if (avatarEl) {
            avatarEl.src = user.avatar || '/images/default-avatar.png';
        }
    }
}

// --------------------------
// Kiểm tra quyền Admin (tự refresh nếu token hết hạn)
// --------------------------
async function requireAdmin() {
    const user = await getUser();

    if (!user || !user.roles || !user.roles.includes("Admin")) {
        sessionStorage.setItem("adminRequired", JSON.stringify({
            title: "Access Denied",
            message: "You need Administrator privileges to view this page."
        }));
        window.location.href = "/";
        return false;
    }

    return true;
}

// --------------------------
// Kiểm tra xác thực
// --------------------------
async function isAuthenticated() {
    const user = await getUser();
    return user && user.id && user.roles && user.roles.length > 0;
}

// --------------------------
// Lấy vai trò người dùng
// --------------------------
async function getUserRole() {
    const user = await getUser();
    if (user && user.roles && user.roles.length > 0) {
        return user.roles[0]; // Return first role
    }
    return null;
}

// --------------------------
// Kiểm tra vai trò Admin
// --------------------------
async function isAdmin() {
    const user = await getUser();
    return user && user.roles && user.roles.includes("Admin");
}

// --------------------------
// Đăng xuất
// --------------------------
async function logout() {
    try {
        // Clear chat history before logout
        if (typeof clearChatHistory === 'function') {
            clearChatHistory();
        }

        await fetch(`${API_BASE_URL}/api/auth/logout`, {
            method: 'POST',
            credentials: 'include'
        });

        sessionStorage.setItem('logoutSuccess', JSON.stringify({
            title: 'Log out successfully',
            message: 'See you again!'
        }));

        window.location.href = '/';
    } catch (err) {
        console.error("[Auth] Logout failed:", err);
    }
}

// --------------------------
// Auto run on page load
// --------------------------
document.addEventListener("DOMContentLoaded", () => {
    if (document.getElementById('loginBtn') || document.getElementById('userInfo')) {
        updateAuthUI();
    }
});
