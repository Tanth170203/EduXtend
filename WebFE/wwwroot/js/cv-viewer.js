// CV Viewer Component using PDF.js
// Requires: PDF.js library loaded via CDN

class CVViewer {
    constructor() {
        this.modal = null;
        this.pdfDoc = null;
        this.currentPage = 1;
        this.totalPages = 0;
        this.scale = 1.5;
        this.cvUrl = null;
        this.canvas = null;
        this.ctx = null;
        this.isRendering = false;
        
        // Initialize when DOM is ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.init());
        } else {
            this.init();
        }
    }

    init() {
        // Get modal element
        this.modal = document.getElementById('cvViewerModal');
        if (!this.modal) {
            console.error('CV Viewer Modal not found. Make sure _CVViewerModal.cshtml is included.');
            return;
        }

        // Get canvas and context
        this.canvas = document.getElementById('cvViewerCanvas');
        if (this.canvas) {
            this.ctx = this.canvas.getContext('2d');
        }

        // Setup event listeners
        this.setupEventListeners();

        console.log('CV Viewer initialized');
    }

    setupEventListeners() {
        // Close modal cleanup
        this.modal.addEventListener('hidden.bs.modal', () => {
            this.cleanup();
        });

        // Zoom controls
        const zoomInBtn = document.getElementById('cvZoomIn');
        const zoomOutBtn = document.getElementById('cvZoomOut');
        if (zoomInBtn) zoomInBtn.addEventListener('click', () => this.zoomIn());
        if (zoomOutBtn) zoomOutBtn.addEventListener('click', () => this.zoomOut());

        // Page navigation
        const prevBtn = document.getElementById('cvPrevPage');
        const nextBtn = document.getElementById('cvNextPage');
        if (prevBtn) prevBtn.addEventListener('click', () => this.previousPage());
        if (nextBtn) nextBtn.addEventListener('click', () => this.nextPage());

        // Open in new tab buttons
        const openNewTabBtn = document.getElementById('cvOpenNewTab');
        const errorOpenNewTabBtn = document.getElementById('cvErrorOpenNewTab');
        if (openNewTabBtn) {
            openNewTabBtn.addEventListener('click', () => this.openInNewTab());
        }
        if (errorOpenNewTabBtn) {
            errorOpenNewTabBtn.addEventListener('click', () => this.openInNewTab());
        }

        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (this.modal && this.modal.classList.contains('show')) {
                if (e.key === 'Escape') {
                    bootstrap.Modal.getInstance(this.modal)?.hide();
                } else if (e.key === 'ArrowLeft') {
                    this.previousPage();
                } else if (e.key === 'ArrowRight') {
                    this.nextPage();
                } else if (e.key === '+' || e.key === '=') {
                    this.zoomIn();
                } else if (e.key === '-') {
                    this.zoomOut();
                }
            }
        });
    }

    async showCV(cvUrl, options = {}) {
        if (!cvUrl) {
            console.error('CV URL is required');
            return;
        }

        // Validate URL
        if (!cvUrl.startsWith('http://') && !cvUrl.startsWith('https://')) {
            console.error('Invalid CV URL:', cvUrl);
            return;
        }

        this.cvUrl = cvUrl;
        this.currentPage = 1;
        this.scale = options.initialScale || 1.5;
        
        // Flag to track if we should show errors
        this.hasShownError = false;

        // Update modal title if provided
        if (options.title) {
            const titleElement = document.querySelector('#cvViewerModalLabel span');
            if (titleElement) {
                titleElement.textContent = options.title;
            }
        }

        console.log('showCV called with:', { cvUrl, options });
        
        // Reset state before showing modal
        const error = document.getElementById('cvViewerError');
        const container = document.getElementById('cvCanvasContainer');
        if (error) error.classList.add('d-none');
        if (container) container.style.display = 'none';
        
        // Show modal
        const bsModal = new bootstrap.Modal(this.modal);
        bsModal.show();
        
        console.log('Modal shown, starting to load PDF...');

        // Show loading state (this will also hide error)
        this.showLoading();

        // Check if PDF.js is loaded
        if (typeof pdfjsLib === 'undefined') {
            console.error('PDF.js library not loaded');
            console.error('Available globals:', Object.keys(window).filter(k => k.toLowerCase().includes('pdf')));
            this.showError('PDF viewer library not loaded. Please refresh the page and try again.');
            return;
        }
        
        console.log('PDF.js version:', pdfjsLib.version);

        try {
            // Try to load PDF with different configurations
            let loadingTask;
            
            // Suppress PDF.js warnings that don't affect functionality
            const originalConsoleWarn = console.warn;
            const originalConsoleError = console.error;
            
            // First attempt: with credentials and CORS handling
            try {
                loadingTask = pdfjsLib.getDocument({
                    url: cvUrl,
                    withCredentials: false, // Changed to false to avoid CORS preflight
                    httpHeaders: {},
                    disableRange: true,
                    disableStream: true,
                    isEvalSupported: false,
                    // Add timeout
                    maxImageSize: -1,
                    cMapUrl: 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/cmaps/',
                    cMapPacked: true,
                    // Suppress non-critical warnings
                    verbosity: 0
                });
            } catch (configError) {
                console.warn('First PDF config failed, trying simple config:', configError);
                // Fallback: simple configuration
                loadingTask = pdfjsLib.getDocument(cvUrl);
            }
            
            // Temporarily suppress console errors from PDF.js that don't affect rendering
            console.warn = function(...args) {
                const msg = args.join(' ');
                // Only log if it's not a PDF.js warning about fonts or rendering
                if (!msg.includes('TrueType') && !msg.includes('CMap') && !msg.includes('font')) {
                    originalConsoleWarn.apply(console, args);
                }
            };
            
            this.pdfDoc = await loadingTask.promise;
            this.totalPages = this.pdfDoc.numPages;

            console.log(`PDF loaded successfully: ${this.totalPages} pages`);

            // Render first page - wrap in try-catch to handle render errors
            try {
                await this.renderPage(1);
                console.log('First page rendered successfully');
            } catch (renderError) {
                console.error('Error rendering first page:', renderError);
                // Check if PDF loaded successfully even if render had issues
                if (this.pdfDoc && this.totalPages > 0) {
                    console.warn('PDF loaded, retrying render...');
                    // Try one more time
                    try {
                        await this.renderPage(1);
                    } catch (retryError) {
                        console.error('Retry failed:', retryError);
                        // If still fails, just show the error
                        throw retryError;
                    }
                } else {
                    throw renderError;
                }
            }

            // Restore console
            console.warn = originalConsoleWarn;
            console.error = originalConsoleError;

            // Show controls
            this.showControls();

        } catch (error) {
            console.error('Error loading PDF:', error);
            console.error('Error details:', {
                name: error.name,
                message: error.message,
                stack: error.stack
            });
            
            // Check if PDF was actually loaded before showing error
            // If pdfDoc exists, it means PDF loaded successfully even if there were warnings
            if (this.pdfDoc && this.pdfDoc.numPages > 0) {
                console.warn('PDF loaded successfully despite warnings, continuing...');
                // Don't show error, PDF is fine
                return;
            }
            
            // More detailed error message
            let errorMessage = 'Failed to load CV. ';
            if (error.name === 'MissingPDFException') {
                errorMessage += 'The file may be corrupted or unavailable.';
            } else if (error.name === 'InvalidPDFException') {
                errorMessage += 'The file is not a valid PDF document.';
            } else if (error.message && (error.message.includes('CORS') || error.message.includes('cross-origin'))) {
                errorMessage += 'Access denied due to security restrictions. Please use the "Open in New Tab" button below.';
            } else if (error.message && (error.message.includes('404') || error.message.includes('Not Found'))) {
                errorMessage += 'The file was not found on the server.';
            } else if (error.message && error.message.includes('NetworkError')) {
                errorMessage += 'Network error. Please check your internet connection.';
            } else {
                errorMessage += 'The file may be corrupted or unavailable. Error: ' + (error.message || 'Unknown error');
            }
            
            this.showError(errorMessage);
            
            // Call error callback if provided
            if (options.onError) {
                options.onError(error);
            }
        }
    }

    async renderPage(pageNum) {
        if (!this.pdfDoc || this.isRendering) return;

        this.isRendering = true;
        this.currentPage = pageNum;

        try {
            console.log(`Starting to render page ${pageNum}...`);
            
            // Get page
            const page = await this.pdfDoc.getPage(pageNum);

            // Calculate viewport
            const viewport = page.getViewport({ scale: this.scale });

            // Set canvas dimensions
            this.canvas.width = viewport.width;
            this.canvas.height = viewport.height;

            // Render page
            const renderContext = {
                canvasContext: this.ctx,
                viewport: viewport
            };

            await page.render(renderContext).promise;

            console.log(`Page ${pageNum} rendered successfully`);

            // Update UI - this will hide loading and error
            this.updatePageInfo();
            this.updateZoomInfo();
            this.hideLoading(); // This hides both loading and error

            console.log(`Rendered page ${pageNum} of ${this.totalPages}`);

        } catch (error) {
            console.error('Error rendering page:', error);
            
            // Check if canvas has content (render was successful despite error)
            if (this.canvas && this.canvas.width > 0 && this.canvas.height > 0) {
                console.warn('Page rendered successfully despite error, continuing...');
                // Still update UI even if there was an error
                this.updatePageInfo();
                this.updateZoomInfo();
                this.hideLoading();
            } else {
                // Only show error if render actually failed
                this.showError('Failed to render PDF page. Error: ' + error.message);
            }
        } finally {
            this.isRendering = false;
        }
    }

    showLoading() {
        const loading = document.getElementById('cvViewerLoading');
        const error = document.getElementById('cvViewerError');
        const container = document.getElementById('cvCanvasContainer');
        
        if (loading) loading.style.display = 'block';
        // IMPORTANT: Always hide error when showing loading
        if (error) error.classList.add('d-none');
        if (container) container.style.display = 'none';
        
        console.log('Showing loading, hiding error and canvas');
    }

    hideLoading() {
        const loading = document.getElementById('cvViewerLoading');
        const error = document.getElementById('cvViewerError');
        const container = document.getElementById('cvCanvasContainer');
        
        if (loading) loading.style.display = 'none';
        // IMPORTANT: Always hide error when showing content
        if (error) error.classList.add('d-none');
        if (container) container.style.display = 'flex';
        
        // Mark that we've successfully shown content
        this.hasShownError = true; // Prevent future errors from showing
        
        console.log('Loading hidden, showing canvas container');
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined' && lucide.createIcons) {
            lucide.createIcons();
        }
    }

    showError(message) {
        // Don't show error if we've already shown content successfully
        if (this.hasShownError) {
            console.warn('Error already shown or content displayed, skipping:', message);
            return;
        }
        
        this.hasShownError = true;
        
        const loading = document.getElementById('cvViewerLoading');
        const error = document.getElementById('cvViewerError');
        const errorMessage = document.getElementById('cvErrorMessage');
        const container = document.getElementById('cvCanvasContainer');
        
        // Check if canvas is already showing content
        if (container && container.style.display === 'flex') {
            console.warn('Canvas already showing, not displaying error');
            return;
        }
        
        if (loading) loading.style.display = 'none';
        if (container) container.style.display = 'none';
        if (error) error.classList.remove('d-none');
        if (errorMessage) {
            // Clear previous content
            errorMessage.innerHTML = '';
            
            // Add main error message
            const mainMsg = document.createElement('p');
            mainMsg.className = 'mb-2';
            mainMsg.textContent = message;
            errorMessage.appendChild(mainMsg);
            
            // Add helpful message
            const helpText = document.createElement('p');
            helpText.className = 'mb-0 small text-muted';
            helpText.textContent = 'If the problem persists, try opening the CV in a new tab using the button below.';
            errorMessage.appendChild(helpText);
        }
        
        // Hide controls on error
        this.hideControls();
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined' && lucide.createIcons) {
            lucide.createIcons();
        }
        
        console.error('CV Viewer Error:', message);
    }

    showControls() {
        const pageControls = document.getElementById('cvPageControls');
        const zoomControls = document.getElementById('cvZoomControls');
        
        if (pageControls && this.totalPages > 1) {
            pageControls.style.display = 'flex';
        }
        if (zoomControls) {
            zoomControls.style.display = 'flex';
        }
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined' && lucide.createIcons) {
            lucide.createIcons();
        }
    }

    hideControls() {
        const pageControls = document.getElementById('cvPageControls');
        const zoomControls = document.getElementById('cvZoomControls');
        
        if (pageControls) pageControls.style.display = 'none';
        if (zoomControls) zoomControls.style.display = 'none';
    }

    updatePageInfo() {
        const pageInfo = document.getElementById('cvPageInfo');
        if (pageInfo) {
            pageInfo.textContent = `Page ${this.currentPage} of ${this.totalPages}`;
        }

        // Update button states
        const prevBtn = document.getElementById('cvPrevPage');
        const nextBtn = document.getElementById('cvNextPage');
        
        if (prevBtn) prevBtn.disabled = this.currentPage <= 1;
        if (nextBtn) nextBtn.disabled = this.currentPage >= this.totalPages;
    }

    updateZoomInfo() {
        const zoomLevel = document.getElementById('cvZoomLevel');
        if (zoomLevel) {
            const percentage = Math.round(this.scale * 100);
            zoomLevel.textContent = `${percentage}%`;
        }

        // Update button states
        const zoomInBtn = document.getElementById('cvZoomIn');
        const zoomOutBtn = document.getElementById('cvZoomOut');
        
        if (zoomInBtn) zoomInBtn.disabled = this.scale >= 3.0;
        if (zoomOutBtn) zoomOutBtn.disabled = this.scale <= 0.5;
    }

    async nextPage() {
        if (this.currentPage < this.totalPages) {
            await this.renderPage(this.currentPage + 1);
        }
    }

    async previousPage() {
        if (this.currentPage > 1) {
            await this.renderPage(this.currentPage - 1);
        }
    }

    async zoomIn() {
        if (this.scale < 3.0) {
            this.scale += 0.25;
            await this.renderPage(this.currentPage);
        }
    }

    async zoomOut() {
        if (this.scale > 0.5) {
            this.scale -= 0.25;
            await this.renderPage(this.currentPage);
        }
    }

    openInNewTab() {
        if (this.cvUrl) {
            window.open(this.cvUrl, '_blank');
        }
    }

    cleanup() {
        console.log('Cleaning up CV Viewer...');
        
        // Clean up PDF document
        if (this.pdfDoc) {
            this.pdfDoc.destroy();
            this.pdfDoc = null;
        }

        // Reset state
        this.currentPage = 1;
        this.totalPages = 0;
        this.scale = 1.5;
        this.cvUrl = null;
        this.isRendering = false;

        // Clear canvas
        if (this.canvas && this.ctx) {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        }

        // Reset UI - hide everything and show loading for next time
        this.hideControls();
        
        // Hide error message
        const error = document.getElementById('cvViewerError');
        if (error) error.classList.add('d-none');
        
        // Show loading for next open
        this.showLoading();

        // Reset title
        const titleElement = document.querySelector('#cvViewerModalLabel span');
        if (titleElement) {
            titleElement.textContent = 'CV Preview';
        }

        console.log('CV Viewer cleaned up');
    }
}

// Create global instance
// Use try-catch to handle any initialization errors
try {
    window.cvViewer = new CVViewer();
    console.log('CV Viewer instance created successfully');
} catch (error) {
    console.error('Failed to create CV Viewer instance:', error);
    // Retry after a delay
    setTimeout(() => {
        try {
            window.cvViewer = new CVViewer();
            console.log('CV Viewer instance created successfully (retry)');
        } catch (retryError) {
            console.error('Failed to create CV Viewer instance (retry):', retryError);
        }
    }, 1000);
}
