import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { CartItem } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly API = `${environment.apiUrl}/cart`;
  cartCount = signal(0);

  constructor(private http: HttpClient) {}

  getCart() { return this.http.get<CartItem[]>(this.API); }

  addToCart(productId: string, quantity: number) {
    return this.http.post<CartItem>(this.API, { productId, quantity }).pipe(
      tap(() => this.refreshCount())
    );
  }

  updateCart(id: string, quantity: number, fulfillmentChoice?: 'Split' | 'Wait' | null) {
    return this.http.put(this.API + '/' + id, { quantity, fulfillmentChoice: fulfillmentChoice ?? null }).pipe(
      tap(() => this.refreshCount())
    );
  }

  removeFromCart(id: string) {
    return this.http.delete(this.API + '/' + id).pipe(
      tap(() => this.refreshCount())
    );
  }

  getCount() {
    return this.http.get<number>(this.API + '/count').pipe(
      tap(n => this.cartCount.set(n))
    );
  }

  // презарежда истинската бройка от сървъра, вместо да я смята локално — за да не се натрупва разминаване
  private refreshCount() {
    this.getCount().subscribe();
  }
}