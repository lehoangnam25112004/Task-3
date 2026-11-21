# Báo cáo TASK-02 – Phase 1: Database và Entity

## 1. Kết luận

Đã triển khai và áp dụng migration Phase 1 trên SQLite hiện có. Build thành công; EF Core xác nhận không còn thay đổi model chưa có migration. Database đạt `integrity_check = ok`, không có vi phạm khóa ngoại, và hai migration hiện có đều được ghi nhận đã áp dụng.

Phạm vi hoàn thành ở giai đoạn này là **schema, entity, cấu hình EF Core và các ràng buộc dữ liệu**. Chưa hoàn tất đồng bộ nghiệp vụ tại Service/Controller/View: các luồng tạo/sửa dự án, bổ nhiệm PM, thêm/kết thúc thành viên và phân công vai trò vẫn cần một giai đoạn tiếp theo.

## 2. File đã tạo hoặc chỉnh sửa

### Thay đổi Phase 1

- `Data/ApplicationDbContext.cs` – DbSet, quan hệ Employee, catalog role, index, check constraint và cấu hình các entity mới.
- `Models/ProjectMember.cs` – liên kết tùy chọn tới Employee và collection lịch sử phân công vai trò.
- `Models/ProjectRoleCode.cs` – mã cố định cho các vai trò được khai báo.
- `Models/ProjectRole.cs` – danh mục vai trò dự án và giới hạn số dự án đồng thời.
- `Models/ProjectMemberRoleAssignment.cs` – lịch sử từng lần phân công vai trò.
- `Models/ProjectManagerHistory.cs` – lịch sử thay đổi PM chính.
- `Migrations/20261114073018_Task02Phase1ProjectMemberAssignments.cs` và `.Designer.cs` – migration Phase 1, backfill, index và trigger SQLite.
- `Migrations/ApplicationDbContextModelSnapshot.cs` – snapshot model hiện tại sau Phase 1.

### Baseline migration

- `Migrations/20261111092125_InitialSqlite.cs`
- `Migrations/20261111092125_InitialSqlite.Designer.cs`

Hai file baseline trên đã được khôi phục trong quá trình xử lý migration; chi tiết rủi ro được nêu tại Mục 8.

## 3. Schema được giữ nguyên và bổ sung

### Thành phần cũ được giữ

- Các bảng và cột hiện hữu không bị xóa.
- `Projects.ProjectCode` vẫn có unique index toàn cục; index cũ này tiếp tục giữ mã dự án sau khi archive.
- `Projects.ProjectManagerId` vốn đã nullable nên không cần đổi cột để đáp ứng dự án tạm thời chưa có PM.
- `ProjectMembers.ProjectId`, `UserId`, `AssignmentRole`, `JoinedAt`, `IsActive` và unique `(ProjectId, UserId)` được giữ nguyên.
- Các bảng Identity và tám vai trò Identity hiện có được giữ nguyên.

### Thành phần mới

- Thêm `ProjectMembers.EmployeeId` nullable cùng FK tới `Employees.Id` (`RESTRICT`); giá trị chỉ được backfill khi `ProjectMembers.UserId` khớp với `Employees.UserId`.
- Thêm `ProjectRoles` làm catalog vai trò dự án, với bốn mã seed: Project Manager, Deputy Project Manager, Site Manager và Supervising Engineer.
- Thêm `ProjectMemberRoleAssignments` lưu `ProjectMemberId`, mã vai trò, `StartedAt`, `EndedAt`.
- Thêm `ProjectManagerHistories` lưu dự án, PM trước/sau, thời điểm bắt đầu/kết thúc và lý do thay đổi.

### Ràng buộc và bảo vệ dữ liệu

- Unique `(ProjectId, EmployeeId)` khi `EmployeeId IS NOT NULL`, bên cạnh unique membership cũ `(ProjectId, UserId)`.
- Unique assignment đang hoạt động theo `(ProjectMemberId, ProjectRoleCode)` khi `EndedAt IS NULL`.
- Unique lịch sử PM đang mở theo `ProjectId` khi `EndedAt IS NULL`.
- Check constraint cho thời gian bắt đầu/kết thúc và giá trị giới hạn số dự án.
- Trigger SQLite ghi lịch sử khi tạo dự án hoặc đổi `ProjectManagerId`; trigger khác chặn phân công active cho membership không active/không gắn Employee, vượt giới hạn số dự án của role, hoặc vô hiệu hóa membership còn role active.
- Giới hạn đồng thời được cấu hình là không giới hạn cho Project Manager và Deputy Project Manager; tối đa một dự án cho Site Manager và Supervising Engineer. Không có cơ chế phê duyệt ngoại lệ trong Phase 1.

## 4. Migration và bảo toàn dữ liệu

