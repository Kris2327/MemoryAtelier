import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../services/product/product';
import { CartService } from '../../services/cartService/cartService';
import { FavouritesService } from '../../services/favourites/favourites';
import { AuthService } from '../../services/auth/auth';
import { HeroImageService } from '../../services/hero-image/hero-image';
import { CategoryRef, Product } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css'
})
export class HomePage implements OnInit, OnDestroy {
  allProducts = signal<Product[]>([]);
  selectedCategory = signal('All');
  loading = signal(false);
  favouriteIds = signal<Set<string>>(new Set());
  addedToCart = signal<Set<string>>(new Set());
  heroIndex = signal(0);
  private heroTimer?: number;

  // снимки за hero слайдъра — избрани от администратора през админ панела
  adminHeroImages = signal<string[]>([]);

  // докато администраторът не е избрал снимки, покажи по една на продукт като резервен вариант
  heroImages = computed(() => {
    const admin = this.adminHeroImages();
    if (admin.length > 0) return admin;
    return this.allProducts()
      .filter(p => p.images.length > 0)
      .slice(0, 6)
      .map(p => p.images[0].imageUrl);
  });

  // продуктите от избраната категория, групирани
  filteredGrouped = computed(() => {
    const cat = this.selectedCategory();
    const all = this.allProducts();
    const filtered = cat === 'All' ? all : all.filter(p => p.categories.some(c => c.name === cat));
    return this.groupProducts(filtered);
  });

  // продуктите от ДРУГИТЕ категории за слайдъра
  otherProducts = computed(() => {
    const cat = this.selectedCategory();
    const all = this.allProducts();
    if (cat === 'All') return [];
    return all.filter(p => !p.categories.some(c => c.name === cat));
  });

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private favouritesService: FavouritesService,
    public authService: AuthService,
    private heroImageService: HeroImageService,
    private route: ActivatedRoute,
    private router: Router,
    public i18n: I18nService,
    private seo: SeoService
  ) {}

  ngOnInit() {
    this.seo.update({
      title: 'Memory Atelier — Ръчно изработени съкровища',
      description: 'Ръчно изработени декорации, свещи, кашпи и подаръци с любов към детайла. Разгледайте колекцията на Memory Atelier.',
      path: '/home'
    });

    // зареди ВСИЧКИ продукти веднъж
    this.loadAllProducts();
    this.loadHeroImages();

    this.route.queryParams.subscribe(params => {
      const cat = params['category'] || 'All';
      this.selectedCategory.set(cat);
    });

    if (this.authService.isLoggedIn()) {
      this.loadFavouriteIds();
    }

    this.heroTimer = window.setInterval(() => this.changeHeroImage(1), 4500);
  }

  ngOnDestroy() {
    if (this.heroTimer) {
      window.clearInterval(this.heroTimer);
    }
  }

  changeHeroImage(direction: number) {
    const count = this.heroImages().length;
    if (count < 2) return;
    this.heroIndex.update(current => (current + direction + count) % count);
  }

  loadAllProducts() {
    this.loading.set(true);
    this.productService.getAll().subscribe({
      next: (data) => { this.allProducts.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadHeroImages() {
    this.heroImageService.getAll().subscribe({
      next: (images) => this.adminHeroImages.set(images.map(i => i.imageUrl))
    });
  }

  groupProducts(data: Product[]): { category: string; categoryEn: string | null; items: Product[] }[] {
    const map = new Map<string, { categoryEn: string | null; items: Product[] }>();
    data.forEach(p => {
      const cats: CategoryRef[] = p.categories.length ? p.categories : [{ id: '', name: this.i18n.t('admin.noCategory'), nameEn: null }];
      cats.forEach(cat => {
        if (!map.has(cat.name)) map.set(cat.name, { categoryEn: cat.nameEn, items: [] });
        map.get(cat.name)!.items.push(p);
      });
    });
    return Array.from(map.entries()).map(([category, { categoryEn, items }]) => ({ category, categoryEn, items }));
  }

  groupLabel(group: { category: string; categoryEn: string | null }): string {
    return this.i18n.pick(group.category, group.categoryEn);
  }

  categoryNames(categories: CategoryRef[]): string {
    return categories.map(c => this.i18n.pick(c.name, c.nameEn)).join(', ');
  }

  loadFavouriteIds() {
    this.favouritesService.getFavourites().subscribe({
      next: (favs) => this.favouriteIds.set(new Set(favs.map(f => f.productId)))
    });
  }

  isFavourite(productId: string) { return this.favouriteIds().has(productId); }

  toggleFavourite(product: Product, event: Event) {
    event.stopPropagation();
    if (!this.authService.isLoggedIn()) return;
    this.favouritesService.toggle(product.id).subscribe({
      next: (res) => {
        const ids = new Set(this.favouriteIds());
        if (res.added) ids.add(product.id);
        else ids.delete(product.id);
        this.favouriteIds.set(ids);
      }
    });
  }

  addToCart(product: Product, event: Event) {
    event.stopPropagation();
    if (!this.authService.isLoggedIn()) return;
    this.cartService.addToCart(product.id, 1).subscribe({
      next: () => {
        const ids = new Set(this.addedToCart());
        ids.add(product.id);
        this.addedToCart.set(ids);
        setTimeout(() => {
          const ids2 = new Set(this.addedToCart());
          ids2.delete(product.id);
          this.addedToCart.set(ids2);
        }, 2000);
      }
    });
  }

  goToProduct(product: Product) { this.router.navigate(['/product', product.id, product.slug]); }

  scrollSlider(dir: number) {
    const el = document.querySelector('.slider-track') as HTMLElement;
    if (el) el.scrollBy({ left: dir * 320, behavior: 'smooth' });
  }
}