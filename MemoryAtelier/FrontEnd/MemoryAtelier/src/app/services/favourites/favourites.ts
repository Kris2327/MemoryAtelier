import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FavouriteItem } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class FavouritesService {
  private readonly API = `${environment.apiUrl}/favourites`;
  favCount = signal(0);

  constructor(private http: HttpClient) {}

  getFavourites() { return this.http.get<FavouriteItem[]>(this.API); }

  toggle(productId: string) {
    return this.http.post<{ added: boolean }>(this.API + '/' + productId, {}).pipe(
      tap(res => this.favCount.update(n => res.added ? n + 1 : Math.max(0, n - 1)))
    );
  }

  getCount() {
    return this.http.get<number>(this.API + '/count').pipe(
      tap(n => this.favCount.set(n))
    );
  }
}