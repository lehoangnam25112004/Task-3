# TASK03_CODEBASE_DEPENDENCY_AUDIT — Kiểm toán codebase phục vụ phát triển TASK-03

> **Ngày kiểm toán:** 2026-11-21
> **Phạm vi:** Toàn bộ repository `D:\Task 3\Task-2-main` (dự án ConstructionProjectManagement).
> **Tính chất:** CODE AUDIT chỉ đọc. Không sửa mã nguồn, không chạy migration, không seed/sửa dữ liệu, không thay đổi cấu hình, không cài/gỡ package. File duy nhất được tạo mới là chính báo cáo này.
> **Phương pháp:** Đọc trực tiếp mã nguồn theo 5 nhóm (Data/Models, Controllers, Services, ViewModels/Views/Frontend, Tests/Config/Docs), đối chiếu chéo giữa các tầng và với tài liệu trong repo. Mọi nhận định đều có dẫn chứng file/class/method.

---

## ⚠️ GHI NHẬN MÂU THUẪN QUAN TRỌNG VỀ HIỆN TRẠNG TASK-03

Đề bài kiểm toán nêu: *"Toàn bộ những thành phần cũ liên quan đến TASK-03 đã được loại bỏ. Hiện tại không có mã nguồn TASK-03 để kế thừa."*

**Thực tế repository KHÔNG khớp với mô tả này.** Mã nguồn hiện tại vẫn chứa đầy đủ các thành phần mà tài liệu trong repo (`docs/TASK03_Business_Description.md`, `TASK_FUNCTION_INVENTORY.md` — các mã FN-03-01…FN-03-06) định danh là TASK-03 "Quản lý hợp đồng và tài liệu":

| Thành phần còn tồn tại | File |
|---|---|
| Controller hợp đồng/tài liệu | `Controllers/ContractsController.cs` |
| Service hợp đồng | `Services/ContractService.cs` + `Services/Interfaces/IContractService.cs` |
| Service tài liệu | `Services/ProjectDocumentService.cs` + `Services/Interfaces/IProjectDocumentService.cs` |
| Entity | `Models/Contract.cs`, `Models/ContractStatus.cs`, `Models/ProjectDocument.cs` |
| ViewModel | `ViewModels/ContractViewModels.cs` |
| Views | `Views/Contracts/` (Index, Details, Create, Edit, `_ContractForm`) |
| Schema DB | Bảng `Contracts`, `ProjectDocuments` trong `Migrations/20261111092125_InitialSqlite.cs`; DbSet + cấu hình trong `Data/ApplicationDbContext.cs` |
| Menu | Mục "Hợp đồng" trong `Views/Shared/_Layout.cshtml` |
| DI | `Program.cs:30-31` đăng ký `IContractService`, `IProjectDocumentService` |

**Cách xử lý trong báo cáo này:** Tuân thủ giới hạn "chỉ phân tích hiện trạng", báo cáo kiểm kê các thành phần trên như **mã nguồn đang tồn tại trong hệ thống** (không giả định chúng là TASK-03 "đã hoàn thiện", cũng không xóa/bỏ qua chúng). Khi TASK-03 được xây dựng mới, nhóm phát triển **bắt buộc phải quyết định trước**: (a) xóa/thay thế nhóm mã này, (b) kế thừa và mở rộng, hay (c) xây song song. Nếu không quyết định, sẽ xung đột trực tiếp về route (`/Contracts`), tên bảng (`Contracts`, `ProjectDocuments`), unique index (`IX_Contracts_ContractNumber`) và menu. Các đánh giá tái sử dụng (Phần VI) phân loại nhóm này riêng.

---

# PHẦN I – TỔNG QUAN CODEBASE

## 1.1 Thông tin chung

