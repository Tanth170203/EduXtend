// AI Chatbot Assistant - Frontend Logic

// Constants
const CHATBOT_API_BASE_URL = "https://localhost:5001";
const CHATBOT_API_ENDPOINT = `${CHATBOT_API_BASE_URL}/api/chatbot/message`;
const MAX_CHAT_HISTORY = 50;
const API_TIMEOUT = 30000; // 30 seconds
const STORAGE_KEY = 'chatbot_history';

// State
let isProcessing = false;
let chatHistory = [];
let typingIndicatorTimeout = null;

// Initialize chatbot on page load
function initChatbot() {
    console.log('🤖 [CHATBOT] Initializing chatbot...');
    
    // Load chat history from session storage
    loadChatHistory();
    
    // Attach event listeners
    const floatButton = document.getElementById('chatbot-float-button');
    const closeButton = document.getElementById('chatbot-close');
    const sendButton = document.getElementById('chatbot-send-button');
    const inputField = document.getElementById('chatbot-input');
    
    if (floatButton) {
        floatButton.addEventListener('click', toggleChatModal);
    }
    
    if (closeButton) {
        closeButton.addEventListener('click', toggleChatModal);
    }
    
    if (sendButton) {
        sendButton.addEventListener('click', handleSendMessage);
    }
    
    if (inputField) {
        inputField.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !isProcessing) {
                handleSendMessage();
            }
        });
    }
    
    // Attach quick action listeners
    const quickActions = document.querySelectorAll('.chatbot-quick-action');
    quickActions.forEach(button => {
        button.addEventListener('click', () => {
            const action = button.getAttribute('data-action');
            handleQuickAction(action);
        });
    });
    
    // Display welcome message if no history
    if (chatHistory.length === 0) {
        displayWelcomeMessage();
    } else {
        // Render existing chat history
        renderChatHistory();
    }
}

// Toggle chat modal visibility
function toggleChatModal() {
    const modal = document.getElementById('chatbot-modal');
    if (modal) {
        modal.classList.toggle('active');
        
        // Focus input when opening
        if (modal.classList.contains('active')) {
            const inputField = document.getElementById('chatbot-input');
            if (inputField) {
                setTimeout(() => inputField.focus(), 100);
            }
        }
    }
}

// Display welcome message and quick actions
function displayWelcomeMessage() {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (!messagesContainer) return;
    
    const welcomeHTML = `
        <div class="chatbot-welcome">
            <h4>Xin chào! 👋</h4>
            <p>Tôi là AI Assistant của EduXtend. Tôi có thể giúp bạn:</p>
            <div class="chatbot-quick-actions">
                <button class="chatbot-quick-action" data-action="find-clubs">
                    🔍 Tìm CLB phù hợp
                </button>
                <button class="chatbot-quick-action" data-action="view-activities">
                    📅 Xem hoạt động
                </button>
                <button class="chatbot-quick-action" data-action="learn-more">
                    💡 Tìm hiểu thêm
                </button>
            </div>
        </div>
    `;
    
    messagesContainer.innerHTML = welcomeHTML;
    
    // Re-attach quick action listeners
    const quickActions = messagesContainer.querySelectorAll('.chatbot-quick-action');
    quickActions.forEach(button => {
        button.addEventListener('click', () => {
            const action = button.getAttribute('data-action');
            handleQuickAction(action);
        });
    });
}

// Handle quick action button clicks
function handleQuickAction(action) {
    let message = '';
    
    switch (action) {
        case 'find-clubs':
            message = 'Tôi muốn tìm CLB phù hợp với chuyên ngành và sở thích của mình';
            break;
        case 'view-activities':
            message = 'Có hoạt động nào sắp tới không?';
            break;
        case 'learn-more':
            message = 'Cho tôi biết thêm về các CLB và hoạt động tại trường';
            break;
        default:
            return;
    }
    
    // Set input value and send
    const inputField = document.getElementById('chatbot-input');
    if (inputField) {
        inputField.value = message;
        handleSendMessage();
    }
}

