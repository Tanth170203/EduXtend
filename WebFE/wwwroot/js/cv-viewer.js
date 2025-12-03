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

        this.cvUrl = cvUrl;
        this.currentPage = 1;
        this.scale = options.initialScale || 1.5;

        // Update modal title if provided
        if (options.title) {
            const titleElement = document.querySelector('#cvViewerModalLabel span');
            if (titleElement) {
                titleElement.textContent = options.title;
            }
        }

        // Show modal
        const bsModal = new bootstrap.Modal(this.modal);
        bsModal.show();

        // Show loading state
        this.showLoading();

        // Check if PDF.js is loaded
        if (typeof pdfjsLib === 'undefined') {
            this.showError('PDF viewer library not loaded. Please refresh the page.');
            console.error('PDF.js library not loaded');
            return;
        }

        try {
            // Load PDF
            const loadingTask = pdfjsLib.getDocument(cvUrl);
            this.pdfDoc = await loadingTask.promise;
            this.totalPages = this.pdfDoc.numPages;

            console.log(`PDF loaded: ${this.totalPages} pages`);

            // Render first page
            await this.renderPage(1);

            // Show controls
            this.showControls();

        } catch (error) {
            console.error('Error loading PDF:', error);
            this.showError('Failed to load CV. The file may be corrupted or unavailable.');
            
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

            // Update UI
            this.updatePageInfo();
            this.updateZoomInfo();
            this.hideLoading();

            console.log(`Rendered page ${pageNum} of ${this.totalPages}`);

        } catch (error) {
            console.error('Error rendering page:', error);
            this.showError('Failed to render PDF page.');
        } finally {
            this.isRendering = false;
        }
    }

    showLoading() {
        const loading = document.getElementById('cvViewerLoading');
        const error = document.getElementById('cvViewerError');
        const container = document.getElementById('cvCanvasContainer');
        
        if (loading) loading.style.display = 'block';
        if (error) error.classList.add('d-none');
        if (container) container.style.display = 'none';
    }

    hideLoading() {
        const loading = document.getElementById('cvViewerLoading');
        const container = document.getElementById('cvCanvasContainer');
        
        if (loading) loading.style.display = 'none';
        if (container) container.style.display = 'flex';
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined' && lucide.createIcons) {
            lucide.createIcons();
        }
    }

    showError(message) {
        const loading = document.getElementById('cvViewerLoading');
        const error = document.getElementById('cvViewerError');
        const errorMessage = document.getElementById('cvErrorMessage');
        const container = document.getElementById('cvCanvasContainer');
        
        if (loading) loading.style.display = 'none';
        if (container) container.style.display = 'none';
        if (error) error.classList.remove('d-none');
        if (errorMessage) errorMessage.textContent = message;
        
        // Hide controls on error
        this.hideControls();
        
        // Reinitialize icons
        if (typeof lucide !== 'undefined' && lucide.createIcons) {
            lucide.createIcons();
        }
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

        // Reset UI
        this.hideControls();
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
window.cvViewer = new CVViewer();
