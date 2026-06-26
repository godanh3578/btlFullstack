# KhoPro Web

Frontend tinh gọn cho 2 backend local/LAN:

- Machine3 User/Report: `http://localhost:8083/api`
- Machine1 Product/Inventory: `http://localhost:5001/api`

## Chạy local

Mo 3 terminal:

```powershell
cd D:\fullStack\btlfullstack\Machine3-UserReportService
dotnet run --urls http://localhost:8083
```

```powershell
cd D:\fullStack\btlfullstack\Machine1-Product-Inventory-Service
dotnet run --urls http://localhost:5001
```

```powershell
cd D:\fullStack\btlfullstack\Machine4-KhoPro-Web
python -m http.server 5173
```

Sau đó mở:

```text
http://localhost:5173
```

## Cho máy khác truy cập

Chạy 3 service bằng địa chỉ `0.0.0.0`:

```powershell
cd D:\fullStack\btlfullstack\Machine3-UserReportService
dotnet run --urls http://0.0.0.0:8083
```

```powershell
cd D:\fullStack\btlfullstack\Machine1-Product-Inventory-Service
dotnet run --urls http://0.0.0.0:5001
```

```powershell
cd D:\fullStack\btlfullstack\Machine4-KhoPro-Web
python -m http.server 5173 --bind 0.0.0.0
```

Máy khác cùng mạng mở:

```text
http://IP_MAY_CHU:5173
```

Ví dụ nếu máy chủ là `192.168.1.20` thì mở `http://192.168.1.20:5173`.

Nếu máy khác không vào được, chạy các file mở firewall bằng quyền Administrator:

```text
Machine1-Product-Inventory-Service\open-firewall-5001-admin.bat
Machine3-UserReportService\open-firewall-8083-admin.bat
Machine4-KhoPro-Web\open-firewall-5173-admin.bat
```

Tài khoản mặc định:

```text
Email: admin.user@khopro.local
Password: AdminUser@123
```

## Phân quyền đăng nhập

Ở màn hình đăng nhập có 2 chế độ:

- `Người dùng`: quản lý sản phẩm, danh mục, nhà cung cấp, nhập xuất kho và hồ sơ cá nhân.
- `Quản trị hệ thống`: cần tài khoản có role `admin-user`; có thêm menu Người dùng để đổi quyền `user/admin-user` và khóa/mở tài khoản.
