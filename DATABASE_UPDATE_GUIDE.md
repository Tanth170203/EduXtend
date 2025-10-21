# 🚀 Hướng dẫn cập nhật Database cho Dev khác

## 📋 **Tóm tắt thay đổi:**
- **Xóa unique constraint** để cho phép cộng dồn nhiều lần cho cùng tiêu chí
- **Tạo non-unique index** để duy trì performance
- **Migration:** `20251021072815_RemoveUniqueConstraintForAccumulation`

## 🔧 **Các bước thực hiện:**

### **Bước 1: Pull code mới nhất**
```bash
git pull origin main
```

### **Bước 2: Cập nhật database**
```bash
# Chạy migration để cập nhật database
dotnet ef database update --project DataAccess
```

### **Bước 3: Verify kết quả**
```sql
-- Kiểm tra constraint đã bị xóa
SELECT 
    i.name AS IndexName,
    i.is_unique AS IsUnique
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('MovementRecordDetails')
AND i.name LIKE '%MovementRecordId_CriterionId%';
```

**Kết quả mong đợi:**
- ❌ `IX_MovementRecordDetails_MovementRecordId_CriterionId` (unique) - ĐÃ BỊ XÓA
- ✅ `IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique` (non-unique) - ĐÃ TẠO

## 🎯 **Tác động của thay đổi:**

### **Trước khi cập nhật:**
- ❌ Không thể cộng dồn nhiều lần cho cùng tiêu chí
- ❌ Lỗi: `Cannot insert duplicate key row`
- ❌ Ví dụ: Olympic 10 điểm/lần → chỉ cộng được 1 lần

### **Sau khi cập nhật:**
- ✅ Có thể cộng dồn nhiều lần cho cùng tiêu chí
- ✅ Ví dụ: Olympic 10 điểm/lần → cộng được 2 lần = 20 điểm
- ✅ Tự động điều chỉnh về điểm trần nếu vượt quá

## 🚨 **Lưu ý quan trọng:**

### **1. Backup database trước khi chạy migration**
```sql
-- Backup trước khi chạy migration
BACKUP DATABASE EduXtend TO DISK = 'C:\Backup\EduXtend_Before_Migration.bak';
```

### **2. Test sau khi cập nhật**
- Thử cộng điểm nhiều lần cho cùng tiêu chí
- Kiểm tra logic tự động điều chỉnh
- Verify performance không bị ảnh hưởng

### **3. Rollback nếu cần**
```bash
# Rollback migration nếu có vấn đề
dotnet ef database update 20251016080722_UpdateStudentTable --project DataAccess
```

## 📊 **Ví dụ test:**

### **Test Case 1: Cộng dồn Olympic**
1. Chọn tiêu chí: "Tham gia kỳ thi Olympic, ACM/CPC, Robocon"
2. Cộng điểm: 10 điểm
3. Cộng điểm lần 2: 10 điểm
4. **Kết quả mong đợi:** 20 điểm (2 x 10)

### **Test Case 2: Tự động điều chỉnh**
1. Chọn tiêu chí: "Tuyên dương công khai trước lớp" (max 2 điểm)
2. Đã có: 1 điểm
3. Cộng thêm: 2 điểm
4. **Kết quả mong đợi:** 2 điểm (tự động điều chỉnh từ 3 về 2)

## ✅ **Checklist hoàn thành:**

- [ ] Pull code mới nhất
- [ ] Backup database
- [ ] Chạy migration
- [ ] Test cộng dồn
- [ ] Test tự động điều chỉnh
- [ ] Verify performance
- [ ] Báo cáo kết quả

## 🆘 **Troubleshooting:**

### **Lỗi: "Cannot insert duplicate key row"**
- **Nguyên nhân:** Migration chưa chạy hoặc chạy không thành công
- **Giải pháp:** Chạy lại migration hoặc kiểm tra database

### **Lỗi: "Index does not exist"**
- **Nguyên nhân:** Database không có constraint cũ
- **Giải pháp:** Bỏ qua lỗi này, migration sẽ tạo index mới

### **Performance chậm**
- **Nguyên nhân:** Thiếu index
- **Giải pháp:** Kiểm tra index `IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique` đã được tạo

---

**📞 Liên hệ:** Nếu gặp vấn đề, hãy liên hệ team lead hoặc tạo issue trên GitLab.
