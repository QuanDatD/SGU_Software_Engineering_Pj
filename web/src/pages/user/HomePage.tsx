import { useLang } from "../../contexts/LanguageContext";

/* ── Nội dung tĩnh trang chủ ─────────────────────────── */
const T = {
  heroEyebrow: { vi: "CHÀO MỪNG BẠN ĐẾN VỚI", en: "WELCOME TO", zh: "欢迎来到", ko: "환영합니다", ja: "ようこそ" },
  heroTitle:   { vi: "Thảo Cầm Viên", en: "Saigon Zoo & Botanical Gardens", zh: "西贡动植物园", ko: "사이공 동물원", ja: "サイゴン動植物園" },
  heroSub:     { vi: "Chạm gần thiên nhiên.", en: "Get close to nature.", zh: "亲近自然。", ko: "자연에 가까이.", ja: "自然に近づこう。" },
  heroDesc:    { vi: "Dạo bước giữa không gian xanh hơn 12 ha ngay giữa lòng TP.HCM — khám phá động thực vật và lắng nghe câu chuyện tại mỗi điểm dừng chân.", en: "Stroll through 12 hectares of greenery in the heart of Ho Chi Minh City — discover wildlife and plants while listening to stories at every stop.", zh: "在胡志明市中心12公顷的绿色空间中漫步，探索动植物，聆听每个停靠点的故事。", ko: "호치민시 중심부의 12헥타르 녹지를 거닐며 야생 동식물을 발견하고 매 정류장마다 이야기를 들어보세요.", ja: "ホーチミン市の中心部にある12ヘクタールの緑地を散策し、野生動物や植物を発見しながら、各スポットのストーリーを聞きましょう。" },
  btnMap:      { vi: "Khám phá bản đồ", en: "Explore map", zh: "探索地图", ko: "지도 탐험", ja: "マップを探索" },
  btnMore:     { vi: "Tìm hiểu thêm", en: "Learn more", zh: "了解更多", ko: "더 알아보기", ja: "もっと詳しく" },
  tag1: { vi: "🌱 Không gian xanh", en: "🌱 Green space", zh: "🌱 绿色空间", ko: "🌱 녹색 공간", ja: "🌱 緑の空間" },
  tag2: { vi: "🐾 Khám phá muôn loài", en: "🐾 Wildlife", zh: "🐾 野生动物", ko: "🐾 야생동물", ja: "🐾 野生動物" },
  tag3: { vi: "🎧 Thuyết minh đa ngôn ngữ", en: "🎧 Multilingual audio", zh: "🎧 多语言讲解", ko: "🎧 다국어 오디오", ja: "🎧 多言語ガイド" },
  tag4: { vi: "📍 TP. Hồ Chí Minh", en: "📍 Ho Chi Minh City", zh: "📍 胡志明市", ko: "📍 호치민시", ja: "📍 ホーチミン市" },
  artLabel: { vi: "Một ngày giữa thiên nhiên", en: "A day with nature", zh: "与自然共度一天", ko: "자연과 함께하는 하루", ja: "自然と過ごす一日" },

  stat1v: { vi: "12 ha", en: "12 ha", zh: "12 公顷", ko: "12 ha", ja: "12 ha" },
  stat1l: { vi: "Diện tích khuôn viên", en: "Garden area", zh: "园区面积", ko: "정원 면적", ja: "庭園面積" },
  stat2v: { vi: "1864", en: "1864", zh: "1864", ko: "1864", ja: "1864" },
  stat2l: { vi: "Năm thành lập", en: "Year founded", zh: "成立年份", ko: "설립 연도", ja: "設立年" },
  stat3v: { vi: "150+", en: "150+", zh: "150+", ko: "150+", ja: "150+" },
  stat3l: { vi: "Loài động vật", en: "Animal species", zh: "动物种类", ko: "동물 종", ja: "動物種" },
  stat4v: { vi: "1000+", en: "1000+", zh: "1000+", ko: "1000+", ja: "1000+" },
  stat4l: { vi: "Loài thực vật", en: "Plant species", zh: "植物种类", ko: "식물 종", ja: "植物種" },

  discoverEyebrow: { vi: "MỖI BƯỚC CHÂN, MỘT KHÁM PHÁ", en: "EVERY STEP, A DISCOVERY", zh: "每一步，都是发现", ko: "매 걸음, 새로운 발견", ja: "一歩ごとに発見" },
  discoverTitle:   { vi: "Bạn muốn khám phá điều gì?", en: "What would you like to explore?", zh: "您想探索什么？", ko: "무엇을 탐험하고 싶으신가요?", ja: "何を探検しますか？" },
  discoverDesc:    { vi: "Chọn chủ đề yêu thích, rồi mở bản đồ để bắt đầu.", en: "Pick a theme and open the map to start.", zh: "选择您喜欢的主题，打开地图开始吧。", ko: "좋아하는 주제를 선택하고 지도를 열어 시작하세요.", ja: "テーマを選んで地図を開いて始めましょう。" },
  cat1t: { vi: "Thế giới động vật", en: "Wildlife", zh: "动物世界", ko: "동물 세계", ja: "動物の世界" },
  cat1d: { vi: "Tìm hiểu đặc điểm và câu chuyện về các loài đang sinh sống tại vườn.", en: "Learn about the animals living in the garden.", zh: "了解生活在园区的动物特征和故事。", ko: "정원에 살고 있는 동물들에 대해 알아보세요.", ja: "庭園に生息する動物について学びましょう。" },
  cat2t: { vi: "Không gian thực vật", en: "Botanical Garden", zh: "植物空间", ko: "식물 공간", ja: "植物の空間" },
  cat2d: { vi: "Khám phá cây xanh trăm tuổi và thế giới thực vật đa dạng.", en: "Discover century-old trees and diverse plant life.", zh: "探索百年古树和多样的植物世界。", ko: "수백 년 된 나무와 다양한 식물 세계를 탐험하세요.", ja: "百年の木々と多様な植物の世界を探検しましょう。" },
  cat3t: { vi: "Nghe thuyết minh", en: "Audio Guide", zh: "收听讲解", ko: "오디오 가이드", ja: "音声ガイド" },
  cat3d: { vi: "Thuyết minh đa ngôn ngữ tích hợp sẵn — tự phát khi bạn đến gần điểm tham quan.", en: "Multilingual audio built-in — plays automatically when you're near a spot.", zh: "内置多语言讲解——走近景点时自动播放。", ko: "다국어 오디오 내장 — 관광지에 가까워지면 자동 재생됩니다.", ja: "多言語音声内蔵 — スポットに近づくと自動再生されます。" },
  openMap: { vi: "Mở bản đồ", en: "Open map", zh: "打开地图", ko: "지도 열기", ja: "地図を開く" },

  hlEyebrow: { vi: "KHÔNG NÊN BỎ LỠ", en: "NOT TO BE MISSED", zh: "不容错过", ko: "놓치면 안 되는", ja: "見逃せない" },
  hlTitle:   { vi: "Điểm nổi bật của vườn", en: "Garden highlights", zh: "园区亮点", ko: "정원 하이라이트", ja: "庭園のハイライト" },
  hlDesc:    { vi: "Những điểm dừng chân được yêu thích nhất.", en: "The most popular stops.", zh: "最受欢迎的参观点。", ko: "가장 인기 있는 정류장.", ja: "最も人気のあるスポット。" },
  tagAnimal: { vi: "Động vật", en: "Animal", zh: "动物", ko: "동물", ja: "動物" },
  tagPlant:  { vi: "Thực vật", en: "Plant", zh: "植物", ko: "식물", ja: "植物" },
  hl1t: { vi: "Voi châu Á", en: "Asian Elephant", zh: "亚洲象", ko: "아시아 코끼리", ja: "アジアゾウ" },
  hl1d: { vi: "Loài biểu tượng của vườn, được bảo tồn hơn 60 năm.", en: "The garden's iconic species, conserved for over 60 years.", zh: "园区标志性物种，保护超过60年。", ko: "60년 이상 보존된 정원의 상징적인 종.", ja: "60年以上保護されている庭園のシンボル種。" },
  hl2t: { vi: "Hươu cao cổ", en: "Giraffe", zh: "长颈鹿", ko: "기린", ja: "キリン" },
  hl2d: { vi: "Điểm yêu thích của các bạn nhỏ, có thể cho ăn trực tiếp.", en: "Kids' favourite — feeding sessions available.", zh: "孩子们的最爱，可直接喂食。", ko: "아이들이 가장 좋아하는 곳, 직접 먹이주기 가능.", ja: "子供たちのお気に入り、直接えさやり可能。" },
  hl3t: { vi: "Hồ Sen", en: "Lotus Pond", zh: "荷花池", ko: "연꽃 연못", ja: "蓮の池" },
  hl3d: { vi: "Không gian yên tĩnh với hàng trăm loài thực vật thủy sinh.", en: "A tranquil space with hundreds of aquatic plant species.", zh: "宁静的空间，拥有数百种水生植物。", ko: "수백 종의 수생 식물이 있는 고요한 공간.", ja: "数百種の水生植物がある静寂な空間。" },
  hl4t: { vi: "Cây cổ thụ 200 tuổi", en: "200-year-old Tree", zh: "200年古树", ko: "200년 된 나무", ja: "200年の古木" },
  hl4d: { vi: "Những cây xanh trăm năm vẫn xanh tươi giữa lòng thành phố.", en: "Ancient trees still thriving in the heart of the city.", zh: "在城市中心依然郁郁葱葱的百年古树。", ko: "도시 한가운데서 여전히 번성하는 고목.", ja: "都市の中心でまだ元気な古木。" },
  viewOnMap: { vi: "Xem trên bản đồ", en: "View on map", zh: "在地图上查看", ko: "지도에서 보기", ja: "地図で見る" },

  guideEyebrow: { vi: "HƯỚNG DẪN THAM QUAN", en: "VISITOR GUIDE", zh: "参观指南", ko: "방문 가이드", ja: "ビジターガイド" },
  guideTitle:   { vi: "Hành trình bắt đầu từ đây", en: "Your journey starts here", zh: "旅程从这里开始", ko: "여정은 여기서 시작됩니다", ja: "旅はここから始まります" },
  guideDesc:    { vi: "Ba bước đơn giản để khám phá cùng bản đồ số.", en: "Three simple steps to explore with the digital map.", zh: "三个简单步骤，用数字地图探索。", ko: "디지털 지도로 탐험하는 세 가지 간단한 단계.", ja: "デジタルマップで探検する3つの簡単なステップ。" },
  s1t: { vi: "Mở bản đồ", en: "Open the map", zh: "打开地图", ko: "지도 열기", ja: "地図を開く" },
  s1d: { vi: "Xem toàn bộ khu vực và điểm tham quan.", en: "View all areas and attractions.", zh: "查看所有区域和景点。", ko: "모든 구역과 명소를 보세요.", ja: "すべてのエリアとアトラクションを表示します。" },
  s2t: { vi: "Chọn điểm bạn thích", en: "Choose a spot", zh: "选择您喜欢的地点", ko: "원하는 곳을 선택하세요", ja: "スポットを選んでください" },
  s2d: { vi: "Nhấn vào điểm trên bản đồ để xem thông tin chi tiết.", en: "Tap a point on the map to see details.", zh: "点击地图上的点查看详细信息。", ko: "지도의 포인트를 탭하여 세부 정보를 보세요.", ja: "地図のポイントをタップして詳細を表示します。" },
  s3t: { vi: "Lắng nghe & khám phá", en: "Listen & explore", zh: "聆听与探索", ko: "듣고 탐험하기", ja: "聞いて探検する" },
  s3d: { vi: "Audio tự phát khi bạn đến gần — hoặc nhấn thủ công từ bản đồ.", en: "Audio plays automatically when near — or tap manually from the map.", zh: "靠近时音频自动播放，或从地图手动点击。", ko: "가까워지면 자동으로 재생되거나 지도에서 수동으로 탭하세요.", ja: "近づくと自動再生、または地図から手動でタップ。" },

  galEyebrow: { vi: "HÌNH ẢNH", en: "GALLERY", zh: "图片", ko: "갤러리", ja: "ギャラリー" },
  galTitle:   { vi: "Một thoáng Thảo Cầm Viên", en: "A glimpse of the garden", zh: "一瞥植物园", ko: "정원 한눈에", ja: "庭園の一コマ" },
  galDesc:    { vi: "Không gian xanh mát giữa lòng thành phố.", en: "A cool green space in the heart of the city.", zh: "城市中心清凉的绿色空间。", ko: "도시 중심부의 시원한 녹색 공간.", ja: "都市の中心にある涼しい緑の空間。" },
  gal: [
    { vi: "Cổng chính", en: "Main entrance", zh: "正门", ko: "정문", ja: "正門" },
    { vi: "Con đường xanh mát", en: "Shaded walkway", zh: "绿荫小道", ko: "그늘진 산책로", ja: "木陰の小道" },
    { vi: "Khu động vật", en: "Animal enclosures", zh: "动物区", ko: "동물 구역", ja: "動物エリア" },
    { vi: "Khách tham quan", en: "Visitors", zh: "游客", ko: "방문객", ja: "訪問者" },
    { vi: "Vườn thực vật", en: "Botanical garden", zh: "植物园", ko: "식물원", ja: "植物園" },
    { vi: "Hoàng hôn trong vườn", en: "Sunset through the trees", zh: "园中日落", ko: "나무 사이 일몰", ja: "木々越しの夕焼け" },
  ] as Record<string, string>[],

  ctaEyebrow: { vi: "SẴN SÀNG CHƯA?", en: "READY?", zh: "准备好了吗？", ko: "준비됐나요?", ja: "準備はいいですか？" },
  ctaTitle:   { vi: "Bắt đầu hành trình", en: "Start your journey", zh: "开始您的旅程", ko: "여정을 시작하세요", ja: "旅を始めましょう" },
  ctaDesc:    { vi: "Mở bản đồ, chọn điểm dừng và nghe câu chuyện từ thiên nhiên.", en: "Open the map, choose a stop and hear stories from nature.", zh: "打开地图，选择停靠点，聆听来自自然的故事。", ko: "지도를 열고, 정류장을 선택하고, 자연의 이야기를 들어보세요.", ja: "地図を開き、停留所を選び、自然の物語を聞きましょう。" },
  ctaBtn:     { vi: "Mở bản đồ ngay", en: "Open map now", zh: "立即打开地图", ko: "지금 지도 열기", ja: "今すぐ地図を開く" },
};

