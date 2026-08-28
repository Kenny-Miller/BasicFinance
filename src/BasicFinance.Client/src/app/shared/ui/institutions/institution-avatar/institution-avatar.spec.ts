import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InstitutionAvatar } from './institution-avatar';

describe('InstitutionAvatar', () => {
  let component: InstitutionAvatar;
  let fixture: ComponentFixture<InstitutionAvatar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InstitutionAvatar]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InstitutionAvatar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
