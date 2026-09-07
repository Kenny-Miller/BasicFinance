import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InstitutionAvatar } from './institution-avatar';

@Component({
  selector: 'app-test-host',
  template: '<app-institution-avatar name="Bank of Test" />',
  imports: [InstitutionAvatar],
})
class TestHost {}

describe('InstitutionAvatar', () => {
  let component: InstitutionAvatar;
  let fixture: ComponentFixture<TestHost>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHost],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();
    component = fixture.debugElement.children[0].componentInstance as InstitutionAvatar;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render initials for the first two words', () => {
    expect(component.initials()).toBe('BO');
  });
});
