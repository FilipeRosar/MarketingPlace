import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error' | 'info' | 'warning';

export interface Notification {
  id: number;
  type: NotificationType;
  title?: string;
  message: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notifications = signal<Notification[]>([]);
  private idCounter = 0;

  // Métodos rápidos
  success(message: string, title = 'Sucesso!', duration = 4000) {
    this.add({ type: 'success', title, message, duration });
  }

  error(message: string, title = 'Erro', duration = 5000) {
    this.add({ type: 'error', title, message, duration });
  }

  info(message: string, title = 'Informação', duration = 3500) {
    this.add({ type: 'info', title, message, duration });
  }

  warning(message: string, title = 'Atenção', duration = 4500) {
    this.add({ type: 'warning', title, message, duration });
  }

  // Método genérico
  private add(notification: Omit<Notification, 'id'>) {
    const id = ++this.idCounter;
    this.notifications.update(notifs => [...notifs, { ...notification, id }]);

    // Auto-remove após duration
    if (notification.duration && notification.duration > 0) {
      setTimeout(() => this.remove(id), notification.duration);
    }
  }

  remove(id: number) {
    this.notifications.update(notifs => notifs.filter(n => n.id !== id));
  }

  // Expor como signal readonly
  list = this.notifications.asReadonly();
}
