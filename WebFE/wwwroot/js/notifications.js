/**
 * EduXtend Notification System
 * Modern toast notifications and modal alerts
 */

/**
 * EduXtend Notification System
 * Modern toast notifications and modal alerts
 * Extended for persistent notifications and real-time updates
 */

class NotificationManager {
    constructor() {
        this.toastContainer = null;
        this.modalContainer = null;
        this.notificationCenter = null;
        this.notifications = [];
        this.unreadCount = 0;
        this.init();
    }

    init() {
        this.createToastContainer();
        this.createModalContainer();
        this.createNotificationCenter();
        this.addStyles();
        this.loadStoredNotifications();
        this.setupEventListeners();
    }

    createToastContainer() {
        this.toastContainer = document.createElement('div');
        this.toastContainer.id = 'toast-container';
        this.toastContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: 12px;
            max-width: 400px;
            pointer-events: none;
        `;
        document.body.appendChild(this.toastContainer);
    }

    createModalContainer() {
        this.modalContainer = document.createElement('div');
        this.modalContainer.id = 'modal-container';
        this.modalContainer.style.cssText = `
            position: fixed !important;
            top: 0 !important;
            left: 0 !important;
            right: 0 !important;
            bottom: 0 !important;
            width: 100vw !important;
            height: 100vh !important;
            margin: 0 !important;
            padding: 0 !important;
            z-index: 10000 !important;
            display: none;
            align-items: center !important;
            justify-content: center !important;
            background: rgba(0, 0, 0, 0.5) !important;
            backdrop-filter: blur(4px);
        `;
        document.body.appendChild(this.modalContainer);
    }

    createNotificationCenter() {
        this.notificationCenter = document.createElement('div');
        this.notificationCenter.id = 'notification-center';
        this.notificationCenter.style.cssText = `
            position: fixed;
            top: 80px;
            right: 20px;
            width: 400px;
            max-height: 600px;
            background: white;
            border-radius: 16px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            z-index: 9998;
            display: none;
            flex-direction: column;
            overflow: hidden;
            transform: translateX(100%);
            opacity: 0;
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        `;
        document.body.appendChild(this.notificationCenter);
    }

    loadStoredNotifications() {
        // Load notifications from API
        this.loadNotificationsFromAPI();
    }

    async loadNotificationsFromAPI() {
        try {
            const response = await fetch('https://localhost:5001/api/notifications', {
                credentials: 'include'
            });

            if (response.ok) {
                const result = await response.json();
                this.notifications = result.data || [];
                this.updateUnreadCount();
                this.renderNotificationCenter();
            } else if (response.status === 401) {
                // User not authenticated, skip loading
                console.log('User not authenticated, skipping notification load');
            }
        } catch (error) {
            console.error('Error loading notifications:', error);
            // Fallback to localStorage if API fails
            const stored = localStorage.getItem('eduxtend_notifications');
            if (stored) {
                this.notifications = JSON.parse(stored);
                this.updateUnreadCount();
            }
        }
    }

    saveNotifications() {
        localStorage.setItem('eduxtend_notifications', JSON.stringify(this.notifications));
    }

    setupEventListeners() {
        // Listen for real-time notifications (SignalR)
        if (window.signalRConnection) {
            window.signalRConnection.on("ReceiveNotification", (notification) => {
                this.addPersistentNotification(notification);
            });
        }

        // Listen for visibility change to update unread count
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden) {
                this.loadNotificationsFromAPI();
            }
        });

        // Auto-refresh notifications every 30 seconds
        setInterval(() => {
            this.loadNotificationsFromAPI();
        }, 30000);

        // Close notification center when clicking outside
        document.addEventListener('click', (e) => {
            const notificationCenter = document.getElementById('notification-center');
            const notificationBell = document.querySelector('.notification-bell');
            
            if (notificationCenter && notificationBell && 
                !notificationCenter.contains(e.target) && 
                !notificationBell.contains(e.target) &&
                notificationCenter.classList.contains('show')) {
                this.hideNotificationCenter();
            }
        });
    }

    addStyles() {
        const style = document.createElement('style');
        style.textContent = `
            .toast {
                background: white;
                border-radius: 12px;
                box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
                border-left: 4px solid;
                padding: 16px 20px;
                display: flex;
                align-items: flex-start;
                gap: 12px;
                min-width: 300px;
                max-width: 400px;
                pointer-events: auto;
                transform: translateX(100%);
                opacity: 0;
                transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                position: relative;
                overflow: hidden;
            }

            .toast.show {
                transform: translateX(0);
                opacity: 1;
            }

            .toast.hide {
                transform: translateX(100%);
                opacity: 0;
            }

            .toast.success {
                border-left-color: #10b981;
            }

            .toast.error {
                border-left-color: #ef4444;
            }

            .toast.warning {
                border-left-color: #f59e0b;
            }

            .toast.info {
                border-left-color: #3b82f6;
            }

            .toast-icon {
                flex-shrink: 0;
                width: 24px;
                height: 24px;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                color: white;
                font-size: 14px;
                font-weight: 600;
            }

            .toast.success .toast-icon {
                background: #10b981;
            }

            .toast.error .toast-icon {
                background: #ef4444;
            }

            .toast.warning .toast-icon {
                background: #f59e0b;
            }

            .toast.info .toast-icon {
                background: #3b82f6;
            }

            .toast-content {
                flex: 1;
                min-width: 0;
            }

            .toast-title {
                font-weight: 600;
                font-size: 14px;
                color: #1f2937;
                margin: 0 0 4px 0;
                line-height: 1.4;
            }

            .toast-message {
                font-size: 13px;
                color: #6b7280;
                margin: 0;
                line-height: 1.4;
            }

            .toast-close {
                position: absolute;
                top: 8px;
                right: 8px;
                background: none;
                border: none;
                color: #9ca3af;
                cursor: pointer;
                padding: 4px;
                border-radius: 4px;
                transition: color 0.2s;
                font-size: 16px;
                line-height: 1;
            }

            .toast-close:hover {
                color: #6b7280;
            }

            .toast-progress {
                position: absolute;
                bottom: 0;
                left: 0;
                height: 3px;
                background: currentColor;
                opacity: 0.3;
                transition: width linear;
            }

            .modal {
                background: white;
                border-radius: 16px;
                box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
                max-width: 500px;
                width: 90%;
                max-height: 90vh;
                overflow: hidden;
                transform: scale(0.95);
                opacity: 0;
                transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            }

            .modal.show {
                transform: scale(1);
                opacity: 1;
            }

            .modal-header {
                padding: 24px 24px 16px;
                border-bottom: 1px solid #e5e7eb;
                display: flex;
                align-items: center;
                gap: 16px;
            }

            .modal-icon {
                width: 48px;
                height: 48px;
                border-radius: 12px;
                display: flex;
                align-items: center;
                justify-content: center;
                color: white;
                font-size: 24px;
                font-weight: 600;
            }

            .modal.success .modal-icon {
                background: #10b981;
            }

            .modal.error .modal-icon {
                background: #ef4444;
            }

            .modal.warning .modal-icon {
                background: #f59e0b;
            }

            .modal.info .modal-icon {
                background: #3b82f6;
            }

            .modal-title {
                font-size: 20px;
                font-weight: 700;
                color: #1f2937;
                margin: 0;
                flex: 1;
            }

            .modal-body {
                padding: 16px 24px 24px;
            }

            .modal-message {
                font-size: 16px;
                color: #6b7280;
                line-height: 1.5;
                margin: 0 0 24px 0;
            }

            .modal-actions {
                display: flex;
                gap: 12px;
                justify-content: flex-end;
            }

            .modal-btn {
                padding: 10px 20px;
                border-radius: 8px;
                font-weight: 600;
                font-size: 14px;
                border: none;
                cursor: pointer;
                transition: all 0.2s;
                min-width: 80px;
            }

            .modal-btn-primary {
                background: #3b82f6;
                color: white;
            }

            .modal-btn-primary:hover {
                background: #2563eb;
            }

            .modal-btn-secondary {
                background: #f3f4f6;
                color: #374151;
            }

            .modal-btn-secondary:hover {
                background: #e5e7eb;
            }

            .modal-btn-danger {
                background: #ef4444;
                color: white;
            }

            .modal-btn-danger:hover {
                background: #dc2626;
            }

            @keyframes slideInRight {
                from {
                    transform: translateX(100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }

            @keyframes slideOutRight {
                from {
                    transform: translateX(0);
                    opacity: 1;
                }
                to {
                    transform: translateX(100%);
                    opacity: 0;
                }
            }

            /* Notification Center Styles */
            .notification-center {
                position: fixed;
                top: 80px;
                right: 20px;
                width: 400px;
                max-height: 600px;
                background: white;
                border-radius: 16px;
                box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
                z-index: 9998;
                display: none;
                flex-direction: column;
                overflow: hidden;
                transform: translateX(100%);
                opacity: 0;
                transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            }

            .notification-center.show {
                transform: translateX(0);
                opacity: 1;
            }

            /* Notification Preview on Hover */
            .notification-preview {
                position: absolute;
                top: 50px;
                right: 0;
                width: 380px;
                background: white;
                border-radius: 12px;
                box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.2);
                z-index: 9999;
                display: none;
                flex-direction: column;
                overflow: hidden;
                opacity: 0;
                transition: opacity 0.2s;
            }

            .notification-preview.show {
                opacity: 1;
            }

            .notification-preview-header {
                padding: 12px 16px;
                border-bottom: 1px solid #e5e7eb;
                background: #f8fafc;
                font-size: 14px;
                font-weight: 600;
                color: #1f2937;
            }

            .notification-preview-list {
                max-height: 350px;
                overflow-y: auto;
            }

            .notification-preview-footer {
                padding: 10px 16px;
                border-top: 1px solid #e5e7eb;
                text-align: center;
            }

            .notification-preview-footer a {
                color: #3b82f6;
                text-decoration: none;
                font-size: 13px;
                font-weight: 600;
            }

            .notification-preview-footer a:hover {
                text-decoration: underline;
            }

            .notification-header {
                padding: 20px 24px 16px;
                border-bottom: 1px solid #e5e7eb;
                display: flex;
                align-items: center;
                justify-content: space-between;
                background: #f8fafc;
            }

            .notification-title {
                font-size: 18px;
                font-weight: 700;
                color: #1f2937;
                margin: 0;
            }

            .notification-actions {
                display: flex;
                gap: 8px;
            }

            .notification-btn {
                padding: 6px 12px;
                border-radius: 6px;
                font-size: 12px;
                font-weight: 600;
                border: none;
                cursor: pointer;
                transition: all 0.2s;
            }

            .notification-btn-primary {
                background: #3b82f6;
                color: white;
            }

            .notification-btn-primary:hover {
                background: #2563eb;
            }

            .notification-btn-secondary {
                background: #f3f4f6;
                color: #374151;
            }

            .notification-btn-secondary:hover {
                background: #e5e7eb;
            }

            .notification-list {
                flex: 1;
                overflow-y: auto;
                max-height: 490px; /* ~7 notifications */
            }

            .notification-list::-webkit-scrollbar {
                width: 6px;
            }

            .notification-list::-webkit-scrollbar-track {
                background: #f3f4f6;
                border-radius: 3px;
            }

            .notification-list::-webkit-scrollbar-thumb {
                background: #d1d5db;
                border-radius: 3px;
            }

            .notification-list::-webkit-scrollbar-thumb:hover {
                background: #9ca3af;
            }

            .notification-item {
                padding: 16px 24px;
                border-bottom: 1px solid #f3f4f6;
                cursor: pointer;
                transition: background-color 0.2s;
                position: relative;
            }

            .notification-item:hover {
                background: #f8fafc;
            }

            .notification-item.unread {
                background: #eff6ff;
                border-left: 4px solid #3b82f6;
            }

            .notification-item.unread::before {
                content: '';
                position: absolute;
                top: 50%;
                right: 16px;
                width: 8px;
                height: 8px;
                background: #3b82f6;
                border-radius: 50%;
                transform: translateY(-50%);
            }

            .notification-item-header {
                display: flex;
                align-items: flex-start;
                gap: 12px;
                margin-bottom: 8px;
            }

            .notification-icon {
                width: 32px;
                height: 32px;
                border-radius: 8px;
                display: flex;
                align-items: center;
                justify-content: center;
                color: white;
                font-size: 14px;
                font-weight: 600;
                flex-shrink: 0;
            }

            .notification-content {
                flex: 1;
                min-width: 0;
            }

            .notification-item-title {
                font-size: 14px;
                font-weight: 600;
                color: #1f2937;
                margin: 0 0 4px 0;
                line-height: 1.4;
            }

            .notification-item-message {
                font-size: 13px;
                color: #6b7280;
                margin: 0 0 8px 0;
                line-height: 1.4;
                display: -webkit-box;
                -webkit-line-clamp: 2;
                -webkit-box-orient: vertical;
                overflow: hidden;
            }

            .notification-meta {
                display: flex;
                align-items: center;
                gap: 8px;
                font-size: 12px;
                color: #9ca3af;
            }

            .notification-time {
                margin: 0;
            }

            .notification-scope {
                background: #e5e7eb;
                color: #374151;
                padding: 2px 6px;
                border-radius: 4px;
                font-size: 11px;
                font-weight: 500;
            }

            .notification-empty {
                padding: 40px 24px;
                text-align: center;
                color: #9ca3af;
            }

            .notification-empty-icon {
                font-size: 48px;
                margin-bottom: 16px;
                opacity: 0.5;
            }

            .notification-empty-text {
                font-size: 16px;
                font-weight: 500;
                margin: 0 0 8px 0;
            }

            .notification-empty-subtext {
                font-size: 14px;
                margin: 0;
            }

            /* Notification Bell */
            .notification-bell {
                position: relative;
                cursor: pointer;
                padding: 8px;
                border-radius: 8px;
                transition: background-color 0.2s;
            }

            .notification-bell:hover {
                background: rgba(255, 255, 255, 0.1);
            }

            .notification-badge {
                position: absolute;
                top: 4px;
                right: 4px;
                background: #ef4444;
                color: white;
                font-size: 11px;
                font-weight: 700;
                padding: 2px 6px;
                border-radius: 10px;
                min-width: 18px;
                height: 18px;
                display: flex;
                align-items: center;
                justify-content: center;
                transform: scale(0);
                transition: transform 0.2s;
            }

            .notification-badge.show {
                transform: scale(1);
            }

            .notification-badge.pulse {
                animation: pulse 2s infinite;
            }

            @keyframes pulse {
                0%, 100% { transform: scale(1); }
                50% { transform: scale(1.1); }
            }
        `;
        document.head.appendChild(style);
    }

    // Persistent Notification Methods
    addPersistentNotification(notification) {
        const notificationData = {
            id: notification.id || Date.now().toString(),
            title: notification.title,
            message: notification.message,
            type: notification.type || 'info',
            scope: notification.scope || 'System',
            targetClubId: notification.targetClubId,
            targetRole: notification.targetRole,
            createdAt: notification.createdAt || new Date().toISOString(),
            isRead: false,
            actionUrl: notification.actionUrl,
            actionText: notification.actionText
        };

        this.notifications.unshift(notificationData);
        this.updateUnreadCount();
        this.saveNotifications();
        this.renderNotificationCenter();

        // Show toast for new notification
        this.showToast(notificationData.type, notificationData.title, notificationData.message, 5000);
    }

    async markAsRead(notificationId) {
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification && !notification.isRead) {
            try {
                const response = await fetch(`https://localhost:5001/api/notifications/${notificationId}/read`, {
                    method: 'PUT',
                    credentials: 'include'
                });

