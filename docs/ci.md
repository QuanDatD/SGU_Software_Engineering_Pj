# Kiểm thử CI

Workflow `.github/workflows/ci.yml` build backend và frontend, chạy unit test, rồi kiểm tra tích hợp với SQL Server 2022 và Chromium. Job tích hợp tải lên báo cáo TRX và HTML Playwright, gồm trace cho ca trình duyệt thất bại.

| Bộ test | Phạm vi |
|---|---|
| SQL | TC04, TC18: SQL Server chặn SessionId trùng và chặn xóa trực tiếp điểm có bản nháp tham chiếu. |
| API | TC04, TC06, TC16, TC18, TC22, TC23, TC26, TC27, TC35 và người tạo nháp UC05.1–05.2: QR đồng thời, quyền Admin và CSRF, xóa điểm, vòng đời xuất bản, lưu người tạo nháp, trạng thái job trong lúc gọi Azure và khi chạy lại. |
| Playwright | TC02–TC05, TC16, TC35: chọn ngôn ngữ trước GPS, mở bản đồ khi từ chối vị trí, ghi nhận QR qua frontend và đăng nhập quản trị. |

Các ca trên dùng dữ liệu tạo trong lúc chạy. Test API dùng SQL Server thật và dịch vụ Azure giả lập. Playwright chạy với API thật và mô phỏng việc trình duyệt từ chối cấp vị trí. Các ca nghiệm thu thiết bị thật và GPS thực địa vẫn cần kiểm thử riêng.

## Chạy cục bộ

Cần .NET 10 SDK, Node.js 24 và SQL Server. Cấp quyền tạo và xóa database tạm cho tài khoản chạy bộ test. Đặt `ZOO_TEST_SQL` thành chuỗi kết nối đến máy SQL Server thử nghiệm và `ZOO_E2E_SQL` thành chuỗi kết nối tới một database thử nghiệm riêng, còn trống. Có thể dùng Windows Authentication trên máy cục bộ hoặc SQL Authentication trên SQL Server thử nghiệm. Cổng 5173 và 5230 cần trống cho Playwright.

```powershell
dotnet build backend/Zoo.sln -c Release
dotnet test backend/Zoo.Tests/Zoo.Tests.csproj -c Release --filter 'Category!=SQL&Category!=API'
dotnet test backend/Zoo.Tests/Zoo.Tests.csproj -c Release --filter 'Category=SQL|Category=API' --logger 'trx;LogFileName=integration.trx'

Set-Location web
npm ci
npm run build
npx playwright install chromium
npm run test:e2e
```

Backend test tự tạo, migrate và dọn database riêng. Playwright chạy API ở chế độ Development để dùng tài khoản mẫu của ứng dụng. Có thể xóa database E2E riêng sau khi thoát test. Xem báo cáo bằng `npx playwright show-report` trong thư mục `web`.
