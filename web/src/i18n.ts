/* ======================================================
   i18n — Nội dung đa ngôn ngữ
   Thêm ngôn ngữ mới: bổ sung key vào type Lang và
   thêm object tương ứng vào translations.
   ====================================================== */

export type Lang = "vi" | "en";

export interface Translations {
  /* Header */
  brandTagline: string;
  langLabel: string;

  /* Nav */
  navExplore: string;
  navHome: string;
  navMap: string;
  navNote: string;

  /* Footer */
  footerTagline: string;

  /* Hero */
  heroEyebrow: string;
  heroTitle: string;
  heroSubtitle: string;
  heroDesc: string;
  heroBtnMap: string;
  heroBtnMore: string;
  heroTag1: string;
  heroTag2: string;
  heroTag3: string;
  heroTag4: string;
  heroArtLabel: string;

  /* Stats */
  stat1Label: string;
  stat2Label: string;
  stat3Label: string;
  stat4Label: string;

  /* Discover */
  discoverEyebrow: string;
  discoverTitle: string;
  discoverDesc: string;
  cat1Title: string;
  cat1Desc: string;
  cat2Title: string;
  cat2Desc: string;
  cat3Title: string;
  cat3Desc: string;
  openMap: string;

  /* Highlights */
  highlightsEyebrow: string;
  highlightsTitle: string;
  highlightsDesc: string;
  tagAnimal: string;
  tagPlant: string;
  hl1Title: string;
  hl1Desc: string;
  hl2Title: string;
  hl2Desc: string;
  hl3Title: string;
  hl3Desc: string;
  hl4Title: string;
  hl4Desc: string;
  viewOnMap: string;

  /* Guide */
  guideEyebrow: string;
  guideTitle: string;
  guideDesc: string;
  step1Title: string;
  step1Desc: string;
  step2Title: string;
  step2Desc: string;
  step3Title: string;
  step3Desc: string;

  /* Gallery */
  galleryEyebrow: string;
  galleryTitle: string;
  galleryDesc: string;
  gallery1: string;
  gallery2: string;
  gallery3: string;
  gallery4: string;
  gallery5: string;
  gallery6: string;

  /* CTA */
  ctaEyebrow: string;
  ctaTitle: string;
  ctaDesc: string;
  ctaBtn: string;

  /* Map page */
  mapEyebrow: string;
  mapTitle: string;
  mapDesc: string;
  mapDrag: string;
  mapReturnBtn: string;
  mapHelp: string;
}

