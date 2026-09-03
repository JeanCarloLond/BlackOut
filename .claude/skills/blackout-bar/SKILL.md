---
name: blackout-bar
description: Convenciones y trampas conocidas del proyecto Blackout Bar (experiencia VR en Unity 6.3 con URP y XR Interaction Toolkit). Úsala siempre que trabajes en este repositorio - antes de tocar la escena, escribir scripts, importar modelos o hacer commits.
---

# Blackout Bar — cómo trabajar en este proyecto

Experiencia VR académica: un bar de rock que el usuario enciende con las
manos antes de abrir. Lee `README.md` para el diseño y `TODO.md` para lo
que falta.

Este documento recoge **lo que ya nos costó tiempo descubrir**. Léelo antes
de empezar.

---

## Regla que no se negocia

> **Los commits NO llevan línea `Co-Authored-By` ni ninguna atribución a
> Claude.** Es una entrega académica y el profesor la rechazaría.
> Mensaje de commit normal y termina ahí.

---

## Conectarse a Unity (MCP for Unity)

El proyecto se edita a través del puente MCP, no a mano.

- El servidor está registrado a scope `user` con el nombre **`unity`**, no
  `UnityMCP`. **Es a propósito**: el paquete borra en cada arranque de
  Unity toda entrada llamada `UnityMCP` en cualquier scope
  (`StartupConfigRewrite`), y además tiene `--scope local` hardcodeado en
  `McpClientConfiguratorBase.cs`. Con otro nombre sobrevive.
- **Nunca uses el botón "Configure All Detected Clients"** de la ventana de
  Unity: reescribe la config y rompe el registro global.
- **El puente se cae en cada domain reload** (instalar un paquete, a veces
  recompilar). Cuando pase, `mcpforunity://instances` devuelve 0 y hay que
  pedirle al usuario: `Window → MCP for Unity → Start`. No hay forma de
  levantarlo desde fuera.
- Si aparece `Unity is reloading; please retry`, espera y reintenta con
  `refresh_unity` (`wait_for_ready: true`), que sabe recuperarse.

---

## `execute_code`: el compilador es viejo

Para construir escena es mucho más fiable que ir componente a componente
con `manage_components`. Pero compila con **CodeDom, C# 6**, y muerde:

### Ambigüedades que rompen la compilación

```csharp
// MAL: 'Object' es ambiguo entre System.Object y UnityEngine.Object
Object.FindObjectsByType<GameObject>(...)
Object.DestroyImmediate(x)
System.Action<MonoBehaviour, string, Object> f = ...

// BIEN: cualifica siempre
UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
UnityEngine.Object.DestroyImmediate(x)
System.Action<MonoBehaviour, string, UnityEngine.Object> f = ...
```

Lo mismo con **`Random`** (`System.Random` vs `UnityEngine.Random`): si el
snippet no tiene `using System;` va bien, pero en cuanto lo tenga, cualifica
o usa arrays de valores fijos.

### Otras trampas

- **No declares una variable con el mismo nombre que un parámetro de lambda
  anterior** en el mismo ámbito: `System.Func<string,X> f = (n) => ...`
  seguido de `int n = 0;` no compila.
- Si una lambda falla al declararse, **todas** sus llamadas dan
  `cannot be used before it is declared`. Arregla la declaración, no las
  llamadas.
- **`Renderer.bounds` puede estar obsoleto** justo después de cambiar el
  transform en el mismo script. Los valores suelen ser correctos aunque la
  captura de pantalla siguiente muestre la escena sin actualizar: verifica
  consultando la posición, no por la imagen.

### Campos privados `[SerializeField]`

Se escriben con `SerializedObject` + `ApplyModifiedPropertiesWithoutUndo()`.
Para arrays: `FindProperty("x").arraySize = n` y luego
`GetArrayElementAtIndex(i).objectReferenceValue = ...`.

Para enums usa `enumValueIndex`, que es el **índice en el orden de
declaración**, no el valor del enum. (En URP, `LightRenderingMode` se
declara `Disabled, PerPixel, PerVertex`, así que `PerPixel` es el índice 1.)

---

## Trampas de Unity y URP en este proyecto

### Las luces adicionales estaban desactivadas

El template VR trae `additionalLightsRenderingMode: Disabled` en
`Performance URP Config` para rendimiento en Quest. Con eso **ningún point
light se dibuja**. Ya está corregido a `PerPixel` con límite 4 y atlas de
sombras 2048. **No lo devuelvas a `Disabled`**: toda la experiencia se apoya
en la luz roja.

### Al recorrer assets URP, salta los de paquetes

`AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")` también
devuelve uno dentro de `Packages/com.unity.xr.androidxr-openxr/`. Escribirlo
provoca el aviso *"assets located in immutable packages were unexpectedly
altered"*. Filtra siempre con `if (path.StartsWith("Packages/")) continue;`.

### Las UV de los cubos: la cara −Z sale invertida

Un `PrimitiveType.Cube` texturizado muestra el texto **al revés en su cara
−Z**. Si el objeto debe mirar hacia −Z, gíralo 180° en Y para que sea la
cara +Z la que se ve. Pasó con el rótulo de la fachada.

### Los cilindros no sirven para caras texturizadas

Las UV de las tapas de un cilindro no mapean un círculo. Para una diana o
similar, usa un cubo fino con la textura y el cilindro solo como cuerpo.

### ProBuilder reparte UV por metro

El tiling del material se **multiplica** por eso. Valores como `(4, 1.5)`
dan cuatro repeticiones por metro y la textura se lee como ruido. En este
proyecto los materiales de muro y suelo van en torno a **0.4–0.7**.

