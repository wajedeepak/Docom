import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminService } from '../../../core/services/admin.service';
import { DoctorDto, PagedResult } from '../../../core/models/doctor.model';
import { AddDoctorDialogComponent } from '../add-doctor-dialog/add-doctor-dialog.component';

@Component({
  selector: 'app-doctors',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatButtonModule, MatInputModule, MatFormFieldModule, MatIconModule,
    MatTableModule, MatChipsModule, MatSnackBarModule, MatDialogModule,
    MatProgressSpinnerModule, MatPaginatorModule, MatSlideToggleModule, MatTooltipModule
  ],
  templateUrl: './doctors.component.html',
  styleUrls: ['./doctors.component.scss']
})
export class DoctorsComponent implements OnInit {
  private adminService = inject(AdminService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  result = signal<PagedResult<DoctorDto> | null>(null);
  loading = signal(true);
  page = 1;
  pageSize = 20;

  displayedColumns = ['slug', 'name', 'specialization', 'status', 'actions'];

  ngOnInit(): void {
    this.loadDoctors();
  }

  loadDoctors(): void {
    this.loading.set(true);
    this.adminService.getDoctors(this.page, this.pageSize).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openAddDoctor(): void {
    const ref = this.dialog.open(AddDoctorDialogComponent, { width: '460px' });
    ref.afterClosed().subscribe((created: DoctorDto | null) => {
      if (created) {
        this.loadDoctors();
        this.snackBar.open(`Dr. ${created.name} added`, '', { duration: 2500 });
      }
    });
  }

  openEditDoctor(doctor: DoctorDto): void {
    const ref = this.dialog.open(AddDoctorDialogComponent, { width: '460px', data: { doctor } });
    ref.afterClosed().subscribe((updated: DoctorDto | null) => {
      if (updated) {
        this.loadDoctors();
        this.snackBar.open(`Dr. ${updated.name} updated`, '', { duration: 2500 });
      }
    });
  }

  toggleActive(doctor: DoctorDto): void {
    this.adminService.updateDoctor(doctor.id, {
      name: doctor.name,
      specialization: doctor.specialization,
      slug: doctor.slug,
      isActive: !doctor.isActive
    }).subscribe({
      next: () => this.loadDoctors(),
      error: (err) => {
        this.snackBar.open(err?.error?.error ?? 'Update failed', 'OK', { duration: 3000 });
      }
    });
  }

  onPageChange(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.loadDoctors();
  }

  getDoctorUrl(slug: string): string {
    return `docom.in/${slug}`;
  }
}
