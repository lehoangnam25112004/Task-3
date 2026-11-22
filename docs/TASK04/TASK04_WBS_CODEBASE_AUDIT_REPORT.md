# BÁO CÁO RÀ SOÁT TOÀN DIỆN TASK-04 – WORK BREAKDOWN STRUCTURE (WBS)

**Phạm vi:** Discovery và audit chỉ đọc trên codebase, tài liệu, migration và schema SQLite hiện có.  
**Ngày rà soát:** 22/11/2026  
**Trạng thái:** Không sửa file, không chạy migration, không ghi dữ liệu database. Không chạy build/test vì đây là audit chỉ đọc và các lệnh đó có thể tạo/cập nhật artifact trong repository.

## PHẦN 1 – TỔNG QUAN TASK-04

### Mục tiêu

Đặc tả nghiệp vụ mô tả TASK-04 là chức năng tạo các giai đoạn trong dự án và phân rã phạm vi công việc thành cây WBS. Mỗi WorkItem có mã, tên, thứ tự, cấp độ, trạng thái, giai đoạn và có thể có WorkItem cha. Các quy tắc được nêu gồm: WorkItem và cha cùng Project; mã WBS duy nhất trong một Project; nút gốc có cấp 1; cấp nút con được tính từ cấp cha; thứ tự được lưu để hiển thị. WBS quản lý cấu trúc và trạng thái hạng mục, không thay thế nghiệp vụ giao việc, lịch chi tiết hoặc tiến độ.  
Tham chiếu: [Business_04_Phases_and_WBS.md](../../docs/01_Business_Analysis/Business_04_Phases_and_WBS.md).

### Phạm vi triển khai hiện tại

- Có Entity, EF Core mapping, service, MVC controller và các Razor View cho WBS.
- Có thể xem cấu trúc WBS, tạo giai đoạn và tạo WorkItem.
- Chưa tìm thấy thao tác sửa, xóa, lưu trữ, đổi thứ tự hoặc API riêng cho WBS.
- Trang hiển thị các WorkItem theo danh sách phẳng có thụt lề theo Level, chưa dựng cây đệ quy theo quan hệ cha–con.
- Có lỗi phạm vi truy cập đáng kể: truy vấn xem cây không kiểm tra phạm vi Project của người dùng.
- Tài liệu kiểm kê đánh dấu cả ba chức năng WBS là `NEEDS_TEST`; tài liệu tiến độ ghi code `DONE`, kiểm thử `PARTIAL` và chờ người dùng kiểm thử. Đây là trạng thái được ghi trong tài liệu, không phải bằng chứng nghiệm thu.

### Kết quả kiểm kê

Theo phương pháp đếm ở Phần 2, có **94 file liên quan**:

| Nhóm | Số file |
|---|---:|
| Trực tiếp với WBS | 22 |
| Gián tiếp: Project/Task/configuration/database | 18 |
| Gián tiếp cấp sau: các nơi tiêu thụ Task ID | 32 |
| Tài liệu | 22 |
| **Tổng cộng** | **94** |

## PHẦN 2 – DANH SÁCH FILE LIÊN QUAN

### Phương pháp đếm

Tính file có tham chiếu trực tiếp đến WBS, file nền tảng Project/Task/configuration/database trực tiếp hỗ trợ hoặc liên kết WBS, các nơi sử dụng Task ID ở tầng kế tiếp, và tài liệu mô tả hoặc truy vết các quan hệ đó. Không tính `bin/obj`, thư viện bên thứ ba, migration không liên quan, hoặc test không có tham chiếu WBS. Các file tiêu thụ Task ID được xếp gián tiếp cấp sau vì quan hệ của chúng là thông qua `WorkItem → Task`, không phải tham chiếu trực tiếp `WorkItem`.

### A. Direct – 22 file

