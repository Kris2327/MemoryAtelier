import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Category, TrashedCategory } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly API = `${environment.apiUrl}/categories`;

  /** Shared category tree, kept in sync so any component (e.g. the navbar) reflects admin changes immediately. */
  categories = signal<Category[]>([]);

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<Category[]>(this.API);
  }

  refresh(): void {
    this.getAll().subscribe(data => this.categories.set(data));
  }

  create(dto: { name: string; nameEn: string | null; parentId: string | null }) {
    return this.http.post<Category>(this.API, dto);
  }

  update(id: string, dto: { name: string; nameEn: string | null; parentId: string | null }) {
    return this.http.put<Category>(`${this.API}/${id}`, dto);
  }

  delete(id: string) {
    return this.http.delete(`${this.API}/${id}`);
  }

  reorder(orderedIds: string[]) {
    return this.http.put(`${this.API}/reorder`, { orderedIds });
  }

  getDeleted() {
    return this.http.get<TrashedCategory[]>(`${this.API}/deleted`);
  }

  restore(id: string) {
    return this.http.post(`${this.API}/${id}/restore`, {});
  }
}