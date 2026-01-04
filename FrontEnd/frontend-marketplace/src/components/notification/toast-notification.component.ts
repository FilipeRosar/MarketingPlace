import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, NotificationType } from '../../services/notification/notification.service';

@Component({
  selector: 'app-toast-notification',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed bottom-6 right-6 z-[100] flex flex-col gap-3 pointer-events-none">
      @for (notif of notifications(); track notif.id) {
        <div
          (click)="notificationService.remove(notif.id)"
          class="pointer-events-auto relative w-full max-w-sm overflow-hidden rounded-xl border shadow-lg transition-all duration-300 hover:scale-[1.02] cursor-pointer animate-slide-in"
          [ngClass]="getContainerClasses(notif.type)"
        >
          <div class="p-4 flex items-start gap-4">

            <div class="flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center"
                 [ngClass]="getIconBgClasses(notif.type)">
              <svg class="w-5 h-5" [ngClass]="getIconColorClasses(notif.type)" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                @switch (notif.type) {
                  @case ('success') {
                    <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                  }
                  @case ('error') {
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                  }
                  @case ('warning') {
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                  }
                  @default {
                    <path stroke-linecap="round" stroke-linejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
                  }
                }
              </svg>
            </div>

            <div class="flex-1 pt-0.5">
              @if (notif.title) {
                <h3 class="text-sm font-bold text-gray-900 leading-none mb-1">{{ notif.title }}</h3>
              }
              <p class="text-sm text-gray-600 leading-snug">{{ notif.message }}</p>
            </div>

            <button
              (click)="notificationService.remove(notif.id); $event.stopPropagation()"
              class="flex-shrink-0 text-gray-400 hover:text-gray-600 transition-colors p-1 rounded-md hover:bg-black/5"
            >
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="absolute bottom-0 left-0 h-1 w-full bg-black/5">
            <div class="h-full w-full origin-left animate-progress"
                 [ngClass]="getProgressBarClasses(notif.type)"
                 [style.animation-duration]="(notif.duration || 5000) + 'ms'">
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    @keyframes slideIn {
      from { opacity: 0; transform: translateY(20px) scale(0.95); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }
    .animate-slide-in {
      animation: slideIn 0.4s cubic-bezier(0.16, 1, 0.3, 1) forwards;
    }
    @keyframes progress {
      from { transform: scaleX(1); }
      to { transform: scaleX(0); }
    }
    .animate-progress {
      animation-name: progress;
      animation-timing-function: linear;
      animation-fill-mode: forwards;
    }
  `]
})
export class ToastNotificationComponent {
  notificationService = inject(NotificationService);
  notifications = this.notificationService.list;

  // 1. Estilos do Container (Fundo sutil e borda)
  getContainerClasses(type: NotificationType): string {
    switch (type) {
      case 'success': return 'bg-emerald-50 border-emerald-100 shadow-emerald-100/50';
      case 'error':   return 'bg-red-50 border-red-100 shadow-red-100/50';
      case 'warning': return 'bg-amber-50 border-amber-100 shadow-amber-100/50';
      case 'info':    return 'bg-blue-50 border-blue-100 shadow-blue-100/50';
      default:        return 'bg-white border-gray-100';
    }
  }

  // 2. Background do Círculo do Ícone
  getIconBgClasses(type: NotificationType): string {
    switch (type) {
      case 'success': return 'bg-emerald-100';
      case 'error':   return 'bg-red-100';
      case 'warning': return 'bg-amber-100';
      case 'info':    return 'bg-blue-100';
      default:        return 'bg-gray-100';
    }
  }

  // 3. Cor do Ícone (SVG)
  getIconColorClasses(type: NotificationType): string {
    switch (type) {
      case 'success': return 'text-emerald-600';
      case 'error':   return 'text-red-600';
      case 'warning': return 'text-amber-600';
      case 'info':    return 'text-blue-600';
      default:        return 'text-gray-600';
    }
  }

  // 4. Cor da Barra de Progresso
  getProgressBarClasses(type: NotificationType): string {
    switch (type) {
      case 'success': return 'bg-emerald-500';
      case 'error':   return 'bg-red-500';
      case 'warning': return 'bg-amber-500';
      case 'info':    return 'bg-blue-500';
      default:        return 'bg-gray-500';
    }
  }
}
