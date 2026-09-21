import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Product } from '../../services/auth/auth-types';
import { ProductService } from '../../services/product/product';  // <- вместо HttpClient
import { CartService } from '../../services/cartService/cartService';
import { FavouritesService } from '../../services/favourites/favourites';
import { AuthService } from '../../services/auth/auth';
import { HttpClient } from '@angular/common/http';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { environment } from '../../../environments/environment';
import { ThumbUrlPipe } from '../../shared/thumb-url';

export interface Review {
  id: string;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, ThumbUrlPipe],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css'
})
export class ProductDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private http = inject(HttpClient);
  private productService = inject(ProductService);  // <- добавено
  public cartService = inject(CartService);
  public favouritesService = inject(FavouritesService);
  public authService = inject(AuthService);
  public i18n = inject(I18nService);
  private seo = inject(SeoService);

  private readonly REVIEWS_API = `${environment.apiUrl}/review`;

  product = signal<Product | null>(null);
  suggestions = signal<Product[]>([]);
  reviews = signal<Review[]>([]);
  loading = signal(true);
  selectedImage = signal(0);
  quantity = signal(1);
  addedToCart = signal(false);
  isFav = signal(false);
  activeTab = signal<'description' | 'reviews'>('description');

  newRating = signal(5);
  newComment = signal('');
  submittingReview = signal(false);

  avgRating = computed(() => {
    const r = this.reviews();
    if (!r.length) return 0;
    return r.reduce((s, x) => s + x.rating, 0) / r.length;
  });

  ratingBreakdown = computed(() => {
    const r = this.reviews();
    return [5, 4, 3, 2, 1].map(star => ({
      star,
      count: r.filter(x => x.rating === star).length,
      pct: r.length ? Math.round(r.filter(x => x.rating === star).length / r.length * 100) : 0
    }));
  });

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id')!;
      this.loadProduct(id);
    });
  }

  loadProduct(id: string) {
    this.loading.set(true);
    this.reviews.set([]);
    this.suggestions.set([]);
    this.isFav.set(false);

    this.productService.getById(id).subscribe({
      next: (p) => {
        this.product.set(p);
        this.selectedImage.set(0);
        this.quantity.set(1);
        this.loading.set(false);           // <- loading спира само след продукта

        this.canonicalizeUrl(p);
        this.updateSeo(p);

        this.loadSuggestions(p.categories[0]?.id, id);
        this.loadReviews(id);              // грешката тук не блокира страницата
        if (this.authService.isLoggedIn()) this.checkFav(id);
      },
      error: (err) => {
        console.error('Product load error:', err);
        this.loading.set(false);
      }
    });
  }

  /** Пренасочва стари /product/:id линкове (без slug) към каноничния /product/:id/:slug адрес, без да презарежда компонента. */
  private canonicalizeUrl(p: Product) {
    const canonicalPath = `/product/${p.id}/${p.slug}`;
    if (this.location.path() !== canonicalPath) {
      this.location.replaceState(canonicalPath);
    }
  }

  private updateSeo(p: Product) {
    const name = this.i18n.pick(p.name, p.nameEn);
    const description = this.i18n.pick(p.description, p.descriptionEn) || name;
    const image = p.images[0]?.imageUrl;

    this.seo.update({
      title: `${name} — €${p.price.toFixed(2)} | Memory Atelier`,
      description: this.truncateForMeta(description),
      image,
      type: 'product',
      path: `/product/${p.id}/${p.slug}`
    });

    this.updateProductJsonLd(p);
  }

  private updateProductJsonLd(p: Product) {
    const name = this.i18n.pick(p.name, p.nameEn);
    const description = this.i18n.pick(p.description, p.descriptionEn) || name;
    const reviews = this.reviews();

    this.seo.setJsonLd('product-jsonld', {
      '@context': 'https://schema.org',
      '@type': 'Product',
      name,
      description,
      image: p.images.map(i => i.imageUrl),
      sku: p.id,
      offers: {
        '@type': 'Offer',
        priceCurrency: 'EUR',
        price: p.price.toFixed(2),
        availability: p.stock > 0 ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock',
        url: `https://memoryatelier.bg/product/${p.id}/${p.slug}`
      },
      ...(reviews.length > 0 ? {
        aggregateRating: {
          '@type': 'AggregateRating',
          ratingValue: this.avgRating().toFixed(1),
          reviewCount: reviews.length
        }
      } : {})
    });

    this.updateBreadcrumbJsonLd(p, name);
  }

  private updateBreadcrumbJsonLd(p: Product, name: string) {
    const category = p.categories[0];
    const items = [
      { name: this.i18n.t('pd.breadcrumbHome'), url: 'https://memoryatelier.bg/home' },
      ...(category ? [{ name: this.i18n.pick(category.name, category.nameEn), url: 'https://memoryatelier.bg/home' }] : []),
      { name, url: `https://memoryatelier.bg/product/${p.id}/${p.slug}` }
    ];

    this.seo.setJsonLd('breadcrumb-jsonld', {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: items.map((item, index) => ({
        '@type': 'ListItem',
        position: index + 1,
        name: item.name,
        item: item.url
      }))
    });
  }

  private truncateForMeta(text: string, maxLength = 160): string {
    return text.length > maxLength ? text.slice(0, maxLength - 1).trimEnd() + '…' : text;
  }

  loadSuggestions(categoryId: string | undefined, excludeId: string) {
    this.productService.getAll(categoryId).subscribe({   // <- getAll от ProductService
      next: (all) => this.suggestions.set(all.filter(p => p.id !== excludeId).slice(0, 4))
    });
  }

  loadReviews(productId: string) {
    this.http.get<Review[]>(`${this.REVIEWS_API}/${productId}`).subscribe({
      next: (r) => {
        this.reviews.set(r);
        const p = this.product();
        if (p) this.updateProductJsonLd(p);
      },
      error: () => this.reviews.set([])    // <- тихо fail-ва, страницата си работи
    });
  }

  checkFav(productId: string) {
    this.favouritesService.getFavourites().subscribe({
      next: (favs) => this.isFav.set(favs.some(f => f.productId === productId))
    });
  }

  selectImage(i: number) { this.selectedImage.set(i); }

  private readonly MAX_QTY = 99;

  changeQty(delta: number) {
    const p = this.product();
    if (!p) return;
    this.quantity.update(q => Math.min(this.MAX_QTY, Math.max(1, q + delta)));
  }

  exceedsStock(): boolean {
    const p = this.product();
    return !!p && this.quantity() > p.stock;
  }

  onQtyInput(event: Event) {
    const raw = (event.target as HTMLInputElement).value;
    const parsed = Math.trunc(Number(raw));
    const clamped = Number.isFinite(parsed) && parsed > 0 ? Math.min(this.MAX_QTY, parsed) : 1;
    this.quantity.set(clamped);
    (event.target as HTMLInputElement).value = String(clamped);
  }

  addToCart() {
    const p = this.product();
    if (!p || !this.authService.isLoggedIn()) return;
    this.cartService.addToCart(p.id, this.quantity()).subscribe({
      next: () => {
        this.addedToCart.set(true);
        setTimeout(() => this.addedToCart.set(false), 2500);
      }
    });
  }

  toggleFav() {
    const p = this.product();
    if (!p || !this.authService.isLoggedIn()) return;
    this.favouritesService.toggle(p.id).subscribe({
      next: (res) => this.isFav.set(res.added)
    });
  }

  submitReview() {
    const p = this.product();
    if (!p || !this.newComment().trim()) return;
    this.submittingReview.set(true);
    this.http.post<Review>(`${this.REVIEWS_API}/${p.id}`, {
      rating: this.newRating(),
      comment: this.newComment()
    }).subscribe({
      next: (r) => {
        this.reviews.update(arr => [r, ...arr]);
        this.newComment.set('');
        this.newRating.set(5);
        this.submittingReview.set(false);
      },
      error: () => this.submittingReview.set(false)
    });
  }

  goToProduct(p: Product) { this.router.navigate(['/product', p.id, p.slug]); }
  goBack() { this.router.navigate(['/home']); }

  categoryNames(categories: { name: string; nameEn: string | null }[]): string {
    return categories.map(c => this.i18n.pick(c.name, c.nameEn)).join(', ');
  }

  stars(n: number) { return Array(5).fill(0).map((_, i) => i < Math.round(n)); }
  formatDate(d: string) { return new Date(d).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }); }
  mainImage() { const p = this.product(); return p?.images[this.selectedImage()]?.imageUrl ?? 'https://placehold.co/600x500'; }

  // -thumb companion файлът може да липсва за снимки, качени преди thumbnail генерирането
  // (или преди reprocess-legacy backfill-а) — тогава падаме обратно на пълната снимка.
  onThumbError(event: Event, original: string): void {
    const el = event.target as HTMLImageElement;
    if (el.src !== original) el.src = original;
  }
}