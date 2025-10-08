/**
 * SystemConfig JavaScript - Optimized Semester CRUD Operations
 * Reduced from ~1000 lines to ~200 lines with better maintainability
 */

class SemesterManager {
    constructor() {
        this.isEditMode = false;
        this.modalElement = document.getElementById('semesterModal');
        this.elements = this.getElements();
        this.init();
    }

    init() {
        this.bindEvents();
        console.log('SemesterManager initialized');
    }

    getElements() {
        return {
            modalLabel: document.getElementById('semesterModalLabel'),
            modalSubtitle: document.querySelector('.modal-subtitle'),
            modalIcon: document.querySelector('#semesterModal .modal-icon i') || document.querySelector('.modal-icon i'),
            semesterId: document.getElementById('semesterId'),
            semesterName: document.getElementById('semesterName'),
            semesterStartDate: document.getElementById('semesterStartDate'),
            semesterEndDate: document.getElementById('semesterEndDate'),
            saveBtn: document.querySelector('#semesterModal .btn-primary')
        };
    }

    bindEvents() {
        // Bind all buttons with data attributes instead of onclick
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-action="add-semester"]')) {
                this.openModal('add');
            } else if (e.target.matches('[data-action="edit-semester"]')) {
                const row = e.target.closest('tr');
                const data = this.extractRowData(row);
                this.openModal('edit', data);
            } else if (e.target.matches('[data-action="delete-semester"]')) {
                const id = e.target.dataset.id;
                this.deleteSemester(id);
            } else if (e.target.matches('[data-action="save-semester"]')) {
                this.saveSemester();
            }
        });
    }

    extractRowData(row) {
        const name = row.querySelector('.semester-name').textContent;
        const dates = row.querySelectorAll('.date-value');
        return {
            id: row.dataset.id,
            name: name,
            startDate: dates[0].textContent.split('/').reverse().join('-'),
            endDate: dates[1].textContent.split('/').reverse().join('-')
        };
    }

    async openModal(mode, data = null) {
        try {
            this.isEditMode = mode === 'edit';
            
            // Set modal content
            this.setModalContent(mode, data);
            
            // Show modal
            this.showModal();
            lucide.createIcons();

        } catch (error) {
            console.error(`Error opening ${mode} modal:`, error);
            notificationManager.error('Lỗi', `Có lỗi khi mở modal ${mode === 'add' ? 'thêm' : 'chỉnh sửa'} học kỳ`, 5000);
        }
    }

    setModalContent(mode, data) {
        const { modalLabel, modalSubtitle, modalIcon, semesterId, semesterName, semesterStartDate, semesterEndDate } = this.elements;

        const config = mode === 'add' ? {
            title: 'Thêm học kỳ',
            subtitle: 'Tạo học kỳ mới trong hệ thống',
            icon: 'calendar-plus',
            values: { id: '0', name: '', startDate: '', endDate: '' }
        } : {
            title: 'Chỉnh sửa học kỳ',
            subtitle: 'Cập nhật thông tin học kỳ',
            icon: 'edit-3',
            values: { id: data.id, name: data.name, startDate: data.startDate, endDate: data.endDate }
        };

        modalLabel.textContent = config.title;
        modalSubtitle.textContent = config.subtitle;
        if (modalIcon) modalIcon.setAttribute('data-lucide', config.icon);
        
        semesterId.value = config.values.id;
        semesterName.value = config.values.name;
        semesterStartDate.value = config.values.startDate;
        semesterEndDate.value = config.values.endDate;
    }

    showModal() {
        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            new bootstrap.Modal(this.modalElement).show();
        } else {
            this.modalElement.style.display = 'block';
            this.modalElement.classList.add('show');
            document.body.classList.add('modal-open');
        }
    }

    async saveSemester(force = false) {
        try {
            const data = this.getFormData();
            if (!this.validateFormData(data)) return;

            this.setLoadingState(true);

            const requestData = {
                name: data.name,
                startDate: data.startDate,
                endDate: data.endDate,
                force: force
            };

            if (this.isEditMode) {
                requestData.id = data.id;
            }

            const endpoint = this.isEditMode ? '/SystemConfig?handler=UpdateSemester' : '/SystemConfig?handler=CreateSemester';
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();
            this.handleSaveResponse(result);

        } catch (error) {
            console.error('Save error:', error);
            notificationManager.error('Lỗi kết nối', 'Đã xảy ra lỗi khi kết nối đến server', 6000);
        } finally {
            this.setLoadingState(false);
        }
    }

    getFormData() {
        const { semesterId, semesterName, semesterStartDate, semesterEndDate } = this.elements;
        return {
            id: parseInt(semesterId.value),
            name: semesterName.value.trim(),
            startDate: semesterStartDate.value,
            endDate: semesterEndDate.value
        };
    }

    validateFormData(data) {
        if (!data.name || !data.startDate || !data.endDate) {
            notificationManager.warning('Thiếu thông tin', 'Vui lòng điền đầy đủ thông tin học kỳ', 4000);
            return false;
        }
        return true;
    }

    setLoadingState(loading) {
        const { saveBtn } = this.elements;
        if (!saveBtn) return;

        saveBtn.disabled = loading;
        saveBtn.innerHTML = loading 
            ? '<i data-lucide="loader-2" class="spin-animation" style="width: 16px; height: 16px;"></i> Đang lưu...'
            : '<i data-lucide="save" style="width: 16px; height: 16px;"></i> <span class="btn-text">Lưu học kỳ</span>';
        
        lucide.createIcons();
    }

    handleSaveResponse(result) {
        console.log('Save response:', result);
        
        if (result.success) {
            notificationManager.success('Thành công!', result.message, 4000);
            this.closeModal();
            setTimeout(() => location.reload(), 1500);
        } else if (result.isWarning && !result.force) {
            console.log('Showing warning confirmation...');
            notificationManager.confirm(
                'Cảnh báo',
                result.message + '\n\nBạn có chắc chắn muốn tiếp tục?',
                () => {
                    console.log('User confirmed, saving with force...');
                    this.saveSemester(true);
                },
                () => {
                    console.log('User cancelled');
                }
            );
        } else {
            notificationManager.error('Lỗi!', result.message, 6000);
        }
    }

    closeModal() {
        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            const modal = bootstrap.Modal.getInstance(this.modalElement);
            if (modal) modal.hide();
        } else {
            this.modalElement.style.display = 'none';
            this.modalElement.classList.remove('show');
            document.body.classList.remove('modal-open');
        }
    }

    async deleteSemester(id) {
        console.log('Delete semester:', id);
        notificationManager.confirm(
            'Xác nhận xóa',
            'Bạn có chắc chắn muốn xóa học kỳ này? Hành động này không thể hoàn tác.',
            async () => {
                console.log('User confirmed deletion');
                try {
                    const response = await fetch(`/SystemConfig?handler=DeleteSemester&id=${id}`, {
                        method: 'POST',
                        headers: {
                            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                        }
                    });
                    
                    const result = await response.json();
                    console.log('Delete response:', result);
                    
                    if (result.success) {
                        notificationManager.success('Đã xóa!', result.message, 4000);
                        setTimeout(() => location.reload(), 1500);
                    } else {
                        notificationManager.error('Lỗi!', result.message || 'Đã xảy ra lỗi', 6000);
                    }
                } catch (error) {
                    console.error('Delete error:', error);
                    notificationManager.error('Lỗi kết nối', 'Đã xảy ra lỗi khi kết nối đến server', 6000);
                }
            },
            () => {
                console.log('User cancelled deletion');
            }
        );
    }
}