// Handle send message button click
function handleSendMessage() {
    const inputField = document.getElementById('chatbot-input');
    if (!inputField) return;
    
    const message = inputField.value.trim();
    if (!message || isProcessing) return;
    
    // Clear input
    inputField.value = '';
    
    // Send message
    sendMessage(message);
}

// Check authentication status
async function checkAuthentication() {
    try {
        const response = await fetch(`${CHATBOT_API_BASE_URL}/api/auth/me`, {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Accept': 'application/json'
            }
        });
        
        if (response.ok) {
            const userData = await response.json();
            console.log('🔍 [CHATBOT DEBUG] Current user:', userData);
            console.log('🔍 [CHATBOT DEBUG] User ID:', userData.id);
            console.log('🔍 [CHATBOT DEBUG] User Name:', userData.fullName);
        }
        
        return response.ok;
    } catch (error) {
        console.error('Authentication check failed:', error);
        return false;
    }
}

// Send message to API
async function sendMessage(message) {
    if (isProcessing) return;
    
    console.log('📤 [CHATBOT] Sending message:', message);
    
    // Check authentication before sending message
    const isAuth = await checkAuthentication();
    if (!isAuth) {
        handleAuthError();
        return;
    }
    
    isProcessing = true;
    updateSendButtonState(true);
    
    // Display user message
    displayMessage(message, true);
    
    // Detect if this is a recommendation request
    const isRecommendation = isRecommendationRequest(message);
    
    // Show typing indicator with appropriate text
    showTypingIndicator(isRecommendation);
    
    try {
        // Prepare request payload
        const payload = {
            message: message,
            conversationHistory: chatHistory.map(msg => ({
                role: msg.role,
                content: msg.content,
                timestamp: msg.timestamp
            }))
        };
        
        console.log('📦 [CHATBOT] Request payload:', payload);
        
        // Create abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), API_TIMEOUT);
        
        // Send request
        const response = await fetch(CHATBOT_API_ENDPOINT, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload),
            signal: controller.signal,
            credentials: 'include' // Include cookies for authentication
        });
        
        clearTimeout(timeoutId);
        
        // Handle response
        if (response.status === 401) {
            // Unauthorized - redirect to login
            handleAuthError();
            return;
        }
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || 'Đã xảy ra lỗi khi gửi tin nhắn');
        }
        
        const data = await response.json();
        
        // Hide typing indicator
        hideTypingIndicator();
        
        if (data.success && data.message) {
            // Try to parse message as JSON for structured responses
            let parsedResponse = null;
            try {
                // Attempt to parse the message as JSON (for structured recommendations)
                parsedResponse = JSON.parse(data.message);
                console.log('📊 [CHATBOT] Parsed structured response:', parsedResponse);
            } catch (e) {
                // Not JSON, treat as plain text
                console.log('📝 [CHATBOT] Plain text response');
                console.log('📝 [CHATBOT] Message content:', data.message.substring(0, 200));
            }
            
            // Display AI response with parsed structured data if available
            if (parsedResponse && typeof parsedResponse === 'object') {
                // Check if it has valid recommendations or news
                const hasValidRecommendations = parsedResponse.recommendations && 
                                               Array.isArray(parsedResponse.recommendations) && 
                                               parsedResponse.recommendations.length > 0;
                const hasValidNews = parsedResponse.newsRecommendations && 
                                    Array.isArray(parsedResponse.newsRecommendations) && 
                                    parsedResponse.newsRecommendations.length > 0;
                
                if (hasValidRecommendations || hasValidNews) {
                    // Structured response with recommendations
                    displayMessage(parsedResponse.message || data.message, false, parsedResponse);
                } else {
                    // Has JSON structure but no recommendations - display as plain text
                    displayMessage(parsedResponse.message || data.message, false, null);
                }
            } else {
                // Plain text response
                displayMessage(data.message, false, null);
            }
        } else {
            // Display error
            displayError(data.errorMessage || 'Không nhận được phản hồi từ AI');
        }
        
    } catch (error) {
        hideTypingIndicator();
        
        if (error.name === 'AbortError') {
            displayError('Yêu cầu mất quá nhiều thời gian. Vui lòng thử lại.');
        } else if (error.message.includes('Failed to fetch')) {
            displayError('Không thể kết nối đến AI Assistant. Vui lòng thử lại sau.');
        } else {
            displayError(error.message || 'Đã xảy ra lỗi. Vui lòng thử lại sau.');
        }
    } finally {
        isProcessing = false;
        updateSendButtonState(false);
    }
}

