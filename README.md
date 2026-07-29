# LibraryManagement

LibraryManagement là ứng dụng desktop Windows dành cho nhân viên thư viện và quản trị viên. Ứng dụng quản lý sách, bản sách, độc giả, mượn–trả, gia hạn, tiền phạt, nhân viên, cài đặt và nhật ký hoạt động. Độc giả là đối tượng được quản lý và không đăng nhập vào ứng dụng.

## Công nghệ

- .NET 10.0, C# và WPF/XAML
- MVVM với CommunityToolkit.Mvvm 8.4.2
- WPF-UI 4.3.0 theo phong cách Fluent Design
- Entity Framework Core 10.0.10 và SQLite
- Microsoft.Extensions.Hosting, Dependency Injection và Logging 10.0.10
- BCrypt.Net-Next 4.2.0
- xUnit v3 3.2.2, FluentAssertions 8.10.0 và Microsoft.NET.Test.Sdk 17.14.1

Dự án chỉ dùng WPF-UI; không dùng MaterialDesignInXamlToolkit.

## Cấu trúc solution

```text
LibraryManagement.slnx
├── src
│   ├── LibraryManagement.App
│   │   ├── Dialogs
│   │   ├── Logging
│   │   ├── Navigation
│   │   ├── Notifications
│   │   ├── Services
│   │   ├── Themes
│   │   ├── ViewModels
│   │   └── Views
│   ├── LibraryManagement.Core
│   │   ├── Constants
│   │   ├── DTOs
│   │   ├── Entities
│   │   ├── Enums
│   │   ├── Interfaces
│   │   ├── Models
│   │   ├── Security
│   │   └── Validation
│   └── LibraryManagement.Infrastructure
│       ├── Data
│       ├── Initialization
│       ├── Repositories
│       └── Services
└── tests
    └── LibraryManagement.Tests
```

Phụ thuộc project đi theo một chiều:

```text
App -> Core
App -> Infrastructure -> Core
Tests -> App, Infrastructure, Core
```

Core không phụ thuộc App hoặc Infrastructure. ViewModel không truy cập trực tiếp `DbContext`.

## Yêu cầu môi trường

- Windows 10/11
- Visual Studio 2022 phiên bản hỗ trợ .NET 10, workload **.NET desktop development**
- .NET 10 SDK

Kiểm tra SDK:

```powershell
dotnet --version
dotnet --list-sdks
```

## Cài đặt và chạy

Từ thư mục solution:

```powershell
dotnet tool restore
dotnet restore LibraryManagement.slnx
dotnet build LibraryManagement.slnx --configuration Debug
dotnet run --project src/LibraryManagement.App/LibraryManagement.App.csproj
```

Trong Visual Studio:

1. Mở `LibraryManagement.slnx`.
2. Chọn `LibraryManagement.App` làm Startup Project.
3. Chọn cấu hình Debug và nền tảng Any CPU.
4. Nhấn `F5` để chạy có debugger hoặc `Ctrl+F5` để chạy không có debugger.

Database được tạo và áp dụng migration tự động ở lần chạy đầu tiên.

## Tài khoản mẫu

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Administrator | `admin` | `Admin@123` |
| Librarian | `librarian1` | `Librarian@123` |
| Librarian | `librarian2` | `Librarian@123` |

Mật khẩu trong database được lưu dưới dạng BCrypt hash, không lưu plain text.

## Database, ảnh và log

Mặc định các file runtime nằm trong:

```text
%LOCALAPPDATA%\LibraryManagement\
├── LibraryManagement.db
├── BookCovers\
├── Logs\
│   └── library-yyyyMMdd.log
└── login-preferences.json
```

Có thể thay đổi đường dẫn trong `src/LibraryManagement.App/appsettings.json`. Không ghi mật khẩu hoặc `PasswordHash` vào log.

## Migration

Khôi phục local tool và xem danh sách migration:

```powershell
dotnet tool restore
dotnet ef migrations list --project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj --startup-project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj
```

Tạo migration mới sau khi thay đổi model:

```powershell
dotnet ef migrations add TenMigration --project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj --startup-project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj --output-dir Data/Migrations
```