| STT | File | Vai trò | Trạng thái |
|---:|---|---|---|
| 1 | [Controllers/WbsController.cs](../../Controllers/WbsController.cs) | Route xem và tạo WBS | Có; truy vấn xem thiếu kiểm tra phạm vi |
| 2 | [Services/WbsService.cs](../../Services/WbsService.cs) | Truy vấn cây và tạo phase/item | Có; truy vấn cây chưa kiểm tra phạm vi |
| 3 | [Services/Interfaces/IWbsService.cs](../../Services/Interfaces/IWbsService.cs) | Hợp đồng service | Chỉ có xem và tạo |
| 4 | [ViewModels/WbsViewModels.cs](../../ViewModels/WbsViewModels.cs) | ViewModel trang danh sách và form | Có |
| 5 | [Views/Wbs/Index.cshtml](../../Views/Wbs/Index.cshtml) | Hiển thị các phase và WorkItem | Có; danh sách phẳng, thụt lề theo Level |
| 6 | [Views/Wbs/CreatePhase.cshtml](../../Views/Wbs/CreatePhase.cshtml) | Form tạo phase | Có |
| 7 | [Views/Wbs/CreateItem.cshtml](../../Views/Wbs/CreateItem.cshtml) | Form tạo WorkItem | Có |
| 8 | [Models/ProjectPhase.cs](../../Models/ProjectPhase.cs) | Entity giai đoạn | Có |
| 9 | [Models/WorkItem.cs](../../Models/WorkItem.cs) | Entity WBS và quan hệ cha–con | Có |
| 10 | [Models/WbsItemStatus.cs](../../Models/WbsItemStatus.cs) | Enum trạng thái WorkItem | Có |
| 11 | [Models/Project.cs](../../Models/Project.cs) | Navigation `Phases` | Có |
| 12 | [Models/Task.cs](../../Models/Task.cs) | Navigation/khóa ngoại WorkItem tùy chọn | Có |
| 13 | [Data/ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs) | DbSet, quan hệ, index và hành vi xóa | Có |
| 14 | [Data/DbInitializer.cs](../../Data/DbInitializer.cs) | Seed mẫu có phase khi bật seed | Có |
| 15 | [Program.cs](../../Program.cs) | Đăng ký WBS service | Có |
| 16 | [Migrations.SqlServer/20261111081339_AddWbs.cs](../../Migrations.SqlServer/20261111081339_AddWbs.cs) | Migration schema SQL Server | Có; không phải migration đang chạy của SQLite |
| 17 | [Migrations.SqlServer/20261111081339_AddWbs.Designer.cs](../../Migrations.SqlServer/20261111081339_AddWbs.Designer.cs) | Metadata migration SQL Server | Có |
| 18 | [Migrations.SqlServer/ApplicationDbContextModelSnapshot.cs](../../Migrations.SqlServer/ApplicationDbContextModelSnapshot.cs) | Snapshot SQL Server | Có |
| 19 | [Migrations/20261111092125_InitialSqlite.cs](../../Migrations/20261111092125_InitialSqlite.cs) | SQLite initial migration, gồm WBS và Tasks | Có |
| 20 | [Migrations/ApplicationDbContextModelSnapshot.cs](../../Migrations/ApplicationDbContextModelSnapshot.cs) | Snapshot SQLite | Có |
| 21 | [Views/Shared/_Layout.cshtml](../../Views/Shared/_Layout.cshtml) | Liên kết điều hướng WBS | Có |
| 22 | [Views/Projects/Details.cshtml](../../Views/Projects/Details.cshtml) | Liên kết từ trang chi tiết Project sang WBS | Có |

### B. Indirect – 18 file nền tảng và luồng Project/Task

| STT | File | Vai trò |
|---:|---|---|
| 23 | [Models/ProjectMember.cs](../../Models/ProjectMember.cs) | Thành viên Project; dùng trong kiểm tra phạm vi khi ghi |
| 24 | [Models/ApplicationUser.cs](../../Models/ApplicationUser.cs) | Người dùng Identity và quan hệ thành viên Project |
| 25 | [Models/RoleNames.cs](../../Models/RoleNames.cs) | Tên role; không có ma trận quyền WBS riêng |
| 26 | [Services/ProjectService.cs](../../Services/ProjectService.cs) | Lấy danh sách Project và kiểm tra phạm vi |
| 27 | [Services/Interfaces/IProjectService.cs](../../Services/Interfaces/IProjectService.cs) | Hợp đồng dịch vụ Project |
| 28 | [Controllers/ProjectsController.cs](../../Controllers/ProjectsController.cs) | Luồng Project và liên kết tới WBS |
| 29 | [Controllers/TasksController.cs](../../Controllers/TasksController.cs) | Nạp WorkItem làm lựa chọn trong form Task |
| 30 | [Services/TaskService.cs](../../Services/TaskService.cs) | Kiểm tra WorkItem thuộc cùng Project với Task |
| 31 | [Services/Interfaces/ITaskService.cs](../../Services/Interfaces/ITaskService.cs) | Hợp đồng dịch vụ Task |
| 32 | [ViewModels/TaskViewModels.cs](../../ViewModels/TaskViewModels.cs) | ViewModel Task có tham chiếu WBS tùy chọn |
| 33 | [Views/Tasks/Create.cshtml](../../Views/Tasks/Create.cshtml) | Tạo Task |
| 34 | [Views/Tasks/Edit.cshtml](../../Views/Tasks/Edit.cshtml) | Sửa Task |
| 35 | [Views/Tasks/_TaskForm.cshtml](../../Views/Tasks/_TaskForm.cshtml) | Dropdown WBS trên form Task |
| 36 | [Views/Tasks/Index.cshtml](../../Views/Tasks/Index.cshtml) | Hiển thị mã WBS đã liên kết |
| 37 | [Views/Tasks/Details.cshtml](../../Views/Tasks/Details.cshtml) | Hiển thị WBS liên kết |
| 38 | [appsettings.json](../../appsettings.json) | Chuỗi kết nối SQLite |
| 39 | [ConstructionProjectManagement.csproj](../../ConstructionProjectManagement.csproj) | Provider SQLite và cấu hình migration |
| 40 | [Data/Databases/ConstructionProjectManagement.db](../../Data/Databases/ConstructionProjectManagement.db) | Database SQLite hiện tại; chỉ kiểm tra schema/history ở chế độ chỉ đọc |

