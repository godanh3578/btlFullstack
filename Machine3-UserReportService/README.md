# Machine 3 - User & Report Service

Service này dành cho máy 3 trong mô hình:

- Máy 3 giữ nghiệp vụ User & Report.
- Máy 4 chỉ là Vue frontend, gọi API qua HTTP.
- Tài khoản Admin User chỉ nằm ở service này, không hardcode trong Vue.

## Chạy service

```powershell
dotnet run
```

Mặc định service chạy tại:

```text
http://localhost:8083
```

Nếu máy 4 chạy ở máy khác, đổi `VITE_USER_API_URL` bên máy 4 thành:

```env
VITE_USER_API_URL=http://localhost:8083/api
```

## Tài khoản Admin User mặc định

Service tự seed tài khoản Admin User trong `appsettings.json`:

```text
Email: admin.user@khopro.local
Password: AdminUser@123
Role: admin-user
```

Đổi mật khẩu và `Jwt:Secret` trước khi dùng chung trong nhóm.

## API cho máy 4

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/reset-password`
- `GET /api/users/me`
- `PUT /api/users/me`
- `GET /api/users` chỉ cho role `admin-user`
- `GET /api/reports/summary`

Frontend gửi token theo header:

```text
Authorization: Bearer <JWT_TOKEN>
```

## Lưu dữ liệu

User được lưu trong:

```text
Data/users.json
```

File này chỉ nằm ở máy 3. Máy 4 không lưu password hoặc danh sách user.
