import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ContactService } from '../../services/contact/contact';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact.html',
  styleUrl: './contact.css'
})
export class Contact {
  name = '';
  email = '';
  message = '';
  website = ''; // honeypot — невидимо поле, истински хора не го попълват

  submitting = signal(false);
  sent = signal(false);
  error = signal('');

  readonly contactEmail = 'memoryatelier25@gmail.com';

  constructor(private contactService: ContactService, public i18n: I18nService, private router: Router, seo: SeoService) {
    seo.update({
      title: 'Контакти | Memory Atelier',
      description: 'Свържете се с Memory Atelier — пишете ни въпроси, поръчки по индивидуален проект или обратна връзка.',
      path: '/contact'
    });
  }

  goHome() { this.router.navigate(['/home']); }

  submit() {
    this.error.set('');

    if (!this.name.trim() || !this.email.trim() || !this.message.trim()) {
      this.error.set(this.i18n.t('contact.errRequired'));
      return;
    }

    this.submitting.set(true);
    this.contactService.send(this.name.trim(), this.email.trim(), this.message.trim(), this.website).subscribe({
      next: () => {
        this.submitting.set(false);
        this.sent.set(true);
        this.name = '';
        this.email = '';
        this.message = '';
      },
      error: () => {
        this.submitting.set(false);
        this.error.set(this.i18n.t('contact.errFailed'));
      }
    });
  }
}
