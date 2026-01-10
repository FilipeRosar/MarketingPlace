import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { provideNgxMask } from 'ngx-mask';
import { routes } from './app.routes';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    provideRouter(routes),

    provideHttpClient(
      withInterceptors([authInterceptor]),
      withFetch()

    ),
    provideNgxMask(),
    provideCharts(withDefaultRegisterables())
  ]
};
