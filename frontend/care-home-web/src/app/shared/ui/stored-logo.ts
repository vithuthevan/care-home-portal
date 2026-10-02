import { Component, inject, input, OnDestroy, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { catchError, combineLatest, of, switchMap } from 'rxjs';
import type { Observable } from 'rxjs';

export function trustedObjectUrl(sanitizer: DomSanitizer, blob: Blob): { objectUrl: string; safeUrl: SafeUrl } {
  const objectUrl = URL.createObjectURL(blob);
  return { objectUrl, safeUrl: sanitizer.bypassSecurityTrustUrl(objectUrl) };
}

@Component({
  selector: 'app-stored-logo',
  template: `
    @if (safeUrl(); as url) {
      <img [src]="url" [alt]="alt()" class="stored-logo" [class.stored-logo--thumb]="size() === 'thumb'" />
    }
  `,
  styles: `
    .stored-logo {
      display: block;
      max-height: 4rem;
      max-width: 10rem;
      object-fit: contain;
      background: #fff;
      border: 1px solid var(--app-border);
      border-radius: 8px;
      padding: 0.25rem;
    }
    .stored-logo--thumb {
      width: 2.5rem;
      height: 2.5rem;
      max-width: 2.5rem;
      max-height: 2.5rem;
      padding: 0.15rem;
    }
  `,
})
export class StoredLogoComponent implements OnDestroy {
  private readonly sanitizer = inject(DomSanitizer);

  readonly sourceKey = input<string | null>(null);
  readonly present = input(false);
  readonly alt = input('Logo');
  readonly size = input<'thumb' | 'preview'>('preview');
  readonly load = input<((key: string) => Observable<Blob>) | null>(null);

  readonly safeUrl = signal<SafeUrl | null>(null);
  private objectUrl: string | null = null;

  constructor() {
    combineLatest([
      toObservable(this.sourceKey),
      toObservable(this.present),
      toObservable(this.load),
    ])
      .pipe(
        switchMap(([key, present, load]) => {
          if (!present || !key || !load) {
            return of(null);
          }
          return load(key).pipe(catchError(() => of(null)));
        }),
        takeUntilDestroyed(),
      )
      .subscribe((blob) => {
        this.revoke();
        if (!blob?.size) {
          return;
        }
        const trusted = trustedObjectUrl(this.sanitizer, blob);
        this.objectUrl = trusted.objectUrl;
        this.safeUrl.set(trusted.safeUrl);
      });
  }

  ngOnDestroy(): void {
    this.revoke();
  }

  private revoke(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
    this.safeUrl.set(null);
  }
}