// Detect message type (recommendations, news, or text)
function detectMessageType(response) {
    // Check if response has news recommendations
    if (response && 
        response.hasNewsRecommendations === true && 
        response.newsRecommendations && 
        Array.isArray(response.newsRecommendations) &&
        response.newsRecommendations.length > 0) {
        return 'news';
    }
    
    // Check if response has club/activity recommendations
    if (response && 
        response.hasRecommendations === true && 
        response.recommendations && 
        Array.isArray(response.recommendations) &&
        response.recommendations.length > 0) {
        return 'recommendations';
    }
    
    return 'text';
}

// Get color based on relevance score
function getScoreColor(score) {
    if (score >= 90) return '#00A86B'; // Dark green
    if (score >= 70) return '#32CD32'; // Medium green
    if (score >= 50) return '#FFD700'; // Yellow
    return '#FF8C00'; // Orange
}

// Get descriptive text for relevance score (for screen readers)
function getScoreDescription(score) {
    if (score >= 90) return 'rất phù hợp';
    if (score >= 70) return 'khá phù hợp';
    if (score >= 50) return 'phù hợp vừa phải';
    return 'ít phù hợp';
}

// Handle keyboard navigation for recommendation cards
function handleCardKeydown(event, id, type) {
    // Support Enter and Space keys for activation
    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        navigateToDetail(id, type);
    }
}

// Render recommendation card
function renderRecommendationCard(recommendation) {
    // Map recommendation type to icon
    const typeIcon = recommendation.type === 'club' ? '👥' : '🎯';
    const scoreColor = getScoreColor(recommendation.relevanceScore);
    
    // Build comprehensive aria-label for screen readers
    const typeLabel = recommendation.type === 'club' ? 'Câu lạc bộ' : 'Hoạt động';
    const scoreDescription = getScoreDescription(recommendation.relevanceScore);
    const ariaLabel = `${typeLabel}: ${recommendation.name}. ${recommendation.description || ''}. Lý do phù hợp: ${recommendation.reason}. Độ phù hợp: ${recommendation.relevanceScore} phần trăm, ${scoreDescription}. Nhấn Enter hoặc Space để xem chi tiết.`;
    
    // Generate card HTML with full accessibility support
    const cardHTML = `
        <div class="recommendation-card" 
             data-id="${recommendation.id}" 
             data-type="${recommendation.type}"
             onclick="navigateToDetail(${recommendation.id}, '${recommendation.type}')"
             role="button"
             tabindex="0"
             aria-label="${escapeHtml(ariaLabel)}"
             onkeydown="handleCardKeydown(event, ${recommendation.id}, '${recommendation.type}')">
            <div class="card-header" aria-hidden="true">
                <span class="card-type-icon" role="img" aria-label="${typeLabel}">${typeIcon}</span>
                <span class="card-type-label">${recommendation.type.toUpperCase()}</span>
            </div>
            <h3 class="card-title" aria-hidden="true">${escapeHtml(recommendation.name)}</h3>
            ${recommendation.description ? 
                `<p class="card-description" aria-hidden="true">${escapeHtml(recommendation.description)}</p>` : ''}
            <div class="card-reason" aria-hidden="true">
                <span class="reason-icon" role="img" aria-label="Lý do">💡</span>
                <p class="reason-text">${escapeHtml(recommendation.reason)}</p>
            </div>
            <div class="card-score" aria-hidden="true">
                <span class="score-icon" role="img" aria-label="Độ phù hợp">✨</span>
                <span class="score-text" style="color: ${scoreColor}">
                    Độ phù hợp: ${recommendation.relevanceScore}%
                </span>
                <span class="sr-only">Độ phù hợp: ${recommendation.relevanceScore} phần trăm, ${scoreDescription}</span>
            </div>
        </div>
    `;
    
    return cardHTML;
}

