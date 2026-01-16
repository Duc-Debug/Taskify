# 🚀 Taskify - AI-Powered Project Management

**Taskify** là giải pháp quản lý dự án thông minh được xây dựng trên nền tảng **ASP.NET Core 9.0**. Không chỉ là một bảng Kanban thông thường, Taskify tích hợp **Google Gemini AI** để tự động lập kế hoạch dự án và phân công nhiệm vụ dựa trên kỹ năng thực tế của từng thành viên trong team.

![Taskify Banner]

## ✨ Tính Năng Nổi Bật

### 🤖 1. AI Project Planner (Powered by Gemini 2.5)
* **Tự động tạo kế hoạch:** Chỉ cần nhập mô tả dự án (ví dụ: "Làm website bán hàng"), AI sẽ tự động tạo các cột (Lists) và công việc (Tasks) chi tiết.
* **Phân công thông minh:** Hệ thống tự động phân tích kỹ năng (Skills) của nhân viên để giao việc cho người phù hợp nhất.
* **Dự đoán độ thành công:** AI chấm điểm "Success Confidence" cho từng task dựa trên độ phù hợp của nhân viên được giao.

### 📋 2. Quản lý Tác vụ (Task Management)
* Giao diện Kanban trực quan (Kéo & Thả).
* Quản lý Deadline, Độ ưu tiên (Priority).
* Hỗ trợ làm việc nhóm (Team Collaboration).

### 🔐 3. Hệ thống & Bảo mật
* Đăng nhập/Đăng ký qua **Google OAuth**.
* Phân quyền người dùng.
* Thông báo qua Email (SMTP Gmail).

### 🔔 4. Hệ thống Thông báo & Nhắc nhở (Automated Reminders)
* **Deadline Reminder:** Hệ thống có background service chạy ngầm, tự động quét và gửi email nhắc nhở khi task sắp đến hạn (trước 24h).
* **Real-time Notifications:** Thông báo ngay lập tức khi được assign vào task mới hoặc khi có thay đổi trong dự án.
---

## 🛠 Công Nghệ Sử Dụng (Tech Stack)

* **Backend:** ASP.NET Core 9.0 (MVC)
* **Database:** SQLite / Entity Framework Core 9.0
* **AI Model:** Google Gemini 2.5 Flash-Lite
* **Frontend:** Bootstrap 5, jQuery, Razor Views
* **Authentication:** ASP.NET Core Identity + Google Auth

---

## 🚀 Hướng Dẫn Cài Đặt (Getting Started)

Làm theo các bước sau để chạy dự án trên máy cục bộ.

### 1. Yêu cầu hệ thống
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* Visual Studio 2022 (hoặc VS Code)

### 2. Clone dự án
```bash
git clone [https://github.com/your-username/Taskify.git](https://github.com/your-username/Taskify.git)
cd Taskify
```

### 🔐 Cấu hình Bảo Mật (User Secrets)
**QUAN TRỌNG:** Không lưu trực tiếp API Key hay Mật khẩu trong file `appsettings.json` khi commit lên GitHub để tránh bị lộ thông tin. Hãy sử dụng **Secret Manager** của .NET.

Tại thư mục gốc của dự án (nơi chứa file `.csproj`), chạy các lệnh sau:

1. **Khởi tạo User Secrets (nếu chưa có):**
   ```bash
   dotnet user-secrets init
   ```
   # Cấu hình Google Login
   ```bash
   dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
   
# Cấu hình Gemini AI
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
   ```
# Cấu hình Email gửi thông báo
```bash
dotnet user-secrets set "EmailSettings:Password" "YOUR_EMAIL_APP_PASSWORD"
```
# check xem lại secert 
```bash
dotnet user-secrets list
