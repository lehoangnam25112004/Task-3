# Mô tả nghiệp vụ TASK-03 — Quản lý hợp đồng và tài liệu

> **Phạm vi tài liệu:** Mô tả hiện trạng suy ra từ source code, tài liệu trong repository và một lần kiểm tra SQLite chỉ đọc. Đây không phải biên bản kiểm thử chấp nhận. Không chạy ứng dụng (startup có áp dụng migration), không chạy migration, không tạo dữ liệu và không thay đổi source code hoặc database trong quá trình khảo sát.
>
> **Cách hiểu trạng thái:** `ĐÃ TRIỂN KHAI` nghĩa là có luồng code tương ứng, không khẳng định đã kiểm thử end-to-end. `TRIỂN KHAI MỘT PHẦN` nghĩa là có một phần luồng nhưng còn giới hạn hoặc điểm không nhất quán. `CHƯA TRIỂN KHAI` nghĩa là đã khảo sát nhưng không tìm thấy chức năng trong source hiện tại. `CHƯA ĐỦ BẰNG CHỨNG` nghĩa là chưa thể kết luận hành vi hoặc quyền.

## 1. Tổng quan

### 1.1 Mục đích và phạm vi

TASK-03 cung cấp các chức năng MVC để quản lý hợp đồng gắn với dự án và tải tài liệu lên/tải tài liệu xuống. Source hiện tại có danh sách, chi tiết, tạo và cập nhật hợp đồng; tài liệu được tải lên từ trang chi tiết hợp đồng và được lưu metadata trong SQLite, nội dung tệp trong thư mục lưu trữ riêng.

Chưa tìm thấy luồng phê duyệt hợp đồng, quản lý phụ lục/điều chỉnh, lịch sử phiên bản, xóa/lưu trữ qua giao diện, hay trang quản trị tài liệu độc lập.

### 1.2 Đối tượng sử dụng

- **Admin** và **Project Director**: được đưa vào các role gate của thao tác tạo/cập nhật hợp đồng và tải tài liệu. Admin/Project Director được coi là có phạm vi dự án rộng trong các truy vấn hợp đồng và tải xuống. Riêng tải lên còn có kiểm tra thành viên dự án ở service, nên không thể kết luận họ luôn tải lên được mọi dự án.
- **Project Manager**: được đưa vào role gate cho tạo/cập nhật hợp đồng và tải tài liệu; service vẫn yêu cầu người dùng là Project Manager của dự án hoặc thành viên dự án đang hoạt động.
- **Các vai trò khác**: không được phép qua role gate của thao tác ghi/tải lên; người đã đăng nhập có thể xem/tải xuống nếu đạt điều kiện phạm vi thành viên dự án trong service.
- **Người chưa đăng nhập**: bị chặn bởi `[Authorize]` trên `ContractsController`.

Các vai trò được khai báo trong `RoleNames.All`: Admin, Project Director, Project Manager, Site Manager, Supervising Engineer, Worker, Materials Staff và Client Representative. Quyền ở đây được mô tả theo các role gate và điều kiện service đang có, không suy luận từ tên role.

### 1.3 Mối liên hệ với module khác

- TASK-02 cung cấp `Project`, `ProjectManagerId` và `ProjectMember` để liên kết hợp đồng/tài liệu và giới hạn phạm vi truy cập.
- ASP.NET Core Identity cung cấp người dùng, đăng nhập và role.
- `ProjectDocument.UploadedById` liên kết tới người dùng Identity; hiện chưa thấy audit log hoặc lịch sử thao tác riêng cho tài liệu.

## 2. Danh sách chức năng

Các mã FN-03-01 đến FN-03-06 lấy từ [TASK_FUNCTION_INVENTORY.md](../TASK_FUNCTION_INVENTORY.md). Trạng thái dưới đây đánh giá sự hiện diện và tính đầy đủ của luồng trong source, không phải kết quả PASS kiểm thử.

