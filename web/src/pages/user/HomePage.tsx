type HomePageProps = {
  onOpenMap: () => void;
};

const categories = [
  {
    icon: "🐘",
    title: "Thế giới động vật",
    description:
      "Tìm hiểu đặc điểm, tập tính và những câu chuyện thú vị về các loài.",
    className: "animal",
  },
  {
    icon: "🌳",
    title: "Không gian thực vật",
    description:
      "Khám phá cây xanh và cảm nhận sự đa dạng của thế giới thực vật.",
    className: "plant",
  },
  {
    icon: "🎧",
    title: "Lắng nghe thiên nhiên",
    description:
      "Tìm hiểu các điểm tham quan qua nội dung thuyết minh trong hành trình.",
    className: "audio",
  },
];

export default function HomePage({ onOpenMap }: HomePageProps) {
  return (
    <>
      <section className="hero">
        <div className="hero__content">
          <span className="eyebrow">CHÀO MỪNG BẠN ĐẾN VỚI</span>

          <h1>
            Thảo Cầm Viên
            <br />
            <span>Chạm gần thiên nhiên.</span>
          </h1>

          <p>
            Dạo bước giữa không gian xanh, khám phá các loài động vật
            và lắng nghe câu chuyện tại mỗi điểm dừng chân.
          </p>

          <div className="hero__actions">
            <button
              className="button button--yellow"
              type="button"
              onClick={onOpenMap}
            >
              Khám phá bản đồ <span aria-hidden="true">↗</span>
            </button>

            <a className="button button--outline" href="#discover">
              Tìm hiểu thêm
            </a>
          </div>

          <div className="hero__tags">
            <span>🌱 Không gian xanh</span>
            <span>🐾 Khám phá muôn loài</span>
            <span>🎧 Thuyết minh</span>
          </div>
        </div>

        <div className="hero-art" aria-hidden="true">
          <div className="hero-art__circle" />
          <span className="hero-art__tree">🌳</span>
          <span className="hero-art__elephant">🐘</span>
          <span className="hero-art__giraffe">🦒</span>
          <span className="hero-art__leaf">🌿</span>

          <div className="hero-art__label">
            Một ngày giữa thiên nhiên
          </div>
        </div>
      </section>

      <section id="discover" className="section">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">
              MỖI BƯỚC CHÂN, MỘT KHÁM PHÁ
            </span>

            <h2>Bạn muốn khám phá điều gì?</h2>
          </div>

          <p>Chọn điểm dừng yêu thích trên bản đồ để bắt đầu.</p>
        </div>

        <div className="category-grid">
          {categories.map((category) => (
            <article
              className={`category-card category-card--${category.className}`}
              key={category.title}
            >
              <div className="category-card__icon" aria-hidden="true">
                {category.icon}
              </div>

              <h3>{category.title}</h3>
              <p>{category.description}</p>

              <button
                className="text-button"
                type="button"
                onClick={onOpenMap}
              >
                Mở bản đồ <span aria-hidden="true">→</span>
              </button>
            </article>
          ))}
        </div>
      </section>

      <section className="guide section">
        <div>
          <span className="eyebrow eyebrow--green">
            HƯỚNG DẪN THAM QUAN
          </span>

          <h2>Hành trình bắt đầu từ đây</h2>
          <p>Ba bước để khám phá Thảo Cầm Viên cùng bản đồ.</p>
        </div>

        <ol className="guide__steps">
          <li>
            <span>01</span>
            <div>
              <h3>Mở bản đồ</h3>
              <p>Xem các khu vực và điểm tham quan.</p>
            </div>
          </li>

          <li>
            <span>02</span>
            <div>
              <h3>Chọn điểm bạn thích</h3>
              <p>Xem thông tin về loài hoặc khu vực muốn khám phá.</p>
            </div>
          </li>

          <li>
            <span>03</span>
            <div>
              <h3>Lắng nghe và khám phá</h3>
              <p>Nghe thuyết minh khi nội dung đã được tích hợp.</p>
            </div>
          </li>
        </ol>
      </section>
    </>
  );
}