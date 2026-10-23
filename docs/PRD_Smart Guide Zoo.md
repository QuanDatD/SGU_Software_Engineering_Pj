# TÀI LIỆU YÊU CẦU SẢN PHẨM (PRODUCT REQUIREMENTS DOCUMENT - PRD)
## DỰ ÁN: HỆ THỐNG BẢN ĐỒ TƯƠNG TÁC & THUYẾT MINH TỰ ĐỘNG THẢO CẦM VIÊN SÀI GÒN (SAIGON ZOO SMART AUDIO GUIDE)

---

### THÔNG TIN TÀI LIỆU
* **Tên dự án:** Saigon Zoo Smart Audio Guide & Interactive Map System
* **Phiên bản:** 1.0.0
* **Trạng thái:** Approved / In Implementation
* **Nền tảng:** Web Responsive (Mobile-First Web App) & Single-Page Application (SPA)
* **Công nghệ chủ đạo:**
  * **Frontend:** React 19, TypeScript, Vite, Leaflet OpenStreetMap.
  * **Backend:** ASP.NET Core Web API (.NET 9 / C#), Entity Framework Core.
  * **Database:** Microsoft SQL Server.
  * **AI Services:** Azure AI Translator, Azure AI Speech (Neural Voices).

---

## 1. TỔNG QUAN & BỐI CẢNH (OVERVIEW & PROBLEM STATEMENT)

### 1.1. Bối cảnh
Thảo Cầm Viên Sài Gòn là một trong 8 vườn thú lâu đời nhất trên thế giới với bề dày lịch sử từ năm 1864, sở hữu diện tích hơn 12 ha cùng hàng ngàn cá thể động thực vật quý hiếm. Việc truyền tải giá trị lịch sử, kiến thức bảo tồn và tập tính sinh học của từng cá thể đến du khách đóng vai trò sống còn trong việc nâng tầm trải nghiệm du lịch thông minh tại TP. Hồ Chí Minh.

### 1.2. Vấn đề thực tế (Problem Statement)
1. **Hạn chế của bảng biển vật lý:** Không gian chữ hạn chế, dễ xuống cấp do thời tiết nhiệt đới và thường chỉ cung cấp tối đa 1–2 ngôn ngữ (Việt - Anh).
2. **Thiếu hụt nhân lực hướng dẫn viên:** Không thể đáp ứng lượng khách đông vào mùa cao điểm và khó hỗ trợ các ngôn ngữ quốc tế như tiếng Trung, Hàn, Nhật.
3. **Rào cản từ ứng dụng di động truyền thống (Native Apps):** Việc bắt du khách tải app từ Google Play / App Store (dung lượng nặng từ 80MB – 150MB) khiến tỷ lệ chuyển đổi bỏ ngang lên đến hơn $70\%$.
4. **Trải nghiệm định vị phức tạp:** Du khách vừa đi vừa phải tự bấm tìm điểm trên danh sách, gây phân tâm và mất đi tính trực quan tự nhiên khi ngắm nhìn thú.

### 1.3. Giải pháp sản phẩm (Product Solution)
Xây dựng nền tảng Web App siêu nhẹ (du khách mở ngay bằng trình duyệt không cần cài đặt), hoạt động theo mô hình:
$$\text{Quét mã QR tại cổng} \longrightarrow \text{Bật GPS nền} \longrightarrow \text{Tự động phát bài thuyết minh theo bước chân}.$$
Hệ thống tích hợp trí tuệ nhân tạo điện toán đám mây Azure AI để tự động dịch thuật và tổng hợp giọng đọc audio chất lượng cao.

### 1.4. Đối tượng sử dụng (Target Personas)
* **Du khách tham quan (Visitor):** Du khách trong nước và quốc tế, gia đình, học sinh, sinh viên có nhu cầu nghe thuyết minh tự động khi tiếp cận chuồng thú.
* **Quản trị viên / Biên tập viên (Admin):** Nhân sự phòng giáo dục, truyền thông vườn thú chịu trách nhiệm biên soạn kịch bản, chỉnh sửa bản dịch và xuất bản nội dung.

---

## 2. MỤC TIÊU & CHỈ SỐ THÀNH CÔNG (GOALS & SUCCESS METRICS)

### 2.1. Mục tiêu sản phẩm (Product Goals)
* Cung cấp giải pháp số hóa toàn diện cho 7 khu tham quan trọng điểm của Thảo Cầm Viên Sài Gòn.
* Tự động nhận diện ranh giới địa lý (Geofencing) với độ trễ phản hồi thời gian thực dưới 2 giây.
* Hỗ trợ chuẩn xác 5 ngôn ngữ: Tiếng Việt, Tiếng Anh (English), Tiếng Trung (中文), Tiếng Hàn (한국어), Tiếng Nhật (日本語).
* Cung cấp bộ công cụ quản trị thông minh (Admin Dashboard) tự động hóa quy trình dịch thuật và thu âm chỉ với 1 cú click chuột.

### 2.2. Phạm vi không thực hiện (Non-Goals / Out of Scope)
* Chưa tích hợp cổng thanh toán trực tuyến và chức năng bán vé qua mạng trong giai đoạn này.
* Chưa hỗ trợ điều hướng trong nhà (Indoor Navigation qua Bluetooth Beacons) mà tập trung hoàn toàn vào Geofencing GPS ngoài trời.
* Không xây dựng mô hình thực tế ảo tăng cường 3D AR nặng nề gây tốn pin thiết bị.

### 2.3. Chỉ số đo lường thành công (Success Metrics / KPIs)
* **Tỷ lệ kích hoạt (Activation Rate):** $\ge 85\%$ du khách quét mã QR đồng ý cấp quyền vị trí Geolocation.
* **Độ chính xác Geofence:** $100\%$ các trường hợp du khách bước vào bán kính chuồng thú đều kích hoạt đúng nội dung tương ứng.
* **Thời gian tải trang (Page Load Time):** Thời gian tải bản đồ lần đầu $\le 2\text{ giây}$ trên đường truyền mạng di động 4G.
* **Tối ưu hóa chi phí AI:** Tỷ lệ phát hiện nội dung không đổi qua thuật toán băm SHA-256 (`ContentHash`) đạt $100\%$, ngăn chặn hoàn toàn việc gọi thừa dịch vụ Azure tính phí.

---

## 3. LUỒNG TRẢI NGHIỆM NGƯỜI DÙNG (USER JOURNEY)

```
[Du khách tới cổng]
        │
        ▼ Quét mã QR đặt tại cổng soát vé
[Trình duyệt mở /?page=map&gps=auto]
        │
        ▼ Yêu cầu quyền vị trí (HTML5 Geolocation)
[Du khách bấm "Cho phép" (Allow)]
        │
        ├──► Bật chấm radar GPS thời gian thực trên Bản đồ số
        │
        ▼ Du khách bước đi trong Thảo Cầm Viên
[Polling Geofence API mỗi 2 giây]
        │
        ├─────────────────────────────────────────────────┐
        ▼                                                 ▼
[Vào bán kính 1 khu (VD: Hổ Đông Dương)]   [Đứng giữa ngã ba ranh giới 2 khu]
        │                                                 │
        ▼                                                 ▼
Tự động phát bài thuyết minh MP3           Hiển thị Pop-up chọn khu vực
Mở Spot Panel chi tiết loài thú             Khách bấm chọn 1 trong 2 khu
        │                                                 │
        └─────────────────┬───────────────────────────────┘
                          ▼
            Nghe thuyết minh audio liên tục
            Tùy ý đổi sang 1 trong 5 ngôn ngữ
```

---

## 4. ĐẶC TẢ CHI TIẾT CÁC USE CASE (USE CASE SPECIFICATIONS)

### PHÂN HỆ 1: DU KHÁCH THAM QUAN (VISITOR / USER)

#### UC-01: Quét mã QR & Khởi tạo phiên tham quan (GPS Onboarding)
* **Mã Use Case:** UC-01
* **Tác nhân:** Du khách tham quan.
* **Mô tả:** Du khách quét mã QR đặt tại cổng soát vé hoặc các biển chỉ dẫn bằng camera điện thoại để vào thẳng bản đồ số và kích hoạt định vị.
* **Tiền điều kiện:** Thiết bị di động có kết nối Internet (4G/Wi-Fi) và trình duyệt hỗ trợ HTML5 Geolocation.
* **Hậu điều kiện:** Giao diện bản đồ hiển thị, phiên định vị GPS được bật, vị trí hiện tại được ghim trên bản đồ.
* **Luồng sự kiện chính:**
  1. Du khách mở camera điện thoại quét mã QR tại cổng kiểm soát.
  2. Trình duyệt mở đường dẫn `/?page=map&gps=auto`.
  3. Ứng dụng nhận diện tham số `gps=auto` và tự động gọi hàm yêu cầu định vị `navigator.geolocation.watchPosition`.
  4. Trình duyệt hiển thị pop-up hỏi quyền truy cập vị trí của thiết bị.
  5. Du khách nhấn **Cho phép (Allow)**.
  6. Hệ thống nhận tọa độ Vĩ độ/Kinh độ và sai số (Accuracy), hiển thị chấm xanh radar và định tâm bản đồ về vị trí du khách.
* **Luồng rẽ nhánh / Ngoại lệ:**
  * *4a. Du khách bấm "Từ chối" (Deny):* Hệ thống chuyển sang UC-08 (Chế độ thủ công), ẩn chấm radar, hiển thị thanh cảnh báo và hướng dẫn du khách cách bật lại vị trí.
  * *6a. Tín hiệu GPS quá yếu (Accuracy > 250m):* Hệ thống hiển thị nhãn cảnh báo "Đang dò tìm tín hiệu GPS chính xác hơn..." trên thanh trạng thái.

---

#### UC-02: Xem bản đồ số & Theo dõi vị trí thời gian thực
* **Mã Use Case:** UC-02
* **Tác nhân:** Du khách tham quan.
* **Mô tả:** Du khách tương tác với bản đồ (kéo, thả, phóng to, thu nhỏ) và theo dõi tọa độ di chuyển thực tế của mình trong khuôn viên vườn thú.
* **Tiền điều kiện:** Đã hoàn thành UC-01 và cấp quyền vị trí.
* **Hậu điều kiện:** Marker vị trí của du khách dịch chuyển mượt mà trên bản đồ tương ứng với bước chân thực địa.
* **Luồng sự kiện chính:**
  1. Giao diện hiển thị bản đồ Thảo Cầm Viên kèm 7 điểm tham quan chính.
  2. Khi du khách bước đi, sự kiện `watchPosition` ghi nhận tọa độ mới mỗi 1–2 giây.
  3. Marker người dùng (chấm xanh radar) tự động dời đến tọa độ mới.
  4. Vòng tròn bán kính sai số (Accuracy Circle) co giãn theo chất lượng sóng GPS.
  5. Bảng HUD góc màn hình cập nhật liên tục: Vĩ độ (Lat), Kinh độ (Lng), Sai số ($\pm\text{m}$).
* **Luồng rẽ nhánh:**
  * *1a. Du khách kéo bản đồ ra xa vị trí của mình:* Chấm radar vẫn di chuyển ngầm; khi muốn tìm lại vị trí bản thân, du khách bấm nút **🎯 Định vị** để camera bay ngay về vị trí hiện tại.

---

#### UC-03: Tự động kích hoạt thuyết minh qua Geofence (Vùng đơn lẻ)
* **Mã Use Case:** UC-03
* **Tác nhân:** Du khách, Hệ thống Geofencing ngầm.
* **Mô tả:** Khi du khách di chuyển vào bán kính nhận diện của một khu vực (ví dụ: Chuồng Hổ Đông Dương), hệ thống tự động phát bài thuyết minh mà không cần bấm nút.
* **Tiền điều kiện:** Du khách đã cấp quyền GPS và đang di chuyển trong khuôn viên.
* **Hậu điều kiện:** Thanh phát Audio Player tự phát bài thuyết minh tương ứng bằng ngôn ngữ du khách đã chọn; Spot Panel hiển thị thông tin khu vực.
* **Luồng sự kiện chính:**
  1. Hook `useVisitSession` định kỳ gửi tọa độ hiện tại lên API `/api/visit/evaluate`.
  2. Backend tính khoảng cách Haversine giữa vị trí khách và tâm 7 khu vực.
  3. Hệ thống xác định khoảng cách $\le$ bán kính `RadiusMeters` của duy nhất 1 khu (ví dụ: `INDOCHINESE_TIGER`).
  4. Backend trả về `{ reasonCode: "IN_ZONE", placeId: "..." }`.
  5. Frontend kiểm tra bộ nhớ đệm `heardSet`: khu vực này chưa được nghe trong phiên tham quan.
  6. Ứng dụng tự động tải file MP3 tương ứng từ `/storage/...` và phát qua `AudioPlayer`.
  7. Panel chi tiết tự động mở lên, làm nổi bật (highlight) marker của chuồng thú trên bản đồ.
* **Luồng rẽ nhánh:**
  * *5a. Du khách vẫn đứng trong khu vực đó sau khi nghe xong bài:* Hệ thống ghi nhận ID vào `heardSet`, không phát lại lần 2 để tránh làm phiền (chống lặp âm thanh).
  * *6a. Khu vực chưa có file âm thanh MP3:* Kích hoạt Web Speech API của trình duyệt đọc tự động phần mô tả văn bản.

---

#### UC-04: Xử lý lựa chọn khi đứng giữa 2 vùng giao nhau (Multiple Zones Overlap)
* **Mã Use Case:** UC-04
* **Tác nhân:** Du khách, Hệ thống Geofencing.
* **Mô tả:** Khi du khách đứng ở ngã ba đường hoặc vị trí ranh giới giữa 2 chuồng thú gần nhau, hệ thống hiển thị bảng lựa chọn để du khách chủ động chọn khu vực muốn nghe.
* **Tiền điều kiện:** Du khách di chuyển đến vị trí nằm trong bán kính giao nhau của $\ge 2$ khu vực.
* **Hậu điều kiện:** Bài thuyết minh của khu vực do du khách chọn được kích hoạt.
* **Luồng sự kiện chính:**
  1. Backend tính toán khoảng cách và phát hiện tọa độ khách thỏa mãn điều kiện nằm giữa 2 khu (ví dụ: Chuồng Hổ và Chuồng Hươu Cao Cổ).
  2. Backend trả về `{ reasonCode: "MULTIPLE_ZONES", candidatePlaceIds: [ID_Hổ, ID_Hươu] }`.
  3. Frontend tạm ngưng cơ chế tự động phát ngẫu nhiên.
  4. Một bảng pop-up xuất hiện ở giữa màn hình: *"Bạn đang đứng gần 2 khu vực! Hãy chọn khu vực bạn muốn nghe thuyết minh:"*.
  5. Pop-up liệt kê 2 thẻ lựa chọn gồm: Biểu tượng icon, Tên khu theo ngôn ngữ hiện hành, Nút **🔊 Nghe**.
  6. Du khách chạm vào khu vực muốn nghe (ví dụ: bấm chọn *Khu Hổ Đông Dương*).
  7. Pop-up đóng lại; hệ thống phát âm thanh của khu vực đã chọn và mở Spot Panel tương ứng.
* **Luồng rẽ nhánh:**
  * *6a. Du khách không bấm chọn mà tiếp tục đi thẳng:* Tọa độ thay đổi; khi du khách tiến hẳn vào sâu 1 khu, pop-up tự động biến mất và luồng quay lại UC-03.

---

#### UC-05: Điều khiển phát âm thanh thuyết minh (Audio Player Controller)
* **Mã Use Case:** UC-05
* **Tác nhân:** Du khách tham quan.
* **Mô tả:** Du khách chủ động điều khiển bài thuyết minh thông qua thanh Audio Player cố định ở cạnh dưới màn hình.
* **Tiền điều kiện:** Đang có một bài thuyết minh được phát (tự động hoặc thủ công).
* **Hậu điều kiện:** Trạng thái phát âm thanh thay đổi theo ý muốn du khách.
* **Luồng sự kiện chính:**
  1. Du khách quan sát thanh Audio Player đang hiển thị: Tiêu đề bài, Trạng thái phát, Thanh trượt thời lượng (Progress bar), Thời gian hiện tại / Tổng thời gian.
  2. Du khách có thể thực hiện một trong các thao tác:
     * Nhấn nút **Tạm dừng (Pause)**: Âm thanh ngừng, nút chuyển thành **Phát tiếp (Play)**.
     * Nhấn nút **Phát tiếp (Play)**: Âm thanh tiếp tục chạy từ vị trí đã dừng.
     * Kéo thanh trượt (Seekbar): Âm thanh nhảy tới mốc thời gian vừa kéo.
     * Nhấn nút **Đóng/Dừng hẳn (Stop)**: Thanh Player thu nhỏ hoặc đóng lại, giải phóng tài nguyên âm thanh.

---

#### UC-06: Xem thông tin chi tiết điểm tham quan (Spot Detail Panel)
* **Mã Use Case:** UC-06
* **Tác nhân:** Du khách tham quan.
* **Mô tả:** Du khách xem ảnh chụp thực tế, tên khoa học, tình trạng bảo tồn trong Sách Đỏ và nội dung giới thiệu chi tiết về loài động vật/công trình.
* **Tiền điều kiện:** Du khách click vào một marker trên bản đồ hoặc hệ thống tự động mở khi vào khu vực.
* **Luồng sự kiện chính:**
  1. Spot Panel trồi lên từ cạnh dưới màn hình.
  2. Giao diện hiển thị: Ảnh minh họa chất lượng cao, Thẻ phân loại (Động vật/Thực vật/Di tích), Tiêu đề tên khu, Khoảng cách từ khách đến khu vực.
  3. Phần mô tả chi tiết hiển thị: Tên khoa học, phân loại Sách Đỏ IUCN, đặc điểm nhận dạng, tập tính sinh học và kịch bản thuyết minh.
  4. Du khách có thể bấm nút **🔊 Nghe thuyết minh** trên panel để nghe đọc.
  5. Du khách bấm dấu **✕** hoặc chạm vùng ngoài panel để đóng lại.

---

#### UC-07: Chuyển đổi đa ngôn ngữ (Multi-language Switching)
* **Mã Use Case:** UC-07
* **Tác nhân:** Du khách tham quan.
* **Mô tả:** Thay đổi toàn bộ ngôn ngữ hiển thị và thuyết minh của hệ thống sang 1 trong 5 ngôn ngữ: Tiếng Việt, English, 中文, 한국어, 日本語.
* **Tiền điều kiện:** Đang ở bất kỳ màn hình nào của ứng dụng.
* **Hậu điều kiện:** Giao diện, nhãn marker, thông tin chi tiết và giọng đọc audio đổi đồng bộ sang ngôn ngữ đích.
* **Luồng sự kiện chính:**
  1. Du khách nhấn vào nút chọn ngôn ngữ (Language Picker) trên thanh tiêu đề.
  2. Danh sách ngôn ngữ mở ra kèm cờ quốc gia tương ứng.
  3. Du khách chọn ngôn ngữ mong muốn (ví dụ: 🇬🇧 English).
  4. Hệ thống cập nhật `LanguageContext`:
     * Menu, nhãn nút bấm, chú thích đổi sang tiếng Anh.
     * Tên và mô tả của 7 khu vực cập nhật sang tiếng Anh.
     * Nếu đang phát bài tiếng Việt, trình phát lập tức chuyển sang phát file âm thanh tiếng Anh (`en.mp3`) của cùng khu vực đó.

---

#### UC-08: Tham quan thủ công khi mất tín hiệu / từ chối GPS (Fallback Mode)
* **Mã Use Case:** UC-08
* **Tác nhân:** Du khách tham quan.
* **Mô tả:** Cho phép du khách vẫn sử dụng được toàn bộ tính năng thuyết minh và xem bản đồ ngay cả khi không có kết nối GPS hoặc thiết bị không hỗ trợ.
* **Tiền điều kiện:** GPS ở trạng thái "Từ chối", "Lỗi" hoặc thiết bị mất sóng vệ tinh.
* **Luồng sự kiện chính:**
  1. Hệ thống hiển thị thanh thông báo: *"Đang ở chế độ xem thủ công (GPS chưa kích hoạt)"*.
  2. Du khách dùng ngón tay di chuyển tự do trên sơ đồ Thảo Cầm Viên.
  3. Du khách chạm vào bất kỳ marker chuồng thú nào muốn khám phá (ví dụ: chạm vào icon 🐘 Khu Voi).
  4. Spot Panel mở ra hiển thị hình ảnh và bài viết chi tiết.
  5. Du khách nhấn nút **🔊 Nghe thuyết minh** để nghe audio thủ công.

---

#### UC-09: Giả lập bước chân du khách (GPS Simulation Mode)
* **Mã Use Case:** UC-09
* **Tác nhân:** Người dùng kiểm thử, Báo cáo viên demo, Du khách thử nghiệm từ xa.
* **Mô tả:** Giả lập tọa độ GPS để kiểm chứng tính năng tự động phát và bảng chọn 2 vùng khi người dùng không đứng trực tiếp tại khuôn viên Thảo Cầm Viên.
* **Luồng sự kiện chính:**
  1. Người dùng mở thanh công cụ *"🚶 Giả lập bước chân du khách"* trên bản đồ.
  2. Người dùng bấm **🐯 Đi tới Chuồng Hổ Đông Dương**:
     * Hệ thống giả lập tọa độ `lat = 10.78985, lng = 106.70512`.
     * Kích hoạt UC-03: Bài thuyết minh Hổ Đông Dương tự động phát ra loa.
  3. Người dùng bấm **⚖️ Đứng giữa 2 khu vực**:
     * Hệ thống giả lập tọa độ nằm tại trung điểm giữa Chuồng Hổ và Chuồng Hươu Cao Cổ.
     * Kích hoạt UC-04: Bảng lựa chọn 2 khu vực lập tức hiện lên giữa màn hình.
  4. Người dùng bấm **❌ Tắt giả lập**: Trả hệ thống về trạng thái đọc GPS thực tế từ phần cứng thiết bị.

---

### PHÂN HỆ 2: BẢN QUẢN TRỊ VIÊN (ADMINISTRATION PANEL)

#### UC-10: Đăng nhập & Xác thực quản trị viên (Admin Authentication)
* **Mã Use Case:** UC-10
* **Tác nhân:** Quản trị viên (Admin).
* **Mô tả:** Xác thực quyền truy cập vào bảng điều khiển quản trị hệ thống.
* **Tiền điều kiện:** Quản trị viên đã được cấp tài khoản trong cơ sở dữ liệu `AdminUsers`.
* **Luồng sự kiện chính:**
  1. Quản trị viên truy cập đường dẫn `/admin`.
  2. Hệ thống kiểm tra phiên đăng nhập; nếu chưa có Cookie xác thực, chuyển hướng sang `/admin/login`.
  3. Quản trị viên nhập `Username` và `Password`, nhấn **Đăng nhập**.
  4. Backend kiểm tra tài khoản, băm mật khẩu qua `PasswordHasher<AdminUser>` và so khớp với cơ sở dữ liệu.
  5. Nếu khớp, Backend thiết lập Authentication Cookie an toàn (HttpOnly, Secure) và trả về thông tin Admin.
  6. Frontend chuyển hướng Quản trị viên vào trang quản trị `AdminPage.tsx`.
* **Luồng ngoại lệ:**
  * *4a. Sai tài khoản hoặc mật khẩu:* Hiển thị thông báo đỏ "Tên đăng nhập hoặc mật khẩu không chính xác", không tạo phiên làm việc.

---

#### UC-11: Quản lý thông tin điểm tham quan (Places / Spots Management)
* **Mã Use Case:** UC-11
* **Tác nhân:** Quản trị viên.
* **Mô tả:** Thêm mới, chỉnh sửa thông tin tọa độ GPS, bán kính quét Geofence, tọa độ pixel hiển thị trên sơ đồ bản đồ, hoặc ẩn/xóa mềm một điểm tham quan.
* **Luồng sự kiện chính:**
  1. Quản trị viên chọn tab **Điểm tham quan** (`SpotsAdmin`).
  2. Bảng danh sách hiển thị toàn bộ các điểm tham quan hiện có lấy từ CSDL.
  3. Quản trị viên bấm **Chỉnh sửa** trên một điểm (ví dụ: *Khu Hổ Đông Dương*):
     * Cập nhật tọa độ thực địa: Vĩ độ (Latitude), Kinh độ (Longitude).
     * Cập nhật bán kính nhận diện GPS (`RadiusMeters`: ví dụ đổi từ 25m lên 30m).
     * Cập nhật vị trí ghim trên sơ đồ minh họa (`MapX`, `MapY` theo kích thước pixel 1024×643).
     * Chọn danh mục (`animal` / `plant` / `facility`) và gán ảnh đại diện.
  4. Quản trị viên nhấn **Lưu thay đổi**.
  5. Hệ thống gọi API `PUT /api/admin/places/{id}` cập nhật bản ghi vào SQL Server.
  6. Danh sách điểm tham quan trên trang Web User lập tức phản ánh vị trí và bán kính mới.

---

#### UC-12: Tạo phiên bản thuyết minh mới (Create Narration Draft Revision)
* **Mã Use Case:** UC-12
* **Tác nhân:** Quản trị viên.
* **Mô tả:** Khởi tạo một phiên bản thuyết minh nháp (Draft Revision) với nội dung tiếng Việt mới cho một khu vực.
* **Tiền điều kiện:** Quản trị viên đã đăng nhập và chọn tab **Thuyết minh** (`NarrationAdmin`).
* **Hậu điều kiện:** Một bản ghi `NarrationRevisions` có trạng thái `Draft` và số phiên bản kế tiếp (`Version = Version cũ + 1`) được tạo ra trong CSDL.
* **Luồng sự kiện chính:**
  1. Quản trị viên chọn khu tham quan cần cập nhật từ menu thả xuống (Dropdown).
  2. Hệ thống tải lịch sử các phiên bản cũ của khu vực đó.
  3. Tại ô soạn thảo *"Nội dung tiếng Việt (bản gốc)"*, Quản trị viên nhập văn bản thuyết minh cập nhật mới.
  4. Quản trị viên bấm **📝 Tạo phiên bản nháp**.
  5. Backend thực hiện:
     * Tìm số `Version` lớn nhất hiện tại của khu vực, tăng lên 1 đơn vị.
     * Tạo bản ghi `NarrationRevisions` với `Status = Draft`.
     * Tạo bản ghi `NarrationTranslations` cho ngôn ngữ `vi` với văn bản vừa nhập và tính mã `ContentHash`.
  6. Giao diện thêm thẻ phiên bản mới với nhãn `📝 Bản nháp` lên đầu danh sách.

---

#### UC-13: Dịch thuật & Tổng hợp giọng nói tự động (Azure AI Translation & TTS Generation)
* **Mã Use Case:** UC-13
* **Tác nhân:** Quản trị viên, Azure AI Services.
* **Mô tả:** Tự động dịch bài thuyết minh tiếng Việt sang 4 ngôn ngữ (Anh, Trung, Hàn, Nhật) và tạo các file âm thanh MP3 tương ứng bằng giọng đọc trí tuệ nhân tạo.
* **Tiền điều kiện:** Đang có một phiên bản ở trạng thái `Draft`.
* **Luồng sự kiện chính:**
  1. Quản trị viên tìm đến phiên bản nháp mong muốn và bấm nút **🎙️ Generate audio**.
  2. Giao diện hiển thị trạng thái xử lý (`busy = true`).
  3. Backend tiếp nhận yêu cầu qua `POST /api/admin/revisions/{id}/generate`:
     * Đọc bài thuyết minh tiếng Việt gốc (`vi`).
     * Với mỗi ngôn ngữ đích (`en`, `zh`, `ko`, `ja`): Gọi **Azure AI Translator API** chuyển ngữ tự động.
     * Với toàn bộ 5 ngôn ngữ (`vi`, `en`, `zh`, `ko`, `ja`): Gọi **Azure AI Speech API** (Neural Voice) tổng hợp luồng âm thanh nhị phân MP3.
     * Lưu các file `.mp3` vào thư mục lưu trữ cục bộ: `storage/audio/{placeId}/{revisionId}/{lang}.mp3`.
     * Lưu đường dẫn `AudioPath`, mã hash và cập nhật `AudioStatus = Ready` vào bảng `NarrationTranslations`.
     * Cập nhật các tác vụ trong `GenerationJobs` thành `Succeeded`.
  4. Frontend nhận kết quả thành công, hiển thị 5 nút ngôn ngữ có dấu tick xanh (`✓`) kèm trình phát nghe thử cho từng thứ tiếng.
  5. Nút **📢 Xuất bản** được mở khóa và hiển thị trên thẻ phiên bản.
* **Luồng ngoại lệ:**
  * *3a. Lỗi kết nối Azure hoặc hết hạn quota:* Trạng thái Job của ngôn ngữ bị lỗi chuyển thành `Failed`, lưu thông báo lỗi cụ thể vào cột `LastError`. Giao diện hiển thị nút **Thử lại (Retry)** để Admin gọi lại riêng ngôn ngữ đó.

---

#### UC-14: Hiệu chỉnh thủ công bản dịch (Manual Translation Editing)
* **Mã Use Case:** UC-14
* **Tác nhân:** Quản trị viên.
* **Mô tả:** Cho phép quản trị viên xem và trau chuốt lại câu từ của các bản dịch do máy dịch trước khi tạo giọng đọc hoặc xuất bản.
* **Luồng sự kiện chính:**
  1. Trên thẻ phiên bản, Quản trị viên bấm vào nút ngôn ngữ muốn hiệu chỉnh (ví dụ: `🇬🇧 EN`).
  2. Hộp thoại modal *"Sửa bản dịch — EN"* hiện lên.
  3. Quản trị viên chỉnh sửa nội dung văn bản tiếng Anh theo đúng ngữ cảnh chuyên ngành sinh học.
  4. Quản trị viên bấm **Lưu**.
  5. Backend cập nhật `ScriptText` mới, đồng thời đánh dấu cờ `IsEditedByAdmin = true` để thuật toán tự động nhận diện và không bao giờ tự ý dịch đè mất công sức của con người.
  6. Trạng thái âm thanh của ngôn ngữ đó được đặt về `AudioStatus = Missing` để báo hiệu cần sinh lại file MP3 theo văn bản mới.

---

#### UC-15: Giám sát Job xử lý & Thử lại tác vụ lỗi (Job Monitoring & Retry)
* **Mã Use Case:** UC-15
* **Tác nhân:** Quản trị viên.
* **Mô tả:** Theo dõi tiến độ chạy ngầm của từng tác vụ dịch thuật, thu âm và bấm thử lại nếu xảy ra sự cố mạng.
* **Luồng sự kiện chính:**
  1. Quản trị viên xem danh sách các Job con đính kèm bên dưới thẻ phiên bản.
  2. Mỗi Job hiển thị: Cờ ngôn ngữ, Giai đoạn (`Translate` / `Synthesize` / `Store`), Trạng thái (`⏳ Đang chờ`, `⚙️ Đang xử lý`, `✅ Xong`, `❌ Lỗi`).
  3. Nếu có Job báo lỗi đỏ kèm dòng mô tả nguyên nhân, nút **Thử lại** xuất hiện kế bên.
  4. Quản trị viên bấm **Thử lại**.
  5. Hệ thống gọi lại tác vụ xử lý độc lập cho ngôn ngữ đó mà không cần chạy lại toàn bộ các ngôn ngữ đã hoàn tất.

---

#### UC-16: Xuất bản phiên bản thuyết minh (Publish Revision to Production)
* **Mã Use Case:** UC-16
* **Tác nhân:** Quản trị viên.
* **Mô tả:** Phê duyệt phiên bản nháp thành phiên bản chính thức công khai, lập tức đồng bộ nội dung và âm thanh mới ra ứng dụng của du khách.
* **Tiền điều kiện:** Phiên bản nháp đã tạo xong âm thanh (ít nhất có bài thuyết minh hoàn chỉnh).
* **Hậu điều kiện:** Khóa ngoại `Places.PublishedRevisionId` trỏ trực tiếp đến Revision vừa duyệt; Web User lập tức nghe được nội dung mới.
* **Luồng sự kiện chính:**
  1. Quản trị viên bấm nút **📢 Xuất bản** trên thẻ phiên bản nháp.
  2. Backend xử lý transaction trong cơ sở dữ liệu:
     * Tìm tất cả phiên bản cũ của khu vực này đang có trạng thái `Published`, đổi thành `Archived` (Lưu trữ lịch sử).
     * Đổi trạng thái phiên bản hiện tại thành `Published`, gán nhãn thời gian `PublishedAt = UTC Now`.
     * Cập nhật trực tiếp: `place.PublishedRevisionId = revision.Id`.
  3. Backend trả về thông báo thành công.
  4. Trên giao diện Admin, thẻ phiên bản đổi huy hiệu sang `✅ Đã xuất bản`.
  5. Ngay tại thời điểm đó, bất kỳ du khách nào đang mở bản đồ hoặc đi vào khu vực này sẽ nhận được đúng bản dịch và file âm thanh mới nhất vừa được xuất bản.

---

## 5. BẢNG TRUY VẾT YÊU CẦU & MA TRẬN CHỨC NĂNG (TRACEABILITY MATRIX)

| Mã Use Case | Tên Chức năng | Phân hệ | Màn hình / Component | API Endpoint tương ứng | Mức ưu tiên |
|:---:|---|:---:|---|---|:---:|
| **UC-01** | Quét QR & Khởi tạo phiên | Du khách | Cổng soát vé / `App.tsx` | `GET /?page=map&gps=auto` | **P0** |
| **UC-02** | Xem bản đồ & Vị trí GPS | Du khách | `MapPage.tsx`, `IllustratedMap.tsx` | Client Geolocation API | **P0** |
| **UC-03** | Tự động thuyết minh (1 vùng) | Du khách | `MapPage.tsx`, `AudioPlayer.tsx` | `POST /api/visit/evaluate` | **P0** |
| **UC-04** | Xử lý đứng giữa 2 vùng | Du khách | Pop-up chọn khu vực (`MapPage.tsx`) | `POST /api/visit/evaluate` | **P0** |
| **UC-05** | Điều khiển phát âm thanh | Du khách | `AudioPlayer.tsx`, `AudioContext.tsx` | Static Files `/storage/audio/...` | **P0** |
| **UC-06** | Xem thông tin chi tiết điểm | Du khách | `SpotPanel.tsx` | `GET /api/places/{id}?lang=...` | **P0** |
| **UC-07** | Đổi ngôn ngữ (5 thứ tiếng) | Du khách | `LanguageSelector.tsx` | `GET /api/places?lang=...` | **P0** |
| **UC-08** | Tham quan thủ công (Offline) | Du khách | `MapPage.tsx`, `staticPlaces.ts` | Local Storage Fallback | **P1** |
| **UC-09** | Giả lập bước chân du khách | Kiểm thử/Demo | Thanh mô phỏng (`MapPage.tsx`) | Mock Coordinates Dispatcher | **P1** |
| **UC-10** | Đăng nhập Quản trị viên | Admin | `LoginPage.tsx` | `POST /api/auth/login` | **P0** |
| **UC-11** | Quản lý điểm tham quan | Admin | `SpotsAdmin.tsx` | `GET, PUT /api/admin/places` | **P1** |
| **UC-12** | Tạo phiên bản nháp mới | Admin | `NarrationAdmin.tsx` | `POST /api/admin/places/{id}/revisions` | **P0** |
| **UC-13** | Dịch thuật & TTS tự động | Admin | `NarrationAdmin.tsx` | `POST /api/admin/revisions/{id}/generate` | **P0** |
| **UC-14** | Hiệu chỉnh thủ công bản dịch | Admin | Modal chỉnh sửa (`NarrationAdmin.tsx`) | `PUT /api/admin/revisions/{id}/translations/{lang}` | **P1** |
| **UC-15** | Giám sát Job & Thử lại lỗi | Admin | Bảng Jobs (`NarrationAdmin.tsx`) | `POST /api/admin/jobs/{id}/retry` | **P1** |
| **UC-16** | Xuất bản phiên bản thuyết minh| Admin | `NarrationAdmin.tsx` | `POST /api/admin/revisions/{id}/publish` | **P0** |

---

## 6. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS - NFR)

### 6.1. Bảo mật (Security)
* Toàn bộ hệ thống Web User và Admin phải chạy qua giao thức **HTTPS** có chứng chỉ SSL hợp lệ để trình duyệt di động cho phép truy cập HTML5 Geolocation API.
* Mật khẩu quản trị viên được mã hóa một chiều qua thuật toán PBKDF2/SHA-256 (`PasswordHasher`).
* Authentication Cookie thiết lập thuộc tính `HttpOnly`, `SameSite=Lax`, `Secure` để chống tấn công XSS và đánh cắp phiên.

### 6.2. Hiệu năng & Khả năng chịu tải (Performance & Scalability)
* Cơ chế thăm dò GPS (Polling) được điều phối mỗi $2\text{ giây}$ một lần, giúp ứng dụng bám sát bước chân thực tế mà không gây quá nhiệt hay hao pin trên điện thoại du khách.
* File âm thanh MP3 lưu trữ tĩnh trực tiếp trên ổ cứng máy chủ và phục vụ qua Middleware `UseStaticFiles` với header bộ nhớ đệm HTTP Cache (`Cache-Control: public, max-age=86400`), giảm tải tối đa cho CSDL SQL Server.
* Cơ chế ContentHash đối chiếu văn bản: Nếu nội dung tiếng Việt không thay đổi, hệ thống bỏ qua bước gọi API Azure, tiết kiệm $100\%$ chi phí API phát sinh.

### 6.3. Khả năng tương thích & Tiếp cận (Compatibility & Accessibility)
* Giao diện Responsive tối ưu cho các trình duyệt phổ biến: Safari trên iOS (15+), Google Chrome trên Android (10+), Microsoft Edge, Chrome, Safari trên PC/macOS.
* Phím bấm và các vùng tương tác chạm (Touch target) trên thiết bị di động có kích thước tối thiểu đạt chuẩn $44 \times 44\text{ px}$.

---

## 7. RỦI RO & PHƯƠNG ÁN XỬ LÝ (RISKS & CONTINGENCY PLANS)

| Rủi ro kỹ thuật / vận hành | Mức độ | Hậu quả | Phương án dự phòng / Giải pháp xử lý |
|---|:---:|---|---|
| Du khách từ chối cấp quyền GPS trên điện thoại | Cao | Không hiện chấm xanh, không tự động phát thuyết minh | Hiển thị thanh hướng dẫn bật lại vị trí; cho phép bấm tay thủ công vào marker chuồng thú để đọc/nghe thuyết minh (UC-08). |
| Vị trí GPS nằm ở ranh giới giao nhau giữa 2 khu | Trung bình | Dễ gây nhiễu loạn nếu hệ thống tự ý phát đè âm thanh | Thuật toán Geofence tự động kích hoạt mã `MULTIPLE_ZONES`, hiển thị pop-up để du khách chủ động chọn khu muốn nghe (UC-04). |
| Mất kết nối Internet tạm thời trong vườn thú | Trung bình | Không tải được dữ liệu mới từ API | Ứng dụng tự động kích hoạt bộ dữ liệu tĩnh dự phòng (`staticPlaces.ts`) lưu sẵn trong ứng dụng web để du khách tiếp tục xem bản đồ. |
| Mất kết nối hoặc hết hạn quota Azure AI | Thấp | Không thể dịch tự động hoặc tạo file MP3 cho bài mới | Admin nhận thông báo lỗi chi tiết qua bảng Jobs; hệ thống cho phép nhập tay bản dịch và sử dụng Web Speech API sẵn có của trình duyệt để đọc tạm thời. |