| Mục | Giá trị |
|---|---|
| Tên dự án | ConstructionProjectManagement (CPMS — Hệ thống quản lý dự án xây dựng) |
| Thư mục gốc | `D:\Task 3\Task-2-main` |
| Kiểu ứng dụng | ASP.NET Core MVC (server-rendered Razor Views) — **một project duy nhất**, không có file `.sln` |
| Framework | **.NET 10** (`net10.0`), Nullable + ImplicitUsings bật |
| ORM / DB | EF Core 10.0.1 + **SQLite** (`Data/Databases/ConstructionProjectManagement.db`). Thư mục `Migrations.SqlServer/` là di sản lịch sử, **bị loại khỏi biên dịch** (`.csproj:11`) |
| Xác thực | ASP.NET Core Identity (`AddDefaultIdentity<ApplicationUser>` + `AddRoles<IdentityRole>`), UI mặc định từ package `Microsoft.AspNetCore.Identity.UI` 10.0.1 (không có `Areas/Identity` tùy biến) |
| Phân quyền | Role-based thuần (`[Authorize(Roles="...")]` chuỗi cứng). **Không có** policy, claim tùy chỉnh, authorization handler |
| Frontend | Bootstrap 5.3.3, jQuery 3.7.1, jquery-validation 1.21.0 + unobtrusive 4.0.0 (commit sẵn trong `wwwroot/lib`); Chart.js 4.4.7 nạp từ CDN (chỉ trang Progress) |
| Test | 1 project xUnit: `tests/ConstructionProjectManagement.Tests` (8 test, SQLite in-memory, chỉ phủ TASK-02 ProjectService/EmployeeService) |
| Package chính | Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.1, Identity.UI 10.0.1, EFCore.Sqlite 10.0.1, EFCore.Tools 10.0.1. **Không có** Serilog, AutoMapper, FluentValidation, Swagger, MediatR, package export Excel/PDF |

## 1.2 Các project trong "solution"

| Project | Đường dẫn | Ghi chú |
|---|---|---|
| ConstructionProjectManagement | `./ConstructionProjectManagement.csproj` | Web app chính |
| ConstructionProjectManagement.Tests | `./tests/ConstructionProjectManagement.Tests/` | xUnit; tham chiếu project chính; **không có file .sln gom chung** — muốn chạy test phải trỏ thẳng csproj |

## 1.3 Kiến trúc

Kiến trúc phân tầng đơn giản trong một project, theo quy ước bắt buộc ghi ở `AGENTS.md`:

```
Razor Views (+ ViewModels) → Controllers → Services (business rules) → ApplicationDbContext (EF Core) → SQLite
```

- **Không có** tầng Repository/Unit of Work — service dùng trực tiếp `ApplicationDbContext`.
- **Không có** BaseController, Middleware tùy chỉnh, Filter tùy chỉnh, Areas, API controller ([ApiController]); endpoint JSON duy nhất là `ProgressController.ChartData`.
- Service là `sealed class` + primary constructor, đăng ký **Scoped** qua interface tại `Program.cs:28-40` (ngoại lệ: `DashboardService` đăng ký class cụ thể, không có interface).
- Service báo lỗi bằng **tuple** `(bool Succeeded, string? Error)` hoặc `(bool, string?, TEntity?)`; thông điệp lỗi tiếng Việt hiển thị thẳng cho người dùng.
- Một phần logic nghiệp vụ TASK-02 nằm trong **6 trigger SQLite** (migration `20261114073018`), ngoài EF model.

## 1.4 Các module hiện có (theo controller)

| Module | Controller | Service | TASK (theo TASK_FUNCTION_INVENTORY.md) |
|---|---|---|---|
| Tài khoản & phân quyền | AccountController | (dùng trực tiếp UserManager/RoleManager) | TASK-01 |
| Dự án | ProjectsController | ProjectService | TASK-02 |
| Thành viên dự án | ProjectMembersController | ProjectService | TASK-02 |
| Hợp đồng & tài liệu | ContractsController | ContractService, ProjectDocumentService | (được inventory gán TASK-03 — xem mâu thuẫn ở đầu) |
| WBS (giai đoạn/hạng mục) | WbsController | WbsService | TASK-04 |
| Công việc | TasksController | TaskService | TASK-05 |
| Tiến độ | ProgressController | ProgressService | TASK-06 |
| Nhân sự | EmployeesController | EmployeeService | TASK-07 |
| Đội thi công | TeamsController | (DbContext trực tiếp) | TASK-07 |
| Vật tư | MaterialsController | MaterialService | TASK-08 |
| Thiết bị | EquipmentController | EquipmentService | TASK-09 |
| Nhật ký thi công | ConstructionLogsController | ConstructionLogService | TASK-10 |
| Chất lượng (Sự cố/RFI/Nghiệm thu) | QualityController | QualityService | TASK-11 |
| Chi phí/Bàn giao/Lưu trữ | CompletionController | CompletionService | TASK-12 |
| Dashboard | DashboardController | DashboardService | — |
| Trang chủ/Lỗi | HomeController | — | — |

