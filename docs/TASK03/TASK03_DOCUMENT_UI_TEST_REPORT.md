# Báo cáo kiểm thử UI Quản lý tài liệu TASK-03

Ngày kiểm thử: 21/11/2026

## 1. Tóm tắt kết quả

- **Build:** PASS — `dotnet build --no-restore -p:UseAppHost=false`; 0 lỗi.
- **Test:** PASS — 25/25 test, 0 thất bại, 0 bỏ qua. Đã build test project trước khi chạy `dotnet test --no-build -p:UseAppHost=false`.
- **Ứng dụng:** PASS — khởi động được trên `http://127.0.0.1:5188`; kiểm tra đăng nhập, danh sách và chi tiết tài liệu trên trình duyệt.
- **Phạm vi UI:** Đã đăng nhập và kiểm tra đủ 8 vai trò; Client Representative được kiểm tra thêm với cả tổ chức khớp và không khớp. Fake data chỉ được ghi vào SQLite và thư mục file riêng trong `%LOCALAPPDATA%\Temp`; không dùng hoặc thay đổi database hệ thống.
- Build có cảnh báo NuGet về các phiên bản package có advisory và cảnh báo các tham chiếu `Contract.ProjectId/Project` đã obsolete; không có lỗi build và các cảnh báo không thuộc phạm vi sửa của lượt kiểm thử này.

## 2. Ma trận kiểm thử giao diện theo vai trò

| Vai trò / tài khoản fake | Kết quả kiểm tra |
|---|---|
| Admin — `fake.task03.admin@example.local` | Xem dữ liệu toàn hệ thống, danh mục, thống kê, xuất file và quản lý Archive/Restore; không có quyền tạo/sửa/duyệt tài liệu nghiệp vụ. Truy cập trực tiếp chức năng tạo bị từ chối. |
| Project Director — `fake.task03.director@example.local` | Xem tài liệu trong phạm vi dự án và tài liệu dùng chung; thao tác phê duyệt cuối, Archive/Restore, thống kê và xuất. |
| Project Manager — `fake.task03.manager@example.local` | Xem trong phạm vi dự án; tạo tài liệu, cập nhật metadata, tải phiên bản mới và gửi duyệt. Không tự review/phê duyệt tài liệu do mình tạo. |
| Site Manager — `fake.task03.site@example.local` | Xem tài liệu theo phạm vi dự án; thao tác tạo/sửa giới hạn theo danh mục được phép và tài liệu do mình tạo. |
| Supervising Engineer — `fake.task03.engineer@example.local` | Xem tài liệu dự án đang hoạt động và tài liệu thiết kế; review cấp 1 theo quyền. Sau khi sửa lỗi, trang chi tiết tài liệu hoạt động mở được; tài liệu Archive ngoài quyền không được hiển thị. |
| Materials Staff — `fake.task03.materials@example.local` | Xem theo phạm vi dự án; tạo/sửa trong nhóm danh mục vật tư được phép và giới hạn tài liệu do mình tạo; không có quyền review/phê duyệt. |
| Worker — `fake.task03.worker@example.local` | Chỉ xem tài liệu đã duyệt thuộc Safety/Minutes/Other trong dự án active và tài liệu system-wide được chia sẻ; không có quyền thao tác nghiệp vụ. Kiểm tra thêm membership inactive. |
| Client Representative — `fake.task03.client.match@example.local` | Chỉ xem tài liệu đã duyệt của dự án active có `ClientName` khớp `OrganizationName`; chỉ đọc. |
| Client Representative — `fake.task03.client.mismatch@example.local` | Tổ chức không khớp/không có phạm vi phù hợp: không thấy tài liệu của dự án và không có quyền thao tác. |

Truy cập khi chưa đăng nhập được chuyển tới Login. Các yêu cầu trái quyền bị chuyển tới trang AccessDenied và không trả nội dung tài liệu; Identity hiển thị trang từ chối với HTTP cuối 200.

## 3. Kết quả kiểm thử chức năng

