import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AdminOrder, OrderStatus } from '../auth/auth-types';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly API = `${environment.apiUrl}/Order`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<AdminOrder[]>(this.API);
  }

  getMine() {
    return this.http.get<AdminOrder[]>(`${this.API}/mine`);
  }

  markSeen(id: string) {
    return this.http.put(`${this.API}/${id}/seen`, {});
  }

  updateStatus(id: string, status: OrderStatus, note?: string | null) {
    return this.http.put(`${this.API}/${id}/status`, { status, note: note ?? null });
  }

  updateShipmentInfo(
    orderId: string,
    itemId: string,
    firstShipmentSentAt: string | null,
    nextShipmentEstimatedAt: string | null,
    finalShipmentSentAt: string | null
  ) {
    return this.http.put(`${this.API}/${orderId}/items/${itemId}/shipment-info`, {
      firstShipmentSentAt,
      nextShipmentEstimatedAt,
      finalShipmentSentAt
    });
  }
}
