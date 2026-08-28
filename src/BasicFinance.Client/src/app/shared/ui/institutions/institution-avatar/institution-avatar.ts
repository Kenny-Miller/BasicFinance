import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-institution-avatar',
  imports: [],
  templateUrl: './institution-avatar.html',
  styleUrl: './institution-avatar.css',
})
export class InstitutionAvatar {
  readonly name = input.required<string>();

  readonly initials = computed(() =>
    this.name()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((word) => word[0]?.toUpperCase() ?? '')
      .join(''),
  );
}
