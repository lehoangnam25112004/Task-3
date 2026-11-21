# TASK-02 — Test Cases kiểm thử độc lập

## Quy ước

- Phạm vi là FN-02-01 đến FN-02-06. AUTH chi tiết và luồng phê duyệt ngoại lệ đa dự án không thuộc phạm vi triển khai hiện tại.
- Tất cả thao tác ghi kiểm thử dùng bản sao SQLite cô lập; không ghi/xóa dữ liệu trên DB đang dùng.
- Trạng thái chỉ chuyển thành PASS/FAIL sau khi có kết quả chạy thực tế. Bằng chứng cụ thể được ghi ở cột Actual Result.

## A. Hiển thị và truy xuất dữ liệu

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| A-01 | Project list | Hiển thị danh sách dự án | Đăng nhập Admin; DB thử có hai dự án | Mở `/Projects` | CPMS-2026-001, TEST-PRJ-002 | Cả hai dự án chưa archive hiển thị | MVC smoke test trên DB cô lập: danh sách hiển thị hai dự án hiện hành | PASS |
| A-02 | Project details | Xem chi tiết dự án | Đăng nhập Admin | Mở chi tiết dự án | CPMS-2026-001 | Thông tin chi tiết khớp các giá trị DB | MVC smoke test: trang chi tiết mở thành công cho dự án đã chọn | PASS |
| A-03 | Project details | Dữ liệu quan hệ Project–PM–Employee–Member–Role | Dự án có PM và lịch sử role | Mở chi tiết và trang thành viên; đối chiếu DB | TEST-NV-001, TEST-NV-005 | Mã nhân viên, PM và các role hiện hành khớp DB | Đối chiếu trang thành viên/chi tiết với DB clone: PM và role hiện hành khớp dữ liệu liên quan | PASS |
| A-04 | Project lookup | Truy cập ID không tồn tại | Đăng nhập Admin | GET `/Projects/Details/999999` | ID không tồn tại | Trả 404, không có exception lộ ra | MVC request với ID không tồn tại trả 404 | PASS |
| A-05 | Project lookup | Xem dự án đã archive | Bản sao có dự án được archive | GET Details bằng ID đã archive | Project.IsArchived=true | Không hiển thị; trả 404 | GET `/Projects/Details/3` trên clone; dự án archived trả 404 | PASS |
| A-06 | Project members | ID dự án không tồn tại | Đăng nhập Admin | GET trang thành viên với ID không tồn tại | ID không tồn tại | Request bị từ chối, không lộ dữ liệu dự án khác | GET `/ProjectMembers/Index?projectId=999999` chuyển đến Identity AccessDenied (trang đích 200); không hiển thị dữ liệu dự án | PASS |

