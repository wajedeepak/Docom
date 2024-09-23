import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'docom';
  isLoggedIn = false;

  ngOnInit(): void {
    // if (this.cacheService.load("auth-token")) {
    //   this.isLoggedIn = true;
    // }
  }
}


