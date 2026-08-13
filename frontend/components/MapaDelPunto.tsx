'use client';

import { useEffect, useRef } from 'react';

/**
 * Mapa del punto de encuentro.
 *
 * Usa Leaflet con teselas de OpenStreetMap: sin clave de API y sin coste, que es lo
 * que permite levantar el entorno completo en local sin dar de alta ningún servicio.
 *
 * Leaflet se carga de forma diferida y solo en el navegador porque manipula el DOM
 * directamente; importarlo de forma normal rompería el renderizado en servidor.
 *
 * Privacidad: el mapa centra en las coordenadas públicas del punto de encuentro.
 * La dirección exacta solo se muestra debajo si el servidor la ha incluido en la
 * respuesta, cosa que solo hace con quien tiene plaza confirmada.
 */

interface Propiedades {
  latitud: number;
  longitud: number;
  lugar: string;
  direccionExacta: string | null;
}

export default function MapaDelPunto({ latitud, longitud, lugar, direccionExacta }: Propiedades) {
  const contenedor = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let mapa: import('leaflet').Map | undefined;
    let cancelado = false;

    async function pintar() {
      const L = await import('leaflet');

      // Si el componente se desmontó mientras se cargaba Leaflet, no se pinta nada.
      if (cancelado || !contenedor.current) {
        return;
      }

      mapa = L.map(contenedor.current, {
        center: [latitud, longitud],
        zoom: 15,
        // Sin controles de zoom ni desplazamiento: es un mapa de referencia dentro
        // de una página con scroll, no un mapa para explorar.
        zoomControl: false,
        scrollWheelZoom: false,
        dragging: false,
        attributionControl: true,
      });

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        // La atribución es obligatoria por la licencia de OpenStreetMap.
        attribution: '&copy; colaboradores de OpenStreetMap',
        maxZoom: 19,
      }).addTo(mapa);

      L.circleMarker([latitud, longitud], {
        radius: 10,
        color: '#0B7C9B',
        fillColor: '#0B7C9B',
        fillOpacity: 0.9,
        weight: 3,
      })
        .addTo(mapa)
        .bindTooltip(lugar);
    }

    void pintar();

    return () => {
      cancelado = true;
      mapa?.remove();
    };
  }, [latitud, longitud, lugar]);

  return (
    <div className="flex flex-col gap-2">
      <div
        ref={contenedor}
        role="img"
        aria-label={`Mapa con la ubicación aproximada de ${lugar}`}
        className="h-[160px] w-full overflow-hidden rounded-[18px] bg-[#E4EDEA]"
      />

      {direccionExacta ? (
        <p className="rounded-[14px] bg-ok-bg px-3.5 py-2.5 text-[13px] text-ok">
          <strong>Dirección exacta:</strong> {direccionExacta}
        </p>
      ) : (
        <p className="rounded-[14px] bg-paper px-3.5 py-2.5 font-mono text-[11px] text-body">
          La dirección exacta se ve al apuntarte
        </p>
      )}
    </div>
  );
}
