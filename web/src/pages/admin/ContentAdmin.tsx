import { useState } from "react";
import type { ContentStore } from "../../data/store";
import type { Lang, Translations } from "../../i18n";

type Props = {
  content: ContentStore;
  onChange: (content: ContentStore) => void;
};

const LANG_LABELS: Record<Lang, string> = { vi: "🇻🇳 Tiếng Việt", en: "🇬🇧 English" };

/* Nhóm các key theo section để dễ quản lý */
const SECTIONS: { label: string; keys: (keyof Translations)[] }[] = [
  {
    label: "Header & Nav",
    keys: ["brandTagline", "langLabel", "navExplore", "navHome", "navMap", "navNote", "footerTagline"],
  },
  {
    label: "Banner chính (Hero)",
    keys: ["heroEyebrow", "heroTitle", "heroSubtitle", "heroDesc", "heroBtnMap", "heroBtnMore",
           "heroTag1", "heroTag2", "heroTag3", "heroTag4", "heroArtLabel"],
  },
  {
    label: "Thống kê",
    keys: ["stat1Label", "stat2Label", "stat3Label", "stat4Label"],
  },
  {
    label: "Khám phá theo chủ đề",
    keys: ["discoverEyebrow", "discoverTitle", "discoverDesc",
           "cat1Title", "cat1Desc", "cat2Title", "cat2Desc", "cat3Title", "cat3Desc", "openMap"],
  },
  {
    label: "Điểm nổi bật",
    keys: ["highlightsEyebrow", "highlightsTitle", "highlightsDesc",
           "tagAnimal", "tagPlant",
           "hl1Title", "hl1Desc", "hl2Title", "hl2Desc",
           "hl3Title", "hl3Desc", "hl4Title", "hl4Desc", "viewOnMap"],
  },
  {
    label: "Hướng dẫn tham quan",
    keys: ["guideEyebrow", "guideTitle", "guideDesc",
           "step1Title", "step1Desc", "step2Title", "step2Desc", "step3Title", "step3Desc"],
  },
  {
    label: "Gallery",
    keys: ["galleryEyebrow", "galleryTitle", "galleryDesc",
           "gallery1", "gallery2", "gallery3", "gallery4", "gallery5", "gallery6"],
  },
  {
    label: "CTA & Bản đồ",
    keys: ["ctaEyebrow", "ctaTitle", "ctaDesc", "ctaBtn",
           "mapEyebrow", "mapTitle", "mapDesc", "mapDrag", "mapReturnBtn", "mapHelp"],
  },
];

export default function ContentAdmin({ content, onChange }: Props) {
  const [activeLang, setActiveLang] = useState<Lang>("vi");
  const [openSection, setOpenSection] = useState<string>(SECTIONS[0].label);

  function handleChange(key: keyof Translations, value: string) {
    onChange({
      ...content,
      [activeLang]: { ...content[activeLang], [key]: value },
    });
  }

  const isLong = (key: keyof Translations) =>
    ["heroDesc", "navNote", "mapHelp", "ctaDesc",
     "cat1Desc", "cat2Desc", "cat3Desc",
     "hl1Desc", "hl2Desc", "hl3Desc", "hl4Desc",
     "step1Desc", "step2Desc", "step3Desc"].includes(key as string);

  return (
    <div className="admin-section">
      <div className="admin-section__header">
        <h2>Nội dung đa ngôn ngữ</h2>
        {/* Tab chọn ngôn ngữ */}
        <div className="admin-lang-tabs">
          {(Object.keys(LANG_LABELS) as Lang[]).map((l) => (
            <button
              key={l}
              className={`admin-lang-tab${activeLang === l ? " is-active" : ""}`}
              onClick={() => setActiveLang(l)}
            >
              {LANG_LABELS[l]}
            </button>
          ))}
        </div>
      </div>

      <p className="admin-hint">
        Chỉnh sửa văn bản hiển thị trên trang. Thay đổi lưu tạm thời — nhấn <strong>Lưu tất cả</strong> để áp dụng.
      </p>

      {/* Accordion theo section */}
      {SECTIONS.map((sec) => (
        <div className="admin-accordion" key={sec.label}>
          <button
            className={`admin-accordion__head${openSection === sec.label ? " is-open" : ""}`}
            onClick={() => setOpenSection(openSection === sec.label ? "" : sec.label)}
          >
            {sec.label}
            <span>{openSection === sec.label ? "▲" : "▼"}</span>
          </button>

          {openSection === sec.label && (
            <div className="admin-accordion__body">
              {sec.keys.map((key) => (
                <label className="admin-form__field" key={key as string}>
                  <span className="admin-form__key">{key as string}</span>
                  {isLong(key) ? (
                    <textarea
                      rows={3}
                      value={content[activeLang][key]}
                      onChange={(e) => handleChange(key, e.target.value)}
                    />
                  ) : (
                    <input
                      value={content[activeLang][key]}
                      onChange={(e) => handleChange(key, e.target.value)}
                    />
                  )}
                </label>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
