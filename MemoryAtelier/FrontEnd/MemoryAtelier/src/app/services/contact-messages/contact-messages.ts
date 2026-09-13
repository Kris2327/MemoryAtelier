import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ContactMessage } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class ContactMessagesService {
  private readonly API = `${environment.apiUrl}/ContactMessages`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<ContactMessage[]>(this.API);
  }

  reply(id: string, replyText: string) {
    return this.http.post(`${this.API}/${id}/reply`, { replyText });
  }
}
