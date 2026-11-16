# Blackout Bar

> Enciende el bar con tus manos antes de abrir.

![La fachada del bar de noche, con el rótulo apagado y el aplique sobre la puerta](Assets/_Blackout/Textures/Docs/blackout_bar_exterior.png)

*Donde empieza todo: llegas a la calle, de noche, con el bar cerrado y el
neón muerto.*

![La misma fachada con el bar ya encendido: las dos ventanas arden en rojo](Assets/_Blackout/Textures/Docs/blackout_bar_ventanas.png)

*La misma fachada cuando ya has encendido el local por dentro. Las ventanas
son huecos reales en el muro, así que la luz roja sale de verdad a la calle.*

![El bar ya encendido: barra retroiluminada, banquetas, tele de tubo y el escenario al fondo](Assets/_Blackout/Textures/Docs/blackout_bar_hero.png)

*Y esto es lo que hay dentro cuando lo has encendido. Al empezar está todo
a oscuras.*

Experiencia inmersiva de realidad virtual para el curso de **Universos y
Realidades Mixtas**. Eres el dueño de un bar de rock y llegas antes de
abrir, con el local a oscuras. El recorrido consiste en despertarlo:
encender las luces, ordenar la barra, poner un disco, probar los
instrumentos, y girar el letrero a OPEN.

Sin forma de perder. El propósito es recreativo: el placer de habitar y
preparar el bar con las manos. Hay **puntos**, pero miden lo bien preparado
que dejas el bar al abrir, nunca restan, y deciden cómo sale la noche.

> **El bar está vacío y a oscuras porque tú eres el dueño y llegas antes de
> abrir.** No es una carencia de contenido: es la premisa. Toda la
> experiencia es el ritual de apertura — encender, ordenar, poner música,
> servirte un trago — y por eso no hay clientes. La única excepción es
> **el Chancho**, el parroquiano que nunca se va: lo pidió el profesor, un
> personaje al que se puede golpear y lanzar.

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
├── Scripts/     32 scripts en el namespace Blackout, más Editor/ y Demo/ (herramientas solo de editor)
├── Materials/   materiales del local y los props
├── Models/      Instrumentos/ y Bar/ (glTF de Poly Pizza)
├── Textures/    PBR/ · Posters/ · Letrero/ · Diana.png
├── Audio/       temas de los vinilos + SFX/ + Voz/ + Instrumentos/
├── Video/       canales de la televisión (WebM VP8)
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
| 03 | La barra | `Bottle` ×4, `Glass` ×2, `Grifo` ×3, `ShelfSlot` ×4, `BarOrderTracker` | Cuatro licores y tres cervezas de grifo con espuma. Servir por inclinación, beber acercando el vaso, brindar, ordenar el estante |
| 04 | Tocadiscos | `Turntable`, `Vinyl` ×5, `BarMusicController` | Pones un vinilo, bajas la aguja, suena en todo el local |
| 05 | El escenario | `StageInstrument` ×14, `StageController` | La batería se golpea con la mano o con las baquetas; las guitarras y el bajo tocan cuatro acordes según el traste que pise la otra mano. El primer sonido enciende los focos |
| 06 | Muro de pósters | `Poster` ×7, `PosterSlot` ×7 | Colgar y reacomodar carteles; cada uno narra su anécdota |
| 07 | El televisor | `Television`, `MandoTele` | Encendido y cambio de canal con los botones o con el mando. Un concierto de 1929, un canal propio y estática |
| 08 | Los dardos | `Dart` ×3, `Dartboard` | Se lanzan con el brazo: salen con la velocidad de la mano al soltar. Vuelan con la punta por delante y se clavan por velocidad. Puntúa 50/25/10/5 y **nunca resta** |
| 09 | Abrir | `OpenSign` | Cartel colgado en la **fachada**, de cara a la calle. Lo giras con la mano y el neón sube con el ángulo |

`HapticFeedback` es un helper compartido: cada acción devuelve su propia
vibración al mando que realmente está interactuando.

