# Báo cáo kiểm thử và xác minh TASK-03

Ngày kiểm thử: 2026-11-21

## 1. Tóm tắt kết quả kiểm thử

- Trạng thái migration: PASS.
- Trạng thái build: PASS khi build bằng `dotnet build --no-restore -p:UseAppHost=false`.
- Trạng thái test: PASS, 12/12 test.
- Trạng thái khởi động ứng dụng: PASS; ứng dụng phản hồi HTTP trên `http://localhost:5167`.
- Route `/Documents`: PASS, trả `302` về trang đăng nhập khi chưa xác thực.
- Route `/Contracts`: PASS, trả `302` về trang đăng nhập khi chưa xác thực.
- Seeder TASK-03: PASS, hỗ trợ chạy lại an toàn và có tiền tố `[FAKE-TASK03]`.

## 2. Danh sách Fake Data đã khởi tạo

| Nhóm dữ liệu | Số lượng / chi tiết | Tiền tố nhận diện |
|---|---:|---|
| Tài khoản fake | 9 tài khoản | Email `fake.task03.*@example.local` |
| Vai trò 8 chức vụ | Admin, Project Director, Project Manager, Site Manager, Supervising Engineer, Materials Staff, Worker, Client Representative | Tài khoản nhóm chức vụ |
| Dự án fake | 2 | `FAKE-TASK03-P01`, `FAKE-TASK03-P02` |
| Thành viên dự án | Thành viên active; Worker trên dự án P02 được đặt `IsActive=false` | `[FAKE-TASK03]` |
| Hợp đồng fake | 7 hợp đồng | `HD-FAKE-TASK03-*` |
| Phụ lục fake | 7 phụ lục | `*-PL01` |
| Tài liệu fake | 6 tài liệu | `DOC-FAKE-TASK03-*` |
| Danh mục fake ngừng kích hoạt | 1 danh mục | `FAKE-TASK03-LEGACY` |
| Đối tác fake | 2 đối tác | `FAKE-CLIENT-A`, `FAKE-CONTRACTOR` |
| File kiểm thử fake | 6 file | Thư mục `App_Data/Documents/fake-task03` |

Các tài khoản đại diện Chủ đầu tư:

| Tài khoản | Tổ chức | Kết quả kỳ vọng |
|---|---|---|
| `fake.task03.client.match@example.local` | `FAKE Chủ đầu tư A` | Khớp tổ chức và ProjectMember active |
| `fake.task03.client.mismatch@example.local` | `Tổ chức không khớp` | Default Deny do tổ chức không khớp |

Các file kiểm thử:

- `fake-valid.pdf`: hợp lệ theo magic bytes PDF.
- `fake-valid.png`: hợp lệ theo magic bytes PNG.
- `fake-valid.docx`: hợp lệ theo cấu trúc ZIP/OOXML.
- `fake-valid.xlsx`: hợp lệ theo cấu trúc ZIP/OOXML.
- `fake-invalid.pdf`: sai signature.
- `fake-oversized.pdf`: kích thước vượt 25 MB.

## 3. Chi tiết kết quả kiểm thử

| Nhóm | Kịch bản | Dữ liệu / điều kiện | Kết quả |
|---|---|---|---|
| Build | Biên dịch toàn bộ project | `dotnet build --no-restore -p:UseAppHost=false` | PASS |
| Migration | Áp dụng schema SQLite | EF Core Migrate | PASS |
| Server | Khởi chạy route MVC | `http://localhost:5167` | PASS |
| Tài liệu | Route bảo vệ xác thực | `/Documents` chưa đăng nhập | PASS, `302` sang login |
| Hợp đồng | Route bảo vệ xác thực | `/Contracts` chưa đăng nhập | PASS, `302` sang login |
| Dữ liệu tài liệu | Số tài liệu fake | 6 tài liệu | PASS |
| Dữ liệu hợp đồng | Số hợp đồng và phụ lục fake | 7 hợp đồng, 7 phụ lục | PASS |
| Dữ liệu dự án | Hai dự án và các vai trò fake | 2 dự án, 9 tài khoản | PASS |
| Thu hồi quyền | Worker P02 `IsActive=false` | Thành viên fake đã bị gỡ | PASS |
| Hợp đồng | Phê duyệt phụ lục cộng/trừ giá trị và cập nhật ngày kết thúc | Giá trị ban đầu 100; điều chỉnh +25; ngày kết thúc mở rộng | PASS |
| Hợp đồng | Chống tự duyệt | Người tạo thực hiện phê duyệt | PASS |
| Tài liệu | Khóa chỉnh sửa 15 phút | Chủ khóa và người khác | PASS |
| Tài liệu | Magic bytes và dung lượng 25 MB | PDF giả mạo và file quá dung lượng | PASS |
| Unit test | Toàn bộ project test | 12 test | PASS |

