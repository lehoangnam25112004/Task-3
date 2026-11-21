# TASK-02 — Báo cáo kiểm thử độc lập

## A. Phạm vi

Đánh giá các Function FN-02-01 đến FN-02-06: danh sách dự án, chi tiết, tạo/cập nhật dự án, xem/thêm thành viên, đồng bộ Project Manager, lịch sử phân công role và validation liên quan.

Đã kiểm tra quyền truy cập theo các nhóm Admin, Project Director và Project Manager trong các luồng đại diện. AUTH chi tiết, Role–Task–Function authorization, quy trình phê duyệt ngoại lệ Site Manager đa dự án, xóa vật lý dự án, và hồi quy toàn bộ các module khác không nằm trong phạm vi thực thi.

Không có test project trước lượt QA này. Đã bổ sung xUnit với SQLite in-memory để kiểm thử service. Các thao tác MVC có ghi dữ liệu chạy trên bản sao SQLite cô lập; database đang dùng không bị ghi hoặc xóa.

## B. Thống kê

Tổng cộng **55 test case** được ghi trong [TASK02_Independent_TestCases.md](./TASK02_Independent_TestCases.md):

| Trạng thái | Số lượng |
|---|---:|
| PASS | 53 |
| FAIL | 0 |
| BLOCKED | 0 |
| NOT IMPLEMENTED | 2 |
| NOT TESTED | 0 |

`NOT IMPLEMENTED` áp dụng cho xóa vật lý Project (ngoài sáu Function) và luồng phê duyệt ngoại lệ đa dự án Site Manager (đang ngoài phạm vi). Những ca chưa có bằng chứng thực thi vẫn để `NOT TESTED`.

## C. Kết quả kiểm thử

| Function | Kết quả | Bằng chứng chính |
|---|---|---|
| FN-02-01 — Danh sách dự án | PASS trong các tình huống đã chạy | Admin mở danh sách; các dự án hiện hành hiển thị trên DB clone. |
| FN-02-02 — Chi tiết dự án | PASS trong các tình huống đã chạy | Mở trang chi tiết; ID không tồn tại trả 404; dữ liệu PM/thành viên/role được đối chiếu với DB clone. |
| FN-02-03 — Tạo dự án | PASS trong các tình huống đã chạy | Tạo dự án không PM và có PM; từ chối mã trùng kể cả mã của dự án archived; service từ chối ngày không hợp lệ, ngân sách âm và Identity manager không tồn tại. |
| FN-02-04 — Cập nhật dự án | PASS trong các tình huống đã chạy | Sửa trường dự án; membership và role assignment vẫn được giữ; thay/bỏ PM đồng bộ và lưu lịch sử. |
| FN-02-05 — Danh sách thành viên | PASS trong các tình huống đã chạy | Danh sách/chi tiết thành viên và role hiện hành khớp dữ liệu liên quan trên clone. |
| FN-02-06 — Thêm thành viên | PASS trong các tình huống đã chạy | Thêm nhiều role, thêm role cho member cũ, từ chối role trùng, nhân viên nghỉ việc/không tồn tại; kết thúc và tái phân công bảo toàn lịch sử. |

### Các ca được hoàn tất trong lượt này

- Đã thực thi 19 ca trước đó `NOT TESTED`; tất cả được cập nhật theo bằng chứng, không còn ca `NOT TESTED`.
- Validation thực tế: trường bắt buộc rỗng/khoảng trắng, Name 201 ký tự, ProjectCode/Location/ClientName vượt độ dài, số/ngày sai định dạng, chuỗi đặc biệt và HTML encoding.
- Resource/role: dự án archived, project/member ID không tồn tại, project code trùng khi edit, role code không hợp lệ, các route quản trị role được thử.
- SQLite trực tiếp từ chối duplicate ProjectCode, invalid ProjectManagerId FK, duplicate ProjectMember, duplicate active role assignment và invalid ProjectRole FK. Mọi probe chạy trong SAVEPOINT và rollback; row counts không đổi.
- Log/error: 4 request âm trả 200/200/404/400; không có exception/stack trace trong response; log không có `fail`, `crit`, `Unhandled` hoặc `Exception`.
- Phát hiện QA-02-002: route/body ID mismatch đã cập nhật nhầm project trên clone trước sửa. Sau khi ràng buộc route ID và thêm kiểm tra tồn tại, mismatch trả 400, project ID không tồn tại trả 404.

### Kiểm thử tự động

Lệnh:

```powershell
dotnet test .\tests\ConstructionProjectManagement.Tests\ConstructionProjectManagement.Tests.csproj --no-restore -p:UseAppHost=false -v:minimal
```

