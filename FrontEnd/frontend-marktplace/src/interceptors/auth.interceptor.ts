import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (req.url.includes('localhost:7113') || req.url.includes('/api/')) {
    const token = authService.getToken();
    if (token) {
      console.log('Token adicionado:', token.substring(0, 20) + '...');
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(authReq);
    } else {
      console.warn('Token não encontrado no localStorage');
    }
  }

  return next(req);
};
