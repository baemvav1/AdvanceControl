# Ficha de Inspección — pendiente de reconstrucción

## Contexto

Esta funcionalidad vivía como la pestaña **"Ficha de Inspección"** dentro de la ventana
`Advance Control/Views/Formularios/InspeccionFormWindow.xaml` (un `TabView` con dos pestañas:
"Ficha de Inspección" y "Mantenimiento Preventivo"). El día de este cambio se separó esa
ventana en dos funcionalidades independientes:

- **Mantenimiento Preventivo** quedó como `Advance Control/Views/Formularios/MantenimientoPreventivoWindow.xaml`
  (+ `.xaml.cs` y `MantenimientoPreventivoModels.cs`), enlazada al botón **"Mtto Prev."** en la
  pestaña Documentos de `OperacionVisorPage`.
- **Ficha de Inspección** se retiró por completo de la UI (no queda ningún botón ni ventana
  que la abra) y su código fue eliminado. Este documento reconstruye su contenido para
  cuando se decida reimplementarla, probablemente como una ventana/página independiente.

Igual que Mantenimiento Preventivo, era **un mockup puramente visual**: no persistía
información, no consultaba datos de la operación ni del backend. El formulario reproducía
visualmente la planilla `FICHA DE INSPECCION -LEVANTAMIENTO - RECAPTURE.XLSX` (presente en la
raíz del repo).

## Modelo de datos original

```csharp
// Pregunta de tipo Sí / No / N-A usada en los apartados de "Test de seguridad".
public class PreguntaTest
{
    public string Numero { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
}
```

Cada sección de "Test de seguridad" se renderizaba con un `ItemsRepeater` sobre una
`ObservableCollection<PreguntaTest>` propia, usando una plantilla común (fila con número,
texto de la pregunta y 3 `RadioButton`: Sí / No / N-A agrupados por `Numero`).

Las colecciones de test (definidas en el code-behind) eran:

**Cuadro de Comando (2.2.6)**
| Num | Texto |
|---|---|
| 2.2.6.1 | ¿Los límites finales de recorrido funcionan? |
| 2.2.6.2 | ¿Sistema de emergencia funciona? |
| 2.2.6.3 | ¿Sistema de maniobra en inspección funciona? |
| 2.2.6.4 | ¿Sistema de operación subir modo inspección funciona? |
| 2.2.6.5 | ¿Sistema de operación bajar modo inspección funciona? |
| 2.2.6.6 | ¿Sensor de falta o inversión de fase funciona? |
| 2.2.6.7 | ¿Relé térmico de sobrecarga funciona? |
| 2.2.6.8 | ¿Cuadro de comando está aterrado (< 5 OHMS)? |

**Máquina de Tracción (2.3.2)**
| Num | Texto |
|---|---|
| 2.3.2.1 | ¿Cables de tracción en buen estado? |
| 2.3.2.2 | ¿Frenos - bobinas / zapatas / tambor en buen estado? |
| 2.3.2.3 | ¿Máquina aterrada < 5 OHMS? |
| 2.3.2.4 | ¿Lonas / balatas con espesor de > 3mm? |
| 2.3.2.5 | ¿Acoplamiento sin holgura? |
| 2.3.2.6 | ¿Tambor de freno sin daños / marcas / rayaduras? |
| 2.3.2.7 | ¿Polea de tracción tiene protección? |

**Regulador de Velocidad (2.8.3)**
| Num | Texto |
|---|---|
| 2.8.3.1 | ¿Contacto de sobrevelocidad opera? |
| 2.8.3.2 | ¿Contacto desarme mecánico opera? |

**Puerta de Cabina (2.9.6)**
| Num | Texto |
|---|---|
| 2.9.6.1 | ¿Funciona barra de protección electrónica? |
| 2.9.6.2 | ¿Funciona contacto de puerta de cabina cerrada? |
| 2.9.6.3 | ¿Funciona circuito de fotocelda? |
| 2.9.6.4 | ¿Funciona límite de fuerza de puerta? |
| 2.9.6.5 | ¿Funciona fuerza de puerta? |
| 2.9.6.6 | ¿Holguras de puerta de cabina < 5 mm? |
| 2.9.6.7 | ¿Funciona contacto de límite puerta abierta? |
| 2.9.6.8 | ¿Funciona contacto de límite puerta cerrada? |
| 2.9.6.9 | ¿Holgura entre marco de puerta y hojas < 5mm? |
| 2.9.6.10 | ¿Puerta de cabina aterrada? |

