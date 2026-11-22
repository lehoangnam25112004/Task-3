# Dữ liệu giả lập WBS TASK-04

## Phạm vi và an toàn

- Dữ liệu nằm trên dự án có sẵn `FAKE-TASK03-P01` (ID 1), đang thực hiện. Không sửa Project, thành viên, tài khoản hoặc Task có sẵn.
- Ba Task mới được tạo riêng vì database không có Task; người dùng đã chấp thuận phương án này.
- Dữ liệu dùng dấu `[TEST_TASK04]`, mã `WBS-TEST-`; không thay đổi quyền truy cập.
- Bản sao lưu nhất quán trước khi tạo được lưu tại `D:\Task 3\TASK04_WBS_TEST_STATE\task04-before-seed-20261122T075419887787Z.db`. Không chia sẻ hoặc đưa backup vào source control; nên lưu thêm một bản ở vị trí an toàn khác.
- Manifest `D:\Task 3\TASK04_WBS_TEST_STATE\task04-fake-manifest.json` ghi các ID thực tế để dọn dẹp. Không chỉnh sửa hoặc xóa manifest trước khi dọn dữ liệu.
- Database hiện không có dự án Archived, Completed hoặc Cancelled; không thay đổi trạng thái dự án hiện có để dựng ca chỉ đọc.

## Giai đoạn

| ID | Giai đoạn | Thứ tự | Trạng thái |
|---|---|---:|---|
| 1 | `[TEST_TASK04] Thi công đang thực hiện` | 10 | Đang thực hiện |
| 2 | `[TEST_TASK04] Nhóm đã lưu trữ` | 20 | Đã lưu trữ; trước đó Đang thực hiện |

## Hạng mục và Task

| WBS | Cha | Cấp | Trạng thái ban đầu | Task |
|---|---|---:|---|---|
| `WBS-TEST-01` | — | 1 | Đang thực hiện | — |
| `WBS-TEST-01.01` | `WBS-TEST-01` | 2 | Chưa bắt đầu | ID 1: Kiểm tra công tác đất |
| `WBS-TEST-01.01.01` | `WBS-TEST-01.01` | 3 | Tạm hoãn | — |
| `WBS-TEST-01.02` | `WBS-TEST-01` | 2 | Hoàn thành | — |
| `WBS-TEST-02` | — | 1 | Đang thực hiện | ID 2: Theo dõi vật tư |
| `WBS-TEST-03` | — | 1 | Đã lưu trữ; trước đó Tạm hoãn | ID 3: Kiểm tra liên kết WBS đã lưu trữ |
| `WBS-TEST-10` | — | 1 | Đã lưu trữ; trước đó Đang thực hiện | — |
| `WBS-TEST-10.01` | `WBS-TEST-10` | 2 | Đã lưu trữ; trước đó Tạm hoãn | — |
| `WBS-TEST-10.02` | `WBS-TEST-10` | 2 | Đã lưu trữ riêng; trước đó Hoàn thành | — |

`WBS-TEST-10` và `WBS-TEST-10.01` cùng `ArchiveGroupId` với giai đoạn 2; `WBS-TEST-10.02` có group riêng. `WBS-TEST-03` cũng có group riêng. Hai ca khôi phục giai đoạn và khôi phục item con cùng thay đổi trạng thái ban đầu, nên hãy chạy riêng từng ca; để trở về trạng thái đầu, dọn dữ liệu rồi tạo lại theo hướng dẫn bên dưới.

## Tài khoản hiện có

Sử dụng mật khẩu hiện có của các tài khoản giả lập TASK-03; script TASK-04 không tạo, đổi hoặc reset mật khẩu.

| Chức vụ | Tài khoản | Phạm vi kiểm tra |
|---|---|---|
| Admin | `fake.task03.admin@example.local` | Xem toàn hệ thống; archive/restore, không sửa cấu trúc |
| Project Director | `fake.task03.director@example.local` | Thành viên active dự án 1 |
| Project Manager | `fake.task03.manager@example.local` | Thành viên active dự án 1; tạo/sửa WBS và sửa Task |
| Site Manager | `fake.task03.site@example.local` | Thành viên active dự án 1; cập nhật trạng thái |
| Supervising Engineer | `fake.task03.engineer@example.local` | Thành viên active dự án 1; chỉ xem cấu trúc |
| Materials Staff | `fake.task03.materials@example.local` | Thành viên active dự án 1; chỉ xem cấu trúc |
| Worker | `fake.task03.worker@example.local` | Active ở dự án 1, inactive ở dự án 2 |
| Client Representative | `fake.task03.client.match@example.local` | Active và khớp tổ chức ở dự án 1; không khớp ở dự án 2 |
| Client Representative lệch | `fake.task03.client.mismatch@example.local` | Active nhưng không khớp tổ chức ở dự án 1 |

