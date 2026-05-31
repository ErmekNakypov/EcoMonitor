// Reusable Leaflet districts overlay.
//
// Lifted from the previously-inlined block in Views/Map/Index.cshtml so any
// view with a Leaflet map can render the same overlay and (optionally) ask
// "which district contains this point?" client-side.
//
// IMPORTANT: getDistrictForPoint() is a CLIENT-SIDE PREVIEW only. The
// authoritative district for a submitted report is resolved server-side by
// IDistrictResolver inside SubmitDumpsiteReportHandler. The preview is for
// UX hints only — never send it to the server, never persist it, never let
// it override server resolution. If a client preview ever disagrees with
// the server (e.g. a point on a polygon edge), the server wins.
//
// Usage:
//   const overlay = DistrictsOverlay.attach(map, {
//       interactive: true,    // tooltip on hover, intercepts clicks
//       addToMap: false,      // controller adds/removes layer (e.g. Map toggle)
//       fillOpacity: 0.15
//   });
//   await overlay.load();                              // idempotent
//   map.addLayer(overlay.layerGroup);                  // if addToMap was false
//   const d = overlay.getDistrictForPoint(lat, lng);   // null if outside / not loaded
//
(function () {
    'use strict';

    function attach(map, options) {
        if (!map || typeof L === 'undefined') {
            throw new Error('DistrictsOverlay.attach requires a Leaflet map');
        }
        const opts = Object.assign({
            interactive: true,
            addToMap: false,
            fillOpacity: 0.15
        }, options || {});

        const layerGroup = L.layerGroup();
        let districts = [];
        let loadPromise = null;

        function load() {
            if (loadPromise) return loadPromise;
            loadPromise = (async () => {
                try {
                    const r = await fetch('/api/districts');
                    if (!r.ok) {
                        console.warn('DistrictsOverlay: /api/districts returned', r.status);
                        return [];
                    }
                    const data = await r.json();
                    data.forEach(d => {
                        const polygon = L.polygon(d.boundary, {
                            color: d.color,
                            fillColor: d.color,
                            fillOpacity: opts.fillOpacity,
                            weight: 2,
                            // When interactive is false the polygons don't capture
                            // clicks, so the underlying map's click handler (e.g. the
                            // citizen pin-placer) keeps working over the overlay.
                            interactive: opts.interactive
                        });
                        if (opts.interactive) {
                            polygon.bindTooltip('<strong>' + (d.name_ru || d.name_en || d.code) + '</strong>',
                                { permanent: false });
                        }
                        layerGroup.addLayer(polygon);
                    });
                    districts = data;
                    if (opts.addToMap) {
                        map.addLayer(layerGroup);
                    }
                    return data;
                } catch (e) {
                    console.warn('DistrictsOverlay: fetch failed', e);
                    return [];
                }
            })();
            return loadPromise;
        }

        // Ray-casting point-in-polygon, mirrors PointInPolygonChecker.cs in the
        // backend so the preview matches the server resolution. First match
        // wins (the seeded rectangles tile cleanly without overlap).
        function isPointInPolygon(lat, lng, boundary) {
            if (!boundary || boundary.length < 3) return false;
            let inside = false;
            let j = boundary.length - 1;
            for (let i = 0; i < boundary.length; i++) {
                const piLat = boundary[i][0], piLng = boundary[i][1];
                const pjLat = boundary[j][0], pjLng = boundary[j][1];
                if (((piLat > lat) !== (pjLat > lat)) &&
                    (lng < (pjLng - piLng) * (lat - piLat) / (pjLat - piLat) + piLng)) {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        function getDistrictForPoint(lat, lng) {
            if (!districts || districts.length === 0) return null;
            for (const d of districts) {
                if (isPointInPolygon(lat, lng, d.boundary)) return d;
            }
            return null;
        }

        return {
            layerGroup,
            load,
            get loaded() { return loadPromise; },
            getDistrictForPoint
        };
    }

    window.DistrictsOverlay = { attach };
})();
