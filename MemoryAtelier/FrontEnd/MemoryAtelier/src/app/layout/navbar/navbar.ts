import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { CartService } from '../../services/cartService/cartService';
import { FavouritesService } from '../../services/favourites/favourites';
import { CategoryService } from '../../services/category/category';
import { ProductService } from '../../services/product/product';
import { Category, Product } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';

const MAX_SEARCH_SUGGESTIONS = 6;

export interface NavChild {
  label: string;
  value: string;
}

export interface NavCategory {
  label: string;
  value: string;
  subcategories?: { label: string; value: string; children?: NavChild[] }[];
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit {
  megaMenuOpen = signal(false);
  hoveredRoot = signal<string | null>(null);
  activeSubcategory = signal<string | null>(null);
  mobileMenuOpen = signal(false);
  activeMobileSection = signal<string | null>(null);
  mobileSearchOpen = signal(false);

  searchTerm = signal('');
  searchSuggestionsOpen = signal(false);
  private allProducts = signal<Product[] | null>(null);

  navItems = computed<NavCategory[]>(() =>
    this.categoryService.categories()
      .filter(category => !category.isHidden)
      .map(category => this.toNavCategory(category))
  );

  currentRoot = computed<NavCategory | undefined>(() =>
    this.navItems().find(item => item.label === this.hoveredRoot())
  );

  searchSuggestions = computed<Product[]>(() => {
    const q = this.searchTerm().trim().toLowerCase();
    const products = this.allProducts();
    if (!q || !products) return [];
    return products
      .filter(p => this.i18n.pick(p.name, p.nameEn).toLowerCase().includes(q))
      .slice(0, MAX_SEARCH_SUGGESTIONS);
  });

  constructor(
    public authService: AuthService,
    private router: Router,
    public cartService: CartService,            // ← public
    public favouritesService: FavouritesService,
    private categoryService: CategoryService,
    private productService: ProductService,
    public i18n: I18nService
  ) {
    // докато си на страницата с резултати от търсене, полето показва какво си търсил;
    // при напускане ѝ (или при навигация без ?q) се изчиства, за да не остава стар текст в друга секция
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        const q = this.router.parseUrl(event.urlAfterRedirects).queryParams['q'] || '';
        this.searchTerm.set(q);
        this.searchSuggestionsOpen.set(false);
      }
    });
  }

  ngOnInit() {
    this.categoryService.refresh();

    if (this.authService.isLoggedIn()) {
      this.cartService.getCount().subscribe();
      this.favouritesService.getCount().subscribe();
    }
  }

  onSearchInput(value: string) {
    this.searchTerm.set(value);
    this.searchSuggestionsOpen.set(value.trim().length > 0);
    if (value.trim().length > 0 && this.allProducts() === null) {
      this.productService.getAll().subscribe(products => this.allProducts.set(products));
    }
  }

  selectSuggestion(product: Product) {
    this.searchTerm.set('');
    this.searchSuggestionsOpen.set(false);
    this.closeMobileSearch();
    this.router.navigate(['/product', product.id, product.slug]);
  }

  closeSearchSuggestions() {
    this.searchSuggestionsOpen.set(false);
  }

  // забавяне на затварянето, за да мине click/mousedown на предложение преди blur да скрие dropdown-а
  closeSearchSuggestionsDelayed() {
    setTimeout(() => this.searchSuggestionsOpen.set(false), 150);
  }

  private toNavCategory(category: Category): NavCategory {
    const visibleChildren = category.children.filter(sub => !sub.isHidden);
    return {
      label: this.i18n.pick(category.name, category.nameEn),
      value: category.id,
      subcategories: visibleChildren.length
        ? visibleChildren.map(sub => {
            const visibleGrandchildren = sub.children.filter(child => !child.isHidden);
            return {
              label: this.i18n.pick(sub.name, sub.nameEn),
              value: sub.id,
              children: visibleGrandchildren.length
                ? visibleGrandchildren.map(child => ({ label: this.i18n.pick(child.name, child.nameEn), value: child.id }))
                : undefined
            };
          })
        : undefined
    };
  }

  openMegaMenu() {
    this.megaMenuOpen.set(true);
    if (!this.hoveredRoot() && this.navItems().length) {
      this.hoveredRoot.set(this.navItems()[0].label);
    }
  }
  closeMegaMenu() {
    this.megaMenuOpen.set(false);
    this.hoveredRoot.set(null);
    this.activeSubcategory.set(null);
  }
  openSubcategory(label: string) { this.activeSubcategory.set(label); }
  clearSubcategory() { this.activeSubcategory.set(null); }
  toggleMobileMenu() { this.mobileMenuOpen.update(v => !v); }
  closeMobileMenu() { this.mobileMenuOpen.set(false); }
  toggleMobileSearch() { this.mobileSearchOpen.update(v => !v); }
  closeMobileSearch() {
    this.mobileSearchOpen.set(false);
    this.searchSuggestionsOpen.set(false);
  }
  // потребителят затваря панела без да търси (backdrop клик) — изчиства недовършения текст
  cancelMobileSearch() {
    this.closeMobileSearch();
    this.searchTerm.set('');
  }

  filterBy(category: string) {
    this.router.navigate(['/home'], { queryParams: { category } });
    this.closeMobileMenu();
  }

  submitSearch(term: string) {
    const q = term.trim();
    if (!q) return;
    this.router.navigate(['/home'], { queryParams: { q } });
    this.searchSuggestionsOpen.set(false);
    this.closeMobileMenu();
    this.closeMegaMenu();
    this.closeMobileSearch();
  }

  logout() { this.authService.logout(); }
  goToAdmin() { this.router.navigate(['/admin']); this.closeMobileMenu(); }
  goToLogin() { this.router.navigate(['/sign-in']); this.closeMobileMenu(); }
  goToProfile() { this.router.navigate(['/profile']); this.closeMobileMenu(); }
  goHome() { this.router.navigate(['/home']); this.closeMobileMenu(); }
  goToCart() { this.router.navigate(['/cart']); this.closeMobileMenu(); }
  goToFavourites() { this.router.navigate(['/favourites']); this.closeMobileMenu(); }
  toggleMobileSection(label: string) {
    this.activeMobileSection.update(v => v === label ? null : label);
  }
}