## 4. Các lỗi phát hiện và đã sửa

| Lỗi | Nguyên nhân | Cách khắc phục |
|---|---|---|
| Thiếu cột `Contracts.Title` khi khởi chạy seeder | Model có `Title` nhưng migration thực tế trên SQLite chưa tạo cột | Thêm migration `20261121144700_Task03FixContractTitle` và áp dụng vào SQLite |
| Cột `Title` bị thêm trùng trong migration | Một số migration sync thêm `Title` cho `Contracts`/`ProjectDocuments` nhiều lần | Loại bỏ các thao tác `AddColumn` trùng và giữ migration gốc duy nhất cho `ProjectDocuments` |
| Server build bị lock bởi executable đang chạy | Tiến trình apphost còn giữ file `.exe` trong `bin/Debug/net10.0` | Dừng tiến trình và sử dụng `-p:UseAppHost=false` cho build/test |
| Test khóa tài liệu không tìm thấy user khác | Fixture test chưa tạo bản ghi Identity user cho khóa ngoại | Bổ sung user `creator` và `other` vào fixture |
| Dữ liệu seeder bị bỏ qua khi restart | Guard kiểm tra tài liệu trả về sớm toàn bộ seeder | Chuyển guard sang kiểm tra từng thực thể và chạy phần tạo file mẫu lại trên startup |

## 5. Các điểm đã hỏi và xác nhận

Không phát sinh điểm mâu thuẫn yêu cầu cần hỏi người dùng trong vòng kiểm thử. Các chính sách chưa nêu rõ được triển khai theo đặc tả TASK-03 hiện tại:

- Hợp đồng 7 trạng thái giữ enum cố định, không thêm trạng thái thứ 8.
- Dữ liệu fake mang tiền tố `[FAKE-TASK03]` và mã riêng.
- Khi chưa đăng nhập, route `/Documents` và `/Contracts` chuyển về trang Login thay vì tiết lộ dữ liệu.

## 6. Hướng dẫn dọn dẹp fake data và vận hành

### Nhận diện dữ liệu fake

- ProjectCode: `FAKE-TASK03-*`
- ContractNumber: `HD-FAKE-TASK03-*`
- DocumentCode: `DOC-FAKE-TASK03-*`
- Email tài khoản: `fake.task03.*@example.local`
- PartyCode: `FAKE-CLIENT-A`, `FAKE-CONTRACTOR`
- CategoryCode: `FAKE-TASK03-LEGACY`
- File: `App_Data/Documents/fake-task03`

### Dọn dẹp

1. Tắt ứng dụng trước khi dọn dữ liệu.
2. Xóa các bản ghi phụ thuộc theo thứ tự `ContractValueHistory`, `ContractAmendment`, `ContractProject`, `Contract`, `ProjectDocument/DocumentVersion`, `ProjectMember`, `Project`.
3. Xóa tài khoản fake bằng email `fake.task03.*@example.local`.
4. Xóa thư mục `App_Data/Documents/fake-task03`.
5. Giữ lại migration và seeder để môi trường QA có thể khởi tạo lại bằng cấu hình:
   `CPMS_SEED_TASK03_TEST_DATA=true`.

### Lưu ý vận hành

- Không bật seeder fake trong môi trường production.
- Mật khẩu tài khoản fake chỉ dành cho môi trường kiểm thử: `Fake@Task03#123`.
- Các cảnh báo NuGet hiện có liên quan advisory `SQLitePCLRaw.lib_e_sqlite3` và `System.Security.Cryptography.Pkcs` không làm test suite thất bại, nhưng nên cập nhật dependency trong vòng vận hành thực tế.
