# Báo cáo triển khai TASK-02 – Phase 2

## 1. Kết luận

Đã triển khai các luồng danh sách, chi tiết, tạo và cập nhật dự án; xem, thêm và quản lý lịch sử thành viên/vai trò dự án. Các thao tác ghi liên quan đến dự án, PM, thành viên và phân công được thực hiện trong transaction, đồng thời tiếp tục dùng schema Phase 1 và SQLite hiện có.

Không tạo migration, không cập nhật database thật, không thay đổi đăng nhập hoặc triển khai Role–Task–Function Authorization. Database thật được kiểm tra chỉ đọc sau khi thử nghiệm: toàn vẹn `ok`, không có lỗi khóa ngoại; dữ liệu mẫu và database thử nghiệm được tách riêng.

## 2. Kết quả theo Function

| Function | Kết quả |
|---|---|
| FN-02-01 – Xem danh sách dự án | Hoàn tất. Dự án lưu trữ không xuất hiện. Admin/Project Director xem mọi dự án còn hoạt động; người dùng khác chỉ thấy dự án mình là PM chính hoặc thành viên. Giao diện có trạng thái danh sách rỗng và hiển thị rõ dự án chưa có PM. |
| FN-02-02 – Xem chi tiết dự án | Hoàn tất. Hiển thị thông tin dự án, PM, thành viên đang tham gia và vai trò hiện hành. Quyền xem được lọc ở service. Bổ sung nạp Employee để chi tiết thành viên hiển thị đúng mã nhân viên. |
| FN-02-03 – Tạo dự án | Hoàn tất. Kiểm tra mã dự án trên toàn hệ thống, kể cả mã của dự án lưu trữ; hỗ trợ không có PM. Khi có PM, tạo đồng bộ membership và vai trò Project Manager. |
| FN-02-04 – Cập nhật dự án | Hoàn tất. Cập nhật thông tin dự án không xóa/tạo lại membership. Đổi hoặc bỏ PM kết thúc phân công PM cũ, đồng bộ PM mới và giữ lịch sử bổ nhiệm cùng lý do. |
| FN-02-05 – Xem thành viên dự án | Hoàn tất. Hiển thị hồ sơ Employee, trạng thái membership và toàn bộ các giai đoạn phân công vai trò, bao gồm cả dữ liệu legacy. |
| FN-02-06 – Thêm thành viên dự án | Hoàn tất. Chọn Employee có sẵn và danh mục `ProjectRoles`; cho phép phân nhiều vai trò đồng thời hoặc phân thêm vai trò cho thành viên hiện tại. Chặn Employee nghỉ việc, vai trò trùng đang hiệu lực và giới hạn số dự án theo catalog. |

Trang quản lý thành viên cũng hỗ trợ kết thúc riêng một vai trò hoặc toàn bộ việc tham gia. Các bản ghi lịch sử được giữ lại; membership chỉ trở thành không hoạt động khi không còn vai trò hiệu lực. Thêm lại sử dụng membership hiện có nếu phù hợp.

## 3. Quy tắc nghiệp vụ đã triển khai

- Phạm vi quản lý thành viên được kiểm tra phía server: Admin/Project Director quản lý mọi dự án; Project Manager chỉ quản lý dự án có `ProjectManagerId` khớp tài khoản hiện tại. Giữ nguyên role gate hiện có ở controller; không xây dựng AUTH chi tiết.
- PM chính phải có Employee đang hoạt động. PM có thể để trống; hệ thống không tự suy diễn PM.
- Bổ nhiệm PM đồng bộ `ProjectManagerId`, `ProjectMember` và `ProjectMemberRoleAssignment`; thay PM đóng phân công PM cũ mà không xóa membership hoặc các vai trò khác của PM cũ.
- Lịch sử PM do cơ chế schema Phase 1 ghi nhận; service lưu lý do vào lịch sử đang hiệu lực khi có lý do được cung cấp.
- Vai trò mới chỉ lấy từ `ProjectRoles`. `AssignmentRole` legacy được giữ để hiển thị, không dùng làm nguồn tạo phân công mới.
- Không tạo Employee hoặc Identity account trong TASK-02. Chỉ nhận Employee hiện hữu; Employee nghỉ việc không thể được phân công mới. Tài khoản đăng nhập bị vô hiệu hóa không bị xem đồng nghĩa với Employee nghỉ việc.
- Khi Employee được chuyển sang trạng thái nghỉ việc, service kết thúc các phân công/membership hiện tại và gỡ PM chính ở các dự án liên quan trong transaction; lịch sử PM được ghi lý do.
- Mã dự án được chuẩn hóa khoảng trắng và kiểm tra duy nhất, kể cả trên dự án lưu trữ. Các ràng buộc SQLite Phase 1 vẫn là lớp bảo vệ bổ sung.