## 1.5 Thống kê file và phạm vi đã kiểm tra

| Nhóm | Số lượng | Đã đọc/phân tích |
|---|---|---|
| File `.cs` (không tính bin/obj) | 138 | — |
| File `.cs` mã nguồn chính (không tính 2 thư mục Migrations) | 106 | **106/106** (Controllers 16, Services 13, Interfaces 12, Models 46, ViewModels 14, Data 3, Program.cs, ErrorViewModel; test 1) |
| Migrations SQLite (`Migrations/`) | 5 file | **5/5** (2 migration + 2 Designer + snapshot) |
| Migrations SQL Server (`Migrations.SqlServer/`) | 27 file | Đọc lướt xác nhận **không được compile**; không phân tích chi tiết từng file (lý do: `.csproj:11` loại khỏi build, không ảnh hưởng runtime) |
| Views `.cshtml` | 62 | **62/62** |
| JS/CSS tự viết (`wwwroot`, ngoài `lib`) | 2 | **2/2** (`site.js` rỗng, `site.css` 91 dòng) |
| Thư viện `wwwroot/lib` | ~40 file | Chỉ kiểm kê tên/phiên bản, không đọc nội dung (lý do: thư viện bên thứ ba nguyên bản) |
| Cấu hình | 4 | **4/4** (csproj, appsettings.json, appsettings.Development.json, launchSettings.json) |
| Tài liệu `.md` | 38 | Đọc/tóm tắt các file trọng yếu (README, AGENTS, PROJECT_STRUCTURE, TASK_FUNCTION_INVENTORY, bộ TASK_02_*, docs/01…07, TASK03_Business_Description); không phân tích chi tiết từng sơ đồ `.mmd` trong docs/06 (18 sơ đồ Mermaid — chỉ kiểm kê) |
| Database | `Data/Databases/` hiện **chỉ có `.gitignore`** — file `.db` chưa tồn tại trên máy; app sẽ tự tạo + migrate khi chạy lần đầu (`Program.cs:66-69`). Không có thư mục `App_Data/` (service tự tạo khi upload) |

**Thư mục chưa kiểm tra chi tiết và lý do:** `bin/`, `obj/` (sinh tự động); `wwwroot/lib` (thư viện bên thứ ba); nội dung từng migration trong `Migrations.SqlServer/` (bị loại khỏi compile, chỉ là bản lưu lịch sử — dùng nó để suy luận schema hiện tại sẽ sai); file nhị phân.

**Những gì KHÔNG tồn tại trong repo** (đã rà soát bằng find/grep): `.sln`, `.git`, CI (GitHub Actions/Azure Pipelines), Dockerfile, `appsettings.Production.json`, `UserSecretsId`, `global.json`, `.editorconfig`, `Directory.Build.props`, thư mục Areas/Middleware/Filters/Helpers/Validators/Extensions, authorization policy (`AddPolicy`), `ILogger<T>` trong controller/service, package.json/libman.json, file `.http`, integration test WebApplicationFactory.

---

# PHẦN II – DANH MỤC FILE CODE LIÊN QUAN

Mức độ liên quan tới TASK-03 (nghiệp vụ hợp đồng – tài liệu, hoặc bất kỳ module mới nào cần project-scope + upload file): **CAO** = TASK-03 chắc chắn đụng tới; **TRUNG BÌNH** = nền tảng dùng chung/tham chiếu; **THẤP** = ít liên quan trực tiếp nhưng thuộc hệ thống phải bảo vệ khỏi regression.

## 2.1 Hạ tầng & cấu hình

