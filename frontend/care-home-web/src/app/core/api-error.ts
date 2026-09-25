import { HttpErrorResponse } from '@angular/common/http';

const GENERIC_SERVER_MESSAGE =
  'An unexpected error occurred. Please try again or contact support.';

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return fallback;
    }

    if (error.status === 502 || error.status === 503 || error.status === 504) {
      return fallback;
    }

    if (error.status === 401) {
      return 'Your session has expired. Please sign in again.';
    }

    if (error.status === 403) {
      return 'You do not have permission to view this information.';
    }

    if (typeof error.error === 'string') {
      if (looksLikeMarkup(error.error) || containsSecretDump(error.error)) {
        return fallback;
      }

      const trimmed = error.error.trim();
      if (trimmed && !looksLikeMarkup(trimmed)) {
        return trimmed;
      }

      return fallback;
    }

    const fromBody = extractJsonMessage(error.error);
    if (fromBody) {
      if (
        fromBody === GENERIC_SERVER_MESSAGE
        || containsSecretDump(fromBody)
        || looksLikeMarkup(fromBody)
      ) {
        return fallback;
      }

      return fromBody;
    }

    if (error.status >= 500) {
      return fallback;
    }

    return fallback;
  }

  if (!isRecord(error)) {
    return fallback;
  }

  const body = error['error'];

  if (typeof body === 'string' && body.trim()) {
    return looksLikeMarkup(body) || containsSecretDump(body) ? fallback : body;
  }

  if (!isRecord(body)) {
    return fallback;
  }

  if (typeof body['message'] === 'string' && body['message'].trim()) {
    const message = body['message'];
    return message === GENERIC_SERVER_MESSAGE || containsSecretDump(message) || looksLikeMarkup(message)
      ? fallback
      : message;
  }

  const errors = body['errors'];

  if (isRecord(errors)) {
    for (const value of Object.values(errors)) {
      if (Array.isArray(value) && typeof value[0] === 'string' && value[0].trim()) {
        return containsSecretDump(value[0]) || looksLikeMarkup(value[0]) ? fallback : value[0];
      }
    }
  }

  if (typeof body['title'] === 'string' && body['title'].trim()) {
    const title = body['title'];
    return looksLikeMarkup(title) ? fallback : title;
  }

  return fallback;
}

export function logApiFailure(error: unknown, context = 'Request failed'): void {
  if (error instanceof HttpErrorResponse) {
    console.error(context, { status: error.status, url: sanitizeUrl(error.url) });
    return;
  }

  console.error(context);
}

function extractJsonMessage(body: unknown): string | null {
  if (!isRecord(body)) {
    return null;
  }

  if (typeof body['message'] === 'string' && body['message'].trim()) {
    return body['message'];
  }

  return null;
}

function looksLikeMarkup(value: string): boolean {
  const sample = value.trim().slice(0, 500).toLowerCase();
  if (!sample) {
    return false;
  }

  return (
    sample.startsWith('<!doctype html')
    || sample.startsWith('<html')
    || sample.includes('<body')
    || sample.includes('bad gateway')
    || sample.includes('nginx')
    || sample.includes('</html>')
  );
}

function containsSecretDump(value: string): boolean {
  return /"(password|currentPassword|newPassword|passwordCipher|token|authorization)"\s*:/i.test(
    value,
  );
}

function sanitizeUrl(url: string | null): string | null {
  if (!url) {
    return null;
  }

  const path = url.split('?')[0]?.split('#')[0];
  return path || url;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
