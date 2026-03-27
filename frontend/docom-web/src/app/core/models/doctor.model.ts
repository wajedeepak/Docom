export interface DoctorDto {
  id: number;
  slug: string;
  name: string;
  specialization: string;
  address?: string;
  pincode?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateDoctorDto {
  name: string;
  specialization: string;
  slug: string;
  email: string;
  address?: string;
  pincode?: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}