### C. Indirect cấp sau – 32 file tiêu thụ Task

Các file dưới đây liên quan qua `Task`/`TaskId`, không tham chiếu trực tiếp WorkItem.

| Nhóm | File | Vai trò |
|---|---|---|
| Models (9) | [Models/EquipmentAssignment.cs](../../Models/EquipmentAssignment.cs), [Models/Incident.cs](../../Models/Incident.cs), [Models/InspectionRequest.cs](../../Models/InspectionRequest.cs), [Models/MaterialRequest.cs](../../Models/MaterialRequest.cs), [Models/MaterialTransaction.cs](../../Models/MaterialTransaction.cs), [Models/RfiRequest.cs](../../Models/RfiRequest.cs), [Models/TaskAssignment.cs](../../Models/TaskAssignment.cs), [Models/TaskDependency.cs](../../Models/TaskDependency.cs), [Models/TaskProgressHistory.cs](../../Models/TaskProgressHistory.cs) | Dữ liệu liên kết Task, phân công, tiến độ, vật tư, thiết bị và chất lượng |
| Services (7) | [Services/CompletionService.cs](../../Services/CompletionService.cs), [Services/DashboardService.cs](../../Services/DashboardService.cs), [Services/EquipmentService.cs](../../Services/EquipmentService.cs), [Services/Interfaces/IProgressService.cs](../../Services/Interfaces/IProgressService.cs), [Services/ProgressService.cs](../../Services/ProgressService.cs), [Services/MaterialService.cs](../../Services/MaterialService.cs), [Services/QualityService.cs](../../Services/QualityService.cs) | Xử lý nghiệp vụ Task/tiến độ/tài nguyên/chất lượng |
| Controllers (5) | [Controllers/CompletionController.cs](../../Controllers/CompletionController.cs), [Controllers/EquipmentController.cs](../../Controllers/EquipmentController.cs), [Controllers/MaterialsController.cs](../../Controllers/MaterialsController.cs), [Controllers/ProgressController.cs](../../Controllers/ProgressController.cs), [Controllers/QualityController.cs](../../Controllers/QualityController.cs) | Endpoint cho nghiệp vụ gắn với Task |
| ViewModels (5) | [ViewModels/CompletionViewModels.cs](../../ViewModels/CompletionViewModels.cs), [ViewModels/EquipmentViewModels.cs](../../ViewModels/EquipmentViewModels.cs), [ViewModels/MaterialViewModels.cs](../../ViewModels/MaterialViewModels.cs), [ViewModels/ProgressViewModels.cs](../../ViewModels/ProgressViewModels.cs), [ViewModels/QualityViewModels.cs](../../ViewModels/QualityViewModels.cs) | Form/data contract dùng Task |
| Views (6) | [Views/Equipment/Assign.cshtml](../../Views/Equipment/Assign.cshtml), [Views/Materials/Request.cshtml](../../Views/Materials/Request.cshtml), [Views/Progress/Index.cshtml](../../Views/Progress/Index.cshtml), [Views/Quality/CreateIncident.cshtml](../../Views/Quality/CreateIncident.cshtml), [Views/Quality/CreateInspection.cshtml](../../Views/Quality/CreateInspection.cshtml), [Views/Quality/CreateRfi.cshtml](../../Views/Quality/CreateRfi.cshtml) | Giao diện nghiệp vụ liên kết Task |

### D. Tài liệu – 22 file

