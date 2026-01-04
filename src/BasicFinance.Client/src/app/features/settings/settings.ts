import { Component } from '@angular/core';
import { BrnDialogImports } from '@spartan-ng/brain/dialog';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';

@Component({
  selector: 'app-settings',
  imports: [HlmCardImports, HlmDialogImports, BrnDialogImports, HlmButtonImports],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {}
