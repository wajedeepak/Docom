import {Component} from '@angular/core'
import { DoctorAdminService } from '../../services/doctoradmin.service';
import { PatientNumber } from '../../models/patient-number.model';


@Component({
  selector: 'app-patient-page',
  templateUrl: './patient-page.component.html',
  styleUrls: ['./patient-page.component.css']
})
export class PatientPageComponent {
  showNumbers = false;
  showError = false;
  patientNumber? : PatientNumber;
  isValidInput = true;
  error = 'Cannot get number now';
 
  constructor(private doctorAdminService: DoctorAdminService){
  }
  
  getNumber(patientName:string, patientContact:string) {
    if(this.isValid(patientName, patientContact))
        this.doctorAdminService.getNumber(patientName, patientContact).subscribe(data => {
          this.patientNumber = data;
          if (this.patientNumber != null){
            this.showNumbers = true;
            this.startTrackingCurrentNumber();
            this.showError = false;
          }
          else{
            this.error = 'Cannot get number now';
            this.showNumbers = false;
            this.showError = true;
          }
        });
    }

  isValid(patientName:string, patientContact:string){
    this.isValidInput = true;
    this.showError = false;
    if(patientName == '' || 
      patientContact == '' ||  
      patientContact.length != 10 ||
      isNaN(parseInt(patientContact))){
      this.isValidInput = false;
      this.error = 'Please give correct name and mobile number';
      this.showError = true;
      this.showNumbers = false;
    }

  return this.isValidInput;
  }

  startTrackingCurrentNumber() {
    setInterval(() => {
            this.doctorAdminService.getCurrentNumber().subscribe(data => {
              if(this.patientNumber != undefined){
                this.patientNumber.currentNumber = data.toString();
              }
            });
      }, 10000);
  }
}