`BrilloAgarrable` va en todo lo interactivo: al apuntarlo con la mano o el
rayo se ilumina con un brillo cálido. Es la restricción de usabilidad del
one page («solo lo interactivo reacciona con un brillo cálido»).

### Lo que se añadió para parecerse al one page

`Blackout → Onepage → Montar todo` lo monta todo de una vez (cada paso
tiene además su propio menú):

- **Mobiliario:** una mesa redonda con tres sillas y una larga con cuatro
  en el centro, como en el dibujo, y dos taburetes por mesa alta. Todo
  queda fuera del carril de los dardos y deja libre el paso de la entrada
  al escenario. Las banquetas tienen collider, así que el Chancho ya no
  las atraviesa.
- **Licores:** cada botella es un licor distinto con su color, su
  graduación y su etiqueta (marcas inventadas: Ron Tres Acordes, Whisky El
  Ampli, Ginebra Acople y Licor Verde Bis). El vidrio es transparente y se
  ve lo que queda dentro. El vaso mezcla lo que se le sirve, en color y en
  graduación: una cerveza emborracha menos que un ron.
- **Cerveza de grifo:** las tres manetas, que eran decorado, ahora se
  agarran y se tiran hacia uno. Rubia, roja y negra, con espuma que baja
  sola.
- **Brindar:** chocar dos vasos suena y vibra en las dos manos.
- **Tele:** los canales son WebM VP8, que Unity importa en todas las
  plataformas (el H.264 no se importa en el editor de Linux). El canal 1 es
  *St. Louis Blues* (1929), de dominio público; el 2 es un canal propio con
  la música de la casa. Hay un mando sobre la barra.
- **Voces:** la voz del dueño al entrar y al abrir, y una anécdota por
  cartel. Son locuciones de prueba con Piper (ver `CREDITOS.md`); se
  cambian por grabaciones propias con el mismo nombre de archivo.
- **Imagen:** sin grano de película y con filtrado anisotrópico 4 en
  suelo y muros, que en VR casi siempre se ven en ángulo.
- **Vinilos:** dos discos tenían el material vacío y salían en magenta.
  Ahora todos son negros con galleta de color.
- **Instrumentos:** cada pieza de la batería tiene su sonido (sintetizado,
  sin licencias) y suena al golpearla con la mano o con una de las dos
  baquetas, más fuerte cuanto más rápido. Las guitarras tocan Mi, Sol, La
  y Do según el traste: la cejuela y la unión con el cuerpo se calculan de
  la malla y quedan como `Mastil_Inicio` y `Mastil_Fin`.
- **Pizarra de cómo se juega:** en la acera, junto a la reja. Explica
  agarre, gatillo, lanzar, caminar y girar con la tiza del bar.
- **Luz de la barra:** las tres luces del estante iban a intensidad
  completa a medio metro de la barra y la quemaban. `LightSwitch` admite
  ahora una escala por luz.
- **Dardos:** punto de agarre en el barril (la punta queda hacia delante
  del mando) y alineación con el vuelo.
- **Quest:** la borrachera quita el desenfoque de profundidad en móvil,
  que es lo más caro del efecto.

---

### `BarScore`: el sistema de puntos

Cada gesto del ritual de apertura suma. Las estaciones lo anotan con una
línea, igual que avanzan la fase:

```csharp
BarScore.Anotar(Hito.Palanca, this, transform.position);
```

Tres reglas sostienen el diseño:

- **Nunca resta.** El documento pide una experiencia sin forma de perder.
  Derramar, fallar un dardo o reiniciar el 501 no cuestan nada.
- **Cada cosa cuenta una vez.** La misma palanca, el mismo disco, el mismo
  cartel. Para sumar hay que recorrer el local, no repetir un gesto.
- **Cada hito tiene tope.** Los dardos, que son lo único repetible, aportan
  como mucho 150. El máximo de la noche es **1335**.

