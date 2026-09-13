import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-sitemap',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './sitemap.html',
  styleUrl: './sitemap.css'
})
export class Sitemap {
  constructor(public i18n: I18nService, private router: Router, seo: SeoService) {
    seo.update({
      title: 'Карта на сайта | Memory Atelier',
      description: 'Всички страници на Memory Atelier на едно място.',
      path: '/sitemap'
    });
  }

  goHome() { this.router.navigate(['/home']); }
}
