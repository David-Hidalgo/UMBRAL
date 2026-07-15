// HTML de un mapa Leaflet de solo lectura (pin del tesoro + círculo del radio).
// Compartido por la versión nativa (WebView) y la web (iframe).
export function buildTreasureMapHtml(
	latitude: number,
	longitude: number,
	radius: number,
): string {
	return `<!DOCTYPE html>
<html>
<head>
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<style>html,body,#map{margin:0;padding:0;height:100%;width:100%;background:#0b0b0f}</style>
</head>
<body>
<div id="map"></div>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script>
  var lat = ${latitude}, lng = ${longitude}, r = ${radius};
  L.Marker.prototype.options.icon = L.icon({
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
    iconSize: [25, 41], iconAnchor: [12, 41]
  });
  var map = L.map('map').setView([lat, lng], 15);
  L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(map);
  L.marker([lat, lng]).addTo(map);
  var circle = L.circle([lat, lng], { radius: r, color: '#6366f1', fillColor: '#6366f1', fillOpacity: 0.15 }).addTo(map);
  // El contenedor (iframe/WebView) puede no tener su tamaño final al iniciar:
  // se recalcula el tamaño y se encuadra al radio una vez dimensionado.
  function fit() {
    map.invalidateSize();
    map.fitBounds(circle.getBounds(), { padding: [20, 20] });
  }
  window.addEventListener('load', fit);
  setTimeout(fit, 300);
  setTimeout(function () { map.invalidateSize(); }, 800);
</script>
</body>
</html>`;
}
