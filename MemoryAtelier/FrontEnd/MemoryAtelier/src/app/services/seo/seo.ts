import { DOCUMENT, Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

export interface SeoData {
  title: string;
  description: string;
  image?: string;
  type?: 'website' | 'product' | 'article';
  path?: string;
  noindex?: boolean;
}

// Смени с реалния production домейн, ако е различен от memoryatelier.bg
const SITE_URL = 'https://memoryatelier.bg';
const DEFAULT_IMAGE = `${SITE_URL}/roundedLogo.png`;

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly doc = inject(DOCUMENT);

  update(data: SeoData): void {
    const url = `${SITE_URL}${data.path ?? this.doc.location.pathname}`;
    const image = data.image ?? DEFAULT_IMAGE;

    this.title.setTitle(data.title);
    this.setTag('description', data.description);
    this.setTag('robots', data.noindex ? 'noindex, nofollow' : 'index, follow');
    this.setTag('og:site_name', 'Memory Atelier', true);
    this.setTag('og:title', data.title, true);
    this.setTag('og:description', data.description, true);
    this.setTag('og:type', data.type ?? 'website', true);
    this.setTag('og:url', url, true);
    this.setTag('og:image', image, true);
    this.setTag('twitter:card', 'summary_large_image');
    this.setTag('twitter:title', data.title);
    this.setTag('twitter:description', data.description);
    this.setTag('twitter:image', image);
    this.setCanonical(url);
  }

  setJsonLd(id: string, data: object): void {
    this.removeJsonLd(id);
    const script = this.doc.createElement('script');
    script.type = 'application/ld+json';
    script.id = id;
    script.text = JSON.stringify(data);
    this.doc.head.appendChild(script);
  }

  removeJsonLd(id: string): void {
    this.doc.getElementById(id)?.remove();
  }

  private setTag(name: string, content: string, isProperty = false): void {
    const attr = isProperty ? 'property' : 'name';
    this.meta.updateTag({ [attr]: name, content });
  }

  private setCanonical(url: string): void {
    let link = this.doc.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.doc.head.appendChild(link);
    }
    link.setAttribute('href', url);
  }
}
