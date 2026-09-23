import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth/auth';
import { OrdersService } from '../../services/orders/orders';
import { AdminOrder, OrderStatus, UserProfile } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

type Tab = 'data' | 'orders';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  activeTab = signal<Tab>('data');

  profile = signal<UserProfile | null>(null);
  loadingProfile = signal(true);
  savingProfile = signal(false);
  profileSaved = signal(false);

  form = { name: '', phoneNumber: '', city: '', address: '', postCode: '' };

  sendingResetLink = signal(false);
  resetLinkSent = signal(false);

  orders = signal<AdminOrder[]>([]);
  loadingOrders = signal(false);
  ordersLoaded = false;

  constructor(
    private authService: AuthService,
    private ordersService: OrdersService,
    public i18n: I18nService,
    private seo: SeoService
  ) {}

  ngOnInit() {
    this.seo.update({ title: 'Моят профил | Memory Atelier', description: 'Вашият профил в Memory Atelier.', path: '/profile', noindex: true });
    this.loadProfile();
  }

  setTab(tab: Tab) {
    this.activeTab.set(tab);
    if (tab === 'orders' && !this.ordersLoaded) this.loadOrders();
  }

  loadProfile() {
    this.loadingProfile.set(true);
    this.authService.getProfile().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.form = {
          name: data.name,
          phoneNumber: data.phoneNumber ?? '',
          city: data.city ?? '',
          address: data.address ?? '',
          postCode: data.postCode ?? ''
        };
        this.loadingProfile.set(false);
      },
      error: () => this.loadingProfile.set(false)
    });
  }

  saveProfile() {
    if (!this.form.name.trim()) return;
    this.savingProfile.set(true);
    this.profileSaved.set(false);
    this.authService.updateProfile({
      name: this.form.name.trim(),
      phoneNumber: this.form.phoneNumber.trim() || null,
      city: this.form.city.trim() || null,
      address: this.form.address.trim() || null,
      postCode: this.form.postCode.trim() || null
    }).subscribe({
      next: (data) => {
        this.profile.set(data);
        this.savingProfile.set(false);
        this.profileSaved.set(true);
        setTimeout(() => this.profileSaved.set(false), 3000);
      },
      error: () => this.savingProfile.set(false)
    });
  }

  sendResetLink() {
    const email = this.profile()?.email;
    if (!email || this.sendingResetLink()) return;
    this.sendingResetLink.set(true);
    this.resetLinkSent.set(false);
    this.authService.forgotPassword(email).subscribe({
      next: () => { this.sendingResetLink.set(false); this.resetLinkSent.set(true); },
      error: () => { this.sendingResetLink.set(false); this.resetLinkSent.set(true); } // не разкриваме дали е успешно вътрешно
    });
  }

  loadOrders() {
    this.loadingOrders.set(true);
    this.ordersService.getMine().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.loadingOrders.set(false);
        this.ordersLoaded = true;
      },
      error: () => this.loadingOrders.set(false)
    });
  }

  statusLabel(status: OrderStatus): string {
    return this.i18n.t(`admin.orders.status${status}`);
  }
}