**Techo de Cabina (2.10.4)**
| Num | Texto |
|---|---|
| 2.10.4.1 | ¿Pulsador stop funciona? |
| 2.10.4.2 | ¿Selector para inspección funciona? |
| 2.10.4.3 | ¿Pulsador subir funciona? |
| 2.10.4.4 | ¿Pulsador de bajada funciona? |
| 2.10.4.5 | ¿Iluminación en techo para mantenimiento funciona? |
| 2.10.4.6 | ¿Carro opera con protección para contacto accidental? |
| 2.10.4.7 | ¿Contacto de bloque / freno de seguridad opera? |
| 2.10.4.8 | ¿Pesador de carga al 110% funciona? |
| 2.10.4.9 | ¿Contacto de seguridad puerta de emergencia cabina funciona? |
| 2.10.4.10 | ¿Tiene protección de polea de doble tiro / tracción doble? |
| 2.10.4.11 | ¿Posee guarda de seguridad contra caídas? |
| 2.10.4.12 | ¿Luz de emergencia de cabina funciona? |
| 2.10.4.13 | ¿Alarma de cabina funciona? |
| 2.10.4.14 | ¿Botón de abrir puerta funciona? |
| 2.10.4.15 | ¿Selector normal inspección funciona? |
| 2.10.4.16 | ¿Cabina posee aterramiento < 5 OHMS? |
| 2.10.4.17 | ¿Iluminación interna de cabina funciona adecuadamente? |
| 2.10.4.18 | ¿Zapatas / roller / rosaderas en buenas condiciones? |
| 2.10.4.19 | Distancia entre parte externa de puerta de cabina y piso: ¿con cabina nivelada < 50mm? |
| 2.10.4.20 | Separación entre sardinel de cabina y piso en todos los niveles: ¿con cabina nivelada < 30mm? |
| 2.10.4.21 | ¿Piso de cabina está íntegro? |
| 2.10.4.22 | ¿Posee tapa vista en dintel / cornisa la cabina? |
| 2.10.4.23 | ¿Posee placa de señalización para teléfonos de emergencia? |

**Equipamientos de Foso (2.12.3)**
| Num | Texto |
|---|---|
| 2.12.3.1 | ¿Botón de stop en puerta de acceso a foso funciona? |
| 2.12.3.2 | ¿Contacto de puerta de inspección funciona? |
| 2.12.3.3 | ¿Iluminación en foso es adecuada? |
| 2.12.3.4 | ¿Límite de fin de curso de bajada funciona? |
| 2.12.3.5 | ¿Límites de parada en bajada funcionan? |
| 2.12.3.6 | ¿Límites de corte en alta de bajada funcionan? |
| 2.12.3.7 | ¿Contacto de estiramiento de cable del regulador funciona? |
| 2.12.3.8 | ¿Contacto de polea tensora de cinta selectora funciona? |
| 2.12.3.9 | ¿Contacto de pistón en cabina hidráulico funciona? |
| 2.12.3.10 | ¿Contacto de pistón en contrapeso hidráulico funciona? |
| 2.12.3.11 | ¿Contacto fin de curso en subida funciona? |
| 2.12.3.12 | ¿Límite de parada en subida funciona? |
| 2.12.3.13 | ¿Límite de corte en alta en subida funciona? |
| 2.12.3.14 | ¿Posee escalera de acceso a foso? |
| 2.12.3.15 | ¿Posee pared de 2.5 m en elevador adyacente? |

**Pavimento (2.13.4)**
| Num | Texto |
|---|---|
| 2.13.4.1 | ¿Contacto mecánico de puerta funciona? |
| 2.13.4.2 | ¿Contacto eléctrico de puerta funciona? |
| 2.13.4.3 | ¿Contacto de puerta modo inspección funciona? |
| 2.13.4.4 | ¿Holguras entre puertas y marco es de < 5mm? |

