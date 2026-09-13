import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { CartService } from '../../services/cartService/cartService';
import { FavouritesService } from '../../services/favourites/favourites';
import { CategoryService } from '../../services/category/category';
import { Category } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';

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
  activeDropdown = signal<string | null>(null);
  activeSubcategory = signal<string | null>(null);
  mobileMenuOpen = signal(false);
  activeMobileSection = signal<string | null>(null);

  navItems = computed<NavCategory[]>(() =>
    this.categoryService.categories().map(category => this.toNavCategory(category))
  );

  constructor(
    public authService: AuthService,
    private router: Router,
    public cartService: CartService,            // ← public
    public favouritesService: FavouritesService,
    private categoryService: CategoryService,
    public i18n: I18nService
  ) {}

  ngOnInit() {
    this.categoryService.refresh();

    if (this.authService.isLoggedIn()) {
      this.cartService.getCount().subscribe();
      this.favouritesService.getCount().subscribe();
    }
  }

  private toNavCategory(category: Category): NavCategory {
    return {
      label: this.i18n.pick(category.name, category.nameEn),
      value: category.id,
      subcategories: category.children.length
        ? category.children.map(sub => ({
            label: this.i18n.pick(sub.name, sub.nameEn),
            value: sub.id,
            children: sub.children.length
              ? sub.children.map(child => ({ label: this.i18n.pick(child.name, child.nameEn), value: child.id }))
              : undefined
          }))
        : undefined
    };
  }

  openDropdown(label: string) { this.activeDropdown.set(label); }
  closeDropdown() { this.activeDropdown.set(null); this.activeSubcategory.set(null); }
  openSubcategory(label: string) { this.activeSubcategory.set(label); }
  clearSubcategory() { this.activeSubcategory.set(null); }
  toggleMobileMenu() { this.mobileMenuOpen.update(v => !v); }
  closeMobileMenu() { this.mobileMenuOpen.set(false); }

  filterBy(category: string) {
    this.router.navigate(['/home'], { queryParams: { category } });
    this.closeMobileMenu();
  }

  logout() { this.authService.logout(); }
  goToAdmin() { this.router.navigate(['/admin']); this.closeMobileMenu(); }
  goToLogin() { this.router.navigate(['/sign-in']); this.closeMobileMenu(); }
  goHome() { this.router.navigate(['/home']); this.closeMobileMenu(); }
  goToCart() { this.router.navigate(['/cart']); this.closeMobileMenu(); }
  goToFavourites() { this.router.navigate(['/favourites']); this.closeMobileMenu(); }
  toggleMobileSection(label: string) {
    this.activeMobileSection.update(v => v === label ? null : label);
  }
}