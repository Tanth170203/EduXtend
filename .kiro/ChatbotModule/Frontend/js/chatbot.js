// AI Chatbot Client
class ChatbotClient {
    constructor() {
        this.apiBaseUrl = 'https://localhost:5001/api/chatbot';
        this.sessionId = null;
        this.isOpen = false;
        this.isTyping = false;
        
        this.init();
    }

    init() {
        // Create chatbot UI
        this.createChatbotUI();
        
        // Event listeners
        document.getElementById('chatbot-button').addEventListener('click', () => this.toggle());
        document.getElementById('chatbot-close').addEventListener('click', () => this.close());
        document.getElementById('chat-input').addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
        document.getElementById('send-button').addEventListener('click', () => this.sendMessage());
        
        // Auto-resize textarea
        const textarea = document.getElementById('chat-input');
        textarea.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 100) + 'px';
        });
    }

    createChatbotUI() {
        const html = `
            <!-- Chatbot Button -->
            <button id="chatbot-button" class="chatbot-button" title="Chat với AI Assistant">
                <i data-lucide="message-circle"></i>
            </button>

            <!-- Chatbot Window -->
            <div id="chatbot-window" class="chatbot-window">
                <!-- Header -->
                <div class="chatbot-header">
                    <div class="chatbot-header-left">
                        <div class="chatbot-avatar">
                            <i data-lucide="bot"></i>
                        </div>
                        <div class="chatbot-title">
                            <h3>AI Assistant</h3>
                            <p>Hỗ trợ tìm CLB & Hoạt động</p>
                        </div>
                    </div>
                    <button id="chatbot-close" class="chatbot-close">
                        <i data-lucide="x"></i>
                    </button>
                </div>

                <!-- Messages -->
                <div id="chatbot-messages" class="chatbot-messages">
                    <div class="welcome-message">
                        <i data-lucide="sparkles"></i>
                        <h4>Xin chào! 👋</h4>
                        <p>Tôi là AI Assistant của EduXtend. Tôi có thể giúp bạn:</p>
                        <div class="suggestion-chips">
                            <div class="suggestion-chip" onclick="chatbot.sendSuggestion('Tìm câu lạc bộ về công nghệ')">
                                🔍 Tìm CLB phù hợp
                            </div>
                            <div class="suggestion-chip" onclick="chatbot.sendSuggestion('Có hoạt động nào sắp diễn ra?')">
                                📅 Xem hoạt động
                            </div>
                            <div class="suggestion-chip" onclick="chatbot.sendSuggestion('Giới thiệu về EduXtend')">
                                💡 Tìm hiểu thêm
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Input -->
                <div class="chatbot-input">
                    <div class="input-wrapper">
                        <textarea 
                            id="chat-input" 
                            placeholder="Nhập tin nhắn của bạn..." 
                            rows="1"
                            maxlength="1000"
                        ></textarea>
                        <button id="send-button" class="send-button">
                            <i data-lucide="send"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', html);
        
        // Initialize Lucide icons for chatbot
        setTimeout(() => {
            if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
                try {
                    // Call with empty config to avoid icons property error
                    lucide.createIcons({});
                } catch (e) {
                    console.warn('Lucide icons init failed:', e);
                }
            }
        }, 100);
    }

    toggle() {
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
    }

    open() {
        const window = document.getElementById('chatbot-window');
        const button = document.getElementById('chatbot-button');
        
        window.classList.add('open');
        button.classList.add('active');
        this.isOpen = true;
        
        // Focus input
        setTimeout(() => {
            document.getElementById('chat-input').focus();
        }, 300);
    }

    close() {
        const window = document.getElementById('chatbot-window');
        const button = document.getElementById('chatbot-button');
        
        window.classList.remove('open');
        button.classList.remove('active');
        this.isOpen = false;
    }

    async checkAuth() {
        // Check if user is logged in by calling /api/auth/me
        try {
            const baseUrl = 'https://localhost:5001';
            const res = await fetch(`${baseUrl}/api/auth/me`, {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Accept': 'application/json'
                }
            });
            return res.ok;
        } catch (err) {
            console.error('Auth check failed:', err);
            return false;
        }
    }

    async sendMessage() {
        const input = document.getElementById('chat-input');
        const message = input.value.trim();
        
        if (!message || this.isTyping) return;
        
        // Check authentication
        const isAuthenticated = await this.checkAuth();
        console.log('Auth check:', isAuthenticated ? 'Authenticated' : 'Not authenticated');
        
        // Debug: Log current user info
        const currentUser = await fetch('https://localhost:5001/api/auth/me', { credentials: 'include' })
            .then(r => r.json())
            .catch(() => null);
        console.log('Current user from token:', currentUser);
        
        if (!isAuthenticated) {
            this.showError('Vui lòng đăng nhập để sử dụng chatbot. <a href="/Auth/Login" style="color: #004080; text-decoration: underline;">Đăng nhập ngay</a>');
            return;
        }

        // Clear input
        input.value = '';
        input.style.height = 'auto';

        // Add user message to UI
        this.addMessage('user', message);

        // Show typing indicator
        this.showTyping();

        try {
            // Send to API with credentials (HTTP-only cookie)
            console.log('Sending message to:', `${this.apiBaseUrl}/message`);
            const response = await fetch(`${this.apiBaseUrl}/message`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({
                    message: message,
                    sessionId: this.sessionId
                })
            });

            console.log('Response status:', response.status);
            this.hideTyping();

            if (response.status === 429) {
                const data = await response.json();
                this.showError(data.message || 'Bạn đã gửi quá nhiều tin nhắn. Vui lòng đợi một chút.');
                return;
            }

            if (response.status === 401) {
                this.showError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
                return;
            }

            if (!response.ok) {
                throw new Error('Failed to send message');
            }

            const data = await response.json();
            
            // Update session ID
            this.sessionId = data.sessionId;

            // Add assistant response
            this.addMessage('assistant', data.response, data.recommendations);

        } catch (error) {
            console.error('Chatbot error:', error);
            this.hideTyping();
            this.showError('Đã xảy ra lỗi. Vui lòng thử lại sau.');
        }
    }

    sendSuggestion(message) {
        const input = document.getElementById('chat-input');
        input.value = message;
        this.sendMessage();
    }

    addMessage(role, content, recommendations = null) {
        const messagesContainer = document.getElementById('chatbot-messages');
        
        // Remove welcome message if exists
        const welcomeMsg = messagesContainer.querySelector('.welcome-message');
        if (welcomeMsg) {
            welcomeMsg.remove();
        }

        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message ${role}`;
        
        const now = new Date();
        const timeStr = now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

        let html = `
            <div class="message-content">
                <div>${this.formatMessage(content)}</div>
                <div class="message-time">${timeStr}</div>
        `;

        // Add recommendations if present
        if (recommendations && recommendations.length > 0) {
            html += '<div class="recommendations">';
            recommendations.forEach(rec => {
                const icon = rec.type === 'Club' ? '👥' : '📅';
                const score = Math.round(rec.confidenceScore * 100);
                html += `
                    <div class="recommendation-card" onclick="chatbot.openRecommendation('${rec.type}', ${rec.id})">
                        <div class="recommendation-header">
                            <span>${icon}</span>
                            <span class="recommendation-type">${rec.type}</span>
                        </div>
                        <div class="recommendation-name">${rec.name}</div>
                        <div class="recommendation-reason">💡 ${rec.reason}</div>
                        <div class="recommendation-score">✨ Độ phù hợp: ${score}%</div>
                    </div>
                `;
            });
            html += '</div>';
        }

        html += '</div>';
        messageDiv.innerHTML = html;
        
        messagesContainer.appendChild(messageDiv);
        
        // Scroll to bottom
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        // Re-initialize Lucide icons
        setTimeout(() => {
            if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
                try {
                    lucide.createIcons({});
                } catch (e) {
                    console.warn('Lucide icons init failed:', e);
                }
            }
        }, 50);
    }

    formatMessage(text) {
        // Convert markdown-style formatting to HTML
        text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        text = text.replace(/\n/g, '<br>');
        return text;
    }

    showTyping() {
        this.isTyping = true;
        const messagesContainer = document.getElementById('chatbot-messages');
        
        const typingDiv = document.createElement('div');
        typingDiv.className = 'chat-message assistant';
        typingDiv.id = 'typing-indicator-wrapper';
        typingDiv.innerHTML = `
            <div class="typing-indicator active">
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
            </div>
        `;
        
        messagesContainer.appendChild(typingDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    hideTyping() {
        this.isTyping = false;
        const typingIndicator = document.getElementById('typing-indicator-wrapper');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }

    showError(message) {
        const messagesContainer = document.getElementById('chatbot-messages');
        
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error-message';
        errorDiv.innerHTML = `
            <i data-lucide="alert-circle"></i>
            <span>${message}</span>
        `;
        
        messagesContainer.appendChild(errorDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        // Re-initialize Lucide icons
        setTimeout(() => {
            if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
                try {
                    lucide.createIcons({});
                } catch (e) {
                    console.warn('Lucide icons init failed:', e);
                }
            }
        }, 50);

        // Remove error after 5 seconds
        setTimeout(() => {
            errorDiv.remove();
        }, 5000);
    }

    openRecommendation(type, id) {
        if (type === 'Club') {
            window.location.href = `/Clubs/Details?id=${id}`;
        } else if (type === 'Activity') {
            window.location.href = `/Activities/Details/${id}`;
        }
    }
}

// Initialize chatbot when DOM is ready
let chatbot;

function initChatbot() {
    // Always initialize chatbot (even for non-logged-in users)
    console.log('Initializing chatbot...');
    chatbot = new ChatbotClient();
}

// Try multiple initialization methods
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initChatbot);
} else {
    // DOM already loaded
    initChatbot();
}

// Also try after window load
window.addEventListener('load', function() {
    if (!chatbot) {
        initChatbot();
    }
});
