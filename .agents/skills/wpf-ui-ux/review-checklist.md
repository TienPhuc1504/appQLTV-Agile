# WPF UI/UX Review Checklist

Đánh dấu từng mục áp dụng trước khi bàn giao thay đổi giao diện.

## Phạm vi và kiến trúc

- [ ] Chỉ khu vực giao diện được yêu cầu đã bị thay đổi.
- [ ] Business logic, entity, repository, service và database không bị thay đổi ngoài yêu cầu.
- [ ] Binding, command, converter, navigation target, permission và DI hiện có được giữ nguyên.
- [ ] MVVM được giữ đúng; code-behind chỉ chứa hành vi WPF/cửa sổ thực sự cần thiết.
- [ ] API WPF-UI đã được xác minh với đúng phiên bản package đang cài.

## Window, TitleBar và layout

- [ ] Không chồng native TitleBar và custom TitleBar.
- [ ] Minimize, Maximize/Restore, Close, drag, double-click và resize hoạt động nếu task liên quan.
- [ ] Sidebar không đè nội dung.
- [ ] Không cắt nội dung khi resize cửa sổ.
- [ ] Không dùng fixed width cho toàn bộ nội dung chính.
- [ ] Không xuất hiện thanh cuộn không cần thiết.
- [ ] ScrollViewer cần thiết không chứa control làm mất keyboard navigation.

## DPI và theme

- [ ] Text không bị cắt ở DPI 100%, 125% và 150%.
- [ ] Icon, focus visual, border và vùng click không bị cắt ở DPI cao.
- [ ] Light Theme hiển thị đúng.
- [ ] Dark Theme hiển thị đúng.
- [ ] Không có màu hard-code đáng lẽ phải thay đổi theo theme.
- [ ] Text và trạng thái có độ tương phản phù hợp ở cả hai theme.

## Keyboard và accessibility

- [ ] Có keyboard focus rõ ràng.
- [ ] Tab order hợp lý theo thứ tự đọc.
- [ ] Control chỉ có icon có tooltip hoặc accessible name.
- [ ] Có thể kích hoạt action chính bằng bàn phím.
- [ ] Trạng thái/error không chỉ được truyền đạt bằng màu sắc.

## State và phản hồi

- [ ] Default state rõ ràng.
- [ ] Hover state không làm thay đổi layout.
- [ ] Pressed state phân biệt được với hover.
- [ ] Disabled state vẫn đọc được và không thể kích hoạt.
- [ ] Validation hiển thị gần trường lỗi.
- [ ] Validation dùng thông báo tiếng Việt rõ ràng.
- [ ] Loading chặn double-submit.
- [ ] Loading luôn kết thúc sau success, failure, cancellation và exception.
- [ ] Empty state rõ ràng và phân biệt với không có kết quả lọc.
- [ ] Snackbar phù hợp severity và không thay validation/dialog.
- [ ] Nút nguy hiểm có dialog xác nhận.
- [ ] Focus mặc định trong dialog là lựa chọn an toàn.

## DataGrid và form

- [ ] DataGrid giữ sorting, filtering, paging và command hiện có.
- [ ] Cột/action quan trọng không bị cắt khi cửa sổ hẹp hoặc DPI cao.
- [ ] Form có label rõ ràng; placeholder không thay thế label.
- [ ] Khoảng cách field/control tuân theo design token.
- [ ] Dữ liệu người dùng không bị mất khi validation thất bại.

## Xác minh kỹ thuật

- [ ] `dotnet build LibraryManagement.slnx --configuration Debug` thành công.
- [ ] Không có lỗi biên dịch hoặc nullable warning mới.
- [ ] Light/Dark và resize đã được kiểm tra thủ công hoặc giới hạn kiểm tra đã được báo rõ.
- [ ] Runtime smoke test, nếu có, kết thúc trong thời gian giới hạn.
- [ ] Không còn process WPF do bài kiểm tra chạy nền.
- [ ] Danh sách file thay đổi và kết quả kiểm tra đã được báo cáo.
