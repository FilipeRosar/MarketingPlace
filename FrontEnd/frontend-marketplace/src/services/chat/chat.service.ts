import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { BehaviorSubject } from 'rxjs';

export interface ChatMessage {
  user: string;
  message: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private hubConnection: signalR.HubConnection | undefined;
  public messages$ = new BehaviorSubject<ChatMessage[]>([]);
  public notifications$ = new BehaviorSubject<any[]>([]);

  constructor() {}

 public startConnection(token: string) {
  this.hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${environment.apiUrl.replace('/api', '')}/chatHub`, {
      accessTokenFactory: () => token,
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

    this.hubConnection
      .start()
      .then(() => console.log('Chat Connection started'))
      .catch(err => console.log('Error while starting connection: ' + err));

    this.hubConnection.on('ReceiveMessage', (user, message) => {
      const newMsg = { user, message, timestamp: new Date() };
      this.messages$.next([...this.messages$.value, newMsg]);
    });

    // Ouve notificações
    this.hubConnection.on('ReceiveNotification', (title, message) => {
      console.log('Nova Notificação:', title, message);
      // Aqui você pode usar um Toastr ou adicionar a uma lista de notificações
      const newNotif = { title, message, read: false };
      this.notifications$.next([...this.notifications$.value, newNotif]);
    });
  }

  public sendMessage(user: string, message: string) {
    this.hubConnection?.invoke('SendMessage', user, message)
      .catch(err => console.error(err));
  }

  public stopConnection() {
    this.hubConnection?.stop();
  }
}
