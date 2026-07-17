import { BreakpointObserver } from '@angular/cdk/layout';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import * as echarts from 'echarts/core';
import { provideEchartsCore } from 'ngx-echarts';
import { of } from 'rxjs';
import { ThemeService } from '../../../../core/theme/theme.service';
import { CategoryPieChart } from './category-pie-chart';

describe('CategoryPieChart', () => {
  let component: CategoryPieChart;
  let fixture: ComponentFixture<CategoryPieChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryPieChart],
      providers: [
        provideEchartsCore({ echarts }),
        {
          provide: ThemeService,
          useValue: {
            appTheme: { getValue: () => 'light' },
            getAppColor: () => '#000000',
          },
        },
        {
          provide: BreakpointObserver,
          useValue: {
            observe: () => of({ matches: false }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryPieChart);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('theme', 'light');
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
