import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Product, CreateProductDto, TrashedProduct } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly API = `${environment.apiUrl}/Product`;

  constructor(private http: HttpClient) {}

  getAll(categoryId?: string) {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId);
    return this.http.get<Product[]>(this.API, { params });
  }

  getById(id: string) {
    return this.http.get<Product>(`${this.API}/${id}`);
  }

  create(dto: CreateProductDto) {
    return this.http.post<Product>(this.API, dto);
  }

  update(id: string, dto: CreateProductDto) {
    return this.http.put<Product>(`${this.API}/${id}`, dto);
  }

  delete(id: string) {
    return this.http.delete(`${this.API}/${id}`);
  }

  getDeleted() {
    return this.http.get<TrashedProduct[]>(`${this.API}/deleted`);
  }

  restore(id: string) {
    return this.http.post(`${this.API}/${id}/restore`, {});
  }
}