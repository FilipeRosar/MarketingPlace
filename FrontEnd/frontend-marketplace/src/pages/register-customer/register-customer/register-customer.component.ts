import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NgxMaskDirective } from 'ngx-mask';
import { AuthService } from '../../../services/auth/auth.service';
import { CustomValidators } from '../../../app/utils/validators';
import { LocationService, IbgeState, IbgeCity } from '../../../services/locations/location.service';

@Component({
  selector: 'app-register-customer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, NgxMaskDirective],
  templateUrl: './register-customer.html',
  styleUrl: './register-customer.css'
})
export class RegisterCustomerComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private http = inject(HttpClient);
  private locationService = inject(LocationService);

  registerForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  states: IbgeState[] = [];
  cities: IbgeCity[] = [];
  isLoadingCities = false;

  constructor() {
    this.registerForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      birthDate: ['', [Validators.required]],
      cpf: ['', [Validators.required, CustomValidators.cpfValidator]],
      phone: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],

      address: this.fb.group({
        zipCode: ['', [Validators.required, Validators.minLength(8)]],
        street: ['', Validators.required],
        number: ['', Validators.required],
        city: [{ value: '', disabled: true }, Validators.required],
        state: ['', Validators.required],
        country: ['Brasil']
      })
    }, {
      validators: CustomValidators.matchPasswords('password', 'confirmPassword')
    });
  }

  ngOnInit() {
    this.locationService.getStates().subscribe({
      next: (data) => this.states = data,
      error: () => console.error('Erro ao carregar estados')
    });

    this.registerForm.get('address.state')?.valueChanges.subscribe(uf => {
      if (uf) {
        this.loadCities(uf);
      } else {
        this.cities = [];
        this.registerForm.get('address.city')?.disable();
        this.registerForm.get('address.city')?.setValue('');
      }
    });
  }

  loadCities(uf: string) {
    this.isLoadingCities = true;
    this.registerForm.get('address.city')?.disable();

    this.locationService.getCitiesByState(uf).subscribe({
      next: (data) => {
        this.cities = data;
        this.isLoadingCities = false;
        this.registerForm.get('address.city')?.enable();
      },
      error: () => {
        this.isLoadingCities = false;
        console.error('Erro ao carregar cidades');
      }
    });
  }

  onZipCodeBlur() {
    const zipCodeControl = this.registerForm.get('address.zipCode');
    const zipCode = zipCodeControl?.value?.replace(/\D/g, '');

    if (zipCode && zipCode.length === 8) {
      zipCodeControl?.setErrors(null);

      this.http.get<any>(`https://viacep.com.br/ws/${zipCode}/json/`).subscribe({
        next: (data) => {
          if (!data.erro) {
            this.registerForm.get('address')?.patchValue({
              street: data.logradouro,
              state: data.uf,
              country: 'Brasil'
            });

            setTimeout(() => {
              this.registerForm.get('address.city')?.setValue(data.localidade);
            }, 600);

          } else {
            zipCodeControl?.setErrors({ invalidCep: true });
          }
        },
        error: () => {
          zipCodeControl?.setErrors({ invalidCep: true });
        }
      });
    }
  }

  onSubmit() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const rawValue = this.registerForm.getRawValue();
    const { confirmPassword, ...formData } = rawValue;

    const cleanData = {
      ...formData,
      cpf: formData.cpf.replace(/\D/g, ''),
      phone: formData.phone.replace(/\D/g, ''),
      address: {
        ...formData.address,
        zipCode: formData.address.zipCode.replace(/\D/g, '')
      }
    };

    this.authService.registerCustomer(cleanData).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 400) {
           this.errorMessage = err.error.message || 'Dados inválidos.';
        } else if (err.status === 409) {
           this.errorMessage = 'Este E-mail ou CPF já está cadastrado.';
        } else {
           this.errorMessage = 'Erro ao criar conta. Tente novamente.';
        }
      }
    });
  }
}
