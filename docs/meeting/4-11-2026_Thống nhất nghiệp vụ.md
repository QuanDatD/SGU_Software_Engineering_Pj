# Nhật ký họp số 01

## Thông tin

- Ngày và thời gian: 20h ngày 04/11/2026.
- Thành viên tham gia: Dương Quan Đạt, Nguyễn Hồng Khương, Trần Trung Hậu.
- Người điều phối: Chưa ghi nhận.
- Người ghi biên bản: Chưa ghi nhận.
- UC hoặc vấn đề được xem xét: Nghiệp vụ quản lý điểm, phiên bản thuyết minh, dịch/audio và hợp đồng dữ liệu.

## Kết quả từ buổi trước

| Công việc | Kết quả thực tế | Bằng chứng hoặc vướng mắc |
|---|---|---|
| Chuẩn bị tài liệu thống nhất nghiệp vụ | Đã có nội dung để nhóm rà soát. | Tài liệu nghiệp vụ và PRD. |
| Rà soát mã nguồn | Phát hiện thiếu người tạo nháp, FK xóa dây chuyền và trạng thái job chưa lưu trước khi gọi Azure. | PlaceEditingService, ZooDbContext, AdminNarrationController; chưa kiểm thử chạy thực tế. |

## Quyết định trong buổi họp

| Mã | Nội dung thống nhất | Lý do | Phần bị ảnh hưởng |
|---|---|---|---|
| QD-01 | Đồng ý các nghiệp vụ đã đưa ra, ngoại trừ Q02 và Q04 cần thảo luận thêm. | Cần làm rõ vòng đời phiên bản và bảo toàn bản dịch thủ công. | UC05, UC06, PRD, API, DB |
| QD-02 | Hợp đồng dữ liệu bổ sung chưa được xem là yêu cầu chính thức khi nhóm chưa xác nhận. | Tránh triển khai khác nhau giữa các thành viên. | PRD, DTO, API, DB |

## Vấn đề chưa chốt

- Vấn đề: Chi tiết Q02, Q04 và hợp đồng dữ liệu bổ sung.
- Thông tin cần kiểm tra thêm: Bảo vệ Published/Archived, dấu nguồn bản dịch, MapX/MapY, enum job, hành vi FK và ánh xạ tên trường.
- Người tìm hiểu và thời hạn: Cả ba rà soát phần liên quan; tổng hợp trước buổi họp ngày 06/11/2026.

## Công việc tiếp theo

Bảng phân công bổ sung sau buổi họp; mỗi phần dự kiến thực hiện trong 1–3 giờ.

| Công việc trong UC | Người thực hiện | Người rà soát | Thời hạn | Điều kiện hoàn thành |
|---|---|---|---|---|
| UC05.1–05.2: Bổ sung CreatedByAdminId khi lưu điểm tạo nháp mới. | Đạt | Khương | Trước họp 06/11/2026 | Nháp mới đúng người tạo; sửa nháp cũ không đổi người tạo. |
| UC05.3: Đổi FK PlaceId từ Cascade sang NoAction, tạo migration sau khi nhóm xác nhận. | Khương | Hậu | Trước họp 06/11/2026 | API và SQL chặn xóa điểm có phiên bản; dữ liệu giữ nguyên. |
| UC06.4: Lưu trạng thái và giai đoạn job trước khi gọi Azure. | Hậu | Đạt | Trước họp 06/11/2026 | DB ghi nhận đang xử lý; thành công/thất bại được cập nhật đúng. |

## Kiểm thử đã thực hiện

- Tình huống: Chưa ghi nhận kiểm thử chạy thực tế trong buổi họp.
- Kết quả: Mới đối chiếu tài liệu và rà soát mã nguồn.
- Bằng chứng: Các vị trí code đã xác định; chưa có kết quả kiểm thử.
- Trường hợp chưa kiểm thử: Người tạo nháp, xóa điểm qua API/SQL và trạng thái job khi Azure đang xử lý hoặc gặp lỗi.

## Xác nhận

- Dương Quan Đạt: Chờ xác nhận biên bản.
- Nguyễn Hồng Khương: Chờ xác nhận biên bản.
- Trần Trung Hậu: Chờ xác nhận biên bản.