## B. Tạo mới dự án

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| B-01 | Project create | Tạo dự án hợp lệ không có PM | Admin; DB cô lập | POST Create | QA-NEW-001, ngày kết thúc sau ngày bắt đầu, ngân sách 0 | Tạo đúng một dự án; ProjectManagerId NULL; lịch sử PM được ghi; redirect chi tiết | MVC POST thành công trên clone; ProjectManagerId NULL, lịch sử bổ nhiệm ban đầu được tạo | PASS |
| B-02 | Project create | Tạo dự án có PM hợp lệ | Có Employee hoạt động | POST Create chọn Employee | TEST-NV-008 | Tạo Project, một ProjectMember liên kết Employee, role PM và lịch sử PM nhất quán | MVC POST thành công; PM được đồng bộ với đúng một ProjectMember, role PM và history | PASS |
| B-03 | Project create | Thiếu mã hoặc tên bắt buộc | Admin | POST Create với trường rỗng | ProjectCode hoặc Name rỗng | Form báo lỗi validation; không tạo dữ liệu | POST rỗng cả mã và tên trả lại form Create với lỗi validation; không phát sinh Project mới | PASS |
| B-04 | Project create | Mã dự án trùng, kể cả dự án lưu trữ | Có mã đã tồn tại | POST Create | CPMS-2026-001 hoặc mã đã archive | Từ chối an toàn; không tạo bản ghi; form hiển thị lỗi nghiệp vụ | MVC POST mã trùng bị từ chối; service test xác nhận mã trùng archived không tạo thêm Project | PASS |
| B-05 | Project create | Sai ngày và ngân sách âm | Admin | POST Create | ExpectedEndDate < StartDate; Budget < 0 | Từ chối; không lưu dự án | SQLite service tests: ngày kết thúc trước ngày bắt đầu và ngân sách âm đều bị từ chối | PASS |
| B-06 | Project create | ProjectManagerId không tồn tại hoặc Employee nghỉ việc | Có thể gửi request giả mạo | POST Create | ID Identity giả / TEST-NV-007 | Từ chối; không phát sinh Project/member/history một phần | Service test: Identity ID không tồn tại bị từ chối; MVC test cũng từ chối nhân viên nghỉ việc | PASS |
| B-07 | Project create | Dữ liệu vượt độ dài | Admin | POST Create | Mã >30, tên >200, location >300, client >200 | Model validation từ chối; không lưu | POST code 33 ký tự, Name 201, Location 301, ClientName 201 trả lại form với lỗi validation; không tạo Project | PASS |
| B-08 | Project create | Dữ liệu đặc biệt được lưu an toàn | Admin | Tạo với dấu nháy, ký tự HTML trong tên | `QA <script> & "quote"` | Lưu/lấy đúng dữ liệu và render encode, không thực thi HTML | Tạo Project ID 6 thành công; DB clone lưu đúng chuỗi `QA <script> & "quote"`; HTML chi tiết chứa `&lt;script&gt;`, không chứa chuỗi raw | PASS |

## C. Chỉnh sửa dự án

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| C-01 | Project edit | Chỉnh sửa hợp lệ | Admin; dự án đang hoạt động | POST Edit đổi tên/địa điểm | QA-EDIT-01 | Scalar cập nhật; ProjectCode/member/history không đổi ngoài dữ liệu chỉ định | MVC POST cập nhật trường dự án thành công trên clone | PASS |
| C-02 | Project edit | Thiếu trường bắt buộc / sai định dạng | Admin | POST Edit với tên rỗng hoặc ngày không hợp lệ | Name rỗng; ngày sai | Validation từ chối; DB giữ nguyên | POST Edit với Name rỗng trả lại form Edit có validation error; không chuyển sang Details và không lưu cập nhật | PASS |
| C-03 | Project edit | Đổi mã thành mã trùng | Có dự án khác cùng mã | POST Edit | CPMS-2026-001 | Từ chối; DB giữ nguyên | POST Edit trả form với lỗi “Mã dự án đã tồn tại, kể cả trên dự án đã lưu trữ.”; mã Project ID 4 vẫn `QA-IND-001` | PASS |
| C-04 | Project edit | Cập nhật ID không tồn tại hoặc ID route/body lệch | Admin | POST Edit | ID 999999 / route id khác model id | Trả 404 hoặc 400; không sửa bản ghi khác | Tái hiện trước sửa: POST route ID 7/body ID 4 chuyển đến Details/4 và đổi Project 4 trên clone. Sau sửa: cùng mismatch trả 400; POST ID 999999 trả 404; chạy lại 8 unit test và build đều đạt | PASS |
| C-05 | Project edit | Chỉ sửa thông tin, kiểm tra membership trước/sau | Dự án có lịch sử member | Chụp DB; POST Edit chỉ đổi tên; đối chiếu DB | Project 1, các TEST-NV | Không xóa/tạo lại ProjectMember hoặc thay đổi role/JoinedAt/IsActive | Đối chiếu DB clone trước/sau: ProjectMember và role assignment được giữ nguyên khi sửa scalar | PASS |