| STT | File | Vai trò / tình trạng |
|---:|---|---|
| 1 | [docs/01_Business_Analysis/Business_01_Accounts_and_Authorization.md](../01_Business_Analysis/Business_01_Accounts_and_Authorization.md) | Bối cảnh Identity/role; không có ma trận quyền WBS |
| 2 | [docs/01_Business_Analysis/Business_02_Project_Management.md](../01_Business_Analysis/Business_02_Project_Management.md) | Quy tắc Project và phạm vi |
| 3 | [docs/01_Business_Analysis/Business_04_Phases_and_WBS.md](../01_Business_Analysis/Business_04_Phases_and_WBS.md) | Đặc tả nghiệp vụ WBS chính |
| 4 | [docs/01_Business_Analysis/Business_05_Task_Management.md](../01_Business_Analysis/Business_05_Task_Management.md) | Quan hệ WBS/Task và cùng Project |
| 5 | [docs/01_Business_Analysis/Business_06_Schedule_and_Progress.md](../01_Business_Analysis/Business_06_Schedule_and_Progress.md) | Tiến độ gắn Task |
| 6 | [docs/02_System_Design/System_Architecture.md](../02_System_Design/System_Architecture.md) | Kiến trúc lớp; mô tả SQL Server |
| 7 | [docs/02_System_Design/System_Design.md](../02_System_Design/System_Design.md) | Vị trí module/service |
| 8 | [docs/03_Database/Data_Dictionary.md](../03_Database/Data_Dictionary.md) | Tên bảng/entity WBS |
| 9 | [docs/03_Database/Database_Design.md](../03_Database/Database_Design.md) | Thiết kế DB; ghi SQL Server |
| 10 | [docs/05_Project_Management/Initial_Project_Plan.md](../05_Project_Management/Initial_Project_Plan.md) | Kế hoạch ban đầu; ghi SQL Server |
| 11 | [docs/05_Project_Management/Implementation_Progress.md](../05_Project_Management/Implementation_Progress.md) | Trạng thái code/test TASK-04 |
| 12 | [docs/06_Diagrams/03_Code_Dependencies.mmd](../06_Diagrams/03_Code_Dependencies.mmd) | Sơ đồ Controller→Service→DbContext |
| 13 | [docs/06_Diagrams/04_Database_ERD.mmd](../06_Diagrams/04_Database_ERD.mmd) | ERD Phase/WorkItem/Task |
| 14 | [docs/06_Diagrams/05_Use_Case_Diagram.mmd](../06_Diagrams/05_Use_Case_Diagram.mmd) | Ngữ cảnh use case |
| 15 | [docs/06_Diagrams/07_Process_01_Project_Planning.mmd](../06_Diagrams/07_Process_01_Project_Planning.mmd) | Luồng lập kế hoạch có tạo WBS |
| 16 | [docs/06_Diagrams/12_Process_06_Project_Handover.mmd](../06_Diagrams/12_Process_06_Project_Handover.mmd) | Luồng bàn giao có nhắc WBS |
| 17 | [docs/07_Testing/Integration_Test_Matrix.md](../07_Testing/Integration_Test_Matrix.md) | Trạng thái luồng WBS chờ kiểm thử người dùng |
| 18 | [PROJECT_STRUCTURE.md](../../PROJECT_STRUCTURE.md) | Cấu trúc repository/module |
| 19 | [TASK_FUNCTION_INVENTORY.md](../../TASK_FUNCTION_INVENTORY.md) | Kiểm kê chức năng; WBS `NEEDS_TEST` |
| 20 | [TASK03_CODEBASE_DEPENDENCY_AUDIT.md](../../TASK03_CODEBASE_DEPENDENCY_AUDIT.md) | Bản đồ dependency; mô tả scope WBS chưa đầy đủ |
| 21 | [TASK_02_DATABASE_GAP_ANALYSIS.md](../../TASK_02_DATABASE_GAP_ANALYSIS.md) | Quy tắc Project/member ảnh hưởng scope |
| 22 | [TASK_02_DEEP_AUDIT.md](../../TASK_02_DEEP_AUDIT.md) | Phạm vi truy cập Project và liên kết WBS |

Không tìm thấy SRS/BRD riêng cho TASK-04, ma trận quyền WBS được phê duyệt, bộ tiêu chí nghiệm thu chính thức hoặc báo cáo test chuyên biệt WBS. [AGENTS.md](../../AGENTS.md) dẫn tới `../BuildManagerMVC/Rule.md`; file đó không có tại vị trí được dẫn trong workspace.

## PHẦN 3 – KIẾN TRÚC HIỆN TẠI

### Luồng xử lý

**Xem:** Razor `/Wbs` → `WbsController.Index` → `IWbsService.GetProjectTreeAsync` → `WbsService` → `ApplicationDbContext` → SQLite.  
**Tạo:** form Razor → POST `WbsController` → `WbsService.CreatePhaseAsync` hoặc `CreateWorkItemAsync` → EF Core → SQLite.

### Cấu trúc dữ liệu

- `Project` có tập hợp `ProjectPhase`.
- `ProjectPhase` thuộc một Project và có danh sách WorkItem.
- `WorkItem` thuộc Project và Phase, có thể tham chiếu một WorkItem cha qua `ParentWorkItemId`.
- `Task` có thể liên kết tới một WorkItem qua `WorkItemId`.

### Giao diện

