# Kiểm thử CI

Phạm vi ngày 26/11/2026 dựa trên `docs/PRD_tieng_viet.md`, phần 11 của PRD và **List cần làm hôm nay 26-11.docx**. Số mục trong bảng giữ theo file danh sách. Ba assertion đã bàn giao ở lượt trước vẫn nằm trong CI.

Theo BR-27 và §9.7, trạng thái `Pending` của PRD tương đương `JobStatus.Queued` (giá trị 0, API trả `queued`). `Running` tương đương `JobStatus.Processing` (giá trị 1, API trả `processing`). `Succeeded` và `Failed` giữ nguyên nghĩa. Test quan sát trực tiếp cặp trạng thái và giai đoạn `Translate` hoặc `Synthesize` trong DB tại thời điểm gọi Azure giả lập.

## Đối chiếu danh sách 26/11 và PRD

| Mục danh sách hoặc mã PRD | Tên test | Kết quả | Phần còn thiếu hoặc cần kiểm thủ công |
|---|---|---|---|
| **1.1** Rà dữ liệu cũ | Các ca SQL BR-13, BR-18, BR-27 bên dưới | Có ca kiểm tra ràng buộc trên dữ liệu tự tạo | Rà bản sao DB thực, đếm và xử lý job trùng, nhiều Published cùng điểm và con trỏ public sai điểm trước khi áp các migration ràng buộc mới. |
| **1.2** Theo dõi bản dịch, BR-15 | Chờ điểm kiểm thử | Chưa chạy | Schema hiện có `IsEditedByAdmin` nhưng chưa có trạng thái Cần duyệt lại hoặc dấu phiên bản/hash nguồn dùng để đối chiếu. |
| **1.3** Job duy nhất, BR-27 | `Br27_SqlRejectsDuplicateJobForRevisionAndLanguage` | Không đạt | SQL nhận job thứ hai cho cùng phiên bản và ngôn ngữ. Ca có đối chứng với ngôn ngữ khác và phiên bản khác. |
| **1.3** Ràng buộc public, BR-13, BR-18 | `Br18_SqlRejectsTwoPublishedRevisionsForSamePlace`, `Br18_SqlRejectsPublishedPointerToAnotherPlace`, `Br18_SqlRejectsPublicPointerToDraftOfSamePlace` | Không đạt | Cần giới hạn một Published/điểm và kiểm cả quyền sở hữu lẫn trạng thái của phiên bản được trỏ đến. |
| **1.4** Xóa điểm có phiên bản, BR-25/TC18 | `Tc18_RawDeleteRejectsPlaceReferencedOnlyByDraft`, `Tc18_DeleteThroughApiRetainsPlaceAndDraft` | SQL không đạt, API đạt | SQL vẫn xóa dây chuyền. API trả 409 và giữ điểm cùng bản nháp. |
| **1.4** Bảo vệ Published/Archived, BR-14 | `Br14_SqlRejectsScriptUpdatesToPublished`, `Br14_SqlRejectsScriptUpdatesToArchived`, `Br14_SqlRejectsDeletionOfPublished`, `Br14_SqlRejectsDeletionOfArchived` | Không đạt | SQL vẫn sửa script hoặc xóa phiên bản. Ca SQL xóa tách quy tắc trạng thái khỏi FK của con trỏ public. `Tc21_ApiRejectsEditsAndDeletesOfPublishedAndArchived` đạt qua HTTP. |
| **1.4**, **2.4** Chuyển trạng thái khi xuất bản, TC06/TC26/TC27 | `Tc06_Tc26_Tc27_PublicEndpointShowsOnlyThePublishedRevision`, `Br18_FailedPublicPointerWriteRollsBackPublication` | Đạt | Xác nhận bản cũ Archived, bản mới Published và con trỏ public đúng khi thành công. Nếu SQL từ chối ghi con trỏ mới, bản cũ vẫn public và bản mới vẫn Draft. |
| **1.5** Migration | `AllMigrationsApplyToAnEmptyDatabase`, `UpgradeFromInitialMigrationPreservesLegacyPublicationAndJob` | Đạt | Nâng cấp từ migration đầu với Admin, điểm Published, script và job mẫu. Bản sao DB thực vẫn cần kiểm riêng khi có dữ liệu theo mục 1.1. |
| **2.1** Tách UC06 sang Zoo.BUS | Các ca API UC06 là kiểm thử hợp đồng qua HTTP | Chờ rà mã | Logic hiện còn trong controller. Khi tách service, chạy lại các test HTTP làm hồi quy và rà ranh giới controller/BUS bằng code review. |
| **2.2** Người tạo Draft, UC05.1–05.2 | `Uc05_CreateDraftRecordsAdminAndUpdateByDifferentAdminKeepsCreator` | Không đạt | Bản nháp tạo khi lưu điểm thiếu `CreatedByAdminId`. |
| **2.2** Dữ liệu khi tạo Draft và đổi nguồn, BR-15 | `Br15_SourceChangePreservesExistingTranslation` | Không đạt | API xóa script bản dịch cũ. Endpoint tạo bản nháp hiện chỉ nhận `SourceText`. Chờ dữ liệu duyệt lại để kiểm giữ nguyên, cập nhật và gắn với nguồn mới. |
| **2.2** Hiệu lực audio, TC24 | `Br15_SavingTheSameScriptKeepsAudioValid`, `Tc24_ChangedScriptInvalidatesOnlyItsOwnAudio` | Cùng script không đạt, script thay đổi đạt | Lưu lại nội dung giống hệt vẫn làm mất hiệu lực audio. |
| **2.3** Ghi DB trước Azure, BR-27/TC22/TC23 | `Tc22_Tc23_JobIsPersistedBeforeAzureAndRetryKeepsExistingJobs` | Không đạt | Thiếu trạng thái hoặc giai đoạn đã lưu trước khi gọi Azure. |
| **2.3** Chặn chạy trùng và retry đúng job, BR-16/BR-27 | `Br27_RepeatedGenerateDoesNotCallProviderForProcessingJob`, `Br27_RetryTargetsOneExistingJobAndIncrementsAttemptCount` | Không đạt | Gọi lại Azure khi job còn Processing. Retry xử lý thêm job lỗi khác và không tăng AttemptCount. Ca retry kiểm các job và audio thành công vẫn được giữ. |
| **2.3** Giữ bản dịch chỉnh tay khi tạo audio, BR-16 | `Br16_GenerationUsesManuallyEditedScript` | Đạt | Giữ script do Admin sửa, sinh audio khớp script đó. |
| **2.3** Khôi phục job, TC33 | `Tc33_RestartRequeuesInterruptedJobsAndKeepsActiveLease` | Đạt | Khởi động lại API thật với cùng DB, kiểm job hết hạn hoặc chưa có khóa, đồng thời giữ job còn khóa. |
| **2.4** Khóa Draft khi Running, BR-14/TC21 | `Br14_TranslationUpdateRejectsDraftWithProcessingJob`, `Br14_DeleteRejectsDraftWithProcessingJob` | Sửa không đạt, xóa đạt | API vẫn nhận sửa nội dung khi DB đã có Processing. Ca xóa có đối chứng khi job chuyển Failed. |
| **2.4** Điều kiện xuất bản, BR-17/TC26 | `Br17_PublishRejectsDraftWithUnfinishedJobEvenWhenAudioIsReady`, `Tc26_PublishRejectsHashMismatch`, `Tc26_PublishRejectsMissingAudioFile` | Job chưa xong không đạt. Sai hash và thiếu file đạt | Chờ trạng thái duyệt lại để tạo dữ liệu đủ 5 audio nhưng còn bản dịch chưa duyệt. |
| **2.5** Bán kính, BR-05/TC17 | `Br05_ApiAcceptsRadiusGreaterThanZeroAndBelowFive`, `Br05_ApiEnforcesRadiusUpperAndLowerBounds` | 0,5 m không đạt. Mốc 0, 500 và trên 500 đạt | DTO hiện áp mức tối thiểu 5 m. |
| **2.5** Sửa thông tin điểm | `Uc05_UpdatingOnlyGpsKeepsRevisionHistory`, `Uc05_UpdatingOnlyRadiusKeepsRevisionHistory`, `Uc05_UpdatingOnlyImageKeepsRevisionHistory` | Không đạt | Ba ca dùng điểm có Published và kiểm lịch sử phiên bản khi chỉ đổi một loại dữ liệu. |
| **3.1** Form Draft | Chờ điểm kiểm thử | Chưa chạy | Form chỉ có script; cần trường tiêu đề và mô tả trong luồng tạo Draft. |
| **3.2** Cần duyệt lại, Giữ nguyên/Cập nhật | `UC06.5 Admin can save and reopen a manually edited translation` | Sửa bản dịch đạt | Chờ nhãn Cần duyệt lại và hai thao tác xác nhận theo nguồn mới. |
| **3.3** Tiến độ, lỗi, retry và nghe thử, TC25 | `UC06.4/UC06.6 Admin sees job results, previews audio and retries failed English`, `Tc25_PreviewStreamsReadyAudioOnlyForAdmin` | Đạt cho kết quả, retry và nghe thử | UI hiện tải kết quả sau khi Generate hoàn tất. Chờ giao diện theo dõi tiến độ theo từng ngôn ngữ trong lúc job chạy. |
| **3.4** Xác nhận xuất bản, UC06.7 | `UC06.7 cancelling publication keeps the draft and confirmation publishes` | Không đạt | Nút hiện gửi POST ngay. UI chưa có bề mặt thông báo chi tiết khi phiên bản không đủ điều kiện. API đã có ca kiểm nguyên nhân thiếu file hoặc sai hash theo ngôn ngữ. |
| **4.1** Dấu nguồn và phiên QR, BR-20–BR-22/TC04/TC05 | `BR20 only entry marked source=qr sends a QR event to the API`, `TC04/TC05 QR entry creates a session and reopening the link reuses it`, `Tc04_ConcurrentQrRequestsCountOnlyOnce`, `Tc04_DatabasePreventsDuplicateQrSession` | Đạt | Quét mã bằng điện thoại và URL public vẫn cần kiểm thủ công. |
| **4.1** Dọn QR sau 90 ngày, §8.4 | Chờ điểm kiểm thử | Chưa chạy | Chưa có dịch vụ, lịch chạy, command hoặc endpoint dọn dữ liệu. Cần kiểm mốc 90 ngày bằng thời điểm UTC cố định khi có chức năng. |
| **4.2** GPS đầu tiên, UC02.2/TC07 | `TC07 first valid location is brought into the map viewport` | Không đạt | Nhận tọa độ ngay lần bấm đầu nhưng bản đồ chưa tự đưa dấu vị trí vào vùng nhìn thấy. GPS thiết bị thật vẫn cần kiểm thủ công. |
| **4.3** Làm lại mô phỏng, §5.10/TC34 | `TC34 stopping and starting simulation resets route progress without requesting GPS` | Đạt cho Tắt rồi Bắt đầu | Chưa có nút Làm lại. Chờ luồng đặt lại audio/lịch sử demo và loại kết quả đánh giá vùng cũ khi làm lại. |
| **4.4** Báo cáo chỉ đổi khi Áp dụng, UC07.1 | `UC07.1 report keeps the applied dates and chart until Apply` | Không đạt | Đổi ngày đã tự gửi request và đổi nhãn/biểu đồ. Sau khi bấm Áp dụng, test kiểm API nhận ngày kết thúc không bao gồm và nhãn UI hiển thị ngày kết thúc bao gồm. |
| **4.4** Dữ liệu báo cáo, TC28–TC30/TC36 | `Tc28_InvalidRangeAndEmptyReportHaveDistinctResponses`, `Tc29_Tc30_Tc36_ReportingUsesVietnamMidnightAndExcludesDemoByDefault` | Đạt | Dữ liệu cố định sát nửa đêm Việt Nam, có đồng hạng và lượt demo. |
| **PRD TC02, TC03, TC16, TC35** | Playwright `TC02/TC03 language selection precedes denied GPS and the map still opens`, `TC16/TC35 protected admin redirects and valid seed login opens the dashboard`; API `Tc16_Tc35_AdminRequiresValidCredentialsAndCsrf` | Đạt | Quyền vị trí được mô phỏng. |

