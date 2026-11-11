# Quy trình làm việc với Git & GitHub (Git Workflow) - EduHub

Hệ thống EduHub áp dụng quy trình Git branching theo chuẩn **GitFlow tinh gọn (Simplified GitFlow)** kết hợp với chuẩn **Conventional Commits** nhằm đảm bảo tính toàn vẹn của mã nguồn khi làm việc nhóm.

---

## 1. Cấu trúc các nhánh chính (Core Branches)

1. **`main` (Production Branch):**
   - Đại diện cho mã nguồn phiên bản ổn định nhất đang chạy thực tế trên VPS.
   - Tuyệt đối **không push code trực tiếp** lên `main`.
   - Mọi thay đổi chỉ được merge từ `develop` hoặc `hotfix/*` thông qua Pull Request (PR) được duyệt.

2. **`develop` (Integration Branch):**
   - Là nhánh tích hợp chính của nhóm phát triển.
   - Các tính năng mới từ các thành viên sẽ được merge vào đây sau khi review.

---

## 2. Quy ước đặt tên nhánh phụ (Supporting Branches)

Khi bắt đầu làm một tính năng hoặc sửa lỗi, thành viên nhóm **tạo nhánh mới từ `develop`**:

| Loại nhánh | Tiền tố | Ví dụ | Mục đích |
| :--- | :--- | :--- | :--- |
| **Tính năng mới** | `feature/<tên-tính-năng>` | `feature/materials-r2-upload`<br>`feature/online-test` | Phát triển chức năng mới theo phân công |
| **Sửa lỗi thường** | `bugfix/<tên-lỗi>` | `bugfix/fix-token-expiration`<br>`bugfix/joincode-validation` | Khắc phục lỗi phát hiện trong quá trình dev |
| **Sửa lỗi khẩn cấp** | `hotfix/<tên-lỗi>` | `hotfix/cors-vps-error` | Tách trực tiếp từ `main` để vá lỗi production |

---

## 3. Quy chuẩn thông điệp Commit (Conventional Commits)

Định dạng commit chuẩn:
```text
<loại>: <tiêu đề ngắn gọn mô tả thay đổi>

[chi tiết nếu có]
```

### Các loại commit:
- `feat:` Thêm chức năng/endpoint mới (vd: `feat: add presigned url generator for R2`)
- `fix:` Sửa lỗi (vd: `fix: handle expired test submission correctly`)
- `refactor:` Tái cấu trúc mã nguồn mà không thay đổi chức năng (vd: `refactor: extract ApiResponse helper`)
- `docs:` Viết tài liệu, báo cáo, cập nhật README (vd: `docs: update API documentation and ERD`)
- `chore:` Cấu hình project, CI/CD, Docker (vd: `chore: setup github actions ci pipeline`)
- `test:` Thêm hoặc sửa test case (vd: `test: add unit tests for JoinCode generator`)

---

## 4. Quy trình thực hiện một Task (Vòng đời phát triển)

1. **Kéo mã nguồn mới nhất:**
   ```bash
   git checkout develop
   git pull origin develop
   ```
2. **Tạo nhánh tính năng:**
   ```bash
   git checkout -b feature/materials-r2-upload
   ```
3. **Phát triển và commit:**
   ```bash
   git add .
   git commit -m "feat: implement presigned upload url endpoint"
   ```
4. **Đẩy nhánh lên GitHub:**
   ```bash
   git push origin feature/materials-r2-upload
   ```
5. **Mở Pull Request (PR):**
   - Mở PR từ `feature/materials-r2-upload` vào `develop`.
   - Điền đầy đủ thông tin theo form template có sẵn.
   - CI Pipeline sẽ tự động chạy kiểm tra build backend, frontend và docker.
6. **Code Review & Merge:**
   - Trưởng nhóm hoặc thành viên khác kiểm tra và xác nhận merge vào `develop`.

