# ĐẶC TẢ QUY TẮC NGHIỆP VỤ – TASK-02: QUẢN LÝ DỰ ÁN VÀ THÀNH VIÊN DỰ ÁN

## 1. Mục đích

Tài liệu này xác định các quy tắc nghiệp vụ chính thức của TASK-02 – Quản lý dự án và thành viên dự án.

Tài liệu được xây dựng dựa trên 15 quyết định nghiệp vụ QD-01 đến QD-15 đã được thống nhất.

Mục tiêu:

- Làm căn cứ thiết kế database mục tiêu.
- Làm căn cứ điều chỉnh các Entity, Service, Controller và View.
- Làm căn cứ xây dựng kiểm thử chức năng.
- Bảo đảm không làm mất dữ liệu hiện có.
- Không xây dựng lại database từ đầu.

## 2. Phạm vi chức năng

| Mã | Function | Mô tả |
|---|---|---|
| FN-02-01 | Xem danh sách dự án | Hiển thị danh sách dự án |
| FN-02-02 | Xem chi tiết dự án | Hiển thị thông tin chi tiết dự án |
| FN-02-03 | Tạo dự án | Tạo dự án mới |
| FN-02-04 | Cập nhật dự án | Cập nhật thông tin dự án |
| FN-02-05 | Xem thành viên dự án | Xem danh sách thành viên của dự án |
| FN-02-06 | Thêm thành viên dự án | Thêm nhân sự vào dự án |

## 3. Quy tắc phân quyền quản lý thành viên

### QD-01 – Phạm vi quản lý thành viên

- Admin (Quản trị viên) được quản lý thành viên của tất cả dự án.
- Project Director (Giám đốc dự án) được quản lý thành viên của tất cả dự án.
- Project Manager (Quản lý dự án) chỉ được quản lý thành viên của dự án mà người đó đang giữ vai trò PM chính.
- Một người chỉ là thành viên của dự án khác không mặc nhiên có quyền quản lý thành viên dự án đó.
- Kiểm tra quyền phải thực hiện ở phía server, không chỉ ẩn hoặc hiện nút trên giao diện.

## 4. Quy tắc quản lý Project Manager

### QD-02 – PM tự động trở thành thành viên

Khi một người được bổ nhiệm làm PM chính:

- Hệ thống tự động tạo hoặc cập nhật ProjectMember tương ứng.
- Không tạo thành viên trùng nếu PM đã được thêm vào danh sách thành viên.
- Người đó phải có vai trò Project Manager trong dự án.

### QD-04 – Thay đổi PM và PM hỗ trợ

- Mỗi dự án chỉ có tối đa một PM chính tại một thời điểm.
- Có thể có nhiều người đảm nhiệm vai trò quản lý hỗ trợ theo quy tắc phân công.
- Khi thay PM chính, PM cũ không mặc nhiên bị xóa khỏi dự án.
- Nếu PM cũ còn vai trò khác đang hiệu lực, tiếp tục giữ tư cách thành viên.
- Nếu PM cũ không còn vai trò nào, kết thúc tư cách thành viên.
- Bổ nhiệm người quản lý hỗ trợ không đồng nghĩa với thay thế PM chính.

### QD-10 – Lịch sử bổ nhiệm PM

Hệ thống phải lưu lịch sử bổ nhiệm PM, tối thiểu gồm:

- Dự án.
- PM trước đó.
- PM mới.
- Thời điểm bắt đầu.
- Thời điểm kết thúc.
- Lý do thay đổi nếu có.

Không ghi đè lịch sử bổ nhiệm cũ khi thay PM.

Tại một thời điểm, mỗi dự án có tối đa một PM chính đang hiệu lực.

### QD-11 – Dự án tạm thời không có PM chính

- Cho phép dự án tạm thời không có PM chính.
- `ProjectManagerId` được phép `NULL`.
- Tình huống áp dụng gồm dự án mới thành lập, PM nghỉ việc hoặc giai đoạn bàn giao.
- Không tự động xem một thành viên khác là PM chính.
- Lịch sử PM trước đó phải được bảo toàn.
- Khi bổ nhiệm PM mới, phải ghi nhận lịch sử bổ nhiệm mới.

