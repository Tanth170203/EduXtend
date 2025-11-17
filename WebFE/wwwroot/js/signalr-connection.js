/**
 * SignalR Connection for Real-time Notifications
 */

class SignalRConnection {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.init();
    }

    async init() {
        try {
            // Create connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("https://localhost:5001/notificationHub", {
                    withCredentials: true,
                    accessTokenFactory: () => this.getAccessToken()
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.elapsedMilliseconds < 60000) {
                            // Retry every 5 seconds for the first minute
                            return 5000;
                        } else {
                            // After 1 minute, retry every 30 seconds
                            return 30000;
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Start connection
            await this.start();
        } catch (error) {
            console.error('Error initializing SignalR connection:', error);
        }
    }

    getAccessToken() {
        // Get token from cookie
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'AccessToken') {
                return value;
            }
        }
        return null;
    }

    setupEventHandlers() {
        // Handle incoming notifications
        this.connection.on("ReceiveNotification", (notification) => {
            console.log('Received notification:', notification);
            
            // Add to notification manager
            if (window.notificationManager) {
                window.notificationManager.addPersistentNotification(notification);
            }
        });

        // Connection lifecycle events
        this.connection.onreconnecting((error) => {
            console.warn('SignalR reconnecting...', error);
            this.isConnected = false;
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected:', connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
        });

        this.connection.onclose((error) => {
            console.error('SignalR connection closed:', error);
            this.isConnected = false;
            
            // Attempt manual reconnect after a delay
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                setTimeout(() => {
                    this.reconnectAttempts++;
                    console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
                    this.start();
                }, 5000);
            }
        });
    }

    async start() {
        try {
            await this.connection.start();
            console.log('SignalR connected successfully');
            this.isConnected = true;
            this.reconnectAttempts = 0;
        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            this.isConnected = false;
            
            // Retry connection
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                setTimeout(() => {
                    this.reconnectAttempts++;
                    console.log(`Retrying connection (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
                    this.start();
                }, 5000);
            }
        }
    }

    async stop() {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log('SignalR connection stopped');
                this.isConnected = false;
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }

    getConnectionState() {
        if (!this.connection) return 'Disconnected';
        
        switch (this.connection.state) {
            case signalR.HubConnectionState.Connected:
                return 'Connected';
            case signalR.HubConnectionState.Connecting:
                return 'Connecting';
            case signalR.HubConnectionState.Reconnecting:
                return 'Reconnecting';
            case signalR.HubConnectionState.Disconnected:
                return 'Disconnected';
            default:
                return 'Unknown';
        }
    }
}

// Initialize SignalR connection when page loads
let signalRConnection = null;

document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if user is logged in
    const token = document.cookie.split(';').find(c => c.trim().startsWith('AccessToken='));
    if (token) {
        signalRConnection = new SignalRConnection();
        window.signalRConnection = signalRConnection.connection;
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (signalRConnection) {
        signalRConnection.stop();
    }
});
