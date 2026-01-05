import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  console.log('Interceptor rodando para:', req.url);
  const auth = inject(AuthService);
  const token = auth.getToken();

  console.log('Token encontrado:', token ? 'SIM' : 'NÃO');

  const publicPaths = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/forgot-password'];
  if (publicPaths.some(p => req.url.includes(p))) {
    console.log('Rota pública, sem token');
    return next(req);
  }

  if (!token) {
    console.log('Sem token, continuando sem Authorization');
    return next(req);
  }

  console.log('Adicionando Authorization header');
  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authReq);
};
