import { NextResponse } from 'next/server';

/**
 * Comprobación de salud del contenedor web.
 *
 * Solo confirma que el servidor de Next responde. Deliberadamente NO comprueba la
 * API ni la base de datos: si lo hiciera, una caída de la base de datos marcaría
 * también como enfermo al contenedor web y Docker lo reiniciaría sin motivo,
 * dejando a la gente sin ni siquiera la página de error.
 */
export async function GET() {
  return NextResponse.json({ estado: 'correcto' }, { status: 200 });
}

// Sin caché: una respuesta cacheada diría «correcto» aunque el proceso ya no lo esté.
export const dynamic = 'force-dynamic';
