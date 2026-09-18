import { Injectable, PLATFORM_ID, inject, signal, computed } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from './auth-types';

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

  logout() {
    if (this.isBrowser) localStorage.clear();
    this._token.set(null);
    this._role.set(null);
    this._name.set(null);
    this._phone.set(null); // <- добави
    this.router.navigate(['/sign-in']);
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