### El bar arranca a oscuras: es el diseño

Los scripts apagan las luces en `Awake`. Si enciendes luces desde
`execute_code` para hacer una captura, **acuérdate de volver a apagarlas y
guardar**, o dejarás la escena en un estado que no es el inicial.

Las superficies emisivas (`Panel_Retroiluminado`, `Cara_Open`) se controlan
con `MaterialPropertyBlock` desde los scripts, así que en el editor están
negras. Es correcto.

---

## Importar modelos de terceros

### Las escalas vienen absurdas

Nunca confíes en la escala de un modelo descargado. Vistos en este
proyecto: un ampli de **410 m**, una guitarra de **21,6 m**, otra de 6,4 m.

**Mide los bounds y reescala:**

```csharp
var r = go.GetComponentsInChildren<Renderer>();
var b = r[0].bounds;
for (int i = 1; i < r.Length; i++) b.Encapsulate(r[i].bounds);
float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
go.transform.localScale *= (tamObjetivo / mayor);
```

⚠️ **Escalar por la dimensión mayor solo vale para objetos alargados.** Para
muebles escala por **altura** (`b.size.y`) — y comprueba el resultado: una
mesa de café de 30 cm llevada a 1,06 m de alto se fue a **3,4 m de ancho**,
y hubo que descartarla y construir la mesa con primitivas.

Los pivotes suelen estar descentrados. Para apoyar algo en el suelo:

```csharp
Vector3 dc = b.center - go.transform.position;   // desfase del centro
float db = b.min.y - go.transform.position.y;    // desfase de la base
go.transform.position = new Vector3(x - dc.x, alturaSuelo - db, z - dc.z);
```

### De dónde bajar assets (todo libre)

| Fuente | Qué | Licencia |
|---|---|---|
| [Poly Pizza](https://poly.pizza) | modelos low poly | CC0 y **CC BY** |
| [Kenney](https://kenney.nl) | sonidos y modelos | CC0 |
| [ambientCG](https://ambientcg.com) | texturas PBR | CC0 |
| [FreePD](https://archive.org/details/freepd) | música | dominio público |
| [Wikimedia Commons](https://commons.wikimedia.org) | imágenes | varía, filtra |

**Poly Pizza no tiene descarga directa por ID.** Hay que sacar el UUID del
HTML de la página del modelo:

```bash
curl -sL -A "Mozilla/5.0" "https://poly.pizza/m/<id>" \
  | grep -oE 'https://static\.poly\.pizza/[a-f0-9-]+\.glb' | head -1
```

**Wikimedia limita el ritmo:** más de dos descargas seguidas devuelven una
página HTML de error de ~2 KB en vez de la imagen. Mete `sleep 3` entre
peticiones y **comprueba el tipo de archivo** antes de dar por buena la
descarga.

**Cuidado con `curl` en Git Bash:** un User-Agent con paréntesis o punto y
coma da `curl: (43) bad argument`, y las URL sacadas de ficheros escritos
por Python en Windows llevan `\r` al final y dan
`URL rejected: Malformed input`. Limpia con `tr -d '\r'`.

### Licencias: obligatorio actualizar `CREDITOS.md`

Cada asset de terceros que entre al proyecto se documenta ahí con autor y
licencia. Los **CC BY exigen atribución** y el repo es público. Por eso no
se descargan logos de bandas actuales: son marcas registradas.

---

## Convenciones del proyecto

- **Todo en español**: nombres de GameObjects, campos serializados,
  comentarios y mensajes de commit. `anguloVertido`, no `pourAngle`.
- **Namespace `Blackout`** para todos los scripts, en
  `Assets/_Blackout/Scripts/`.
- **Un MonoBehaviour por archivo**, con el nombre del archivo.
- Las estaciones son objetos raíz `Estacion_0X_Nombre`.
- Los comentarios explican **por qué**, citando el documento de diseño
  cuando aplica. No narran lo que el código ya dice.
- Campos serializados con `[Tooltip]` cuando el nombre no basta.
- Los scripts **toleran referencias nulas**: si falta un `AudioClip`, no
  suena, pero no se rompe. Manténlo así.

### Generar texturas

Varias texturas del proyecto se generan en vez de descargarse:

- **ffmpeg `drawtext`** para texto (letreros, marcador de tiza, rótulo).
  La fuente en Windows se escapa así: `C\\:/Windows/Fonts/impact.ttf`.
- **`Texture2D` por código** para lo geométrico (la diana está dibujada
  píxel a píxel con los 20 sectores en su orden real).
- **ffmpeg `lavfi`** para audio sintético (zumbido, crujido, estática) y
  para vídeo (`smptebars`, `testsrc2`, `noise`).

Unity solo acepta vídeo **H.264**. Con otro códec falla con
`Could not find supported video track`.

---

## Antes de dar algo por terminado

1. `read_console` filtrando errores. Los avisos de
   `OpenXRPackageSettings.asset` son ruido conocido del importador, ignóralos.
2. Devuelve la escena a su estado inicial (luces apagadas, emisión a negro).
3. Guarda con `EditorSceneManager.SaveOpenScenes()`.
4. Commit **sin coautoría**, en español, explicando el porqué de las
   decisiones y no solo el qué.
5. `git push` puede tardar: LFS sube los binarios. Lánzalo en segundo plano.

**Y lo más importante:** este proyecto **nunca se ha ejecutado en Play**.
No afirmes que algo funciona porque compila. Di qué está verificado y qué
no.
