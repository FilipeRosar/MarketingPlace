import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch, HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideNgxMask } from 'ngx-mask';
import { routes } from './app.routes';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { provideAnimations } from '@angular/platform-browser/animations';
import { EventTrackingService } from '../services/analytics/event-tracking.service';
import { PageTrackingService } from '../services/analytics/page-tracking.service';
import { AnalyticsHttpInterceptor } from '../services/analytics/analytics-http.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAnimations(),
    provideRouter(routes),

    provideHttpClient(
      withInterceptors([authInterceptor]),
      withFetch()
    ),
    provideNgxMask(),
    provideCharts(withDefaultRegisterables()),
    
    // Analytics Services
    EventTrackingService,
    PageTrackingService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AnalyticsHttpInterceptor,
      multi: true
    }
  ]
};