## 5. Quy tắc quản lý thành viên dự án

### QD-03 – Kết thúc tư cách thành viên

- Không xóa vật lý ProjectMember khi nhân viên rời dự án.
- Chuyển `IsActive` thành `false` khi thành viên không còn vai trò nào đang hiệu lực.
- Bảo toàn lịch sử thành viên.
- Nếu được thêm lại, xem xét tái kích hoạt ProjectMember hiện có thay vì tạo bản ghi trùng.

### QD-06 – Nguồn nhân sự khi thêm thành viên

- Ưu tiên chọn Employee (Hồ sơ nhân viên) đã tồn tại.
- Nếu chưa có hồ sơ Employee, sử dụng quy trình của TASK-07 để tạo hồ sơ.
- TASK-02 không tự xây dựng quy trình quản lý hồ sơ nhân sự riêng.
- Không tạo trùng Employee hoặc tài khoản Identity.

### QD-07 – Một nhân viên có nhiều vai trò trong một dự án

- Một nhân viên có thể đảm nhiệm nhiều vai trò trong cùng một dự án.
- Không tạo nhiều ProjectMember cho cùng một Employee trong cùng dự án.
- Các vai trò được quản lý bằng những bản ghi phân công riêng.

### QD-08 – Thay đổi vai trò

- Khi thay đổi vai trò, kết thúc bản ghi phân công cũ bằng `EndedAt`.
- Tạo bản ghi phân công mới cho vai trò mới.
- Giữ lại lịch sử phân công.
- Cho phép nhiều vai trò đồng thời nếu nghiệp vụ cho phép.
- Kết thúc một vai trò không đồng nghĩa kết thúc toàn bộ tư cách thành viên.

### QD-09 – Nhân viên rời dự án

Cho phép:

- Kết thúc một vai trò cụ thể.
- Kết thúc toàn bộ vai trò của nhân viên trong dự án.

Quy tắc cập nhật:

- Nếu còn ít nhất một vai trò đang hiệu lực, `ProjectMember.IsActive = true`.
- Nếu không còn vai trò nào đang hiệu lực, `ProjectMember.IsActive = false`.
- Không xóa lịch sử phân công.

## 6. Quy tắc danh mục vai trò

### QD-12 – Danh mục vai trò cố định

- Danh mục vai trò do hệ thống định nghĩa.
- Người dùng chỉ được lựa chọn các vai trò có sẵn.
- Admin không được tự thêm, sửa tên hoặc xóa vai trò qua chức năng quản trị thông thường.
- Nếu cần bổ sung vai trò, thực hiện thông qua thay đổi có kiểm soát ở cấp hệ thống.

Danh sách vai trò chính thức cần được đối chiếu với mã nguồn và dữ liệu hiện tại trước khi thiết kế database. Không tự ý suy diễn hoặc tạo thêm vai trò.

## 7. Quy tắc kiêm nhiệm và phân công nhiều dự án

### QD-05 – Phân công nhân sự trên nhiều dự án

- Project Manager (Quản lý dự án) có thể tham gia nhiều dự án nếu được phân công.
- Deputy Project Manager (Phó quản lý dự án) có thể tham gia nhiều dự án nếu được phân công.
- Site Manager (Quản lý công trường) thông thường chỉ phụ trách một dự án tại một thời điểm.
- Supervising Engineer (Kỹ sư giám sát) thông thường chỉ phụ trách một dự án tại một thời điểm.
- Nhân sự thuộc các vai trò khác có thể được phân công nhiều dự án theo nhu cầu.
- Trường hợp ngoại lệ đối với vai trò bị giới hạn phải có cơ chế phê duyệt có thẩm quyền.

Chi tiết quy trình phê duyệt ngoại lệ sẽ được xử lý trong giai đoạn nghiệp vụ và phân quyền tương ứng, không tự ý triển khai trong TASK-02 nếu chưa có đặc tả.