Kết quả cuối: **8 passed, 0 failed, 0 skipped**. Các test bao phủ tạo dự án chưa có PM; từ chối dữ liệu ngày/ngân sách sai, manager không tồn tại và ProjectCode archived trùng; đồng bộ/thay PM; role trùng/không hợp lệ; tình trạng Employee và tài khoản đăng nhập; giới hạn Site Manager; kết thúc/tái phân công membership; và chấm dứt Employee.

### Build

Lệnh:

```powershell
dotnet build .\ConstructionProjectManagement.csproj --no-restore -p:UseAppHost=false -v:minimal
```

Kết quả sau sửa: **Build succeeded, 0 errors**. Build có 3 cảnh báo package audit mức High/Moderate (các advisory được ghi trong [TASK02_BugReport.md](./TASK02_BugReport.md)).

### Database

- Các request MVC ghi trên DB clone mới từ clone QA; service test dùng SQLite in-memory và migration của ứng dụng. Database đang sử dụng không bị ghi.
- Sau thao tác kiểm thử trên clone: `PRAGMA integrity_check` trả `ok`; `PRAGMA foreign_key_check` không có dòng vi phạm. Trước khi loại bỏ clone, số lượng là 6 Projects, 12 ProjectMembers, 21 RoleAssignments và 4 ProjectRoles.
- Đã đối chiếu lịch sử PM, ProjectMember và role assignment sau các thao tác thay/bỏ PM, kết thúc role/membership và phân công lại.
- H-06 đã chạy trực tiếp 5 probe FK/unique trên clone; tất cả bị từ chối và rollback, các số lượng hàng giữ nguyên.
- Test đặc biệt B-08 lưu đúng chuỗi ký tự vào clone; trang Details render `<script>` dưới dạng HTML-encoded.
- Không thực hiện đăng nhập trên trình duyệt đồ họa; các luồng xác thực/ủy quyền đã kiểm tra qua HTTP MVC trên môi trường thử. Từ chối quyền có thể chuyển hướng đến Identity AccessDenied và trả trang đích HTTP 200.

## D. Lỗi phát hiện

Đã phát hiện QA-02-002 trong C-04: POST có route ID và body ID khác nhau có thể cập nhật record theo body. Đã sửa `ProjectsController.Edit` để lấy ID từ route, kiểm tra dự án hoạt động tồn tại, và xác minh mismatch trả 400 / unknown ID trả 404. Không có lỗi chức năng khác được xác nhận trong 55 ca.

Lỗi hạ tầng build QA-02-001 do glob Web SDK quét artifact test vẫn được sửa bằng cách loại `tests/**` khỏi default items trong `ConstructionProjectManagement.csproj`. Sau QA-02-002, chạy lại thành công toàn bộ 8 unit test và build ứng dụng.

Các lần thất bại ban đầu do test harness đếm tập kết quả rỗng sai và do kỳ vọng HTTP 403 trực tiếp thay vì redirect AccessDenied; đây không phải lỗi sản phẩm. Chi tiết ở [TASK02_BugReport.md](./TASK02_BugReport.md).

## E. Regression test

- Sau sửa QA-02-002, chạy lại xUnit: 8/8 PASS; build ứng dụng: 0 lỗi.
- Retest C-04 sau sửa: mismatch route/body trả 400, POST ID không tồn tại trả 404.
- Đã kiểm tra các request Project Manager bị giới hạn theo dự án của họ và Project Director truy cập được các dự án thử; request bị từ chối được chuyển hướng đến AccessDenied.
- Chưa chạy toàn bộ hồi quy các module khác đang tham chiếu Project, ProjectMember hoặc Employee; cũng chưa chạy bộ integration test hiện hữu riêng vì repository ban đầu không có test project khác.

## F. Đánh giá chất lượng

Các luồng cốt lõi và 19 tình huống validation/resource/constraint/error còn thiếu đã được thực thi trên service và MVC/SQLite cô lập. QA-02-002 đã được sửa và retest; không có lỗi TASK-02 đã xác nhận đang mở.

Rủi ro/hạn chế: chưa chạy hồi quy toàn solution hoặc kiểm thử giao diện bằng trình duyệt đồ họa. Approval exception Site Manager không được triển khai (ngoài phạm vi TASK-02). Build vẫn cảnh báo advisory package mức High/Moderate; chưa nâng package vì nằm ngoài phạm vi lượt QA.

## G. Kết luận

**READY WITH KNOWN ISSUES**

Toàn bộ ca trong ma trận đã có trạng thái kết luận: 53 PASS, 0 FAIL, 2 NOT IMPLEMENTED ngoài phạm vi. Lỗi C-04 đã sửa và xác minh sau sửa; 8 unit test và build đều đạt. Các cảnh báo dependency High/Moderate và hồi quy toàn solution chưa thực hiện vẫn là vấn đề cần theo dõi. Kết luận này chỉ dành cho kiểm thử độc lập TASK-02, không phải nghiệm thu toàn hệ thống.
