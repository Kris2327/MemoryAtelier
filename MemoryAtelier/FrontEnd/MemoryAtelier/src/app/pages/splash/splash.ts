import { Component, HostListener, OnDestroy, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { I18nService } from '../../services/i18n/i18n';
import { HeroImageService } from '../../services/hero-image/hero-image';
import { ProductService } from '../../services/product/product';
import { thumbnailUrl } from '../../shared/thumb-url';

const SESSION_KEY = 'ma-splash-seen';
const LEAVE_TRANSITION_MS = 500;

@Component({
  selector: 'app-splash',
  standalone: true,
  templateUrl: './splash.html',
  styleUrl: './splash.css'
})
export class Splash implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly heroImageService = inject(HeroImageService);
  private readonly productService = inject(ProductService);
  private timer?: ReturnType<typeof setTimeout>;

  readonly leaving = signal(false);

  constructor(public readonly i18n: I18nService) {}

  ngOnInit(): void {
    if (!this.isBrowser) return;

    let alreadySeen = false;
    try {
      alreadySeen = sessionStorage.getItem(SESSION_KEY) === '1';
    } catch {
      /* sessionStorage unavailable */
    }

    if (alreadySeen) {
      this.router.navigateByUrl('/home', { replaceUrl: true });
      return;
    }

    try {
      sessionStorage.setItem(SESSION_KEY, '1');
    } catch {
      /* sessionStorage unavailable */
    }

    this.preloadHomeAssets();
  }

  ngOnDestroy(): void {
    if (this.timer) clearTimeout(this.timer);
  }

  // Докато потребителят гледа splash-а, топлим кеша на браузъра с всичко, което home страницата
  // ще поиска веднага след нея — hero снимките и thumbnail-ите на продуктите — плюс нейния JS чънк,
  // за да се появи мигновено при клик, вместо да зарежда на празно.
  private preloadHomeAssets(): void {
    this.heroImageService.getAll().subscribe({
      next: images => images.forEach(image => this.preloadImage(image.imageUrl)),
      error: () => {}
    });

    this.productService.getAll().subscribe({
      next: products => products.forEach(product => {
        const url = product.images[0]?.imageUrl;
        if (url) this.preloadImage(thumbnailUrl(url));
      }),
      error: () => {}
    });

    import('../home-page/home-page').catch(() => {});
  }

  private preloadImage(url: string): void {
    const img = new Image();
    img.src = url;
  }

  // Splash sits visually on top of the rest of the app (cookie banner, etc.), but those elements
  // are still in the DOM and keyboard-focusable, so Tab/Enter could reach and activate them
  // underneath. Swallow all keyboard input while the splash is up — only an actual click on the
  // screen moves past it.
  @HostListener('window:keydown', ['$event'])
  onWindowKeydown(event: KeyboardEvent): void {
    if (this.leaving()) return;
    event.preventDefault();
    event.stopPropagation();
  }

  // event.detail is 0 for a keyboard-triggered synthetic click (e.g. Enter/Space) and >=1 for an actual
  // mouse click — only a real click on the screen should dismiss the splash.
  enter(event?: MouseEvent): void {
    if (event && event.detail === 0) return;
    if (this.leaving()) return;
    this.leaving.set(true);
    this.timer = setTimeout(() => this.router.navigateByUrl('/home', { replaceUrl: true }), LEAVE_TRANSITION_MS);
  }
}
