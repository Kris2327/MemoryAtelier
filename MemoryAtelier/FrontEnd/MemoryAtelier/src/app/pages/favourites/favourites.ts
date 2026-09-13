import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { Router } from '@angular/router';
import { FavouritesService } from '../../services/favourites/favourites';
import { FavouriteItem } from '../../services/auth/auth-types';
import { CartService } from '../../services/cartService/cartService';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-favourites',
  standalone: true,
  imports: [CommonModule, NgFor, NgIf],
  templateUrl: './favourites.html',
  styleUrl: './favourites.css'
})
export class Favourites implements OnInit {
  items = signal<FavouriteItem[]>([]);
  loading = signal(false);

  constructor(
    private favouritesService: FavouritesService,
    private cartService: CartService,
    private router: Router,
    public i18n: I18nService,
    private seo: SeoService
  ) {}

  ngOnInit() {
    this.seo.update({ title: 'Любими | Memory Atelier', description: 'Вашите любими продукти в Memory Atelier.', path: '/favourites', noindex: true });
    this.loadFavourites();
  }

  loadFavourites() {
    this.loading.set(true);
    this.favouritesService.getFavourites().subscribe({
      next: (data) => { this.items.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  remove(item: FavouriteItem) {
    this.favouritesService.toggle(item.productId).subscribe({
      next: () => this.items.update(items => items.filter(i => i.id !== item.id))
    });
  }

  addToCart(item: FavouriteItem) {
    this.cartService.addToCart(item.productId, 1).subscribe();
  }

  goToHome() { this.router.navigate(['/home']); }
}