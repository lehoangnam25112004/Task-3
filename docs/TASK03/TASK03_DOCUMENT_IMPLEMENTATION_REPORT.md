# Báo cáo triển khai và xác minh Document Management — TASK-03

## 1. Tóm tắt kết quả

- Đối chiếu và hoàn thiện phân quyền server-side theo đặc tả: Admin chỉ xem/quản trị danh mục/lưu trữ/thống kê; nhân sự dự án bị giới hạn bởi thành viên active; Worker chỉ xem tài liệu đã duyệt thuộc nhóm được phép; Client Representative cần thành viên active và tổ chức khớp Chủ đầu tư.
- Hoàn thiện kiểm tra file tối đa 25 MiB, phần mở rộng, kích thước stream thực tế, magic bytes và cấu trúc OOXML.
- Hoàn thiện tìm kiếm/lọc, thống kê, xuất Excel/PDF, phiên bản, khóa chỉnh sửa 15 phút, phê duyệt và Archive/Restore.
- `dotnet build --no-restore -p:UseAppHost=false`: **PASS**, 0 lỗi; còn 10 cảnh báo có sẵn (cảnh báo package và các tham chiếu `Contract.ProjectId` obsolete, ngoài phạm vi thay đổi).
- `dotnet test tests\ConstructionProjectManagement.Tests\ConstructionProjectManagement.Tests.csproj --no-build -p:UseAppHost=false`: **PASS**, 24/24.
- Smoke test ứng dụng với SQLite tạm biệt lập: trang chủ `200`, trang đăng nhập `200`, `/Documents` chuyển hướng đăng nhập `302` khi chưa xác thực. Migration chỉ áp dụng lên database tạm; database hệ thống không bị thay đổi.

## 2. Các hạng mục đã hoàn thiện

### Phân quyền và phạm vi

- Service lọc dữ liệu trước khi thực hiện thao tác; tài khoản inactive hoặc thiếu `ProjectMember.IsActive` không truy cập tài liệu dự án.
- Client Representative chỉ thấy tài liệu đã duyệt trong dự án active khi `OrganizationName` khớp `Project.ClientName`; mặc định từ chối nếu tổ chức trống/lệch.
- Worker chỉ thấy tài liệu đã duyệt thuộc danh mục `Safety`, `Minutes`, `Other`, hoặc tài liệu dùng chung toàn hệ thống.
- Admin không được tạo, sửa, gửi duyệt, kiểm tra hoặc phê duyệt nội dung nghiệp vụ. Archive/Restore, quản trị Category, thống kê và tải/xem tài liệu vẫn được kiểm tra riêng.
- Quyền Archive/Restore của PD/PM yêu cầu thành viên active trong dự án của tài liệu; tài liệu system-wide ngoài phạm vi dự án chỉ Admin quản lý được.

### File, phiên bản và khóa

- Whitelist: `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.jpg`, `.jpeg`, `.png`; giới hạn 25 MiB mỗi file.
- Kiểm tra magic bytes cho PDF, ảnh, OLE2; kiểm tra ZIP và các thành phần bắt buộc cho DOCX/XLSX/PPTX. UI chọn file và thông báo giới hạn đồng bộ với whitelist.
- Cập nhật metadata không tạo version; tải file mới tạo version độc lập. Khóa chỉnh sửa hết hạn sau 15 phút, chỉ chủ khóa hoặc Admin có thể giải phóng; thao tác sửa/version bị từ chối trong khi tài liệu chờ duyệt.
- Office không còn mở endpoint preview không hỗ trợ; chi tiết hiển thị hướng dẫn tải xuống.

### Quy trình, lưu trữ và tìm kiếm

- Quy trình review/approve/reject tuân theo số cấp của Category và vai trò; chặn tự thao tác dựa trên uploader, submitter và uploader của version hiện tại.
- Archive cần lý do và ghi audit; Restore giữ nguyên tài liệu, version, liên kết và lịch sử. Danh sách hoạt động mặc định ẩn tài liệu đã Archive; người có quyền có thể lọc xem lại và Restore.
- Bổ sung lọc theo từ khóa, dự án, Category, hợp đồng, trạng thái phê duyệt, người tải lên, khoảng ngày tải lên và trạng thái lưu trữ; giữ bộ lọc khi phân trang/xuất.
- Thống kê theo Category, dự án, trạng thái và tháng tải lên; hỗ trợ xuất danh sách Excel/PDF và báo cáo thống kê Excel/PDF theo phạm vi dữ liệu người dùng được xem.

## 3. Kiểm thử đã chạy

| Nhóm | Xác minh | Kết quả |
|---|---|---|
| Build | `dotnet build --no-restore -p:UseAppHost=false` | PASS |
| Unit/integration | 24 test của test project, gồm kiểm tra scope, Worker, Client Representative, Admin, khóa, version, phê duyệt, Archive/Restore, audit bất biến và migration SQLite | PASS |
| Upload validation | File giả mạo, quá giới hạn, OLE2 Office, PPTX hợp lệ và ZIP giả mạo | PASS |
| Tìm kiếm | Lọc người tải lên và khoảng ngày | PASS |
| Runtime | Khởi động ứng dụng trên SQLite tạm; kiểm tra trang chủ, đăng nhập và route `/Documents` | PASS |

Database SQLite hệ thống không được migrate hoặc seed trong lượt xác minh này. Ứng dụng khởi động smoke test dùng database tạm và database đó đã được dọn sau khi kiểm tra.

## 4. File chính đã tạo/sửa

- Service/API: `Services/ProjectDocumentService.cs`, `Services/Interfaces/IProjectDocumentService.cs`, `Controllers/DocumentsController.cs`.
- Kiểm tra file và dữ liệu: `Helpers/FileValidationHelper.cs`, `Models/DocumentModels.cs`, `ViewModels/DocumentViewModels.cs`.
- Giao diện: `Views/Documents/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Statistics.cshtml`.
- Test: `tests/ConstructionProjectManagement.Tests/Task03DocumentServiceTests.cs`.
- Các entity, DbContext, seeder và migration Document Management được cập nhật trong phần triển khai TASK-03 tương ứng.

## 5. Làm rõ nghiệp vụ

Không phát sinh điểm cần hỏi thêm trong lượt triển khai này. Quyền Admin strict và quyền xem tài liệu dùng chung của nhóm nội bộ được giữ theo quyết định đã chốt trước đó.

## 6. Hướng dẫn kiểm tra UI và dữ liệu test

1. Chạy ứng dụng với cấu hình SQLite phù hợp môi trường kiểm thử; đăng nhập bằng tài khoản test đã được chuẩn bị trong môi trường đó.
2. Mở `/Documents`, thử các bộ lọc, phân trang, xem chi tiết, lịch sử version/audit, preview PDF/ảnh, tải file, Archive và Restore theo đúng vai trò.
3. Vào `/Documents/Statistics` để xem thống kê và thử xuất Excel/PDF; so sánh kết quả với phạm vi dự án của tài khoản.
4. Seeder TASK-03 dùng nhãn `[FAKE-TASK03]` và có cờ `CPMS_SEED_TASK03_TEST_DATA`; chỉ bật trên database kiểm thử. Không xóa dữ liệu theo pattern rộng; đối chiếu nhãn fake trước khi dọn dữ liệu.
5. Không chạy seeder hoặc migration trên database thật nếu chưa có bản sao lưu và phê duyệt vận hành.
