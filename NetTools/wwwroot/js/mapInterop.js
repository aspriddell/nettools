function initMap(id) {
    const map = L.map(id).setView([51.505, -0.09], 4);
    const tiles = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    });

    tiles.addTo(map);
    return map;
}

function disposeMap(map) {
    map.off();
    map.remove();
}

function createLayer(map) {
    const layer = L.layerGroup();
    map.addLayer(layer);
    
    return layer;
}

function clearLayer(layer) {
    layer.clearLayers();
}

function removeLayer(map, layer) {
    map.removeLayer(layer);
}

function addMarkers(map, layerGroup, markers, includePolyline) {    
    const markerInstances = markers.map(marker => {
        return L.marker(marker.position).bindPopup(marker.label);
    });

    // add layers
    markerInstances.forEach(m => layerGroup.addLayer(m));
    
    if (includePolyline) {
        addPolyline(layerGroup, markers.map(m => m.position));
    }

    // https://stackoverflow.com/a/16845714
    const group = new L.featureGroup(markerInstances);
    map.fitBounds(group.getBounds().pad(0.1));
}

function addPolyline(layerGroup, polyline) {
    const line = L.polyline(polyline, {color: 'orange'});
    layerGroup.addLayer(line);
}
