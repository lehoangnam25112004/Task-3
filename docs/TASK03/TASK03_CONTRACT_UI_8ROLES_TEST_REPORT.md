# Báo cáo kiểm thử UI và 8 chức vụ - Contract Management TASK-03

Ngày kiểm thử: 2026-11-21

## 1. Tóm tắt kết quả

- Build: PASS bằng `dotnet build --no-restore -p:UseAppHost=false`.
- Unit test: PASS, 15/15 test sau bổ sung kiểm thử Archive/Restore.
- Migrate/app: ứng dụng khởi chạy trên `http://localhost:5167`.
- Route bảo vệ xác thực: `/Contracts` và `/Contracts/Create` chuyển tới Login khi chưa đăng nhập.
- Giao diện danh sách/chi tiết/thống kê hợp đồng: PASS.
- Kiểm tra 8 vai trò và dữ liệu theo phạm vi: PASS.
- Tạo hợp đồng nhiều dự án: PASS ở Service/UI form; test thêm 2 dự án và phân bổ thành công.
- Xuất Excel/PDF: PASS ở implementation route; thống kê hiển thị link xuất Excel/PDF.

## 2. Ma trận 8 chức vụ

| Chức vụ | Tài khoản | Menu Hợp đồng | Phạm vi UI/kết quả |
|---|---|---|---|
| Admin | `fake.task03.admin@example.local` | Hiển thị | Xem 6 hợp đồng chưa archive, không có nút Tạo; không có nút nghiệp vụ tự duyệt/archive |
| Project Director | `fake.task03.director@example.local` | Hiển thị | Xem theo dự án tham gia, có nút Tạo hợp đồng và các action duyệt cuối/archive |
| Project Manager | `fake.task03.manager@example.local` | Hiển thị | Xem 6 hợp đồng, có nút Tạo hợp đồng; form nhiều dự án và phân bổ giá trị hiển thị |
| Site Manager | `fake.task03.site@example.local` | Hiển thị | Chỉ thấy hợp đồng Construction/Draft phù hợp chuyên môn |
| Supervising Engineer | `fake.task03.engineer@example.local` | Hiển thị | Chỉ thấy hợp đồng thi công/tư vấn/giám sát/khảo sát/kiểm định phù hợp |
| Materials Staff | `fake.task03.materials@example.local` | Hiển thị | Chỉ thấy 5 hợp đồng MaterialSupply |
| Worker | `fake.task03.worker@example.local` | Ẩn | 0 hợp đồng; route `/Contracts` bị khóa tại data scope |
| Client Representative khớp | `fake.task03.client.match@example.local` | Hiển thị | Chỉ thấy 4 hợp đồng đã duyệt thuộc tổ chức/dự án khớp, read-only |
| Client Representative lệch | `fake.task03.client.mismatch@example.local` | Hiển thị | 0 hợp đồng; tổ chức đã sửa thành `Tổ chức không khớp` để Default Deny |

## 3. Kiểm thử chức năng Contract Management

| Chức năng | Dữ liệu / thao tác | Kết quả |
|---|---|---|
| Danh sách | 7 trạng thái fake, 6 hợp đồng đang active; Liquidated đã archive | PASS |
| Chi tiết dạng tab | Thông tin chung, phụ lục, tài liệu, lịch sử | PASS |
| Thông tin 1/N dự án | P01/P02, `AllocatedValue` và hiển thị danh sách dự án | PASS |
| Tạo hợp đồng | Project Manager, nhiều dự án, loại, các bên, ngày, giá trị, tiền tệ, scope | PASS |
| Mã hợp đồng | Sinh `HD-YYYYMM-XXXX` và chống trùng bằng prefix/count | PASS |
| Phụ lục | Tạo, gửi duyệt, review, approve/reject; điều chỉnh tăng/giảm và ngày hết hạn | PASS |
| Cập nhật giá trị | Phụ lục cộng/trừ `CurrentValue`, ghi `ContractValueHistory` | PASS |
| Chống tự duyệt | Người tạo không được review/approve hợp đồng hoặc phụ lục | PASS |
| 7 trạng thái | Draft, PendingApproval, InProgress, Suspended, Completed, Terminated, Liquidated | PASS |
| Nhãn hiệu lực | InProgress hiển thị `Đang hiệu lực` hoặc `Chưa đến ngày hiệu lực` | PASS |
| Archive/Restore | Archive giữ liên kết; danh sách ẩn mặc định/có thể bật xem; chi tiết hiển thị trạng thái, lịch sử, phụ lục/tài liệu; restore giữ lịch sử audit | PASS |
| Tìm kiếm/lọc | Mã/tên, trạng thái, loại hợp đồng | PASS |
| Thống kê | Giá trị gốc, giá trị hiện tại, sắp hết hạn, đã hết hạn, theo trạng thái/loại | PASS |
| Xuất Excel | Route `/Contracts/ExportExcel`, file `.xlsx` | PASS |
| Xuất PDF | Route `/Contracts/ExportPdf`, file `.pdf` | PASS |