| # | Hito | Puntos | Tope |
|---|---|---|---|
| 01 | Subir la reja | 50 | 50 |
| 02 | Cada palanca · las cuatro abajo | 25 · 100 | 100 · 100 |
| 03 | Botella en su hueco · estante completo | 20 · 60 | 80 · 60 |
| 03 | Servir un vaso · vaciarlo bebiendo | 40 · 30 | 80 · 60 |
| 04 | Cada vinilo distinto que suena | 25 | 125 |
| 05 | Despertar el escenario · cada instrumento distinto | 50 · 10 | 50 · 100 |
| 06 | Volver a colgar un cartel | 15 | 105 |
| 07 | Encender la tele · cada canal distinto | 30 · 15 | 30 · 45 |
| 08 | Lo que marque la diana · cerrar el 501 | 5–50 · 100 | 150 · 100 |
| 09 | Girar el letrero a OPEN | 100 | 100 |

Los valores están en el inspector de `BarScore`, en `GameManager`.

**Dónde se ve.** Todo es diegético, no hay HUD:

- `ScorePopup`: el «+25» sube y se apaga donde hiciste el gesto, con
  `SFX_Puntaje`. Los hitos grandes (reja, bar encendido, abrir) aparecen
  frente a la vista.
- `BarScoreBoard`: la **pizarra de la noche**, junto a la de precios,
  sobre el estante. Una línea por estación, a tiza; la que completas se
  repasa en rojo neón. Abajo el total y la mejor noche. Está apagada hasta
  que hay luz: TextMeshPro no recibe iluminación y brillaría sola a oscuras.

**Qué decide.** Al girar el letrero, el total fija cómo sale la noche:
*tranquila* (menos del 40 %), *buena* o *legendaria* (75 % o más). Se anuncia
frente a la vista, porque en ese momento estás fuera, y **se oye**: el rumor
de gente que entra suena más fuerte cuanto mejor preparaste el bar.

**La mejor noche se guarda** entre sesiones con `PlayerPrefs`.
`Blackout → Borrar mejor noche` la limpia antes de una demo.

**Montarlo en la escena:** `Blackout → Montar sistema de puntos`. Añade
`BarScore` al `GameManager`, cuelga la pizarra y cablea fuente y sonidos.
Se puede repetir: rehace la pizarra.

---

### El Chancho: el parroquiano

`Parroquiano` + `GolpeMano`. Un cerdito original hecho con primitivas,
camiseta negra y vaqueros, junto a la barra. Personaje propio a propósito:
el profesor sugirió uno conocido de una serie infantil, pero es marca
registrada y el repo es público (ver la regla de `CREDITOS.md`).

- **De pie** lo sostienen muelles en las articulaciones y en la cadera: un
  golpe flojo lo hace tambalearse como un tentetieso.
- **Golpearlo:** cada mando lleva un trigger (`Golpe_Mano`) que, si entra
  en el Chancho a más de 1,2 m/s, le transmite el golpe con vibración.
  Un golpe de más de 7 N·s lo tumba.
- **Lanzarlo:** cabeza, torso y brazos son agarrables. Agarrarlo lo deja
  sin fuerzas, y al soltarlo sale volando con la velocidad de la mano.
- **Se levanta solo** a los 8 s en su sitio, para poder repetirlo.

Es un ragdoll que nunca es cinemático: `XRGrabInteractable` restaura el
estado cinemático al soltar, y un cuerpo cinemático no sale lanzado.

`Blackout → Montar parroquiano` lo construye y pone las manos.

### `Borrachera`: beber se nota

Beber del vaso o de la botella a morro (llevándolos a la boca e
inclinándolos) sube un nivel que se pasa solo en unos dos minutos. Un vaso
suma 0,35; la botella cuenta como tres vasos.

Se ve con un segundo volumen de post-procesado (`Blackout_Borrachera`)
cuyo peso es el nivel: lo lejano se emborrona, la viñeta cierra la vista,
el color se satura y su tono oscila despacio. **La cámara nunca se mueve**:
en VR balancear la vista sin mover la cabeza marea. Por lo mismo la
aberración cromática va con tope bajo, porque la documentación de URP la
desaconseja en VR. `intensidadMaxima` en el inspector baja el techo si a
alguien le sienta mal.

