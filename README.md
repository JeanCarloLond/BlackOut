# Blackout Bar

> Enciende el bar con tus manos antes de abrir.

Experiencia inmersiva de realidad virtual para el curso de **Universos y
Realidades Mixtas**. Eres el dueño de un bar de rock y llegas antes de
abrir, con el local a oscuras. El recorrido consiste en despertarlo:
encender las luces, ordenar la barra, poner un disco, probar los
instrumentos, y girar el letrero a OPEN.

Sin puntaje y sin forma de perder. El propósito es recreativo: el placer
de habitar y preparar el bar con las manos.

> **El bar está vacío y a oscuras porque tú eres el dueño y llegas antes de
> abrir.** No es una carencia de contenido: es la premisa. Toda la
> experiencia es el ritual de apertura — encender, ordenar, poner música,
> servirte un trago — y por eso **no hay NPCs**. La única persona en el
> local eres tú, hasta que giras el letrero y entra el rumor de la gente.

**Autores:** Jean Carlo Londoño y Alejo · **Entrega 01**

---

## Requisitos

| | |
|---|---|
| Unity | **6000.3.19f1** (Unity 6.3 LTS) |
| Render pipeline | Universal RP 17.3.0 |
| XR | XR Interaction Toolkit 3.4.1 · XR Hands 1.7.3 · OpenXR 1.16.1 |
| Extras | ProBuilder 6.1.2 · glTFast 6.20.0 |
| Control de versiones | Git + **Git LFS** (obligatorio) |

### Clonar

```bash
git lfs install
git clone https://github.com/JeanCarloLond/BlackOut.git
```

Sin `git lfs install` los modelos, audios y texturas se descargan como
punteros de texto y el proyecto se abre roto.

### Escena

Abrir **`Assets/_Blackout/Scenes/BlackoutBar.unity`**.

`Assets/Scenes/SampleScene.unity` es la escena del template de Unity y se
conserva solo como referencia. No se trabaja ahí.

---

## Cómo está organizado

Todo lo propio del proyecto vive bajo `Assets/_Blackout/`:

```
_Blackout/
├── Scenes/      BlackoutBar.unity      ← la escena
├── Scripts/     20 scripts en el namespace Blackout
├── Materials/   materiales del local y los props
├── Models/      Instrumentos/ y Bar/ (glTF de Poly Pizza)
├── Textures/    PBR/ · Posters/ · Letrero/ · Diana.png
├── Audio/       temas de los vinilos + SFX/
├── Video/       canales de la televisión
├── Settings/    Blackout_PostFX.asset
└── CREDITOS.md  autoría y licencia de cada asset de terceros
```

En la jerarquía de la escena, cada estación es un objeto raíz llamado
`Estacion_0X_Nombre`. El estado global vive en `GameManager`.

---

## Arquitectura

### `BarStateManager` — el hilo conductor

Cuatro fases, en un solo sentido:

```
Street  →  Inside  →  Lit  →  Open
 calle     dentro    luces   abierto
```

- **`Street`** — fuera, con la reja abajo y el ruido de la calle.
- **`Inside`** — al abrir la reja. Crossfade de la calle al ambiente
  interior y voz del dueño.
- **`Lit`** — al bajar la cuarta palanca. **Habilita el resto de
  estaciones**: encender es lo que abre el juego, guiando sin forzar.
- **`Open`** — al girar el letrero. Cierre de la experiencia.

Las estaciones consultan `BarStateManager.Instance.HasLight` y avanzan la
fase con `AdvanceTo(...)`. Nunca retrocede.

### Las nueve estaciones

| # | Estación | Scripts | Qué hace |
|---|---|---|---|
| 01 | La entrada | `EntranceGate` | Apuntas, gatillo, sube la reja. Golpe de pestillo y crossfade de ambiente |
| 02 | Luces | `LightSwitch` ×4, `LightSwitchBank` | Cada palanca enciende su luz con chisporroteo y zumbido. La cuarta pasa el bar a `Lit` |
| 03 | La barra | `Bottle` ×4, `Glass` ×2, `ShelfSlot` ×4, `BarOrderTracker` | Servir por inclinación, beber acercando el vaso, ordenar el estante |
| 04 | Tocadiscos | `Turntable`, `Vinyl` ×5, `BarMusicController` | Pones un vinilo, bajas la aguja, suena en todo el local |
| 05 | El escenario | `StageInstrument` ×5, `StageController` | Batería, guitarras y micro. El primer sonido enciende los focos |
| 06 | Muro de pósters | `Poster` ×7, `PosterSlot` ×7 | Colgar y reacomodar carteles; cada uno narra su anécdota |
| 07 | El televisor | `Television` | Encendido con estática y cambio de canal, con `VideoPlayer` real |
| 08 | Los dardos | `Dart` ×3, `Dartboard` | Se clavan por velocidad. Puntúa 50/25/10/5 y **nunca resta** |
| 09 | Abrir | `OpenSign` | Cartel colgado de una bisagra: lo giras con la mano y el neón sube con el ángulo |

`HapticFeedback` es un helper compartido: cada acción devuelve su propia
vibración al mando que realmente está interactuando.

---

## Decisiones que conviene no deshacer

**Las luces adicionales están en `PerPixel` a propósito.** El template VR
las traía en `Disabled` para rendimiento en Quest, lo que hacía que
**ningún point light se dibujara**. Toda la experiencia se apoya en la luz
roja, así que se activaron en los dos assets URP del proyecto, con límite
de 4 luces por objeto y atlas de sombras de 2048. Es un coste asumido.

**El bar arranca a oscuras y eso es el diseño.** Si abres la escena y no
ves nada, es correcto: las luces están apagadas hasta que se accionan las
palancas en Play.

**Las estaciones se habilitan con la luz.** No es un bug que el letrero no
responda al principio: rechaza el gesto hasta que el bar está encendido.

**El rótulo de la fachada arranca apagado. No es un bug.** Llegas de noche
a un local cerrado; el neón es la recompensa del final, no el decorado del
principio. `OpeningFinale` lo enciende cuando giras el letrero a OPEN. Si
lo ves oscuro en el editor, está bien.

**Post-procesado.** `Blackout_PostFX.asset` (bloom, ACES, vignette, grano)
es lo que hace que la luz roja tenga profundidad. Sin él la escena se ve
plana.

---

## Licencias

El proyecto usa material de terceros CC0 y **CC BY**. Los CC BY (modelos de
Poly Pizza) **exigen atribución**: si esto se presenta o entrega,
[`Assets/_Blackout/CREDITOS.md`](Assets/_Blackout/CREDITOS.md) tiene que
acompañarlo.

---

## Estado

Las nueve estaciones están montadas, vestidas y sonorizadas.

⚠️ **Nada se ha ejecutado nunca en Play.** Todo el desarrollo se hizo desde
el editor. Ver [`TODO.md`](TODO.md) para lo que falta y para los valores
que hay que calibrar con el visor puesto.
