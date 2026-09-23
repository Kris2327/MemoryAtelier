// src/app/core/interceptors/auth.interceptor.ts
import { PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  const token = isBrowser ? localStorage.getItem('token') : null;

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  if (!token) {
    return next(req);
  }

  // Само за заявки, за които сме пратили токен — 401 тук означава изтекла/невалидна сесия,
  // а не грешен имейл/парола при вход (тази заявка не носи токен и си минава необезпокоявана).
  const authService = inject(AuthService);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.expireSession();
      }
      return throwError(() => error);
    })
  );
};