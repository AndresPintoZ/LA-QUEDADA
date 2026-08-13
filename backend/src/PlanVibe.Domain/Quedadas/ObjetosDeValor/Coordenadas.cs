using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Quedadas.ObjetosDeValor;

/// <summary>
/// Punto geográfico en WGS 84 (el sistema que usan GPS, OpenStreetMap y PostGIS con SRID 4326).
/// </summary>
public readonly record struct Coordenadas
{
    /// <summary>Precisión con la que se almacenan: ~1 metro, suficiente para un punto de encuentro
    /// y deliberadamente insuficiente para señalar una vivienda concreta.</summary>
    private const int DecimalesSignificativos = 5;

    public Coordenadas(double latitud, double longitud)
    {
        ExcepcionDeDominio.SiNo(
            latitud is >= -90 and <= 90,
            "coordenadas.latitud_invalida",
            "La latitud debe estar entre -90 y 90 grados.");

        ExcepcionDeDominio.SiNo(
            longitud is >= -180 and <= 180,
            "coordenadas.longitud_invalida",
            "La longitud debe estar entre -180 y 180 grados.");

        Latitud = Math.Round(latitud, DecimalesSignificativos);
        Longitud = Math.Round(longitud, DecimalesSignificativos);
    }

    public double Latitud { get; }

    public double Longitud { get; }

    /// <summary>
    /// Distancia en metros por la fórmula del haversine. Se usa para comprobaciones dentro del
    /// dominio y en pruebas; las consultas por cercanía las resuelve PostGIS, que además puede
    /// apoyarse en un índice espacial.
    /// </summary>
    public double DistanciaEnMetrosHasta(Coordenadas otra)
    {
        const double RadioTerrestreEnMetros = 6_371_000;

        var latitud1 = double.DegreesToRadians(Latitud);
        var latitud2 = double.DegreesToRadians(otra.Latitud);
        var deltaLatitud = double.DegreesToRadians(otra.Latitud - Latitud);
        var deltaLongitud = double.DegreesToRadians(otra.Longitud - Longitud);

        var a = (Math.Sin(deltaLatitud / 2) * Math.Sin(deltaLatitud / 2))
              + (Math.Cos(latitud1) * Math.Cos(latitud2) * Math.Sin(deltaLongitud / 2) * Math.Sin(deltaLongitud / 2));

        return RadioTerrestreEnMetros * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Latitud},{Longitud}");
}
