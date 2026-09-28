import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/** Maps blank optional email fields to null for API requests. */
export function optionalEmail(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed === '' ? null : trimmed;
}

/** Use on optional email fields so empty values skip format validation. */
export function optionalEmailValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const trimmed = (control.value ?? '').toString().trim();
    if (trimmed === '') {
      return null;
    }
    return Validators.email(control);
  };
}