## D. Xóa / kết thúc tham gia

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| D-01 | Project | Xóa vật lý dự án | Không có chức năng xóa trong sáu Function | Kiểm tra route/UI | — | Chức năng xóa vật lý không thuộc phạm vi; không được suy diễn là có | Chưa chạy | NOT IMPLEMENTED |
| D-02 | Project member | Xóa vật lý ProjectMember | QD-03 yêu cầu bảo toàn lịch sử | Kiểm tra thao tác kết thúc membership | Thành viên có assignment history | Không xóa vật lý; kết thúc assignment và đặt IsActive=false khi hết role | MVC/service test kết thúc membership: row ProjectMember và lịch sử role vẫn tồn tại; trạng thái được đóng | PASS |
| D-03 | Project member | Kết thúc thành viên đang được tham chiếu | Có membership/role history | POST EndMembership; đối chiếu DB/FK | TEST-NV-005 | Giữ hàng lịch sử; đóng các assignment; không phát sinh lỗi FK | Kết thúc membership thành công; assignment được đóng; DB clone không có FK violation | PASS |
| D-04 | Project member | Kết thúc ID member/assignment không tồn tại | Admin | POST EndRole/EndMembership sai ID | ID không tồn tại | Trả lỗi nghiệp vụ an toàn; dữ liệu không đổi | Cả hai POST chuyển về danh sách với thông báo tương ứng “Không tìm thấy phân công vai trò đang hoạt động.” / “Không tìm thấy thành viên đang hoạt động.”; số member/assignment không đổi (12/21) | PASS |

## E. Project và ProjectMember

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| E-01 | Project | Tạo project hợp lệ | — | Theo B-01 | QA project | Kết quả theo B-01 | POST MVC tạo `QA-FU-E01` thành công; chuyển đến `/Projects/Details/7`; tổng Project trong clone tăng từ 4 lên 6 do hai ca tạo hợp lệ B-08/E-01 | PASS |
| E-02 | Project | Chỉnh sửa project hợp lệ | — | Theo C-01 | Project hiện có | Kết quả theo C-01 | POST MVC cập nhật Project ID 4 thành công và chuyển đến `/Projects/Details/4`; dữ liệu kiểm thử chỉ nằm trên clone | PASS |
| E-03 | Project member | Thêm Employee hiện có | Employee hoạt động | POST Add | TEST-NV-008 + Deputy PM | Có đúng một ProjectMember và assignment | MVC/service test thêm thành viên hoạt động thành công, có assignment đúng role | PASS |
| E-04 | Project member | Thêm nhiều vai trò đồng thời | Employee hoạt động, chưa có các role đó | POST Add chọn hai role | TEST-NV-008 + Deputy PM, Supervising Engineer | Một membership, hai assignment riêng; nếu role bị giới hạn thì áp dụng giới hạn | MVC/service test thêm nhiều role: một membership, các assignment riêng được ghi | PASS |
| E-05 | Project member | Thêm vai trò khác cho member hiện có | Membership đã tồn tại | POST Add role chưa có | TEST-NV-001 + Deputy PM tại project chưa có role này | Không tạo ProjectMember trùng; thêm đúng assignment | Bổ sung role cho Employee đã là member; clone xác nhận chỉ một ProjectMember | PASS |
| E-06 | Project member | Gửi lặp role đang hoạt động | Employee đã có role | POST Add cùng role lần nữa | TEST-NV-005 + Site Manager | Báo lỗi duplicate; không thêm assignment trùng | MVC/service test role active trùng bị từ chối; số assignment không tăng | PASS |
| E-07 | Project member | Nhân viên nghỉ việc | Employee.IsActive=false | POST Add | TEST-NV-007 | Từ chối; DB không phát sinh membership/assignment mới | MVC POST và service test từ chối Employee nghỉ việc; không tạo assignment | PASS |
| E-08 | Project member | Employee không tồn tại | Admin | POST Add | EmployeeId=999999 | Từ chối an toàn; không lưu dữ liệu dở dang | MVC POST Employee không tồn tại bị từ chối; không có membership/assignment một phần | PASS |
| E-09 | Project member | Hai membership cùng project/employee | Employee đã có membership | Thử service/API và kiểm tra unique index | Cùng project + TEST-NV-008 | Không tạo ProjectMember trùng | Service/MVC kiểm tra bổ sung role cho cùng Employee; chỉ một ProjectMember tồn tại | PASS |
| E-10 | Project member | Kết thúc một role khi còn role khác | Member có >=2 role active | POST EndRole | TEST-NV-005 | Assignment chọn bị đóng; membership vẫn active; role kia còn hiệu lực | Kết thúc một role trên clone; assignment khác và membership vẫn active | PASS |
| E-11 | Project member | Kết thúc toàn bộ role | Member đang active | POST EndMembership | TEST-NV-005 | Mọi role active bị đóng, membership inactive, lịch sử giữ nguyên | MVC/service test: membership inactive, mọi assignment đóng và history còn nguyên | PASS |
| E-12 | Project member | Tái phân công role đã kết thúc | Có assignment cũ đã ended | POST Add cùng role | TEST-NV-006 + Supervising Engineer | Tạo assignment mới; không ghi đè assignment cũ; membership được tái kích hoạt | Re-add trên clone tạo assignment mới, giữ assignment ended và tái kích hoạt cùng membership | PASS |

