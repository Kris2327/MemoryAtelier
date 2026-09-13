import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly API = `${environment.apiUrl}/Contact`;

  constructor(private http: HttpClient) {}

  send(name: string, email: string, message: string, website: string = '') {
    return this.http.post(this.API, { name, email, message, website });
  }
}
