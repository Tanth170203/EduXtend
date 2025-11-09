# ✅ Income vs Expenses Chart - 12 Months with Year Filter

## 📊 **Changes Summary:**

Updated the chart to display **all 12 months** with a **year filter** dropdown.

---

## 🔧 **Backend Changes:**

### **1. Service Interface**
**File:** `Services/FinancialDashboard/IFinancialDashboardService.cs`

**Changed:**
```csharp
// Before:
Task<IncomeExpenseChartDto> GetIncomeExpenseChartAsync(int clubId, int? semesterId = null);

// After:
Task<IncomeExpenseChartDto> GetIncomeExpenseChartAsync(int clubId, int? year = null);
```

**Why:** Changed from semester-based to year-based filtering

---

### **2. Service Implementation**
**File:** `Services/FinancialDashboard/FinancialDashboardService.cs`

**Key Changes:**

#### **A. Year-Based Date Range:**
```csharp
// Before: Last 6 months
startDate = DateTime.UtcNow.AddMonths(-6);

// After: Full year (Jan 1 - Dec 31)
var targetYear = year ?? DateTime.UtcNow.Year;
var startDate = new DateTime(targetYear, 1, 1);
var endDate = new DateTime(targetYear, 12, 31);
```

#### **B. Generate All 12 Months:**
```csharp
var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
                         "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

for (int month = 1; month <= 12; month++)
{
    labels.Add($"{monthNames[month - 1]} {targetYear}");
    
    var monthIncome = transactions
        .Where(t => t.Month == month && t.Type == "Income")
        .Sum(t => t.Total);
    
    var monthExpense = transactions
        .Where(t => t.Month == month && t.Type == "Expense")
        .Sum(t => t.Total);
    
    incomeData.Add(monthIncome);
    expenseData.Add(monthExpense);
}
```

**Benefits:**
- ✅ Always shows 12 months (Jan-Dec)
- ✅ Empty months show as 0
- ✅ Easy to compare year-over-year

---

### **3. API Controller**
**File:** `WebAPI/Controllers/FinancialDashboardController.cs`

**Updated Endpoint:**
```csharp
// Before:
[HttpGet("club/{clubId}/chart")]
public async Task<IActionResult> GetIncomeExpenseChart(
    int clubId, 
    [FromQuery] int? semesterId = null)

// After:
[HttpGet("club/{clubId}/chart")]
public async Task<IActionResult> GetIncomeExpenseChart(
    int clubId, 
    [FromQuery] int? year = null)
```

**API Usage:**
```http
GET /api/FinancialDashboard/club/1/chart           # Current year (2025)
GET /api/FinancialDashboard/club/1/chart?year=2024 # Specific year
GET /api/FinancialDashboard/club/1/chart?year=2023 # Historical data
```

---

## 🎨 **Frontend Changes:**

### **1. UI - Year Filter Dropdown**
**File:** `WebFE/Pages/ClubManager/Financial/Dashboard.cshtml`

**Added Filter:**
```html
<div class="d-flex justify-content-between align-items-center mb-3">
    <div>
        <h5 class="card-title mb-1">Income vs Expenses</h5>
        <p class="text-muted small mb-0">Monthly comparison</p>
    </div>
    <div style="width: 120px;">
        <select class="form-select form-select-sm" id="chartYearFilter">
            <!-- Years populated by JavaScript -->
        </select>
    </div>
</div>
```

**Design:**
```
┌────────────────────────────────────────────┐
│ Income vs Expenses         [2025 ▼]       │
│ Monthly comparison                         │
├────────────────────────────────────────────┤
│                                            │
│         [Chart with 12 months]            │
│                                            │
└────────────────────────────────────────────┘
```

---

### **2. JavaScript - Populate Year Filter**

**Added Function:**
```javascript
function populateYearFilter() {
    const currentYear = new Date().getFullYear();
    const yearFilter = document.getElementById('chartYearFilter');
    
    // Generate years: +2 years forward, -5 years back
    for (let year = currentYear + 2; year >= currentYear - 5; year--) {
        const option = document.createElement('option');
        option.value = year;
        option.textContent = year;
        if (year === currentYear) {
            option.selected = true; // Select current year
        }
        yearFilter.appendChild(option);
    }
    
    // Listen for year change
    yearFilter.addEventListener('change', async (e) => {
        await loadIncomeExpenseChart(parseInt(e.target.value));
    });
}
```