// Render news recommendation card
function renderNewsCard(news) {
    const typeIcon = '📰';
    const scoreColor = getScoreColor(news.relevanceScore);
    
    // Format published date
    const publishedDate = new Date(news.publishedAt);
    const dateString = publishedDate.toLocaleDateString('vi-VN', { 
        year: 'numeric', 
        month: '2-digit', 
        day: '2-digit' 
    });
    
    // Determine news type label and URL
    const isClubNews = news.type === 'club_news';
    const typeLabel = isClubNews ? 'Tin CLB' : 'Thông báo';
    const newsUrl = isClubNews ? `/News/ClubNewsDetails/${news.id}` : `/News/SystemNewsDetails/${news.id}`;
    
    // Build aria-label for accessibility
    const scoreDescription = getScoreDescription(news.relevanceScore);
    const ariaLabel = `${typeLabel}: ${news.title}. Nguồn: ${news.source}. Ngày đăng: ${dateString}. ${news.summary}. Lý do liên quan: ${news.reason}. Độ phù hợp: ${news.relevanceScore} phần trăm, ${scoreDescription}. Nhấn Enter hoặc Space để xem chi tiết.`;
    
    // Generate card HTML
    const cardHTML = `
        <div class="recommendation-card news-card" 
             data-id="${news.id}" 
             data-type="${news.type}"
             onclick="navigateToNewsDetail(${news.id}, '${news.type}')"
             role="button"
             tabindex="0"
             aria-label="${escapeHtml(ariaLabel)}"
             onkeydown="handleNewsCardKeydown(event, ${news.id}, '${news.type}')">
            <div class="card-header" aria-hidden="true">
                <span class="card-type-icon" role="img" aria-label="${typeLabel}">${typeIcon}</span>
                <span class="card-type-label">${typeLabel.toUpperCase()}</span>
            </div>
            <h3 class="card-title" aria-hidden="true">${escapeHtml(news.title)}</h3>
            <div class="news-meta" aria-hidden="true">
                <span class="news-source">📍 ${escapeHtml(news.source)}</span>
                <span class="news-date">📅 ${dateString}</span>
            </div>
            ${news.summary ? 
                `<p class="card-description" aria-hidden="true">${escapeHtml(news.summary)}</p>` : ''}
            <div class="card-reason" aria-hidden="true">
                <span class="reason-icon" role="img" aria-label="Lý do">💡</span>
                <p class="reason-text">${escapeHtml(news.reason)}</p>
            </div>
            <div class="card-score" aria-hidden="true">
                <span class="score-icon" role="img" aria-label="Độ phù hợp">✨</span>
                <span class="score-text" style="color: ${scoreColor}">
                    Độ phù hợp: ${news.relevanceScore}%
                </span>
                <span class="sr-only">Độ phù hợp: ${news.relevanceScore} phần trăm, ${scoreDescription}</span>
            </div>
        </div>
    `;
    
    return cardHTML;
}

