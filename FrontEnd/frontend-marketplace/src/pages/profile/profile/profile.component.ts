import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth/auth.service';
import { UserService } from '../../../services/user/user.service';
import { User } from '../../../models/user/user.model';
import { HttpClient } from '@angular/common/http';
import { debounceTime, filter, switchMap } from 'rxjs';
import { NotificationService } from '../../../services/notification/notification.service';
import { DeleteAccountDialogComponent } from '../../../components/delete-account-dialog/delete-account-dialog.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DeleteAccountDialogComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private http = inject(HttpClient);
  private notification = inject(NotificationService);
  currentUser: User | null = null;
  profileForm: FormGroup;
  isEditing = false;
  isLoading = false;
  isUploadingPhoto = false;
  showDeleteDialog = signal(false);

  constructor() {
    this.profileForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: [{ value: '', disabled: true }],
      phone: ['', [Validators.required, Validators.minLength(10)]],
      cpf: [{ value: '', disabled: true }],
      address: this.fb.group({
        zipCode: ['', [Validators.required, Validators.minLength(8)]],
        street: ['', Validators.required],
        number: ['', Validators.required],
        district: ['', Validators.required],
        complement: [''],
        city: ['', Validators.required],
        state: ['', Validators.required],
        country: ['Brasil', Validators.required]
      })
    });

    // Inicia com tudo desabilitado
    this.profileForm.disable();
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

    this.setupCepAutofill();
  }

  private patchForm(user: any) {
    this.profileForm.patchValue({
      name: user.name || '',
      email: user.email || '',
      phone: user.phone || '',
      cpf: user.cpf || '',
      address: {
        zipCode: user.address?.zipCode || '',
        street: user.address?.street || '',
        number: user.address?.number || '',
        district: user.address?.district || '',
        complement: user.address?.complement || '',
        city: user.address?.city || '',
        state: user.address?.state || '',
        country: user.address?.country || 'Brasil'
      }
    });
  }

  setupCepAutofill() {
    const cepControl = this.profileForm.get('address.zipCode');

    cepControl?.valueChanges
      .pipe(
        debounceTime(600),
        filter(() => this.isEditing), // Só busca se estiver editando
        filter(cep => cep && this.normalizeZip(cep).length === 8),
        switchMap(cep => this.http.get(`https://viacep.com.br/ws/${this.normalizeZip(cep)}/json/`))
      )
      .subscribe((data: any) => {
        if (data && !data.erro) {
          const addressGroup = this.profileForm.get('address') as FormGroup;
          addressGroup.patchValue({
            street: data.logradouro,
            district: data.bairro,
            city: data.localidade,
            state: data.uf
          });
        }
      });
  }

  toggleEdit() {
    this.isEditing = !this.isEditing;

    if (this.isEditing) {
      // Habilita todos os campos editáveis
      this.profileForm.get('name')?.enable();
      this.profileForm.get('phone')?.enable();
      this.profileForm.get('address')?.enable();

      // Mantém email e CPF desabilitados
      this.profileForm.get('email')?.disable();
      this.profileForm.get('cpf')?.disable();
    } else {
      // Volta ao estado bloqueado
      this.profileForm.disable();

      // Recarrega os dados originais se cancelou
      if (this.currentUser) {
        this.patchForm(this.currentUser);
      }
    }
  }

  onSubmit() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    const formValue = this.profileForm.getRawValue();

    const updateData = {
      name: formValue.name.trim(),
      phone: this.normalizePhone(formValue.phone),
      address: {
        zipCode: this.normalizeZip(formValue.address.zipCode),
        street: formValue.address.street.trim(),
        number: formValue.address.number.trim(),
        district: formValue.address.district.trim(),
        complement: formValue.address.complement?.trim() || null,
        city: formValue.address.city.trim(),
        state: formValue.address.state.trim(),
        country: formValue.address.country
      }
    };

    this.userService.updateProfile(updateData).subscribe({
      next: () => {
        this.isLoading = false;
        this.isEditing = false;
        this.profileForm.disable();

        if (this.currentUser) {
          const updatedUser = {
            ...this.currentUser,
            name: updateData.name,
            phone: updateData.phone,
            address: updateData.address
          };
          this.authService.updateCurrentUser(updatedUser);
          this.currentUser = updatedUser;
        }

        this.notification.success('Perfil atualizado com sucesso!');
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Erro ao atualizar perfil:', err);
        this.notification.error('Não foi possível salvar as alterações. Tente novamente.');
      }
    });
  }

  onPhotoSelected(event: any) {
    const file = event.target.files[0];
    if (!file) return;

    this.isUploadingPhoto = true;

    this.userService.uploadProfilePhoto(file).subscribe({
      next: (res) => {
        if (this.currentUser) {
          const updatedUser = { ...this.currentUser, profileImageUrl: res.imageUrl };
          this.currentUser = updatedUser;
          this.authService.updateCurrentUser(updatedUser);
        }
        this.isUploadingPhoto = false;
        this.notification.success('Foto atualizada com sucesso!');
      },
      error: () => {
        this.isUploadingPhoto = false;
        this.notification.error('Erro ao enviar foto.');
      }
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  // Máscaras apenas quando está editando
  onZipInput() {
    if (!this.isEditing) return;
    const control = this.profileForm.get('address.zipCode');
    if (!control) return;
    const digits = this.normalizeZip(control.value);
    const masked = digits.length >= 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
    control.setValue(masked, { emitEvent: false });
  }

  onPhoneInput() {
    if (!this.isEditing) return;
    const control = this.profileForm.get('phone');
    if (!control) return;
    const digits = this.normalizePhone(control.value);
    let masked = '';
    if (digits.length <= 10) {
      masked = digits.replace(/^(\d{2})(\d{4})(\d{4})$/, '($1) $2-$3');
    } else {
      masked = digits.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3');
    }
    control.setValue(masked, { emitEvent: false });
  }

  private normalizeZip(value: string): string {
    return (value || '').replace(/\D/g, '').slice(0, 8);
  }

  private normalizePhone(value: string): string {
    return (value || '').replace(/\D/g, '').slice(0, 11);
  }

  openDeleteDialog() {
    this.showDeleteDialog.set(true);
  }

  closeDeleteDialog() {
    this.showDeleteDialog.set(false);
  }
}