| Mã chức năng | Tên chức năng | Mô tả hiện trạng | Đối tượng sử dụng theo code | Trạng thái triển khai |
|---|---|---|---|---|
| FN-03-01 | Xem danh sách hợp đồng | Hiển thị hợp đồng chưa archive thuộc phạm vi truy cập; bảng có số hợp đồng, dự án, giá trị, ngày hiệu lực và trạng thái. | Người đăng nhập có quyền xem theo phạm vi dự án. | ĐÃ TRIỂN KHAI |
| FN-03-02 | Xem chi tiết hợp đồng | Hiển thị thông tin hợp đồng và danh sách tài liệu gắn với hợp đồng. | Người đăng nhập có quyền xem theo phạm vi dự án. | ĐÃ TRIỂN KHAI |
| FN-03-03 | Tạo hợp đồng | Chọn dự án, nhập thông tin hợp đồng; Controller kiểm tra ModelState và Service kiểm tra dự án, số hợp đồng, giá trị và ngày. | Admin, Project Director, Project Manager; PM còn bị giới hạn theo dự án. | ĐÃ TRIỂN KHAI |
| FN-03-04 | Cập nhật hợp đồng | Có GET/POST Edit và `ContractService.UpdateAsync`; service tải bản ghi theo ID mà không kiểm tra phạm vi dự án hiện tại trước khi cập nhật. | Admin, Project Director, Project Manager theo Controller; PM cần phạm vi trên dự án đích. | TRIỂN KHAI MỘT PHẦN — còn rủi ro kiểm tra quyền trên bản ghi nguồn; chưa kiểm thử động. |
| FN-03-05 | Tải tài liệu lên | Form trên trang Details; kiểm tra phần mở rộng, dung lượng, dự án, liên kết hợp đồng và thành viên; lưu file và metadata. Role gate Controller cho Admin/Director/PM nhưng service không có nhánh bỏ qua kiểm tra thành viên cho Admin/Director. | Admin, Project Director, Project Manager theo Controller; service còn yêu cầu là PM của dự án hoặc thành viên active. | TRIỂN KHAI MỘT PHẦN — có khả năng role gate và service không nhất quán; chưa kiểm thử động. |
| FN-03-06 | Tải tài liệu xuống | Truy vấn document chưa archive, kiểm tra phạm vi truy cập và file vật lý tồn tại; trả file hoặc 404. | Người đăng nhập; Admin/Director bỏ qua kiểm tra thành viên, các role khác cần là PM của dự án hoặc thành viên active. | ĐÃ TRIỂN KHAI |
| — | Tải tài liệu gắn trực tiếp dự án, không gắn hợp đồng | `UploadAsync` nhận `contractId` nullable, nhưng UI/Controller hiện tải từ một hợp đồng cụ thể; chưa thấy luồng giao diện tải tài liệu project-only. | Chưa xác định cho luồng giao diện. | TRIỂN KHAI MỘT PHẦN |
| — | Danh sách/chi tiết tài liệu độc lập | Tài liệu chỉ được liệt kê trong trang chi tiết hợp đồng; không thấy trang danh sách hoặc chi tiết riêng cho document. | — | CHƯA TRIỂN KHAI |
| — | Cập nhật metadata, xóa hoặc archive tài liệu | Không tìm thấy action/service tương ứng. Entity có `IsArchived` nhưng không có luồng đổi trạng thái. | — | CHƯA TRIỂN KHAI |
| — | Archive/khôi phục hợp đồng | Entity có `IsArchived`; truy vấn ẩn hợp đồng đã archive, nhưng không thấy action/service archive hoặc restore. | — | CHƯA TRIỂN KHAI |
| — | Phụ lục, điều chỉnh và lịch sử phiên bản hợp đồng/tài liệu | Không tìm thấy entity, service hoặc màn hình lịch sử/versioning tương ứng trong phần khảo sát. | — | CHƯA TRIỂN KHAI |
| — | Phê duyệt hợp đồng/tài liệu | Không tìm thấy action, workflow, trạng thái phê duyệt hoặc người phê duyệt. | — | CHƯA TRIỂN KHAI |

## 3. Quy trình nghiệp vụ

### 3.1 Tạo hợp đồng

1. Người dùng vào GET `ContractsController.Create`; Controller nạp danh sách dự án người dùng có thể thấy.
2. Role Admin, Project Director hoặc Project Manager được vào action.
3. Người dùng nhập dự án, số hợp đồng, chủ đầu tư, nhà thầu, giá trị, ngày ký, ngày hiệu lực và trạng thái.
4. POST kiểm tra `ModelState`, sau đó gọi `ContractService.CreateAsync`.
5. Service từ chối dự án không tồn tại/đã archive, người dùng không có phạm vi dự án, giá trị âm, ngày hiệu lực trước ngày ký hoặc số hợp đồng trùng với hợp đồng chưa archive.
6. Nếu hợp lệ, service trim một số chuỗi, đặt `CreatedAt`/`UpdatedAt`, lưu hợp đồng và chuyển đến trang Details.

**Ngoại lệ/giới hạn:** Unique index database áp dụng toàn cục cho `ContractNumber`, trong khi kiểm tra service chỉ tìm bản ghi chưa archive. Nếu tái sử dụng số của hợp đồng đã archive, kiểm tra service có thể cho qua nhưng database có thể từ chối. Trường hợp này chưa được kiểm thử vì database hiện không có hợp đồng.

### 3.2 Xem danh sách và chi tiết

1. Người dùng đã đăng nhập gọi `Index` hoặc `Details`.
2. Service loại hợp đồng đã archive và hợp đồng thuộc dự án đã archive.
3. Admin/Project Director được truy vấn toàn bộ hợp đồng còn hiệu lực; người dùng khác chỉ thấy hợp đồng mà họ là `ProjectManagerId` hoặc có `ProjectMember.IsActive`.
4. Details nạp thêm `Project` và `Documents`; nếu không tìm thấy trong phạm vi, Controller trả 404.

Trang Details hiển thị thông tin hợp đồng, form tải lên (dựa trên role ở giao diện) và các tài liệu. Collection tài liệu không bị lọc `IsArchived` trong `VisibleQuery`, trong khi tải xuống lại từ chối document đã archive; nếu có document archived trong database, giao diện có thể hiện link nhưng tải xuống trả 404.

