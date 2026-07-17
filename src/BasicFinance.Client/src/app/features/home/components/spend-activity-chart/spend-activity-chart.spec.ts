import { BreakpointObserver } from '@angular/cdk/layout';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import * as echarts from 'echarts/core';
import { provideEchartsCore } from 'ngx-echarts';
import { of } from 'rxjs';
import { ThemeService } from '../../../../core/theme/theme.service';
import { SpendActivityChart } from './spend-activity-chart';

describe('SpendActivityChart', () => {
  let component: SpendActivityChart;
  let fixture: ComponentFixture<SpendActivityChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpendActivityChart],
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

    fixture = TestBed.createComponent(SpendActivityChart);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('theme', 'light');
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