## Kết quả lần chạy ngày 26/11/2026

Môi trường Windows, .NET 10, Node.js 24, Chromium và SQL Server cục bộ 17.0.1000.7. Build backend, BrowserHost và frontend đạt. `actionlint` 1.7.12 và kiểm kiểu file Playwright đều đạt. NuGet Audit không truy cập được feed ở lượt đầu; build lặp lại bằng cache cục bộ với audit tắt. Việc đó không làm mất ca kiểm thử nào. Workflow trên GitHub Actions chưa chạy. Job CI cấu hình SQL Server 2022.

| Bộ test | Đạt | Thất bại nghiệp vụ | Chưa chạy |
|---|---:|---:|---:|
| Unit | 12 | 0 | 0 |
| SQL, gồm migration | 3 | 9 | 0 |
| API | 16 | 12 | 0 |
| Playwright Chromium | 7 | 3 | 0 |
| **Tổng** | **38** | **24** | **0** |

Bảy nhóm chưa có ca chạy tự động gồm bản sao DB thực, trạng thái và thao tác Cần duyệt lại, trường tiêu đề/mô tả của Draft, tiến độ trực tiếp theo ngôn ngữ, lý do thiếu điều kiện xuất bản trên UI, dọn QR sau 90 ngày và thao tác Làm lại mô phỏng. Quét QR trên điện thoại và GPS thực địa cần kiểm thủ công. Mục tách BUS cần rà mã.

