/**
 * Map Picker Component for Interview Location Selection
 * Uses Leaflet + OpenStreetMap (free, no API key required)
 */

class MapPicker {
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.options = {
            defaultLat: options.defaultLat || 10.762622, // Default to Ho Chi Minh City
            defaultLng: options.defaultLng || 106.660172,
            defaultZoom: options.defaultZoom || 13,
            onLocationSelected: options.onLocationSelected || null,
            ...options
        };
        
        this.map = null;
        this.marker = null;
        this.selectedLat = null;
        this.selectedLng = null;
    }

    /**
     * Initialize the map
     */
    init() {
        const container = document.getElementById(this.containerId);
        if (!container) {
            console.error(`Container with id "${this.containerId}" not found`);
            return;
        }

        // Create map
        this.map = L.map(this.containerId).setView(
            [this.options.defaultLat, this.options.defaultLng],
            this.options.defaultZoom
        );

        // Add OpenStreetMap tiles
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(this.map);

        // Add click handler
        this.map.on('click', (e) => this.onMapClick(e));

        // If initial coordinates provided, set marker
        if (this.options.initialLat && this.options.initialLng) {
            this.setLocation(this.options.initialLat, this.options.initialLng);
        }
    }

    /**
     * Handle map click
     */
    onMapClick(e) {
        const { lat, lng } = e.latlng;
        this.setLocation(lat, lng);
    }

    /**
     * Set location and update marker
     */
    setLocation(lat, lng) {
        this.selectedLat = lat;
        this.selectedLng = lng;

        // Remove existing marker
        if (this.marker) {
            this.map.removeLayer(this.marker);
        }

        // Add new marker
        this.marker = L.marker([lat, lng], {
            draggable: true
        }).addTo(this.map);

        // Handle marker drag
        this.marker.on('dragend', (e) => {
            const position = e.target.getLatLng();
            this.setLocation(position.lat, position.lng);
        });

        // Center map on marker
        this.map.setView([lat, lng], this.map.getZoom());

        // Trigger callback
        if (this.options.onLocationSelected) {
            this.options.onLocationSelected(lat, lng);
        }
    }

    /**
     * Get current selected location
     */
    getLocation() {
        return {
            lat: this.selectedLat,
            lng: this.selectedLng
        };
    }

    /**
     * Clear selection
     */
    clear() {
        if (this.marker) {
            this.map.removeLayer(this.marker);
            this.marker = null;
        }
        this.selectedLat = null;
        this.selectedLng = null;
    }

    /**
     * Destroy the map
     */
    destroy() {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
    }

    /**
     * Use current device location
     */
    useCurrentLocation() {
        if (!navigator.geolocation) {
            alert('Geolocation is not supported by your browser');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                const { latitude, longitude } = position.coords;
                this.setLocation(latitude, longitude);
            },
            (error) => {
                console.error('Error getting location:', error);
                alert('Unable to get your location. Please select manually on the map.');
            }
        );
    }
}

/**
 * Map Viewer Component for displaying interview location (read-only)
 */
class MapViewer {
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.options = {
            lat: options.lat,
            lng: options.lng,
            zoom: options.zoom || 15,
            ...options
        };
        
        this.map = null;
        this.marker = null;
    }

    /**
     * Initialize the map viewer
     */
    init() {
        const container = document.getElementById(this.containerId);
        if (!container) {
            console.error(`Container with id "${this.containerId}" not found`);
            return;
        }

        if (!this.options.lat || !this.options.lng) {
            console.error('Latitude and longitude are required');
            return;
        }

        // Create map
        this.map = L.map(this.containerId, {
            dragging: true,
            touchZoom: true,
            scrollWheelZoom: false,
            doubleClickZoom: true,
            boxZoom: false,
            keyboard: false,
            zoomControl: true
        }).setView([this.options.lat, this.options.lng], this.options.zoom);

        // Add OpenStreetMap tiles
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(this.map);

        // Add marker
        this.marker = L.marker([this.options.lat, this.options.lng]).addTo(this.map);

        // Add popup if address provided
        if (this.options.address) {
            this.marker.bindPopup(this.options.address).openPopup();
        }
    }

    /**
     * Generate external map links
     */
    getExternalMapLinks() {
        const { lat, lng } = this.options;
        return {
            googleMaps: `https://www.google.com/maps?q=${lat},${lng}`,
            appleMaps: `https://maps.apple.com/?q=${lat},${lng}`,
            openStreetMap: `https://www.openstreetmap.org/?mlat=${lat}&mlon=${lng}&zoom=15`
        };
    }

    /**
     * Destroy the map
     */
    destroy() {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
    }
}

// Export for use in other scripts
window.MapPicker = MapPicker;
window.MapViewer = MapViewer;