---

### `PruebaManosSimuladas`: probar sin visor

También solo de editor (`Scripts/Demo/`). Conecta un visor y dos mandos
simulados del Input System, los mismos que usa el simulador de XRI, y hace
con ellos los gestos reales: agarra cada dardo y lo lanza moviendo la mano,
tira de un grifo, golpea al Chancho, toca la caja y dos platillos y rasga
la guitarra pisando trastes. Pasa por los interactores del rig, el cálculo
de velocidad al soltar y la física, igual que con las gafas. Escribe el
resultado en `Temp/Demo/prueba.txt`. Lo único que no prueba es la mano de
una persona.

### `RecorridoDemo`: video sin visor

Herramienta solo de editor (`Scripts/Demo/`, dentro de `#if UNITY_EDITOR`,
no entra en el APK). En Play recorre el bar haciendo cada gesto por los
caminos reales (eventos de XRI, sockets, física) y graba cada cuadro a
30 fps fijos con `Time.captureFramerate` en `Temp/Demo/cuadros`, más los
rótulos en `Temp/Demo/rotulos.txt` y los momentos de cada voz y sonido
en `Temp/Demo/eventos.txt`, para mezclarlos en el montaje. Para lanzarla: entrar en Play y añadir
el componente a cualquier objeto. Sirve para enseñar el proyecto a quien
no tiene las gafas; **no sustituye al video del visor** que pide el curso.

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

**El cartel CLOSED/OPEN está fuera, no dentro.** Cuelga de un brazo en la
fachada, a la derecha de la puerta, mirando a la calle: un cartel de abierto
existe para quien pasa por delante, no para quien ya está dentro. Eso hace
que el último gesto de la experiencia sea **salir** a girarlo, que es lo que
hace cualquier dueño al abrir. Solo responde con el bar ya encendido.

**Los puntos nunca restan y cada cosa cuenta una vez.** Si se añade un
hito nuevo, que pase un `origen` a `BarScore.Anotar` para que no se pueda
repetir en bucle, y que tenga tope.

**Los carteles que arrancan colgados no puntúan.** Los siete sockets del
muro los «fijan» al empezar; `PosterSlot` ignora esa primera fijación, que
además hacía sonar las siete chinchetas a la vez. Solo cuenta volver a
colgar uno.

**Post-procesado.** `Blackout_PostFX.asset` (bloom, ACES, vignette)
es lo que hace que la luz roja tenga profundidad. Sin él la escena se ve
plana. Hasta noviembre **no se veía**, por tres fallos encadenados: los
efectos del perfil no estaban guardados dentro del asset, la cámara tenía
el post-procesado apagado y el renderer de Android (`Android Preset`, el
de las Quest) no tenía `PostProcessData`, que es su interruptor general.
El template VR trae esas dos últimas cosas apagadas por rendimiento.
`Blackout → Montar post-procesado y borrachera` deja las tres bien. Cuesta
rendimiento en Quest: medirlo.

---

## Licencias

El proyecto usa material de terceros CC0 y **CC BY**. Los CC BY (modelos de
Poly Pizza) **exigen atribución**: si esto se presenta o entrega,
[`Assets/_Blackout/CREDITOS.md`](Assets/_Blackout/CREDITOS.md) tiene que
acompañarlo.

---

## Estado

Las nueve estaciones están montadas, vestidas y sonorizadas. El sistema
de puntos está escrito y enganchado a todas. El Chancho, la borrachera y
lo añadido para parecerse al one page están probados en Play desde el
editor.

⚠️ **Nada se ha probado con el visor puesto.** Todo el desarrollo se hizo
desde el editor. Ver [`TODO.md`](TODO.md) para lo que falta y para los valores
que hay que calibrar con el visor puesto.