### 3.3 Cập nhật hợp đồng

1. GET `Edit` chỉ trả dữ liệu nếu hợp đồng được tìm thấy trong truy vấn có phạm vi.
2. POST yêu cầu role Admin/Project Director/Project Manager và gọi `ContractService.UpdateAsync`.
3. Service tải hợp đồng chưa archive theo ID, xác thực dữ liệu mới và cập nhật các trường thông tin cùng `UpdatedAt`.

**Điểm cần xác minh:** `UpdateAsync` không kiểm tra người dùng có quyền xem/sửa hợp đồng hiện tại trước khi lấy bản ghi; việc kiểm tra quyền trong `ValidateAsync` áp dụng cho dự án gửi lên làm dữ liệu mới. Vì vậy, cần kiểm thử phân quyền chéo dự án và quyết định liệu hợp đồng có được phép chuyển dự án hay không. Đây là rủi ro suy ra từ source, chưa phải kết quả khai thác/kiểm thử.

### 3.4 Tải lên và tải xuống tài liệu

**Tải lên:**

1. Form nằm trong trang Details; Controller yêu cầu role Admin, Project Director hoặc Project Manager.
2. Controller lấy hợp đồng theo phạm vi hợp đồng và gọi `ProjectDocumentService.UploadAsync` với `ProjectId`, `ContractId`, category, file và user ID.
3. Service kiểm tra kích thước 1 byte–10 MiB, phần mở rộng cho phép, dự án chưa archive, hợp đồng (nếu có) thuộc cùng dự án và user là Project Manager hoặc thành viên active.
4. Service tạo `App_Data/ProjectDocuments`, ghi file tên GUID + extension chữ thường, sau đó lưu metadata vào database.
5. Controller hiển thị thông báo qua TempData và redirect về Details.

**Ngoại lệ/giới hạn:** Admin/Project Director có thể qua role gate và xem hợp đồng ở mọi dự án, nhưng service upload vẫn yêu cầu họ thuộc dự án. Tệp được ghi trước khi lưu metadata; không thấy transaction hoặc dọn file nếu bước lưu database thất bại. Kiểm tra chỉ dựa vào extension, không thấy kiểm tra chữ ký nội dung tệp hoặc đối chiếu MIME.

**Tải xuống:**

1. Người dùng đăng nhập gọi `DownloadDocument`.
2. Service tìm document chưa archive và kiểm tra quyền dự án; Admin/Project Director bỏ qua kiểm tra thành viên.
3. Service kiểm tra file vật lý có tồn tại; nếu document hoặc file không tồn tại/không được phép, Controller trả 404; nếu hợp lệ, trả `PhysicalFile` với ContentType và tên gốc.

### 3.5 Quy trình chưa có bằng chứng

Không tìm thấy quy trình phê duyệt, ký số, kết thúc/hủy hợp đồng, lập phụ lục, thay phiên bản tài liệu, xóa/khôi phục tài liệu hoặc audit lịch sử. Không mô tả các nội dung này như chức năng đang hoạt động.

## 4. Quy tắc nghiệp vụ (Business Rules)

