import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-register-seller',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register-seller.html',
  styleUrl: './register-seller.css'
})
export class RegisterSellerComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor() {
    this.registerForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      phone: ['', [Validators.required]],
      cpf: [''],
      cnpj: [''],

      address: this.fb.group({
        zipCode: ['', Validators.required],
        street: ['', Validators.required],
        number: ['', Validators.required],
        city: ['', Validators.required],
        state: ['', Validators.required],
        country: ['Brasil']
      })
    });
  }

  onSubmit() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const data = this.registerForm.value;

    this.authService.registerSeller(data).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 400 || err.status === 409) {
          this.errorMessage = err.error.message || 'Dados inválidos. Verifique os campos.';
        } else {
          this.errorMessage = 'Ocorreu um erro ao criar a loja. Tente novamente.';
        }
      }
    });
  }
}