| Nhóm | Thao tác và kết quả thực tế | Trạng thái |
|---|---|---|
| Tải file và validation | Upload PDF hợp lệ và DOCX/OOXML hợp lệ thành công. PDF sai magic bytes và file 25 MiB + 1 byte bị từ chối với thông báo lỗi. | PASS |
| Tạo nhiều tài liệu | Chọn hai PDF trong một lần tạo; hệ thống báo tạo 2 tài liệu, sinh mã riêng và hai trang chi tiết độc lập (ID 9 và 10). | PASS |
| Metadata và version | Đổi tiêu đề không tăng version; upload file mới tạo phiên bản v2, giữ lại v1 và hiển thị lịch sử. | PASS |
| Khóa chỉnh sửa | PM nhận khóa 15 phút; PD thấy thông tin người giữ khóa và không thấy nút sửa, truy cập trực tiếp Edit bị từ chối. Đặt khóa hết hạn trong SQLite cô lập để kiểm tra PD lấy khóa mới; thao tác Hủy sửa trả về trang chi tiết và nhả khóa. | PASS |
| Duyệt 2 cấp | PM gửi duyệt → SE thông qua cấp 1 → PD phê duyệt cuối; trạng thái lần lượt là “Chờ kiểm tra cấp 1”, “Chờ phê duyệt”, “Đã phê duyệt”. Lịch sử approval và audit ghi đúng người, bước, thời điểm. | PASS |
| Từ chối và gửi lại | SE từ chối với lý do; lý do hiện trong chi tiết/lịch sử. PM gửi duyệt lại; trạng thái trở về chờ kiểm tra và lịch sử ghi “Gửi duyệt lại”. | PASS |
| Chống tự duyệt | Giao diện không hiển thị nút review/phê duyệt cho PM là người tạo. Unit test xác nhận service từ chối self-review và self-approval. | PASS |
| Archive / Restore | PD lưu trữ với lý do; tài liệu biến mất khỏi danh sách mặc định. Bật bộ lọc “Đã lưu trữ” thấy lại tài liệu; trang chi tiết mở thành công, hiển thị nhãn/lý do, versions, approval history và audit. Restore thành công, bản ghi trở lại danh sách hoạt động và audit ghi nhận “Khôi phục”. | PASS |
| Tìm kiếm và bộ lọc | Tìm “Multi upload A” trả đúng một kết quả, không trả “Multi upload B”; URL giữ giá trị keyword. Bộ lọc Archive được giữ qua query và phân trang tạo link kèm tham số lọc. | PASS |
| Xem trước / tải xuống / xuất | Preview PDF trả HTTP 200 `application/pdf`; tải file PDF trả HTTP 200, đúng MIME/attachment. Excel trả HTTP 200 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (7236 byte); PDF báo cáo trả HTTP 200 `application/pdf` (32251 byte). | PASS |
| UI tiếng Việt / lịch sử | Trạng thái thống kê và phiên bản hiển thị tiếng Việt/đúng dạng `v1`, `v2`; lịch sử thao tác chỉ đọc. Tài liệu Office active hiển thị hướng dẫn tải xuống thay vì preview trực tiếp. | PASS |
| Audit bất biến | Unit test kiểm tra audit/approval history không bị chỉnh sửa hoặc xóa qua service/context nghiệp vụ. | PASS |

## 4. Lỗi phát hiện và đã xử lý

1. **Trang chi tiết của vai trò chuyên môn bị AccessDenied dù tài liệu còn hoạt động.** `GetByIdAsync` mặc định nhận `includeArchived=true`, nhưng `VisibleQueryAsync` lại từ chối mọi vai trò không có quyền Archive. Đã đổi logic để vai trò không được xem Archive vẫn xem được tài liệu đang hoạt động, đồng thời luôn loại bản ghi Archive khỏi phạm vi của họ. Bổ sung `SpecialistsCanViewActiveDocumentsButNotArchivedDocuments`; test xác nhận.
2. **Lịch sử phê duyệt hiển thị mã hành động tiếng Anh.** Dùng bộ ánh xạ tên hành động hiện có để hiển thị nhãn tiếng Việt.
3. **Một số nhãn UI trước đó chưa đúng.** Sửa biểu thức nhãn version ở Index/Details, trạng thái thống kê và vị trí hướng dẫn preview Office; kiểm tra lại trên bản publish mới.
4. **Fixture DOCX thiếu thành phần OOXML bắt buộc.** Bổ sung `[Content_Types].xml` và content type để fixture hợp lệ.
5. **Lần thử tải file đầu tiên trả 404 do đổi content root của ứng dụng kiểm thử sau publish.** Database lưu tên file tương đối theo thư mục data của content root; đã đồng bộ file trong thư mục tạm và chạy lại, sau đó download trả 200. Đây là sai khác cấu hình của test harness, không phải lỗi lưu file trên database hệ thống.

## 5. Các điểm làm rõ nghiệp vụ

Không phát sinh điểm nghiệp vụ cần người dùng xác nhận. Ma trận vai trò, phạm vi dự án, thời hạn khóa, quy trình 2 cấp và quy tắc Archive/Restore được đối chiếu với đặc tả đã cung cấp.

## 6. Dọn dẹp fake data và ghi chú vận hành

- Toàn bộ tài khoản và dữ liệu thử nghiệm có tiền tố `[FAKE-TASK03]` hoặc email `fake.task03.*`; chúng chỉ tồn tại trong database SQLite cô lập dưới `%LOCALAPPDATA%\Temp\cpms-task03-ui-20261121-a82b5914\ui.db`.
- Ứng dụng kiểm thử đã được dừng và thư mục `%LOCALAPPDATA%\Temp\cpms-task03-ui-20261121-a82b5914` đã được xóa sau khi hoàn tất; database/file fake đã được dọn. Không xóa database hoặc thư mục `App_Data` của môi trường thật.
- Không bật `CPMS_SEED_TASK03_TEST_DATA=true` trên môi trường thật. Nếu dữ liệu fake từng được seed vào một database khác, chỉ dọn các tài khoản `fake.task03.*`, dự án/hợp đồng/tài liệu có nhãn `[FAKE-TASK03]` sau khi xác minh rõ database đích.
