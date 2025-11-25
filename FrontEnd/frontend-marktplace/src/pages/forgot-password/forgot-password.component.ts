import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/auth`;

  forgotForm: FormGroup;
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  constructor() {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit() {
    if (this.forgotForm.invalid) return;

    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    const email = this.forgotForm.value.email;

    // TODO: Chamar endpoint do Backend: [POST] /api/auth/forgot-password
    this.http.post(`${this.apiUrl}/forgot-password`, { email }).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Se as informações estiverem corretas, você receberá um link de redefinição por e-mail.';
        this.forgotForm.disable();
      },
      error: (err) => {
        this.isLoading = false;
        this.successMessage = 'Se as informações estiverem corretas, você receberá um link de redefinição por e-mail.';
      }
    });
  }
}