// Navigate to detail page
function navigateToDetail(id, type) {
    // Track card click for analytics (optional)
    try {
        console.log(`📊 [CHATBOT ANALYTICS] Card clicked - Type: ${type}, ID: ${id}`);
        
        // If Google Analytics is available, track the event
        if (typeof gtag !== 'undefined') {
            gtag('event', 'recommendation_card_click', {
                'event_category': 'chatbot',
                'event_label': `${type}_${id}`,
                'value': id
            });
        }
    } catch (error) {
        console.error('Analytics tracking failed:', error);
    }
    
    // Construct detail page URL based on type
    const url = type === 'club' 
        ? `/Clubs/Details/${id}` 
        : `/Activities/Details/${id}`;
    
    // Navigate to detail page in same tab
    window.location.href = url;
}

// Navigate to news detail page
function navigateToNewsDetail(id, type) {
    // Track news card click for analytics
    try {
        console.log(`📊 [CHATBOT ANALYTICS] News card clicked - Type: ${type}, ID: ${id}`);
        
        if (typeof gtag !== 'undefined') {
            gtag('event', 'news_card_click', {
                'event_category': 'chatbot',
                'event_label': `${type}_${id}`,
                'value': id
            });
        }
    } catch (error) {
        console.error('Analytics tracking failed:', error);
    }
    
    // Construct news detail page URL based on type
    const url = type === 'club_news' 
        ? `/News/ClubNewsDetails/${id}` 
        : `/News/SystemNewsDetails/${id}`;
    
    // Navigate to news detail page in same tab
    window.location.href = url;
}

// Handle keyboard navigation for news cards
function handleNewsCardKeydown(event, id, type) {
    // Support Enter and Space keys for activation
    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        navigateToNewsDetail(id, type);
    }
}

// Display message in chat
function displayMessage(content, isUser, response = null) {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (!messagesContainer) return;
    
    // Remove welcome message if exists
    const welcomeMsg = messagesContainer.querySelector('.chatbot-welcome');
    if (welcomeMsg) {
        welcomeMsg.remove();
    }
    
    // Create message element
    const messageDiv = document.createElement('div');
    messageDiv.className = `chatbot-message ${isUser ? 'user' : 'assistant'}`;
    
    const timestamp = new Date();
    const timeString = timestamp.toLocaleTimeString('vi-VN', { 
        hour: '2-digit', 
        minute: '2-digit' 
    });
    
    // Handle assistant messages with potential recommendations
    if (!isUser && response) {
        const messageType = detectMessageType(response);
        
        if (messageType === 'news') {
            // Render intro text and news recommendations INSIDE the same bubble
            const formattedContent = response.message ? parseMessageLinks(response.message) : '';
            const newsHTML = response.newsRecommendations.map(news => renderNewsCard(news)).join('');
            
            messageDiv.innerHTML = `
                <div class="chatbot-message-bubble">
                    ${formattedContent ? `<div class="message-intro">${formattedContent}</div>` : ''}
                    <div class="recommendations-container">
                        ${newsHTML}
                    </div>
                    <div class="chatbot-message-time">${timeString}</div>
                </div>
            `;
        } else if (messageType === 'recommendations') {
            // Render intro text and recommendations INSIDE the same bubble
            const formattedContent = response.message ? parseMessageLinks(response.message) : '';
            const recommendationsHTML = response.recommendations.map(rec => renderRecommendationCard(rec)).join('');
            
            messageDiv.innerHTML = `
                <div class="chatbot-message-bubble">
                    ${formattedContent ? `<div class="message-intro">${formattedContent}</div>` : ''}
                    <div class="recommendations-container">
                        ${recommendationsHTML}
                    </div>
                    <div class="chatbot-message-time">${timeString}</div>
                </div>
            `;
        } else {
            // Standard text message
            const formattedContent = parseMessageLinks(content);
            messageDiv.innerHTML = `
                <div class="chatbot-message-bubble">
                    ${formattedContent}
                    <div class="chatbot-message-time">${timeString}</div>
                </div>
            `;
        }
    } else {
        // User message or plain text
        const formattedContent = isUser ? escapeHtml(content) : parseMessageLinks(content);
        messageDiv.innerHTML = `
            <div class="chatbot-message-bubble">
                ${formattedContent}
                <div class="chatbot-message-time">${timeString}</div>
            </div>
        `;
    }
    
    messagesContainer.appendChild(messageDiv);
    
    // Scroll to bottom
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
    
    // Add to chat history
    const chatMessage = {
        role: isUser ? 'user' : 'assistant',
        content: content,
        timestamp: timestamp.toISOString()
    };
    
    chatHistory.push(chatMessage);
    
    // Limit history to max messages
    if (chatHistory.length > MAX_CHAT_HISTORY) {
        chatHistory = chatHistory.slice(-MAX_CHAT_HISTORY);
    }
    
    // Save to session storage
    saveChatHistory();
}

