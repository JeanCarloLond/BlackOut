# Blackout Bar — Qué falta

Estado a fecha del último commit. Ordenado por lo que más bloquea.

---

## 🔴 Bloqueante

### 1. Nada se ha ejecutado nunca en Play

Todo el proyecto se construyó desde el editor con el MCP. **Ni una sola
vez se ha entrado en modo Play**, ni en el editor ni en el visor. Compila
limpio y las referencias están cableadas, pero el comportamiento en runtime
está sin verificar.

**Qué hacer:** ponerse las Quest y jugar una vuelta completa —
reja → luces → barra → disco → escenario → dardos → letrero — anotando
todo lo que se sienta raro.

### 2. Valores que hay que calibrar con el cuerpo

Todos son números de partida elegidos a ciegas. Están en el inspector.

| Dónde | Campo | Valor actual | Qué mirar |
|---|---|---|---|
| `Bottle` (4 botellas) | `anguloVertido` | 60° | ¿Sirve solo al inclinar de verdad, o se derrama sin querer? |
| `Bottle` | `caudal` | 0.4 /s | ¿Tarda demasiado en llenar el vaso? |
| `Glass` (2 vasos) | `distanciaBoca` | 0.3 m | ¿Bebe cuando quieres, o se dispara antes de llegar? |
| `Glass` | `anguloTrago` | 40° | Igual que arriba |
| `Dart` (3 dardos) | `velocidadMinima` | 1.2 m/s | ¿Se clavan al lanzar, o rebotan siempre? |
| `OpenSign` (cartel) | `anguloApertura` | 140° | ¿Cuánto hay que girarlo para que cuente? |
| Cartel `Rigidbody` | `mass` / `angularDamping` | 0.25 / 1.4 | Si gira como molinillo, sube el damping. Si cuesta, baja la masa |
| `StageInstrument` | `velocidadMinima` | 0.55 | Solo afecta al modo `Golpear` |
| `LightSwitch` (4) | chisporroteo | 2 parpadeos | ¿Molesta o marea en VR? |

### 3. Navegación: montada, sin probar

> Corrección: una versión anterior de este documento decía que no había
> navegación. Era falso. El rig sí conserva toda la pila de locomoción del
> template; lo que se había borrado eran los **destinos** de teleport, no
> los proveedores.

Estado actual sobre `XR Origin Hands (XR Rig) → Locomotion`:

| Proveedor | Estado |
|---|---|
| `DynamicMoveProvider` | activo — desplazamiento continuo |
| `ContinuousTurnProvider` / `SnapTurnProvider` | ambos activos |
| `TeleportationProvider` | activo, **ya con destinos** |
| `GravityProvider`, `ClimbProvider`, `JumpProvider` | activos |

Se añadieron `TeleportationArea` a `Piso_Bar`, `Calle_Piso` y
`Tarima_Escenario`, con `matchOrientation: WorldSpaceUp` y disparo al
soltar. Antes el teleport apuntaba a la nada.

**Qué falta:** probarlo. Y decidir si dejar **los dos giros a la vez**
(snap y continuo están ambos habilitados), que puede dar comportamiento
raro según cómo esté configurado el `ControllerInputActionManager`.

---

### 4. Sistema de puntos: probar con el visor

Montado en la escena con `Blackout → Montar sistema de puntos` y probado en
Play **desde el editor, sin visor**, llamando a `BarScore` con los objetos
reales de la escena:

- Los siete carteles arrancan colgados y el total sigue en 0: la
  colocación inicial no puntúa.
- La misma palanca dos veces suma una sola vez; los dardos se cortan en 150.
- La pizarra aparece al haber luz, repasa en rojo lo completado y se lee al
  derecho desde la barra.
- Al abrir, el veredicto sale frente a la vista, el rumor de gente cambia de
  volumen y el récord se guarda.

**Lo que falta es hacer los gestos con las manos**, que es lo que no se
puede simular desde el editor:

| Qué | Por qué |
|---|---|
| ¿La pizarra se lee desde la barra con el visor? | Letra de ~4 cm a 1,5 m. Si cuesta, sube `fontSize` de `Lineas` |
| ¿Los «+N» se leen y no marean? | `tamPopup`, `segundosPopup`, `subidaPopup` en `BarScore` |
| ¿Los umbrales de la noche son justos? | 40 % y 75 % se eligieron a ciegas. Una vuelta normal debería dar *buena* |

