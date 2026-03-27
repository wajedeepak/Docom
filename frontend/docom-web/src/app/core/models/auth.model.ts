export interface AuthResponse {
  token: string;
  email: string;
  name: string;
  role: 'Admin' | 'Doctor' | 'Receptionist';
  doctorId: number | null;
  doctorSlug: string | null;
  hasPasswordSet?: boolean;  // Indicates if user has set a password
}

export interface CurrentUser {
  token: string;
  email: string;
  name: string;
  role: 'Admin' | 'Doctor' | 'Receptionist';
  doctorId: number | null;
  doctorSlug: string | null;
  hasPasswordSet?: boolean;
}
