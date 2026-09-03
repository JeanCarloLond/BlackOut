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

### 3. No hay navegación

Se eliminó el teleport del template porque estaba atado a su suelo demo, y
**no se ha puesto nada en su lugar**. Ahora mismo solo te puedes mover
físicamente dentro del área de juego.

El local mide 10 × 8 m, así que hace falta locomoción. Decidir entre
teleport (menos mareo) o desplazamiento continuo, y montarlo sobre el
`XR Origin Hands (XR Rig)`.

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
- **04 Tocadiscos** — El disco no se ve girar sobre el plato (gira el plato,
  no el vinilo encajado). Falta el brazo siguiendo el surco.
- **05 Escenario** — La batería entera es **un solo instrumento**: tocar
  cualquier parte suena a tambor. Separar bombo, tom y platillo en tres
  interactables con su sonido.
- **06 Pósters** — Los 7 arrancan colgados. Falta el sonido de papel al
  desenrollar que pide el documento.
- **07 Televisor** — No hay mando a distancia; los botones están en la
  carcasa. El documento menciona cambiar de canal «con el control».
- **08 Dardos** — El marcador de tiza es una textura fija: no refleja la
  puntuación real de `Dartboard.Total`.
- **09 Abrir** — Al abrir suena la campanilla y el rumor de la gente, pero
  **no pasa nada más**. El documento pide que la puerta se abra y entre el
  ruido de la calle.

---

## ⚙️ Técnico

- **Build para Quest sin configurar.** El proyecto está en
  StandaloneWindows64. Hay que cambiar a Android, revisar el perfil de
  OpenXR y probar rendimiento real.
- **Rendimiento sin medir.** Se activaron las luces adicionales en
  `PerPixel` con límite 4 y sombras a 2048, más post-procesado completo.
  Es justo lo que un Quest sufre. Medir con el profiler antes de asumir
  que aguanta.
- **Sin iluminación horneada.** Todo es tiempo real. Considerar hornear
  lo estático (paredes, suelo, barra) para recuperar frames.
- **Sin prefabs.** Todo está construido directamente en la escena. La
  carpeta `Prefabs/` existe pero está vacía. Convendría al menos para
  botellas, vasos, dardos y vinilos.
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
