import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

type LoginStep = 'email' | 'otp' | 'password' | 'forgot-email' | 'forgot-code' | 'forgot-password';
type AuthMode = 'otp' | 'password';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatInputModule, MatFormFieldModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatIconModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  step = signal<LoginStep>('email');
  authMode = signal<AuthMode>('otp');
  email = '';
  password = '';
  otp = '';
  resetCode = '';
  loading = signal(false);
  isPasswordSetRequired = signal(false);
  showPassword = signal(false);
  isResettingPassword = signal(false);

  toggleAuthMode(): void {
    this.authMode.set(this.authMode() === 'otp' ? 'password' : 'otp');
    this.resetForm();
  }

  togglePasswordVis(): void {
    this.showPassword.set(!this.showPassword());
  }

  requestOtp(): void {
    if (!this.email || this.loading()) return;
    this.loading.set(true);

    this.auth.requestOtp(this.email).subscribe({
      next: () => {
        this.step.set('otp');
        this.loading.set(false);
      },
      error: (err) => {
        const msg = err?.error?.error ?? 'Could not send OTP. Check your email.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
        this.loading.set(false);
      }
    });
  }

  verifyOtp(): void {
    if (!this.otp || this.loading()) return;
    this.loading.set(true);

    this.auth.verifyOtp(this.email, this.otp).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.isPasswordSetRequired.set(false);
        // Check if user has set password before
        if (!response.hasPasswordSet) {
          // Prompt user to set password
          this.step.set('password');
          this.snackBar.open('Please set a password to secure your account', 'OK', { duration: 4000 });
        } else {
          this.navigateAfterLogin(response.role);
        }
      },
      error: (err) => {
        const msg = err?.error?.error ?? 'Invalid or expired OTP.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
        this.loading.set(false);
      }
    });
  }

  loginWithPassword(): void {
    if (!this.password || this.loading()) return;
    this.loading.set(true);
    this.isPasswordSetRequired.set(false);

    this.auth.loginWithPassword(this.email, this.password).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.navigateAfterLogin(response.role);
      },
      error: (err) => {
        this.loading.set(false);
        if (err?.status === 422) {
          // Password not set - show option to use OTP
          this.isPasswordSetRequired.set(true);
          this.snackBar.open('Password not set. Please use OTP to set one.', 'OK', { duration: 4000 });
          this.authMode.set('otp');
          this.step.set('email');
        } else {
          const msg = err?.error?.error ?? 'Invalid email or password.';
          this.snackBar.open(msg, 'OK', { duration: 4000 });
        }
      }
    });
  }

  setPasswordAfterOtp(): void {
    if (!this.password || this.loading()) return;
    if (this.password.length < 6) {
      this.snackBar.open('Password must be at least 6 characters', 'OK', { duration: 3000 });
      return;
    }
    this.loading.set(true);

    this.auth.setPassword(this.email, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        this.snackBar.open('Password set successfully! Logging in...', 'OK', { duration: 3000 });
        // Auto-login with the OTP that was just verified
        // Since user already verified OTP, we can proceed to dashboard
        const user = this.auth.user();
        if (user) {
          this.navigateAfterLogin(user.role);
        }
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.error ?? 'Failed to set password.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      }
    });
  }

  startPasswordReset(): void {
    this.isResettingPassword.set(true);
    this.step.set('forgot-email');
    this.email = '';
    this.resetCode = '';
    this.password = '';
  }

  requestPasswordReset(): void {
    if (!this.email || this.loading()) return;
    this.loading.set(true);

    this.auth.requestPasswordReset(this.email).subscribe({
      next: () => {
        this.loading.set(false);
        this.step.set('forgot-code');
        this.snackBar.open('Reset code sent to your email', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.error ?? 'Could not send reset code. Check your email.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      }
    });
  }

  resetPasswordWithOtp(): void {
    if (!this.resetCode || !this.password || this.loading()) return;
    if (this.resetCode.length !== 6) {
      this.snackBar.open('Reset code must be 6 digits', 'OK', { duration: 3000 });
      return;
    }
    if (this.password.length < 6) {
      this.snackBar.open('Password must be at least 6 characters', 'OK', { duration: 3000 });
      return;
    }
    this.loading.set(true);

    this.auth.resetPasswordWithOtp(this.email, this.resetCode, this.password).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.snackBar.open('Password reset successful! Logging in...', 'OK', { duration: 3000 });
        this.navigateAfterLogin(response.role);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.error ?? 'Invalid reset code or password. Please try again.';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      }
    });
  }

  onSubmit(): void {
    if (this.step() === 'forgot-email') {
      if (!this.email) {
        this.snackBar.open('Please enter your email', 'OK', { duration: 3000 });
        return;
      }
      this.requestPasswordReset();
      return;
    }

    if (this.step() === 'forgot-code') {
      if (!this.resetCode || !this.password) {
        this.snackBar.open('Please enter reset code and new password', 'OK', { duration: 3000 });
        return;
      }
      this.resetPasswordWithOtp();
      return;
    }

    if (!this.email) {
      this.snackBar.open('Please enter your email', 'OK', { duration: 3000 });
      return;
    }

    if (this.step() === 'email') {
      if (this.authMode() === 'password') {
        // Directly login with password (no second form needed)
        this.loginWithPassword();
      } else {
        // Request OTP
        this.requestOtp();
      }
    } else if (this.step() === 'otp') {
      this.verifyOtp();
    } else if (this.step() === 'password' && this.authMode() === 'otp') {
      // Setting password after OTP
      this.setPasswordAfterOtp();
    }
  }

  goBack(): void {
    if (this.step() === 'forgot-code') {
      // Back from reset code
      this.step.set('forgot-email');
      this.resetCode = '';
      this.password = '';
    } else if (this.step() === 'forgot-email') {
      // Back from forgot email to main login
      this.isResettingPassword.set(false);
      this.step.set('email');
      this.email = '';
      this.resetCode = '';
      this.password = '';
    } else if (this.step() === 'password' && this.authMode() === 'otp') {
      // Back from password setup after OTP
      this.step.set('email');
      this.password = '';
    } else if (this.step() === 'otp') {
      // Back from OTP
      this.step.set('email');
      this.otp = '';
    }
  }

  private navigateAfterLogin(role: string): void {
    if (role === 'Admin') {
      this.router.navigate(['/admin']);
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  private resetForm(): void {
    this.step.set('email');
    this.email = '';
    this.password = '';
    this.otp = '';
    this.resetCode = '';
    this.isPasswordSetRequired.set(false);
    this.showPassword.set(false);
    this.isResettingPassword.set(false);
  }
}
