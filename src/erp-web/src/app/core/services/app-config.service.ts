import { Injectable } from '@angular/core';

export interface AppConfig {
  apiBaseUrl: string;
}

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private config: AppConfig = {
    apiBaseUrl: 'http://localhost:8080/api/v1'
  };

  async load(): Promise<void> {
    try {
      const response = await fetch('assets/app-config.json', { cache: 'no-store' });
      if (!response.ok) {
        return;
      }

      const loaded = (await response.json()) as Partial<AppConfig>;
      this.config = {
        ...this.config,
        ...loaded
      };
    } catch {
      // Fallback to the default local API URL.
    }
  }

  get apiBaseUrl(): string {
    return this.config.apiBaseUrl.replace(/\/$/, '');
  }
}
