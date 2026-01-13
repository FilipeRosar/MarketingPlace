import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth/auth.service';
import { environment } from './../../../environments/environment';

declare global {
  interface Window {
    onTurnstileSuccess: (token: string) => void;
    turnstile?: {
      render: (
        container: HTMLElement,
        options: {
          sitekey: string;
          callback: (token: string) => void;
          'error-callback'?: () => void;
          'expired-callback'?: () => void;
        }
      ) => string;
      reset: (widgetId?: string) => void;
    };
  }
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent implements AfterViewInit, OnDestroy {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  @ViewChild('turnstileContainer', { static: true }) turnstileContainer!: ElementRef<HTMLDivElement>;

  siteKey = environment.turnstileSiteKey;

  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  captchaError = '';
  turnstileToken: string | null = null;
  private turnstileWidgetId: string | null = null;
  private turnstilePollId: number | null = null;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });

    window.onTurnstileSuccess = (token: string) => {
      this.turnstileToken = token;
      this.captchaError = '';
    };
  }

  ngAfterViewInit(): void {
    this.renderTurnstile();
  }

  ngOnDestroy(): void {
    if (this.turnstilePollId !== null) {
      window.clearInterval(this.turnstilePollId);
      this.turnstilePollId = null;
    }
  }

  onSubmit(): void {
    if (!this.turnstileToken) {
      this.captchaError = 'Confirme o captcha para continuar.';
    }
    if (this.loginForm.invalid || !this.turnstileToken) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.captchaError = '';

    const credentials = {
      ...this.loginForm.value,
      turnstileToken: this.turnstileToken
    };

    this.authService.login(credentials).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading = false;

        if (err.status === 401) {
          this.errorMessage = 'E-mail ou senha invǭlidos.';
        } else if (err.status === 429) {
          this.errorMessage = 'Muitas tentativas. Aguarde um pouco.';
        } else {
          this.errorMessage = 'Erro ao conectar. Tente novamente mais tarde.';
        }
      }
    });
  }

  private renderTurnstile(): void {
    if (!this.siteKey) {
      this.captchaError = 'Captcha indisponível: site key não configurada.';
      return;
    }

    const tryRender = () => {
      if (!window.turnstile || this.turnstileWidgetId) {
        return;
      }

      this.turnstileWidgetId = window.turnstile.render(this.turnstileContainer.nativeElement, {
        sitekey: this.siteKey,
        callback: (token: string) => {
          this.turnstileToken = token;
          this.captchaError = '';
        },
        'error-callback': () => {
          this.turnstileToken = null;
          this.captchaError = 'Captcha indisponível. Atualize a página e tente novamente.';
        },
        'expired-callback': () => {
          this.turnstileToken = null;
          this.captchaError = 'Captcha expirado. Confirme novamente.';
        }
      });
    };

    tryRender();
    if (!this.turnstileWidgetId) {
      let attempts = 0;
      this.turnstilePollId = window.setInterval(() => {
        attempts += 1;
        tryRender();
        if (this.turnstileWidgetId || attempts >= 50) {
          window.clearInterval(this.turnstilePollId!);
          this.turnstilePollId = null;
          if (!this.turnstileWidgetId) {
            this.captchaError = 'Captcha indisponível. Verifique o carregamento do Turnstile.';
          }
        }
      }, 100);
    }
  }
}
