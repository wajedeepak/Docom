import { Component, HostListener, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatCardModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent {
  menuOpen = signal(false);
  scrolled  = signal(false);
  readonly year = new Date().getFullYear();

  toggleMenu(): void { this.menuOpen.update(v => !v); }
  closeMenu(): void  { this.menuOpen.set(false); }

  @HostListener('window:scroll')
  onScroll(): void { this.scrolled.set(window.scrollY > 20); }

  scrollTo(id: string): void {
    this.closeMenu();
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  readonly problems = [
    { icon: 'groups',         title: 'Crowded waiting rooms',       body: 'Patients pile up with no way to know how long the wait will be.' },
    { icon: 'help_outline',   title: '"When is my turn?"',          body: 'Staff constantly answer the same question instead of focusing on patients.' },
    { icon: 'receipt_long',   title: 'Paper token chaos',           body: 'Receptionists managing hand-written slips with no real-time visibility.' }
  ];

  readonly steps = [
    { num: '01', icon: 'play_circle', title: 'Doctor starts a session',         body: 'Open the dashboard, tap Start Session. The queue goes live instantly.' },
    { num: '02', icon: 'qr_code_2',   title: 'Patients open the clinic link',   body: 'Share docom.in/your-name. No app download needed — works in any browser.' },
    { num: '03', icon: 'smartphone',  title: 'Patients track from their phone', body: 'They see their token number, live queue position, and estimated wait.' }
  ];

  readonly benefits = [
    { icon: 'chair',              title: 'Fewer people in the waiting room', body: 'Patients arrive only when their turn is close. Less crowding, less noise.' },
    { icon: 'touch_app',         title: 'One-click for receptionists',       body: 'Advance the queue, add walk-ins, pause for lunch — all from one screen.' },
    { icon: 'bolt',              title: 'Live updates, zero delay',          body: 'Queue changes push instantly via real-time sync. No refresh needed.' }
  ];

  readonly stats = [
    { value: '1,200+',   label: 'Clinics & Doctors',   icon: 'local_hospital' },
    { value: '5 lakh+',  label: 'Tokens issued',        icon: 'confirmation_number' },
    { value: '98%',      label: 'Uptime SLA',           icon: 'verified' },
    { value: '0',        label: 'App downloads needed', icon: 'phonelink_erase' }
  ];
}
