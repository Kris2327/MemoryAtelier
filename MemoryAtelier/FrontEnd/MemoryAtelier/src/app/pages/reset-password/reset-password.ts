import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { PasswordField } from '../../shared/password-field/password-field';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PasswordField],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css'
})
export class ResetPassword {
  newPassword = '';
  token = '';
  loading = signal(false);
  success = signal(false);
  error = signal<string | null>(null);

  constructor(
    private authService: AuthService,
    private router: Router,
    route: ActivatedRoute,
    public i18n: I18nService,
    seo: SeoService
  ) {
    seo.update({ title: 'Нова парола | Memory Atelier', description: 'Задайте нова парола за профила си в Memory Atelier.', path: '/reset-password', noindex: true });
    this.token = route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) this.error.set(this.i18n.t('resetPassword.errInvalid'));
  }

  goHome() { this.router.navigate(['/home']); }

  submit() {
    if (!this.token || this.newPassword.length < 6 || this.loading()) return;
    this.loading.set(true);
    this.error.set(null);
    this.authService.resetPassword({ token: this.token, newPassword: this.newPassword }).subscribe({
      next: () => { this.loading.set(false); this.success.set(true); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? this.i18n.t('resetPassword.errInvalid'));
      }
    });
  }
}
