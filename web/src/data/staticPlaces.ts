/* ======================================================
   Dữ liệu tĩnh — dùng khi backend chưa sẵn sàng
   Sau khi backend chạy, MapPage sẽ tự dùng API thay thế.
   ====================================================== */
import type { PlaceDetail } from "../types/place";

export const staticPlaces: PlaceDetail[] = [
  {
    id: 1, slug: "entrance", icon: "🚪", category: "facility",
    lat: 10.78638, lng: 106.70498, radiusMeters: 25,
    mapX: 48, mapY: 88,
    imageUrl: "/images/entrance.jpg",
    name: { vi: "Cổng chính", en: "Main Entrance", zh: "正门", ko: "정문", ja: "正門" },
    desc: { vi: "Cổng vào chính, xây dựng từ thế kỷ 19.", en: "Main entrance, built in the 19th century.", zh: "正门，建于19世纪。", ko: "19세기에 지어진 정문.", ja: "19世紀に建てられた正門。" },
    audioUrl: {},
  },
  {
    id: 2, slug: "elephant", icon: "🐘", category: "animal",
    lat: 10.78820, lng: 106.70420, radiusMeters: 35,
    mapX: 28, mapY: 30,
    imageUrl: "/images/elephant.jpg",
    name: { vi: "Khu voi châu Á", en: "Asian Elephant Enclosure", zh: "亚洲象区", ko: "아시아 코끼리 구역", ja: "アジアゾウエリア" },
    desc: { vi: "Loài biểu tượng của vườn, bảo tồn hơn 60 năm.", en: "The garden's iconic species, conserved for over 60 years.", zh: "园区标志性物种，保护超过60年。", ko: "60년 이상 보존된 정원의 상징적인 종.", ja: "60年以上保護されている庭園のシンボル種。" },
    audioUrl: {},
  },
  {
    id: 3, slug: "giraffe", icon: "🦒", category: "animal",
    lat: 10.78900, lng: 106.70560, radiusMeters: 30,
    mapX: 65, mapY: 20,
    imageUrl: "/images/giraffe.jpg",
    name: { vi: "Khu hươu cao cổ", en: "Giraffe Enclosure", zh: "长颈鹿区", ko: "기린 구역", ja: "キリンエリア" },
    desc: { vi: "Điểm yêu thích nhất, có thể cho ăn trực tiếp.", en: "Most popular stop — feeding sessions available.", zh: "最受欢迎的景点，可以直接喂食。", ko: "가장 인기 있는 곳, 직접 먹이주기 가능.", ja: "最も人気のスポット、直接えさやり可能。" },
    audioUrl: {},
  },
  {
    id: 4, slug: "lion", icon: "🦁", category: "animal",
    lat: 10.78750, lng: 106.70610, radiusMeters: 30,
    mapX: 72, mapY: 45,
    imageUrl: "/images/lion.jpg",
    name: { vi: "Khu sư tử & hổ", en: "Lion & Tiger Enclosure", zh: "狮虎区", ko: "사자 & 호랑이 구역", ja: "ライオン＆トラエリア" },
    desc: { vi: "Sư tử châu Phi và hổ Đông Dương.", en: "African lions and Indochinese tigers.", zh: "非洲狮和印度支那虎。", ko: "아프리카 사자와 인도차이나 호랑이.", ja: "アフリカライオンとインドシナトラ。" },
    audioUrl: {},
  },
  {
    id: 5, slug: "reptile", icon: "🐊", category: "animal",
    lat: 10.78680, lng: 106.70580, radiusMeters: 25,
    mapX: 60, mapY: 60,
    imageUrl: "/images/reptile.jpg",
    name: { vi: "Nhà bò sát", en: "Reptile House", zh: "爬行动物馆", ko: "파충류관", ja: "爬虫類館" },
    desc: { vi: "Cá sấu, rắn, kỳ đà và các loài thằn lằn.", en: "Crocodiles, snakes, monitor lizards and more.", zh: "鳄鱼、蛇、巨蜥等。", ko: "악어, 뱀, 왕도마뱀 등.", ja: "ワニ、ヘビ、オオトカゲなど。" },
    audioUrl: {},
  },
  {
    id: 6, slug: "lotus", icon: "🌸", category: "plant",
    lat: 10.78800, lng: 106.70700, radiusMeters: 30,
    mapX: 80, mapY: 35,
    imageUrl: "/images/lotus.jpg",
    name: { vi: "Hồ Sen", en: "Lotus Pond", zh: "荷花池", ko: "연꽃 연못", ja: "蓮の池" },
    desc: { vi: "Hàng trăm loài thực vật thủy sinh.", en: "Hundreds of aquatic plant species.", zh: "数百种水生植物。", ko: "수백 종의 수생 식물.", ja: "数百種の水生植物。" },
    audioUrl: {},
  },
  {
    id: 7, slug: "ancient-tree", icon: "🌳", category: "plant",
    lat: 10.78720, lng: 106.70480, radiusMeters: 30,
    mapX: 35, mapY: 55,
    imageUrl: "/images/ancient-tree.jpg",
    name: { vi: "Vườn cây cổ thụ", en: "Ancient Tree Garden", zh: "古树园", ko: "고목 정원", ja: "古木園" },
    desc: { vi: "Cây xanh trăm năm tuổi, một số hơn 200 năm.", en: "Century-old trees, some over 200 years.", zh: "百年古树，有些超过200年。", ko: "100년이 넘은 나무, 일부는 200년 이상.", ja: "100年以上の木々、一部は200年以上。" },
    audioUrl: {},
  },
  {
    id: 8, slug: "museum", icon: "🏛️", category: "facility",
    lat: 10.78850, lng: 106.70480, radiusMeters: 30,
    mapX: 30, mapY: 18,
    imageUrl: "/images/museum.jpg",
    name: { vi: "Bảo tàng thiên nhiên", en: "Natural History Museum", zh: "自然历史博物馆", ko: "자연사 박물관", ja: "自然史博物館" },
    desc: { vi: "Lịch sử 160 năm và mẫu vật quý hiếm.", en: "160 years of history and rare specimens.", zh: "160年历史和珍贵标本。", ko: "160년의 역사와 희귀 표본.", ja: "160年の歴史と希少標本。" },
    audioUrl: {},
  },
];