| Mã | Quy tắc và phạm vi | Bằng chứng / nhận xét |
|---|---|---|
| BR-03-001 | Hợp đồng phải gắn với dự án chưa archive. Áp dụng khi tạo/cập nhật. | `ContractService.ValidateAsync`; `Contract.ProjectId` bắt buộc. |
| BR-03-002 | Số hợp đồng không được trùng theo kiểm tra Service với hợp đồng chưa archive. Tuy nhiên database có unique index toàn cục, không có filter `IsArchived`. | `ContractService.ValidateAsync`; `ApplicationDbContext.OnModelCreating`; SQLite index `IX_Contracts_ContractNumber`. **Source và tài liệu Business-03 không thống nhất về trường hợp hợp đồng archived.** |
| BR-03-003 | Giá trị hợp đồng không được âm. | Range trên `ContractEditViewModel.ContractValue` và kiểm tra `ContractService.ValidateAsync`; cấu hình precision 18,2 trong DbContext. |
| BR-03-004 | Nếu cả ngày ký và ngày hiệu lực có giá trị thì ngày hiệu lực không được trước ngày ký. | `ContractService.ValidateAsync`. Không có ngày kết thúc/expiration date trên entity. |
| BR-03-005 | Admin/Project Director bỏ qua project scope khi đọc/tạo/cập nhật hợp đồng; người khác phải là Project Manager của project hoặc có ProjectMember active. | `ContractsController` truyền cờ Admin/Director; `ContractService.VisibleQuery` và `ValidateAsync`. Việc Update chưa kiểm tra scope của hợp đồng hiện tại. |
| BR-03-006 | Nếu document có `ContractId`, hợp đồng đó phải chưa archive và thuộc cùng `ProjectId` được gửi đến service. | `ProjectDocumentService.UploadAsync`. Đây là kiểm tra ở service, không phải ràng buộc khóa ngoại ghép trong database. |
| BR-03-007 | Tệp tải lên phải lớn hơn 0, không quá 10 MiB và có extension `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.png`, `.jpg` hoặc `.jpeg`. | `ProjectDocumentService.UploadAsync`; không thấy kiểm tra nội dung thực của file. |
| BR-03-008 | Tệp vật lý được đặt ngoài `wwwroot`, trong `App_Data/ProjectDocuments`, tên lưu sinh từ GUID; database giữ tên gốc và metadata. | `ProjectDocumentService.UploadAsync` và `GetDownloadAsync`. |
| BR-03-009 | Trạng thái hợp đồng có các giá trị Draft, Active, Expired, Terminated; người dùng chọn trong form. Không thấy quy tắc chuyển trạng thái, phê duyệt, tự hết hạn hoặc kiểm tra thứ tự chuyển. | `ContractStatus`, `_ContractForm.cshtml`, `ContractService`; service gán trực tiếp `Status`. |
| BR-03-010 | Contract archived bị loại khỏi danh sách/chi tiết; document archived bị từ chối khi tải xuống. Không thấy thao tác archive/restore. | `ContractService.VisibleQuery`; `ProjectDocumentService.GetDownloadAsync`; `IsArchived` trên hai entity. Details không lọc document archived. |
| BR-03-011 | Role gate của thao tác tạo/cập nhật hợp đồng và upload là Admin, Project Director, Project Manager. Xem và download chỉ yêu cầu đăng nhập ở Controller, sau đó service giới hạn theo project (trừ Admin/Director). | Các action trong `ContractsController`; các service tương ứng. |
| BR-03-012 | Người tải lên và thời điểm tải lên được lưu qua `UploadedById` và `UploadedAt`; không tìm thấy lịch sử chỉnh sửa/audit nhiều phiên bản. | `ProjectDocument`; `ProjectDocumentService.UploadAsync`. |
| BR-03-013 | Không có bằng chứng về danh mục Category được quản trị hoặc chuẩn hóa; hiện tại category là chuỗi do form gửi lên. | Form Details và `ProjectDocumentService.UploadAsync`; chưa thấy entity/danh mục riêng trong luồng TASK-03. |

## 5. Ma trận phân quyền

Giá trị thể hiện hành vi được chứng minh bởi Controller/Service; `UNKNOWN` cho thao tác chưa có action/service tương ứng, không hàm ý hệ thống đã triển khai từ chối có chủ đích. `PARTIAL` nghĩa là quyền phụ thuộc project scope hoặc có bất nhất giữa lớp.

| Vai trò | Create | View | Update | Delete / Archive | Upload | Download | Approve |
|---|---|---|---|---|---|---|---|
| Người chưa đăng nhập | DENIED | DENIED | DENIED | UNKNOWN | DENIED | DENIED | UNKNOWN |
| Admin | ALLOWED | ALLOWED | ALLOWED | UNKNOWN | PARTIAL | ALLOWED | UNKNOWN |
| Project Director | ALLOWED | ALLOWED | ALLOWED | UNKNOWN | PARTIAL | ALLOWED | UNKNOWN |
| Project Manager | PARTIAL | PARTIAL | PARTIAL | UNKNOWN | PARTIAL | PARTIAL | UNKNOWN |
| Site Manager | DENIED | PARTIAL | DENIED | UNKNOWN | DENIED | PARTIAL | UNKNOWN |
| Supervising Engineer | DENIED | PARTIAL | DENIED | UNKNOWN | DENIED | PARTIAL | UNKNOWN |
| Worker | DENIED | PARTIAL | DENIED | UNKNOWN | DENIED | PARTIAL | UNKNOWN |
| Materials Staff | DENIED | PARTIAL | DENIED | UNKNOWN | DENIED | PARTIAL | UNKNOWN |
| Client Representative | DENIED | PARTIAL | DENIED | UNKNOWN | DENIED | PARTIAL | UNKNOWN |

**Giải thích điều kiện:**

- `PARTIAL` ở View/Download với role ngoài Admin/Director yêu cầu người dùng là Project Manager của dự án hoặc có ProjectMember active; role Identity tự nó không cấp quyền xem mọi dự án.
- Project Manager cần vừa qua role gate ở thao tác ghi/tải lên, vừa có phạm vi trên dự án.
- Upload của Admin/Project Director là `PARTIAL`: Controller cho phép, nhưng `ProjectDocumentService.UploadAsync` vẫn yêu cầu người dùng là ProjectManagerId hoặc thành viên active. Chưa có bằng chứng mọi Admin/Director đều là thành viên các dự án.
- Delete/Archive và Approve là `UNKNOWN` trong ma trận vì không có action/service tương ứng; các chức năng này được phân loại `CHƯA TRIỂN KHAI` ở mục 2.
- Vai trò không có trong `RoleNames.All` chưa được suy diễn quyền.

## 6. Mô hình dữ liệu

### 6.1 Entity và trường chính

