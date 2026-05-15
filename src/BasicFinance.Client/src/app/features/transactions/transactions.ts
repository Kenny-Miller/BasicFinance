import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-transactions',
  imports: [HlmCardImports],
  templateUrl: './transactions.html',
  styleUrl: './transactions.css',
})
export class Transactions {}
