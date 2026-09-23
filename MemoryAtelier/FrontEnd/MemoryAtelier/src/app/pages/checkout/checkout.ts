import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CartService } from '../../services/cartService/cartService';
import { AuthService } from '../../services/auth/auth';
import { CartItem } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { environment } from '../../../environments/environment';

interface DeliveryAddress {
  firstName: string;
  lastName: string;
  phone: string;
  city: string;
  address: string;
  postCode: string;
  officeName: string;
  notes: string;
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class Checkout implements OnInit {
  private router = inject(Router);
  private http = inject(HttpClient);
  public cartService = inject(CartService);
  public authService = inject(AuthService);
  public i18n = inject(I18nService);
  private seo = inject(SeoService);

  private readonly API = environment.apiUrl;

  cartItems = signal<CartItem[]>([]);
  loading = signal(true);
  submitting = signal(false);
  orderPlaced = signal(false);
  orderId = signal<string>('');

  paymentMethod = signal<'CashOnDelivery' | 'BankTransfer'>('CashOnDelivery');
  deliveryMethod = signal<'Address' | 'SpeedyOffice'>('Address');

  address: DeliveryAddress = {
    firstName: '',
    lastName: '',
    phone: '',
    city: '',
    address: '',
    postCode: '',
    officeName: '',
    notes: ''
  };

  // доставката е за сметка на купувача и се заплаща директно на куриера — не влиза в тази сума
  subtotal = computed(() => this.cartItems().reduce((s, i) => s + i.total, 0));
  total = computed(() => this.subtotal());

  // ако клиентът стигне до тук без да е избрал "раздели"/"изчакай" за продукт над наличността (напр. чрез директен линк) — блокираме поръчката
  hasUnresolvedStockChoice = computed(() => this.cartItems().some(i => i.quantity > i.stock && !i.fulfillmentChoice));

  errors: Partial<DeliveryAddress> = {};

  ngOnInit() {
    this.seo.update({ title: 'Поръчка | Memory Atelier', description: 'Завършете поръчката си в Memory Atelier.', path: '/checkout', noindex: true });

    // зареди количката
    this.cartService.getCart().subscribe({
      next: (items) => { this.cartItems.set(items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });

    // напълни формата от профила
    if (this.authService.isLoggedIn()) {
      this.prefillFromProfile();
    }
  }

  prefillFromProfile() {
    const fullName = this.authService.name() ?? '';
    const parts = fullName.trim().split(' ');
    this.address.firstName = parts[0] ?? '';
    this.address.lastName = parts.slice(1).join(' ') ?? '';
    this.address.phone = this.authService.phone() ?? '';

    // допълни адреса от запазения профил, ако е наличен (не презаписва, ако потребителят вече е започнал да пише)
    this.authService.getProfile().subscribe({
      next: (profile) => {
        if (!this.address.city) this.address.city = profile.city ?? '';
        if (!this.address.address) this.address.address = profile.address ?? '';
        if (!this.address.postCode) this.address.postCode = profile.postCode ?? '';
      },
      error: () => {}
    });
  }

  validate(): boolean {
    this.errors = {};
    const required = this.i18n.t('checkout.errRequired');
    if (!this.address.firstName.trim()) this.errors.firstName = required;
    if (!this.address.lastName.trim()) this.errors.lastName = required;
    if (!this.address.phone.trim()) this.errors.phone = required;
    else if (!/^[0-9+\s]{7,15}$/.test(this.address.phone.trim())) this.errors.phone = this.i18n.t('checkout.errInvalidPhone');
    if (!this.address.city.trim()) this.errors.city = required;
    if (this.deliveryMethod() === 'Address') {
      if (!this.address.address.trim()) this.errors.address = required;
      if (!this.address.postCode.trim()) this.errors.postCode = required;
    } else {
      if (!this.address.officeName.trim()) this.errors.officeName = required;
    }
    return Object.keys(this.errors).length === 0;
  }

  selectPaymentMethod(method: 'CashOnDelivery' | 'BankTransfer') {
    this.paymentMethod.set(method);
  }

  selectDeliveryMethod(method: 'Address' | 'SpeedyOffice') {
    this.deliveryMethod.set(method);
  }

  placeOrder() {
    if (!this.validate() || this.hasUnresolvedStockChoice()) return;
    this.submitting.set(true);

    // забележка: не превеждаме тук — backend-ът показва името на метода на доставка отделно, на български, във всички имейли
    const deliveryAddress = this.deliveryMethod() === 'Address'
      ? `${this.address.address}, ${this.address.city}, ${this.address.postCode}`
      : `${this.address.officeName}, ${this.address.city}`;

    const payload = {
      deliveryAddress,
      deliveryMethod: this.deliveryMethod(),
      deliveryFirstName: this.address.firstName,
      deliveryLastName: this.address.lastName,
      deliveryPhone: this.address.phone,
      notes: this.address.notes,
      paymentMethod: this.paymentMethod(),
      items: this.cartItems().map(i => ({
        productId: i.productId,
        quantity: i.quantity,
        fulfillmentChoice: i.fulfillmentChoice
      }))
    };

    this.http.post<{ id: string }>(`${this.API}/order`, payload).subscribe({
      next: (res) => {
        this.orderId.set(res.id);
        this.orderPlaced.set(true);
        this.submitting.set(false);
        this.cartService.cartCount.set(0);
      },
      error: () => this.submitting.set(false)
    });
  }

  goHome() { this.router.navigate(['/home']); }
  goCart() { this.router.navigate(['/cart']); }
}