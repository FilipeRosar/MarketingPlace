import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../services/user/user.service';
import { NotificationService } from '../../services/notification/notification.service';
import { AuthService } from '../../services/auth/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-delete-account-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './delete-account-dialog.component.html',
  styleUrl: './delete-account-dialog.component.css'
})
export class DeleteAccountDialogComponent {
  @Output() close = new EventEmitter<void>();

  private userService = inject(UserService);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  password = '';
  isLoading = signal(false);
  showPassword = signal(false);

  onCancel() {
    this.close.emit();
  }

  onConfirmDelete() {
    if (!this.password.trim()) {
      this.notificationService.error('Por favor, digite sua senha');
      return;
    }

    this.isLoading.set(true);

    this.userService.deleteAccount(this.password).subscribe({
      next: () => {
        this.notificationService.success('Conta deletada com sucesso. Redirecionando...');
        
        // Wait a bit for the notification to show, then logout and redirect
        setTimeout(() => {
          this.authService.logout();
          this.router.navigate(['/']);
        }, 2000);
      },
      error: (err) => {
        this.isLoading.set(false);
        console.error(err);
        if (err.status === 400) {
          this.notificationService.error('Senha incorreta. Tente novamente.');
        } else {
          this.notificationService.error('Erro ao deletar conta. Tente novamente mais tarde.');
        }
      }
    });
  }

  togglePasswordVisibility() {
    this.showPassword.set(!this.showPassword());
  }
}
