import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { COMMERCIAL_REVENUE_ENABLED } from './commercial-revenue.feature';

export const commercialRevenueGuard: CanActivateFn = () => {
  if (COMMERCIAL_REVENUE_ENABLED) {
    return true;
  }
  return inject(Router).createUrlTree(['/']);
};
