import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { Router } from '@angular/router';
import { CartService } from '../../../services/cartService/cartService';
import { CartItem, FulfillmentChoice } from '../../../services/auth/auth-types';
import { I18nService } from '../../../services/i18n/i18n';
import { SeoService } from '../../../services/seo/seo';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, NgFor, NgIf],
  templateUrl: './cart.html',
  styleUrls: ['./cart.css']
})
export class Cart implements OnInit {
  items = signal<CartItem[]>([]);
  loading = signal(false);

  constructor(private cartService: CartService, private router: Router, public i18n: I18nService, private seo: SeoService) {}

  ngOnInit() {
    this.seo.update({ title: 'Количка | Memory Atelier', description: 'Вашата количка в Memory Atelier.', path: '/cart', noindex: true });
    this.loadCart();
  }

  loadCart() {
    this.loading.set(true);
    this.cartService.getCart().subscribe({
      next: (data) => { this.items.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  updateQuantity(item: CartItem, quantity: number) {
    if (quantity < 1) { this.remove(item); return; }
    this.cartService.updateCart(item.id, quantity).subscribe({
      next: () => this.items.update(items => items.map(i => i.id === item.id ? { ...i, quantity, total: i.price * quantity, fulfillmentChoice: null } : i))
    });
  }

  onQtyInputChange(item: CartItem, event: Event) {
    const raw = (event.target as HTMLInputElement).value;
    const parsed = Math.trunc(Number(raw));
    const quantity = Number.isFinite(parsed) && parsed > 0 ? Math.min(999, parsed) : 1;
    (event.target as HTMLInputElement).value = String(quantity);
    if (quantity !== item.quantity) {
      this.updateQuantity(item, quantity);
    }
  }

  setFulfillmentChoice(item: CartItem, choice: FulfillmentChoice) {
    this.cartService.updateCart(item.id, item.quantity, choice).subscribe({
      next: () => this.items.update(items => items.map(i => i.id === item.id ? { ...i, fulfillmentChoice: choice } : i))
    });
  }

  exceedsStock(item: CartItem): boolean {
    return item.quantity > item.stock;
  }

  splitNowQty(item: CartItem): number {
    return Math.max(0, Math.min(item.quantity, item.stock));
  }

  splitLaterQty(item: CartItem): number {
    return item.quantity - this.splitNowQty(item);
  }

  hasUnresolvedChoice(): boolean {
    return this.items().some(i => this.exceedsStock(i) && !i.fulfillmentChoice);
  }

  remove(item: CartItem) {
    this.cartService.removeFromCart(item.id).subscribe({
      next: () => this.items.update(items => items.filter(i => i.id !== item.id))
    });
  }

  get subtotal() { return this.items().reduce((s, i) => s + i.total, 0); }
  get itemCount() { return this.items().reduce((s, i) => s + i.quantity, 0); }

  goToHome() { this.router.navigate(['/home']); }

  proceedToCheckout() {
    if (this.hasUnresolvedChoice()) return;
    this.router.navigate(['/checkout']);
  }
}