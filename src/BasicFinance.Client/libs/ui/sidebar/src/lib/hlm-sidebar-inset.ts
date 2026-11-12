import { Directive } from '@angular/core';
import { classes, hlm } from '@spartan-ng/helm/utils';

@Directive({
	selector: 'main[hlmSidebarInset]',
	host: { 'data-slot': 'sidebar-inset' },
})
export class HlmSidebarInset {
	constructor() {
		classes(() =>
			hlm(
				'bg-background relative flex w-full flex-1 flex-col',
				'md:peer-data-[variant=inset]:m-2.5 md:peer-data-[variant=inset]:ms-0 md:peer-data-[variant=inset]:rounded-sheet md:peer-data-[variant=inset]:border md:peer-data-[variant=inset]:border-sheet-border md:peer-data-[variant=inset]:bg-sheet md:peer-data-[variant=inset]:shadow-sheet md:peer-data-[variant=inset]:overflow-hidden md:peer-data-[variant=inset]:px-10 md:peer-data-[variant=inset]:pt-7 md:peer-data-[variant=inset]:pb-8',
				'md:peer-data-[variant=inset]:peer-data-[state=collapsed]:ms-2.5',
			),
		);
	}
}
