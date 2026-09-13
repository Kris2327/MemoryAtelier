import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { HeroImage } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class HeroImageService {
  private readonly API = `${environment.apiUrl}/HeroImages`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<HeroImage[]>(this.API);
  }

  create(imageUrl: string) {
    return this.http.post<HeroImage>(this.API, { imageUrl });
  }

  delete(id: string) {
    return this.http.delete(`${this.API}/${id}`);
  }

  reorder(orderedIds: string[]) {
    return this.http.put(`${this.API}/reorder`, { orderedIds });
  }
}