// Display error message
function displayError(errorMessage) {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (!messagesContainer) return;
    
    const errorDiv = document.createElement('div');
    errorDiv.className = 'chatbot-error';
    errorDiv.textContent = errorMessage;
    
    messagesContainer.appendChild(errorDiv);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

// Detect if message is requesting recommendations
function isRecommendationRequest(message) {
    const lowerMessage = message.toLowerCase();
    
    // Keywords that indicate recommendation requests
    const recommendationKeywords = [
        'tìm clb',
        'tìm câu lạc bộ',
        'tìm club',
        'gợi ý clb',
        'gợi ý câu lạc bộ',
        'gợi ý club',
        'đề xuất clb',
        'đề xuất câu lạc bộ',
        'đề xuất club',
        'giới thiệu clb',
        'giới thiệu câu lạc bộ',
        'giới thiệu club',
        'clb nào',
        'câu lạc bộ nào',
        'club nào',
        'clb phù hợp',
        'câu lạc bộ phù hợp',
        'club phù hợp',
        'hoạt động nào',
        'activity nào',
        'tìm hoạt động',
        'gợi ý hoạt động',
        'đề xuất hoạt động'
    ];
    
    return recommendationKeywords.some(keyword => lowerMessage.includes(keyword));
}

// Show typing indicator with debouncing to prevent flickering
function showTypingIndicator(isRecommendation = false) {
    // Clear any existing timeout to prevent flickering
    if (typingIndicatorTimeout) {
        clearTimeout(typingIndicatorTimeout);
        typingIndicatorTimeout = null;
    }
    
    // Debounce: only show indicator after 150ms delay
    // This prevents flickering for very fast responses
    typingIndicatorTimeout = setTimeout(() => {
        const indicator = document.getElementById('chatbot-typing');
        if (indicator) {
            // Update typing indicator text based on message type
            const typingText = indicator.querySelector('.chatbot-typing-text');
            if (typingText) {
                if (isRecommendation) {
                    typingText.textContent = 'AI đang tìm kiếm câu lạc bộ phù hợp';
                } else {
                    typingText.textContent = 'AI đang suy nghĩ';
                }
            }
            
            indicator.classList.add('active');
            
            // Scroll to bottom
            const messagesContainer = document.getElementById('chatbot-messages');
            if (messagesContainer) {
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            }
        }
        typingIndicatorTimeout = null;
    }, 150); // 150ms debounce delay
}

// Hide typing indicator
function hideTypingIndicator() {
    // Clear debounce timeout if indicator is being hidden before it shows
    if (typingIndicatorTimeout) {
        clearTimeout(typingIndicatorTimeout);
        typingIndicatorTimeout = null;
    }
    
    const indicator = document.getElementById('chatbot-typing');
    if (indicator) {
        indicator.classList.remove('active');
    }
}

// Update send button state
function updateSendButtonState(disabled) {
    const sendButton = document.getElementById('chatbot-send-button');
    if (sendButton) {
        sendButton.disabled = disabled;
    }
}

// Load chat history from session storage
function loadChatHistory() {
    try {
        const stored = sessionStorage.getItem(STORAGE_KEY);
        if (stored) {
            chatHistory = JSON.parse(stored);
        }
    } catch (error) {
        console.error('Failed to load chat history:', error);
        chatHistory = [];
    }
}

// Save chat history to session storage
function saveChatHistory() {
    try {
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(chatHistory));
    } catch (error) {
        console.error('Failed to save chat history:', error);
    }
}