| Entity | Trường liên quan | Vai trò |
|---|---|---|
| `Project` | `Id`, `ProjectCode`, `Name`, `ProjectManagerId`, `IsArchived` | Dự án cha; ProjectManagerId liên kết tới người dùng Identity; có tập `Members`. |
| `ProjectMember` | `Id`, `ProjectId`, `UserId`, `IsActive`, `AssignmentRole` | Liên kết thành viên với dự án; quyền truy cập TASK-03 dựa trên `IsActive`, không dựa vào `AssignmentRole` trong service. |
| `Contract` | `Id`, `ProjectId`, `ContractNumber`, `ClientName`, `ContractorName`, `ContractValue`, `SignedDate`, `EffectiveDate`, `Status`, `CreatedAt`, `UpdatedAt`, `IsArchived` | Thông tin hợp đồng. Không có `EndDate`, `ApprovedBy`, `ApprovalStatus`, phụ lục hoặc collection lịch sử. |
| `ProjectDocument` | `Id`, `ProjectId`, nullable `ContractId`, `FileName`, `StoredFileName`, `ContentType`, `FileSize`, `Category`, `UploadedById`, `UploadedAt`, `IsArchived` | Metadata tệp; nội dung nằm trong filesystem. Không có version number hoặc history. |
| `ApplicationUser` | Identity `Id` và thông tin tài khoản | Liên kết ProjectManager, ProjectMember, người tải lên tài liệu. |

### 6.2 Quan hệ và ràng buộc

- `Project 1 — N Contract`: `Contract.ProjectId` bắt buộc; xóa Project bị Restrict theo cấu hình EF.
- `Project 1 — N ProjectDocument`: `ProjectDocument.ProjectId` bắt buộc; xóa Project bị Restrict.
- `Contract 1 — N ProjectDocument`: `ProjectDocument.ContractId` nullable; cấu hình EF `ClientSetNull`. SQLite migration tạo FK nullable nhưng không có `ON DELETE CASCADE`.
- `ApplicationUser 1 — N ProjectDocument`: `UploadedById` bắt buộc; xóa user bị Restrict.
- `ContractNumber` có unique index toàn cục. `ProjectId` của Contract và `ProjectId`/`ContractId`/`UploadedById` của Document có index.
- Database có FK cho Project/Contract/User nhưng không có composite constraint bảo đảm `ProjectDocument.ProjectId` bằng `ProjectDocument.Contract.ProjectId`; service upload thực hiện đối chiếu này.
- Entity khai báo MaxLength và DbContext cấu hình precision; migration SQLite lưu nhiều trường dạng `TEXT`. Không thấy CHECK constraint riêng cho độ dài chuỗi, giá trị hợp đồng không âm hoặc enum status.

### 6.3 Cơ chế lưu trữ và trạng thái snapshot

`Program.cs` cấu hình `UseSqlite`; connection string trỏ tới `Data/Databases/ConstructionProjectManagement.db`. Database được kiểm tra chỉ đọc trong lần khảo sát:

- `Contracts`: 0 bản ghi.
- `ProjectDocuments`: 0 bản ghi.
- `PRAGMA integrity_check`: `ok`.
- `PRAGMA foreign_key_check`: không có dòng vi phạm.
- `__EFMigrationsHistory`: có `20261111092125_InitialSqlite` và `20261114073018_Task02Phase1ProjectMemberAssignments`.

Đây chỉ là trạng thái snapshot tại thời điểm khảo sát, không xác nhận mọi luồng ghi/đọc đã hoạt động runtime. Thư mục `App_Data/ProjectDocuments` chưa tồn tại tại thời điểm kiểm tra; service tạo thư mục khi upload.

## 7. Trạng thái và vòng đời

### Hợp đồng

- Trạng thái mặc định là `Draft`; enum còn có `Active`, `Expired`, `Terminated`.
- Form tạo/sửa cho phép chọn trực tiếp một giá trị enum; service lưu giá trị gửi lên.
- Chưa tìm thấy chuyển trạng thái có điều kiện, workflow duyệt, kết thúc/hủy có xác nhận, tự chuyển Expired theo ngày hoặc lịch sử thay đổi.
- `SignedDate` và `EffectiveDate` có thể null. Không có ngày kết thúc hợp đồng.
- `IsArchived` được dùng để lọc bản ghi nhưng chưa có thao tác archive/restore trong UI/service.

### Tài liệu

- Luồng hiện có: upload file + metadata, liệt kê trong trang Details của hợp đồng, tải xuống.
- `UploadedAt` và `UploadedById` ghi nhận lần tải lên ban đầu; chưa có chỉnh sửa metadata, versioning hoặc audit history.
- `IsArchived` được kiểm tra lúc download nhưng chưa có thao tác archive/restore. Danh sách trên Details không lọc archived documents.
- Không có bằng chứng về quy trình lưu trữ dài hạn, retention, xóa vật lý hoặc khôi phục file.

## 8. Validation và xử lý ngoại lệ

### Validation tìm thấy

