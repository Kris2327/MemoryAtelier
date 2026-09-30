import { Component } from '@angular/core';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-links',
  standalone: true,
  templateUrl: './links.html',
  styleUrl: './links.css'
})
export class Links {
  constructor(seo: SeoService) {
    // Страница само за QR кода на базара — не се линква никъде в сайта, затова noindex.
    seo.update({
      title: 'Memory Atelier — Линкове',
      description: 'Открийте Memory Atelier онлайн.',
      path: '/links',
      noindex: true
    });
  }
}