## F. Phân công Project Manager

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| F-01 | Project PM | Bổ nhiệm PM | Employee hoạt động | POST Create/Edit chọn PM | TEST-NV-008 | ProjectManagerId, ProjectMember, role PM và history đồng bộ | MVC create/edit bổ nhiệm PM thành công; đối chiếu Project, member, assignment và history trên clone | PASS |
| F-02 | Project PM | PM tự động trở thành member | Employee chưa có member | Bổ nhiệm PM; đối chiếu DB | TEST-NV-008 | Tạo đúng một ProjectMember có role PM | Clone xác nhận tự tạo một membership và assignment PM | PASS |
| F-03 | Project PM | PM đã có membership và vai trò khác | PM đã là thành viên | Bổ nhiệm/đổi PM sang người đó | TEST-NV-005 | Tái dùng member; không duplicate; bổ sung role PM | Service test xác nhận membership được tái sử dụng, không duplicate, role PM được đồng bộ | PASS |
| F-04 | Project PM | Thay PM chính | Dự án có PM hiện tại | POST Edit đổi PM | TEST-NV-001 sang TEST-NV-002 | Đóng role/history cũ, mở history mới; PM cũ giữ member nếu còn role khác | Thay PM trên clone; history cũ đóng, history mới mở; PM cũ giữ member khi còn role khác | PASS |
| F-05 | Project PM | Bỏ PM chính | Dự án có PM | POST Edit bỏ chọn PM | ProjectManagerId=NULL | Dự án hợp lệ không PM; role PM cũ kết thúc; lịch sử được giữ | Bỏ PM thành công; ProjectManagerId NULL, assignment/history cũ được bảo toàn và kết thúc | PASS |
| F-06 | Project PM | Tạo dự án không PM | Admin | POST Create không chọn PM | QA-NEW-002 | ProjectManagerId NULL, không tự chọn member khác làm PM | MVC và service test tạo project không PM thành công; ProjectManagerId NULL | PASS |
| F-07 | Project PM | Kiểm tra nhất quán sau đổi/bỏ PM | Có history và role assignments | Đối chiếu Project, Employee, User, ProjectMember, assignment, history | Dự án 1 | Không FK sai/duplicate; tối đa một PM history active | Sau bổ nhiệm/thay/bỏ PM, clone không có FK violation; service test xác nhận history và assignment đồng bộ | PASS |

## G. Trường hợp ngoại lệ và danh mục vai trò

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| G-01 | Assignment | Site Manager trên dự án đầu | Employee hoạt động | POST Add Site Manager | TEST-NV-008 ở project 1 | Phân công thành công | MVC/service test: Site Manager được gán cho dự án đầu tiên | PASS |
| G-02 | Assignment | Site Manager dự án thứ hai khi chưa có phê duyệt | G-01 đã thành công | POST Add Site Manager vào project 2 | Cùng Employee | Bị chặn bởi giới hạn đồng thời 1 dự án | MVC/service test: gán vào project thứ hai bị từ chối; sau khi kết thúc assignment cũ mới gán lại được | PASS |
| G-03 | Assignment | Ngoại lệ Site Manager có phê duyệt | Không có mô hình/quy trình phê duyệt trong phạm vi | Thử tìm UI/API phê duyệt | — | Ngoại lệ đa dự án chưa triển khai; không được cho phép bằng cách bỏ qua ràng buộc | Chưa chạy | NOT IMPLEMENTED |
| G-04 | Project role | Gán mã role không tồn tại | Employee hoạt động | Gửi POST Add với role tùy ý | `QA_FAKE_ROLE` | Từ chối; không ghi assignment | POST trả về danh sách với thông báo “Danh sách có vai trò không hợp lệ.”; tổng role assignments giữ nguyên 21 | PASS |
| G-05 | Project role | Người dùng tự thêm/đổi tên role hệ thống | Role catalog cố định | Kiểm tra route/controller quản trị và DB catalog | ProjectRoles | Không có chức năng quản trị thông thường để thêm/sửa/xóa role | GET `/ProjectRoles/Create` và `/ProjectRoles/Edit/1` đều 404; không có controller quản trị ProjectRoles; catalog vẫn có đúng 4 role, không thay đổi | PASS |