const vi: Translations = {
  brandTagline: "Khám phá thiên nhiên",
  langLabel: "🇻🇳 Tiếng Việt",
  navExplore: "CÙNG KHÁM PHÁ",
  navHome: "Trang chủ",
  navMap: "Bản đồ tham quan",
  navNote: "Một hành trình nhỏ,\nnhiều điều thú vị.",
  footerTagline: "Khám phá · Lắng nghe · Yêu thiên nhiên",
  heroEyebrow: "CHÀO MỪNG BẠN ĐẾN VỚI",
  heroTitle: "Thảo Cầm Viên",
  heroSubtitle: "Chạm gần thiên nhiên.",
  heroDesc: "Dạo bước giữa không gian xanh hơn 12 ha ngay giữa lòng Thành phố Hồ Chí Minh — khám phá các loài động thực vật và lắng nghe câu chuyện tại mỗi điểm dừng chân.",
  heroBtnMap: "Khám phá bản đồ",
  heroBtnMore: "Tìm hiểu thêm",
  heroTag1: "🌱 Không gian xanh",
  heroTag2: "🐾 Khám phá muôn loài",
  heroTag3: "🎧 Thuyết minh đa ngôn ngữ",
  heroTag4: "📍 TP. Hồ Chí Minh",
  heroArtLabel: "Một ngày giữa thiên nhiên",
  stat1Label: "Diện tích khuôn viên",
  stat2Label: "Năm thành lập",
  stat3Label: "Loài động vật",
  stat4Label: "Loài thực vật",
  discoverEyebrow: "MỖI BƯỚC CHÂN, MỘT KHÁM PHÁ",
  discoverTitle: "Bạn muốn khám phá điều gì?",
  discoverDesc: "Chọn chủ đề yêu thích, rồi mở bản đồ để bắt đầu hành trình.",
  cat1Title: "Thế giới động vật",
  cat1Desc: "Tìm hiểu đặc điểm, tập tính và những câu chuyện thú vị về các loài đang sinh sống tại vườn.",
  cat2Title: "Không gian thực vật",
  cat2Desc: "Khám phá cây xanh trăm tuổi, các loài hoa quý và cảm nhận sự đa dạng của thế giới thực vật.",
  cat3Title: "Nghe thuyết minh",
  cat3Desc: "Tìm hiểu các điểm tham quan qua nội dung thuyết minh đa ngôn ngữ tích hợp sẵn trên bản đồ.",
  openMap: "Mở bản đồ",
  highlightsEyebrow: "KHÔNG NÊN BỎ LỠ",
  highlightsTitle: "Điểm nổi bật của vườn",
  highlightsDesc: "Một vài điểm dừng chân được khách tham quan yêu thích nhất.",
  tagAnimal: "Động vật",
  tagPlant: "Thực vật",
  hl1Title: "Voi châu Á",
  hl1Desc: "Loài biểu tượng của vườn, được chăm sóc và bảo tồn hơn 60 năm.",
  hl2Title: "Hươu cao cổ",
  hl2Desc: "Điểm dừng chân được yêu thích nhất của các bạn nhỏ khi đến vườn.",
  hl3Title: "Hồ Sen",
  hl3Desc: "Khu vực thực vật thủy sinh với hàng trăm loài cây nước độc đáo.",
  hl4Title: "Cây cổ thụ 200 tuổi",
  hl4Desc: "Những cây xanh trăm năm tuổi vẫn xanh tươi giữa lòng thành phố.",
  viewOnMap: "Xem trên bản đồ",
  guideEyebrow: "HƯỚNG DẪN THAM QUAN",
  guideTitle: "Hành trình bắt đầu từ đây",
  guideDesc: "Ba bước đơn giản để khám phá Thảo Cầm Viên cùng bản đồ số.",
  step1Title: "Mở bản đồ",
  step1Desc: "Xem toàn bộ khu vực và các điểm tham quan trong vườn.",
  step2Title: "Chọn điểm bạn thích",
  step2Desc: "Nhấn vào điểm trên bản đồ để xem thông tin chi tiết về loài hoặc khu vực.",
  step3Title: "Lắng nghe & khám phá",
  step3Desc: "Nghe thuyết minh đa ngôn ngữ và tự mình trải nghiệm từng điểm dừng chân.",
  galleryEyebrow: "HÌNH ẢNH",
  galleryTitle: "Một thoáng Thảo Cầm Viên",
  galleryDesc: "Không gian xanh mát giữa lòng thành phố — nơi thiên nhiên và lịch sử cùng hiện diện.",
  gallery1: "Cổng chính Thảo Cầm Viên",
  gallery2: "Con đường xanh mát",
  gallery3: "Khu động vật",
  gallery4: "Khách tham quan",
  gallery5: "Vườn thực vật",
  gallery6: "Hoàng hôn trong vườn",
  ctaEyebrow: "SẴN SÀNG CHƯA?",
  ctaTitle: "Bắt đầu hành trình khám phá",
  ctaDesc: "Mở bản đồ, chọn điểm dừng và nghe câu chuyện từ thiên nhiên — ngay trên điện thoại của bạn.",
  ctaBtn: "Mở bản đồ ngay",
  mapEyebrow: "HÀNH TRÌNH CỦA BẠN",
  mapTitle: "Bản đồ tham quan",
  mapDesc: "Khám phá không gian xanh tại Thảo Cầm Viên.",
  mapDrag: "Kéo bản đồ để xem các khu vực",
  mapReturnBtn: "📍 Về Thảo Cầm Viên",
  mapHelp: "Dùng nút + / − để phóng to hoặc thu nhỏ. Trên điện thoại, dùng hai ngón tay để thay đổi độ phóng đại.",
};

