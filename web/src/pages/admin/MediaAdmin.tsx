/* ======================================================
   MediaAdmin — quản lý ảnh & audio
   Hiện tại chỉ hiển thị danh sách file đang dùng
   và hướng dẫn upload thủ công.
   Khi có backend: thay bằng form upload thật.
   ====================================================== */

import { type Spot } from "../../data/spots";

type Props = { spots: Spot[] };

const HERO_IMAGES = [
  { path: "/images/hero-bg.jpg",       label: "Ảnh nền Hero" },
  { path: "/images/cta-bg.jpg",        label: "Ảnh nền CTA" },
];

const HIGHLIGHT_IMAGES = [
  { path: "/images/highlight-elephant.jpg", label: "Highlight — Voi châu Á" },
  { path: "/images/highlight-giraffe.jpg",  label: "Highlight — Hươu cao cổ" },
  { path: "/images/highlight-lotus.jpg",    label: "Highlight — Hồ Sen" },
  { path: "/images/highlight-tree.jpg",     label: "Highlight — Cây cổ thụ" },
];

const GALLERY_IMAGES = Array.from({ length: 6 }, (_, i) => ({
  path:  `/images/gallery-${i + 1}.jpg`,
  label: `Gallery ${i + 1}`,
}));

function ImageCard({ path, label }: { path: string; label: string }) {
  return (
    <div className="media-card">
      <div className="media-card__preview">
        <img
          src={path}
          alt={label}
          onError={(e) => {
            (e.currentTarget as HTMLImageElement).style.display = "none";
            const ph = (e.currentTarget as HTMLImageElement)
              .nextElementSibling as HTMLElement | null;
            if (ph) ph.style.display = "flex";
          }}
        />
        <div className="media-card__placeholder" style={{ display: "none" }}>📷</div>
      </div>
      <div className="media-card__info">
        <strong>{label}</strong>
        <code>{path}</code>
      </div>
    </div>
  );
}

export default function MediaAdmin({ spots }: Props) {
  return (
    <div className="admin-section">
      <div className="admin-section__header">
        <h2>Quản lý Media</h2>
      </div>

      <div className="admin-notice">
        <span>ℹ️</span>
        <div>
          <strong>Hướng dẫn upload ảnh thủ công</strong>
          <p>
            Đặt file ảnh vào thư mục <code>web/public/images/</code> với đúng tên như bảng bên dưới.
            Khi có backend, chức năng upload trực tiếp sẽ được tích hợp tại đây.
          </p>
        </div>
      </div>

      {/* Ảnh trang chủ */}
      <h3 className="admin-subtitle">Ảnh trang chủ</h3>
      <div className="media-grid">
        {[...HERO_IMAGES, ...HIGHLIGHT_IMAGES, ...GALLERY_IMAGES].map((img) => (
          <ImageCard key={img.path} {...img} />
        ))}
      </div>

      {/* Ảnh điểm tham quan */}
      <h3 className="admin-subtitle">Ảnh điểm tham quan</h3>
      <div className="media-grid">
        {spots.map((spot) => (
          <ImageCard
            key={spot.id}
            path={spot.img}
            label={`${spot.icon} ${spot.name.vi}`}
          />
        ))}
      </div>

      {/* Placeholder audio */}
      <h3 className="admin-subtitle">Audio thuyết minh</h3>
      <div className="admin-notice admin-notice--warning">
        <span>🎧</span>
        <div>
          <strong>Chưa có audio</strong>
          <p>
            Đặt file <code>.mp3</code> vào <code>web/public/audio/&lt;spot-id&gt;.mp3</code>.
            Ví dụ: <code>audio/elephant.mp3</code>.
            Tích hợp audio player sẽ được thêm trong bước tiếp theo.
          </p>
        </div>
      </div>

      <div className="media-grid">
        {spots.map((spot) => (
          <div className="media-card" key={spot.id}>
            <div className="media-card__preview media-card__preview--audio">
              <span>🎧</span>
            </div>
            <div className="media-card__info">
              <strong>{spot.icon} {spot.name.vi}</strong>
              <code>/audio/{spot.id}.mp3</code>
              <span className="media-card__status media-card__status--missing">
                Chưa có file
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
