import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Product, CreateProductDto, TrashedProduct } from '../auth/auth-types';
import { SKIP_TRANSFER_CACHE } from '../http-context-tokens';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly API = `${environment.apiUrl}/Product`;

  constructor(private http: HttpClient) {}

  getAll(categoryId?: string, take?: number) {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId);
    if (take) params = params.set('take', take);
    // пълният (без categoryId/take) каталог е ~250 KB — не бива да се вгражда в SSR HTML-а, вж. SKIP_TRANSFER_CACHE
    const context = new HttpContext().set(SKIP_TRANSFER_CACHE, !categoryId && !take);
    return this.http.get<Product[]>(this.API, { params, context });
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

  setHidden(id: string, hidden: boolean) {
    return this.http.patch<Product>(`${this.API}/${id}/hidden`, { hidden });
  }

  getDeleted() {
    return this.http.get<TrashedProduct[]>(`${this.API}/deleted`);
  }

  restore(id: string) {
    return this.http.post(`${this.API}/${id}/restore`, {});
  }

  purge(id: string) {
    return this.http.delete(`${this.API}/${id}/purge`);
  }
}