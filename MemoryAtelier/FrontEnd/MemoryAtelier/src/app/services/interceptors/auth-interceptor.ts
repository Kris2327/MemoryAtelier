// src/app/core/interceptors/auth.interceptor.ts
import { PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpInterceptorFn } from '@angular/common/http';

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
  return next(req);
};