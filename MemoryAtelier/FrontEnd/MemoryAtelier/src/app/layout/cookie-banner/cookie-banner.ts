import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CookieConsentService } from '../../services/cookie-consent/cookie-consent';
import { I18nService } from '../../services/i18n/i18n';

@Component({
  selector: 'app-cookie-banner',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './cookie-banner.html',
  styleUrl: './cookie-banner.css'
})
export class CookieBanner {
  readonly consent = inject(CookieConsentService);
  readonly i18n = inject(I18nService);
}