## 8. Quy tắc nghỉ việc và vô hiệu hóa tài khoản

### QD-13 – Phân biệt trạng thái nhân sự và trạng thái tài khoản

**Trường hợp nhân viên thực sự nghỉ việc:**

- Cập nhật trạng thái nhân sự.
- Kết thúc tất cả phân công vai trò dự án đang hiệu lực.
- Chặn phân công mới cho nhân viên đã nghỉ việc.
- Giữ nguyên lịch sử nhân viên, thành viên dự án và phân công.

**Trường hợp tài khoản chỉ bị khóa tạm thời:**

- Chỉ cập nhật trạng thái tài khoản đăng nhập.
- Không tự động kết thúc phân công dự án.
- Không đồng nhất việc khóa tài khoản với việc nhân viên nghỉ việc.

## 9. Quy tắc phân công lại cùng vai trò

### QD-14 – Tạo bản ghi mới khi phân công lại

Ví dụ: Nhân viên A từng là Site Manager (Quản lý công trường) của dự án X, đã kết thúc phân công và sau đó được bổ nhiệm lại.

Quy tắc:

- Giữ nguyên bản ghi phân công cũ.
- Không ghi đè ngày bắt đầu hoặc ngày kết thúc cũ.
- Tạo bản ghi phân công mới với StartedAt mới.
- Cho phép nhiều giai đoạn phân công cùng vai trò trong lịch sử.
- Không cho phép hai bản ghi phân công trùng nhau đang hiệu lực cho cùng Employee, Project và Role.

## 10. Quy tắc mã dự án

### QD-15 – Mã dự án duy nhất toàn hệ thống

- `ProjectCode` phải duy nhất trên toàn hệ thống.
- Dự án đã lưu trữ vẫn giữ mã cũ.
- Không cho phép sử dụng lại mã của dự án đã lưu trữ.
- Kiểm tra trùng mã ở tầng nghiệp vụ và database.
- Lưu trữ dự án không làm giải phóng mã dự án.

## 11. Nguyên tắc dữ liệu và bảo toàn lịch sử

1. Không xóa vật lý dữ liệu lịch sử phân công khi thay đổi nhân sự.
2. Không ghi đè lịch sử PM.
3. Không suy diễn ngày bắt đầu phân công từ ngày tạo thành viên nếu dữ liệu cũ không chứng minh được điều đó.
4. Không tự tạo lịch sử PM trong quá khứ khi chỉ có thông tin PM hiện tại.
5. Dữ liệu cũ không xác định được thời gian phải được xử lý minh bạch, không tự đặt ngày giả.
6. Mọi thay đổi schema phải thực hiện qua migration có kiểm soát.
7. Phải kiểm tra dữ liệu hiện có trước khi thêm ràng buộc unique hoặc foreign key.
8. Không tự ý xóa hoặc đổi tên các bảng đang được Function khác sử dụng.

## 12. Nguyên tắc triển khai

- Database hiện tại là nền tảng để nâng cấp, không tạo database mới từ đầu.
- Tận dụng Entity, bảng và quan hệ đang có nếu phù hợp.
- Trước khi thay đổi phải kiểm tra các Service, Controller, View và Function đang sử dụng dữ liệu liên quan.
- Thay đổi database phải có kế hoạch migration và rollback.
- Không triển khai chi tiết Role–Task–Function Authorization trong giai đoạn này.
- Không loại bỏ cơ chế đăng nhập hoặc các biện pháp bảo vệ cơ bản hiện có.
- Triển khai và kiểm thử từng Function.

## 13. Trạng thái tài liệu

Tài liệu này ghi nhận 15 quyết định nghiệp vụ đã được người dùng thống nhất.

Các nội dung về cấu trúc bảng, tên cột, quan hệ, index và migration sẽ được xác định trong tài liệu riêng:

`TASK_02_TARGET_DATABASE_DESIGN.md`

Không coi tài liệu nghiệp vụ này là thiết kế database hoặc chỉ dẫn tự động thay đổi schema.