## Secciones del formulario (16 `Expander`, en orden)

1. **1. Datos Generales** — nombre del establecimiento, dirección, ciudad, teléfono, empresa
   actual de mantenimiento; tipo de inmueble (radios: Hotel, Shopping Center, Zona Comercial,
   Residencial, Escuela/Facultad, Hospital, Supermercado, Corporativo, Otros); tipo de equipo
   (radios: Pasajeros, Montacargas, Monta autos, Montacamillas, Discapacitados, Hidráulico,
   Usillo, Esc./Rampas, Otro).
2. **2. Datos Técnicos** — marca, modelo, fabricante, número original del fabricante, año de
   fabricación, modelo del cuadro de comando, número de paradas, recorrido en metros, tipo de
   llamadas, número de pasajeros, velocidad real con tacómetro (M/S), capacidad (KG).
3. **2.1 Cuarto de Máquinas** — localización (Sin cuarto de máquinas / Zona baja / Zona sup. /
   Con sala de máquinas / Sin sala de máquinas / Hidráulico); estado (checkboxes: pintar
   paredes/piso/máquina de tracción/cuadro de comando/generador-regulador, corregir
   iluminación, canaleta de puerta, con insectos/plagas); apreciación general (texto libre);
   dispositivos de rescate (Sí/No); poleas con protección de atrapamiento (Sí/No); protección
   en otro nivel (Sí/No/N-A); escalera para acceder a máquina (Sí/No); extintor CO2 (Sí/No);
   puerta de acceso (Íntegro/Quebrada/Sin llaves/No existe); tablero de fuerza (necesita
   limpieza/está normado; interruptor principal: Cuchilla/Disyuntor/Seccionadora/Fusible; Amp.,
   Capacidad, Tensión de red, Iluminación Íntegro/Averiado); fuerza de emergencia (Generador
   diesel / Generador estático a baterías / Otro).
4. **2.2 Cuadro de Comando** — tecnología (A relé/Microprocesador); tensión, N° CPU, N° I/O, N°
   EPROM 1°/2°; tipo de comando (Simples/Duplex/Otros); estado general (OK/Necesita
   reparación); ruido anormal (Sí/No); tipo de accionamiento (VVVF, AC-1 VEL., HD, CC-C7
   GENER., AC2-2 VEL., CC-CONVERSOR ESTÁTICO, ACVV); unidad IGBT/Driver, potencia-tensión,
   corriente, tensión, potencia (KW); **2.2.6 Test de seguridad en cuadro de comando**
   (realizado por: Departamento técnico/Ingeniería de campo + tabla `CuadroComandoTests`).
5. **2.3 Máquina de Tracción** — tipo (Sin engranaje/Con engranaje), modelo, rotación (RPM),
   fuga de aceite (Sí/No); **2.3.2 Test de seguridad** (tabla `MaquinaTraccionTests`).
6. **2.4 Polea de Tracción** — diámetro de la polea (MM), equalizar cables tractores (Sí/No),
   rectificar o sustituir polea (Sí/No).
7. **2.5 Freno de Máquina de Tracción** — tipo de freno (Disco/Tambor/Pinzas), modelo, tensión
   de bobina (V).
8. **2.6 Cables de Tracción** — diámetro de cables, cantidad; checkboxes de problemas de
   trabajo del cable (hilos rotos, corrosión, desgaste externo > 1/3, disminución de diámetro >
   5%, daños por alta temperatura).
9. **2.7 Motor de Tracción** — modelo, potencia, frecuencia (HZ); tensión (V), corriente
   nominal (A), corriente de partida; sistema motor generador (generador CV, tensión, regulador
   corriente, excitador tensión); estado del conjunto de tracción (checkboxes: vibración, ruido
   anormal, cables mal aislados, gomas de acoplamiento desgastadas, juego axial excesivo, tapas
   de terminales en orden).