## H. Validation và xử lý lỗi

| Test Case ID | Module | Scenario | Preconditions | Test Steps | Test Data | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|---|---|
| H-01 | Validation | Chuỗi rỗng/khoảng trắng | Admin | POST ProjectCode/Name whitespace | `"   "` | Model/service từ chối; không ghi DB | POST với ProjectCode và Name chỉ có khoảng trắng trả form Create kèm lỗi validation; không tạo Project | PASS |
| H-02 | Validation | Chuỗi vượt giới hạn | Admin | Gửi trường vượt MaxLength | Name 201 ký tự | Form báo validation; không ghi DB | POST với Name 201 ký tự trả form Create kèm lỗi validation; không tạo Project | PASS |
| H-03 | Validation | Dữ liệu sai kiểu | Admin | Gửi số/ngày không parse được | Budget=`abc`; ngày=`bad` | Binding/ModelState lỗi; không có exception 500 | POST Budget=`abc`, StartDate=`bad` trả form Create với lỗi validation, HTTP 200; không có response 5xx | PASS |
| H-04 | Validation | Thiếu tham số hoặc ID không tồn tại | Đăng nhập | GET/POST không đủ route/body fields | thiếu projectId / ID 999999 | Request bị từ chối phù hợp; không sửa bản ghi khác | GET `/ProjectMembers/Index` thiếu projectId chuyển đến AccessDenied; GET `/Projects/Edit` thiếu id trả 404; không có 5xx | PASS |
| H-05 | Validation | POST lặp cùng role | Có assignment hiện hành | Gửi hai lần | Cùng project/member/role | Không duplicate; request sau có phản hồi lỗi nghiệp vụ rõ | MVC/service test gửi role active trùng; request bị từ chối và assignment không tăng | PASS |
| H-06 | Persistence | Lỗi FK hoặc DB constraint | Bản sao SQLite cô lập | Gửi FK sai hoặc thử insert vi phạm ràng buộc trực tiếp | Employee/project/role không tồn tại | DB từ chối; transaction không để lại dữ liệu một phần | SQLite từ chối 5 thử nghiệm trực tiếp: ProjectCode trùng, ProjectManagerId không tồn tại, ProjectMember trùng, role assignment active trùng, ProjectRoleCode không tồn tại. Tất cả trong SAVEPOINT và rollback; số hàng giữ nguyên (Projects 6, Members 12, Assignments 21), integrity `ok`, FK violations 0 | PASS |
| H-07 | Persistence | DB integrity sau thao tác ghi | Bản sao SQLite | Chạy `PRAGMA integrity_check`, `foreign_key_check` | DB sau test | integrity ok; không có violation | Đã chạy trên DB clone sau các request thử: `integrity_check=ok`, `foreign_key_check` không có dòng vi phạm | PASS |
| H-08 | Error handling | Kiểm tra log/exception sau các request âm | Ứng dụng chạy trên DB cô lập | Thu log và status code | Các input invalid trên | Không có unhandled exception hoặc dữ liệu nhạy cảm trong response | 4 request âm trả status 200, 200, 404, 400; response không chứa exception/stack trace. Log ASP.NET Core không có `fail`, `crit`, `Unhandled`, hoặc `Exception`; có 1 cảnh báo startup HTTPS redirect do chạy HTTP cục bộ | PASS |