Antes de una demo: `Blackout → Borrar mejor noche`.

**Recorrido simulado (27/10):** se hizo una vuelta completa en Play
disparando los gestos reales (eventos de XRI y dardos lanzados con física),
de la reja al letrero: 785 puntos, *buena noche*. Salieron tres cosas:

- **La diana tenía un `CapsuleCollider` de 1 × 2 m de más.** Los dardos
  chocaban con él medio metro antes de la diana, se quedaban en el aire y
  siempre marcaban 5. Se quitó; ahora se clavan en el corcho y puntúan bien.
- **Los «+N» rojos no se leían** sobre el estante retroiluminado ni con las
  luces rojas. Ahora son blanco cálido. El de la tele sale encima de la
  pantalla, no sobre ella.
- **En Linux los canales de la tele salen vacíos:** el editor de Linux no
  importa vídeo H.264. En Windows no pasa. No es un problema del repo.

**Arreglado de paso:** `Marcador_Numero` (el 501 de los dardos) se veía en
espejo desde la sala. Tenía la rotación del cubo de tiza de detrás, pero
TextMeshPro se lee mirando hacia su forward; se giró 180° en Y.

---

### 5. El Chancho y la borrachera: probar con el visor

Montados con sus menús y probados en Play **sin visor**, disparando los
caminos reales: el Chancho aguanta de pie, un golpe flojo no lo tumba, uno
fuerte sí, el trigger de la mano lo golpea, agarrarlo lo deja sin fuerzas,
sale lanzado y se levanta a los 8 s. Beber del vaso y de la botella a
morro sube el efecto, y se ve con la cámara del jugador.

Con el visor puesto falta calibrar:

| Dónde | Campo | Valor | Qué mirar |
|---|---|---|---|
| `GolpeMano` (los dos mandos) | `fuerza` / `velocidadMinima` | 2.2 / 1.2 m/s | ¿Un puñetazo normal lo tumba? ¿Un roce lo empuja sin querer? |
| `Parroquiano` | `umbralNoqueo` | 7 N·s | Igual que arriba |
| `Parroquiano` | `muelleCadera` | 700 | Si apenas se tambalea, bájalo; si se cae solo, súbelo |
| `Borrachera` | `porVaso` / `recuperacion` | 0.35 / 0.008 | ¿Cuántos tragos hasta notarlo? ¿Dura demasiado? |
| `Borrachera` | `intensidadMaxima` | 1 | **Si marea, bajarlo.** Al máximo es fuerte |
| `Bottle` | `distanciaBoca` / `anguloTrago` | 0.3 m / 50° | ¿Bebe al acercarla, o se dispara sin querer al servir? |

Más pendientes:
- **Rendimiento:** activar el post-procesado en el renderer de Android
  cuesta en Quest, y el desenfoque de la borrachera más. Medir con el profiler.
- **El post-procesado base oscurece algo la escena** (el ACES). Si queda
  demasiado oscuro, subir `postExposure` o las luces, no quitar el ACES.
- **Sonidos:** el golpe usa `SFX_Tambor` y el noqueo `SFX_Platillo`, que
  son de relleno. `Borrachera` tiene un campo `clipHipo` vacío.
- **Sin puntos:** el Chancho no suma en `BarScore`. Si se quiere, añadir
  un hito (por ejemplo, lanzarlo lejos) y una línea en la pizarra.

### 6. Lo añadido para el one page: probar con el visor

Montado con `Blackout → Onepage → Montar todo` y probado en Play **sin
visor**: el grifo llena el vaso con espuma y suma el trago servido, el vaso
mezcla el color de dos licores, el brillo aparece al apuntar, el mando
enciende la tele y cambia de canal, dos vasos que chocan suenan. Falta:

| Dónde | Campo | Valor | Qué mirar |
|---|---|---|---|
| `BrilloAgarrable` | `intensidad` | 0.5 | ¿Se nota con la luz roja? ¿Molesta al tener muchas cosas cerca? |
| `Grifo` | `caudal` / `anguloAbierto` | 0.3 / 35° | ¿Tarda lo justo en llenar? ¿La maneta se siente bien al tirar? |
| `Glass` | `bajadaEspuma` / `velocidadBrindis` | 0.04 / 0.35 m/s | ¿La espuma dura lo creíble? ¿Suena al chocar sin querer? |
| `Bottle` | `graduacion` | 0.8 a 1.1 | Con la cerveza a 0.35, ¿la diferencia se nota? |

