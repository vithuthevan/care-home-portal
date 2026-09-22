import { Routes } from '@angular/router';

import {
  authGuard,
  guestGuard,
  adminGuard,
  platformGuard,
  organisationSettingsGuard,
  passwordChangeGuard,
} from './core/auth.guard';
import { LoginPage } from './features/login/login';
import { ChangePasswordPage } from './features/login/change-password';
import { ForbiddenPage } from './features/forbidden/forbidden';
import { NotFoundPage } from './features/not-found/not-found';
import { DashboardPage } from './features/dashboard/dashboard';
import { CompanyList } from './features/companies/pages/company-list/company-list';
import { CompanyForm } from './features/companies/pages/company-form/company-form';
import { CompanyDetail } from './features/companies/pages/company-detail/company-detail';
import { CareHomeList } from './features/care-homes/pages/care-home-list/care-home-list';
import { CareHomeForm } from './features/care-homes/pages/care-home-form/care-home-form';
import { CareHomeDashboardPage } from './features/care-homes/pages/care-home-dashboard/care-home-dashboard';
import { CareHomePortalSettingsPage } from './features/care-homes/pages/care-home-portal-settings/care-home-portal-settings';
import { ClientList } from './features/clients/pages/client-list/client-list';
import { ClientForm } from './features/clients/pages/client-form/client-form';
import { ClientProfilePage } from './features/clients/pages/client-profile/client-profile';
import { ClientFundingContractForm } from './features/clients/pages/client-funding-contract-form/client-funding-contract-form';
import { ClientFundingRateForm } from './features/clients/pages/client-funding-rate-form/client-funding-rate-form';
import { FundingAuthorityList } from './features/funding-authorities/pages/funding-authority-list/funding-authority-list';
import { FundingAuthorityForm } from './features/funding-authorities/pages/funding-authority-form/funding-authority-form';
import { InvoiceCategoryList } from './features/invoice-categories/pages/invoice-category-list/invoice-category-list';
import { InvoiceCategoryForm } from './features/invoice-categories/pages/invoice-category-form/invoice-category-form';
import { NominalCodeList } from './features/nominal-codes/pages/nominal-code-list/nominal-code-list';
import { NominalCodeForm } from './features/nominal-codes/pages/nominal-code-form/nominal-code-form';
import { InvoiceTemplateListPage } from './features/invoice-templates/pages/invoice-template-list/invoice-template-list';
import { InvoiceTemplateFormPage } from './features/invoice-templates/pages/invoice-template-form/invoice-template-form';
import { BillingWorkspacePage } from './features/billing/pages/billing-workspace/billing-workspace';
import { InvoiceListPage } from './features/invoices/pages/invoice-list/invoice-list';
import { InvoiceDetailPage } from './features/invoices/pages/invoice-detail/invoice-detail';
import { CreditNoteWorkspacePage } from './features/credit-notes/pages/credit-note-workspace/credit-note-workspace';
import { MiscChargesPage } from './features/misc-charges/pages/misc-charges/misc-charges';
import { ReportsPage } from './features/reports/pages/reports/reports';
import { SageExportPage } from './features/sage/pages/sage-export/sage-export';
import { UserListPage } from './features/users/pages/user-list/user-list';
import { UserFormPage } from './features/users/pages/user-form/user-form';
import { AuditListPage } from './features/audit/pages/audit-list/audit-list';
import { PlatformTenantListPage } from './features/platform/pages/platform-tenant-list/platform-tenant-list';
import { PlatformTenantFormPage } from './features/platform/pages/platform-tenant-form/platform-tenant-form';
import { OrganisationSettingsPage } from './features/settings/pages/organisation-settings/organisation-settings';
import { ReceivablesWorkspacePage } from './features/receivables/pages/receivables-workspace/receivables-workspace';
import { PaymentsWorkspacePage } from './features/payments/pages/payments-workspace/payments-workspace';
import { PaymentDetailPage } from './features/payments/pages/payment-detail/payment-detail';
import { BankingWorkspacePage } from './features/banking/pages/banking-workspace/banking-workspace';
import { RemittanceWorkspacePage } from './features/remittances/pages/remittance-workspace/remittance-workspace';
import { RevenueAssuranceWorkspacePage } from './features/revenue-assurance/pages/revenue-assurance-workspace/revenue-assurance-workspace';
import { DisputesWorkspacePage } from './features/disputes/pages/disputes-workspace/disputes-workspace';
import { CollectionsWorkspacePage } from './features/collections/pages/collections-workspace/collections-workspace';
import { RenewalsWorkspacePage } from './features/renewals/pages/renewals-workspace/renewals-workspace';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  { path: 'change-password', component: ChangePasswordPage, canActivate: [passwordChangeGuard] },
  { path: 'forbidden', component: ForbiddenPage, canActivate: [authGuard] },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardPage, canActivate: [authGuard] },
  { path: 'companies', component: CompanyList, canActivate: [authGuard] },
  { path: 'companies/new', component: CompanyForm, canActivate: [authGuard] },
  { path: 'companies/:id/care-homes', component: CareHomeList, canActivate: [authGuard] },
  { path: 'companies/:id/edit', component: CompanyForm, canActivate: [authGuard] },
  { path: 'companies/:id', component: CompanyDetail, canActivate: [authGuard] },
  { path: 'care-homes', component: CareHomeList, canActivate: [authGuard] },
  { path: 'care-homes/new', component: CareHomeForm, canActivate: [authGuard] },
  { path: 'care-homes/:id/dashboard', component: CareHomeDashboardPage, canActivate: [authGuard] },
  { path: 'care-homes/:id/settings', component: CareHomePortalSettingsPage, canActivate: [authGuard] },
  { path: 'care-homes/:id/edit', component: CareHomeForm, canActivate: [authGuard] },
  { path: 'clients', component: ClientList, canActivate: [authGuard] },
  { path: 'clients/new', component: ClientForm, canActivate: [authGuard] },
  { path: 'clients/:id/funding/rates/new', component: ClientFundingRateForm, canActivate: [authGuard] },
  { path: 'clients/:id/funding/new', component: ClientFundingContractForm, canActivate: [authGuard] },
  { path: 'clients/:id/edit', component: ClientForm, canActivate: [authGuard] },
  { path: 'clients/:id', component: ClientProfilePage, canActivate: [authGuard] },
  { path: 'funding-authorities', component: FundingAuthorityList, canActivate: [authGuard] },
  { path: 'funding-authorities/new', component: FundingAuthorityForm, canActivate: [authGuard] },
  {
    path: 'funding-authorities/:id/edit',
    component: FundingAuthorityForm,
    canActivate: [authGuard],
  },
  { path: 'invoice-categories', component: InvoiceCategoryList, canActivate: [authGuard] },
  { path: 'invoice-categories/new', component: InvoiceCategoryForm, canActivate: [authGuard] },
  { path: 'invoice-categories/:id/edit', component: InvoiceCategoryForm, canActivate: [authGuard] },
  { path: 'nominal-codes', component: NominalCodeList, canActivate: [authGuard] },
  { path: 'nominal-codes/new', component: NominalCodeForm, canActivate: [authGuard] },
  { path: 'nominal-codes/:id/edit', component: NominalCodeForm, canActivate: [authGuard] },
  { path: 'invoice-templates', component: InvoiceTemplateListPage, canActivate: [authGuard] },
  { path: 'invoice-templates/new', component: InvoiceTemplateFormPage, canActivate: [authGuard] },
  {
    path: 'invoice-templates/:id/edit',
    component: InvoiceTemplateFormPage,
    canActivate: [authGuard],
  },
  { path: 'billing', component: BillingWorkspacePage, canActivate: [authGuard] },
  { path: 'invoices', component: InvoiceListPage, canActivate: [authGuard] },
  { path: 'invoices/:id', component: InvoiceDetailPage, canActivate: [authGuard] },
  { path: 'credit-notes', component: CreditNoteWorkspacePage, canActivate: [authGuard] },
  { path: 'receivables', component: ReceivablesWorkspacePage, canActivate: [authGuard] },
  { path: 'payments', component: PaymentsWorkspacePage, canActivate: [authGuard] },
  { path: 'payments/:id', component: PaymentDetailPage, canActivate: [authGuard] },
  { path: 'banking', component: BankingWorkspacePage, canActivate: [authGuard] },
  { path: 'remittances', component: RemittanceWorkspacePage, canActivate: [authGuard] },
  { path: 'revenue-assurance', component: RevenueAssuranceWorkspacePage, canActivate: [authGuard] },
  { path: 'disputes', component: DisputesWorkspacePage, canActivate: [authGuard] },
  { path: 'collections', component: CollectionsWorkspacePage, canActivate: [authGuard] },
  { path: 'contract-renewals', component: RenewalsWorkspacePage, canActivate: [authGuard] },
  { path: 'misc-charges', component: MiscChargesPage, canActivate: [authGuard] },
  { path: 'reports', component: ReportsPage, canActivate: [authGuard] },
  { path: 'sage-exports', component: SageExportPage, canActivate: [authGuard] },
  { path: 'users', component: UserListPage, canActivate: [authGuard, adminGuard] },
  { path: 'users/new', component: UserFormPage, canActivate: [authGuard, adminGuard] },
  { path: 'audit', component: AuditListPage, canActivate: [authGuard, adminGuard] },
  {
    path: 'settings/organisation',
    component: OrganisationSettingsPage,
    canActivate: [authGuard, organisationSettingsGuard],
  },
  {
    path: 'platform/tenants',
    component: PlatformTenantListPage,
    canActivate: [authGuard, platformGuard],
  },
  {
    path: 'platform/tenants/new',
    component: PlatformTenantFormPage,
    canActivate: [authGuard, platformGuard],
  },
  {
    path: 'platform/tenants/:id',
    component: PlatformTenantFormPage,
    canActivate: [authGuard, platformGuard],
  },
  { path: '**', component: NotFoundPage },
];
