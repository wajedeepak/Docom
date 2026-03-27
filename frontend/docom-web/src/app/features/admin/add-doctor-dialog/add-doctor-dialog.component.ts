import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { AdminService } from '../../../core/services/admin.service';
import { DoctorDto } from '../../../core/models/doctor.model';

export interface AddDoctorDialogData {
  doctor?: DoctorDto;
}

@Component({
  selector: 'app-add-doctor-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatInputModule, MatFormFieldModule,
    MatDialogModule, MatProgressSpinnerModule, MatSlideToggleModule
  ],
  templateUrl: './add-doctor-dialog.component.html',
  styleUrls: ['./add-doctor-dialog.component.scss']
})
export class AddDoctorDialogComponent {
  private fb          = inject(FormBuilder);
  private adminService = inject(AdminService);
  private dialogRef   = inject(MatDialogRef<AddDoctorDialogComponent>);
  private snackBar    = inject(MatSnackBar);
  readonly data       = inject<AddDoctorDialogData>(MAT_DIALOG_DATA, { optional: true });

  readonly editMode = !!this.data?.doctor;
  readonly doctor   = this.data?.doctor ?? null;

  loading        = signal(false);
  slugAvailable  = signal<boolean | null>(null);
  checkingSlug   = signal(false);

  form = this.fb.group({
    name:           [this.doctor?.name ?? '',           [Validators.required, Validators.minLength(2)]],
    specialization: [this.doctor?.specialization ?? '', [Validators.required, Validators.minLength(2)]],
    slug:           [this.doctor?.slug ?? '',           [Validators.required, Validators.pattern(/^[a-z0-9\-]+$/)]],
    email:          ['', this.editMode ? [] : [Validators.required, Validators.email]],
    address:        [this.doctor?.address ?? ''],
    pincode:        [this.doctor?.pincode ?? '', [Validators.pattern(/^\d{6}$/)]],
    isActive:       [this.doctor?.isActive ?? true]
  });

  constructor() {
    // Auto-generate slug from name (add mode only, when slug not yet touched)
    if (!this.editMode) {
      this.form.get('name')?.valueChanges.subscribe(name => {
        if (name && !this.form.get('slug')?.dirty) {
          const slug = name.toLowerCase()
            .replace(/[^a-z0-9\s]/g, '')
            .replace(/\s+/g, '')
            .substring(0, 30);
          this.form.get('slug')?.setValue('dr' + slug, { emitEvent: true });
        }
      });
    }

    // In edit mode, if slug is unchanged don't flag it as taken
    this.form.get('slug')?.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(slug => {
        if (!slug || !/^[a-z0-9\-]+$/.test(slug)) {
          this.slugAvailable.set(null);
          return;
        }
        // If editing and slug hasn't changed, no need to check
        if (this.editMode && slug === this.doctor?.slug) {
          this.slugAvailable.set(true);
          return;
        }
        this.checkingSlug.set(true);
        this.adminService.checkSlug(slug, this.editMode ? this.doctor!.id : undefined).subscribe({
          next: (r) => { this.slugAvailable.set(r.available); this.checkingSlug.set(false); },
          error: () => this.checkingSlug.set(false)
        });
      });

    // In edit mode, trigger an initial slug check to pre-populate availability
    if (this.editMode && this.doctor?.slug) {
      this.slugAvailable.set(true);
    }
  }

  get canSubmit(): boolean {
    return this.form.valid && !this.loading() && this.slugAvailable() !== false;
  }

  submit(): void {
    if (!this.canSubmit) return;
    this.loading.set(true);

    const { name, specialization, slug, email, address, pincode, isActive } = this.form.value;

    if (this.editMode) {
      this.adminService.updateDoctor(this.doctor!.id, {
        name: name!,
        specialization: specialization!,
        slug: slug!,
        address: address || undefined,
        pincode: pincode || undefined,
        isActive: isActive!
      }).subscribe({
        next: (updated) => {
          this.loading.set(false);
          this.dialogRef.close(updated);
        },
        error: (err) => {
          this.snackBar.open(err?.error?.error ?? 'Failed to update doctor', 'OK', { duration: 4000 });
          this.loading.set(false);
        }
      });
    } else {
      this.adminService.createDoctor({
        name: name!,
        specialization: specialization!,
        slug: slug!,
        email: email!,
        address: address || undefined,
        pincode: pincode || undefined
      }).subscribe({
        next: (doctor) => {
          this.loading.set(false);
          this.dialogRef.close(doctor);
        },
        error: (err) => {
          this.snackBar.open(err?.error?.error ?? 'Failed to create doctor', 'OK', { duration: 4000 });
          this.loading.set(false);
        }
      });
    }
  }
}
