import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule, MatTooltipModule],
  template: `
    <div class="admin-layout">
      <aside class="sidebar">
        <div class="brand">
          <div class="brand-icon">D</div>
          <span class="brand-name">docom</span>
          <span class="admin-tag">Admin</span>
        </div>
        <nav class="nav">
          <a class="nav-item" routerLink="/admin/doctors" routerLinkActive="nav-item--active">
            <mat-icon>people</mat-icon>
            <span>Doctors</span>
          </a>
        </nav>
        <div class="sidebar-footer">
          <span class="user-email">{{ auth.user()?.email }}</span>
          <button mat-icon-button (click)="logout()" matTooltip="Logout">
            <mat-icon>logout</mat-icon>
          </button>
        </div>
      </aside>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .admin-layout { display: flex; height: 100vh; background: #f7f8fc; }
    .sidebar { width: 220px; background: white; border-right: 1px solid #e5e7eb; display: flex; flex-direction: column; padding: 20px 12px; flex-shrink: 0; }
    .brand { display: flex; align-items: center; gap: 8px; padding: 0 8px 20px; border-bottom: 1px solid #e5e7eb; margin-bottom: 16px; }
    .brand-icon { width: 32px; height: 32px; background: #1a73e8; color: white; border-radius: 8px; font-size: 16px; font-weight: 800; display: flex; align-items: center; justify-content: center; }
    .brand-name { font-size: 16px; font-weight: 700; color: #1a1a2e; }
    .admin-tag { background: #fce8e6; color: #d32f2f; font-size: 10px; font-weight: 700; padding: 2px 6px; border-radius: 6px; text-transform: uppercase; }
    .nav { flex: 1; }
    .nav-item { display: flex; align-items: center; gap: 10px; padding: 10px 12px; border-radius: 10px; font-size: 14px; color: #6b7280; cursor: pointer; text-decoration: none; }
    .nav-item--active { background: #e8f0fe; color: #1a73e8; font-weight: 600; }
    .sidebar-footer { border-top: 1px solid #e5e7eb; padding-top: 16px; display: flex; align-items: center; justify-content: space-between; }
    .user-email { font-size: 12px; color: #9ca3af; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .content { flex: 1; overflow-y: auto; padding: 24px; }
  `]
})
export class AdminLayoutComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  logout(): void { this.auth.logout(); }
}
