import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';

import { AuthResponse, LoginRequest, User } from '../../models/user/user.model';
import { RegisterCustomer, RegisterSeller } from '../../models/register/register.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.loadUserFromStorage();
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  registerCustomer(data: RegisterCustomer): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register/customer`, {
      ...data,
      role: 'Customer'
    }).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  registerSeller(data: RegisterSeller): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register/seller`, {
      ...data,
      role: 'Seller'
    }).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }
  updateCurrentUser(user: User) {
    this.setStorageItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  logout() {
    this.clearStorage();
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return isPlatformBrowser(this.platformId) ? localStorage.getItem('token') : null;
  }

  get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  isLoggedIn(): boolean {
    return !!this.getToken() && !!this.currentUserValue;
  }

  // ====================== PRIVATE HELPERS ======================
  private handleAuthSuccess(response: AuthResponse) {
    this.setStorageItem('token', response.token);
    this.setStorageItem('user', JSON.stringify(response.user));
    this.currentUserSubject.next(response.user);
  }

  private loadUserFromStorage() {
    if (!isPlatformBrowser(this.platformId)) return;

    const token = localStorage.getItem('token');
    const userJson = localStorage.getItem('user');

    if (token && userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserSubject.next(user);
      } catch (e) {
        console.error('Erro ao parsear usuário do localStorage', e);
        this.clearStorage();
      }
    }
  }

  // Métodos seguros para localStorage (nunca quebram no SSR)
  private setStorageItem(key: string, value: string) {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(key, value);
    }
  }

  private clearStorage() {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
  }
}
