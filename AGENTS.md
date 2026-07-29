# Hướng dẫn làm việc trong LibraryManagement

## Phạm vi

Các quy tắc này áp dụng cho toàn bộ repository. Giữ nguyên kiến trúc .NET 10, WPF, XAML, MVVM, WPF-UI, Entity Framework Core và SQLite hiện có.

## Quy tắc bắt buộc cho task UX/UI

Trước mọi task liên quan đến giao diện, phải đọc đầy đủ:

1. `.agents/skills/wpf-ui-ux/SKILL.md`
2. Các tài liệu mà skill yêu cầu cho task đang thực hiện.

Task giao diện bao gồm mọi thay đổi tới View, XAML, ResourceDictionary, Style, ControlTemplate, theme, cửa sổ, navigation, dialog, snackbar, loading, validation hiển thị hoặc ViewModel phục vụ trình bày.

## Bảo vệ phạm vi và kiến trúc

- Khi chỉ được yêu cầu sửa UI, không thay đổi business logic, quy tắc nghiệp vụ, entity, repository, service hoặc database.
- Không xóa, đổi tên hoặc thay đổi binding, command, converter, validation, navigation target, DI registration hay permission nếu chưa được yêu cầu rõ ràng.
- Không redesign toàn bộ ứng dụng khi người dùng chỉ yêu cầu sửa một phần.
- Mỗi task chỉ sửa một khu vực giao diện. Nếu cần mở rộng sang khu vực khác, phải báo rõ lý do và xin chỉ dẫn.
- Không tạo lại toàn bộ View hoặc MainWindow khi có thể chỉnh sửa cục bộ.
- Giữ đúng MVVM. Không đưa truy vấn dữ liệu hoặc nghiệp vụ vào View/code-behind.
- Code-behind chỉ dành cho hành vi cửa sổ hoặc tương tác WPF thực sự không phù hợp với binding/command.

## WPF-UI

- Không tự phát minh control, property, event, enum, resource key hoặc API của WPF-UI.
- Trước khi dùng API WPF-UI, phải kiểm tra phiên bản package đang cài trong `.csproj` hoặc bằng `dotnet list ... package --no-restore`.
- Xác minh API bằng assembly/XML documentation của đúng phiên bản package hoặc bằng một build tối thiểu.
- Không kết hợp WPF-UI với MaterialDesignInXamlToolkit.
- Ưu tiên control và resource có sẵn của WPF-UI; không dựng lại hành vi native Windows nếu thư viện đã hỗ trợ.

## Kiểm tra sau thay đổi

- Sau mỗi thay đổi code/XAML, phải chạy:

  ```powershell
  dotnet build LibraryManagement.slnx --configuration Debug
  ```

- Sửa toàn bộ lỗi biên dịch và nullable warning do thay đổi gây ra.
- Khi cần runtime smoke test, chạy ứng dụng bằng process riêng, không chạy WPF ở foreground và không chờ vô thời hạn.
- Giới hạn smoke test mặc định tối đa 10 giây, đóng đúng process đã tạo trong `finally`, rồi xác nhận không còn `LibraryManagement.App.exe` do bài kiểm tra để lại.
- Không đóng process có sẵn của người dùng.
- Chạy checklist tại `.agents/skills/wpf-ui-ux/review-checklist.md` trước khi bàn giao.

## Bàn giao

- Nêu rõ khu vực đã sửa và các file đã thay đổi.
- Báo kết quả build và runtime smoke test nếu có.
- Ghi rõ mọi phần chưa thể kiểm tra trực tiếp như DPI hoặc Windows phiên bản khác.