**Year Range:**
- Current Year - 5: **2020**
- Current Year: **2025** (selected by default)
- Current Year + 2: **2027**

**Total:** 8 years available in dropdown

---

### **3. Updated API Call**

**Before:**
```javascript
async function loadIncomeExpenseChart() {
    const response = await fetch(
        `${API_BASE}/FinancialDashboard/club/${CLUB_ID}/chart`,
        { credentials: 'include' }
    );
}
```

**After:**
```javascript
async function loadIncomeExpenseChart(year = null) {
    const yearParam = year ? `?year=${year}` : '';
    const response = await fetch(
        `${API_BASE}/FinancialDashboard/club/${CLUB_ID}/chart${yearParam}`,
        { credentials: 'include' }
    );
}
```

**Usage:**
```javascript
await loadIncomeExpenseChart();      // Current year
await loadIncomeExpenseChart(2024);  // Specific year (from dropdown)
```

---

## 📊 **Chart Output:**

### **X-Axis Labels (12 months):**
```
Jan 2025, Feb 2025, Mar 2025, Apr 2025, May 2025, Jun 2025,
Jul 2025, Aug 2025, Sep 2025, Oct 2025, Nov 2025, Dec 2025
```

### **Data Series:**
- 🔵 **Income:** Blue bars for each month
- 🔴 **Expenses:** Red bars for each month

### **Example Data:**
```javascript
{
  "labels": ["Jan 2025", "Feb 2025", ..., "Dec 2025"],
  "incomeData": [5000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
  "expenseData": [0, 2000000, 500000, 0, 0, 0, 0, 0, 0, 0, 0, 0]
}
```

**Result:**
- Months with data show bars
- Months without data show 0 (no bar)

---

## 🧪 **Testing:**

### **1. Restart WebAPI:**
```bash
cd WebAPI
dotnet run
```

### **2. Navigate to Dashboard:**
```
URL: /ClubManager/Financial/Dashboard
```

### **3. Verify Display:**
- ✅ Chart shows 12 months (Jan-Dec)
- ✅ Year dropdown appears (top-right of chart)
- ✅ Current year is selected by default
- ✅ Data displays for months with transactions
- ✅ Empty months show 0 (no bar)

### **4. Test Year Filter:**
1. Click year dropdown
2. Select different year (e.g., 2024)
3. Chart should reload with data for that year
4. X-axis labels update (e.g., "Jan 2024", "Feb 2024")

### **5. Test Edge Cases:**

**A. Future Year (2027):**
- Should show 12 months with all 0 (no data yet)

**B. Past Year (2020):**
- Should show historical data if exists

**C. Current Year:**
- Should show YTD data (Jan to current month)
- Future months show 0

---

## 📊 **Expected Visual:**

```
Income vs Expenses                        [2025 ▼]
Monthly comparison

  5M ┤     🔵
     │
  4M ┤
     │
  3M ┤
     │
  2M ┤          🔴
     │
  1M ┤               🔴
     │
  0  ┼─────────────────────────────────────────
     Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec
```

**Legend:**
- 🔵 Blue = Income
- 🔴 Red = Expenses

---

## ✅ **Summary:**

| Feature | Before | After |
|---------|--------|-------|
| **Months Displayed** | 6 (last 6 months) | 12 (full year) |
| **Filter Type** | Semester | Year |
| **Filter UI** | None | Dropdown (top-right) |
| **Year Range** | N/A | -5 to +2 years |
| **Default Year** | N/A | Current year |
| **API Parameter** | `?semesterId=X` | `?year=YYYY` |
| **Empty Months** | Not shown | Show as 0 |

---

## 🚀 **Ready to Use:**

1. **Restart WebAPI**
2. **Refresh Dashboard** (Ctrl+F5)
3. **View 12-month chart**
4. **Test year filter**

**→ Chart now displays full year with year filter!** 📊🎉

