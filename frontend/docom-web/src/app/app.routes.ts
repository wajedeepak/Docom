import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/doctor/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    canActivate: [adminGuard],
    children: [
      {
        path: 'doctors',
        loadComponent: () =>
          import('./features/admin/doctors/doctors.component').then(m => m.DoctorsComponent)
      },
      { path: '', redirectTo: 'doctors', pathMatch: 'full' }
    ]
  },
  {
    path: 't/:publicTokenId',
    loadComponent: () =>
      import('./features/patient/tracking-resolver/tracking-resolver.component').then(m => m.TrackingResolverComponent)
  },
  {
    path: ':slug',
    loadComponent: () =>
      import('./features/patient/queue-page/queue-page.component').then(m => m.QueuePageComponent)
  },
  {
    path: '',
    loadComponent: () =>
      import('./features/landing/landing.component').then(m => m.LandingComponent),
    pathMatch: 'full'
  }
];
