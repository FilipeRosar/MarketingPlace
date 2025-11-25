import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth/auth.service';
import { UserService } from '../../../services/user/user.service';
import { User } from '../../../models/user/user.model';
import { HttpClient } from '@angular/common/http';
import { debounceTime, filter, switchMap } from 'rxjs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private http = inject(HttpClient);

  currentUser: User | null = null;
  profileForm: FormGroup;
  isEditing = false;
  isLoading = false;
  isUploadingPhoto = false;

  constructor() {
    this.profileForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: [{ value: '', disabled: true }],
      phone: ['', [Validators.required, Validators.minLength(10)]],
      cpf: [{ value: '', disabled: true }],

      // Validadores de Endereço
      address: this.fb.group({
        zipCode: ['', [Validators.required, Validators.minLength(8)]],
        street: ['', Validators.required],
        number: ['', Validators.required],
        city: ['', Validators.required],
        state: ['', Validators.required],
        country: ['Brasil', Validators.required]
      })
    });
  }

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUser = user;
        this.patchForm(user);
      } else {
        this.router.navigate(['/login']);
      }
    });

    // 4. Lógica de Autocompletar CEP
    this.setupCepAutofill();
  }

  // Novo método para buscar o CEP
  setupCepAutofill() {
    const cepControl = this.profileForm.get('address.zipCode');

    cepControl?.valueChanges.pipe(
      debounceTime(400), // Espera 400ms após a digitação
      filter(cep => cep && cep.length === 8 && !this.isUploadingPhoto), // Filtra para ter 8 dígitos e não estar ocupado
      switchMap(cep => this.http.get(`https://viacep.com.br/ws/${cep}/json/`)) // Chama ViaCEP
    ).subscribe((data: any) => {
      if (data && !data.erro) {
        const addressGroup = this.profileForm.get('address') as FormGroup;
        addressGroup.patchValue({
          street: data.logradouro,
          city: data.localidade,
          state: data.uf
        }, { emitEvent: false });
      } else if (data?.erro) {
        const addressGroup = this.profileForm.get('address') as FormGroup;
        addressGroup.patchValue({
          street: '',
          city: '',
          state: '',
        }, { emitEvent: false });
      }
    });
  }

  patchForm(user: any) {
    this.profileForm.patchValue({
      name: user.name,
      email: user.email,
      phone: user.phone || '',
      cpf: user.cpf || '',
      address: user.address || {}
    });
  }

  onPhotoSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.isUploadingPhoto = true;
      this.userService.uploadProfilePhoto(file).subscribe({
        next: (res) => {
          if (this.currentUser) {
            this.currentUser = { ...this.currentUser, profileImageUrl: res.imageUrl };
            this.authService.updateCurrentUser(this.currentUser);
          }
          this.isUploadingPhoto = false;
          alert('Foto de perfil atualizada com sucesso!');
        },
        error: (err) => {
          console.error('Erro no upload', err);
          this.isUploadingPhoto = false;
          alert('Erro ao enviar foto.');
        }
      });
    }
  }

  toggleEdit() {
    this.isEditing = !this.isEditing;
    if (this.isEditing) {
      this.profileForm.enable();
      this.profileForm.get('email')?.disable();
      this.profileForm.get('cpf')?.disable();
    } else {
      this.profileForm.disable();
      if (this.currentUser) this.patchForm(this.currentUser);
    }
  }

  onSubmit() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    setTimeout(() => {
      this.isLoading = false;
      this.isEditing = false;
      this.profileForm.disable();
      alert('Perfil atualizado com sucesso! (Simulação Front)');
    }, 1000);
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