const en: Translations = {
  brandTagline: "Explore nature",
  langLabel: "🇬🇧 English",
  navExplore: "EXPLORE",
  navHome: "Home",
  navMap: "Visit Map",
  navNote: "A small journey,\nmany great discoveries.",
  footerTagline: "Explore · Listen · Love Nature",
  heroEyebrow: "WELCOME TO",
  heroTitle: "Saigon Zoo & Botanical Gardens",
  heroSubtitle: "Get close to nature.",
  heroDesc: "Stroll through 12 hectares of greenery in the heart of Ho Chi Minh City — discover wildlife and plants while listening to stories at every stop.",
  heroBtnMap: "Explore the map",
  heroBtnMore: "Learn more",
  heroTag1: "🌱 Green space",
  heroTag2: "🐾 Wildlife",
  heroTag3: "🎧 Multilingual audio",
  heroTag4: "📍 Ho Chi Minh City",
  heroArtLabel: "A day with nature",
  stat1Label: "Garden area",
  stat2Label: "Year founded",
  stat3Label: "Animal species",
  stat4Label: "Plant species",
  discoverEyebrow: "EVERY STEP, A DISCOVERY",
  discoverTitle: "What would you like to explore?",
  discoverDesc: "Pick a theme and open the map to start your journey.",
  cat1Title: "Wildlife",
  cat1Desc: "Learn about the characteristics, behaviors and fascinating stories of the animals living in the garden.",
  cat2Title: "Botanical Garden",
  cat2Desc: "Discover century-old trees, rare flowers and the rich diversity of the plant world.",
  cat3Title: "Audio Guide",
  cat3Desc: "Explore attractions through multilingual audio commentary built into the map.",
  openMap: "Open map",
  highlightsEyebrow: "NOT TO BE MISSED",
  highlightsTitle: "Garden highlights",
  highlightsDesc: "A few must-see stops that visitors love the most.",
  tagAnimal: "Animal",
  tagPlant: "Plant",
  hl1Title: "Asian Elephant",
  hl1Desc: "The garden's iconic species, cared for and conserved for over 60 years.",
  hl2Title: "Giraffe",
  hl2Desc: "The most popular stop for young visitors to the garden.",
  hl3Title: "Lotus Pond",
  hl3Desc: "An aquatic plant area featuring hundreds of unique water plant species.",
  hl4Title: "200-year-old Tree",
  hl4Desc: "Ancient trees that have stood for centuries, still thriving in the city.",
  viewOnMap: "View on map",
  guideEyebrow: "VISITOR GUIDE",
  guideTitle: "Your journey starts here",
  guideDesc: "Three simple steps to explore the garden with the digital map.",
  step1Title: "Open the map",
  step1Desc: "View all areas and attractions within the garden.",
  step2Title: "Choose a spot",
  step2Desc: "Tap a point on the map to see detailed information about a species or area.",
  step3Title: "Listen & explore",
  step3Desc: "Listen to multilingual audio commentary and experience each stop yourself.",
  galleryEyebrow: "GALLERY",
  galleryTitle: "A glimpse of the garden",
  galleryDesc: "A cool green space in the heart of the city — where nature and history coexist.",
  gallery1: "Main entrance",
  gallery2: "Shaded walkway",
  gallery3: "Animal enclosures",
  gallery4: "Visitors",
  gallery5: "Botanical garden",
  gallery6: "Sunset through the trees",
  ctaEyebrow: "READY?",
  ctaTitle: "Start your journey",
  ctaDesc: "Open the map, choose a stop and hear stories from nature — right on your phone.",
  ctaBtn: "Open map now",
  mapEyebrow: "YOUR JOURNEY",
  mapTitle: "Visit Map",
  mapDesc: "Explore the green spaces of Saigon Zoo & Botanical Gardens.",
  mapDrag: "Drag the map to explore areas",
  mapReturnBtn: "📍 Back to Zoo Center",
  mapHelp: "Use the + / − buttons to zoom in or out. On mobile, use two fingers to change zoom level.",
};

export const translations: Record<Lang, Translations> = { vi, en };
