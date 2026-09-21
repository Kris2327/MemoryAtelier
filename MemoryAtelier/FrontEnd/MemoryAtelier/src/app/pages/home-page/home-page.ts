import { Component, OnInit, OnDestroy, PLATFORM_ID, inject, signal, computed, effect } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../services/product/product';
import { CartService } from '../../services/cartService/cartService';
import { FavouritesService } from '../../services/favourites/favourites';
import { AuthService } from '../../services/auth/auth';
import { HeroImageService } from '../../services/hero-image/hero-image';
import { CategoryService } from '../../services/category/category';
import { Category, CategoryRef, Product } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { ThumbUrlPipe } from '../../shared/thumb-url';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ThumbUrlPipe],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css'
})
export class HomePage implements OnInit, OnDestroy {
  allProducts = signal<Product[]>([]);
  // малка резервна извадка за hero fallback-а — тегли се и на SSR, за разлика от allProducts (вж. loadAllProducts)
  heroFallbackProducts = signal<Product[]>([]);
  selectedCategory = signal('All');
  // старт true, за да не рендира SSR празна решетка, докато чака allProducts (зареждан само в браузъра)
  loading = signal(true);
  favouriteIds = signal<Set<string>>(new Set());
  addedToCart = signal<Set<string>>(new Set());
  heroIndex = signal(0);
  // hero снимките се зареждат при поискване (не всички наведнъж) — тук пазим кои индекси вече са пуснати за сваляне
  loadedHeroIndices = signal<Set<number>>(new Set([0]));
  sidebarOpen = signal(false);
  private heroTimer?: number;
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  // снимки за hero слайдъра — избрани от администратора през админ панела
  adminHeroImages = signal<string[]>([]);

  // докато администраторът не е избрал снимки, покажи по една на продукт като резервен вариант
  // (от heroFallbackProducts, не allProducts — трябва да е налично още на SSR за LCP-то, вж. loadAllProducts)
  heroImages = computed(() => {
    const admin = this.adminHeroImages();
    if (admin.length > 0) return admin;
    return this.heroFallbackProducts()
      .filter(p => p.images.length > 0)
      .slice(0, 6)
      .map(p => p.images[0].imageUrl);
  });

  // главните секции — за бързите линкове под hero заглавието
  rootCategories = computed(() => this.categoryService.categories());

  // id-та на избраната категория И всичките й подкатегории (null = "All")
  private selectedCategoryIds = computed<Set<string> | null>(() => {
    const catId = this.selectedCategory();
    if (catId === 'All') return null;

    const root = this.findCategory(this.categoryService.categories(), catId);
    const ids = new Set<string>();
    if (root) this.collectCategoryIds(root, ids);
    else ids.add(catId);
    return ids;
  });

  // продуктите от избраната категория (и подкатегориите й), групирани — само за изгледа "Всички"
  filteredGrouped = computed(() => {
    const ids = this.selectedCategoryIds();
    const all = this.allProducts();
    const filtered = ids === null ? all : all.filter(p => p.categories.some(c => ids.has(c.id)));
    return this.groupProducts(filtered, ids);
  });

  // плосък списък продукти (без дублиране по категория) — за изгледа с избрана секция
  filteredProducts = computed(() => {
    const ids = this.selectedCategoryIds();
    if (ids === null) return [];
    return this.allProducts().filter(p => p.categories.some(c => ids.has(c.id)));
  });

  // продуктите от ДРУГИТЕ категории за слайдъра
  otherProducts = computed(() => {
    const ids = this.selectedCategoryIds();
    const all = this.allProducts();
    if (ids === null) return [];
    return all.filter(p => !p.categories.some(c => ids.has(c.id)));
  });

  isFiltered = computed(() => this.selectedCategory() !== 'All');

  // главната секция (корена), в чието дърво попада избраната категория — за страничното меню
  selectedRootCategory = computed<Category | null>(() => {
    const catId = this.selectedCategory();
    if (catId === 'All') return null;
    return this.findRootCategory(this.categoryService.categories(), catId);
  });