## 4. File đã tạo hoặc chỉnh sửa

### Đã chỉnh sửa

- `Services/ProjectService.cs` – triển khai truy vấn danh sách/chi tiết, kiểm tra phạm vi quản lý, tạo/cập nhật dự án, đồng bộ PM, thêm/kết thúc vai trò và membership.
- `Services/Interfaces/IProjectService.cs` – khai báo các thao tác dự án, thành viên và catalog vai trò.
- `Services/EmployeeService.cs` – kiểm tra hồ sơ trùng; phân biệt nghỉ việc với trạng thái tài khoản; kết thúc phân công dự án khi Employee nghỉ việc.
- `Services/Interfaces/IEmployeeService.cs` – loại bỏ hợp đồng cũ dùng dữ liệu tài khoản/free-text để thêm thành viên dự án.
- `Controllers/ProjectsController.cs` – kết nối service cho 4 Function dự án, tải Employee cho lựa chọn PM và trả lỗi nghiệp vụ về form.
- `Controllers/ProjectMembersController.cs` – kết nối danh sách/thêm/kết thúc phân công; kiểm tra quyền dự án ở server và chống CSRF cho POST.
- `ViewModels/ProjectViewModels.cs` – dữ liệu form dự án, lý do đổi PM và màn hình quản lý thành viên.
- `ViewModels/EmployeeViewModels.cs` – loại bỏ ViewModel thành viên cũ không còn phù hợp với Employee và role catalog.
- `Views/Projects/_ProjectForm.cshtml` – lựa chọn PM từ Employee, cho phép không bổ nhiệm PM và nhập lý do thay đổi.
- `Views/Projects/Details.cshtml` – hiển thị PM, thành viên/vai trò và liên kết quản lý theo quyền.
- `Views/Projects/Index.cshtml` – hiển thị danh sách, trạng thái chưa có PM và trạng thái không có kết quả.
- `Views/ProjectMembers/Index.cshtml` – chọn Employee/role, xem lịch sử, kết thúc vai trò hoặc toàn bộ tham gia.
- `Data/DbInitializer.cs` – chuyển tạo dự án mẫu qua `IProjectService` để dùng cùng quy trình đồng bộ PM.
- `Program.cs` – đăng ký `IProjectService` trước khi khởi tạo dữ liệu mẫu.

### Đã tạo

- `TASK_02_PHASE_2_REPORT.md` – báo cáo triển khai và kiểm thử.

Không thêm test project vào solution; kiểm thử nghiệp vụ được chạy bằng harness cô lập tạm thời bên ngoài project và đã dọn sau khi hoàn tất.

## 5. Migration, database và dữ liệu cũ

- Không tạo migration mới vì schema Phase 1 đã có các bảng và ràng buộc cần thiết: `ProjectRoles`, `ProjectMemberRoleAssignments`, `ProjectManagerHistories`, nullable `ProjectManagerId`, unique membership/mã dự án và các ràng buộc vai trò đang hiệu lực.
- Không áp dụng migration hoặc ghi dữ liệu Phase 2 vào database thật.
- Kiểm tra database thật bằng kết nối chỉ đọc: `PRAGMA integrity_check = ok`, `PRAGMA foreign_key_check` có 0 vi phạm. Các bảng `Projects`, `Employees`, `ProjectMembers`, `AspNetUsers` hiện đều có 0 bản ghi; `ProjectRoles` có 4 bản ghi; lịch sử migration vẫn chỉ gồm baseline và migration Phase 1.
- Giữ nguyên cột `ProjectMembers.AssignmentRole` để tương thích các module legacy.

## 6. Kiểm thử thực tế

### Build

`dotnet build ConstructionProjectManagement.csproj --no-restore -p:UseAppHost=false` – thành công, 0 lỗi.

