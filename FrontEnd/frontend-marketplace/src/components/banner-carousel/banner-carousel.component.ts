import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BannerService, Banner } from '../../services/banner/banner.service';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-banner-carousel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './banner-carousel.component.html',
  styleUrl: './banner-carousel.component.css'
})
export class BannerCarouselComponent implements OnInit, OnDestroy {
  private bannerService = inject(BannerService);
  
  banners = signal<Banner[]>([]);
  currentIndex = signal(0);
  isLoading = signal(false);
  private autoPlaySubscription?: Subscription;
  private readonly AUTO_PLAY_INTERVAL = 5000; // 5 seconds

  ngOnInit() {
    this.loadBanners();
  }

  ngOnDestroy() {
    if (this.autoPlaySubscription) {
      this.autoPlaySubscription.unsubscribe();
    }
  }

  private loadBanners() {
    this.isLoading.set(true);
    this.bannerService.getActiveBanners().subscribe({
      next: (data) => {
        const sorted = data.sort((a, b) => a.displayOrder - b.displayOrder);
        this.banners.set(sorted);
        this.isLoading.set(false);
        
        // Start auto-play if there are banners
        if (sorted.length > 1) {
          this.startAutoPlay();
        }
      },
      error: (err) => {
        console.error('Error loading banners:', err);
        this.isLoading.set(false);
      }
    });
  }

  private startAutoPlay() {
    this.autoPlaySubscription = interval(this.AUTO_PLAY_INTERVAL).subscribe(() => {
      const nextIndex = (this.currentIndex() + 1) % this.banners().length;
      this.currentIndex.set(nextIndex);
    });
  }

  goToSlide(index: number) {
    if (this.autoPlaySubscription) {
      this.autoPlaySubscription.unsubscribe();
    }
    this.currentIndex.set(index);
    if (this.banners().length > 1) {
      this.startAutoPlay();
    }
  }

  previousSlide() {
    const prevIndex = this.currentIndex() === 0 ? this.banners().length - 1 : this.currentIndex() - 1;
    this.goToSlide(prevIndex);
  }

  nextSlide() {
    const nextIndex = (this.currentIndex() + 1) % this.banners().length;
    this.goToSlide(nextIndex);
  }
}