- ViewModel hợp đồng: ProjectId/ContractNumber required; ContractNumber tối đa 50 ký tự; ContractValue không âm; ngày là Date input.
- Service hợp đồng: dự án tồn tại và chưa archive; scope dự án; số hợp đồng trùng theo truy vấn chưa archive; giá trị không âm; EffectiveDate không trước SignedDate.
- Upload: kích thước, extension, project active, contract cùng project và quyền thành viên.
- POST Create/Edit/UploadDocument có `ValidateAntiForgeryToken`.
- Tải xuống trả 404 khi document/file không tồn tại hoặc không được phép.

### Thiếu hoặc chưa xác minh

- Không thấy validation server-side riêng cho `ClientName`/`ContractorName` ngoài MaxLength trên entity; ViewModel không đặt StringLength cho hai trường này.
- `Category` là required ở HTML, nhưng Service chỉ gọi `Trim()`; không thấy kiểm tra null/blank/max length ở tầng Service. HTML validation không thay thế validation server-side.
- Không thấy xác thực MIME với nội dung thực, chữ ký file, malware scan, quota hoặc giới hạn tổng dung lượng theo dự án.
- Không thấy xử lý bù để xóa file vật lý nếu `SaveChangesAsync` thất bại sau khi ghi file; cũng không thấy catch/ánh xạ riêng cho lỗi filesystem hoặc `DbUpdateException`.
- Upload action nhận `IFormFile` và `category` nhưng không có nhánh kiểm tra null tường minh trước khi gọi service; Service truy cập `file.Length` và `category.Trim()`. Thiếu tham số có thể gây lỗi runtime; chưa thực hiện request kiểm chứng.
- Update service tìm bản ghi hiện tại theo ID mà không scope theo user. Cần kiểm thử quyền chéo dự án trước khi coi chức năng update an toàn.
- POST Edit nhận `id` không khai báo `[FromRoute]` và so sánh với `model.Id`; cần kiểm thử binding route/form để xác nhận không thể nhầm ID. Không kết luận hành vi runtime chỉ từ source.
- Số hợp đồng của bản ghi archived có thể vượt kiểm tra Service nhưng xung đột unique index toàn cục. Lỗi database thực tế chưa được tái hiện.

## 9. Tích hợp với các module khác

### TASK-02 — Project và ProjectMember

Contract và Document bắt buộc gắn Project; tài liệu có thể gắn thêm Contract. Scope truy cập dựa vào `Project.ProjectManagerId` hoặc `ProjectMember.IsActive`. `ProjectMember.AssignmentRole`/ProjectRole không được dùng trong các truy vấn scope TASK-03 đã khảo sát. Project archived bị loại khỏi contract list/validation và không được upload document.

### Employee và User

Không thấy Contract/Document FK trực tiếp tới Employee. Thành viên dự án và người tải lên được liên kết qua `ApplicationUser`; ProjectMember có thể liên kết thêm Employee nhưng TASK-03 service dùng UserId.

### AUTH / Identity

`ContractsController` yêu cầu đăng nhập. Các action Create/Edit/Upload có role gate cụ thể. Không thấy policy/claim chuyên biệt cho Contracts hoặc Documents; phạm vi project được kiểm tra tại Service. Download không có role gate riêng ngoài `[Authorize]`, còn quyền chi tiết do service quyết định.

### Database và filesystem

EF Core `ApplicationDbContext` quản lý entity, index và quan hệ. Metadata lưu trong SQLite; file lưu dưới content root tại `App_Data/ProjectDocuments`, không được lưu dưới `wwwroot`. Không thấy abstraction lưu trữ ngoài filesystem hoặc job đồng bộ/đối soát database với file.

## 10. Những điểm chưa hoàn thiện và cần xác nhận

### Đã triển khai theo source

- CRUD một phần cho Contract: list, details, create, edit.
- Liên kết Contract với Project, thông tin hai bên, giá trị, ngày ký/ngày hiệu lực và trạng thái.
- Upload/download tài liệu gắn với project/contract; lưu metadata, người tải lên, thời gian tải lên và trạng thái archive.
- Kiểm tra project scope, giá trị, ngày, số hợp đồng, extension và dung lượng.

### Triển khai một phần

- Cập nhật hợp đồng có service nhưng chưa kiểm tra quyền trên hợp đồng nguồn trước khi cập nhật.
- Upload có role gate và scope check nhưng Admin/Project Director được Controller cho phép trong khi Service vẫn đòi membership.
- `ContractId` nullable hỗ trợ về mặt service việc tải tài liệu chỉ gắn Project, nhưng chưa có luồng UI/Controller tương ứng.
- Có `IsArchived` nhưng không có thao tác archive/restore; document list và download xử lý cờ archive không đồng nhất.
- Có enum trạng thái nhưng chưa có quy trình vòng đời/trạng thái.
- Có giới hạn extension và size, nhưng chưa thấy kiểm tra nội dung file hoặc xử lý bù nếu lưu DB lỗi.

### Chưa triển khai theo source khảo sát

