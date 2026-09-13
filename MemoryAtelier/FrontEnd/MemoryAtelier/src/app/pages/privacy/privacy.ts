import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './privacy.html',
  styleUrl: './privacy.css'
})
export class Privacy {
  constructor(public i18n: I18nService, private router: Router, seo: SeoService) {
    seo.update({
      title: 'Политика за поверителност и бисквитки | Memory Atelier',
      description: 'Как Memory Atelier обработва лични данни и локално съхранение (бисквитки/localStorage).',
      path: '/privacy'
    });
  }

  goHome() { this.router.navigate(['/home']); }
}
