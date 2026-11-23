# EduHub Code Base
ASP.NET Core 8 Web API + React/TypeScript + PostgreSQL, theo mô hình Modular Monolith.

## Chạy
```bash
docker compose up --build
```
Frontend: http://localhost:5173 | API: http://localhost:8080 | Swagger: http://localhost:8080/swagger

Tài khoản demo:
- admin@eduhub.local / Admin@123
- lecturer@eduhub.local / Lecturer@123
- student@eduhub.local / Student@123

Đây là code base khởi đầu. Đăng nhập, phân quyền, khoa, lớp, tham gia lớp, bài tập, nộp bài, chấm điểm, thông báo và thống kê đã có nền tảng API. Module tài liệu/R2 và bài kiểm tra trực tuyến cần hoàn thiện tiếp trước khi vận hành thật. Đổi JWT secret và mật khẩu DB trước khi public.
