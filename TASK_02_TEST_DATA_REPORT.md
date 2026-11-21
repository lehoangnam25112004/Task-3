# TASK-02 – BÁO CÁO DỮ LIỆU KIỂM THỬ

## 1. Cách triển khai
- Seed riêng: [Data/Task02TestDataSeeder.cs](Data/Task02TestDataSeeder.cs), gọi từ [Program.cs](Program.cs).
- Chỉ chạy khi `ASPNETCORE_ENVIRONMENT=Development` **và** bật cờ cấu hình. Production không bao giờ chạy.
- Toàn bộ dữ liệu nghiệp vụ được tạo qua `IProjectService` / `IEmployeeService` (validation, transaction, trigger SQLite áp dụng như thật). Chỉ riêng tài khoản Identity giả được thêm trực tiếp vì `Employee.UserId` bắt buộc.
- Idempotent: mỗi bước kiểm tra dữ liệu đã có (theo mã/tên/lịch sử vai trò) trước khi tạo; chạy lại không sinh bản ghi trùng.
- Không thay đổi schema, migration hay Entity.

## 2. Cách nhận diện dữ liệu kiểm thử
| Loại | Dấu hiệu |
|---|---|
| Department | Tên bắt đầu `[TEST] ` |
| Employee | `EmployeeCode` = `TEST-NV-001..008` |
| Tài khoản Identity | Email `test.nv00X@test.invalid`, không có mật khẩu (không đăng nhập được) |
| Project | `TEST-PRJ-002` (dự án `CPMS-2026-001` của bạn được tái sử dụng, không tạo lại) |
| Lý do đổi PM | Bắt đầu bằng `[TEST]` |

## 3. Dữ liệu đã tạo
- 4 Department; 8 Employee (+8 tài khoản Identity giả); 1 Project mới (`TEST-PRJ-002`, **chưa có PM**).
- 9 ProjectMember, 13 role assignment, thêm 3 dòng ProjectManagerHistories.
- Bảng bị thay đổi: `Departments`, `Employees`, `AspNetUsers`, `Projects`, `ProjectMembers`, `ProjectMemberRoleAssignments`, `ProjectManagerHistories`.
- Dự án thật `CPMS-2026-001` **không bị ghi đè**: chỉ được bổ nhiệm PM (test) và thêm thành viên. Dòng lịch sử PM ban đầu (#1) giữ nguyên.

| Mã | Vai trò kiểm thử |
|---|---|
| TEST-NV-001 | PM chính của CPMS-2026-001; Phó PM ở TEST-PRJ-002 |
| TEST-NV-002 | PM ban đầu (đã bị thay, vai trò PM đã kết thúc) → nay là Phó PM ở CPMS-2026-001 và TEST-PRJ-002 |
| TEST-NV-003 | Site Manager CPMS-2026-001 (thử thêm vào TEST-PRJ-002 để kiểm tra giới hạn 1 dự án) |
| TEST-NV-004 | Supervising Engineer CPMS-2026-001 |
| TEST-NV-005 | Nhiều vai trò cùng dự án: Phó PM + Site Manager |
| TEST-NV-006 | Lịch sử đổi vai trò: SE (đã kết thúc) → Phó PM → SE (phân công lại, bản ghi mới) |
| TEST-NV-007 | Đã nghỉ việc (`Employee.IsActive=false`); phân công Phó PM đã được kết thúc, ProjectMember `IsActive=false`; không xuất hiện trong form thêm thành viên |
| TEST-NV-008 | Nhân viên sẵn sàng, chưa vào dự án nào – dùng thử thêm thành viên |

Ghi chú: mô hình hiện tại chỉ có điều kiện "không được tham gia dự án" là nhân viên nghỉ việc (`Employee.IsActive=false`), nên TEST-NV-007 đáp ứng cả hai yêu cầu nhân viên không hoạt động / không đủ điều kiện. Khóa tài khoản đăng nhập không chặn phân công (QD-13).

## 4. Chạy lại / xóa
Chạy lại (an toàn): đặt biến môi trường rồi chạy ứng dụng ở Development.
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:CPMS_SEED_TASK02_TEST_DATA = "true"
dotnet run
```
Xóa riêng dữ liệu kiểm thử (chỉ bản ghi có dấu hiệu ở mục 2; giữ nguyên `CPMS-2026-001`, gỡ PM/thành viên test và khôi phục dòng lịch sử PM ban đầu):
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:CPMS_REMOVE_TASK02_TEST_DATA = "true"
dotnet run
```
Nhớ tắt các biến sau khi chạy.

## 5. Kết quả kiểm tra
- Build: thành công, 0 lỗi (chỉ còn 3 cảnh báo NU1902/NU1903 đã có từ trước).
- Sao lưu trước khi chạy: `ConstructionProjectManagement_pre_testdata.db` (thư mục files của session).
- Thử trên bản sao DB: seed → chạy lại (số lượng không đổi) → xóa → DB trở về đúng trạng thái ban đầu (1 project, 1 user, 1 dòng lịch sử PM).
- DB thật sau seed (chạy 2 lần): `integrity_check = ok`, `foreign_key_check` không lỗi; số lượng Dept/Emp/Proj/Member/Assignment/PMHistory/User = 4/8/2/9/13/4/9.
- Giao diện (trên bản sao DB, dùng tài khoản Admin tạm vì không có mật khẩu Admin thật): đăng nhập, danh sách dự án, chi tiết dự án, form thêm thành viên hiển thị nhân viên hoạt động và **không** có TEST-NV-007; dự án chưa có PM hiển thị bình thường.

## 6. Lỗi phát hiện và đã xử lý
- Lần seed đầu: nhân viên 007 nghỉ việc nhưng phân công chưa được kết thúc (do đối tượng được theo dõi nên service không thấy thay đổi `IsActive`). Đã sửa và seed lại dữ liệu test (xóa dữ liệu test rồi tạo lại).
- Một lần ghi file làm hỏng mã hóa tiếng Việt; đã sửa và seed lại.

## 7. Lưu ý
- Đăng nhập UI bằng tài khoản Admin thật chưa được thực hiện vì không có mật khẩu; hãy tự đăng nhập để kiểm tra.
- Id của TEST-PRJ-002 là 3 (id 2 đã dùng ở lần seed trước và bị xóa).
- Dữ liệu CPMS-2026-001 đã có PM/thành viên test; hãy dùng lệnh xóa ở mục 4 để hoàn tác.