## Chạy cục bộ

Cần .NET 10 SDK, Node.js 24, Chromium của Playwright và SQL Server thử nghiệm. Tài khoản SQL cần quyền tạo/xóa database tạm. Đặt `ZOO_TEST_SQL` là chuỗi kết nối đến máy SQL thử nghiệm. Đặt `ZOO_E2E_SQL` là chuỗi kết nối chỉ rõ một database **chưa tồn tại**, có tên bắt đầu bằng `ZooCi_`. Cổng 5173 và 5230 cần trống.

```powershell
dotnet build backend/Zoo.sln -c Release
dotnet build backend/Zoo.Tests.BrowserHost/Zoo.Tests.BrowserHost.csproj -c Release
dotnet test backend/Zoo.Tests/Zoo.Tests.csproj -c Release --no-build --filter 'Category!=SQL&Category!=API'
dotnet test backend/Zoo.Tests/Zoo.Tests.csproj -c Release --no-build --filter 'Category=SQL|Category=API' --logger 'trx;LogFileName=integration.trx'

Set-Location web
npm ci
npm run build
npx playwright install chromium
npm run test:e2e
```

Job tích hợp chạy trên SQL Server 2022, giữ TRX và báo cáo HTML kèm trace khi Chromium lỗi. BrowserHost khởi động API thật bằng Kestrel với Azure giả lập và âm thanh kiểm thử. Nó tạo database riêng và dọn database khi nhận tín hiệu dừng. Nếu Windows kết thúc tiến trình đột ngột, dọn đúng database thử nghiệm vừa tạo trước khi dùng lại tên đó. Các ca Chromium chạy tuần tự vì API dùng khóa ghi SQL dùng chung trong một database. Xem báo cáo từ thư mục `web` bằng `npx playwright show-report`.

Patch lượt này áp trên commit nền cục bộ `35796cd67299b0be73a4bbb48abe5221e37f15b8`. Nội dung của commit đó là `cd91eaa` cộng patch 13 file `ci-github-actions.patch` của lượt trước.
