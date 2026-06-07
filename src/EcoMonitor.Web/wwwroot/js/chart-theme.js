/* =====================================================================
   EcoMonitor — Chart.js global theme.
   Per ecomonitor-design SKILL.md step 5: "set global defaults once
   (Chart.defaults): token font family, ink-muted tick color, hairline
   grid (or no vertical grid), brand/semantic dataset colors, themed
   tooltip (surface bg, hairline border, token radius)".

   Loaded in _Layout.cshtml AFTER chart.js (both deferred). The module
   reads CSS custom properties off :root so the source of truth stays in
   tokens.css; if a token rename / palette tweak happens later, no edits
   needed here. Per-page chart options can still override anything they
   need — these are defaults only.
   ===================================================================== */
(function () {
    'use strict';

    function readToken(name, fallback) {
        var raw = getComputedStyle(document.documentElement).getPropertyValue(name);
        return raw ? raw.trim() : fallback;
    }

    function applyTheme() {
        if (typeof Chart === 'undefined' || !Chart.defaults) {
            // Chart.js failed to load — silently bail; pages without charts
            // are unaffected, pages with charts will surface the real error.
            return;
        }

        var ink       = readToken('--em-ink',         '#1B263B');
        var inkMuted  = readToken('--em-ink-soft',    '#6B7280');
        var line      = readToken('--em-line',        '#E9ECEF');
        var surface   = readToken('--em-surface',     '#FFFFFF');
        var brand     = readToken('--em-brand',       '#2D6A4F');
        var radiusRaw = readToken('--em-radius-control', '6px');
        var radius    = parseInt(radiusRaw, 10) || 6;

        var fontStack = readToken('--em-font-body',
            "'Golos Text', 'PT Root UI', system-ui, sans-serif");

        // Global font + ink
        Chart.defaults.font.family = fontStack;
        Chart.defaults.font.size   = 12;
        Chart.defaults.color       = inkMuted;

        // Hairline grid using the token line colour. Skill: "hairline grid
        // (or no vertical grid)" — we keep both axes but at minimum
        // weight, so dense time-series remain readable without grid
        // distracting from the data.
        if (Chart.defaults.scale && Chart.defaults.scale.grid) {
            Chart.defaults.scale.grid.color = line;
            Chart.defaults.scale.grid.lineWidth = 1;
        }
        if (Chart.defaults.scale && Chart.defaults.scale.ticks) {
            Chart.defaults.scale.ticks.color = inkMuted;
        }
        if (Chart.defaults.scale && Chart.defaults.scale.border) {
            Chart.defaults.scale.border.color = line;
        }

        // Default dataset accent — only used when a chart doesn't set its
        // own colour. /Live and Air/Station already pass explicit brand
        // colours per dataset, so this is just a safe fallback.
        Chart.defaults.borderColor       = line;
        Chart.defaults.backgroundColor   = brand;
        Chart.defaults.elements.line.borderColor       = brand;
        Chart.defaults.elements.line.backgroundColor   = brand;
        Chart.defaults.elements.point.backgroundColor  = brand;
        Chart.defaults.elements.point.borderColor      = brand;

        // Legend ink
        if (Chart.defaults.plugins && Chart.defaults.plugins.legend &&
            Chart.defaults.plugins.legend.labels) {
            Chart.defaults.plugins.legend.labels.color = ink;
            Chart.defaults.plugins.legend.labels.boxHeight = 10;
            Chart.defaults.plugins.legend.labels.boxWidth = 10;
        }

        // Themed tooltip — surface bg, hairline border, token radius.
        if (Chart.defaults.plugins && Chart.defaults.plugins.tooltip) {
            var tt = Chart.defaults.plugins.tooltip;
            tt.backgroundColor   = surface;
            tt.titleColor        = ink;
            tt.bodyColor         = ink;
            tt.footerColor       = inkMuted;
            tt.borderColor       = line;
            tt.borderWidth       = 1;
            tt.cornerRadius      = radius;
            tt.padding           = 10;
            tt.boxPadding        = 4;
            tt.displayColors     = true;
            tt.titleFont         = { family: fontStack, weight: '600', size: 13 };
            tt.bodyFont          = { family: fontStack, weight: '400', size: 12 };
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyTheme);
    } else {
        applyTheme();
    }
})();