- Phê duyệt hợp đồng/tài liệu; ký số.
- Ngày kết thúc/expiration date và tự động hết hạn.
- Phụ lục/điều chỉnh, versioning và lịch sử thay đổi.
- Xóa, archive/restore qua giao diện/service.
- Trang danh sách/chi tiết/quản trị tài liệu độc lập; chỉnh sửa metadata tài liệu.
- Audit log đầy đủ cho các thay đổi hợp đồng/tài liệu.

### Chưa đủ bằng chứng / sai khác tài liệu

1. [Business_03_Contracts_and_Documents.md](../01_Business_Analysis/Business_03_Contracts_and_Documents.md) nói số hợp đồng duy nhất trong dữ liệu chưa lưu trữ; `ApplicationDbContext` và database hiện tại lại có unique index không filter. Cần chốt quy tắc.
2. [Database_Design.md](../03_Database/Database_Design.md) mô tả SQL Server, trong khi `Program.cs` và `appsettings.json` hiện cấu hình SQLite.
3. [Implementation_Progress.md](../05_Project_Management/Implementation_Progress.md) đánh dấu code TASK-03 `DONE`, đồng thời ghi tự kiểm thử chỉ `PARTIAL` và chờ user test. Bảng tiến độ không phải bằng chứng nghiệm thu. Database hiện tại không có Contract/ProjectDocument để xác nhận luồng CRUD.
4. [TASK_FUNCTION_INVENTORY.md](../TASK_FUNCTION_INVENTORY.md) đánh dấu sáu function TASK-03 `NEEDS_TEST`; không tìm thấy kết quả kiểm thử chức năng TASK-03 trong các tài liệu đã đối chiếu.
5. Tài liệu thiết kế tổng quát nói dữ liệu nghiệp vụ quan trọng dùng lịch sử hoặc soft delete, nhưng Contract/ProjectDocument hiện không có history entity và chưa có thao tác archive trong ứng dụng.
6. Các migration trong `Migrations.SqlServer` không được compile theo [ConstructionProjectManagement.csproj](../ConstructionProjectManagement.csproj). Migration `AddContractsAndDocuments` tạo schema hợp đồng/tài liệu, còn `LinkDocumentsToContracts` rỗng; schema SQLite đang hoạt động đã có `ContractId` ngay trong migration ban đầu. Không dùng migration SQL Server rỗng để kết luận FK hiện tại của SQLite bị thiếu.

### Câu hỏi cần BA/người dùng xác nhận

1. Số hợp đồng có phải duy nhất vĩnh viễn, kể cả khi hợp đồng đã archive, hay chỉ duy nhất trong nhóm chưa archive?
2. Ai được tạo/cập nhật hợp đồng? Project Manager có quyền với mọi dự án hay chỉ dự án được giao/tham gia? Admin/Project Director có được upload lên dự án mà họ không phải thành viên không?
3. Có cho phép chuyển hợp đồng sang Project khác khi cập nhật không? Nếu có, cần kiểm tra cả quyền trên Project nguồn và đích như thế nào?
4. Các trạng thái Draft/Active/Expired/Terminated được chuyển theo điều kiện nào? Có cần phê duyệt, người phê duyệt hoặc lịch sử chuyển trạng thái không?
5. Hợp đồng có cần ngày kết thúc/expiration date, tự động hết hạn, kết thúc/hủy hoặc quy trình bàn giao không?
6. Có cần quản lý phụ lục, phiên bản và lịch sử thay đổi hợp đồng/tài liệu không?
7. Tài liệu chỉ gắn hợp đồng hay cũng cần upload/list riêng theo Project? Ai được xem, sửa metadata, archive, xóa và khôi phục?
8. Category là danh mục cố định hay chuỗi tự do? Có cần giới hạn kiểu MIME/nội dung, quét malware, quota hoặc chính sách lưu trữ không?
9. `IsArchived` hiện có ý nghĩa nghiệp vụ gì, ai được thay đổi và có cần restore không?
10. Nếu lưu file thành công nhưng lưu metadata thất bại, hệ thống cần xóa file vừa ghi hay có quy trình đối soát?

## 11. Danh sách file và bằng chứng

