import {Injectable} from '@angular/core'
import { HttpClient, HttpParams} from '@angular/common/http';

@Injectable()
export class DoctorAdminService {
  //private apiUrl = 'https://docom.in/SessionManager'
  //private apiUrl = 'https://localhost:7256/SessionManager'
  private apiUrl = 'http://192.168.1.184/api/SessionManager'

  //session: Session;
  constructor(private http: HttpClient){ }

  getSessionStatus(){
    return this.http.post(this.apiUrl + '/GetSessionStatus', null);
  }

  startSession(){
    return this.http.post(this.apiUrl + '/StartSession', null);
  }

  pauseSession(){
    return this.http.post(this.apiUrl + '/PauseSession', null);
  }

  endSession(){
    return this.http.post(this.apiUrl + '/EndSession', null);
  }

  moveNumber(){
    return this.http.post(this.apiUrl + '/MoveNumber', null);
  }

  getTotalNumbers(){
    return this.http.post(this.apiUrl + '/GetTotalNumbers', null);
  }

  getNumber(patientName: string, patientContact: string){
    const params = new HttpParams()
                    .set('patientName', patientName)
                    .set('patientContact', patientContact)
    return this.http.post(this.apiUrl + '/GetNumber', null, { params });
  }

  getCurrentNumber(){
    return this.http.post(this.apiUrl + '/GetCurrentNumber', null);
  }
    
}

