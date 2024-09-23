import {Component, OnInit} from '@angular/core'
import { Router } from '@angular/router';
import { DoctorAdminService } from '../../services/doctoradmin.service';
import { DoctorSession } from '../../models/doctor-session.model';
import { SessionStatus } from '../../models/session-status.model';
import { DoctorQueueNumber } from '../../models/doctor-queue-number.model';
import { StorageService } from '../../services/storage.service';

@Component({
    selector: 'doctor-admin',
    templateUrl: './doctor-admin.component.html'
})
export class DoctorAdminComponent implements OnInit {
    doctorSession?:DoctorSession;
    sessionStatus?:string;
    currentQueueNumber?: DoctorQueueNumber;
    totalNumbers = 0;
    currentNumber : any;
    tracking : any;
    constructor(private doctorAdminService: DoctorAdminService, 
      private storageService: StorageService,
      private router: Router){
    }

    ngOnInit(){
      if(this.storageService.isLoggedIn()){
        this.doctorAdminService.getSessionStatus().subscribe(data => {
          this.sessionStatus = data.toString();
        });
      }
      else
      {
        this.router.navigate(['/login']);
      }
    }
    
    startSession() {
      this.doctorAdminService.startSession().subscribe(data => {
        this.doctorSession = data;
        if(this.doctorSession?.status?.toString() == "0"){
          this.sessionStatus = SessionStatus.started;
          this.startTrackingTotalNumber();
        }
      });
    }

    pauseSession() {
      this.doctorAdminService.pauseSession().subscribe(data => {
        this.sessionStatus = data.toString();
      });
    }

    endSession() {
      this.doctorAdminService.endSession().subscribe(data => {
        if (data.toString() == "Ended"){
          this.sessionStatus = "";
          this.stopTrackingTotalNumber();
        }
      });
    }

    moveNumber() {
      this.doctorAdminService.moveNumber().subscribe(data => {
        this.currentQueueNumber = data;
      });
    }
    
    startTrackingTotalNumber() {
      if (this.sessionStatus == SessionStatus.started || SessionStatus.paused){
        this.tracking = setInterval(() => {
          this.getTotalNumber()
        }, 10000);
      }
    }
    stopTrackingTotalNumber() {
        clearInterval(this.tracking);
        this.tracking = null;
    }
    getTotalNumber(){
      this.doctorAdminService.getTotalNumbers().subscribe(data => {
        this.totalNumbers = parseInt(data.toString());
      });
    }
}