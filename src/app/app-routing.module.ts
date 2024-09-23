import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { DoctorAdminComponent } from './views/doctor-admin/doctor-admin.component';
import { PatientPageComponent } from './views/patient-page/patient-page.component';
import { LoginComponent } from './views/login/login.component';


const routes:Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'doctoradmin', component: DoctorAdminComponent},
    { path: 'patientpage', component: PatientPageComponent},
    { path: '', redirectTo: '/patientpage', pathMatch: 'full' },  // Example of a redirect
  { path: '**', redirectTo: '/patientpage' }  // Wildcard route
]
@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
