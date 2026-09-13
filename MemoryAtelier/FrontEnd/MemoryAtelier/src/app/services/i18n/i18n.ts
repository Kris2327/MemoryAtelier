import { Injectable, signal } from '@angular/core';
import { TRANSLATIONS } from './translations';

export type Lang = 'bg' | 'en';

const STORAGE_KEY = 'ma-lang';

@Injectable({ providedIn: 'root' })
export class I18nService {
  lang = signal<Lang>(this.readInitialLang());

  constructor() {
    this.syncDocumentLang(this.lang());
  }

  private readInitialLang(): Lang {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'bg' || stored === 'en') return stored;
    } catch {
      /* localStorage unavailable */
    }
    return 'bg';
  }

  setLang(lang: Lang): void {
    this.lang.set(lang);
    this.syncDocumentLang(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      /* localStorage unavailable */
    }
  }

  private syncDocumentLang(lang: Lang): void {
    try {
      document.documentElement.lang = lang;
    } catch {
      /* no DOM (e.g. during SSR) */
    }
  }

  toggle(): void {
    this.setLang(this.lang() === 'bg' ? 'en' : 'bg');
  }

  t(key: string): string {
    return TRANSLATIONS[this.lang()]?.[key] ?? TRANSLATIONS['bg'][key] ?? key;
  }

  /** Picks the English variant of a bilingual field (e.g. a category name) when available, otherwise falls back to the Bulgarian one. */
  pick(bg: string, en?: string | null): string {
    return this.lang() === 'en' && en ? en : bg;
  }
}
