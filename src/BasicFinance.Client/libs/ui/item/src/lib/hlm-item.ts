import { type BooleanInput } from '@angular/cdk/coercion';
import { booleanAttribute, Directive, input } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';
import { cva, type VariantProps } from 'class-variance-authority';
import { injectHlmItemConfig } from './hlm-item-token';

const itemVariants = cva(
	'data-[active=true]:focus:bg-accent data-[active=true]:hover:bg-accent data-[active=true]:bg-accent/50 data-[active=true]:text-accent-foreground group/item [a]:hover:bg-accent/50 focus-visible:border-ring focus-visible:ring-ring/50 flex flex-wrap items-center rounded-md border border-transparent text-sm transition-colors duration-100 outline-none focus-visible:ring-[3px] [a]:transition-colors',
	{
		variants: {
			variant: {
				default: 'bg-transparent',
				outline: 'border-border',
				muted: 'bg-muted/50',
			},
			size: {
				default: 'gap-4 p-4',
				sm: 'gap-2.5 px-4 py-3',
			},
		},
		defaultVariants: {
			variant: 'default',
			size: 'default',
		},
	},
);

export type ItemVariants = VariantProps<typeof itemVariants>;

@Directive({
	selector: 'div[hlmItem], a[hlmItem]',
	host: {
		'data-slot': 'item',
		'[attr.data-variant]': 'variant()',
		'[attr.data-size]': 'size()',
		'[attr.data-active]': 'isActive()',
	},
})
export class HlmItem {
	private readonly _config = injectHlmItemConfig();
	public readonly variant = input<ItemVariants['variant']>(this._config.variant);
	public readonly size = input<ItemVariants['size']>(this._config.size);
	public readonly isActive = input<boolean, BooleanInput>(false, { transform: booleanAttribute });

	constructor() {
		classes(() => itemVariants({ variant: this.variant(), size: this.size() }));
	}
}
