import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';

@Component({
  selector: 'app-terms',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './terms.html',
  styleUrl: './terms.css'
})
export class Terms {
  constructor(public i18n: I18nService, private router: Router, seo: SeoService) {
    seo.update({
      title: 'Общи условия | Memory Atelier',
      description: 'Общи условия за поръчки, плащане, доставка и връщане в Memory Atelier.',
      path: '/terms'
    });
  }

  goHome() { this.router.navigate(['/home']); }
}
