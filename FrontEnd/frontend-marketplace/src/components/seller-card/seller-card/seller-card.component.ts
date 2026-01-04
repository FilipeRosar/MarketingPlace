import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-seller-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div [routerLink]="['/sellers', seller.id]"
         class="bg-white rounded-xl shadow-sm hover:shadow-md transition-all duration-300 cursor-pointer border border-stone-100 overflow-hidden group flex items-center p-4 gap-4 h-full">

      <!-- Avatar -->
      <div class="flex-shrink-0">
        <div class="h-16 w-16 rounded-full bg-stone-100 border-2 border-white shadow-sm overflow-hidden flex items-center justify-center text-xl font-bold text-stone-400 group-hover:border-primary transition-colors">
          @if (seller.profileImageUrl) {
            <img [src]="seller.profileImageUrl" class="w-full h-full object-cover">
          } @else {
            {{ seller.name.charAt(0).toUpperCase() }}
          }
        </div>
      </div>

      <!-- Info -->
      <div class="flex-1 min-w-0">
        <h3 class="text-base font-bold text-gray-900 truncate group-hover:text-primary transition-colors">
          {{ seller.name }}
        </h3>
        @if (seller.bio) {
          <p class="text-xs text-gray-500 line-clamp-1 mt-0.5">{{ seller.bio }}</p>
        }
        <p class="text-xs text-stone-400 mt-1 flex items-center">
           <svg class="w-3 h-3 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/></svg>
           {{ seller.address?.city || 'Brasil' }}
        </p>
      </div>

      <!-- Seta -->
      <div class="text-stone-300 group-hover:text-primary transition-colors">
        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </div>
    </div>
  `,
  styles: []
})
export class SellerCardComponent {
  @Input() seller: any; 
}