| File | Class / method / nội dung liên quan |
|---|---|
| [Controllers/ContractsController.cs](../Controllers/ContractsController.cs) | `ContractsController`; Index, Details, Create GET/POST, Edit GET/POST, UploadDocument, DownloadDocument; `[Authorize]`, role gates, antiforgery và xử lý kết quả. |
| [Services/ContractService.cs](../Services/ContractService.cs) | `ContractService`; `GetVisibleAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `VisibleQuery`, `ValidateAsync`. |
| [Services/ProjectDocumentService.cs](../Services/ProjectDocumentService.cs) | `ProjectDocumentService`; `UploadAsync`, `GetDownloadAsync`, extension/size/access checks, filesystem storage. |
| [Services/Interfaces/IContractService.cs](../Services/Interfaces/IContractService.cs) | Contract service API hiện có; không có delete/archive/approve. |
| [Services/Interfaces/IProjectDocumentService.cs](../Services/Interfaces/IProjectDocumentService.cs) | Upload/download là các thao tác document service hiện có. |
| [Models/Contract.cs](../Models/Contract.cs) | `Contract` fields, navigation, `IsArchived`. |
| [Models/ContractStatus.cs](../Models/ContractStatus.cs) | `Draft`, `Active`, `Expired`, `Terminated`. |
| [Models/ProjectDocument.cs](../Models/ProjectDocument.cs) | Metadata, FK, uploader/time và `IsArchived`. |
| [Models/Project.cs](../Models/Project.cs) | `ProjectManagerId`, `IsArchived`, collection `Members`. |
| [Models/ProjectMember.cs](../Models/ProjectMember.cs) | `ProjectId`, `UserId`, `IsActive`, role metadata. |
| [Models/RoleNames.cs](../Models/RoleNames.cs) | Danh sách role được khai báo trong ứng dụng. |
| [ViewModels/ContractViewModels.cs](../ViewModels/ContractViewModels.cs) | Input validation và list/details ViewModels. |
| [Data/ApplicationDbContext.cs](../Data/ApplicationDbContext.cs) | DbSet Contracts/ProjectDocuments; unique index, precision, FK và delete behavior trong `OnModelCreating`. |
| [Migrations/20261111092125_InitialSqlite.cs](../Migrations/20261111092125_InitialSqlite.cs) | Schema SQLite cho Contracts/ProjectDocuments và các index/FK ban đầu. |
| [Migrations.SqlServer/20261111080853_AddContractsAndDocuments.cs](../Migrations.SqlServer/20261111080853_AddContractsAndDocuments.cs) | Migration SQL Server lịch sử tạo Contracts/ProjectDocuments; không thuộc provider đang chạy. |
| [Migrations.SqlServer/20261111081008_LinkDocumentsToContracts.cs](../Migrations.SqlServer/20261111081008_LinkDocumentsToContracts.cs) | Migration SQL Server có Up/Down rỗng; không được compile theo project file. |
| [ConstructionProjectManagement.csproj](../ConstructionProjectManagement.csproj) | Target framework, SQLite provider và loại `Migrations.SqlServer` khỏi compile. |
| [Program.cs](../Program.cs) | `UseSqlite`, đăng ký services, Identity, middleware và conventional MVC route. Lưu ý startup gọi `DbInitializer.ApplyMigrationsAsync`; không chạy ứng dụng trong khảo sát. |
| [appsettings.json](../appsettings.json) | Connection string SQLite hiện tại. |
| [Views/Contracts/Index.cshtml](../Views/Contracts/Index.cshtml) | Bảng danh sách hợp đồng và nút tạo theo role. |
| [Views/Contracts/Details.cshtml](../Views/Contracts/Details.cshtml) | Chi tiết hợp đồng, form upload và danh sách/link tải tài liệu. |
| [Views/Contracts/Create.cshtml](../Views/Contracts/Create.cshtml) | Trang tạo; dùng partial form chung. |
| [Views/Contracts/Edit.cshtml](../Views/Contracts/Edit.cshtml) | Trang cập nhật; dùng partial form chung. |
| [Views/Contracts/_ContractForm.cshtml](../Views/Contracts/_ContractForm.cshtml) | Các trường hợp đồng, lựa chọn Project/Status và client validation. |
| [docs/01_Business_Analysis/Business_03_Contracts_and_Documents.md](../01_Business_Analysis/Business_03_Contracts_and_Documents.md) | Mô tả nghiệp vụ hiện có để đối chiếu; có điểm khác biệt về tính duy nhất số hợp đồng. |
| [docs/03_Database/Database_Design.md](../03_Database/Database_Design.md) | Tài liệu thiết kế database; hiện ghi SQL Server, khác provider đang cấu hình. |
| [docs/05_Project_Management/Implementation_Progress.md](../05_Project_Management/Implementation_Progress.md) | Tiến độ TASK-03 và ghi nhận mức tự kiểm thử. |
| [TASK_FUNCTION_INVENTORY.md](../TASK_FUNCTION_INVENTORY.md) | Danh sách function FN-03-01..06, dependency TASK-02 và trạng thái NEEDS_TEST. |
| [Data/Databases/ConstructionProjectManagement.db](../Data/Databases/ConstructionProjectManagement.db) | Được mở chỉ đọc để đếm bảng, kiểm tra index, migration history, `integrity_check` và `foreign_key_check`; không thay đổi dữ liệu. |

### Kết luận khảo sát

TASK-03 hiện có luồng quản lý hợp đồng cơ bản và upload/download tài liệu gắn hợp đồng. Chưa đủ căn cứ để coi toàn bộ nghiệp vụ đã hoàn chỉnh hoặc được nghiệm thu: chưa có kết quả kiểm thử chức năng trong khảo sát này, dữ liệu Contract/Document hiện rỗng, còn các điểm cần xác minh về quyền upload, quyền cập nhật chéo dự án, vòng đời hợp đồng, unique index và xử lý file/metadata.

Không chỉnh sửa source code, migration hoặc database trong quá trình lập tài liệu này.
