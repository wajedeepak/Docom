import { Component, signal, inject } from '@angular/core';
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
    <div class="admin-layout" [class.sidebar-open]="sidebarOpen()">
      <button mat-icon-button class="mobile-menu-btn" (click)="toggleSidebar()" matTooltip="Toggle menu">
        <mat-icon>{{ sidebarOpen() ? 'close' : 'menu' }}</mat-icon>
      </button>
      
      <aside class="sidebar" [class.sidebar--mobile-open]="sidebarOpen()">
        <div class="brand">
          <div class="brand-icon">D</div>
          <span class="brand-name">docom</span>
          <span class="admin-tag">Admin</span>
        </div>
        <nav class="nav">
          <a class="nav-item" routerLink="/admin/doctors" routerLinkActive="nav-item--active" (click)="closeSidebar()">
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
      
      <div class="sidebar-overlay" [class.sidebar-overlay--visible]="sidebarOpen()" (click)="closeSidebar()"></div>
      
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styleUrls: ['./admin-layout.component.scss']
})
export class AdminLayoutComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  
  sidebarOpen = signal(false);
  
  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }
  
  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
  
  logout(): void { 
    this.auth.logout(); 
  }
}
