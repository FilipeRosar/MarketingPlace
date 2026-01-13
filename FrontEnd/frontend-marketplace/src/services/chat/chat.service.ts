import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { BehaviorSubject, Observable, map } from 'rxjs';

export interface ChatMessage {
  user: string;
  message: string;
  timestamp: Date;
}

export interface ChatThread {
  customerId: string;
  customerName: string;
  customerImageUrl?: string;
  lastMessage: string;
  lastMessageAt: string;
}

export interface ChatCustomerThread {
  sellerId: string;
  sellerUserId: string;
  sellerName: string;
  sellerImageUrl?: string;
  lastMessage: string;
  lastMessageAt: string;
}

export interface ContactRequestThread {
  customerId: string;
  customerName: string;
  customerImageUrl?: string;
  lastMessage: string;
  lastMessageAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = `${environment.apiUrl}/chat`;
  private hubConnection: signalR.HubConnection | undefined;
  public messages$ = new BehaviorSubject<ChatMessage[]>([]);
  public privateMessages$ = new BehaviorSubject<ChatMessage[]>([]);
  public notifications$ = new BehaviorSubject<any[]>([]);

  constructor(private http: HttpClient) {}

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

    this.hubConnection.on('ReceivePrivateMessage', (user, message) => {
      const newMsg = { user, message, timestamp: new Date() };
      this.privateMessages$.next([...this.privateMessages$.value, newMsg]);
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

  public sendPrivateMessage(userId: string, message: string) {
    this.hubConnection?.invoke('SendPrivateMessage', userId, message)
      .catch(err => console.error(err));
  }

  public sendPrivateMessageHttp(recipientUserId: string, message: string): Observable<ChatMessage> {
    return this.http.post<any>(`${this.apiUrl}/messages`, { recipientUserId, message }).pipe(
      map(m => ({
        user: m.senderUserId,
        message: m.message,
        timestamp: new Date(m.createdAt)
      }))
    );
  }

  public getThreads(): Observable<ChatThread[]> {
    return this.http.get<ChatThread[]>(`${this.apiUrl}/threads`);
  }

  public getContactRequestThreads(): Observable<ContactRequestThread[]> {
    return this.http.get<ContactRequestThread[]>(`${this.apiUrl}/threads/contact-requests`);
  }

  public createContactRequest(sellerUserId: string, message: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/contact-requests`, {
      sellerUserId,
      message
    });
  }

  public getCustomerThreads(): Observable<ChatCustomerThread[]> {
    return this.http.get<ChatCustomerThread[]>(`${this.apiUrl}/threads/customer`);
  }

  public getMessages(sellerId: string, customerId: string): Observable<ChatMessage[]> {
    return this.http.get<any[]>(`${this.apiUrl}/messages`, { params: { sellerId, customerId } }).pipe(
      map(messages => messages.map(m => ({
        user: m.senderUserId,
        message: m.message,
        timestamp: new Date(m.createdAt)
      })))
    );
  }

  public stopConnection() {
    this.hubConnection?.stop();
  }
}
