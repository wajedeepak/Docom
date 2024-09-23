import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

//const AUTH_API = 'http://192.168.1.184/api/Auth/';
//const AUTH_API = 'https://docom.in/api/Auth/';
const AUTH_API = 'https://localhost:7256/Auth/';

const httpOptions = {
  headers: new HttpHeaders({ 'Authorization': 'Bearer your-token',
                              'Custom-Header': 'CustomHeaderValue',
                              'Content-Type': 'application/json' })
};

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(username: string, password: string): Observable<any> {
    return this.http.post(
      AUTH_API + 'signin',
      {
        username,
        password,
      },
      httpOptions
    );
  }

  getRequest(): Observable<any> {
    return this.http.get(AUTH_API + 'GetRequest', httpOptions);
  }

  register(username: string, email: string, password: string): Observable<any> {
    return this.http.post(
      AUTH_API + 'signup',
      {
        username,
        email,
        password,
      },
      httpOptions
    );
  }

  logout(): Observable<any> {
    return this.http.post(AUTH_API + 'signout', { }, httpOptions);
  }
}