- **Rendimiento:** el vidrio de vasos y botellas ahora es transparente,
  y el mobiliario suma unas 80 piezas (con batching estático). Medir.
- **Recorrido:** las mesas del centro reducen el espacio libre. Comprobar
  que el teletransporte no deja al jugador dentro de una silla.

| Dónde | Campo | Valor | Qué mirar |
|---|---|---|---|
| Batería (`StageInstrument`, modo Percutir) | `velocidadPercusion` / `velocidadMaxima` | 0.6 / 4 m/s | ¿Suena con un golpe normal? ¿Se dispara al pasar la mano cerca? |
| Guitarras | `distanciaMastil` | 0.15 m | ¿Se pisa el traste sin tener que atinar demasiado? |
| `Dart` | `alineacion` / `velocidadMinima` | 18 / 1.2 m/s | ¿Vuela recto? ¿Se clava con un lanzamiento suave? |
| `LightSwitch` (Palanca_01) | `escalaPorLuz` | 0.35 en el estante | ¿Se ven las botellas sin que la barra se queme? |

- **Sonidos sintetizados:** la batería y las guitarras están hechas por
  código. Si en el visor suenan pobres, sustituir los `.wav` de
  `Audio/Instrumentos/` manteniendo el nombre.
- **Sin Quest para probar:** `PruebaManosSimuladas` cubre la lógica con
  mandos simulados, pero la puntería, la fuerza y el rendimiento solo se
  ven con unas gafas.

---

## 🟡 Contenido que falta

### Voz del dueño y anécdotas — estaciones 01 y 06

**Hecho, con voz de prueba.** `Audio/Voz/` tiene la voz del dueño al
entrar y al abrir, y una anécdota por cartel, generadas con Piper (TTS
local). Suenan a voz sintética. Lo ideal es grabarlas con voz propia:
basta con sustituir cada `.wav` manteniendo el nombre. Los textos están
en el commit que las añadió y son recuerdos del dueño sobre cada cartel,
no datos sobre las bandas reales.

### Sonido: los SFX son de interfaz reutilizados

Los 19 efectos vienen de **Kenney Interface Sounds** (CC0), renombrados
según su uso. Funcionan, pero se nota que son sonidos de UI en algunos:

- `SFX_Servir` y `SFX_Trago` son tintineos de vidrio, no líquido
- `SFX_Aguja` es un *scratch* de interfaz
- `SFX_Reja_Riel` no suena a metal corriendo

Prioridad de sustitución: trago, aguja, reja. Servir ya usa `AMB_Chorro`,
un chorro sintetizado con ffmpeg; si suena raro en el visor, buscar uno
grabado.

### Vídeo de la televisión — estación 07

**Hecho.** Canal 1: *St. Louis Blues* (1929, dominio público). Canal 2:
canal propio con la música de la casa. Canal 3: estática. Son **WebM VP8**,
que Unity importa en todas las plataformas; el H.264 de antes no se
importaba en el editor de Linux. La pantalla emitía un color plano que
tapaba el vídeo (se veía blanca): ahora el vídeo es el mapa de emisión.

### El bajo no es un bajo

`Bajo` en la escena reutiliza la malla de la guitarra eléctrica, un 18 %
más grande y afinada una octava por debajo. Suena a bajo pero no lo
parece. No hay ninguno decente en Poly Pizza; buscar en otra fuente CC0.

---

## 🟢 Por estación

- **01 Entrada** — La reja sube 2,1 m de golpe. Podría quedar mejor con
  sonido de riel en bucle mientras sube, no un `PlayOneShot`.
- **02 Luces** — Las cuatro palancas hacen lo mismo salvo la 1, que además
  enciende el estante retroiluminado. Estaría bien que cada una encendiera
  una zona distinta del local.
- **03 Barra** — El líquido del vaso es un cilindro que escala y no se
  nivela al inclinar. ~~Los grifos de cerveza son decorado.~~ **Hecho:**
  `Grifo` en las tres manetas, con espuma en el vaso. El vidrio ahora es
  transparente.
- **04 Tocadiscos** — ~~El disco no se ve girar sobre el plato.~~
  **Hecho:** se añadió `Eje_Plato`, un pivote sin escala del que cuelgan el
  plato y el socket, así que el vinilo encajado gira con él. El brazo
  también recorre el surco: se cierra de −28° a −46° a lo largo de cuatro
  minutos de cara.