Trang WBS tải phase và item theo thứ tự; giao diện lặp qua các item trong từng phase và thụt lề dựa vào Level. Không thấy dựng cây đệ quy từ quan hệ cha–con. Không tìm thấy JavaScript/TypeScript, AJAX, kéo-thả, modal chỉnh sửa, tìm kiếm hoặc phân trang riêng cho WBS.

## PHẦN 4 – DANH MỤC CHỨC NĂNG

| Chức năng | Yêu cầu nghiệp vụ | Implementation | File liên quan | Đánh giá |
|---|---|---|---|---|
| Xem cây WBS | Nạp phase/item theo phạm vi Project | Có truy vấn theo ID, nhưng không lọc theo quyền người dùng | [WbsController.cs](../../Controllers/WbsController.cs), [WbsService.cs](../../Services/WbsService.cs), [Index.cshtml](../../Views/Wbs/Index.cshtml) | **Partially Implemented**; lỗi phạm vi truy cập |
| Tạo phase | Phase thuộc Project hợp lệ | Controller và service tạo phase; có kiểm tra quyền ghi | [WbsController.cs](../../Controllers/WbsController.cs), [WbsService.cs](../../Services/WbsService.cs) | **Implemented**, nhưng chính sách vai trò chưa được xác nhận |
| Tạo WorkItem | Kiểm tra Project/phase/parent, level, mã duy nhất | Service kiểm tra phase/parent cùng Project, tính Level, kiểm tra mã; DB có unique index | [WbsService.cs](../../Services/WbsService.cs), [WorkItem.cs](../../Models/WorkItem.cs), [ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs) | **Implemented**, còn thiếu kiểm thử và có rủi ro race |
| Sửa phase/item | Không nêu rõ trong Business 04 | Không thấy endpoint/service sửa | [IWbsService.cs](../../Services/Interfaces/IWbsService.cs) | Yêu cầu **Cannot Verify**; hiện chưa triển khai |
| Xóa/lưu trữ phase/item | Không nêu rõ vòng đời | Không thấy endpoint; FK hạn chế một số thao tác xóa | [ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs) | Yêu cầu **Cannot Verify**; cần quyết định nghiệp vụ |
| Cập nhật trạng thái | WorkItem có trường trạng thái | Chọn trạng thái khi tạo; không thấy luồng cập nhật/chuyển trạng thái hoặc đồng bộ Task | [WorkItem.cs](../../Models/WorkItem.cs), [CreateItem.cshtml](../../Views/Wbs/CreateItem.cshtml) | **Partially Implemented** |
| Tạo mã WBS | Mã duy nhất trong Project; chưa chỉ định format/generation | Người dùng nhập mã; service kiểm tra và DB có unique index | [WbsService.cs](../../Services/WbsService.cs), [ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs) | Tính duy nhất **Implemented**; format/generation **Cannot Verify** |
| Thứ tự/cây phân cấp | Lưu thứ tự và quan hệ cha–con | Có SequenceOrder/ParentId/Level; trang hiển thị phẳng | [WorkItem.cs](../../Models/WorkItem.cs), [Index.cshtml](../../Views/Wbs/Index.cshtml) | **Partially Implemented** |
| Tìm kiếm/lọc/xuất | Không tìm thấy yêu cầu | Không thấy chức năng WBS tương ứng | — | **Cannot Verify** |
| Liên kết Task–WBS | Task có thể gắn WBS; phải cùng Project | TaskService kiểm tra WorkItem cùng Project | [TaskService.cs](../../Services/TaskService.cs), [TasksController.cs](../../Controllers/TasksController.cs) | **Implemented** trong luồng Task; chưa có test WBS riêng |

## PHẦN 5 – PHÂN QUYỀN

Không có tài liệu ma trận quyền WBS được xác nhận. Bảng dưới đây chỉ ghi nhận **hành vi code hiện tại**, không khẳng định đó là chính sách nghiệp vụ đã duyệt.

| Vai trò | Xem WBS theo Project ID | Tạo phase/item | Ghi chú |
|---|---|---|---|
| Admin | Có thể xem mọi Project do thiếu kiểm tra scope | Controller cho phép; service coi là bypass | Implementation hiện tại |
| Project Director | Có thể xem mọi Project do thiếu kiểm tra scope | Controller cho phép; được truyền như cờ bypass | Quyền toàn cục chưa có căn cứ đặc tả |
| Project Manager | Có thể xem mọi Project do thiếu kiểm tra scope | Chỉ khi là PM của Project hoặc thành viên active theo service | Đọc và ghi không đồng nhất |
| Site Manager | Có thể xem mọi Project do thiếu kiểm tra scope | Bị role gate từ chối | Chưa có ma trận nghiệp vụ |
| Supervising Engineer | Có thể xem mọi Project do thiếu kiểm tra scope | Bị role gate từ chối | Chưa có ma trận nghiệp vụ |
| Materials Staff | Có thể xem mọi Project do thiếu kiểm tra scope | Bị role gate từ chối | Chưa có ma trận nghiệp vụ |
| Worker | Có thể xem mọi Project do thiếu kiểm tra scope | Bị role gate từ chối | Chưa có ma trận nghiệp vụ |
| Client Representative | Có thể xem mọi Project do thiếu kiểm tra scope | Bị role gate từ chối | Chưa có ma trận nghiệp vụ |

