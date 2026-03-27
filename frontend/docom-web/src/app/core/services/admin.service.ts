import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DoctorDto, CreateDoctorDto, PagedResult } from '../models/doctor.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getDoctors(page = 1, pageSize = 20): Observable<PagedResult<DoctorDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<DoctorDto>>(`${this.base}/doctors`, { params });
  }

  getDoctor(id: number): Observable<DoctorDto> {
    return this.http.get<DoctorDto>(`${this.base}/doctors/${id}`);
  }

  createDoctor(dto: CreateDoctorDto): Observable<DoctorDto> {
    return this.http.post<DoctorDto>(`${this.base}/doctors`, dto);
  }

  updateDoctor(id: number, dto: Partial<CreateDoctorDto> & { isActive: boolean }): Observable<DoctorDto> {
    return this.http.put<DoctorDto>(`${this.base}/doctors/${id}`, dto);
  }

  checkSlug(slug: string, excludeId?: number): Observable<{ slug: string; available: boolean }> {
    let params = new HttpParams().set('slug', slug);
    if (excludeId !== undefined) params = params.set('excludeId', excludeId);
    return this.http.get<{ slug: string; available: boolean }>(
      `${this.base}/doctors/check-slug`, { params }
    );
  }
}
