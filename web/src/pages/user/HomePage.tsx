import type { Translations } from "../../i18n";

type HomePageProps = {
  onOpenMap: () => void;
  t: Translations;
};

export default function HomePage({ onOpenMap, t }: HomePageProps) {
  const stats = [
    { value: "12 ha", label: t.stat1Label },
    { value: "1864",  label: t.stat2Label },
    { value: "150+",  label: t.stat3Label },
    { value: "1000+", label: t.stat4Label },
  ];

  const categories = [
    { icon: "🐘", title: t.cat1Title, description: t.cat1Desc, className: "animal" },
    { icon: "🌳", title: t.cat2Title, description: t.cat2Desc, className: "plant"  },
    { icon: "🎧", title: t.cat3Title, description: t.cat3Desc, className: "audio"  },
  ];

  const highlights = [
    { img: "/images/highlight-elephant.jpg", tag: t.tagAnimal, tagClass: "tag--animal", title: t.hl1Title, desc: t.hl1Desc },
    { img: "/images/highlight-giraffe.jpg",  tag: t.tagAnimal, tagClass: "tag--animal", title: t.hl2Title, desc: t.hl2Desc },
    { img: "/images/highlight-lotus.jpg",    tag: t.tagPlant,  tagClass: "tag--plant",  title: t.hl3Title, desc: t.hl3Desc },
    { img: "/images/highlight-tree.jpg",     tag: t.tagPlant,  tagClass: "tag--plant",  title: t.hl4Title, desc: t.hl4Desc },
  ];

  const gallery = [
    { img: "/images/gallery-1.jpg", alt: t.gallery1 },
    { img: "/images/gallery-2.jpg", alt: t.gallery2 },
    { img: "/images/gallery-3.jpg", alt: t.gallery3 },
    { img: "/images/gallery-4.jpg", alt: t.gallery4 },
    { img: "/images/gallery-5.jpg", alt: t.gallery5 },
    { img: "/images/gallery-6.jpg", alt: t.gallery6 },
  ];

  return (
    <>
      {/* ── 1. HERO ── */}
      <section className="hero">
        <div className="hero__content">
          <span className="eyebrow">{t.heroEyebrow}</span>
          <h1>
            {t.heroTitle}
            <br />
            <span>{t.heroSubtitle}</span>
          </h1>
          <p>{t.heroDesc}</p>
          <div className="hero__actions">
            <button className="button button--yellow" type="button" onClick={onOpenMap}>
              {t.heroBtnMap} <span aria-hidden="true">↗</span>
            </button>
            <a className="button button--outline" href="#discover">
              {t.heroBtnMore}
            </a>
          </div>
          <div className="hero__tags">
            <span>{t.heroTag1}</span>
            <span>{t.heroTag2}</span>
            <span>{t.heroTag3}</span>
            <span>{t.heroTag4}</span>
          </div>
        </div>

        <div className="hero-visual" aria-hidden="true">
          <div className="hero-art">
            <div className="hero-art__circle" />
            <span className="hero-art__tree">🌳</span>
            <span className="hero-art__elephant">🐘</span>
            <span className="hero-art__giraffe">🦒</span>
            <span className="hero-art__leaf">🌿</span>
            <div className="hero-art__label">{t.heroArtLabel}</div>
          </div>
        </div>
      </section>

      {/* ── 2. THỐNG KÊ ── */}
      <section className="stats-bar" aria-label={t.stat1Label}>
        {stats.map((s) => (
          <div className="stats-bar__item" key={s.label}>
            <strong>{s.value}</strong>
            <span>{s.label}</span>
          </div>
        ))}
      </section>

      {/* ── 3. KHÁM PHÁ ── */}
      <section id="discover" className="section">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t.discoverEyebrow}</span>
            <h2>{t.discoverTitle}</h2>
          </div>
          <p>{t.discoverDesc}</p>
        </div>
        <div className="category-grid">
          {categories.map((cat) => (
            <article className={`category-card category-card--${cat.className}`} key={cat.title}>
              <div className="category-card__icon" aria-hidden="true">{cat.icon}</div>
              <h3>{cat.title}</h3>
              <p>{cat.description}</p>
              <button className="text-button" type="button" onClick={onOpenMap}>
                {t.openMap} <span aria-hidden="true">→</span>
              </button>
            </article>
          ))}
        </div>
      </section>

      {/* ── 4. ĐIỂM NỔI BẬT ── */}
      <section className="section highlights-section">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t.highlightsEyebrow}</span>
            <h2>{t.highlightsTitle}</h2>
          </div>
          <p>{t.highlightsDesc}</p>
        </div>
        <div className="highlights-grid">
          {highlights.map((h) => (
            <article className="highlight-card" key={h.title}>
              <div className="highlight-card__img-wrap">
                <img
                  src={h.img}
                  alt={h.title}
                  className="highlight-card__img"
                  onError={(e) => {
                    const img = e.currentTarget as HTMLImageElement;
                    img.style.display = "none";
                    const ph = img.nextElementSibling as HTMLElement | null;
                    if (ph) ph.style.display = "flex";
                  }}
                />
                <div className="highlight-card__img-placeholder" aria-hidden="true" style={{ display: "none" }}>
                  🌿
                </div>
              </div>
              <div className="highlight-card__body">
                <span className={`tag ${h.tagClass}`}>{h.tag}</span>
                <h3>{h.title}</h3>
                <p>{h.desc}</p>
                <button className="text-button" type="button" onClick={onOpenMap}>
                  {t.viewOnMap} <span aria-hidden="true">→</span>
                </button>
              </div>
            </article>
          ))}
        </div>
      </section>

      {/* ── 5. HƯỚNG DẪN ── */}
      <section className="guide section">
        <div>
          <span className="eyebrow eyebrow--green">{t.guideEyebrow}</span>
          <h2>{t.guideTitle}</h2>
          <p>{t.guideDesc}</p>
        </div>
        <ol className="guide__steps">
          <li>
            <span>01</span>
            <div><h3>{t.step1Title}</h3><p>{t.step1Desc}</p></div>
          </li>
          <li>
            <span>02</span>
            <div><h3>{t.step2Title}</h3><p>{t.step2Desc}</p></div>
          </li>
          <li>
            <span>03</span>
            <div><h3>{t.step3Title}</h3><p>{t.step3Desc}</p></div>
          </li>
        </ol>
      </section>

      {/* ── 6. GALLERY ── */}
      <section className="gallery-section" aria-label={t.galleryTitle}>
        <div className="gallery-section__header section">
          <div>
            <span className="eyebrow eyebrow--green">{t.galleryEyebrow}</span>
            <h2>{t.galleryTitle}</h2>
          </div>
          <p>{t.galleryDesc}</p>
        </div>
        <div className="gallery-grid">
          {gallery.map((g, i) => (
            <div
              className={`gallery-item${i === 0 ? " gallery-item--wide" : ""}`}
              key={g.alt}
            >
              <img
                src={g.img}
                alt={g.alt}
                className="gallery-item__img"
                onError={(e) => {
                  const wrap = (e.currentTarget as HTMLImageElement)
                    .closest(".gallery-item") as HTMLElement | null;
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

      {/* ── 7. CTA ── */}
      <section className="cta-section">
        <div className="cta-section__content">
          <span className="eyebrow">{t.ctaEyebrow}</span>
          <h2>{t.ctaTitle}</h2>
          <p>{t.ctaDesc}</p>
          <button className="button button--yellow" type="button" onClick={onOpenMap}>
            {t.ctaBtn} <span aria-hidden="true">↗</span>
          </button>
        </div>
      </section>
    </>
  );
}
