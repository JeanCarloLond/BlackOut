# Blackout Bar — Créditos y licencias de assets

Todo el material de terceros usado en este proyecto es de dominio público
(CC0) o de libre uso comercial. Ninguno exige atribución, pero se acredita
igualmente.

## Sonido

### Efectos (`Assets/_Blackout/Audio/SFX/SFX_*.wav`)
- **Kenney — Interface Sounds v1.0** · <https://kenney.nl>
- Licencia: **CC0 1.0 Universal** (dominio público)
- <https://creativecommons.org/publicdomain/zero/1.0/>
- Obtenidos del mirror <https://github.com/Calinou/kenney-interface-sounds>
- Renombrados según su uso en la experiencia. Son sonidos de interfaz
  reutilizados como foley; conviene sustituirlos por grabaciones propias
  donde se note (servir, trago, aguja).

### Ambientes sintetizados (`Assets/_Blackout/Audio/SFX/AMB_*.wav`)
- Generados con **ffmpeg** (`sine`, `anoisesrc`) para este proyecto.
- Sin licencia de terceros: son señal sintética.
- `AMB_Zumbido_Tubo` — 120 Hz + 60 Hz, zumbido del fluorescente
- `AMB_Vinilo_Crujido` — ruido rosa filtrado, surco del vinilo
- `AMB_TV_Estatica` — ruido blanco filtrado

### Música de los vinilos (`Assets/_Blackout/Audio/Tema_*.mp3`)
- **FreePD** (Kevin MacLeod) · <https://freepd.com> · vía
  <https://archive.org/details/freepd>
- Licencia: **dominio público / CC0**
- `Tema_01_Dark_Rock` · `Tema_02_Metaltania` · `Tema_03_Grunge_Meditations`

## Texturas

`Assets/_Blackout/Textures/PBR/`
- **ambientCG** · <https://ambientcg.com>
- Licencia: **CC0 1.0 Universal**
- `Ladrillo_*` ← Bricks097 · `Madera_*` ← Planks037A · `Cemento_*` ← Concrete034

## Vídeo

`Assets/_Blackout/Video/Canal_*.mp4`
- Generados con **ffmpeg** (`smptebars`, `testsrc2`, `noise`) para este
  proyecto. Cartas de ajuste y estática, sin licencia de terceros.
- **Placeholder**: sustituir por metraje real de conciertos cuando lo haya.

## Modelos del escenario (estación 05)

Obtenidos de **[Poly Pizza](https://poly.pizza)**. Todos venían con escalas
disparatadas (el ampli medía 410 m) y se reescalaron a tamaño real por código.

| Archivo | Modelo original | Autor | Licencia |
|---|---|---|---|
| `Bateria.glb` | Full Drum Kit | Batoski | **CC BY 3.0** |
| `Guitarra_Electrica.glb` | Electric guitar | jeremy | **CC BY 3.0** |
| `Ampli.glb` | Guitar Amp | Poly by Google | **CC BY 3.0** |
| `Baqueta.glb` | Drumstick | jeremy | **CC BY 3.0** |
| `Monitor_Suelo.glb` | Floor Monitor | Peter Simcoe | **CC BY 3.0** |
| `Guitarra_Acustica.glb` | Guitar | Quaternius | CC0 |

> **La atribución de los CC BY es obligatoria.** Si el proyecto se muestra
> o entrega, estos créditos deben acompañarlo.

> **Pendiente**: no hay bajo eléctrico decente en Poly Pizza. El objeto
> `Bajo` de la escena reutiliza la malla de la guitarra eléctrica, un 18 %
> más grande y afinada una octava por debajo (`rangoTono` 0.46–0.54).
> Sustituir cuando aparezca un modelo mejor.

## Mobiliario del bar (estación 03)

También de **[Poly Pizza](https://poly.pizza)**, reescalados por código.

| Archivo | Modelo original | Autor | Licencia |
|---|---|---|---|
| `Banqueta.glb` | Bar Stool | Kenney | CC0 |
| `Banqueta_Cuadrada.glb` | Stool Bar Square | Kenney | CC0 |
| `Botella_Vino.glb` | Bottle of wine | Poly by Google | **CC BY 3.0** |
| `Barril_Cerveza.glb` | Keg of beer | Poly by Google | **CC BY 3.0** |
| `Cerveza.glb` | Beer | Poly by Google | **CC BY 3.0** |

## Carteles del muro (estación 06)

Carteles de concierto reales obtenidos de **Wikimedia Commons**, todos en
dominio público o CC0. Redimensionados a 1024 px de alto con ffmpeg.

| Archivo | Original | Licencia |
|---|---|---|
| `Poster_PD_Mops_Lyon.jpg` | *Affiche de concert Rock n' roll Mops à Lyon* (Lyon capitale du rock, 1978-1983) | CC0 |
| `Poster_PD_BigBlack_1985.jpg` | *Big Black, Urge Overkill & Squirrel Bait at the Jockey Club*, flyer del 26-05-1985 | Dominio público |
| `Poster_PD_Helix_1967.jpg` | *Helix*, v.1 n.9, 16-08-1967 (vía Digital Public Library of America) | Dominio público |

> Se optó deliberadamente por carteles de dominio público en lugar de
> descargar logos de bandas actuales: son marcas registradas de sus
> titulares y este repositorio es público.

## Material propio del autor

- `Textures/Posters/Poster_Megadeth|Venom|Nargaroth|Death.jpg` — logos de
  bandas aportados por el autor.
- `Models/Guitar/` y `Models/Barril/` — modelos aportados por el autor.

> Los logos de bandas son marcas de sus respectivos titulares y se usan aquí
> con fines académicos y no comerciales.
