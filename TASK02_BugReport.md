# TASK-02 — Bug Report kiểm thử độc lập

## Phạm vi

Đã đánh giá lỗi quan sát được trong phạm vi FN-02-01 đến FN-02-06 trên ứng dụng và bản sao SQLite cô lập. Không thực hiện thay đổi dữ liệu trên database đang dùng. Một lỗi cập nhật sai dự án trong FN-02-04 được sửa và xác minh lại.

## Lỗi được xác nhận

| Bug ID | Mức độ | Mô tả | Nguyên nhân gốc | Thay đổi | Xác minh |
|---|---|---|---|---|---|
| QA-02-001 | Medium | Sau khi thêm test project nằm dưới thư mục ứng dụng, build web project lỗi MSB3030 khi cố sao chép `tests/.../obj/...sourcelink.json` vào đường dẫn test lồng nhau. | Default item glob của Web SDK tiếp nhận artifact `tests/bin` và `tests/obj` như nội dung của ứng dụng. Chỉ loại file C# test là chưa đủ. | `ConstructionProjectManagement.csproj`: loại `tests/**` khỏi default items. | Chạy lại xUnit: 8/8 PASS; chạy lại `dotnet build`: 0 lỗi. |
| QA-02-002 | Medium | POST tới `/Projects/Edit/7` với body `Id=4` đã chuyển tới Details/4 và thay đổi Project 4 thay vì từ chối request không khớp route/body. | Tham số `id` không khai báo binding source nên ASP.NET Core nhận giá trị `Id` từ form; phép so sánh `id != model.Id` so sánh hai giá trị body giống nhau thay vì đối chiếu route với body. POST cho ID không tồn tại cũng trả form 200 thay vì 404. | `ProjectsController.Edit` POST: ràng buộc `id` bằng `[FromRoute]` và kiểm tra project chưa archive tồn tại trước khi cập nhật. | Sau sửa, route ID 7/body ID 4 trả 400; ID 999999 trả 404. Chạy lại 8 unit test và build đều thành công. |

## Lỗi nghiệp vụ ứng dụng khác

Ngoài QA-02-002 đã được sửa và xác minh lại, không xác nhận thêm lỗi sản phẩm trong các ca đã thực thi. Các thao tác validation, tạo/cập nhật dự án, đồng bộ PM, thêm/kết thúc/tái phân công role, giới hạn Site Manager và bảo toàn lịch sử cho kết quả như mong đợi trên DB cô lập.

Một số lỗi ban đầu trong lúc chạy thử được xác định là lỗi của harness/giả định kiểm thử, không phải lỗi ứng dụng:

- Helper PowerShell ban đầu đếm tập kết quả rỗng không chính xác; đã sửa phép kiểm tra.
- Một số request bị từ chối được chuyển hướng đến trang Identity AccessDenied và trang đó trả HTTP 200. Xác nhận theo URL/nội dung đích thay vì kỳ vọng status 403 trực tiếp.
- Bộ kiểm tra C-03 đầu tiên tìm thông báo lỗi trên HTML chưa decode nên cho kết quả âm tính giả. Đọc nội dung đã HTML-decode xác nhận thông báo mã dự án trùng xuất hiện, DB không đổi; C-03 là PASS.

## Vấn đề còn theo dõi

- Build phát cảnh báo advisory mức High cho `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 và `System.Security.Cryptography.Pkcs` 9.0.0, cùng một cảnh báo Moderate cho package thứ hai. Không nâng phiên bản vì việc cập nhật dependency không thuộc phạm vi QA TASK-02.
- Chưa có bằng chứng về lỗi Critical/High trong logic TASK-02; các cảnh báo dependency là rủi ro riêng cần được xử lý/đánh giá trong công việc dependency-security.
- QA-02-002 đã được xác nhận Medium, sửa an toàn trong phạm vi TASK-02 và retest PASS; không còn lỗi đã xác nhận đang mở trong các ca đã chạy.
- Luồng phê duyệt ngoại lệ Site Manager đa dự án được đánh dấu `NOT IMPLEMENTED`, đúng với phạm vi hiện tại; hệ thống chặn phân công dự án thứ hai khi assignment cũ còn hiệu lực.
- Tại thời điểm hoàn tất lượt này, ma trận đã không còn ca `NOT TESTED`; các ca ngoài phạm vi tiếp tục được đánh dấu `NOT IMPLEMENTED` trong [TASK02_Independent_TestCases.md](./TASK02_Independent_TestCases.md).