// Clear chat history from session storage
function clearChatHistory() {
    try {
        sessionStorage.removeItem(STORAGE_KEY);
        chatHistory = [];
    } catch (error) {
        console.error('Failed to clear chat history:', error);
    }
}

// Render existing chat history
function renderChatHistory() {
    const messagesContainer = document.getElementById('chatbot-messages');
    if (!messagesContainer) return;
    
    messagesContainer.innerHTML = '';
    
    chatHistory.forEach(msg => {
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-message ${msg.role === 'user' ? 'user' : 'assistant'}`;
        
        const timestamp = new Date(msg.timestamp);
        const timeString = timestamp.toLocaleTimeString('vi-VN', { 
            hour: '2-digit', 
            minute: '2-digit' 
        });
        
        messageDiv.innerHTML = `
            <div class="chatbot-message-bubble">
                ${escapeHtml(msg.content)}
                <div class="chatbot-message-time">${timeString}</div>
            </div>
        `;
        
        messagesContainer.appendChild(messageDiv);
    });
    
    // Scroll to bottom
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

// Handle authentication error
function handleAuthError() {
    // Clear chat history on authentication error
    clearChatHistory();
    
    displayError('Bạn cần đăng nhập để sử dụng AI Assistant.');
    
    // Redirect to login after 2 seconds
    setTimeout(() => {
        window.location.href = '/Login';
    }, 2000);
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Parse message content and convert [CLUB:ID:Name] and [ACTIVITY:ID:Name] to clickable links
// Also format text with line breaks for better readability
function parseMessageLinks(content) {
    // Escape HTML first
    let escaped = escapeHtml(content);
    
    // Replace [CLUB:ID:Name] with clickable link
    escaped = escaped.replace(/\[CLUB:(\d+):([^\]]+)\]/g, (match, id, name) => {
        return `<a href="/Clubs/Details/${id}" target="_blank" class="chatbot-link chatbot-club-link" title="Xem chi tiết CLB">
            <i data-lucide="users" style="width: 14px; height: 14px;"></i>
            ${name}
            <i data-lucide="external-link" style="width: 12px; height: 12px;"></i>
        </a>`;
    });
    
    // Replace [ACTIVITY:ID:Name] with clickable link
    escaped = escaped.replace(/\[ACTIVITY:(\d+):([^\]]+)\]/g, (match, id, name) => {
        return `<a href="/Activities/Details/${id}" target="_blank" class="chatbot-link chatbot-activity-link" title="Xem chi tiết hoạt động">
            <i data-lucide="calendar" style="width: 14px; height: 14px;"></i>
            ${name}
            <i data-lucide="external-link" style="width: 12px; height: 12px;"></i>
        </a>`;
    });
    
    // Convert line breaks to <br> for better formatting
    escaped = escaped.replace(/\n/g, '<br>');
    
    // Add spacing after emoji-prefixed lines for better readability
    escaped = escaped.replace(/(📌|🎯|📅|📍|👥|✨|📰|💡|🔔|ℹ️)([^<\n]+)/g, '<div class="emoji-line">$1 $2</div>');
    
    // Reinitialize Lucide icons for the new links
    setTimeout(() => {
        if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
            lucide.createIcons();
        }
    }, 10);
    
    return escaped;
}

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initChatbot);
} else {
    initChatbot();
}

// Export functions for external use
window.chatbot = {
    clearChatHistory: clearChatHistory,
    toggleChatModal: toggleChatModal,
    checkAuthentication: checkAuthentication
};

// Make clearChatHistory available globally for auth.js
window.clearChatHistory = clearChatHistory;
