import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { Paginator } from './paginator';

@Component({
  selector: 'app-test-host',
  template:
    '<app-paginator [page]="page()" [pageSize]="pageSize()" [totalCount]="totalCount()" />',
  imports: [Paginator],
})
class TestHost {
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
}

describe('Paginator', () => {
  let fixture: ComponentFixture<TestHost>;
  let host: TestHost;
  let paginator: Paginator;

  const setup = (overrides: { page?: number; pageSize?: number; totalCount?: number } = {}): void => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TestHost],
    });

    fixture = TestBed.createComponent(TestHost);
    host = fixture.componentInstance;
    host.page.set(overrides.page ?? 1);
    host.pageSize.set(overrides.pageSize ?? 10);
    host.totalCount.set(overrides.totalCount ?? 0);
    fixture.detectChanges();
    paginator = fixture.debugElement.children[0].componentInstance as Paginator;
  };

  it('should create', () => {
    setup();
    expect(paginator).toBeTruthy();
  });

  describe('pageWindow', () => {
    it('should show every page when there are seven or fewer', () => {
      setup({ totalCount: 60 });
      expect(paginator.pageWindow()).toEqual([1, 2, 3, 4, 5, 6]);
    });

    it('should use ellipses around the current page when there are more than seven', () => {
      setup({ totalCount: 1000, page: 50 });
      expect(paginator.pageWindow()).toEqual([1, null, 49, 50, 51, null, 100]);
    });

    it('should omit the leading ellipsis near the start', () => {
      setup({ totalCount: 1000, page: 1 });
      expect(paginator.pageWindow()).toEqual([1, 2, null, 100]);
    });

    it('should omit the trailing ellipsis near the end', () => {
      setup({ totalCount: 1000, page: 99 });
      expect(paginator.pageWindow()).toEqual([1, null, 98, 99, 100]);
    });
  });

  describe('navigation state', () => {
    it('should disable previous on the first page', () => {
      setup({ totalCount: 100 });
      expect(paginator.previousDisabled()).toBe(true);
      expect(paginator.nextDisabled()).toBe(false);
    });

    it('should disable next on the last page', () => {
      setup({ totalCount: 100, page: 10 });
      expect(paginator.nextDisabled()).toBe(true);
      expect(paginator.previousDisabled()).toBe(false);
    });
  });

  describe('row range', () => {
    it('should be zero when there are no items', () => {
      setup();
      expect(paginator.rangeStart()).toBe(0);
      expect(paginator.rangeEnd()).toBe(0);
    });

    it('should cap the end of the last partial page', () => {
      setup({ totalCount: 25, page: 3 });
      expect(paginator.rangeStart()).toBe(21);
      expect(paginator.rangeEnd()).toBe(25);
    });
  });

  describe('changePage', () => {
    it('should emit the requested page', () => {
      setup({ totalCount: 60 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');

      paginator.changePage(4);

      expect(emit).toHaveBeenCalledWith(4);
    });

    it('should emit when a page button is clicked', () => {
      setup({ totalCount: 60 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');
      const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
      const pageButton = buttons.find(button => button.textContent?.trim() === '3');

      pageButton?.click();

      expect(emit).toHaveBeenCalledWith(3);
    });
  });

  describe('selectPage', () => {
    it('should emit the selected page and clear the selector', () => {
      setup({ totalCount: 500 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');
      paginator.pageSelector.set(42);

      paginator.selectPage();

      expect(emit).toHaveBeenCalledWith(42);
      expect(paginator.pageSelector()).toBeNull();
    });

    it('should clamp a target above the last page to the last page', () => {
      setup({ totalCount: 100 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');
      paginator.pageSelector.set(999);

      paginator.selectPage();

      expect(emit).toHaveBeenCalledWith(10);
    });

    it('should clamp a target below the first page to the first page', () => {
      setup({ totalCount: 100 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');
      paginator.pageSelector.set(-5);

      paginator.selectPage();

      expect(emit).toHaveBeenCalledWith(1);
    });

    it('should do nothing when the selector is empty', () => {
      setup({ totalCount: 100 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');

      paginator.selectPage();

      expect(emit).not.toHaveBeenCalled();
    });

    it('should submit on Enter', () => {
      setup({ totalCount: 100 });
      const emit = vi.spyOn(paginator.pageChange, 'emit');
      const preventDefault = vi.fn();
      paginator.pageSelector.set(7);

      paginator.pageSelectorKeydown({ key: 'Enter', preventDefault } as unknown as KeyboardEvent);

      expect(preventDefault).toHaveBeenCalled();
      expect(emit).toHaveBeenCalledWith(7);
    });
  });

  describe('selectPageSize', () => {
    it('should emit the chosen size', () => {
      setup();
      const emit = vi.spyOn(paginator.pageSizeChange, 'emit');

      paginator.selectPageSize(50);

      expect(emit).toHaveBeenCalledWith(50);
    });

    it('should ignore null', () => {
      setup();
      const emit = vi.spyOn(paginator.pageSizeChange, 'emit');

      paginator.selectPageSize(null);

      expect(emit).not.toHaveBeenCalled();
    });
  });

  describe('template', () => {
    it('should show an ellipsis span where the window has a gap', () => {
      setup({ totalCount: 1000, page: 50 });
      const ellipses = fixture.nativeElement.querySelectorAll('nav span[class*="text-gray-400"]');
      expect(ellipses.length).toBe(2);
    });
  });
});
