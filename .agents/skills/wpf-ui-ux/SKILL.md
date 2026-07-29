---
name: wpf-ui-ux
description: Hướng dẫn thiết kế, sửa đổi và review UX/UI cho ứng dụng desktop LibraryManagement dùng .NET, WPF, XAML, MVVM và WPF-UI. Dùng skill này trước mọi task liên quan đến Window, Page, UserControl, XAML layout, ResourceDictionary, Style, ControlTemplate, theme, TitleBar, NavigationView/sidebar, page header, form, DataGrid, dialog, snackbar, loading, empty state, validation hoặc hành vi giao diện WPF.
---

# WPF UI/UX

## Tài liệu bắt buộc

Đọc trước khi chỉnh giao diện:

- [design-tokens.md](design-tokens.md) để dùng đúng spacing, kích thước, typography và semantic resources.
- [review-checklist.md](review-checklist.md) trước khi bàn giao.

## Nguyên tắc

- Chỉ áp dụng cho WPF desktop. Không áp dụng máy móc quy tắc React, HTML hoặc CSS.
- Tuân theo Fluent Design và API thực tế của WPF-UI đang cài.
- Ưu tiên `Grid`, `ResourceDictionary`, `Style`, `ControlTemplate` và `DynamicResource`.
- Hỗ trợ Light Theme, Dark Theme, resize cửa sổ và DPI 100%, 125%, 150%.
- Dùng đơn vị độc lập thiết bị của WPF; tránh tính toán pixel vật lý.
- Giữ đúng MVVM. Giữ nguyên binding, command, converter, validation và navigation ngoài phạm vi yêu cầu.
- Chỉ dùng code-behind cho hành vi cửa sổ hoặc tương tác WPF thực sự cần thiết.
- Không thay business logic trong task UI.
- Chỉ sửa một khu vực giao diện trong mỗi task.

## Quy trình

1. Đọc View, code-behind, ViewModel, resources và layout cha của khu vực cần sửa.
2. Xác định binding, command, navigation, validation và state phải giữ nguyên.
3. Kiểm tra phiên bản WPF-UI trong project và xác minh API bằng tài liệu/assembly đúng phiên bản.
4. Chọn thay đổi cục bộ nhỏ nhất đáp ứng yêu cầu.
5. Áp dụng token; dùng semantic `DynamicResource` cho màu theo theme.
6. Kiểm tra các state: default, hover, pressed, focus, disabled, loading và error.
7. Kiểm tra resize, keyboard, DPI và Light/Dark.
8. Chạy build và checklist.

## State bắt buộc

Với mọi control tương tác, đánh giá:

- Default: nội dung, phân cấp và affordance rõ ràng.
- Hover: phản hồi nhẹ, không đổi layout.
- Pressed: phân biệt được với hover.
- Focus: focus visual rõ cho bàn phím.
- Disabled: giảm nhấn mạnh nhưng vẫn đọc được.
- Loading: khóa thao tác lặp và giữ phản hồi trạng thái.
- Error: thông báo tiếng Việt, gần nơi phát sinh, không chỉ dựa vào màu.

## Hướng dẫn theo khu vực

### TitleBar

- Ưu tiên `Wpf.Ui.Controls.FluentWindow` và `ui:TitleBar`.
- Kiểm tra `ExtendsContentIntoTitleBar`; không để native title bar và custom title bar chồng nhau.
- Không dùng `WindowStyle=None` cùng xử lý minimize/maximize/close thủ công khi WPF-UI đã hỗ trợ.
- Đặt TitleBar ở row riêng phía trên nội dung; dùng token chiều cao TitleBar.
- Giữ resize hit-testing, drag, double-click maximize/restore, system menu và Close hover đỏ chuẩn Windows.
- Không đặt control tương tác vào vùng drag nếu chưa kiểm tra hit testing.

### Sidebar và NavigationView

- Giữ nguyên `TargetPageType`, navigation service, menu permission và selected state.
- Sidebar không được đè nội dung; content phải co giãn theo cửa sổ.
- Hỗ trợ pane thu gọn và tooltip cho mục chỉ còn icon.
- Giữ navigation item theo token chiều cao và vùng click đủ lớn.

### Page Header

- Dùng Page Title, mô tả ngắn và action chính theo thứ tự thị giác rõ ràng.
- Cho action xuống dòng hoặc thích ứng khi cửa sổ hẹp; không cố định toàn bộ header bằng width lớn.
- Dùng khoảng cách page/section từ token.

### Form

- Dùng `Grid` cho label/input thẳng hàng và `ScrollViewer` khi chiều cao có thể thiếu.
- Label rõ ràng, dấu bắt buộc nhất quán và validation gần field.
- Input dùng chiều cao token; field cách nhau theo `FieldSpacing`.
- Giữ tab order theo thứ tự đọc; không dùng placeholder thay cho label.
- Nhóm Save/Cancel rõ ràng; khóa double-submit khi loading.

### DataGrid

- Ưu tiên độ rộng co giãn, `MinWidth` hợp lý và cột quan trọng nhìn thấy trước.
- Không tải toàn bộ dữ liệu chỉ để phục vụ trình bày.
- Header, row, selected, hover, focus và empty state phải rõ ở cả hai theme.
- Tránh horizontal scrollbar nếu có thể; không cắt action quan trọng ở DPI cao.
- Giữ sorting, filtering, paging và command hiện có.

### Dialog

- Dùng dialog service/WPF-UI dialog hiện có; ViewModel không mở UI trực tiếp.
- Title và nội dung nêu rõ hậu quả; action chính và action hủy dễ phân biệt.
- Thao tác nguy hiểm phải xác nhận, focus mặc định an toàn và hỗ trợ Escape.
- Không đóng dialog khi validation thất bại hoặc operation còn loading.

### Snackbar

- Dùng cho phản hồi ngắn sau hành động, không thay validation field hoặc dialog xác nhận.
- Nội dung tiếng Việt, ngắn, nêu kết quả; severity phù hợp Success/Info/Warning/Error.
- Không spam nhiều snackbar đồng thời và không chứa dữ liệu nhạy cảm.

### Loading

- Dùng loading cục bộ cho khu vực nhỏ; overlay toàn trang chỉ khi toàn trang không thể thao tác.
- Disable command/control gây double-submit trong khi chạy.
- Hiển thị mô tả ngắn, không khóa UI thread và không dùng animation gây khó chịu.
- Luôn kết thúc loading trong cả success, validation failure, cancellation và exception.

### Empty State

- Phân biệt chưa có dữ liệu với không có kết quả lọc.
- Nêu nguyên nhân/ngữ cảnh và action tiếp theo nếu có.
- Empty state không che header, filter hoặc action tạo mới.
- Giữ trình bày gọn, không dùng hình minh họa quá lớn.

### Validation

- Hiển thị lỗi cạnh field liên quan, bằng tiếng Việt và có thể hiểu độc lập.
- Không chỉ dùng viền/màu đỏ; thêm text hoặc biểu tượng có accessible name.
- Không làm layout nhảy mạnh khi lỗi xuất hiện.
- Giữ validation ở cả ViewModel và Service; UI không thay thế business validation.
- Focus field lỗi đầu tiên khi phù hợp và giữ dữ liệu người dùng đã nhập.

## Xác minh

Chạy:

```powershell
dotnet build LibraryManagement.slnx --configuration Debug
```

Nếu chạy runtime, dùng process riêng tối đa 10 giây và luôn đóng process đã tạo. Hoàn tất toàn bộ [review-checklist.md](review-checklist.md) trước khi báo kết quả.