- **05 Escenario** — ~~La batería entera es un solo instrumento.~~
  **Hecho:** ahora son 10 piezas tocables por separado (`BigKick`,
  `Bigsnare`, dos toms, dos timbales, tres platillos y el charles), cada
  una con su rango de tono. Falta comprobar que los colliders de los
  platillos, que son láminas de 2 cm, se puedan tocar con la mano.
- **06 Pósters** — Los 7 arrancan colgados. ~~Falta el sonido de papel.~~
  **Hecho:** `SFX_Papel` al descolgar.
- **07 Televisor** — ~~No hay mando a distancia.~~ **Hecho:** `MandoTele`
  sobre la barra, con el gatillo enciende y cambia de canal.
- **08 Dardos** — ~~El marcador de tiza es una textura fija.~~
  **Hecho:** `ChalkScoreboard` arranca en 501 y descuenta con cada dardo,
  clavado sobre un `TextMeshPro`. Se queda en cero si te pasas: no hay
  castigo, como pide el documento. La textura de tiza conserva los nombres
  y las rayas de conteo, que siguen siendo decorativas.
- **09 Abrir** — ~~Al abrir solo suena la campanilla.~~
  **Hecho:** nuevo `OpeningFinale`. El rótulo de la fachada arranca
  **apagado** y al pasar a `Open` prende con tres parpadeos de neón viejo,
  sube su luz sobre la calle y vuelve el rumor de la gente. Falta la puerta
  física abriéndose: ahora mismo el vano solo tiene la reja, que ya subió
  en la estación 01.

---

## ⚙️ Técnico

- **Build para Quest sin probar.** OpenXR para Android ya tiene el soporte
  de Meta Quest, los perfiles de mando Touch y el seguimiento de manos. El
  proyecto se edita en Linux/Windows. Hay que cambiar a Android, revisar el perfil de
  OpenXR y probar rendimiento real.
  *Ya hecho:* `BlackoutBar` es la escena 0 en Build Settings y la
  `SampleScene` del template quedó desactivada. Antes la escena del
  proyecto **no estaba incluida en el build**.
- **Rendimiento sin medir.** Se activaron las luces adicionales en
  `PerPixel` con límite 4 y sombras a 2048, más post-procesado completo.
  Es justo lo que un Quest sufre. Medir con el profiler antes de asumir
  que aguanta.
  *Ya hecho:* **ningún point light proyecta sombra.** Cada uno cuesta seis
  mapas (uno por cara del cubemap), y las diez de la escena habrían pedido
  60. Ahora solo proyectan los tres focos de tipo Spot, a un mapa cada uno.
  Si al probar faltan sombras, súbelas en los Spot antes que en los Point.
- **Sin iluminación horneada.** Todo es tiempo real. Considerar hornear
  lo estático (paredes, suelo, barra) para recuperar frames.
- **Prefabs: primera tanda hecha.** Hay siete en `Assets/_Blackout/Prefabs/`
  — `Botella`, `Vaso`, `Dardo`, `Vinilo`, `Cartel`, `Mesa_Alta` y
  `Palanca_Interruptor` — y la instancia original de cada uno quedó
  conectada. **Las demás copias en la escena siguen sueltas**: las otras
  tres botellas, el segundo vaso, los dos dardos restantes, los cuatro
  vinilos y los seis carteles. Reemplazarlas por instancias del prefab para
  que editar uno los cambie todos.
- **Sin tests.** No hay ni un test.

---

## 💡 Pulido opcional

- Humo o niebla volumétrica sobre la tarima
- Reflejo real en la barra (ahora hay una sonda, pero es básica)
- Variedad en las texturas de póster: los 4 logos usan el mismo papel
- ~~Taburetes alrededor de las mesas altas~~ **Hecho**, más una mesa
  redonda y una larga con sillas en el centro
- Un espejo detrás de la barra
- Puerta real en el vano, además de la reja

---

## Cómo seguir trabajando con Claude

El repo incluye una skill en
[`.claude/skills/blackout-bar/SKILL.md`](.claude/skills/blackout-bar/SKILL.md)
con las convenciones del proyecto y las trampas que ya nos costaron tiempo
(el MCP de Unity, las UV de los cubos, las escalas de Poly Pizza, las
ambigüedades del compilador de `execute_code`…).

Se carga sola cuando Claude Code trabaja en este repositorio.
