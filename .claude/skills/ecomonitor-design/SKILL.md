---
name: ecomonitor-design
description: Design system and visual rules for the EcoMonitor web app (ASP.NET Core Razor + Bootstrap 5 + Leaflet + Chart.js). This skill should be used whenever creating or editing any UI in this repository - Razor views, partials, layouts, CSS, Leaflet map styling, Chart.js charts, e-mail templates - or when the user asks to restyle, polish, or redesign a page. It keeps the existing EcoMonitor color palette and removes the generic AI-generated look.
---

# EcoMonitor Design Skill

EcoMonitor is a municipal environmental-monitoring and waste-management product for Bishkek,
used by four roles (citizen, inspector, cleanup crew, administrator) in three languages
(Russian, English, Kyrgyz). The visual tone is: civic, trustworthy, calm, data-first.
Not a startup landing page. Not a crypto dashboard.

## Hard rules (never violate)

1. KEEP THE EXISTING BRAND PALETTE. Never introduce new brand hues. Source colors from the
   existing CSS (site.css / current variables). If colors are hard-coded all over the views,
   first extract them into CSS custom properties in one place, then reuse the variables.
   Shades and tints of existing colors are allowed; new hues are not.
2. Never use: purple/violet gradients, glassmorphism cards, neon glows, emoji as icons,
   gradient text headings, oversized hero sections, "three feature cards in a row" marketing
   patterns, default Bootstrap blue (#0d6efd) as an accent.
3. Never leave default Bootstrap look on visible primitives: buttons, badges, tables, forms,
   pagination must be themed through tokens (see below), not left stock.
4. All fonts must fully support Cyrillic (the UI is RU/EN/KY). Before choosing any Google
   Font, verify Cyrillic coverage. Never use a display font that renders Russian/Kyrgyz text
   in a fallback font.
5. Status colors carry meaning (report lifecycle, air-quality bands, container fill).
   Semantic colors must stay consistent across the whole app: the same status is always the
   same color everywhere (queue, badge, map marker, timeline, chart). Never communicate
   status by color alone - always pair with a label or icon (accessibility).
6. Do not redesign by replacing Bootstrap with another framework. Improve within
   Bootstrap 5 via its CSS variables and a single custom token layer.

## Design tokens (single source of truth)

Maintain one file, e.g. wwwroot/css/tokens.css, loaded before site.css:

- Colors: map the existing palette to semantic variables:
  --em-brand (the existing primary green), --em-brand-strong, --em-surface, --em-surface-2,
  --em-ink, --em-ink-muted, --em-line, plus semantic: --em-ok, --em-warn, --em-danger,
  --em-info, and the air-quality band scale.
- Bridge into Bootstrap by overriding its variables (--bs-primary, --bs-link-color,
  --bs-border-radius, --bs-body-font-family, etc.) instead of fighting utility classes.
- Spacing: a 4px base scale; section padding consistent across pages.
- Radius: ONE radius for controls (6-8px) and ONE for cards (10-12px). Not 20px+ pills
  everywhere.
- Shadows: two levels only - a hairline border for most things, one soft elevation
  (0 4px 14px rgba(brand-ink, .06-.10)) for raised cards/popovers. No shadow soup.
- Motion: transitions 150-200ms ease-out, only on hover/focus/expand. One page-level
  entrance is enough; no scattered animations.

## Typography

- Pick ONE characterful display face for headings/numbers and ONE quiet face for body,
  both with full Cyrillic. Good Cyrillic-safe candidates: Golos Text, Onest, Manrope,
  PT Root UI, IBM Plex Sans; pair with PT Serif or Source Serif 4 if a serif accent fits.
  Avoid Inter-everywhere and system-font defaults; avoid the overused Space Grotesk.
- Headings: tighter line-height (1.1-1.2), real hierarchy (page title clearly larger than
  card titles). Body 15-16px, line-height 1.5-1.6.
- Numbers in tables and KPIs: tabular figures (font-variant-numeric: tabular-nums),
  right-aligned in columns.

## Components (Bootstrap 5, themed)

- Buttons: brand-filled primary, quiet outline secondary, plain-text tertiary. Consistent
  height, radius from tokens, visible :focus-visible ring in brand color.
- Tables/queues (inspector and crew screens are the core of the product): denser rows,
  hairline row separators (no heavy zebra), sticky header on long lists, right-aligned
  numeric columns, status as a compact badge with dot + label.
- Cards: hairline border + surface color; reserve the elevation shadow for hover or for
  truly floating elements. No border AND heavy shadow together.
- Forms: clear labels above inputs, helper/error text styled once globally, identical
  control heights; file-upload and map-picker styled to match.
- Badges/status: one badge component for the whole report lifecycle; colors from semantic
  tokens; never invent a new badge style per page.
- Empty states: every queue/list needs a designed empty state (small line icon, one
  sentence, primary action) instead of a blank area.
- Navigation/layout: persistent left sidebar or top nav (whichever exists - refine, do not
  replace) with a clear active state; the live /Live page link visually distinct.

## Map (Leaflet) and charts (Chart.js)

- Leaflet: restyle popups and controls with the token radius/border/shadow; district
  polygons use each district's existing ColorHex at low fill-opacity (0.08-0.15) with a
  2px border; custom div-icon markers colored by semantic status, not default blue pins.
- Chart.js: set global defaults once (Chart.defaults): token font family, ink-muted tick
  color, hairline grid (or no vertical grid), brand/semantic dataset colors, themed tooltip
  (surface bg, hairline border, token radius). Air-quality series use the band colors.

## Quality checklist before finishing any UI task

- [ ] No raw default-Bootstrap controls visible on the changed screens.
- [ ] All colors come from tokens; no new hex values sprinkled in views.
- [ ] Cyrillic renders in the chosen fonts (check a Russian and a Kyrgyz string).
- [ ] Status colors match the single semantic mapping everywhere.
- [ ] Hover/focus states exist; focus is visible by keyboard.
- [ ] Mobile width 360px: tables collapse or scroll intentionally; map controls reachable.
- [ ] Empty state exists for any list that can be empty.
- [ ] Contrast: body text on surface >= 4.5:1.

## How to run a redesign pass

When asked to "improve the design", do not restyle everything blindly. Work in this order:
1) create/extend tokens.css and bridge Bootstrap variables; 2) typography; 3) the shared
layout and nav; 4) the highest-traffic screens (citizen report form, inspector queue,
/Live dashboard); 5) map and charts theming; 6) empty states and micro-interactions.
After each step, show the result and wait for confirmation before the next.

## Dashboards and KPI stats (anti-AI patterns)

The single biggest "AI-generated" tell is a grid of identical KPI cards:
uppercase letter-spaced label + decorative icon in the top-right corner +
oversized number + muted caption, each in its own bordered box. Never build
stats this way.

Rules:
- No decorative icons in card corners. An icon is allowed only when it carries
  meaning (status, trend), never as filler.
- No uppercase letter-spaced labels. Normal case, medium weight, --em-ink-muted.
- ONE accent maximum per card. Never combine a colored top border with a
  colored left border. For semantic states use a small colored dot before the
  label OR a 3px left bar - not both, and no brand top strip on top of it.
- Prefer a stat strip over a card grid: one shared surface, stats separated by
  hairline vertical dividers, label above value, context line below. Boxes are
  for interactive content, not for single numbers.
- Numbers: tabular figures, 24-32px, not 48px+. Context (period, X of Y) lives
  in the caption, not in inflated digits.
- Density: key stats fit above the fold; generous padding inside near-empty
  boxes is a smell.