                if (response.ok) {
                    notification.isRead = true;
                    this.updateUnreadCount();
                    this.renderNotificationCenter();
                }
            } catch (error) {
                console.error('Error marking notification as read:', error);
            }
        }
    }

    async markAllAsRead() {
        try {
            const response = await fetch('https://localhost:5001/api/notifications/read-all', {
                method: 'PUT',
                credentials: 'include'
            });

            if (response.ok) {
                this.notifications.forEach(notification => {
                    notification.isRead = true;
                });
                this.updateUnreadCount();
                this.renderNotificationCenter();
            }
        } catch (error) {
            console.error('Error marking all notifications as read:', error);
        }
    }

    async deleteNotification(notificationId) {
        try {
            // Delete notification on server
            const response = await fetch(`https://localhost:5001/api/notification/${notificationId}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (response.ok) {
                // Remove locally
                this.notifications = this.notifications.filter(n => n.id !== notificationId);
                this.updateUnreadCount();
                this.saveNotifications();
                this.renderNotificationCenter();
            } else {
                console.error('Failed to delete notification');
            }
        } catch (error) {
            console.error('Error deleting notification:', error);
        }
    }

    async clearAllNotifications() {
        try {
            // Delete all notifications on server
            const response = await fetch('https://localhost:5001/api/notification/delete-all', {
                method: 'DELETE',
                credentials: 'include'
            });

            if (response.ok) {
                // Clear locally
                this.notifications = [];
                this.updateUnreadCount();
                this.saveNotifications();
                this.renderNotificationCenter();
            } else {
                console.error('Failed to delete all notifications');
            }
        } catch (error) {
            console.error('Error deleting all notifications:', error);
        }
    }

    updateUnreadCount() {
        this.unreadCount = this.notifications.filter(n => !n.isRead).length;
        this.updateNotificationBadge();
    }

    updateNotificationBadge() {
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            if (this.unreadCount > 0) {
                badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount.toString();
                badge.classList.add('show');
                if (this.unreadCount > 0) {
                    badge.classList.add('pulse');
                }
            } else {
                badge.classList.remove('show', 'pulse');
            }
        }
    }

    renderNotificationCenter() {
        if (!this.notificationCenter) return;

        const unreadCount = this.notifications.filter(n => !n.isRead).length;
        
        this.notificationCenter.innerHTML = `
            <div class="notification-header">
                <h3 class="notification-title">Notifications</h3>
            </div>
            <div class="notification-list">
                ${this.notifications.length === 0 ? this.renderEmptyState() : this.renderNotificationList()}
            </div>
        `;
    }

    renderEmptyState() {
        return `
            <div class="notification-empty">
                <div class="notification-empty-icon">🔔</div>
                <p class="notification-empty-text">No notifications</p>
                <p class="notification-empty-subtext">New notifications will appear here</p>
            </div>
        `;
    }

    renderNotificationList() {
        // Show maximum 7 notifications, rest can be scrolled
        return this.notifications.map(notification => this.renderNotificationItem(notification)).join('');
    }

    getTypeColor(type) {
        const colors = {
            success: '#10b981',
            error: '#ef4444',
            warning: '#f59e0b',
            info: '#3b82f6'
        };
        return colors[type] || '#3b82f6';
    }

    getTimeAgo(dateString) {
        const now = new Date();
        const date = new Date(dateString);
        const diffInSeconds = Math.floor((now - date) / 1000);

        if (diffInSeconds < 60) return 'Just now';
        if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} minutes ago`;
        if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} hours ago`;
        if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)} days ago`;
        return date.toLocaleDateString('en-US');
    }

    // Notification Center UI Methods
    showNotificationCenter() {
        console.log('Show notification center', this.notifications.length, 'notifications');
        this.renderNotificationCenter();
        this.notificationCenter.style.display = 'flex';
        setTimeout(() => {
            this.notificationCenter.style.transform = 'translateX(0)';
            this.notificationCenter.style.opacity = '1';
            console.log('Notification center should be visible now');
            
            // Auto mark all as read after 1 second
            setTimeout(() => {
                this.markAllAsRead();
            }, 1000);
        }, 10);
    }

    hideNotificationCenter() {
        this.notificationCenter.style.transform = 'translateX(100%)';
        this.notificationCenter.style.opacity = '0';
        setTimeout(() => {
            this.notificationCenter.style.display = 'none';
        }, 300);
    }

    toggleNotificationCenter() {
        console.log('Toggle notification center', this.notificationCenter.style.display);
        if (this.notificationCenter.style.display === 'flex') {
            this.hideNotificationCenter();
        } else {
            this.showNotificationCenter();
        }
    }

    renderNotificationItem(notification) {
        const iconMap = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };

        const timeAgo = this.getTimeAgo(notification.createdAt);
        const scopeText = notification.scope === 'Club' ? 'Club' : 'System';
        
        // Determine the link based on notification title/type
        let targetUrl = '#'; // Default: no redirect, just mark as read
        
        // Fund collection / Payment notifications
        if (notification.title.includes('payment') || notification.title.includes('Payment') ||
            notification.title.includes('fund collection') || notification.title.includes('Fund collection')) {
            // For members: go to Finance section in Member Dashboard
            if (notification.title.includes('New payment request') || 
                notification.title.includes('Payment confirmed') ||
                notification.title.includes('Payment reminder') ||
                notification.title.includes('Payment overdue') ||
                notification.title.includes('Payment successful')) {
                // Extract club ID from notification if available, otherwise use a default
                const clubId = notification.targetClubId || 1; // You may need to store clubId in notification
                targetUrl = `/Clubs/MemberDashboard/${clubId}?section=finance`;
            }
            // For club managers: go to Member Funds page
            else if (notification.title.includes('cash payment') || 
                     notification.title.includes('bank transfer') ||
                     notification.title.includes('VNPAY payment')) {
                targetUrl = '/ClubManager/Financial/MemberFunds';
            }
        }
        // Club news notifications
        else if (notification.title.includes('article') || notification.title.includes('Article')) {
            // For admin: go to pending approval page
            if (notification.title.includes('pending approval')) {
                targetUrl = '/Admin/ClubNews';
            }
            // For club manager: go to their news page
            else if (notification.title.includes('approved') || notification.title.includes('rejected')) {
                targetUrl = '/ClubManager/News';
            }
        }
        // Interview notifications
        else if (notification.title.includes('phỏng vấn') || notification.title.includes('interview')) {
            targetUrl = '/Student/MyApplications';
        }

        return `
            <div class="notification-item ${!notification.isRead ? 'unread' : ''}" 
                 onclick="notificationManager.handleNotificationClick('${notification.id}', '${targetUrl}')">
                <div class="notification-item-header">
                    <div class="notification-icon" style="background: ${this.getTypeColor(notification.type)}">
                        ${iconMap[notification.type] || 'ℹ'}
                    </div>
                    <div class="notification-content">
                        <h4 class="notification-item-title">${notification.title}</h4>
                        <p class="notification-item-message">${notification.message}</p>
                        <div class="notification-meta">
                            <span class="notification-time">${timeAgo}</span>
                            <span class="notification-scope">${scopeText}</span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    handleNotificationClick(notificationId, targetUrl) {
        // Mark as read
        this.markAsRead(notificationId);
        
        // Close notification center
        this.hideNotificationCenter();
        
        // Navigate to target page only if targetUrl is not '#'
        if (targetUrl && targetUrl !== '#') {
            window.location.href = targetUrl;
        }
    }

    // Toast Methods
    showToast(type, title, message, duration = 5000) {
        const toast = this.createToast(type, title, message, duration);
        this.toastContainer.appendChild(toast);

        // Trigger animation
        setTimeout(() => toast.classList.add('show'), 10);

        // Auto remove
        if (duration > 0) {
            setTimeout(() => this.removeToast(toast), duration);
        }

        return toast;
    }

    createToast(type, title, message, duration) {
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        
        const iconMap = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };

        toast.innerHTML = `
            <div class="toast-icon">${iconMap[type] || 'ℹ'}</div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
            <button class="toast-close" onclick="this.parentElement.remove()">×</button>
            ${duration > 0 ? `<div class="toast-progress" style="width: 100%; animation: progress ${duration}ms linear forwards;"></div>` : ''}
        `;

        // Add progress animation
        if (duration > 0) {
            const progressStyle = document.createElement('style');
            progressStyle.textContent = `
                @keyframes progress {
                    from { width: 100%; }
                    to { width: 0%; }
                }
            `;
            document.head.appendChild(progressStyle);
        }

        return toast;
    }

    removeToast(toast) {
        toast.classList.add('hide');
        setTimeout(() => {
            if (toast.parentElement) {
                toast.parentElement.removeChild(toast);
            }
        }, 300);
    }

    // Modal Methods
    showModal(type, title, message, options = {}) {
        const modal = this.createModal(type, title, message, options);
        this.modalContainer.innerHTML = '';
        this.modalContainer.appendChild(modal);
        this.modalContainer.style.display = 'flex';

        // Trigger animation
        setTimeout(() => modal.classList.add('show'), 10);

        return modal;
    }

    createModal(type, title, message, options) {
        const modal = document.createElement('div');
        modal.className = `modal ${type}`;
        
        const iconMap = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };

        const buttons = options.buttons || [
            { text: 'OK', type: 'primary', action: () => this.hideModal() }
        ];

        modal.innerHTML = `
            <div class="modal-header">
                <div class="modal-icon">${iconMap[type] || 'ℹ'}</div>
                <h3 class="modal-title">${title}</h3>
            </div>
            <div class="modal-body">
                <p class="modal-message">${message}</p>
                <div class="modal-actions">
                    ${buttons.map((btn, index) => 
                        `<button class="modal-btn modal-btn-${btn.type}" data-action="${index}">${btn.text}</button>`
                    ).join('')}
                </div>
            </div>
        `;

        // Add event listeners for buttons
        buttons.forEach((btn, index) => {
            const button = modal.querySelector(`[data-action="${index}"]`);
            if (button && btn.action) {
                button.addEventListener('click', () => {
                    btn.action();
                });
            }
        });

        return modal;
    }

    hideModal() {
        const modal = this.modalContainer.querySelector('.modal');
        if (modal) {
            modal.classList.remove('show');
            setTimeout(() => {
                this.modalContainer.style.display = 'none';
            }, 300);
        }
    }

    // Convenience Methods
    success(title, message, duration = 5000) {
        return this.showToast('success', title, message, duration);
    }

    error(title, message, duration = 7000) {
        return this.showToast('error', title, message, duration);
    }

    warning(title, message, duration = 6000) {
        return this.showToast('warning', title, message, duration);
    }

    info(title, message, duration = 5000) {
        return this.showToast('info', title, message, duration);
    }

    // Modal convenience methods
    alert(title, message, type = 'info') {
        return this.showModal(type, title, message);
    }

    confirm(title, message, onConfirm, onCancel) {
        return this.showModal('warning', title, message, {
            buttons: [
                { text: 'Hủy', type: 'secondary', action: () => { this.hideModal(); if (onCancel) onCancel(); } },
                { text: 'Xác nhận', type: 'primary', action: () => { this.hideModal(); if (onConfirm) onConfirm(); } }
            ]
        });
    }
}

// Initialize global instance
const notificationManager = new NotificationManager();

// Global functions for easy access
window.showToast = (type, title, message, duration) => notificationManager.showToast(type, title, message, duration);
window.showModal = (type, title, message, options) => notificationManager.showModal(type, title, message, options);
window.hideModal = () => notificationManager.hideModal();

// Convenience functions
window.success = (title, message, duration) => notificationManager.success(title, message, duration);
window.error = (title, message, duration) => notificationManager.error(title, message, duration);
window.warning = (title, message, duration) => notificationManager.warning(title, message, duration);
window.info = (title, message, duration) => notificationManager.info(title, message, duration);
window.alert = (title, message, type) => notificationManager.alert(title, message, type);
window.confirm = (title, message, onConfirm, onCancel) => notificationManager.confirm(title, message, onConfirm, onCancel);

// Persistent notification functions
window.addNotification = (notification) => notificationManager.addPersistentNotification(notification);
window.showNotificationCenter = () => notificationManager.showNotificationCenter();
window.hideNotificationCenter = () => notificationManager.hideNotificationCenter();
window.toggleNotificationCenter = () => notificationManager.toggleNotificationCenter();
window.markNotificationAsRead = (id) => notificationManager.markAsRead(id);
window.markAllNotificationsAsRead = () => notificationManager.markAllAsRead();
window.clearAllNotifications = () => notificationManager.clearAllNotifications();

// Demo functions for testing
window.testNotifications = () => {
    // Add some sample notifications
    notificationManager.addPersistentNotification({
        title: 'Hoạt động mới',
        message: 'CLB Tin học đã tạo hoạt động "Workshop React" vào ngày 15/1/2025',
        type: 'info',
        scope: 'Club',
        targetClubId: 1
    });
    
    setTimeout(() => {
        notificationManager.addPersistentNotification({
            title: 'Đăng ký thành công',
            message: 'Bạn đã đăng ký tham gia hoạt động "Workshop React" thành công',
            type: 'success',
            scope: 'Club'
        });
    }, 1000);
    
    setTimeout(() => {
        notificationManager.addPersistentNotification({
            title: 'Cảnh báo bảo mật',
            message: 'Tài khoản của bạn đã được đăng nhập từ thiết bị khác',
            type: 'warning',
            scope: 'System'
        });
    }, 2000);
    
    setTimeout(() => {
        notificationManager.addPersistentNotification({
            title: 'Lỗi hệ thống',
            message: 'Không thể tải dữ liệu điểm rèn luyện. Vui lòng thử lại sau',
            type: 'error',
            scope: 'System'
        });
    }, 3000);
};
