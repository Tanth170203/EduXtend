# 🔐 AUTHENTICATION - USER GUIDE

## User Guide for Login / Logout Features

---

## 1. Authentication System Overview

### 1.1 Login Method

EduXtend system uses **Google OAuth 2.0** for user authentication.

| Feature | Description |
|---------|-------------|
| **Method** | Google Sign-In |
| **Allowed Email** | Only `@fpt.edu.vn` emails |
| **Token** | JWT (JSON Web Token) |
| **Access Token Lifetime** | 30 minutes |
| **Refresh Token Lifetime** | 7 days |

### 1.2 System Roles

| Role | Description | Access Rights |
|------|-------------|---------------|
| **Admin** | System Administrator | Full system access |
| **ClubManager** | Club Manager | ClubManager + Club pages |
| **ClubMember** | Club Member | Club pages |
| **Student** | Student | Student + Club pages |

---

## 2. Login

### 2.1 Login with Google Account

**Step 1:** On the Home page, click on the **"Login"** button on the navigation bar.

**Step 2:** On the Login page, click on **"Continue with Google"** button.

**Step 3:** In the Google popup, select your **@fpt.edu.vn** email account.

**Step 4:** After successful authentication, you will be redirected to the appropriate dashboard based on your role:
- Admin → Admin Dashboard
- ClubManager → ClubManager Dashboard  
- Student/ClubMember → Home Page

*Figure 1. Login Page*

---

## 3. Logout

### 3.1 Logout from System

**Step 1:** On any page, click on your **avatar** or **username** in the top right corner.

**Step 2:** In the dropdown menu, click on **"Logout"**.

**Step 3:** After clicking "Logout", you will be redirected to the Home page with a success message "Successfully logged out".

*Figure 2. Logout Dropdown Menu*

---

## 4. Session Expiration

### 4.1 Handle Expired Session

**Step 1:** When your session expires (after 30 minutes of inactivity), the system will automatically redirect you to the Login page.

**Step 2:** A message will display: "Your session has expired. Please log in again."

**Step 3:** Click **"Continue with Google"** to log in again.

*Figure 3. Session Expired Message*

---

## 5. Access Denied

### 5.1 Handle Unauthorized Access

**Step 1:** If you try to access a page without permission, you will be redirected to the Access Denied page.

**Step 2:** The page displays: "You do not have permission to access this page."

**Step 3:** Click **"Return to Home"** to go back to the Home page, or contact Admin if you need access.

*Figure 4. Access Denied Page*

---

## 6. Role-Based Access

### 6.1 Access Permission by Role

| Page/Area | Admin | ClubManager | ClubMember | Student |
|-----------|:-----:|:-----------:|:----------:|:-------:|
| `/Admin/*` | ✅ | ❌ | ❌ | ❌ |
| `/ClubManager/*` | ✅ | ✅ | ❌ | ❌ |
| `/Clubs/*` | ✅ | ✅ | ✅ | ✅ |
| `/Student/*` | ✅ | ✅ | ✅ | ✅ |
| `/Profile` | ✅ | ✅ | ✅ | ✅ |

---

## 7. Common Errors

### 7.1 "Login failed. Please use @fpt.edu.vn email"

**Cause:** Using email that is not `@fpt.edu.vn` or email not registered in the system.

**Solution:** 
1. Ensure using `@fpt.edu.vn` email
2. Contact Admin to be added to the system

### 7.2 "Your email is not registered in the system"

**Cause:** Your `@fpt.edu.vn` email has not been imported into the system.

**Solution:**
1. Contact Admin
2. Provide: Email, Full name, Student ID
3. Wait for Admin to import and notify

### 7.3 "Cannot load Google Sign-In"

**Cause:** Unstable internet connection or browser blocking popups.

**Solution:**
1. Check internet connection
2. Disable popup blocker for the website
3. Try a different browser

---

## 8. FAQ

**Q1: Can I log in with a personal email?**

A: No. The system only accepts registered `@fpt.edu.vn` emails.

**Q2: Why am I automatically logged out?**

A: Access Token expires after 30 minutes of inactivity. This is a security measure.

**Q3: How do I know my role?**

A: After logging in, check the dropdown menu or Profile page to see your role.

**Q4: Can I log in on multiple devices?**

A: Yes. Each device will have its own session.

---

*Document updated: December 2024*
