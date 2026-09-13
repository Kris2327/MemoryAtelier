import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-sign-in',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './sign-in.html',
  styleUrl: './sign-in.css'
})
export class SignIn {
  email = '';
  password = '';
  error = signal('');
  loading = signal(false);

  constructor(private authService: AuthService, private router: Router, public i18n: I18nService, seo: SeoService) {
    seo.update({ title: 'Вход | Memory Atelier', description: 'Влезте в своя акаунт в Memory Atelier.', path: '/sign-in', noindex: true });
  }

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