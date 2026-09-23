import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { PasswordField } from '../../shared/password-field/password-field';

@Component({
  selector: 'app-sign-in',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PasswordField],
  templateUrl: './sign-in.html',
  styleUrl: './sign-in.css'
})
export class SignIn {
  email = '';
  password = '';
  error = signal('');
  loading = signal(false);
  sessionExpired = signal(false);

  constructor(private authService: AuthService, private router: Router, public i18n: I18nService, seo: SeoService) {
    seo.update({ title: 'Вход | Memory Atelier', description: 'Влезте в своя акаунт в Memory Atelier.', path: '/sign-in', noindex: true });

    // Еднократно "изяждаме" флага от AuthService, за да не се показва пак при следваща визита.
    if (this.authService.sessionExpired()) {
      this.sessionExpired.set(true);
      this.authService.sessionExpired.set(false);
    }
  }

  goHome() { this.router.navigate(['/home']); }

  submit() {
    this.error.set('');
    this.loading.set(true);
    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/home']),
      error: () => {
        this.error.set(this.i18n.t('signin.error'));
        this.loading.set(false);
      }
    });
  }
}