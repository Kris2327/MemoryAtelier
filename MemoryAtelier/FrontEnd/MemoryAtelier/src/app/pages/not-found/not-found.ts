import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.css'
})
export class NotFound {
  constructor(public i18n: I18nService, seo: SeoService) {
    seo.update({
      title: 'Страницата не е намерена | Memory Atelier',
      description: 'Страницата, която търсите, не съществува.',
      path: '/404'
    });
  }
}
