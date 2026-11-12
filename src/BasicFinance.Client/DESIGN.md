# Basic Finance — Design Guide (G3 · Open Field)

The visual language for `BasicFinance.Client`. Read this before building or restyling any screen. This file is self-contained: every value and rule needed to build the UI is written here.

---

## 1. Principles

1. **Open field, not cards.** Each page has one inset _sheet_. Inside it, content is grouped by 1px hairline rules, never by boxes, background tints, shadows or coloured left borders.
2. **Black and white; colour only carries meaning.** Text is `foreground` on `sheet`/`frame`, and solid controls use `ink`. Hue appears only in:
   - charts (`chart-*`, `category-*`);
   - gain/loss (`positive`, `negative`);
   - warnings (`warning`);
   - institution avatars.
3. **Light numbers, quiet labels.** Figures are large and light (300 weight). Labels are small, uppercase and widely spaced. Weight is saved for the active nav item and selected options.
4. **Always compare to last period.** Each figure gets a delta pill plus last period's value. Charts draw last period dashed.
5. **Show the shape while loading.** Use Skeleton on first load and Refreshing during a sync. Never put a spinner in place of content.

---

## 2. Tokens

The CSS variables below are the single source of truth: `src/styles.css` holds them and everything else (Tailwind utilities, helm components, charts) reads from there. They are mapped onto spartan/shadcn variable names so the `libs/ui` helm components pick them up unchanged, and extra G3 names are added for what helm has no slot for.

### 2.1 `src/styles.css`

