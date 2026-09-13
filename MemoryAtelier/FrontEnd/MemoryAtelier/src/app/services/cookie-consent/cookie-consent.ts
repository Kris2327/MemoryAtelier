import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'cookie_consent_ack';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  acknowledged = signal(this.readStoredValue());

  private readStoredValue(): boolean {
    try {
      return localStorage.getItem(STORAGE_KEY) === '1';
    } catch {
      return false;
    }
  }

  acknowledge(): void {
    this.acknowledged.set(true);
    try {
      localStorage.setItem(STORAGE_KEY, '1');
    } catch {
      // localStorage недостъпен (напр. частен режим) — банерът просто ще се показва пак при следващо зареждане
    }
  }
}