`[Authorize]` yêu cầu đăng nhập. Các action tạo giới hạn ở Admin/Project Director/Project Manager; service kiểm tra thêm quyền Project khi ghi. Riêng truy vấn cây không sử dụng `userId`/`isAdmin` để giới hạn dữ liệu. Chưa tìm thấy authorization policy riêng hoặc resource-based authorization cho WBS.

## PHẦN 6 – DATABASE

### Bảng và cột chính

- **ProjectPhases:** `Id` (PK), `ProjectId`, `Name`, `SequenceOrder`, `Status`.
- **WorkItems:** `Id` (PK), `ProjectId`, `ProjectPhaseId`, `ParentWorkItemId` (nullable), `WbsCode`, `Name`, `Level`, `SequenceOrder`, `Status`.
- **Tasks:** có `ProjectId` và `WorkItemId` tùy chọn.

### Khóa ngoại, index và ràng buộc

- `ProjectPhases.ProjectId → Projects.Id`, `ON DELETE CASCADE`.
- `WorkItems.ProjectId → Projects.Id`, `ProjectPhaseId → ProjectPhases.Id`, `ParentWorkItemId → WorkItems.Id`, đều `ON DELETE RESTRICT`.
- Unique index `(ProjectId, WbsCode)`.
- Index cho `ParentWorkItemId`, `ProjectPhaseId`, và `(ProjectId, SequenceOrder)` ở ProjectPhases.
- `Tasks.WorkItemId → WorkItems.Id`, `ON DELETE SET NULL`; có index theo WorkItemId.
- Không thấy composite FK bảo đảm phase/parent thuộc cùng Project với WorkItem; không thấy check constraint cho Level/SequenceOrder. Các kiểm tra cùng Project chủ yếu nằm ở service.

### Migration và nguy cơ toàn vẹn

Database SQLite hiện có bảy migration đã áp dụng, từ `20261111092125_InitialSqlite` đến `20261121150000_Task03DocumentManagementCompletion`. WBS nằm trong SQLite initial migration. Có migration `AddWbs` riêng cho SQL Server nhưng project hiện chạy SQLite và loại migration SQL Server khỏi biên dịch.

Nếu xóa WorkItem đang được Task tham chiếu, FK sẽ đặt `Task.WorkItemId` thành `NULL`, làm mất liên kết WBS. Xóa WorkItem cha có con bị FK `RESTRICT` ngăn cản. Xóa Phase có WorkItem cũng bị hạn chế. Hiện không có endpoint xóa; không nên thêm trước khi chốt quy tắc lưu trữ/xóa.

## PHẦN 7 – TÍCH HỢP VỚI TASK KHÁC

- **TASK-02 / Project:** Phase và WorkItem thuộc Project. Trang chi tiết Project có liên kết tới WBS. Lỗi truy vấn cây hiện bỏ qua phạm vi Project.
- **TASK-05 / Task:** `Task.WorkItemId` là tùy chọn; `TaskService` kiểm tra WorkItem cùng Project. FK đơn trong DB không tự bảo đảm điều kiện này.
- **TASK-06 / Tiến độ:** dữ liệu tiến độ gắn với Task, không trực tiếp với WorkItem. Không tìm thấy việc tự đồng bộ trạng thái WBS với tiến độ Task.
- **Nguồn lực, chất lượng, nghiệm thu và báo cáo:** phần lớn gắn với Task/TaskId, không trực tiếp với WorkItem. Đây là phụ thuộc gián tiếp qua Task.
- Thay đổi ID, ràng buộc Project, quy tắc cha–con hoặc xử lý xóa có thể ảnh hưởng liên kết Task–WBS và các nghiệp vụ Task downstream. Cần giữ nguyên hoặc có kế hoạch chuyển đổi liên kết hiện hữu.

## PHẦN 8 – DANH SÁCH VẤN ĐỀ VÀ RỦI RO