Build còn phát ba cảnh báo dependency hiện có: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (NU1903) và `System.Security.Cryptography.Pkcs` 9.0.0 (NU1902/NU1903). Không thay đổi package ngoài phạm vi.

### Kiểm thử service/SQLite cô lập

Harness chạy với SQLite riêng và migration hiện hữu, đạt **36/36 assertions**, bao gồm:

- Tạo dự án có PM/không PM; từ chối PM không có hồ sơ Employee.
- Từ chối ngày dự án/ngân sách không hợp lệ.
- Từ chối ProjectCode đã được dùng bởi dự án lưu trữ.
- Danh sách/chi tiết dự án theo quyền và trạng thái lưu trữ.
- Member management chỉ dành cho PM chính hoặc vai trò đặc quyền.
- Đồng bộ PM với membership/role; lưu lịch sử, lý do, đổi PM và bỏ PM.
- Giữ PM cũ nếu còn vai trò khác; tái bổ nhiệm không tạo membership trùng và không ghi đè lịch sử.
- Chọn lại thành viên hiện tại để phân thêm vai trò; nhiều vai trò đồng thời và từ chối vai trò đang trùng.
- Giới hạn số dự án theo role catalog.
- Kết thúc một vai trò/toàn bộ membership, tái kích hoạt membership và phân công lại sau khi kết thúc kỳ cũ.
- Từ chối Employee nghỉ việc; phân biệt trạng thái Employee với tài khoản đăng nhập bị vô hiệu hóa.
- Nghỉ việc kết thúc các phân công hiện hành, bỏ PM chính và ghi lý do lịch sử.
- Chi tiết dự án nạp đúng `EmployeeCode`.

### Kiểm thử MVC

Trên database web thử nghiệm tách biệt, đã đăng nhập tài khoản Admin tổng hợp và kiểm tra:

- Tạo dự án với PM là Employee, mở trang chi tiết và trang danh sách.
- Thêm nhân sự với nhiều vai trò, kết thúc một vai trò, kết thúc toàn bộ tham gia và thay đổi PM.
- Trang chi tiết hiển thị mã Employee và tên PM; trang danh sách hiển thị dữ liệu đúng.
- Sau khi sửa bộ lọc lựa chọn Employee, trang quản lý thành viên hiển thị cả thành viên hiện tại để có thể phân thêm vai trò.

Database web thử nghiệm và harness tạm thời đã được dọn. Database thật không bị dùng cho thao tác thử.

## 7. Lỗi phát hiện và xử lý

- Trang chi tiết chưa nạp `ProjectMember.Employee`, dẫn đến thiếu mã nhân viên. Bổ sung `ThenInclude(Employee)` và xác nhận bằng assertion SQLite và trình duyệt.
- Bộ chọn Employee chỉ liệt kê người chưa là thành viên, cản trở phân thêm vai trò sau lần thêm đầu tiên. Điều chỉnh để liệt kê Employee còn hoạt động; service vẫn chặn trùng vai trò và kiểm tra giới hạn.
- Form danh sách dự án chưa thể hiện trạng thái rỗng và PM chưa bổ nhiệm. Bổ sung nội dung hiển thị tương ứng.
- Loại bỏ eager-load membership không cần thiết khỏi truy vấn trang sửa dự án.

## 8. Ảnh hưởng và vấn đề còn lại

- `AssignmentRole` vẫn còn trong entity/schema và các snapshot/migration cũ; các caller legacy khác không bị đổi hoặc xóa trong Phase 2.
- `EmployeeService` nay chủ động kết thúc phân công khi Employee nghỉ việc. Các luồng TASK-07 thay đổi trạng thái Employee cần kiểm thử hồi quy trong môi trường tích hợp.
- Đã kiểm tra build và các luồng TASK-02; chưa chạy bộ regression tự động cho toàn bộ module vì source hiện không có test project phù hợp. Không chạy kiểm thử tải hoặc cạnh tranh ghi đồng thời; unique index/trigger SQLite vẫn là lớp bảo vệ dữ liệu.
- Git branch/status không thể xác minh vì thư mục dự án hiện tại không có Git repository. Không commit hoặc push.
- Các yêu cầu AUTH chi tiết, quy trình phê duyệt ngoại lệ đa dự án và các nghiệp vụ bổ sung được nêu là ngoài phạm vi, chưa triển khai.
