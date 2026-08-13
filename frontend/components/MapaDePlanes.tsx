'use client';

import { useEffect, useRef } from 'react';

import { cuandoCorto, estadoDePlazas } from '@/lib/formato';
import type { ResumenDePlan } from '@/lib/tipos';

/**
 * Mapa con todos los planes cercanos.
 *
 * Leaflet se importa de forma diferida porque toca el DOM directamente y no puede
 * ejecutarse durante el renderizado en servidor.
 *
 * Cada marcador lleva un globo con el título, cuándo es y el estado de plazas: es la
 * información mínima para decidir si merece la pena abrir el plan, y evita tener que
 * entrar y volver una y otra vez.
 */

interface Propiedades {
  planes: ResumenDePlan[];
  centro: { latitud: number; longitud: number };
}

export default function MapaDePlanes({ planes, centro }: Propiedades) {
  const contenedor = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let mapa: import('leaflet').Map | undefined;
    let cancelado = false;

    async function pintar() {
      const L = await import('leaflet');

      if (cancelado || !contenedor.current) {
        return;
      }

      mapa = L.map(contenedor.current, {
        center: [centro.latitud, centro.longitud],
        zoom: 14,
        zoomControl: true,
      });

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; colaboradores de OpenStreetMap',
        maxZoom: 19,
      }).addTo(mapa);

      for (const plan of planes) {
        const estado = estadoDePlazas(plan.capacidad, plan.plazasOcupadas, plan.estado);

        L.circleMarker([plan.latitud, plan.longitud], {
          radius: 11,
          color: '#0B7C9B',
          fillColor: '#0B7C9B',
          fillOpacity: 0.85,
          weight: 3,
        })
          .addTo(mapa)
          .bindPopup(
            // El contenido se construye con la API del DOM y no con HTML en texto:
            // insertar el título de un plan como cadena permitiría inyectar marcado
            // a quien lo escribió.
            crearGlobo(plan, cuandoCorto(plan.inicio), estado.texto),
          );
      }

      // Si hay planes, se ajusta la vista para que quepan todos.
      if (planes.length > 0) {
        mapa.fitBounds(
          L.latLngBounds(planes.map((plan) => [plan.latitud, plan.longitud] as [number, number])),
          { padding: [50, 50], maxZoom: 15 },
        );
      }
    }

    void pintar();

    return () => {
      cancelado = true;
      mapa?.remove();
    };
  }, [planes, centro]);

  return (
    <div
      ref={contenedor}
      role="application"
      aria-label={`Mapa con ${planes.length} planes cercanos`}
      className="min-h-0 flex-1"
    />
  );
}

/** Construye el contenido del globo con nodos del DOM, nunca concatenando HTML. */
function crearGlobo(plan: ResumenDePlan, cuando: string, estadoDePlazasTexto: string): HTMLElement {
  const raiz = document.createElement('div');
  raiz.className = 'flex flex-col gap-1';

  const titulo = document.createElement('strong');
  titulo.textContent = plan.titulo;
  raiz.appendChild(titulo);

  const detalle = document.createElement('span');
  detalle.textContent = `${cuando} · ${plan.lugar}`;
  raiz.appendChild(detalle);

  const plazas = document.createElement('span');
  plazas.textContent = estadoDePlazasTexto;
  raiz.appendChild(plazas);

  const enlace = document.createElement('a');
  enlace.href = `/plan/${plan.id}`;
  enlace.textContent = 'Ver el plan →';
  enlace.style.fontWeight = 'bold';
  raiz.appendChild(enlace);

  return raiz;
}
