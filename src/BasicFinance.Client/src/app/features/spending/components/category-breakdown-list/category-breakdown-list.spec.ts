import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CategoryBreakdownList } from './category-breakdown-list';

describe('CategoryBreakdownList', () => {
  let component: CategoryBreakdownList;
  let fixture: ComponentFixture<CategoryBreakdownList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryBreakdownList],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryBreakdownList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