Áp dụng migration thủ công:

```powershell
dotnet ef database update --project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj --startup-project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj
```

Kiểm tra model còn thay đổi chưa được migration:

```powershell
dotnet ef migrations has-pending-model-changes --project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj --startup-project src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj
```

## Sao lưu và phục hồi

Chức năng này chỉ dành cho Administrator:

1. Đăng nhập bằng tài khoản Administrator.
2. Mở **Cài đặt**.
3. Chọn **Sao lưu database** và chọn vị trí lưu file `.db`.
4. Để phục hồi, chọn **Phục hồi database**, chọn bản sao và xác nhận.
5. Đăng xuất hoặc khởi động lại ứng dụng sau khi phục hồi.

Trước khi phục hồi, ứng dụng tự tạo một file an toàn cạnh database hiện tại với tên:

```text
LibraryManagement.before-restore-yyyyMMdd-HHmmss-fff-xxxxxxxx.db
```

Ứng dụng kiểm tra tính toàn vẹn SQLite và các bảng bắt buộc trước khi phục hồi. Không sao chép file database đang chạy bằng File Explorer vì SQLite có thể đang ở chế độ WAL; hãy dùng chức năng trong ứng dụng.

## Quy tắc nghiệp vụ chính

- Chỉ độc giả đang hoạt động, còn hạn thẻ, không có sách quá hạn, không vượt mức phạt và giới hạn mượn mới được mượn.
- Chỉ bản sách `Available` được thêm vào phiếu mượn.
- Lập phiếu mượn, trả sách và thanh toán tiền phạt chạy trong transaction.
- Trả quá hạn tạo phạt theo `OverdueDays × OverdueFinePerDay`.
- Mất sách tạo phạt theo `Book.Price × LostBookFineMultiplier`.
- Hư hỏng tạo phạt theo `Book.Price × DamagedBookFineMultiplier`.
- Các giới hạn và hệ số đều lấy từ `SystemSetting`, không hard-code trong service.
- Không được trả một bản sách hai lần hoặc thanh toán vượt số tiền còn lại.
- Gia hạn chỉ áp dụng cho sách chưa quá hạn và chưa vượt số lần cho phép.
- Quyền được kiểm tra ở cả UI và service; Librarian không thể gọi nghiệp vụ Administrator.

## Chức năng đã hoàn thành

- Đăng nhập, ghi nhớ tài khoản, đăng xuất và phân quyền Administrator/Librarian
- Dashboard, thống kê theo tháng, top sách/thể loại và hoạt động gần đây
- CRUD thể loại, tác giả, nhà xuất bản
- Quản lý sách, bản sách, ảnh bìa, tìm kiếm, lọc và phân trang
- Quản lý độc giả, khóa/mở khóa, gia hạn thẻ, lịch sử mượn và tiền phạt
- Lập phiếu mượn nhiều bản sách, trả một/nhiều sách và gia hạn
- Phạt quá hạn, mất, hư hỏng; thanh toán toàn phần/một phần và miễn phạt
- Quản lý nhân viên, reset mật khẩu, đổi vai trò và cài đặt hệ thống
- Nhật ký hoạt động, Light/Dark theme, loading, empty state, dialog và snackbar
- Xử lý exception tập trung, log file hằng ngày, sao lưu và phục hồi SQLite
- Seed data và bộ unit/service/repository/business-rule test

## Chức năng chưa hoàn thành

- Bộ cài MSIX/MSI và ký số ứng dụng
- Xuất báo cáo ra Excel/PDF
- Bộ UI automation test chạy trên máy Windows thật

## Kiểm thử và phát hành

Chạy toàn bộ test:

```powershell
dotnet test LibraryManagement.slnx --configuration Debug
```

Build Release:

```powershell
dotnet build LibraryManagement.slnx --configuration Release
```

Publish bản framework-dependent cho Windows x64:

```powershell
dotnet publish src/LibraryManagement.App/LibraryManagement.App.csproj --configuration Release --runtime win-x64 --self-contained false --output artifacts/publish/win-x64
```

File chạy sau publish:

```text
artifacts\publish\win-x64\LibraryManagement.App.exe
```
