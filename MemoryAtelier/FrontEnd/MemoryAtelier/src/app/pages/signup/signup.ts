import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { PasswordField } from '../../shared/password-field/password-field';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PasswordField],
  templateUrl: './signup.html',
  styleUrl: './signup.css'
})
export class Signup {
  name = '';
  email = '';
  password = '';
  confirmPassword = '';
  phone = '';
  birthDate = '';
  error = signal('');
  loading = signal(false);

  constructor(private authService: AuthService, private router: Router, public i18n: I18nService, seo: SeoService) {
    seo.update({ title: 'Регистрация | Memory Atelier', description: 'Създайте акаунт в Memory Atelier.', path: '/signup', noindex: true });
  }

  goHome() { this.router.navigate(['/home']); }

  submit() {
    this.error.set('');

    if (!this.name || !this.email || !this.password || !this.phone || !this.birthDate) {
      this.error.set(this.i18n.t('signup.errAllRequired'));
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error.set(this.i18n.t('signup.errPasswordsMismatch'));
      return;
    }

    if (this.password.length < 6) {
      this.error.set(this.i18n.t('signup.errPasswordTooShort'));
      return;
    }

    this.loading.set(true);
    this.authService.register({
      name: this.name,
      email: this.email,
      password: this.password,
      phoneNumber: this.phone,
      birthDate: this.birthDate ? new Date(this.birthDate) : undefined
    }).subscribe({
      next: () => this.router.navigate(['/home']),
      error: (err) => {
        this.error.set(err.error?.message || this.i18n.t('signup.errGeneric'));
        this.loading.set(false);
      }
    });
  }
}