| STT | File | Đường dẫn | Module/Tầng | Chức năng chính | Liên quan |
|---|---|---|---|---|---|
| 1 | Program.cs | `./Program.cs` | Hạ tầng | Composition root: DbContext SQLite, Identity, DI 13 service, pipeline, route, migrate + seed khi khởi động | CAO |
| 2 | ConstructionProjectManagement.csproj | `./ConstructionProjectManagement.csproj` | Hạ tầng | net10.0, 4 package, loại Migrations.SqlServer + tests khỏi compile | CAO |
| 3 | appsettings.json | `./appsettings.json` | Cấu hình | Connection string SQLite, logging | CAO |
| 4 | appsettings.Development.json | `./appsettings.Development.json` | Cấu hình | Chỉ logging | THẤP |
| 5 | launchSettings.json | `./Properties/launchSettings.json` | Cấu hình | Profile http:5167 / https:7181, env Development | THẤP |

## 2.2 Tầng dữ liệu

| STT | File | Đường dẫn | Module/Tầng | Chức năng chính | Liên quan |
|---|---|---|---|---|---|
| 6 | ApplicationDbContext.cs | `./Data/ApplicationDbContext.cs` | Data | IdentityDbContext; 29 DbSet; toàn bộ cấu hình index/FK/precision/check constraint | CAO |
| 7 | DbInitializer.cs | `./Data/DbInitializer.cs` | Data | Migrate + seed 8 role + seed admin (CPMS_ADMIN_*) + dự án mẫu DEMO-001 | TRUNG BÌNH |
| 8 | Task02TestDataSeeder.cs | `./Data/Task02TestDataSeeder.cs` | Data | Seed/gỡ dữ liệu test TASK-02 (chỉ Development, theo cờ CPMS_*) | THẤP |
| 9 | 20261111092125_InitialSqlite.cs | `./Migrations/` | Data | Baseline schema 30 bảng (raw SQL), gồm cả Contracts/ProjectDocuments | CAO |
| 10 | 20261114073018_Task02Phase1ProjectMemberAssignments.cs | `./Migrations/` | Data | Bảng ProjectRoles/ProjectMemberRoleAssignments/ProjectManagerHistories + 6 trigger SQLite | CAO |
| 11 | ApplicationDbContextModelSnapshot.cs | `./Migrations/` | Data | Snapshot EF model, khớp migration 2 | CAO |
| 12 | (27 file) | `./Migrations.SqlServer/` | Data (di sản) | Lịch sử migration SQL Server — KHÔNG compile, KHÔNG dùng suy luận schema | THẤP |

## 2.3 Models (entity, enum, hằng số)