- Migration mới: `20261114073018_Task02Phase1ProjectMemberAssignments`.
- Database tiếp tục dùng SQLite; không tạo database mới và không đổi provider.
- `EmployeeId` được thêm nullable để không buộc dữ liệu legacy phải có hồ sơ Employee. Chỉ liên kết tự động khi xác định được bằng `UserId`.
- Không chuyển `AssignmentRole` cũ thành role assignment vì không có bằng chứng đáng tin cậy về ngày bắt đầu vai trò.
- Lịch sử PM baseline không tự đặt ngày bắt đầu giả; thời điểm không biết được để `NULL`.
- Có kiểm thử migration trên database rỗng và database thử nghiệm có dữ liệu legacy tổng hợp, bao gồm rollback/reapply và bảo toàn sequence SQLite.
- Đã tạo backup trước khi áp dụng migration: `C:\Users\OSAKA\.copilot\session-state\1eb6f4af-20b6-43a4-a5d4-efd8a8206794\files\ConstructionProjectManagement_pre_task02_phase1_final.db`. Backup được kiểm tra `integrity_check = ok` và chỉ có migration baseline.

## 5. Kết quả kiểm tra

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build ConstructionProjectManagement.csproj --no-restore -p:UseAppHost=false` | Thành công, 0 lỗi |
| `dotnet ef migrations has-pending-model-changes --no-build` | Không có thay đổi model chưa migration |
| `dotnet ef migrations list --no-build` | Baseline và Phase 1 đều được ghi nhận đã áp dụng |
| SQLite `PRAGMA integrity_check` | `ok` |
| SQLite `PRAGMA foreign_key_check` | 0 vi phạm |
| DB hiện tại – migration history | `20261111092125_InitialSqlite`, `20261114073018_Task02Phase1ProjectMemberAssignments` |
| DB hiện tại – dữ liệu liên quan | `AspNetRoles`: 8; `Projects`, `Employees`, `ProjectMembers`, `AspNetUsers`: 0; `ProjectRoles`: 4 |
| Kiểm thử ràng buộc cô lập | Đã kiểm tra giới hạn role, assignment active trùng, PM history active, trạng thái membership, mã dự án archived, backfill legacy, rollback/reapply và SQLite sequence |

Database hiện tại không có project, employee, project member hoặc user nên không thể chứng minh bằng dữ liệu thật rằng các bản ghi loại đó đã được giữ nguyên; kiểm thử trên database legacy tổng hợp dùng để xác nhận đường migration/backfill. Tám Identity role có sẵn và cấu trúc Identity vẫn còn sau migration.

Build phát ra ba cảnh báo dependency đã tồn tại: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (NU1903) và `System.Security.Cryptography.Pkcs` 9.0.0 (NU1902/NU1903). Không thay đổi package trong phạm vi này.

## 6. Chưa giải quyết và ảnh hưởng tới Function

Không sửa Service, Controller, View hay quy trình phân quyền. Vì vậy, schema mới chưa tự bảo đảm luồng nghiệp vụ hoàn chỉnh:

- Service cần đặt `ProjectMember.EmployeeId`, tạo và kết thúc các role assignment, đồng bộ PM chính với membership/role, và xử lý nghỉ việc.
- Trigger PM tự ghi thời điểm nhưng không nhận được lý do thay đổi từ service; muốn lưu lý do cần luồng nghiệp vụ ghi lịch sử tường minh.
- Các dữ liệu legacy không có ngày bắt đầu role hoặc kỳ PM chính xác vẫn không thể khôi phục lịch sử đã mất.
- Các chức năng khác có thể tiếp tục đọc `AssignmentRole` cũ; không xóa cột này. Cần chuyển đổi từng caller sau khi kiểm kê và kiểm thử ở phase phù hợp.
- Các task dùng Project/ProjectMember/Employee không bị thay đổi code trong Phase 1. Cần regression test các luồng liên quan trước khi phát hành.

## 7. Nghiệm thu đề xuất cho bước tiếp theo

1. Tạo/sửa project phải đồng bộ PM chính, membership và role assignment trong cùng transaction; đổi/bỏ PM phải đóng đúng kỳ lịch sử.
2. Thêm lại Employee vào project phải tái sử dụng membership phù hợp, không tạo bản ghi trùng; không chấp nhận Employee nghỉ việc.
3. Thay đổi hoặc kết thúc role phải đóng assignment cũ bằng `EndedAt`, giữ lịch sử và chỉ đặt `ProjectMember.IsActive = false` khi không còn role active.
4. Kiểm tra authorization phạm vi dự án ở server; UI không phải là ranh giới bảo vệ duy nhất.
5. Kiểm thử hồi quy các Function khác sử dụng Project, ProjectMember, Employee, `ProjectManagerId`, `AssignmentRole` và `IsActive`.

## 8. Lưu ý về baseline migration

Trong quá trình scaffold/loại bỏ migration, thao tác EF chạy với assembly cũ đã làm mất source baseline migration và snapshot. Các file baseline được khôi phục trực tiếp từ mã nguồn EF còn lại và schema SQLite live; không dùng decompiler hay cài thêm công cụ. Baseline Designer được dựng lại từ snapshot source, loại các phần Phase 1 để biểu diễn model trước migration.

Build, danh sách migration, kiểm tra pending model changes và kiểm tra database hiện tại đều đạt. Tuy nhiên, do source baseline được tái dựng thay vì giữ nguyên bản EF sinh ban đầu, cần review baseline này trước khi dùng lịch sử migration để bootstrap một database khác hoặc làm căn cứ cho các migration độc lập tiếp theo. Migration Phase 1 đã được áp dụng thành công trên database hiện tại và các database kiểm thử cô lập.
