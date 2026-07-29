# Design Tokens cho LibraryManagement

## Cách dùng

- Dùng các giá trị dưới đây làm hệ thống thống nhất, không tạo giá trị gần giống tùy ý.
- Định nghĩa token dùng lại trong `ResourceDictionary` và tham chiếu bằng `StaticResource` hoặc `DynamicResource` phù hợp.
- Dùng `DynamicResource` cho màu hoặc resource cần thay đổi khi chuyển Light/Dark Theme.
- Không dùng màu hard-code nếu màu đó cần thay đổi theo theme.
- Ưu tiên semantic brushes có sẵn của WPF-UI trước khi tạo brush riêng.

## Spacing

Thang spacing chuẩn gồm: **4, 8, 12, 16, 24, 32, 48**.

| Token | Giá trị | Cách dùng |
|---|---:|---|
| `Spacing4` | 4 | Khoảng cách rất nhỏ, icon với badge |
| `Spacing8` | 8 | Thành phần nhỏ liên quan |
| `Spacing12` | 12 | Control liên quan |
| `Spacing16` | 16 | Field hoặc nhóm nội dung |
| `Spacing24` | 24 | Section và page padding |
| `Spacing32` | 32 | Phân tách vùng lớn |
| `Spacing48` | 48 | Khoảng nghỉ cấp trang |

Không tạo spacing 5, 10, 14, 18 hoặc giá trị tùy ý nếu token hiện có đáp ứng được.

## Corner radius

| Token | Giá trị | Cách dùng |
|---|---:|---|
| `CornerRadiusSmall` | 4 | Badge, phần tử nhỏ |
| `CornerRadiusMedium` | 8 | Input, button, card nhỏ |
| `CornerRadiusLarge` | 12 | Card, panel, empty state |

Giữ corner radius phù hợp template WPF-UI; không override nếu control đã có Fluent radius đúng.

## Control heights

| Token | Giá trị |
|---|---:|
| `ButtonHeight` | 36 |
| `InputHeightCompact` | 38 |
| `InputHeight` | 40 |
| `NavigationItemHeight` | 40 |
| `TitleBarHeight` | 48 |

- Dùng `MinHeight` thay cho `Height` khi nội dung có thể tăng do localization hoặc DPI.
- Không ép chiều cao nếu làm cắt text, validation hoặc focus visual.

## Page layout

| Token | Giá trị | Cách dùng |
|---|---:|---|
| `PagePadding` | 24 | Padding ngoài của Page |
| `SectionSpacing` | 24 | Khoảng cách giữa section |
| `FieldSpacing` | 16 | Khoảng cách giữa field |
| `RelatedControlSpacing` | 12 | Khoảng cách control liên quan |

- Nội dung chính phải co giãn; tránh fixed width cho toàn bộ page.
- Dùng `MaxWidth` cục bộ cho form dài dòng khi cần duy trì khả năng đọc.

## Typography

| Token | Vai trò | Gợi ý |
|---|---|---|
| `PageTitleTextStyle` | Tiêu đề trang | Lớn nhất trong page, SemiBold |
| `SectionTitleTextStyle` | Tiêu đề section/card | Nhỏ hơn Page Title, SemiBold |
| `BodyTextStyle` | Nội dung và label | Kích thước đọc mặc định |
| `CaptionTextStyle` | Mô tả phụ, metadata | Nhỏ hơn Body, semantic secondary foreground |

### Quy tắc typography

- Dùng style/resource thay vì lặp `FontSize` và `FontWeight`.
- Không dùng quá nhiều cấp chữ trong cùng một page.
- Body text phải đọc được ở DPI 150%.
- Caption dùng semantic secondary foreground, nhưng vẫn phải đủ tương phản.
- Không truyền đạt trạng thái chỉ bằng font weight hoặc màu.

## Mẫu ResourceDictionary

Khi repository bổ sung dictionary token, có thể dùng tên resource thống nhất sau:

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:sys="clr-namespace:System;assembly=System.Runtime">
    <sys:Double x:Key="Spacing4">4</sys:Double>
    <sys:Double x:Key="Spacing8">8</sys:Double>
    <sys:Double x:Key="Spacing12">12</sys:Double>
    <sys:Double x:Key="Spacing16">16</sys:Double>
    <sys:Double x:Key="Spacing24">24</sys:Double>
    <sys:Double x:Key="Spacing32">32</sys:Double>
    <sys:Double x:Key="Spacing48">48</sys:Double>

    <CornerRadius x:Key="CornerRadiusSmall">4</CornerRadius>
    <CornerRadius x:Key="CornerRadiusMedium">8</CornerRadius>
    <CornerRadius x:Key="CornerRadiusLarge">12</CornerRadius>

    <sys:Double x:Key="ButtonHeight">36</sys:Double>
    <sys:Double x:Key="InputHeightCompact">38</sys:Double>
    <sys:Double x:Key="InputHeight">40</sys:Double>
    <sys:Double x:Key="NavigationItemHeight">40</sys:Double>
    <sys:Double x:Key="TitleBarHeight">48</sys:Double>

    <Thickness x:Key="PagePadding">24</Thickness>
    <sys:Double x:Key="SectionSpacing">24</sys:Double>
    <sys:Double x:Key="FieldSpacing">16</sys:Double>
    <sys:Double x:Key="RelatedControlSpacing">12</sys:Double>
</ResourceDictionary>
```

Mẫu chỉ quy định tên và giá trị token. Không tự động thêm dictionary vào ứng dụng nếu task không yêu cầu.
