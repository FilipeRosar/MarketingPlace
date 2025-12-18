import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';

interface Notification {
  id: number;
  title: string;
  message: string;
  icon: string;
  type: string;
}

@Injectable({
  providedIn: 'root'
})
export class HubService {
  private hubConnection!: signalR.HubConnection;
  private authService = inject(AuthService);

  public notifications = signal<Notification[]>([]);

  public startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/notificationhub`, {
        accessTokenFactory: () => this.authService.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('SignalR Connected - Notificações em tempo real ativas!'))
      .catch(err => console.error('Erro SignalR:', err));

    this.hubConnection.on('ReceiveNotification', (data: any) => {
      const id = Date.now();
      this.notifications.update(notifs => [...notifs, { id, ...data }]);

      // Remove após 8 segundos
      setTimeout(() => {
        this.notifications.update(notifs => notifs.filter(n => n.id !== id));
      }, 8000);
    });
  }

  public stopConnection() {
    this.hubConnection.stop();
  }
}
