import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfile } from './auth-types';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = `${environment.apiUrl}/auth`;

  private _token = signal<string | null>(localStorage.getItem('token'));
  private _role = signal<string | null>(localStorage.getItem('role'));
  private _name = signal<string | null>(localStorage.getItem('name'));
  private _phone = signal<string | null>(localStorage.getItem('phone')); // <- добави

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
    localStorage.clear();
    this._token.set(null);
    this._role.set(null);
    this._name.set(null);
    this._phone.set(null); // <- добави
    this.router.navigate(['/sign-in']);
  }

  private saveSession(res: AuthResponse) {
    localStorage.setItem('token', res.token);
    localStorage.setItem('role', res.role);
    localStorage.setItem('name', res.name);
    if (res.phone) localStorage.setItem('phone', res.phone); // <- добави
    this._token.set(res.token);
    this._role.set(res.role);
    this._name.set(res.name);
    if (res.phone) this._phone.set(res.phone); // <- добави
  }
}