| STT | File | Đường dẫn | Loại | Chức năng chính | Liên quan |
|---|---|---|---|---|---|
| 13 | Project.cs | `./Models/Project.cs` | Entity | Dự án — thực thể gốc; ProjectManagerId; IsArchived; CreatedAt/UpdatedAt | CAO |
| 14 | ProjectMember.cs | `./Models/ProjectMember.cs` | Entity | Thành viên dự án (UserId + EmployeeId); IsActive — nền tảng mọi kiểm tra scope | CAO |
| 15 | ProjectMemberRoleAssignment.cs | `./Models/ProjectMemberRoleAssignment.cs` | Entity | Phân công vai trò theo khoảng thời gian (StartedAt/EndedAt) | CAO |
| 16 | ProjectRole.cs | `./Models/ProjectRole.cs` | Entity | Danh mục vai trò dự án (PK = Code, seed 4 dòng, MaximumConcurrentProjects) | CAO |
| 17 | ProjectManagerHistory.cs | `./Models/ProjectManagerHistory.cs` | Entity | Lịch sử thay PM (ghi bởi trigger) | TRUNG BÌNH |
| 18 | Contract.cs | `./Models/Contract.cs` | Entity | Hợp đồng gắn dự án; ContractNumber unique toàn cục; IsArchived | CAO |
| 19 | ContractStatus.cs | `./Models/ContractStatus.cs` | Enum | Draft/Active/Expired/Terminated | CAO |
| 20 | ProjectDocument.cs | `./Models/ProjectDocument.cs` | Entity | Metadata tài liệu (FileName/StoredFileName/ContentType/Category/UploadedBy/IsArchived); ContractId nullable | CAO |
| 21 | ApplicationUser.cs | `./Models/ApplicationUser.cs` | Entity | IdentityUser + FullName + IsActive | CAO |
| 22 | RoleNames.cs | `./Models/RoleNames.cs` | Static | 8 role Identity của hệ thống | CAO |
| 23 | ProjectRoleCode.cs | `./Models/ProjectRoleCode.cs` | Static | 4 hằng mã vai trò dự án | TRUNG BÌNH |
| 24 | Employee.cs | `./Models/Employee.cs` | Entity | Hồ sơ nhân sự 1-1 với User; IsActive | TRUNG BÌNH |
| 25 | Department.cs | `./Models/Department.cs` | Entity | Phòng ban | THẤP |
| 26 | ConstructionTeam.cs / TeamMember.cs | `./Models/` | Entity | Đội thi công + thành viên đội | THẤP |
| 27 | ProjectPhase.cs | `./Models/ProjectPhase.cs` | Entity | Giai đoạn dự án (Status là **string**, không enum) | THẤP |
| 28 | WorkItem.cs / WbsItemStatus.cs | `./Models/` | Entity+Enum | Hạng mục WBS dạng cây; unique (ProjectId, WbsCode) | THẤP |
| 29 | Task.cs / TaskStatus.cs / TaskPriority.cs | `./Models/` | Entity+Enum | Công việc (alias TaskEntity do trùng tên BCL) | TRUNG BÌNH |
| 30 | TaskAssignment.cs / TaskDependency.cs / TaskProgressHistory.cs | `./Models/` | Entity | Giao việc / phụ thuộc (chưa dùng) / lịch sử tiến độ | TRUNG BÌNH |
| 31 | Material.cs / MaterialRequest.cs / MaterialRequestItem.cs / MaterialTransaction.cs + 2 enum | `./Models/` | Entity+Enum | Vật tư, phiếu yêu cầu, sổ nhập xuất | THẤP |
| 32 | Equipment.cs / EquipmentAssignment.cs / EquipmentStatus.cs | `./Models/` | Entity+Enum | Thiết bị và bàn giao thiết bị | THẤP |
| 33 | ConstructionLog.cs | `./Models/ConstructionLog.cs` | Entity | Nhật ký thi công — có pattern lưu file đính kèm thứ hai | TRUNG BÌNH |
| 34 | Incident.cs / RfiRequest.cs / InspectionRequest.cs / InspectionHistory.cs + 4 enum | `./Models/` | Entity+Enum | Sự cố, RFI, nghiệm thu, lịch sử nghiệm thu | THẤP |
| 35 | ProjectExpense.cs / ProjectHandover.cs | `./Models/` | Entity | Chi phí, biên bản bàn giao | THẤP |
| 36 | ProjectStatus.cs | `./Models/ProjectStatus.cs` | Enum | 6 trạng thái dự án | TRUNG BÌNH |
| 37 | ErrorViewModel.cs | `./Models/ErrorViewModel.cs` | ViewModel | RequestId cho trang Error | THẤP |

## 2.4 Services + Interfaces