```css
@import url('https://fonts.googleapis.com/css2?family=Hanken+Grotesk:wght@300;400;500;600;700&display=swap');

:root {
  color-scheme: light;
  --font-sans: 'Hanken Grotesk', system-ui, -apple-system, 'Segoe UI', sans-serif;
  --radius: 0.5rem;

  /* G3 surfaces */
  --frame: #f0f0ee; /* behind everything; sidebar sits on it */
  --sheet: #fdfdfc; /* the inset content panel */
  --sheet-border: #e2e2e0;
  --nav-foreground: #3d3d3b; /* inactive nav labels — never greyer */
  --ink: #1b1b1b; /* solid controls */
  --ink-foreground: #fdfdfc;
  --skeleton: #efefed;
  --horizon: rgb(0 0 0 / 16%); /* chart baseline */

  /* meaning */
  --positive: #2f7a5f;
  --positive-tint: rgb(47 122 95 / 8%);
  --negative: #b44a3f;
  --negative-tint: rgb(180 74 63 / 8%);
  --warning: #8a6116;
  --warning-tint: rgb(201 138 51 / 12%);

  /* charts */
  --chart-current: #3e8c78;
  --chart-previous: #9a9a96;
  --chart-spend: #c98a33;
  --category-1: #3e8c78;
  --category-2: #c98a33;
  --category-3: #5b7fa6;
  --category-4: #b5605a;
  --category-5: #8a77a8;
  --category-6: #8a9a4a;
  --category-7: #5a9aa6;
  --category-8: #9a9a96;

  /* spartan / shadcn slots */
  --background: var(--sheet);
  --foreground: #1b1b1b;
  --card: var(--sheet);
  --card-foreground: var(--foreground);
  --popover: var(--sheet);
  --popover-foreground: var(--foreground);
  --primary: var(--ink);
  --primary-foreground: var(--ink-foreground);
  --secondary: #f3f3f1;
  --secondary-foreground: var(--foreground);
  --muted: #f3f3f1;
  --muted-foreground: #62625f;
  --accent: #f3f3f1;
  --accent-foreground: var(--foreground);
  --destructive: var(--negative);
  --border: #e4e4e2;
  --input: #e4e4e2;
  --ring: #1b1b1b;
  --sidebar: var(--frame);
  --sidebar-foreground: var(--nav-foreground);
  --sidebar-primary: var(--ink);
  --sidebar-primary-foreground: var(--ink-foreground);
  --sidebar-accent: var(--sheet); /* active nav pill */
  --sidebar-accent-foreground: var(--foreground);
  --sidebar-border: var(--sheet-border);
  --sidebar-ring: var(--ring);

  --shadow-sheet: 0 1px 2px rgb(0 0 0 / 3%), 0 8px 24px rgb(0 0 0 / 3.5%);
  --shadow-popover: 0 12px 32px rgb(0 0 0 / 14%), 0 2px 6px rgb(0 0 0 / 6%);
}

:root.dark {
  color-scheme: dark;
  --frame: #070707;
  --sheet: #101010;
  --sheet-border: #1f1f1f;
  --nav-foreground: #cfcfcc;
  --ink: #ececea;
  --ink-foreground: #101010;
  --skeleton: #1a1a1a;
  --horizon: rgb(255 255 255 / 16%);
  --positive: #84ccaa;
  --positive-tint: rgb(132 204 170 / 9%);
  --negative: #e48f84;
  --negative-tint: rgb(228 143 132 / 9%);
  --warning: #e2aa58;
  --warning-tint: rgb(226 170 88 / 10%);
  --chart-current: #79c9b1;
  --chart-previous: #77776f;
  --chart-spend: #e2aa58;
  --category-1: #79c9b1;
  --category-2: #e2aa58;
  --category-3: #8fb0d6;
  --category-4: #e48f84;
  --category-5: #b9a6d6;
  --category-6: #b8c77a;
  --category-7: #8cc6cf;
  --category-8: #77776f;
  --foreground: #ececea;
  --secondary: #171717;
  --muted: #171717;
  --muted-foreground: #a09f9b;
  --accent: #171717;
  --border: #222222;
  --input: #222222;
  --ring: #ececea;
  --shadow-sheet: none;
}

/* Expose G3 names as Tailwind utilities: bg-frame, bg-sheet, text-ink, text-positive, bg-category-3 … */
@theme inline {
  --color-frame: var(--frame);
  --color-sheet: var(--sheet);
  --color-sheet-border: var(--sheet-border);
  --color-nav-foreground: var(--nav-foreground);
  --color-ink: var(--ink);
  --color-ink-foreground: var(--ink-foreground);
  --color-skeleton: var(--skeleton);
  --color-horizon: var(--horizon);
  --color-positive: var(--positive);
  --color-positive-tint: var(--positive-tint);
  --color-negative: var(--negative);
  --color-negative-tint: var(--negative-tint);
  --color-warning: var(--warning);
  --color-warning-tint: var(--warning-tint);
  --color-chart-current: var(--chart-current);
  --color-chart-previous: var(--chart-previous);
  --color-chart-spend: var(--chart-spend);
  --color-category-1: var(--category-1);
  --color-category-2: var(--category-2);
  --color-category-3: var(--category-3);
  --color-category-4: var(--category-4);
  --color-category-5: var(--category-5);
  --color-category-6: var(--category-6);
  --color-category-7: var(--category-7);
  --color-category-8: var(--category-8);
  --shadow-sheet: var(--shadow-sheet);
  --shadow-popover: var(--shadow-popover);
}

@layer base {
  body {
    @apply bg-frame text-foreground antialiased;
    font-feature-settings: 'tnum';
  }
}
```

The Tailwind and spartan preset imports sit above this block. Dark mode stays a `.dark` class on `:root`.

### 2.2 Colour roles

| Role                    | Use                                                                  | Never                                               |
| ----------------------- | -------------------------------------------------------------------- | --------------------------------------------------- |
| `frame`                 | App background, sidebar                                              | Inside the sheet                                    |
| `sheet`                 | The content panel, popovers, the active nav pill                     | As a "card" inside the sheet                        |
| `foreground`            | Text, numbers, headings                                              | —                                                   |
| `nav-foreground`        | Inactive nav labels, row icons                                       | Body copy                                           |
| `muted-foreground`      | Section labels, captions, comparisons, axis text, inactive nav icons | Nav labels                                          |
| `muted`                 | Row hover/selected, empty bar tracks                                 | Section backgrounds                                 |
| `border`                | Every hairline                                                       | Thick (>1px) rules                                  |
| `ink`                   | App mark, primary button, toggles/checkboxes on, count badges, toast | Text colour                                         |
| `positive` / `negative` | Delta pills, income amounts, debt balances                           | Ordinary spending amounts (those stay `foreground`) |
| `warning`               | Over budget, due before payday, reconnect needed                     | Decoration                                          |
| `chart-*`, `category-*` | Chart marks, 6px category dots, budget bars                          | Text, backgrounds, buttons                          |

