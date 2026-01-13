// src/app/app.component.ts
import { Component, inject, signal, OnInit, OnDestroy, PLATFORM_ID } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';

import { HeaderComponent } from '../components/header/header.component';
import { FooterComponent } from '../components/footer/footer.component';
import { AuthService } from '../services/auth/auth.service';
import { ChatService } from '../services/chat/chat.service';
import { NotificationService } from '../services/notification/notification.service';
import { ToastNotificationComponent } from "../components/notification/toast-notification.component";

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent, ToastNotificationComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class AppComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private chatService = inject(ChatService);
  private notificationService = inject(NotificationService);
  private platformId = inject(PLATFORM_ID);

  isDark = signal(false);
  private lastChatNotificationCount = 0;

  ngOnInit(): void {
    this.setupDarkMode();
    this.setupChatConnection();
    this.setupChatNotifications();
    this.preloadCriticalAssets();
  }

  ngOnDestroy(): void {
    this.chatService.stopConnection();
  }

  // DARK MODE AUTOMÁTICO + PERSISTENTE
  private setupDarkMode(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const saved = localStorage.getItem('trama-theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (saved === 'dark' || (!saved && prefersDark)) {
      this.isDark.set(true);
    }

    // Aplica a classe imediatamente
    this.applyDarkMode();

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      if (!localStorage.getItem('trama-theme')) {
        this.isDark.set(e.matches);
        this.applyDarkMode();
      }
    });
  }

  private applyDarkMode(): void {
    if (isPlatformBrowser(this.platformId)) {
      const shouldBeDark = this.isDark();
      document.documentElement.classList.toggle('dark-mode', shouldBeDark);
      localStorage.setItem('trama-theme', shouldBeDark ? 'dark' : 'light');
    }
  }

  // Toggle manual (chame do header)
  toggleDarkMode(): void {this.isDark.update(v => !v);    this.applyDarkMode();  }

  // CHAT: só inicia se tiver usuário logado
  private setupChatConnection(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.authService.currentUser$.subscribe(user => {
      if (user && this.authService.getToken()) {
        this.chatService.startConnection(this.authService.getToken()!);
      } else {
        this.chatService.stopConnection();
      }
    });
  }

  private setupChatNotifications(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.chatService.notifications$.subscribe(notifs => {
      if (notifs.length <= this.lastChatNotificationCount) return;

      const currentUser = this.authService.currentUserValue;
      if (!currentUser || currentUser.role !== 'Seller') {
        this.lastChatNotificationCount = notifs.length;
        return;
      }

      const newOnes = notifs.slice(this.lastChatNotificationCount);
      newOnes.forEach(n => {
        this.notificationService.info(n.message, n.title || 'Chat');
      });
      this.lastChatNotificationCount = notifs.length;
    });
  }

  // PRELOAD: hero + fontes + pattern (LCP < 1s garantido)
  private preloadCriticalAssets(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    // Hero principal
    const hero = new Image();
    hero.src = 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1350&q=80';

    // Pattern artesanal
    const pattern = new Image();
    pattern.src = '/assets/patterns/artesanato-pattern.svg';

    // Preconnect Unsplash e Google Fonts
    const links = [
      'https://images.unsplash.com',
      'https://fonts.googleapis.com',
      'https://fonts.gstatic.com'
    ];

    links.forEach(href => {
      const link = document.createElement('link');
      link.rel = 'preconnect';
      link.href = href;
      if (href.includes('gstatic')) link.crossOrigin = 'anonymous';
      document.head.appendChild(link);
    });
  }
}