| STT | File | Đường dẫn | Chức năng chính | Liên quan |
|---|---|---|---|---|
| 38 | ProjectService.cs + IProjectService.cs | `./Services/`, `./Services/Interfaces/` | Trung tâm TASK-02: CRUD dự án, visibility, quản lý thành viên/vai trò, transaction + trigger | **CAO** |
| 39 | ContractService.cs + IContractService.cs | `./Services/` | CRUD hợp đồng theo scope (VisibleQuery/ValidateAsync) | **CAO** |
| 40 | ProjectDocumentService.cs + IProjectDocumentService.cs | `./Services/` | Upload/Download tài liệu: validate extension/size, lưu App_Data, kiểm quyền | **CAO** |
| 41 | TaskService.cs + ITaskService.cs | `./Services/` | CRUD task theo scope; mẫu VisibleQuery có nhánh assignee | TRUNG BÌNH |
| 42 | ProgressService.cs + IProgressService.cs | `./Services/` | Cập nhật tiến độ + history + chart | THẤP |
| 43 | WbsService.cs + IWbsService.cs | `./Services/` | Cây WBS; CanAccessProjectAsync | TRUNG BÌNH |
| 44 | EmployeeService.cs + IEmployeeService.cs | `./Services/` | CRUD nhân sự; tự kết thúc phân công khi nghỉ việc (transaction) | TRUNG BÌNH |
| 45 | MaterialService.cs + IMaterialService.cs | `./Services/` | Vật tư + quy trình duyệt/xuất kho (IssueRequest có transaction) | THẤP |
| 46 | EquipmentService.cs + IEquipmentService.cs | `./Services/` | Thiết bị + bàn giao/thu hồi | THẤP |
| 47 | ConstructionLogService.cs + IConstructionLogService.cs | `./Services/` | Nhật ký + file đính kèm (pattern upload thứ hai) | TRUNG BÌNH |
| 48 | QualityService.cs + IQualityService.cs | `./Services/` | Sự cố/RFI/nghiệm thu | THẤP |
| 49 | CompletionService.cs + ICompletionService.cs | `./Services/` | Chi phí/bàn giao/**ArchiveProjectAsync** (đặt Project.IsArchived) | TRUNG BÌNH |
| 50 | DashboardService.cs (không interface) | `./Services/` | 9 chỉ số tổng hợp theo scope | THẤP |

## 2.5 Controllers

| STT | File | Role gate cấp class | Chức năng chính | Liên quan |
|---|---|---|---|---|
| 51 | ContractsController.cs | `[Authorize]` (+ gate Admin/PD/PM từng action ghi) | Hợp đồng + upload/download tài liệu | **CAO** |
| 52 | ProjectsController.cs | `[Authorize]` (+ Admin/PD cho Create/Edit) | CRUD dự án | CAO |
| 53 | ProjectMembersController.cs | Admin/PD/PM | Quản lý thành viên — **mẫu chuẩn duy nhất về kiểm quyền theo tài nguyên** (`AuthorizeProjectAsync`) | CAO |
| 54 | AccountController.cs | `[Authorize]` (+ Admin từng action) | CRUD tài khoản + gán role + Profile | TRUNG BÌNH |
| 55 | DashboardController.cs | `[Authorize]` | Trang tổng quan | THẤP |
| 56 | TasksController.cs | `[Authorize]` (+ Admin/PD/PM) | CRUD task | TRUNG BÌNH |
| 57 | WbsController.cs | `[Authorize]` (+ Admin/PD/PM) | WBS | THẤP |
| 58 | ProgressController.cs | `[Authorize]` | Tiến độ + ChartData JSON | THẤP |
| 59 | EmployeesController.cs | Admin/PD | Nhân sự | THẤP |
| 60 | TeamsController.cs | Admin/PD | Đội thi công — **ghi DbContext trực tiếp, vi phạm phân lớp** | THẤP |
| 61 | MaterialsController.cs | Admin/PD/PM/SM/MS | Vật tư + duyệt phiếu | THẤP |
| 62 | EquipmentController.cs | Admin/PD/SM | Thiết bị | THẤP |
| 63 | ConstructionLogsController.cs | Admin/PD/PM/SM/SE | Nhật ký thi công | TRUNG BÌNH |
| 64 | QualityController.cs | Admin/PD/PM/SM/SE | Chất lượng | THẤP |
| 65 | CompletionController.cs | Admin/PD/PM | Chi phí/bàn giao/archive dự án | TRUNG BÌNH |
| 66 | HomeController.cs | (không Authorize) | Trang chủ/Privacy/Error template | THẤP |

## 2.6 ViewModels (14 file, `./ViewModels/`)

| STT | File | Classes | Liên quan |
|---|---|---|---|
| 67 | ContractViewModels.cs | ContractEditViewModel, ContractListViewModel, ContractDetailsViewModel | **CAO** |
| 68 | ProjectViewModels.cs | ProjectEditViewModel, ProjectListViewModel, ProjectMemberViewModel, ProjectMemberManagementViewModel | CAO |
| 69 | AccountViewModels.cs | AccountList(Item)ViewModel, AccountEditViewModel, ProfileViewModel | TRUNG BÌNH |
| 70 | TaskViewModels.cs | TaskEditViewModel, TaskListViewModel, TaskDetailsViewModel | TRUNG BÌNH |
| 71 | DashboardViewModel.cs | DashboardViewModel (9 chỉ số) | THẤP |
| 72 | ConstructionLogViewModels.cs | ConstructionLogEditViewModel (+IFormFile), ConstructionLogIndexViewModel | TRUNG BÌNH |
| 73–80 | Completion/Employee/Equipment/Material/Progress/Quality/Team/Wbs ViewModels.cs | (chi tiết Phần III) | THẤP |

## 2.7 Views (62 file, `./Views/`)

| STT | Thư mục | File | Liên quan |
|---|---|---|---|
| 81 | Views/Contracts | Index, Details, Create, Edit, `_ContractForm` | **CAO** |
| 82 | Views/Shared | `_Layout` (+`.css`), `_LoginPartial`, `_ValidationScriptsPartial`, Error | CAO |
| 83 | Views/Projects | Index, Details, Create, Edit, `_ProjectForm` | CAO |
| 84 | Views/ProjectMembers | Index | CAO |
| 85 | Views/Account | Index, Create, Edit, Profile, `_AccountForm` | TRUNG BÌNH |
| 86 | Views/Tasks | Index, Details, Create, Edit, `_TaskForm` | TRUNG BÌNH |
| 87 | Views/ConstructionLogs | Index, Create, Edit, `_Form` | TRUNG BÌNH |
| 88–96 | Views/{Completion, Dashboard, Employees, Equipment, Home, Materials, Progress, Quality, Teams, Wbs} + `_ViewImports`, `_ViewStart` | (chi tiết Phần III) | THẤP |

## 2.8 Frontend assets & Tests

| STT | File | Chức năng | Liên quan |
|---|---|---|---|
| 97 | `wwwroot/css/site.css` | Layout sidebar/topbar/dashboard-card | TRUNG BÌNH |
| 98 | `wwwroot/js/site.js` | **Rỗng** (chỉ comment) | THẤP |
| 99 | `wwwroot/lib/*` | bootstrap 5.3.3, jquery 3.7.1, jquery-validation(-unobtrusive) | TRUNG BÌNH |
| 100 | `tests/.../ConstructionProjectManagement.Tests.csproj` | xUnit 2.9.3 + Test.Sdk 17.14.1 + coverlet | CAO (mẫu test) |
| 101 | `tests/.../Task02ProjectServiceTests.cs` | 8 test ProjectService/EmployeeService trên SQLite in-memory + MigrateAsync | CAO (mẫu test) |

## 2.9 Tài liệu trọng yếu

| STT | File | Nội dung | Liên quan |
|---|---|---|---|
| 102 | `docs/TASK03_Business_Description.md` | Mô tả hiện trạng nghiệp vụ hợp đồng/tài liệu (320 dòng) — **tài liệu quan trọng nhất cho TASK-03** | **CAO** |
| 103 | `TASK_FUNCTION_INVENTORY.md` | Kiểm kê 14 task, 65 function, 7 GAP, mapping Controller→Service→View | CAO |
| 104 | `AGENTS.md` | Quy ước bắt buộc: phân tầng, naming, tiếng Việt UI, không hardcode, không xóa cứng | CAO |
| 105 | `TASK_02_BUSINESS_RULES.md` | 15 quyết định nghiệp vụ QD-01..15 (nguồn chân lý TASK-02) | CAO |
| 106 | `PROJECT_STRUCTURE.md` | Cây thư mục chú thích (lỗi thời: nói "không có test project") | TRUNG BÌNH |
| 107 | `docs/01_Business_Analysis/Business_03_Contracts_and_Documents.md` | Nghiệp vụ hợp đồng — **lệch với code về unique ContractNumber** | CAO |
| 108 | `TASK_02_PHASE_1_REPORT.md` / `TASK_02_PHASE_2_REPORT.md` | Mẫu quy trình chuẩn khi làm module (migration → service → controller → view → report) | TRUNG BÌNH |
| 109 | README.md, docs/02…05, TASK02_* còn lại | Lỗi thời một phần (ghi SQL Server) — xem Phần III | THẤP |

**Tổng cộng:** 106 file `.cs` mã nguồn chính + 5 file migration SQLite + 62 view + 4 cấu hình + 2 asset + 38 tài liệu đã được kiểm kê; mức phân tích chi tiết ở Phần III.
