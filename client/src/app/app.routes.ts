import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./pages/landing/landing').then((m) => m.LandingComponent),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register').then((m) => m.RegisterComponent),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.DashboardComponent),
  },
  {
    path: 'projects/:projectId/chat',
    canActivate: [authGuard],
    loadComponent: () => import('./features/chat/chat.component').then((m) => m.ChatComponent),
  },
  {
    path: 'billing',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/billing/billing').then((m) => m.BillingComponent),
  },
  {
    path: 'projects/:projectId/pipeline',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/pipeline/pipeline').then((m) => m.PipelineComponent),
  },
  {
    path: 'projects/:projectId/widget',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/widget-config/widget-config').then((m) => m.WidgetConfigComponent),
  },
];
