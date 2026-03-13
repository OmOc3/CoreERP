import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfigService } from './app-config.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(AppConfigService);

  get<T>(path: string, params?: Record<string, unknown>): Observable<T> {
    return this.http.get<T>(this.url(path), { params: this.toParams(params) });
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(this.url(path), body);
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(this.url(path), body);
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<T>(this.url(path));
  }

  download(path: string, params?: Record<string, unknown>): Observable<Blob> {
    return this.http.get(this.url(path), {
      params: this.toParams(params),
      responseType: 'blob'
    });
  }

  private url(path: string): string {
    return `${this.appConfig.apiBaseUrl}/${path.replace(/^\//, '')}`;
  }

  private toParams(params?: Record<string, unknown>): HttpParams {
    let httpParams = new HttpParams();

    if (!params) {
      return httpParams;
    }

    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') {
        return;
      }

      if (Array.isArray(value)) {
        value.forEach((item) => {
          httpParams = httpParams.append(key, String(item));
        });
        return;
      }

      httpParams = httpParams.set(key, String(value));
    });

    return httpParams;
  }
}
