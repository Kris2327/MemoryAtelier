import { Injectable, PLATFORM_ID, inject, signal, computed } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, ChangePasswordRequest, LoginRequest, RegisterRequest, ResetPasswordRequest, UpdateProfileRequest, UserProfile } from './auth-types';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = `${environment.apiUrl}/auth`;
  // localStorage не съществува по време на SSR — четем/пишем само в браузъра.
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private _token = signal<string | null>(this.isBrowser ? localStorage.getItem('token') : null);
  private _role = signal<string | null>(this.isBrowser ? localStorage.getItem('role') : null);
  private _name = signal<string | null>(this.isBrowser ? localStorage.getItem('name') : null);
  private _phone = signal<string | null>(this.isBrowser ? localStorage.getItem('phone') : null); // <- добави

  readonly token = this._token.asReadonly();
  readonly role = this._role.asReadonly();
  readonly name = this._name.asReadonly();
  readonly phone = this._phone.asReadonly(); // <- добави
  readonly isLoggedIn = computed(() => !!this._token());
  readonly isAdmin = computed(() => this._role() === 'Admin');

  // Вдига се от auth-interceptor-а при 401 от вече логнат потребител (изтекъл/невалиден токен).
  // Sign-in страницата го чете еднократно, за да покаже съобщение, после го изчиства.
  readonly sessionExpired = signal(false);

  constructor(private http: HttpClient, private router: Router) {}

  login(dto: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.API}/login`, dto).pipe(
      tap(res => this.saveSession(res))
    );
  }

  register(dto: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.API}/register`, dto).pipe(
      tap(res => this.saveSession(res))
    );
  }

  getProfile() {
    return this.http.get<UserProfile>(`${this.API}/profile`);
  }

  updateProfile(dto: UpdateProfileRequest) {
    return this.http.put<UserProfile>(`${this.API}/profile`, dto).pipe(
      tap(profile => {
        this._name.set(profile.name);
        this._phone.set(profile.phoneNumber);
        if (this.isBrowser) {
          localStorage.setItem('name', profile.name);
          if (profile.phoneNumber) localStorage.setItem('phone', profile.phoneNumber);
          else localStorage.removeItem('phone');
        }
      })
    );
  }

  changePassword(dto: ChangePasswordRequest) {
    return this.http.put<void>(`${this.API}/change-password`, dto);
  }

  forgotPassword(email: string) {
    return this.http.post<void>(`${this.API}/forgot-password`, { email });
  }

  resetPassword(dto: ResetPasswordRequest) {
    return this.http.post<void>(`${this.API}/reset-password`, dto);
  }

  logout() {
    this.clearSession();
    this.router.navigate(['/sign-in']);
  }

  // Токенът вече не е валиден (изтекла сесия) — разлика от logout() е, че маркира sessionExpired,
  // за да може sign-in страницата да покаже съобщение защо потребителят се озовава там.
  expireSession(): void {
    if (!this._token()) return; // вече сме разлогнати — не пренавигирай многократно при паралелни 401-ци
    this.clearSession();
    this.sessionExpired.set(true);
    this.router.navigate(['/sign-in']);
  }

  private clearSession(): void {
    if (this.isBrowser) {
      // Само данните за сесията — localStorage.clear() трие и несвързани неща (съгласие за
      // бисквитки, избран език), които трябва да оцелеят след логаут.
      localStorage.removeItem('token');
      localStorage.removeItem('role');
      localStorage.removeItem('name');
      localStorage.removeItem('phone');
    }
    this._token.set(null);
    this._role.set(null);
    this._name.set(null);
    this._phone.set(null);
  }

  private saveSession(res: AuthResponse) {
    if (this.isBrowser) {
      localStorage.setItem('token', res.token);
      localStorage.setItem('role', res.role);
      localStorage.setItem('name', res.name);
      if (res.phone) localStorage.setItem('phone', res.phone); // <- добави
    }
    this._token.set(res.token);
    this._role.set(res.role);
    this._name.set(res.name);
    if (res.phone) this._phone.set(res.phone); // <- добави
  }
}