type Props = { onOpenMap: () => void };

export default function HomePage({ onOpenMap }: Props) {
  const { lang } = useLang();
  const t = (key: keyof typeof T) => {
    const val = T[key];
    if (typeof val === "object" && !Array.isArray(val)) {
      return (val as Record<string, string>)[lang] ?? (val as Record<string, string>)["en"] ?? "";
    }
    return "";
  };

  const stats = [
    { value: t("stat1v"), label: t("stat1l") },
    { value: t("stat2v"), label: t("stat2l") },
    { value: t("stat3v"), label: t("stat3l") },
    { value: t("stat4v"), label: t("stat4l") },
  ];

  const categories = [
    { icon: "🐘", title: t("cat1t"), description: t("cat1d"), className: "animal" },
    { icon: "🌳", title: t("cat2t"), description: t("cat2d"), className: "plant"  },
    { icon: "🎧", title: t("cat3t"), description: t("cat3d"), className: "audio"  },
  ];

  const highlights = [
    { img: "/images/highlight-elephant.jpg", tag: t("tagAnimal"), tagClass: "tag--animal", title: t("hl1t"), desc: t("hl1d") },
    { img: "/images/highlight-giraffe.jpg",  tag: t("tagAnimal"), tagClass: "tag--animal", title: t("hl2t"), desc: t("hl2d") },
    { img: "/images/highlight-lotus.jpg",    tag: t("tagPlant"),  tagClass: "tag--plant",  title: t("hl3t"), desc: t("hl3d") },
    { img: "/images/highlight-tree.jpg",     tag: t("tagPlant"),  tagClass: "tag--plant",  title: t("hl4t"), desc: t("hl4d") },
  ];

  const gallery = T.gal.map((g, i) => ({
    img: `/images/gallery-${i + 1}.jpg`,
    alt: g[lang] ?? g["en"] ?? "",
  }));

  return (
    <>
      {/* ── HERO ── */}
      <section className="hero">
        <div className="hero__content">
          <span className="eyebrow">{t("heroEyebrow")}</span>
          <h1>{t("heroTitle")}<br /><span>{t("heroSub")}</span></h1>
          <p>{t("heroDesc")}</p>
          <div className="hero__actions">
            <button className="button button--yellow" type="button" onClick={onOpenMap}>
              {t("btnMap")} <span aria-hidden="true">↗</span>
            </button>
            <a className="button button--outline" href="#discover">{t("btnMore")}</a>
          </div>
          <div className="hero__tags">
            <span>{t("tag1")}</span><span>{t("tag2")}</span>
            <span>{t("tag3")}</span><span>{t("tag4")}</span>
          </div>
        </div>

      </section>

      {/* ── STATS ── */}
      <section className="stats-bar" aria-label="stats">
        {stats.map((s) => (
          <div className="stats-bar__item" key={s.label}>
            <strong>{s.value}</strong><span>{s.label}</span>
          </div>
        ))}
      </section>

      {/* ── DISCOVER ── */}
      <section id="discover" className="section">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t("discoverEyebrow")}</span>
            <h2>{t("discoverTitle")}</h2>
          </div>
          <p>{t("discoverDesc")}</p>
        </div>
        <div className="category-grid">
          {categories.map((cat) => (
            <article className={`category-card category-card--${cat.className}`} key={cat.title}>
              <div className="category-card__icon" aria-hidden="true">{cat.icon}</div>
              <h3>{cat.title}</h3>
              <p>{cat.description}</p>
              <button className="text-button" type="button" onClick={onOpenMap}>
                {t("openMap")} <span aria-hidden="true">→</span>
              </button>
            </article>
          ))}
        </div>
      </section>

      {/* ── HIGHLIGHTS ── */}
      <section className="section highlights-section">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t("hlEyebrow")}</span>
            <h2>{t("hlTitle")}</h2>
          </div>
          <p>{t("hlDesc")}</p>
        </div>
        <div className="highlights-grid">
          {highlights.map((h) => (
            <article className="highlight-card" key={h.title}>
              <div className="highlight-card__img-wrap">
                <img
                  src={h.img} alt={h.title} className="highlight-card__img"
                  onError={(e) => {
                    (e.currentTarget as HTMLImageElement).style.display = "none";
                    const ph = (e.currentTarget as HTMLImageElement).nextElementSibling as HTMLElement | null;
                    if (ph) ph.style.display = "flex";
                  }}
                />
                <div className="highlight-card__img-placeholder" style={{ display: "none" }} aria-hidden="true">🌿</div>
              </div>
              <div className="highlight-card__body">
                <span className={`tag ${h.tagClass}`}>{h.tag}</span>
                <h3>{h.title}</h3>
                <p>{h.desc}</p>
                <button className="text-button" type="button" onClick={onOpenMap}>
                  {t("viewOnMap")} <span aria-hidden="true">→</span>
                </button>
              </div>
            </article>
          ))}
        </div>
      </section>

      {/* ── GUIDE ── */}
      <section className="guide section">
        <div>
          <span className="eyebrow eyebrow--green">{t("guideEyebrow")}</span>
          <h2>{t("guideTitle")}</h2>
          <p>{t("guideDesc")}</p>
        </div>
        <ol className="guide__steps">
          {[["s1t","s1d"],["s2t","s2d"],["s3t","s3d"]].map(([tt, td], i) => (
            <li key={i}>
              <span>0{i+1}</span>
              <div><h3>{t(tt as keyof typeof T)}</h3><p>{t(td as keyof typeof T)}</p></div>
            </li>
          ))}
        </ol>
      </section>

      {/* ── GALLERY ── */}
      <section className="gallery-section" aria-label={t("galTitle")}>
        <div className="gallery-section__header section">
          <div>
            <span className="eyebrow eyebrow--green">{t("galEyebrow")}</span>
            <h2>{t("galTitle")}</h2>
          </div>
          <p>{t("galDesc")}</p>
        </div>
        <div className="gallery-grid">
          {gallery.map((g, i) => (
            <div className={`gallery-item${i === 0 ? " gallery-item--wide" : ""}`} key={g.alt}>
              <img
                src={g.img} alt={g.alt} className="gallery-item__img"
                onError={(e) => {
                  const wrap = (e.currentTarget as HTMLImageElement).closest(".gallery-item") as HTMLElement | null;
                  if (wrap) wrap.classList.add("gallery-item--placeholder");
                  (e.currentTarget as HTMLImageElement).style.display = "none";
                }}
              />
              <div className="gallery-item__placeholder" aria-hidden="true">📷</div>
              <span className="gallery-item__caption">{g.alt}</span>
            </div>
          ))}
        </div>
      </section>

      {/* ── CTA ── */}
      <section className="cta-section">
        <div className="cta-section__content">
          <span className="eyebrow">{t("ctaEyebrow")}</span>
          <h2>{t("ctaTitle")}</h2>
          <p>{t("ctaDesc")}</p>
          <button className="button button--yellow" type="button" onClick={onOpenMap}>
            {t("ctaBtn")} <span aria-hidden="true">↗</span>
          </button>
        </div>
      </section>
    </>
  );
}
