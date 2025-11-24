import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';

import { AuthResponse, LoginRequest, User } from '../../models/user/user.model';
import { RegisterCustomer } from '../../models/register/register.model';
import { RegisterSeller } from '../../models/register/register.model';

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

  // --- LOGIN ---
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        this.handleAuthSuccess(response);
      })
    );
  }

  registerCustomer(data: RegisterCustomer): Observable<AuthResponse> {
    const payload = { ...data, role: 'Customer' };

    // Nota: Certifique-se que no Backend a rota é 'register/customer' ou apenas 'register'
    // No seu último código backend estava como 'register/customer', então vou manter assim:
    return this.http.post<AuthResponse>(`${this.apiUrl}/register/customer`, payload).pipe(
      tap(response => {
        this.handleAuthSuccess(response);
      })
    );
  }

  registerSeller(data: RegisterSeller): Observable<AuthResponse> {
    const payload = { ...data, role: 'Seller' };

    return this.http.post<AuthResponse>(`${this.apiUrl}/register/seller`, payload).pipe(
      tap(response => {
        this.handleAuthSuccess(response);
      })
    );
  }

  logout() {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
    this.currentUserSubject.next(null);
  }


  private handleAuthSuccess(response: AuthResponse) {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));
    }
    this.currentUserSubject.next(response.user);
  }

  getToken(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem('token');
    }
    return null;
  }

  get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  private loadUserFromStorage() {
    if (isPlatformBrowser(this.platformId)) {
      const userJson = localStorage.getItem('user');
      if (userJson) {
        try {
          this.currentUserSubject.next(JSON.parse(userJson));
        } catch (e) {
          console.error('Erro ao ler usuário do storage', e);
          this.logout();
        }
      }
    }
  }
}
