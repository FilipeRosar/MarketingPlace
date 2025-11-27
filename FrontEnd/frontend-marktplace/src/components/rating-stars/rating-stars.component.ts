import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-rating-stars',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex items-center">
      @for (star of stars; track $index) {
        <svg class="w-5 h-5" [ngClass]="star.class" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
          <path d="M9.049 2.927c.3-.921 1.631-.921 1.932 0l1.243 3.824a1 1 0 00.95.691h4.026c.969 0 1.371 1.24.588 1.81l-3.266 2.372a1 1 0 00-.364 1.118l1.243 3.824c.3.921-.755 1.688-1.54 1.118l-3.266-2.372a1 1 0 00-1.176 0l-3.266 2.372c-.784.57-1.84-.197-1.54-1.118l1.243-3.824a1 1 0 00-.364-1.118L2.096 9.252c-.783-.57-.381-1.81.588-1.81h4.026a1 1 0 00.95-.691l1.243-3.824z" />
          @if (star.type === 'half') {
            <defs><linearGradient id="half"><stop offset="50%" stop-color="currentColor"/><stop offset="50%" stop-color="#D1D5DB"/></linearGradient></defs>
            <rect [attr.fill]="'url(#half)'" x="0" y="0" width="100%" height="100%"/>
          }
        </svg>
      }
      @if (totalRatings > 0) {
        <span class="ml-2 text-sm font-medium text-gray-500">
          {{ rating | number:'1.1-1' }} ({{ totalRatings }})
        </span>
      }
    </div>
  `
})
export class RatingStarsComponent implements OnInit {
  @Input() rating: number = 0;
  @Input() totalRatings: number = 0;
  stars: { class: string; type: 'full' | 'half' | 'empty' }[] = [];

  ngOnInit() {
    this.calculateStars();
  }

  calculateStars() {
    const fullStars = Math.floor(this.rating);
    const hasHalfStar = this.rating % 1 >= 0.25 && this.rating % 1 < 0.75;
    const remainingStars = 5 - fullStars - (hasHalfStar ? 1 : 0);

    this.stars = [];

    for (let i = 0; i < fullStars; i++) {
      this.stars.push({ class: 'text-yellow-400', type: 'full' });
    }

    if (hasHalfStar) {
      this.stars.push({ class: 'text-yellow-400', type: 'half' });
    }

    for (let i = 0; i < remainingStars; i++) {
      this.stars.push({ class: 'text-gray-300', type: 'empty' });
    }
  }
}
