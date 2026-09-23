import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css'
})
export class ForgotPassword {
  email = '';
  loading = signal(false);
  submitted = signal(false);

  constructor(private authService: AuthService, private router: Router, public i18n: I18nService, seo: SeoService) {
    seo.update({ title: 'Забравена парола | Memory Atelier', description: 'Възстановете паролата си в Memory Atelier.', path: '/forgot-password', noindex: true });
  }

  goHome() { this.router.navigate(['/home']); }

  submit() {
    if (!this.email.trim() || this.loading()) return;
    this.loading.set(true);
    this.authService.forgotPassword(this.email.trim()).subscribe({
      next: () => { this.loading.set(false); this.submitted.set(true); },
      error: () => { this.loading.set(false); this.submitted.set(true); } // не разкриваме дали имейлът съществува
    });
  }
}
