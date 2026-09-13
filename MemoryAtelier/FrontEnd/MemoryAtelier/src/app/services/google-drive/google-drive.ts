import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    gapi?: any;
    google?: any;
  }
}

@Injectable({ providedIn: 'root' })
export class GoogleDriveService {
  private readonly scope = 'https://www.googleapis.com/auth/drive.readonly';

  private readonly storageKey = 'gdrive_access_token';

  private scriptsLoaded = false;
  private tokenClient: any;

  private loadScript(src: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = src;
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error(`Неуспешно зареждане на ${src}`));
      document.body.appendChild(script);
    });
  }

  private async ensureScripts(): Promise<void> {
    if (this.scriptsLoaded) {
      return;
    }

    await Promise.all([
      this.loadScript('https://apis.google.com/js/api.js'),
      this.loadScript('https://accounts.google.com/gsi/client')
    ]);

    await new Promise<void>(resolve => {
      window.gapi.load('picker', () => resolve());
    });

    this.scriptsLoaded = true;
  }

  private loadStoredToken(): string | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      if (!raw) return null;

      const { token, expiresAt } = JSON.parse(raw);
      if (!token || Date.now() >= expiresAt) {
        localStorage.removeItem(this.storageKey);
        return null;
      }
      return token;
    } catch {
      return null;
    }
  }

  private storeToken(token: string, expiresInSeconds: number): void {
    try {
      // изтича малко по-рано от реалния срок, за да не ползваме токен "на ръба"
      const expiresAt = Date.now() + Math.max(expiresInSeconds - 60, 0) * 1000;
      localStorage.setItem(this.storageKey, JSON.stringify({ token, expiresAt }));
    } catch {
      // localStorage недостъпен (напр. частен режим) — токенът просто няма да се пази между презарежданията
    }
  }

  private requestAccessToken(forceConsent = false): Promise<string> {
    if (!forceConsent) {
      const cached = this.loadStoredToken();
      if (cached) return Promise.resolve(cached);
    }

    return new Promise((resolve, reject) => {
      if (!this.tokenClient) {
        this.tokenClient = window.google.accounts.oauth2.initTokenClient({
          client_id: environment.googleDrive.clientId,
          scope: this.scope,
          callback: () => {}
        });
      }

      this.tokenClient.callback = (response: any) => {
        if (response.error) {
          reject(new Error(response.error));
          return;
        }
        this.storeToken(response.access_token, response.expires_in ?? 3600);
        resolve(response.access_token);
      };

      this.tokenClient.error_callback = () => {
        if (forceConsent) {
          reject(new Error('Google Drive оторизацията беше отказана.'));
          return;
        }
        // тихият опит (без диалог) не мина — пробвай пак, този път с изричен consent екран
        this.requestAccessToken(true).then(resolve, reject);
      };

      this.tokenClient.requestAccessToken({ prompt: forceConsent ? 'consent' : '' });
    });
  }

  private openPicker(accessToken: string): Promise<any[]> {
    return new Promise(resolve => {
      const view = new window.google.picker.DocsView(window.google.picker.ViewId.DOCS_IMAGES)
        .setSelectFolderEnabled(false)
        .setIncludeFolders(true);

      const picker = new window.google.picker.PickerBuilder()
        .addView(view)
        .setOAuthToken(accessToken)
        .setDeveloperKey(environment.googleDrive.apiKey)
        .setCallback((data: any) => {
          if (data.action === window.google.picker.Action.PICKED) {
            picker.dispose();
            resolve(data.docs);
          } else if (data.action === window.google.picker.Action.CANCEL) {
            picker.dispose();
            resolve([]);
          }
        })
        .build();

      picker.setVisible(true);
    });
  }

  private async downloadFile(doc: any, accessToken: string): Promise<File> {
    const response = await fetch(`https://www.googleapis.com/drive/v3/files/${doc.id}?alt=media`, {
      headers: { Authorization: `Bearer ${accessToken}` }
    });

    if (!response.ok) {
      throw new Error(`Неуспешно изтегляне на ${doc.name} от Google Drive`);
    }

    const blob = await response.blob();
    return new File([blob], doc.name, { type: doc.mimeType || blob.type });
  }

  async pickImages(): Promise<File[]> {
    await this.ensureScripts();
    const accessToken = await this.requestAccessToken();
    const docs = await this.openPicker(accessToken);

    if (!docs.length) {
      return [];
    }

    return Promise.all(docs.map(doc => this.downloadFile(doc, accessToken)));
  }
}