Mật khẩu ban đầu của seed TASK-03 được thiết lập trong [`Task03TestDataSeeder.cs`](../../Data/Task03TestDataSeeder.cs); nếu không đăng nhập được, không tự reset tài khoản có sẵn. Quyền Task Create/Edit vẫn giữ nguyên role gate hiện hành, không mở thêm cho SM/SE/MS.

## Tự kiểm thử trên trình duyệt

1. Đăng nhập Project Manager, mở `/Wbs?projectId=1`. Kiểm tra thứ tự giai đoạn, cây ba cấp, nhãn tiếng Việt, và số công việc liên kết của `WBS-TEST-01.01` và `WBS-TEST-02`. Hạng mục lưu trữ bị ẩn mặc định.
2. Với `WBS-TEST-01`, chọn trạng thái **Hoàn thành**, nhấn **Cập nhật**. Xác nhận xuất hiện cảnh báo vì con `WBS-TEST-01.01` chưa hoàn thành; thao tác vẫn được phép.
3. Bật **Hiển thị mục đã lưu trữ**. Thấy giai đoạn 2 cùng các mục đã archive và `WBS-TEST-03`. Khôi phục giai đoạn 2: `WBS-TEST-10` trở lại Đang thực hiện, `WBS-TEST-10.01` trở lại Tạm hoãn, còn `WBS-TEST-10.02` vẫn lưu trữ.
4. Với dữ liệu đã đưa về trạng thái ban đầu, bật bộ lọc và **Khôi phục** riêng `WBS-TEST-10.01`: giai đoạn 2 và tổ tiên `WBS-TEST-10` tự khôi phục; `WBS-TEST-10.02` vẫn lưu trữ.
5. Mở `/Tasks/Create?projectId=1`, kiểm tra dropdown WBS không có `WBS-TEST-03` khi nó đang lưu trữ. Mở `/Tasks/Edit/3`, xác nhận `WBS-TEST-03` có nhãn liên kết hiện tại đã lưu trữ; nhấn **Lưu** không đổi WBS. Trang chi tiết vẫn hiển thị liên kết, WBS không tự khôi phục.
6. Đăng nhập lần lượt các vai trò ở bảng trên để kiểm tra nút thao tác; truy cập `/Wbs?projectId=2` bằng Worker phải bị từ chối do membership inactive; tài khoản Client Representative lệch bị từ chối ở dự án 1. Không đổi membership hoặc tổ chức của dữ liệu có sẵn để kiểm tra.

## Dọn và tạo lại

Chạy PowerShell từ bất kỳ thư mục nào. Dừng thao tác kiểm thử và chờ các yêu cầu ứng dụng hoàn tất trước khi dọn:

```powershell
python 'D:\Task 3\Task-2-main\docs\TASK04\task04_fake_data.py' cleanup --database 'D:\Task 3\Task-2-main\Data\Databases\ConstructionProjectManagement.db' --state-dir 'D:\Task 3\TASK04_WBS_TEST_STATE'
```

Script sao lưu DB trước khi xóa, chỉ xóa ID trong manifest và kiểm tra các khóa ngoại tham chiếu. Nếu có dữ liệu ngoài bộ mẫu tham chiếu tới các bản ghi test, hoặc marker đã bị sửa/mất, script dừng và rollback thay vì xóa dây chuyền. Hãy xem thông báo lỗi và xử lý có chủ đích; không xóa dữ liệu gốc để ép cleanup thành công. Script không xóa dữ liệu mới do bạn tự tạo ngoài manifest.

Tạo lại trạng thái ban đầu sau khi đã dọn thành công:

```powershell
python 'D:\Task 3\Task-2-main\docs\TASK04\task04_fake_data.py' seed --database 'D:\Task 3\Task-2-main\Data\Databases\ConstructionProjectManagement.db' --state-dir 'D:\Task 3\TASK04_WBS_TEST_STATE'
```

Chạy `seed` lần nữa khi bộ mẫu còn nguyên chỉ báo đã tồn tại và không ghi đè các thay đổi kiểm thử. Mỗi lần tạo/dọn thành công có backup riêng; manifest sau cleanup được lưu lại với tên `task04-cleaned-...json`.