| ID | Vấn đề | Mức độ | Bằng chứng | Đề xuất |
|---|---|---|---|---|
| WBS-01 | Truy vấn cây không dùng `userId`/`isAdmin`; người dùng đã đăng nhập có thể đọc WBS Project khác nếu biết ID. | **High** | [WbsController.cs:38](../../Controllers/WbsController.cs#L38), [WbsService.cs:10](../../Services/WbsService.cs#L10) | Kiểm tra phạm vi trong service trước khi trả dữ liệu; bổ sung test truy cập trực tiếp theo ID. |
| WBS-02 | GET tạo WorkItem cũng nạp cây bằng truy vấn không giới hạn; có thể lộ lựa chọn phase/item của Project khác dù POST có kiểm tra quyền. | **High** | [WbsController.cs:90](../../Controllers/WbsController.cs#L90), [WbsService.cs:53](../../Services/WbsService.cs#L53) | Bảo vệ cả GET và POST bằng cùng chính sách truy cập Project. |
| WBS-03 | Quyền WBS chưa được đặc tả; Project Director được xử lý như bypass toàn cục khi tạo. | **Medium** | [WbsController.cs:46](../../Controllers/WbsController.cs#L46), [WbsService.cs:96](../../Services/WbsService.cs#L96) | Xác nhận ma trận quyền và phạm vi Project trước khi sửa authorization. |
| WBS-04 | Giao diện là danh sách phẳng, không dựng cây đệ quy; thứ tự toàn danh sách có thể không phản ánh đúng thứ tự phân cấp. | **Medium** | [Views/Wbs/Index.cshtml:21](../../Views/Wbs/Index.cshtml#L21) | Chốt thứ tự hiển thị và dựng từ quan hệ cha–con nếu đó là yêu cầu. |
| WBS-05 | Không có sửa/xóa/archive/reorder; Business 04 chưa xác nhận các thao tác này có thuộc scope hay không. | **Medium** | [IWbsService.cs](../../Services/Interfaces/IWbsService.cs), [TASK_FUNCTION_INVENTORY.md:281](../../TASK_FUNCTION_INVENTORY.md#L281) | Hỏi nghiệp vụ, không tự suy diễn thêm CRUD. |
| WBS-06 | Ràng buộc cùng Project của phase/parent phụ thuộc vào service, không được biểu diễn đầy đủ bằng composite FK. | **Medium** | [ApplicationDbContext.cs:330](../../Data/ApplicationDbContext.cs#L330), [WbsService.cs:59](../../Services/WbsService.cs#L59), [TaskService.cs:154](../../Services/TaskService.cs#L154) | Đánh giá có cần DB constraint; kiểm tra dữ liệu trước mọi migration. |
| WBS-07 | Kiểm tra mã trùng trước khi lưu có thể gặp race khi tạo đồng thời; unique index chặn dữ liệu trùng nhưng lỗi DB có thể chưa được chuyển thành lỗi nghiệp vụ rõ ràng. | **Medium** | [WbsService.cs:81](../../Services/WbsService.cs#L81), [ApplicationDbContext.cs:348](../../Data/ApplicationDbContext.cs#L348) | Xác định xử lý xung đột và kiểm thử tạo đồng thời. |
| WBS-08 | Có trạng thái khi tạo nhưng không thấy luồng cập nhật/chuyển trạng thái hoặc quy tắc đồng bộ với Task; nhãn trạng thái có thể hiển thị giá trị enum. | **Low** | [WorkItem.cs](../../Models/WorkItem.cs), [Views/Wbs/CreateItem.cshtml](../../Views/Wbs/CreateItem.cshtml), [Views/Wbs/Index.cshtml](../../Views/Wbs/Index.cshtml) | Chốt vòng đời trạng thái và nhãn tiếng Việt. |
| WBS-09 | Xóa WorkItem được Task tham chiếu sẽ làm `Task.WorkItemId` thành null; xóa cha có con bị RESTRICT. | **Medium** | Schema SQLite và quan hệ trong [ApplicationDbContext.cs](../../Data/ApplicationDbContext.cs) | Chốt archive/xóa/tái liên kết trước khi triển khai xóa. |
| WBS-10 | Tài liệu kiến trúc/DB ghi SQL Server, trong khi ứng dụng và DB hiện tại dùng SQLite. | **Low** | [Database_Design.md](../03_Database/Database_Design.md), [Program.cs](../../Program.cs), [appsettings.json](../../appsettings.json) | Đồng bộ tài liệu và xác định migration nào là nguồn chuẩn. |
| WBS-11 | Không tìm thấy test WBS chuyên biệt; kiểm kê chức năng ghi `NEEDS_TEST`. | **Medium** | [tests/ConstructionProjectManagement.Tests/](../../tests/ConstructionProjectManagement.Tests/), [TASK_FUNCTION_INVENTORY.md:107](../../TASK_FUNCTION_INVENTORY.md#L107) | Bổ sung test sau khi quy tắc nghiệp vụ và quyền được chốt. |

## PHẦN 9 – ĐIỂM NGHIỆP VỤ CẦN XÁC NHẬN

### Đã được tài liệu xác nhận

- WBS phân rã công việc theo Project thành phase và các WorkItem phân cấp.
- WorkItem và cha phải thuộc cùng Project.
- Mã WBS duy nhất trong Project.
- Level nút gốc bằng 1; Level con tính theo cha.
- Có lưu thứ tự.
- Task có thể liên kết WBS và cần cùng Project.
- WBS không thay thế giao việc, lịch chi tiết, phần trăm hoàn thành hoặc lịch sử tiến độ.

### Đã thấy trong code, nhưng không đồng nghĩa đã được nghiệp vụ phê duyệt

- Role hiện tại được phép tạo là Admin, Project Director, Project Manager.
- Quyền ghi phụ thuộc Project Manager hoặc thành viên active, trừ đường bypass Admin/Project Director.
- Có enum trạng thái, quan hệ cha–con và liên kết Task–WBS.
- Tài liệu tiến độ tự ghi code `DONE`, testing `PARTIAL`; chưa có kết quả test WBS để xác minh nghiệm thu.

### Cần người dùng/nghiệp vụ quyết định

1. Role nào được xem, tạo, sửa, đổi trạng thái, sắp xếp, archive hoặc xóa WBS? Project Director có quyền toàn cục hay theo Project?
2. Sửa/xóa/archive/reorder có nằm trong TASK-04 không?
3. Mã WBS do người dùng nhập hay tự sinh? Quy tắc format, phân biệt hoa-thường và tái sử dụng mã là gì?
4. Cha và con có bắt buộc ở cùng phase không? Tài liệu hiện chỉ nói cùng Project.
5. `SequenceOrder` áp dụng giữa các sibling, trong phase hay toàn cây?
6. Task được gắn ở mọi cấp WBS hay chỉ cấp cụ thể? Trạng thái WBS độc lập với tiến độ Task hay có đồng bộ?
7. Nếu xóa, xử lý WorkItem con và Task đang tham chiếu thế nào?
8. Khi Project archive hoặc thành viên bị inactive, quyền xem/sửa WBS được xử lý ra sao?

Không có quyết định nghiệp vụ nào được tự suy diễn trong audit này.

## PHẦN 10 – KẾ HOẠCH HOÀN THIỆN TASK-04

| Giai đoạn | Phạm vi | Phụ thuộc |
|---|---|---|
| 1. Xác nhận nghiệp vụ | Chốt role/scope, lifecycle, format mã, thứ tự, trạng thái, quan hệ phase-cha và xóa/archive | Quyết định của nghiệp vụ |
| 2. Thiết kế kiến trúc | Chốt truy vấn cây, mô hình đọc và ranh giới kiểm tra quyền Project | Giai đoạn 1 |
| 3. Database | Đánh giá constraint/index, hành vi xóa và chênh lệch SQLite/SQL Server; kiểm tra dữ liệu trước migration | Giai đoạn 1–2 |
| 4. Backend | Triển khai đúng các thao tác đã duyệt; validation và xử lý xung đột mã trùng | Quy tắc và thiết kế được duyệt |
| 5. Frontend | Hiển thị cây và chỉ bổ sung thao tác đã được chốt; chuẩn hóa nhãn trạng thái tiếng Việt | API/backend contract |
| 6. Authorization | Bảo vệ mọi GET/POST/direct URL theo role và Project scope ở server | Chính sách ở giai đoạn 1, code backend |
| 7. Kiểm thử chức năng | Kiểm thử phase, cây nhiều cấp, thứ tự, Project/parent, mã trùng, trạng thái và Task link | Giai đoạn 3–6 |
| 8. Kiểm thử bảo mật/hồi quy | Thử truy cập chéo Project, membership inactive, URL trực tiếp và luồng Task downstream | Authorization và test chức năng |
| 9. Nghiệm thu | Kiểm thử end-to-end và cập nhật inventory/diagram theo hành vi đã xác minh | Hoàn tất các giai đoạn trước |

## KẾT LUẬN

- **Tổng số file liên quan:** 94 (22 trực tiếp, 50 gián tiếp gồm 18 dependency chính và 32 consumer Task cấp sau, 22 tài liệu).
- **Mức triển khai:** nền tảng xem/tạo đã có; chưa đủ bằng chứng để kết luận hoàn thiện hoặc nghiệm thu.
- **Rủi ro ưu tiên:** đọc chéo Project do truy vấn WBS không kiểm tra scope.
- **Thiếu cần làm rõ:** chính sách quyền WBS, vòng đời chỉnh sửa/xóa/archive, quy tắc mã/thứ tự/trạng thái và kiểm thử chuyên biệt.
- **Trạng thái bảo toàn:** audit chỉ đọc; không thay đổi file, migration hay dữ liệu. Thư mục `App_Data/Documents/2026/` là trạng thái untracked đã tồn tại trước audit và không bị tác động.