All text tokens reach at least 4.5:1 on `sheet` in both themes. Chart colours are graphics-only (3:1 or more).

### 2.3 Type

Hanken Grotesk, tabular figures everywhere.

| Style         | Tailwind                                                                      | Use                                 |
| ------------- | ----------------------------------------------------------------------------- | ----------------------------------- |
| hero          | `text-[52px] leading-[1.05] font-light tracking-[-0.04em]`                    | One headline number per page        |
| page-title    | `text-[28px] leading-8 font-medium tracking-[-0.03em]`                        | Sheet header, one noun              |
| metric        | `text-[30px] leading-[38px] font-light tracking-[-0.03em]`                    | Stat tiles, section figures         |
| body          | `text-sm`                                                                     | Default                             |
| body-strong   | `text-sm font-medium`                                                         | Row names, amounts, buttons         |
| nav-active    | `text-sm font-semibold`                                                       | Active nav item, selected option    |
| small         | `text-[13px] leading-[18px]`                                                  | Sync status, chips, segments, links |
| caption       | `text-xs text-muted-foreground`                                               | Comparisons, row meta               |
| section-label | `text-[11px] font-semibold uppercase tracking-[0.16em] text-muted-foreground` | Section and group labels            |
| day-label     | `text-[11px] font-semibold uppercase tracking-[0.08em] text-muted-foreground` | Day dividers                        |
| axis          | `text-[10px] tracking-[0.2em] text-muted-foreground`                          | Month labels under the horizon      |

For now apply these with standard Tailwind utilities inline (e.g. `text-sm`, `text-xs font-semibold uppercase tracking-widest`); the `@utility` wrappers and the exact off-scale values (`text-[28px]`, `text-[13px]`, `text-[11px]`) are deferred to FE-40 (G3 style pass).

### 2.4 Space, radius, elevation

| Token         | Value                                                                                                                                                          | Use                                            |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| hairline      | 1px                                                                                                                                                            | Gap of the hairline grid                       |
| sheet-inset   | 10px                                                                                                                                                           | Sheet margin (top, right, bottom)              |
| sheet-x       | 40px (`px-10`)                                                                                                                                                 | Sheet, section and list-row horizontal padding |
| section-y     | 26px top / 30px bottom                                                                                                                                         | Section padding                                |
| sidebar width | 252px                                                                                                                                                          | Desktop sidebar                                |
| radius        | 0 inside the sheet; `rounded-full` for controls; `rounded-lg` (8px) for inputs; `rounded-xl` (12px) for popovers and the toast; `rounded-[14px]` for the sheet |                                                |
| shadows       | `shadow-sheet` (light only), `shadow-popover`                                                                                                                  | Nothing else casts a shadow                    |

---

## 3. Layout

