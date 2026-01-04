import { Injectable, inject } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { Router } from '@angular/router';

export interface SeoConfig {
  title: string;
  description: string;
  image?: string;
  slug?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  private titleService = inject(Title);
  private metaService = inject(Meta);
  private router = inject(Router);

  // Configuração padrão (Fallback)
  private defaultTitle = 'Trama - Marketplace de Artesanato Brasileiro';
  private defaultDesc = 'Conecte-se com artesãos de todo o Brasil. Compre peças únicas, feitas à mão com alma e história.';
  private defaultImage = 'assets/images/trama-share-image.jpg'; // Crie essa imagem depois
  private siteUrl = 'https://filiperosar.github.io/MarketingPlace'; // Sua URL real

  constructor() {}

  updateSeoData(config: SeoConfig) {
    const title = config.title ? `${config.title} | Trama` : this.defaultTitle;
    const description = config.description || this.defaultDesc;
    const image = config.image || this.defaultImage;
    const url = this.siteUrl + (config.slug || this.router.url);

    this.titleService.setTitle(title);

    this.metaService.updateTag({ name: 'description', content: description });

    this.metaService.updateTag({ property: 'og:title', content: title });
    this.metaService.updateTag({ property: 'og:description', content: description });
    this.metaService.updateTag({ property: 'og:image', content: image });
    this.metaService.updateTag({ property: 'og:url', content: url });
    this.metaService.updateTag({ property: 'og:type', content: 'website' });

    this.metaService.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.metaService.updateTag({ name: 'twitter:title', content: title });
    this.metaService.updateTag({ name: 'twitter:description', content: description });
    this.metaService.updateTag({ name: 'twitter:image', content: image });
  }

  resetSeoData() {
    this.updateSeoData({
      title: '',
      description: ''
    });
  }
}
