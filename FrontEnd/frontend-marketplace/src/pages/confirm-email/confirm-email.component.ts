import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.css'
})
export class ConfirmEmailComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private apiUrl = `${environment.apiUrl}/auth`;

  isLoading = true;
  isSuccess = false;
  message = '';
  email = '';

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const token = params['token'];
      const email = params['email'];

      if (!token || !email) {
        this.isLoading = false;
        this.message = 'Link inválido ou incompleto.';
        return;
      }

      this.email = email;
      this.confirmEmail(email, token);
    });
  }

  private confirmEmail(email: string, token: string) {
    this.http.post(`${this.apiUrl}/confirm-email`, { email, token }).subscribe({
      next: () => {
        this.isLoading = false;
        this.isSuccess = true;
        this.message = 'Email confirmado com sucesso! Você será redirecionado para o login.';
        setTimeout(() => this.router.navigate(['/login']), 3000);
      },
      error: (err) => {
        this.isLoading = false;
        this.isSuccess = false;
        this.message = err.error.message || 'Erro ao confirmar email. O link pode ter expirado.';
      }
    });
  }

  resendEmail() {
    this.isLoading = true;
    this.http.post(`${this.apiUrl}/resend-confirmation-email`, { email: this.email }).subscribe({
      next: () => {
        this.isLoading = false;
        this.isSuccess = true;
        this.message = 'Email de confirmação reenviado com sucesso!';
      },
      error: (err) => {
        this.isLoading = false;
        this.isSuccess = false;
        this.message = err.error.message || 'Erro ao reenviar email.';
      }
    });
  }
}
