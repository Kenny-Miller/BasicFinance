import { JsonPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';

@Component({
  selector: 'app-home',
  imports: [JsonPipe],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  oauthService = inject(OAuthService);

  user = signal({});

  async ngOnInit() {
    const user = await this.oauthService.loadUserProfile();
    console.log(user)
    this.user.set(user);
  }
}
