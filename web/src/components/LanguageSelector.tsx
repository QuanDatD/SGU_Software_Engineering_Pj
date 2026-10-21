import { useState } from "react";
import { LANGUAGES, type Lang } from "../types/common";
import { useLang } from "../contexts/LanguageContext";

export default function LanguageSelector() {
  const { lang, setLang } = useLang();
  const [open, setOpen] = useState(false);
  const current = LANGUAGES.find((l) => l.code === lang)!;

  function choose(code: Lang) {
    setLang(code);
    setOpen(false);
  }

  return (
    <div className="lang-picker">
      <button
        className="lang-picker__btn"
        type="button"
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen(!open)}
      >
        {current.flag} {current.label}
        <span className="lang-picker__arrow" aria-hidden="true">{open ? "▲" : "▼"}</span>
      </button>

      {open && (
        <>
          <button
            className="lang-picker__overlay"
            type="button"
            aria-label="Đóng"
            onClick={() => setOpen(false)}
          />
          <ul className="lang-picker__menu" role="listbox">
            {LANGUAGES.map((l) => (
              <li key={l.code}>
                <button
                  type="button"
                  role="option"
                  aria-selected={lang === l.code}
                  className={lang === l.code ? "is-active" : ""}
                  onClick={() => choose(l.code)}
                >
                  {l.flag} {l.label}
                </button>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
