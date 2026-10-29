/* ======================================================
   Dữ liệu điểm tham quan trên bản đồ
   Tọa độ dựa trên vị trí thực tế của Thảo Cầm Viên
   (2 Nguyễn Bỉnh Khiêm, Q.1, TP.HCM)
   ====================================================== */

export interface Spot {
  id: string;
  coords: [number, number]; // [lat, lng]
  icon: string;             // emoji dùng làm icon marker
  category: "animal" | "plant" | "facility";
  name:    { vi: string; en: string };
  desc:    { vi: string; en: string };
  details: { vi: string; en: string }; // mô tả dài cho panel
  /*
   * ĐẶT ẢNH spot: /public/images/spots/<id>.jpg
   * Ví dụ: /public/images/spots/elephant.jpg
   * Kích thước gợi ý: 800×500 px
   */
  img: string;
}

export const spots: Spot[] = [
  {
    id: "entrance",
    coords: [10.78638, 106.70498],
    icon: "🚪",
    category: "facility",
    img: "/images/entrance.jpg",
    name: { vi: "Cổng chính", en: "Main Entrance" },
    desc: {
      vi: "Cổng vào chính của Thảo Cầm Viên, xây dựng từ thế kỷ 19.",
      en: "The main entrance of the zoo, built in the 19th century.",
    },
    details: {
      vi: "Cổng chính Thảo Cầm Viên được xây dựng từ thế kỷ 19, mang kiến trúc Pháp cổ điển. Qua cổng là con đường dài dẫn vào vườn, hai bên là những hàng cây cổ thụ tạo bóng mát. Đây là điểm xuất phát lý tưởng để bắt đầu hành trình khám phá toàn bộ khu vườn.",
      en: "The main gate of the zoo was built in the 19th century with classic French architecture. Beyond the gate lies a long path flanked by ancient shading trees. It's the ideal starting point for exploring the entire garden.",
    },
  },
  {
    id: "elephant",
    coords: [10.78820, 106.70420],
    icon: "🐘",
    category: "animal",
    img: "/images/elephant.jpg",
    name: { vi: "Khu voi châu Á", en: "Asian Elephant Enclosure" },
    desc: {
      vi: "Nơi sinh sống của đàn voi châu Á — loài biểu tượng của vườn.",
      en: "Home to Asian elephants — the garden's iconic species.",
    },
    details: {
      vi: "Voi châu Á (Elephas maximus) là loài biểu tượng của Thảo Cầm Viên Sài Gòn. Đàn voi tại đây đã được chăm sóc và bảo tồn hơn 60 năm. Mỗi con voi có tên riêng và câu chuyện đặc biệt. Voi châu Á nhỏ hơn voi châu Phi, đặc trưng bởi đôi tai nhỏ và trán phẳng. Chúng là loài động vật thông minh, có trí nhớ tốt và sống theo bầy đàn.",
      en: "The Asian elephant (Elephas maximus) is the iconic species of Saigon Zoo. The herd here has been cared for and conserved for over 60 years. Each elephant has its own name and story. Asian elephants are smaller than African elephants, distinguished by their smaller ears and flat forehead. They are highly intelligent animals with good memory who live in herds.",
    },
  },
  {
    id: "giraffe",
    coords: [10.78900, 106.70560],
    icon: "🦒",
    category: "animal",
    img: "/images/giraffe.jpg",
    name: { vi: "Khu hươu cao cổ", en: "Giraffe Enclosure" },
    desc: {
      vi: "Điểm dừng chân được yêu thích nhất, đặc biệt với các bạn nhỏ.",
      en: "The most popular stop, especially for children.",
    },
    details: {
      vi: "Hươu cao cổ (Giraffa camelopardalis) là loài động vật cao nhất thế giới, có thể đạt chiều cao 5–6 mét. Chiếc cổ dài giúp chúng với tới những chiếc lá trên ngọn cây cao. Mỗi con hươu cao cổ có hoa văn đốm độc đáo, không con nào giống con nào — giống như dấu vân tay ở người. Vào một số khung giờ nhất định, du khách có thể trực tiếp cho hươu ăn.",
      en: "The giraffe (Giraffa camelopardalis) is the tallest animal in the world, reaching heights of 5–6 meters. Their long necks help them reach leaves at the tops of tall trees. Each giraffe has a unique spot pattern — no two are alike, similar to human fingerprints. At certain times, visitors can directly feed the giraffes.",
    },
  },
  {
    id: "lion",
    coords: [10.78750, 106.70610],
    icon: "🦁",
    category: "animal",
    img: "/images/lion.jpg",
    name: { vi: "Khu sư tử & hổ", en: "Lion & Tiger Enclosure" },
    desc: {
      vi: "Khu vực nuôi dưỡng các loài thú lớn như sư tử châu Phi và hổ Đông Dương.",
      en: "Houses large predators including African lions and Indochinese tigers.",
    },
    details: {
      vi: "Khu này là nơi sinh sống của hai loài thú lớn hùng mạnh nhất vườn. Sư tử châu Phi (Panthera leo) được mệnh danh là 'vua của rừng xanh' với bờm oai vệ. Hổ Đông Dương (Panthera tigris corbetti) — phân loài đặc trưng của Đông Nam Á — đang nằm trong danh sách nguy cấp cần được bảo tồn khẩn cấp.",
      en: "This enclosure houses the two most powerful large predators in the garden. The African lion (Panthera leo) is known as the 'king of the jungle' with its majestic mane. The Indochinese tiger (Panthera tigris corbetti) — a subspecies native to Southeast Asia — is listed as endangered and in urgent need of conservation.",
    },
  },
  {
    id: "reptile",
    coords: [10.78680, 106.70580],
    icon: "🐊",
    category: "animal",
    img: "/images/reptile.jpg",
    name: { vi: "Nhà bò sát", en: "Reptile House" },
    desc: {
      vi: "Triển lãm các loài bò sát nhiệt đới: cá sấu, rắn, kỳ đà và nhiều loài thằn lằn đặc sắc.",
      en: "Showcases tropical reptiles: crocodiles, snakes, monitor lizards and more.",
    },
    details: {
      vi: "Nhà bò sát là nơi trưng bày các loài bò sát nhiệt đới độc đáo của vùng Đông Nam Á và châu Á. Nổi bật là cá sấu nước ngọt (Crocodylus siamensis) — loài đang cực kỳ nguy cấp tại Việt Nam, cùng nhiều loài rắn, kỳ đà, và thằn lằn màu sắc rực rỡ. Đây là khu vực lý tưởng để tìm hiểu về sự đa dạng sinh học của nhóm động vật có vảy.",
      en: "The Reptile House displays unique tropical reptiles from Southeast and East Asia. Notable residents include the Siamese crocodile (Crocodylus siamensis) — critically endangered in Vietnam — along with various snakes, monitor lizards, and colorful lizards. It's an ideal area to learn about the biodiversity of scaled animals.",
    },
  },
  {
    id: "lotus",
    coords: [10.78800, 106.70700],
    icon: "🌸",
    category: "plant",
    img: "/images/lotus.jpg",
    name: { vi: "Hồ Sen", en: "Lotus Pond" },
    desc: {
      vi: "Hồ nước yên tĩnh với hàng trăm loài thực vật thủy sinh.",
      en: "A tranquil pond with hundreds of aquatic plant species.",
    },
    details: {
      vi: "Hồ Sen là một trong những điểm đẹp nhất của Thảo Cầm Viên. Mặt hồ yên tĩnh phản chiếu bóng cây xanh xung quanh, tạo không gian thư giãn lý tưởng. Hoa sen (Nelumbo nucifera) — biểu tượng quốc hoa của Việt Nam — nở rộ vào mùa hè với màu hồng và trắng rực rỡ. Xung quanh hồ còn có nhiều loài thực vật thủy sinh như súng, bèo sen và các loài cây thủy trúc.",
      en: "The Lotus Pond is one of the most beautiful spots in the garden. The still water reflects the surrounding green trees, creating an ideal relaxing space. The lotus flower (Nelumbo nucifera) — Vietnam's national flower — blooms in summer in vivid pink and white. Around the pond grow many aquatic plant species including water lilies, lotus, and papyrus.",
    },
  },
  {
    id: "ancient-tree",
    coords: [10.78720, 106.70480],
    icon: "🌳",
    category: "plant",
    img: "/images/ancient-tree.jpg",
    name: { vi: "Vườn cây cổ thụ", en: "Ancient Tree Garden" },
    desc: {
      vi: "Khu vực tập trung nhiều cây cổ thụ trăm năm tuổi.",
      en: "An area with many century-old trees.",
    },
    details: {
      vi: "Đây là khu vực đặc biệt nhất về mặt thực vật học trong toàn bộ Thảo Cầm Viên. Nhiều cây tại đây đã tồn tại từ khi vườn mới thành lập năm 1864, tức là đã hơn 160 tuổi. Một số cây cổ thụ có chu vi thân lên tới 5–6 mét, tán lá rộng hàng trăm mét vuông. Bên dưới tán cây là thảm thực vật phong phú với nhiều loài dương xỉ, rêu và phong lan bám đá.",
      en: "This is the most botanically significant area in the entire garden. Many trees here have existed since the garden was founded in 1864, making them over 160 years old. Some ancient trees have trunk circumferences of up to 5–6 meters and canopies spanning hundreds of square meters. Beneath the canopy grows a rich understory with many ferns, mosses, and rock orchids.",
    },
  },
  {
    id: "museum",
    coords: [10.78850, 106.70480],
    icon: "🏛️",
    category: "facility",
    img: "/images/museum.jpg",
    name: { vi: "Bảo tàng thiên nhiên", en: "Natural History Museum" },
    desc: {
      vi: "Bảo tàng lưu giữ mẫu vật động thực vật quý hiếm và trưng bày lịch sử 160 năm của vườn.",
      en: "A museum preserving rare specimens and displaying 160 years of the garden's history.",
    },
    details: {
      vi: "Bảo tàng thiên nhiên là trái tim của Thảo Cầm Viên về mặt học thuật và lịch sử. Được thành lập cùng với vườn năm 1864, bảo tàng lưu giữ hàng nghìn mẫu vật động thực vật quý hiếm, trong đó có nhiều loài hiện đã tuyệt chủng hoặc cực kỳ nguy cấp. Các phòng trưng bày kể câu chuyện về lịch sử tự nhiên Nam Bộ và vai trò của vườn trong công tác bảo tồn đa dạng sinh học.",
      en: "The Natural History Museum is the academic and historical heart of the zoo. Founded with the garden in 1864, it houses thousands of rare animal and plant specimens, including many species now extinct or critically endangered. The exhibition halls tell the story of southern Vietnam's natural history and the garden's role in biodiversity conservation.",
    },
  },
];