10. **2.8 Regulador de Velocidad** — modelo, vel. de desarme, vel. de disparo eléctrico; estado
    (checkboxes: cable satisfactorio, ruido anormal, canal de polea profunda, contacto eléctrico
    dañado, cable dañado/oxidado, regulador con ruido/problemas); protección en regulador;
    **2.8.3 Test de seguridad** (tabla `ReguladorVelocidadTests`).
11. **2.9 Puerta de Cabina** — tipo (Acero/Aluminio); apertura (Automática/Manual/
    Pantográfica/Guillotina/Telescópica); revestimiento (Chapa pintada/Fierro/Madera/Latón/
    Lámina fórmica/Otros); estado (rasguñada, pintura desgastada, hendiduras/golpes);
    funcionamiento (ruido en trayecto, vibra en trayecto); **2.9.6 Test puerta de cabina**
    (tabla `PuertaCabinaTests`).
12. **2.10 Cabina** — pasajeros, entradas; tipo de terminación (Madera/Fórmica/Inoxidable/Base
    general); estado general de cabina: techo (lámparas quemadas, ruido en reactor, fijaciones
    mal puestas), difusor (Chapa/Acrílico, quebrado/dañado), pasamanos (sueltos, dañados),
    revestimiento (Laminado-fórmica/Pintura/Otro), estado general (rallada, golpeada,
    hendiduras, espejo quebrado), botonera (números apagados, carcasa quebrada, braille
    instalado), piso (Granito/Vulcapiso/Antideslizante/Otro), sardinel (suelto, crujido,
    desgastado, roda pies dañado); opcionales (ventilador, botón AP, botón CP, llave
    preferencial, llave de bomberos, llave de ventilador, accionamiento de liberación de
    emergencia); **2.10.4 Test de seguridad en techo de cabina** (tabla `TechoCabinaTests`).
13. **2.11 Indicador de Posición** — tipo (7/14/16 segmentos, Matriz de puntos, Luminoso);
    estado (lámpara quemada, segmentos/puntos apagados, difusor dañado, visor transparente
    dañado); dimensiones H/L; nomenclaturas PAV/Nomencl.
14. **2.12 Equipamiento de Foso** — estado general (Limpio/Con agua/Con basura); necesita
    cortar cables de tracción (Sí/No); última altura, profundidad de foso, dimensiones X e Y de
    ducto; tipo de guía de cabina (T-3/T-160/T-161/T-162), tipo de guía de CP.; **Test de
    seguridad de los equipamientos de foso** (tabla `FosoTests`).
15. **2.13 Puertas de Pavimento** — tipo de apertura (ALD/ALI/EVD/EVI/Guillotina/
    Pantográfica/Otra); revestimiento — amortiguadores hidráulicos (Piso/Embutidos/Fijos en
    base de bastidor con brazo); tipo de cierre electromecánico (Enganche/Pernos); **2.13.4
    Test de seguridad de pavimento** (tabla `PavimentoTests`).
16. **2.14 Señalización** — señalizadores (Gongo electrónico/Gongo mecánico/Linterna);
    indicadores de posición (Sobre el marco/En botonera de piso/No tiene); botonera de piso
    (checkboxes: botón único ACSD, botón doble, con braille, con flechas indicadoras).
17. **2.15 Niveles de Piso** — desnivel, acciones correctivas (texto libre).
18. **Firmas y Visto Bueno** — nombre del responsable; 3 cajas de firma (V°B° Realizó, V°B°
    Operaciones, V°B° Comercial).

## Notas para la reconstrucción

- El XAML original usaba los estilos `CampoCortoStyle` / `CampoMedioStyle` / `CampoAnchoStyle`
  (anchos de `TextBox`) y `SeccionExpanderStyle` — estos siguen existiendo en
  `MantenimientoPreventivoWindow.xaml` y se pueden copiar/compartir.
- Los grupos de `RadioButton` usaban nombres genéricos (`rbGroup1`, `rbGroup2`, ...) únicos por
  bloque de opciones — al reconstruir, conviene darles nombres descriptivos.
- No existía botón de guardado ni ningún endpoint asociado; toda la persistencia queda por
  definir cuando se retome esta funcionalidad.
