import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css'
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute); // Para ler a URL
  private router = inject(Router);
  private apiUrl = `${environment.apiUrl}/auth`;

  resetForm: FormGroup;
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  // Variáveis para guardar o que veio da URL
  tokenFromUrl = '';
  emailFromUrl = '';

  constructor() {
    this.resetForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    });
  }

  ngOnInit() {
    // 1. Capturar token e email da URL (query params)
    // A URL é algo como: /reset-password?token=abc123&email=user@test.com
    this.route.queryParams.subscribe(params => {
      this.tokenFromUrl = params['token'];
      this.emailFromUrl = params['email'];

      if (!this.tokenFromUrl || !this.emailFromUrl) {
        this.errorMessage = 'Link inválido ou incompleto. Solicite uma nova recuperação.';
        this.resetForm.disable();
      }
    });
  }

  onSubmit() {
    if (this.resetForm.invalid) return;

    // Validação simples de senha igual
    if (this.resetForm.value.newPassword !== this.resetForm.value.confirmPassword) {
      this.errorMessage = 'As senhas não coincidem.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    // 2. Montar o objeto DTO exato que o C# espera (ResetPasswordDto)
    const payload = {
      email: this.emailFromUrl,
      token: this.tokenFromUrl,
      newPassword: this.resetForm.value.newPassword
    };

    this.http.post(`${this.apiUrl}/reset-password`, payload).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Senha alterada com sucesso! Redirecionando...';
        this.resetForm.disable();

        // Redireciona para o login após 3 segundos
        setTimeout(() => this.router.navigate(['/login']), 3000);
      },
      error: (err) => {
        this.isLoading = false;
        // Pega a mensagem de erro do Backend (ex: "Token inválido ou expirado")
        this.errorMessage = err.error.message || 'Erro ao redefinir senha.';
      }
    });
  }
}