```
┌ frame ───────────────────────────────────────────────────────────┐
│ sidebar (252px)   ┌ sheet (inset 10px, r14, sheet-border) ──────┐ │
│  ● Basic Finance  │ Home  ● Synced 12 min ago        [Sync now] │ │
│  ▣ Home  (pill)   ├─────────────────────────────┬───────────────┤ │
│  ▢ Transactions   │ NET WORTH            1M … 1Y│ BALANCES      │ │
│  ▢ Spending       │ $37,053.63 ▲18.8%           │               │ │
│  ▢ Recurring      │ ──────────────── horizon    │               │ │
│  ▢ Budgets        ├──────────────┬──────────────┴───────────────┤ │
│  INSTITUTIONS     │ SPENDING     │ CASH FLOW                    │ │
│  (WF) Wells Fargo ├──────────────┴──────────────┬───────────────┤ │
│  + Add institution│ RECENT TRANSACTIONS         │ UPCOMING      │ │
│  ⚙ Settings       │                             │               │ │
│  ☾ Dark mode      └─────────────────────────────┴───────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

- **Shell** (`app.html`): the `bg-frame` grid `grid-cols-[252px_1fr]`. The `<main>` is the sheet: `m-2.5 ml-0 rounded-[14px] border border-sheet-border bg-sheet shadow-sheet px-10 pt-7 pb-8 relative overflow-hidden`.
- **Header:** page title, `SyncStatus`, then right-aligned actions (`ml-auto`). 20px below it the content starts.
- **Hairline grid:** `grid grid-cols-12 gap-px bg-border border-y border-border -mx-10`. Each child section is `bg-sheet px-10 pt-[26px] pb-[30px]`. The 1px gaps show the `border` colour and draw every rule.
- **Common spans:**

  | Page     | Rows (spans out of 12)                                                                   |
  | -------- | ---------------------------------------------------------------------------------------- |
  | Home     | Net worth 8 + Balances 4 · Spending 6 + Cash flow 6 · Recent transactions 8 + Upcoming 4 |
  | Settings | Settings nav 3 + content 9                                                               |

- **Lists inside a section** set the section to `p-0` and pad their own rows `px-10`, so row dividers reach the sheet edges.
- **Responsive:**
  - under 1100px: the sidebar hides and every span becomes 12;
  - phone: the sheet is the whole screen, with a bottom tab bar (Home, Transactions, Spending, Settings).

---

## 4. Components

Where a component already has a home in the app, its path is in brackets.

### Sidebar (`libs/ui/sidebar`, `app.html`)

- **Brand:** a 28px round `bg-ink` mark with the Lucide `chart-line` icon, then "Basic Finance" (15/600).
- **Nav item:**
  - `h-[34px] rounded-full px-2.5 gap-2.5`, icon 16px.
  - **Inactive:** label `text-nav-foreground font-normal`, icon `text-muted-foreground`.
  - **Active** (`routerLinkActive`): `bg-sheet ring-1 ring-sheet-border font-semibold text-foreground` with the icon in `text-foreground`, plus `aria-current="page"`.
- **Routes and icons:** Home `house`, Transactions `receipt-text`, Spending `chart-pie`, Recurring `repeat`, Budgets `target`.
- **Institutions group:**
  - an "INSTITUTIONS" section-label;
  - rows of InstitutionAvatar + name + account count (or "Reconnect needed" in `text-warning`) + balance;
  - "Add institution" (`plus`) opens Teller Connect.
- **Footer** (`mt-auto`): Settings (`settings`), then the theme toggle (`moon` "Dark mode" / `sun` "Light mode").

### Section

Borderless block in the hairline grid. Head: a section-label on the left, with an optional link (`View all ›`) or SegmentedControl on the right.

### StatTile

- **Label row:** section-label + DeltaPill.
- **Value:** `metric` in `foreground`. Never coloured.
- **Comparison:** caption, e.g. "$3,112.80 last month".
- **Optional progress line:** a 2px `bg-muted` track with a `bg-foreground/80` fill and a 10px `bg-chart-previous` tick marking last month at the same day.

### DeltaPill

- `rounded-full px-2 py-0.5 text-xs font-medium`.
- **Variants:**

  | Variant | Text                    | Background         |
  | ------- | ----------------------- | ------------------ |
  | up      | `text-positive`         | `bg-positive-tint` |
  | down    | `text-negative`         | `bg-negative-tint` |
  | flat    | `text-muted-foreground` | `bg-muted`         |
  | warn    | `text-warning`          | `bg-warning-tint`  |

- Always include ▲/▼ or a sign.
- **Colour follows favourability, not direction:** spending going ▼ uses `up`.

### SyncStatus

- **Synced:** a 6px `bg-positive` dot with a `ring-[3px] ring-positive-tint` halo, then "Synced 12 min ago" (13px, `foreground`).
- **Syncing:** the dot becomes a 12px spinning ring, and the text reads "Syncing…".
- **Attention:** a `bg-warning` dot, and the text names the problem.

### Button (`libs/ui/button`)

| Variant           | Classes                                                                                 |
| ----------------- | --------------------------------------------------------------------------------------- |
| default (primary) | `rounded-full bg-ink text-ink-foreground` — Sync now, Add institution. One per surface. |
| outline           | `rounded-full border bg-sheet hover:bg-muted` — Export, Sign out                        |
| ghost             | `rounded-full hover:bg-muted`                                                           |
| destructive       | `rounded-full text-negative hover:bg-negative-tint`                                     |
| icon              | 30px, no chrome, `text-muted-foreground hover:text-foreground`                          |
| link              | `text-[13px] font-medium hover:underline underline-offset-3`, chevron in muted          |

- **Sizes:** default 34px; `sm` 28px at 13px.
- **No shadows.**
- **Focus:** `focus-visible:outline-2 outline-offset-2 outline-ring`.

### SegmentedControl & PeriodStepper (`shared/ui/period-selector`)

- **Segments:**
  - plain text, 13/500, `text-muted-foreground`;
  - the selected one is `text-foreground font-semibold underline decoration-2 underline-offset-[5px]`.
  - Used for 1M · 3M · 6M · 1Y · All, and All · Over budget · On track.
- **Stepper:**
  - a `rounded-full border` shell holding ‹ "October 2026" (13/600, min-w-[120px]) ›;
  - next is disabled at the current period.

### FilterChip (transactions toolbar)

- **Empty:** `h-[30px] rounded-full border border-dashed text-muted-foreground`, with a `plus` icon.
- **Applied:**
  - solid `border-foreground text-foreground`;
  - a divider, then the values in bold;
  - an `ink` count badge.
- **Popover:** search on top, then checkbox rows, each with a category dot, the name and a count.
- **Filter logic:** every filter is multi-select. Values within a chip combine with OR; chips combine with AND.

### TransactionItem (`shared/ui/transactions/transaction-item`)

- **Day divider:** day-label plus the day's net, with a `border-t` rule above and no fill.
- **Row layout:**
  - grid `grid-cols-[24px_1fr_auto_auto] gap-3 px-10 py-2.5`, with `border-t` between rows;
  - icon in `text-nav-foreground`, no tile;
  - name: body-strong, truncated;
  - meta: caption with the 6px category dot, the Teller category, and "· Pending" when pending;
  - account: caption, right-aligned (e.g. "Checking ••4417");
  - amount: body-strong, right-aligned.
- **Amount colours:**

  | Kind                  | Colour             |
  | --------------------- | ------------------ |
  | Outflow (`−$72.37`)   | `foreground`       |
  | Income (`+$2,706.00`) | `positive`         |
  | Transfer              | `muted-foreground` |

- **States:**
  - hover: `bg-muted/60`;
  - selected (detail sheet open): `bg-muted shadow-[inset_2px_0_0_var(--foreground)]`.

### CategoryBreakdown (`features/spending/.../category-breakdown-list`)

- **Row:** grid `grid-cols-[1fr_220px_110px]`, `border-t`, `px-10 py-2.5`.
- **Name column:** dot, name, then either a count or a `warn` pill reading "Over budget".
- **Bar:** a 3px square `bg-muted` track, filled with the category colour up to amount ÷ budget.
- **Amount column:** the amount, with "of $900" beneath as a caption.
- **Order and colour:** sort by amount; colours `category-1…7` in that order, then `category-8` for the rest.
- **Empty categories:** collapse into one muted row, e.g. "18 categories with no spending".

### InstitutionAvatar (`shared/ui/institutions/institution-avatar`)

- **Shape and size:** round, 32px (40px `lg`).
- **Content:**
  - the Teller logo if one exists;
  - otherwise initials (11/700) on the institution's brand colour pair;
  - fallback: `bg-ink text-ink-foreground`.
- **This is the only brand colour in the chrome.**

### Paginator (`shared/ui/paginator`)

- **Container:** `border-t px-10 py-3.5 text-[13px] text-muted-foreground`.
- **Left side:** "Showing 1–25 of 142".
- **Pages:** 30px round ghost buttons. The current page uses the segment underline style.

### Popover / select / dialog

- **Popover and select:** `bg-sheet border rounded-xl shadow-popover p-2`. Options are `rounded-md hover:bg-muted`.
- **Dialogs and sheets:** use the same surfaces.

---

## 5. Charts (ECharts)

ECharts takes colour values, not `var(--…)` references. Don't copy the hex values into TypeScript; resolve them from the CSS variables at runtime so `styles.css` stays the only source, e.g. in `core/theme/chart-palette.ts`:

```ts
export function chartPalette() {
  const css = getComputedStyle(document.documentElement);
  const v = (name: string) => css.getPropertyValue(`--${name}`).trim();
  return {
    current: v('chart-current'),
    previous: v('chart-previous'),
    spend: v('chart-spend'),
    horizon: v('horizon'),
    text: v('muted-foreground'),
    categories: [1, 2, 3, 4, 5, 6, 7, 8].map(i => v(`category-${i}`)),
  };
}
```

Call it when building chart options and again whenever the theme (`.dark` on `:root`) changes, then re-apply the options.

Common rules:

- Lines are smooth with `showSymbol: false`. There's no area fill, no y-axis and no split lines.
- The only baseline is a 1px `horizon` line.
- This period: a 1.75px solid `chart-current` line.
- Last period: a 1.5px `chart-previous` line, dashed `[4, 4]`.
- Axis labels are 10–11px in `muted-foreground`; month labels use 0.2em letter-spacing.
- Values appear in tooltips (`sheet` background, `border`, `shadow-popover`), not on the axis.

**NetWorthLine (Home):**

- Header: a hero number with a DeltaPill, plus range segments.
- A 110px fine line ending in a 3px dot.
- Months every other label under the horizon. No grey axis band.
- Keep it inline (span 8). It is not a full-width hero.

**SpendActivityChart (Home):**

- Cumulative spend for this month (solid) against last month (dashed).
- A legend below, with a round swatch and a dashed stub.

**DailySpendChart (Transactions):**

- Bars:
  - this month: `chart-spend`, narrow, in front;
  - last month: `chart-previous` at 45% opacity, wider, behind.
- ECharts settings: `barGap: '-100%'`, radius 0.
- Clicking a bar filters the list to that day.

**Donut (Spending):**

- The top five categories plus Other, using `category-*`.
- Stroke 14px, total in the centre at 22/300.

---

## 6. Loading states

Every data view takes a state: `loading | refreshing | ready`.

**Skeleton** (first load, no data yet):

- Replace every number, amount, row line, pill and chart with a `bg-skeleton` block the same size as the content. Render the real or placeholder text as `text-transparent` so the size matches.
- Keep headings, section labels, nav and controls real.
- Avatars and row icons become circles; a chart becomes one block of the chart's height.
- Animation: `animate-pulse` (or 1.6s to 45% opacity), switched off under `motion-reduce`.

**Refreshing** (sync over existing data):

- Data stays in place at `opacity-55`.
- A 2px `bg-chart-current` bar slides along the top of the sheet.
- SyncStatus switches to Syncing…, and the Sync button's icon spins.
- An `ink` toast at the sheet's bottom-right reports progress, e.g. "Syncing Wells Fargo · 2 of 4 accounts":
  - it hides on success;
  - on error it stays until dismissed.

---

## 7. Content

- **Page titles:** one noun (Home, Transactions, Spending, Recurring, Budgets, Settings). Section labels are uppercase nouns.
- **Money:** use the `currency` pipe, e.g. `$37,053.63`, with tabular figures. Prefix outflows with `−` and income with `+`.
- **Change:** written as `▲ $5,853.63 · 18.8%`, then the reference as a caption ("$3,112.80 last month").
- **Time:** "Synced 12 min ago", periods as "October 2026", day dividers as "TUE, OCT 14".
- **Categories:** use Teller's 28 category names, capitalised.
- **Errors:** say what failed and offer one action ("Chase needs reconnecting" + Reconnect).
- **Don't:** use subtitles under page titles, or "Assets/Liabilities" jargon on Home.

---

## 8. Icons

Lucide via `@ng-icons/lucide`:

- 16px, stroke 1.75, `currentColor`;
- no icon tiles or backgrounds, except the app mark and avatars;
- inactive nav icons use `muted-foreground`, active ones `foreground`.

---

## 9. Don'ts

- No cards, tinted panels or shadows inside the sheet; use hairline sections.
- No coloured section titles, coloured big numbers or decorative accent colours.
- No grey band under chart axes; no gradients or area fills.
- Don't grey out inactive nav labels below `nav-foreground`.
- No more than one `ink` primary button per surface.
- No spinners in place of content.
- Component `.css` files stay empty (repo convention); style with Tailwind utilities in templates.