// Tab switching function
function switchTab(tabName) {
    document.querySelectorAll('.tab-content-panel').forEach(panel => panel.style.display = 'none');
    document.querySelectorAll('.nav-tabs .nav-link').forEach(link => {
        link.classList.remove('active');
        link.style.color = 'var(--muted-foreground)';
        link.style.borderBottom = 'none';
    });
    
    document.getElementById('content-' + tabName).style.display = 'block';
    const activeTab = document.getElementById('tab-' + tabName);
    activeTab.classList.add('active');
    activeTab.style.color = 'var(--foreground)';
    activeTab.style.borderBottom = '2px solid var(--primary)';
    
    lucide.createIcons();
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.semesterManager = new SemesterManager();
    lucide.createIcons();
    
    if (typeof bootstrap === 'undefined') {
        console.error('Bootstrap is not loaded!');
        return;
    }
    
    console.log('SystemConfig page loaded successfully');
    
    // Test confirm modal (for debugging)
    window.testConfirm = function() {
        console.log('Testing confirm modal...');
        notificationManager.confirm(
            'Test Confirmation',
            'Đây là test modal confirmation. Bạn có muốn tiếp tục?',
            () => {
                console.log('User confirmed test');
                notificationManager.success('Test', 'Bạn đã xác nhận!', 3000);
            },
            () => {
                console.log('User cancelled test');
                notificationManager.info('Test', 'Bạn đã hủy!', 3000);
            }
        );
    };
});

// Legacy function compatibility (for onclick attributes)
function openAddSemesterModal() {
    window.semesterManager?.openModal('add');
}

function openEditSemesterModal(id, name, startDate, endDate) {
    window.semesterManager?.openModal('edit', { id, name, startDate, endDate });
}

function deleteSemester(id) {
    window.semesterManager?.deleteSemester(id);
}

function saveSemester() {
    window.semesterManager?.saveSemester();
}
