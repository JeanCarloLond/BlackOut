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

## 🟡 Contenido que falta

### Voz del dueño — estación 01

`BarStateManager` tiene los campos `vozEntrada` y `vozCierre` **vacíos**.
Los scripts los ignoran sin romperse, pero es la pieza que convierte el
blockout en la experiencia del documento:

- **Entrada:** «otra noche, a encender esto»
- **Cierre:** al girar el letrero a OPEN

### Anécdotas de los pósters — estación 06

Los 7 componentes `Poster` tienen el campo `anecdota` **vacío**. El
documento pide que al fijar un cartel suene la historia de esa banda.
`PosterSlot` ya está preparado: solo hay que grabar los audios y
asignarlos.

### Sonido: los SFX son de interfaz reutilizados

Los 19 efectos vienen de **Kenney Interface Sounds** (CC0), renombrados
según su uso. Funcionan, pero se nota que son sonidos de UI en algunos:

- `SFX_Servir` y `SFX_Trago` son tintineos de vidrio, no líquido
- `SFX_Aguja` es un *scratch* de interfaz
- `SFX_Reja_Riel` no suena a metal corriendo

Prioridad de sustitución: servir, trago, aguja, reja.

### Vídeo de la televisión — estación 07

Los tres canales son **cartas de ajuste generadas con ffmpeg** (barras
SMPTE, estática, test pattern). Sustituir por metraje real de conciertos.
Requisito: **H.264**, o Unity lo rechaza con
`Could not find supported video track`.

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
- **03 Barra** — El líquido del vaso es un cilindro que escala. No tiene
  superficie ni se mueve al inclinar. Los grifos de cerveza son decorado:
  no sirven.
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
- **06 Pósters** — Los 7 arrancan colgados. Falta el sonido de papel al
  desenrollar que pide el documento.
- **07 Televisor** — No hay mando a distancia; los botones están en la
  carcasa. El documento menciona cambiar de canal «con el control».
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

- **Build para Quest sin configurar.** El proyecto está en
  StandaloneWindows64. Hay que cambiar a Android, revisar el perfil de
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
- Taburetes alrededor de las mesas altas
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
