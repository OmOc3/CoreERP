import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.accessToken;
  const isAuthRequest = request.url.includes('/auth/login') || request.url.includes('/auth/refresh');

  const authorizedRequest = token && !isAuthRequest
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : request;

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 403) {
        void router.navigate(['/forbidden']);
        return throwError(() => error);
      }

      if (error.status !== 401 || isAuthRequest || !auth.refreshToken) {
        if (error.status === 401 && !isAuthRequest) {
          auth.logout(false);
        }

        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((refreshedToken) =>
          next(
            request.clone({
              setHeaders: {
                Authorization: `Bearer ${refreshedToken}`
              }
            })
          )
        ),
        catchError((refreshError) => {
          auth.logout(false);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
