# ADR-006 · Verificación de identidad sin almacenar documentos

**Estado:** aceptada · 2026-08-11

## Contexto

RF-20 exige verificar la identidad del organizador antes de su primera publicación. RF-21 y RF-22
añaden que la plataforma **no debe guardar** copia del documento y que solo conserve estado,
referencia, fecha y proveedor.

`docs/04-seguridad-privacidad-moderacion.md` lo plantea como principio clave: verificar sin
almacenar. Este ADR recoge cómo se traduce eso en código.

La tentación evidente es guardar «solo por si acaso» una foto del documento en un bucket, o el
número de DNI «para poder identificar en caso de incidente». Ese razonamiento es exactamente el que
produce las filtraciones de datos de identidad.

## Decisión

La interfaz del proveedor de verificación está diseñada para que **sea imposible** que un documento
entre en el sistema:

```csharp
public interface IProveedorDeVerificacion
{
    string Nombre { get; }
    Task<SesionDeVerificacion> IniciarAsync(UsuarioId usuarioId, CancellationToken cancelacion);
    Task<ResultadoDeVerificacion> ConsultarResultadoAsync(string referenciaExterna, CancellationToken cancelacion);
}
```

Ningún parámetro ni ningún resultado admite una imagen, un número de documento ni una fecha de
nacimiento. PlanVibe abre una sesión, redirige a la persona al proveedor y recibe un veredicto.

Lo único que se guarda es `DatosDeVerificacion`:

| Campo | Por qué se guarda |
|---|---|
| `Estado` | Decidir si puede organizar |
| `Proveedor` | Trazabilidad ante un incidente |
| `ReferenciaExterna` | Poder reclamar al proveedor |
| `MayoriaDeEdadConfirmada` | RF-24, como un sí/no, no como fecha |
| `ActualizadaEn` | Auditoría y caducidad |
| `Observacion` | Poder explicar un rechazo |

De la edad se guarda además **solo el año de nacimiento** en el perfil, nunca la fecha completa.
Basta para comprobar la edad mínima de acceso.

### La regla está protegida por una prueba

```csharp
[Fact]
public void La_verificacion_no_guarda_ningun_dato_del_documento()
```

Recorre por reflexión las propiedades de `DatosDeVerificacion` y falla si aparece una nueva. No es
una prueba de comportamiento: es un **candado**. Quien añada un campo `FotoDelDocumento` verá la
prueba en rojo y tendrá que venir a este documento a justificar por qué.

## Consecuencias

**A favor:**

- Una filtración de la base de datos de PlanVibe no expone ningún documento de identidad, porque no
  hay ninguno.
- Se cumple la minimización de datos sin depender de que nadie se acuerde.
- Cambiar de proveedor real afecta a una carpeta.

**En contra:**

- Se depende de un tercero para una función crítica: sin proveedor, nadie puede publicar.
- Ante un incidente grave que requiera identificar a alguien, hay que **reclamar al proveedor** con
  la referencia técnica. Es más lento que consultarlo en la propia base de datos, y es
  precisamente la contrapartida que se acepta.
- El proveedor de desarrollo es **simulado y aprueba a todo el mundo**. Se mitiga con dos barreras:
  solo se registra si el entorno es Development, y fuera de él el contenedor de dependencias lanza
  una excepción explícita en lugar de dejar la aplicación sin verificación.

## Pendiente antes de abrir el piloto

1. Elegir proveedor real y revisar su contrato de tratamiento de datos.
2. Comprobar que su respuesta incluye confirmación de mayoría de edad sin devolver la fecha
   completa. Si solo devuelve la fecha, hay que descartarla en el adaptador y no propagarla.
3. Definir la caducidad de una verificación y si hay que repetirla periódicamente.

## Alternativas descartadas

**Guardar una foto del documento en almacenamiento cifrado.** Permitiría verificar sin terceros.
Descartada: contradice RF-21 y convierte la base de datos en un objetivo mucho más valioso. El
cifrado en reposo no protege de un acceso con las credenciales de la aplicación.

**Guardar solo un hash del número de documento.** Parece un buen término medio. Descartada porque
el espacio de números de DNI es lo bastante pequeño para invertir el hash por fuerza bruta en
minutos: no es un dato anonimizado, es un dato con un disfraz.

**Verificación manual por una persona del equipo.** Viable con veinte usuarios. Descartada porque
significa que alguien recibe y mira documentos por un canal sin control, que es peor que
guardarlos ordenadamente.
