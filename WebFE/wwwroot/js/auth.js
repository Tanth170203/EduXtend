// ==========================
// EduXtend Auth Utility (v2.2)
// ==========================

const API_BASE_URL = "https://localhost:5001";

// --------------------------
// Lấy thông tin user (tự refresh nếu token hết hạn)
// --------------------------
// Enhanced getUser function with better error handling
async function getUser() {
    try {
        const res = await fetch(`${API_BASE_URL}/api/auth/me`, {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Accept': 'application/json'
            }
        });

        // ✅ Handle 401 - try refresh
        if (res.status === 401) {
            console.warn("[Auth] Access token expired → attempting refresh...");

            const refreshRes = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (refreshRes.ok) {
                console.info("[Auth] ✅ Token refreshed successfully.");

                // Wait for browser to save the new cookies
                await new Promise(resolve => setTimeout(resolve, 100));

                // Retry after refresh
                const retry = await fetch(`${API_BASE_URL}/api/auth/me`, {
                    method: 'GET',
                    credentials: 'include',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                if (retry.ok) {
                    return await retry.json();
                } else {
                    console.error(`[Auth] ❌ Retry failed with status: ${retry.status}`);
                    const errorText = await retry.text();
                    console.error(`[Auth] Error details: ${errorText}`);
                    return null;
                }
            } else {
                console.error(`[Auth] ❌ Refresh failed with status: ${refreshRes.status}`);
                const errorText = await refreshRes.text();
                console.error(`[Auth] Error details: ${errorText}`);
                return null;
            }
        }

        if (!res.ok) {
            console.error(`[Auth] ❌ Request failed with status: ${res.status}`);
            const errorText = await res.text();
            console.error(`[Auth] Error details: ${errorText}`);
            return null;
        }

        return await res.json();

    } catch (err) {
        console.error("[Auth] ⚠️ Error fetching user:", err);
        console.error("[Auth] Error stack:", err.stack);
        return null;
    }
}

// Add this function to manually check cookies (for debugging)
function debugCheckCookies() {
    const cookies = document.cookie.split(';').reduce((acc, cookie) => {
        const [key, value] = cookie.trim().split('=');
        acc[key] = value;
        return acc;
    }, {});

    console.log("[Auth Debug] All cookies:", cookies);

    if (cookies.AccessToken) {
        const dotCount = cookies.AccessToken.split('.').length - 1;
        console.log(`[Auth Debug] AccessToken dots: ${dotCount}`);
        console.log(`[Auth Debug] AccessToken length: ${cookies.AccessToken.length}`);
    } else {
        console.log("[Auth Debug] No AccessToken cookie found");
    }
}

// Call this on page load for debugging
document.addEventListener("DOMContentLoaded", () => {
    // Debug cookies
    debugCheckCookies();

    if (document.getElementById('loginBtn') || document.getElementById('userInfo')) {
        updateAuthUI();
    }
});

// --------------------------
// Cập nhật giao diện người dùng FE
// --------------------------
async function updateAuthUI() {
    const user = await getUser();
    const loginBtn = document.getElementById('loginBtn');
    const userInfo = document.getElementById('userInfo');

    if (user && user.name) {
        // ✅ Hiển thị thông tin người dùng
        loginBtn.style.display = 'none';
        userInfo.style.display = 'block';

        document.getElementById('userName').textContent = user.name;

        if (user.avatar) {
            document.getElementById('userAvatar').src = user.avatar;
        } else {
            // Avatar fallback
            document.getElementById('userAvatar').src = '/images/default-avatar.png';
        }

        console.log(`[Auth] Logged in as ${user.name} (${user.roles.join(", ")})`);
    } else {
        // ❌ Chưa đăng nhập
        loginBtn.style.display = 'inline-block';
        userInfo.style.display = 'none';
        console.log("[Auth] Not logged in.");
    }
}

// --------------------------
// Cập nhật giao diện Admin Dashboard
// --------------------------
async function updateAdminAuthUI() {
    const user = await getUser();

    if (user) {
        const nameEl = document.getElementById('adminUserName');
        const avatarEl = document.getElementById('adminUserAvatar');

        if (nameEl) nameEl.textContent = user.name;
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
        console.warn("[Auth] ❌ Access denied - Admin only.");
        sessionStorage.setItem("adminRequired", JSON.stringify({
            title: "Truy cập bị từ chối",
            message: "Bạn cần quyền Quản trị viên để xem trang này."
        }));
        window.location.href = "/";
        return false;
    }

    console.log("[Auth] ✅ Admin verified.");
    return true;
}

// --------------------------
// Đăng xuất
// --------------------------
async function logout() {
    try {
        await fetch(`${API_BASE_URL}/api/auth/logout`, {
            method: 'POST',
            credentials: 'include'
        });

        sessionStorage.setItem('logoutSuccess', JSON.stringify({
            title: 'Đăng xuất thành công',
            message: 'Hẹn gặp lại bạn!'
        }));

        console.info("[Auth] ✅ Logged out.");
        window.location.href = '/';
    } catch (err) {
        console.error("[Auth] ⚠️ Logout failed:", err);
    }
}

// --------------------------
// Auto run on page load (nếu có UI)
// --------------------------
document.addEventListener("DOMContentLoaded", () => {
    if (document.getElementById('loginBtn') || document.getElementById('userInfo')) {
        updateAuthUI();
    }
});