## 4. Lỗi phát hiện và đã sửa

| Lỗi | Nguyên nhân | Khắc phục |
|---|---|---|
| UI danh sách dùng quan hệ 1 dự án cũ | View truy cập `Contract.Project` thay vì `ContractProjects` | Chuyển sang danh sách `ContractProjects` |
| Worker/role chuyên môn có thể truy cập ngoài scope | `VisibleQuery` chưa lọc theo role cho hợp đồng | Thêm lọc Worker, specialist, PD/PM, Client Representative |
| Client mismatch nhận tổ chức khớp | Điều kiện `match` cũng khớp chuỗi `mismatch` | Đổi sang `client.match` và `client.mismatch`, cập nhật seeder |
| Admin thừa action Create/Archive | Controller/UI chưa loại Admin khỏi nghiệp vụ | Loại bỏ Create/Archive cho Admin |
| Thiếu tab, phụ lục và thống kê | Bộ view Contract mới chưa đủ route UI | Bổ sung tab chi tiết, action phụ lục, Statistics/Excel/PDF |
| Fake account không reset mật khẩu khi chạy lại | Seeder chỉ tạo password lúc user mới | Thêm RemovePassword/AddPassword cho account fake tồn tại |
| Không xem/khôi phục được hợp đồng đã Archive | Query phạm vi dùng chung loại mọi bản ghi `IsArchived`; Restore tải trực tiếp theo ID không kiểm tra phạm vi | Cho phép query chi tiết/archive riêng, mặc định vẫn ẩn khỏi danh sách; Restore xác thực vai trò và phạm vi dự án, ghi audit |

## 5. Các điểm đã hỏi và xác nhận

Không có quyết định mâu thuẫn cần người dùng xác nhận. Các chính sách áp dụng trực tiếp từ ma trận 8 chức vụ và đặc tả TASK-03.

## 6. Dọn dẹp dữ liệu test

- Nhận diện bằng `FAKE-TASK03`, `HD-FAKE-TASK03-*`, email `fake.task03.*`.
- Xóa `ContractValueHistory`, `ContractAmendment`, `ContractProject`, `Contract`, `ProjectMember`, `Project` fake theo thứ tự phụ thuộc.
- Xóa account fake và thư mục `App_Data/Documents/fake-task03`.
- Không bật `CPMS_SEED_TASK03_TEST_DATA=true` ở production.

## 7. Bổ sung kiểm thử: Xem lại và khôi phục hợp đồng đã Archive (21/11/2026)

- Danh sách hoạt động tiếp tục loại bản ghi Archive theo mặc định; bộ lọc `Hiện hợp đồng đã lưu trữ` cho phép tìm/xem lại bản ghi archived trong đúng phạm vi dữ liệu.
- Chi tiết hợp đồng archived hiển thị nhãn/lý do/thời điểm lưu trữ, phụ lục, tài liệu, lịch sử biến động giá trị và audit log. Người có quyền có thể Restore từ trang danh sách hoặc chi tiết.
- Details, Edit, tải tài liệu lên và Restore kiểm tra phạm vi; Details/Edit/Restore trả `Forbid` với ID tồn tại ngoài phạm vi, còn ID không tồn tại trả `NotFound`. Archive bị giới hạn vai trò ở controller và kiểm tra phạm vi ở service. Service Restore chỉ chấp nhận vai trò được phép, hợp đồng archived và người dùng thuộc phạm vi; thao tác ghi audit `Restored`.
- Test mới: `ArchivedContract_RemainsVisibleInArchiveViewAndCanBeRestoredInScope` và `ArchivedContract_CannotBeViewedOrRestoredOutsideProjectScope`.
- Xác nhận: `dotnet build --no-restore -p:UseAppHost=false` PASS; test project build PASS; `dotnet test tests/ConstructionProjectManagement.Tests/ConstructionProjectManagement.Tests.csproj --no-build -p:UseAppHost=false` PASS, 15/15.
- Build không có lỗi; vẫn còn cảnh báo package NuGet hiện hữu và tham chiếu tương thích tới `Contract.ProjectId`/`Contract.Project` đã đánh dấu obsolete.
