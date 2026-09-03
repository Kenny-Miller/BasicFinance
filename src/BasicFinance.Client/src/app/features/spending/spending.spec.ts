import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PieChart } from 'echarts/charts';
import { LegendComponent, TooltipComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { provideEchartsCore } from 'ngx-echarts';
import { ThemeService } from '../../core/theme/theme.service';
import { TimePeriod } from '../../shared/data/time-period';
import { Spending } from './spending';
import { SpendingService } from './spending-service';

echarts.use([PieChart, LegendComponent, TooltipComponent, CanvasRenderer]);

describe('Spending', () => {
  let component: Spending;
  let fixture: ComponentFixture<Spending>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Spending],
      providers: [
        provideEchartsCore({ echarts }),
        {
          provide: SpendingService,
          useValue: {
            selectedPeriod: signal<TimePeriod>('Monthly'),
            loading: () => false,
            error: () => undefined,
            data: () => ({
              periodStartDate: '',
              periodEndDate: '',
              totalSpend: 0,
              totalIncome: 0,
              spendingActivityByCategory: {},
            }),
            selectPeriod: () => undefined,
            refetchAll: () => undefined,
          },
        },
        {
          provide: ThemeService,
          useValue: {
            appTheme: () => 'light',
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Spending);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
