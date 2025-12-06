import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, Notification, NotificationType } from '../../services/notification/notification.service';

@Component({
  selector: 'app-toast-notification',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed bottom-4 right-4 z-50 space-y-3 pointer-events-none">
      @for (notif of notifications(); track notif.id) {
        <div
          (click)="notificationService.remove(notif.id)"
          class="pointer-events-auto max-w-sm w-full bg-white shadow-2xl rounded-2xl border border-gray-100 overflow-hidden animate-in slide-in-from-bottom-5 fade-in duration-300"
          role="alert"
          [ngClass]="getTypeClasses(notif.type)"
        >
          <div class="p-5">
            <div class="flex items-start gap-4">
              <!-- Ícone -->
              <div class="flex-shrink-0 mt-0.5">
                <svg class="w-6 h-6" [ngClass]="getIconClasses(notif.type)" fill="currentColor" viewBox="0 0 20 20">
                  @switch (notif.type) {
                    @case ('success') {
                      <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd"/>
                    }
                    @case ('error') {
                      <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd"/>
                    }
                    @case ('warning') {
                      <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0A6.978 6.978 0 0114 8.25a6.98 6.98 0 01-1.5 4.28c-.673 1.167-2.357 1.167-3.03 0A6.978 6.978 0 017 8.25a6.978 6.978 0 011.485-5.755zM10 15a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
                    }
                    @default {
                      <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                    }
                  }
                </svg>
              </div>

              <div class="flex-1">
                @if (notif.title) {
                  <p class="font-bold text-gray-900">{{ notif.title }}</p>
                }
                <p class="text-sm text-gray-600 mt-1">{{ notif.message }}</p>
              </div>

              <button
                (click)="notificationService.remove(notif.id); $event.stopPropagation()"
                class="text-gray-400 hover:text-gray-600 transition">
                <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414/tutorials:"/>
                </svg>
              </button>
            </div>
          </div>

          <!-- Barra de progresso (opcional) -->
          <div class="h-1 w-full bg-gray-200">
            <div
              class="h-full transition-all duration-300 ease-linear"
              [style.width.%]="100"
              [ngClass]="getProgressClasses(notif.type)"
              [@slideOut]="notif.duration ? 'active' : 'inactive'">
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
})
export class ToastNotificationComponent {
  notificationService = inject(NotificationService);
  notifications = this.notificationService.list;

  getTypeClasses(type: NotificationType): string {
    const base = 'border-l-4';
    switch (type) {
      case 'success': return `${base} border-green-500`;
      case 'error': return `${base} border-red-500`;
      case 'warning': return `${base} border-yellow-500`;
      case 'info': return `${base} border-blue-500`;
      default: return `${base} border-gray-500`;
    }
  }

  getIconClasses(type: NotificationType): string {
    switch (type) {
      case 'success': return 'text-green-500';
      case 'error': return 'text-red-500';
      case 'warning': return 'text-yellow-500';
      case 'info': return 'text-blue-500';
      default: return 'text-gray-500';
    }
  }

  getProgressClasses(type: NotificationType): string {
    switch (type) {
      case 'success': return 'bg-green-500';
      case 'error': return 'bg-red-500';
      case 'warning': return 'bg-yellow-500';
      case 'info': return 'bg-blue-500';
      default: return 'bg-gray-500';
    }
  }
}
