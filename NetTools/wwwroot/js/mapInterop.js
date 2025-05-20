function initMap(id, authCode) {
    mapkit.init({
        authorizationCallback: function (done) {
            done(authCode);
        }
    });

    map = new mapkit.Map(id, {
        center: new mapkit.Coordinate(51.505, -0.09),
        region: new mapkit.CoordinateRegion(
            new mapkit.Coordinate(51.505, -0.09),
            new mapkit.CoordinateSpan(10, 10)
        ),
        showsZoomControl: true
    });

    map.addEventListener("region-change-end", () => {
        const zoomThreshold = 6;
        const showTitles = map._impl.zoomLevel >= zoomThreshold;
        markerLayer.forEach(annotation => {
            annotation.title = showTitles ? annotation._originalTitle : "";
        });
    });

    return map;
}

function disposeMap(map) {
    map.destroy();
}

function createLayer(map) {
    markerLayer = [];
    return markerLayer;
}

function clearLayer(layer) {
    layer.forEach(item => {
        if (item instanceof mapkit.MarkerAnnotation) {
            map.removeAnnotation(item);
        } else if (item instanceof mapkit.PolylineOverlay) {
            map.removeOverlay(item);
        }
    });

    layer.length = 0;
}

function addMarkers(map, layer, markers, includePolyline) {
    clearLayer(layer);

    const coords = markers.map(m => new mapkit.Coordinate(m.position[0], m.position[1]));
    const seen = new Set();
    const uniqueMarkers = markers.filter(m => {
        const key = m.position.join(",");
        if (seen.has(key)) return false;
        seen.add(key);
        return true;
    });

    uniqueMarkers.forEach((marker) => {
        const coord = new mapkit.Coordinate(marker.position[0], marker.position[1]);
        const annotation = new mapkit.MarkerAnnotation(coord);
        annotation._originalTitle = marker.label;

        map.addAnnotation(annotation);
        layer.push(annotation);
    });

    if (includePolyline && coords.length > 1) {
        addPolyline(layer, coords);
    }

    if (coords.length > 0) {
        map.showItems(layer);
        map._impl.zoomLevel--;
    }
}

function addPolyline(layer, coords) {
    const polyline = new mapkit.PolylineOverlay(coords, {
        style: new mapkit.Style({
            strokeColor: "#FFA500",
            lineWidth: 3
        })
    });

    map.addOverlay(polyline);
    layer.push(polyline);
}
