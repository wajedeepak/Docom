import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { AppRoutingModule } from './app-routing.module';

import { AppComponent } from './app.component';
import { HttpClientModule } from '@angular/common/http';
import { DoctorAdminService} from './services/doctoradmin.service'
import { DoctorAdminComponent } from './views/doctor-admin/doctor-admin.component';
import { PatientPageComponent } from './views/patient-page/patient-page.component';
import { LoginComponent } from './views/login/login.component';
//import { httpInterceptorProviders } from './_helpers/http.interceptor';

@NgModule({
  declarations: [
    AppComponent,
    DoctorAdminComponent,
    PatientPageComponent,
    LoginComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [DoctorAdminService],
  bootstrap: [AppComponent]
})
export class AppModule { }
