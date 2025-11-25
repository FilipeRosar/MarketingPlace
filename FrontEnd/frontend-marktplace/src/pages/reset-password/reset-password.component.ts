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
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/auth`;

  resetForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  token: string | null = null;
  resetSuccessful = false;

  constructor() {
    this.resetForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit() {
    // Captura o token da URL
    this.route.queryParams.subscribe(params => {
      this.token = params['token'];
      if (!this.token) {
        this.errorMessage = 'Token de redefinição inválido ou ausente.';
        this.resetForm.disable();
      }
    });
  }

  // Validador Customizado: Verifica se as senhas são iguais
  passwordMatchValidator(group: FormGroup) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { mismatch: true };
  }

  onSubmit() {
    if (this.resetForm.invalid || !this.token) return;

    this.isLoading = true;
    this.errorMessage = '';

    const payload = {
      token: this.token,
      newPassword: this.resetForm.value.password
    };

    // TODO: Chamar endpoint do Backend: [POST] /api/auth/reset-password
    this.http.post(`${this.apiUrl}/reset-password`, payload).subscribe({
      next: () => {
        this.isLoading = false;
        this.resetSuccessful = true;
        this.resetForm.disable();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Falha na redefinição. Tente novamente ou solicite um novo link.';
      }
    });
  }
}
