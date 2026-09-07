import { Component, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-info-card',
  imports: [HlmCardImports],
  templateUrl: './info-card.html',
  styleUrl: './info-card.css',
})
export class InfoCard {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly badge = input<string>('');
  readonly caption = input<string>('');
  readonly tone = input<'default' | 'good' | 'bad'>('default');
}
