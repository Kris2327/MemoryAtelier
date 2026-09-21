import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './services/interceptors/auth-interceptor';
import { provideClientHydration, withEventReplay, withHttpTransferCacheOptions } from '@angular/platform-browser';
import { SKIP_TRANSFER_CACHE } from './services/http-context-tokens';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' }),
    ),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideClientHydration(
      withEventReplay(),
      withHttpTransferCacheOptions({ filter: req => !req.context.get(SKIP_TRANSFER_CACHE) }),
    ),
  ],
};