  // пътят от главната секция до конкретно избраната (под)категория — за breadcrumb и заглавие
  selectedCategoryPath = computed<Category[]>(() => {
    const catId = this.selectedCategory();
    if (catId === 'All') return [];
    const path: Category[] = [];
    this.buildCategoryPath(this.categoryService.categories(), catId, [], path);
    return path;
  });

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private favouritesService: FavouritesService,
    public authService: AuthService,
    private heroImageService: HeroImageService,
    private categoryService: CategoryService,
    private route: ActivatedRoute,
    private router: Router,
    public i18n: I18nService,
    private seo: SeoService
  ) {
    // предзарежда активния слайд и следващия, за да има плавен crossfade,
    // без да тегли наведнъж снимките на всичките (до 6) слайда при първо зареждане
    effect(() => {
      const count = this.heroImages().length;
      if (count === 0) return;
      const current = this.heroIndex();
      const next = (current + 1) % count;
      this.loadedHeroIndices.update(set =>
        set.has(current) && set.has(next) ? set : new Set(set).add(current).add(next)
      );
    });
  }

  isHeroSlideLoaded(i: number): boolean {
    return this.loadedHeroIndices().has(i);
  }

  ngOnInit() {
    this.seo.update({
      title: 'Memory Atelier — Ръчно изработени съкровища',
      description: 'Ръчно изработени декорации, свещи, кашпи и подаръци с любов към детайла. Разгледайте колекцията на Memory Atelier.',
      path: '/home'
    });

    // пълният каталог е тежък (~250 KB, десетки продукти) и не е нужен за LCP-то (hero-то) —
    // тегли се само в браузъра, след hydration, за да остане SSR страницата лека на мобилни връзки.
    // Малката резервна извадка за hero fallback-а обаче трябва да е налична още на SSR.
    this.loadHeroFallbackProducts();
    if (this.isBrowser) {
      this.loadAllProducts();
    }
    this.loadHeroImages();
    this.categoryService.refresh();

    this.route.queryParams.subscribe(params => {
      const cat = params['category'] || 'All';
      this.selectedCategory.set(cat);
    });

    if (this.authService.isLoggedIn()) {
      this.loadFavouriteIds();
    }

    if (this.isBrowser) {
      this.heroTimer = window.setInterval(() => this.changeHeroImage(1), 4500);
    }
  }

  ngOnDestroy() {
    if (this.isBrowser && this.heroTimer) {
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

  loadHeroFallbackProducts() {
    this.productService.getAll(undefined, 6).subscribe({
      next: (data) => this.heroFallbackProducts.set(data)
    });
  }

  loadHeroImages() {
    this.heroImageService.getAll().subscribe({
      next: (images) => this.adminHeroImages.set(images.map(i => i.imageUrl))
    });
  }

  groupProducts(data: Product[], restrictToIds: Set<string> | null = null): { category: string; categoryEn: string | null; items: Product[] }[] {
    const map = new Map<string, { categoryEn: string | null; items: Product[] }>();
    data.forEach(p => {
      const relevant = restrictToIds ? p.categories.filter(c => restrictToIds.has(c.id)) : p.categories;
      const cats: CategoryRef[] = relevant.length ? relevant : [{ id: '', name: this.i18n.t('admin.noCategory'), nameEn: null, isHidden: false }];
      cats.forEach(cat => {
        if (!map.has(cat.name)) map.set(cat.name, { categoryEn: cat.nameEn, items: [] });
        map.get(cat.name)!.items.push(p);
      });
    });
    return Array.from(map.entries()).map(([category, { categoryEn, items }]) => ({ category, categoryEn, items }));
  }

  private findCategory(list: Category[], id: string): Category | null {
    for (const cat of list) {
      if (cat.id === id) return cat;
      const found = this.findCategory(cat.children, id);
      if (found) return found;
    }
    return null;
  }

  private collectCategoryIds(cat: Category, out: Set<string>): void {
    out.add(cat.id);
    cat.children.forEach(child => this.collectCategoryIds(child, out));
  }

  private findRootCategory(roots: Category[], id: string): Category | null {
    return roots.find(root => this.containsCategoryId(root, id)) ?? null;
  }

  private containsCategoryId(cat: Category, id: string): boolean {
    if (cat.id === id) return true;
    return cat.children.some(child => this.containsCategoryId(child, id));
  }

  private buildCategoryPath(nodes: Category[], targetId: string, trail: Category[], out: Category[]): boolean {
    for (const node of nodes) {
      const nextTrail = [...trail, node];
      if (node.id === targetId) {
        out.push(...nextTrail);
        return true;
      }
      if (this.buildCategoryPath(node.children, targetId, nextTrail, out)) return true;
    }
    return false;
  }

  goToCategory(id: string): void {
    this.router.navigate(['/home'], { queryParams: { category: id } });
    this.sidebarOpen.set(false);
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

  scrollToSections(): void {
    document.querySelector('.sections, .category-layout')?.scrollIntoView({ behavior: 'smooth' });
  }

  // -thumb companion файлът може да липсва за снимки, качени преди thumbnail генерирането
  // (или преди reprocess-legacy backfill-а) — тогава падаме обратно на пълната снимка.
  onThumbError(event: Event, original: string): void {
    const el = event.target as HTMLImageElement;
    if (el.src !== original) el.src = original;
  }

  isNew(product: Product): boolean {
    const ageInDays = (Date.now() - new Date(product.createdAt).getTime()) / 86_400_000;
    return ageInDays <= 14;
  }
}