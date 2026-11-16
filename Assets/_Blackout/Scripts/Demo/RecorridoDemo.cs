#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// Herramienta de demo, solo editor: recorre el bar en Play haciendo cada
    /// gesto por los caminos reales (eventos de XRI, sockets, fisica) y graba
    /// cada cuadro a fps fijos con Time.captureFramerate, asi el video sale
    /// fluido aunque el editor vaya lento. Deja los cuadros y los rotulos en
    /// Temp/Demo para montarlos con ffmpeg.
    ///
    /// No sustituye al video del visor que pide el curso: sirve para ensenar
    /// la experiencia a quien no tiene las gafas puestas.
    /// </summary>
    public class RecorridoDemo : MonoBehaviour
    {
        [SerializeField] private string carpeta = "Temp/Demo";
        [SerializeField] private int ancho = 1280;
        [SerializeField] private int alto = 720;
        [SerializeField] private int fps = 30;

        public bool Terminado { get; private set; }

        private Camera cam;
        private RenderTexture rt;
        private Texture2D tex;
        private int cuadro;
        private Vector3 camPos;
        private Quaternion camRot;
        private Transform mano;
        private Transform manoObjetivo;
        private Vector3 manoDesfase;
        private readonly List<string> rotulos = new List<string>();
        private readonly List<string> eventos = new List<string>();
        private float inicioRotulo;
        private string textoRotulo;
        private string rutaCuadros;

        private void Start()
        {
            cam = Camera.main;
            foreach (var mb in cam.GetComponents<MonoBehaviour>())
                if (mb.GetType().Name.Contains("TrackedPoseDriver")) mb.enabled = false;

            rutaCuadros = Path.Combine(carpeta, "cuadros");
            if (Directory.Exists(rutaCuadros)) Directory.Delete(rutaCuadros, true);
            Directory.CreateDirectory(rutaCuadros);

            rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            cam.aspect = (float)ancho / alto;
            camPos = cam.transform.position;
            camRot = cam.transform.rotation;

            CrearMano();
            Time.captureFramerate = fps;
            RenderPipelineManager.endCameraRendering += AlTerminarCamara;
            StartCoroutine(Guion());
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= AlTerminarCamara;
            Time.captureFramerate = 0;
            if (cam != null) { cam.targetTexture = null; cam.ResetAspect(); }
            if (rt != null) rt.Release();
        }

        private void LateUpdate()
        {
            cam.transform.SetPositionAndRotation(camPos, camRot);

            if (mano == null) return;
            bool hay = manoObjetivo != null;
            if (mano.gameObject.activeSelf != hay) mano.gameObject.SetActive(hay);
            if (!hay) return;
            Vector3 p = manoObjetivo.position + manoDesfase;
            Vector3 dir = p - camPos;
            mano.SetPositionAndRotation(p - dir.normalized * 0.07f, Quaternion.LookRotation(dir, Vector3.up));
        }

        private void AlTerminarCamara(ScriptableRenderContext ctx, Camera c)
        {
            if (c != cam || Terminado) return;
            var previo = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0, false);
            RenderTexture.active = previo;
            File.WriteAllBytes(Path.Combine(rutaCuadros, "f" + cuadro.ToString("00000") + ".jpg"), tex.EncodeToJPG(90));
            cuadro++;
        }

        private float Segundo { get { return cuadro / (float)fps; } }

        // ---------- El guion ----------

        private IEnumerator Guion()
        {
            yield return null;
            Ajustes();

            // 01 La calle
            Poner(new Vector3(0.4f, 1.65f, -8.8f), new Vector3(0.3f, 1.8f, -4.1f));
            Rotulo("Llegas de noche. El bar está cerrado y a oscuras.");
            yield return Ir(new Vector3(0.35f, 1.65f, -6.6f), new Vector3(0.2f, 1.6f, -4.1f), 3.5f);
            Rotulo("En la acera, una pizarra explica cómo se juega.");
            yield return Ir(new Vector3(-1.2f, 1.5f, -6.0f), new Vector3(-1.95f, 0.6f, -4.95f), 1.5f);
            yield return Esperar(2.2f);
            yield return Ir(new Vector3(0.35f, 1.65f, -6.6f), new Vector3(0.2f, 1.6f, -4.1f), 1.2f);

            Rotulo("Subes la reja con el gatillo:  +50");
            ManoA(Buscar<EntranceGate>().transform, new Vector3(0.25f, -0.35f, -0.2f));
            Pulsar(Buscar<EntranceGate>().gameObject);
            Evento("Voz/Voz_Entrada.wav");
            yield return Esperar(0.4f);
            ManoA(null);
            yield return Esperar(2.6f);

            // 02 Luces
            Poner(new Vector3(2.0f, 1.5f, -2.7f), new Vector3(2.0f, 1.35f, -3.9f));
            Rotulo("Dentro no hay luz. Solo el tablero de palancas.");
            yield return Esperar(1.8f);
            Rotulo("Cada palanca enciende su zona:  +25");
            foreach (var n in new[] { "Palanca_01", "Palanca_02", "Palanca_03" })
            {
                ManoA(GameObject.Find(n).transform, new Vector3(0f, 0f, 0.05f));
                Pulsar(GameObject.Find(n));
                yield return Esperar(1.1f);
            }
            Rotulo("La cuarta enciende el bar:  +100 BAR ENCENDIDO");
            ManoA(GameObject.Find("Palanca_04").transform, new Vector3(0f, 0f, 0.05f));
            Pulsar(GameObject.Find("Palanca_04"));
            yield return Esperar(0.5f);
            ManoA(null);
            yield return Esperar(2.2f);

            Rotulo("El bar despierta.");
            Poner(new Vector3(2.7f, 1.7f, -3.4f), new Vector3(-2.5f, 1.2f, 1.2f));
            yield return Ir(new Vector3(2.2f, 1.7f, -2.9f), new Vector3(-1.2f, 1.1f, 3.2f), 3.5f);
            Rotulo("El salón del plano: mesas con sillas y taburetes, fuera del carril de los dardos.");
            yield return Ir(new Vector3(1.4f, 1.8f, -2.6f), new Vector3(-1.6f, 0.4f, -0.2f), 3.5f);

            // 03 La barra
            Poner(new Vector3(-3.0f, 1.65f, -0.3f), new Vector3(-4.6f, 1.55f, -0.3f));
            Rotulo("Ordenas el estante: +20 por botella y +60 al completarlo.");
            for (int i = 1; i <= 4; i++)
            {
                var b = GameObject.Find("Botella_0" + i).transform;
                var hueco = GameObject.Find("Hueco_0" + i).transform;
                ManoA(b, new Vector3(0.12f, 0.1f, 0f));
                yield return Llevar(b, hueco.position + Vector3.up * 0.05f, b.rotation, 0.6f);
                Socket(hueco.gameObject, b.gameObject, true);
                yield return Esperar(0.25f);
            }
            ManoA(null);
            yield return Esperar(1.4f);

            Poner(new Vector3(-3.1f, 1.55f, 1.0f), new Vector3(-4.1f, 1.2f, 1.45f));
            Rotulo("Lo que se puede agarrar brilla al apuntarlo. Cada botella es un licor distinto.");
            yield return Servir();

            Poner(new Vector3(-3.35f, 1.5f, -1.45f), new Vector3(-4.25f, 1.22f, -0.95f));
            Rotulo("La cerveza sale del grifo, con espuma. Hay rubia, roja y negra.");
            yield return Cerveza();

            Poner(new Vector3(-3.2f, 1.55f, 1.55f), new Vector3(-4.1f, 1.15f, 2.3f));
            Rotulo("Pones un vinilo y bajas la aguja: +25 por cada disco distinto.");
            var vinilo = GameObject.Find("Vinilo_Megadeth").transform;
            var plato = GameObject.Find("Socket_Vinilo").transform;
            ManoA(vinilo, new Vector3(0.1f, 0.06f, 0f));
            yield return Llevar(vinilo, plato.position + Vector3.up * 0.05f, plato.rotation, 1.0f);
            Socket(plato.gameObject, vinilo.gameObject, true);
            yield return Esperar(0.4f);
            ManoA(GameObject.Find("Palanca_Aguja").transform, new Vector3(0.05f, 0.05f, 0f));
            Pulsar(GameObject.Find("Palanca_Aguja"));
            yield return Esperar(0.6f);
            ManoA(null);
            yield return Esperar(2.4f);

            // 05 El escenario
            Poner(new Vector3(-0.3f, 1.6f, 0.5f), new Vector3(0f, 1.0f, 3.4f));
            Rotulo("La batería se toca a golpes: suena más fuerte cuanto más rápido. +50 y +10 por instrumento.");
            Poner(new Vector3(0.6f, 1.75f, 2.3f), new Vector3(0.7f, 0.9f, 3.7f));
            yield return Bateria();
            Rotulo("La guitarra cambia de acorde según el traste que pisa la otra mano.");
            Poner(new Vector3(-1.0f, 1.5f, 2.2f), new Vector3(-1.35f, 1.0f, 3.25f));
            yield return Guitarra();
            yield return Ir(new Vector3(-0.2f, 1.55f, 1.2f), new Vector3(0f, 1.0f, 3.5f), 1.5f);

            // 07 La tele
            Poner(new Vector3(-2.9f, 1.5f, -1.75f), new Vector3(-3.97f, 1.3f, -1.8f));
            Rotulo("La tele se enciende con el mando: un concierto de 1929, de dominio público.  +30");
            var mando = GameObject.Find("Mando_TV");
            var tele = Buscar<Television>();
            ManoA(mando.transform, new Vector3(0.05f, 0.04f, 0f));
            mando.GetComponent<XRBaseInteractable>().activated.Invoke(new ActivateEventArgs());
            Evento("Video/Canal_01_Concierto_1929.webm", 4.6f);
            yield return Esperar(4.6f);
            Rotulo("Cada canal distinto suma:  +15");
            mando.GetComponent<XRBaseInteractable>().activated.Invoke(new ActivateEventArgs());
            yield return Esperar(0.5f);
            ManoA(null);
            yield return Esperar(2.6f);
            if (tele != null && tele.Encendida) tele.Encender(null);

            // 06 Los posters
            Poner(new Vector3(3.3f, 1.65f, 0.35f), new Vector3(4.94f, 1.68f, 0.35f));
            Rotulo("Al volver a colgar un cartel, el dueño cuenta su historia:  +15");
            var cartel = GameObject.Find("Cartel_Venom").transform;
            var sitio = GameObject.Find("Sitio_Venom");
            var sitioSock = sitio.GetComponent<XRSocketInteractor>();
            Socket(sitio, cartel.gameObject, false);
            sitioSock.socketActive = false;
            ManoA(cartel, new Vector3(-0.03f, -0.2f, 0f));
            yield return Llevar(cartel, cartel.position + new Vector3(-0.35f, -0.05f, 0f), cartel.rotation, 0.8f);
            yield return Llevar(cartel, sitio.transform.position, sitio.transform.rotation, 0.8f);
            sitioSock.socketActive = true;
            Socket(sitio, cartel.gameObject, true);
            Evento("Voz/Anecdota_Venom.wav");
            ManoA(null);
            yield return Ir(new Vector3(3.6f, 1.65f, 0.9f), new Vector3(4.94f, 1.9f, 1.4f), 6.5f);

            // 08 Los dardos
            Poner(new Vector3(3.2f, 1.65f, -2.15f), new Vector3(4.93f, 1.6f, -2.4f));
            Rotulo("Dardos: suman lo que marque la diana, y el 501 de tiza baja.");
            yield return Dardos();
            yield return Esperar(1.2f);

            // El Chancho
            Poner(new Vector3(-1.2f, 1.45f, -0.75f), new Vector3(-2.3f, 0.95f, 0.45f));
            Rotulo("El Chancho, el parroquiano. Un golpe flojo lo tambalea...");
            yield return Esperar(1.0f);
            yield return Punetazo(1.8f);
            yield return Esperar(1.2f);
            Rotulo("...uno fuerte lo tumba.");
            yield return Punetazo(5.5f);
            yield return Esperar(1.8f);
            Rotulo("Agarrarlo y soltarlo lo lanza. Al rato se levanta solo.");
            yield return Lanzar();
            yield return Esperar(4.2f);

            // La pizarra
            Poner(new Vector3(-3.9f, 2.25f, 0f), new Vector3(-4.94f, 2.45f, 0f));
            Rotulo("La pizarra de la noche lleva la cuenta por estación.");
            yield return Esperar(3.2f);

            // Borrachera
            Poner(new Vector3(-3.0f, 1.6f, 1.6f), new Vector3(-4.2f, 1.3f, 1.9f));
            Rotulo("Beber se nota: cada trago del vaso o la botella sube la borrachera.");
            yield return Beber();
            Rotulo("Lo lejano se emborrona y los colores se van. La cámara no se mueve: así no marea.");
            yield return Ir(new Vector3(-2.6f, 1.6f, 1.2f), new Vector3(2.5f, 1.2f, -1.5f), 4.5f);
            Campo(Borrachera.Instance, "recuperacion", 0.25f);

            // 09 Abrir
            Poner(new Vector3(0.9f, 1.6f, -7.0f), new Vector3(1.5f, 1.7f, -4.4f));
            Rotulo("Sales y giras el letrero a OPEN:  +100");
            yield return Esperar(1.0f);
            Evento("Voz/Voz_Cierre.wav", 0f, 1.6f);
            yield return GirarLetrero();
            yield return Esperar(0.6f);
            Rotulo("El total decide cómo sale la noche. Cuanto mejor, más gente entra.");
            yield return Esperar(3.4f);
            yield return Ir(new Vector3(0.4f, 1.7f, -8.8f), new Vector3(0.4f, 2.0f, -4.1f), 3.0f);
            Rotulo("Blackout Bar");
            yield return Esperar(2.0f);

            Rotulo(null);
            File.WriteAllText(Path.Combine(carpeta, "rotulos.txt"), string.Join("\n", rotulos.ToArray()), Encoding.UTF8);
            File.WriteAllText(Path.Combine(carpeta, "eventos.txt"), string.Join("\n", eventos.ToArray()), Encoding.UTF8);
            Terminado = true;
            Debug.Log("[Demo] Terminado: " + cuadro + " cuadros, " + Segundo.ToString("F1") + " s.");
        }

        // ---------- Escenas con mas pasos ----------

        private IEnumerator Servir()
        {
            var vaso = GameObject.Find("Vaso_01").transform;
            // La mas llena: no todas arrancan igual y alguna casi no da para un vaso.
            int mejor = 1; float max = -1f;
            for (int i = 1; i <= 4; i++)
            {
                float c = GameObject.Find("Botella_0" + i).GetComponent<Bottle>().Contenido;
                if (c > max) { max = c; mejor = i; }
            }
            var bot = GameObject.Find("Botella_0" + mejor).transform;
            var hueco = GameObject.Find("Hueco_0" + mejor).GetComponent<XRSocketInteractor>();
            Socket(hueco.gameObject, bot.gameObject, false);
            hueco.socketActive = false;
            var boca = Leer<Transform>(bot.GetComponent<Bottle>(), "boca");
            if (boca == null) boca = bot;

            // Primero se apunta: la botella brilla antes de agarrarla.
            var agarre = bot.GetComponent<XRBaseInteractable>();
            ManoA(bot, new Vector3(0.18f, 0.12f, 0f));
            agarre.hoverEntered.Invoke(new HoverEnterEventArgs());
            yield return Esperar(1.3f);
            agarre.hoverExited.Invoke(new HoverExitEventArgs());
            ManoA(bot, new Vector3(0.1f, 0.1f, 0f));
            yield return Llevar(bot, vaso.position + new Vector3(0.1f, 0.35f, -0.05f), Quaternion.identity, 0.8f);
            // Se inclina buscando que la boca quede sobre el vaso.
            Quaternion inclinada = Quaternion.AngleAxis(-115f, Vector3.right);
            // Desfase de la boca en el espacio de rotacion de la botella (sin escala).
            Vector3 local = Quaternion.Inverse(bot.rotation) * (boca.position - bot.position);
            Vector3 desfase = inclinada * local;
            yield return Llevar(bot, vaso.position + Vector3.up * 0.26f - desfase, inclinada, 0.6f);
            yield return Esperar(2.6f);
            yield return Llevar(bot, vaso.position + new Vector3(0.15f, 0.35f, -0.35f), Quaternion.identity, 0.6f);
            ManoA(null);
            yield return Esperar(0.6f);
        }

        private IEnumerator Cerveza()
        {
            var vaso = GameObject.Find("Vaso_02").transform;
            var salida = GameObject.Find("Salida_01").transform;
            var maneta = GameObject.Find("Maneta_01");
            ManoA(vaso, new Vector3(0.06f, 0f, 0f));
            yield return Llevar(vaso, new Vector3(salida.position.x, 1.105f, salida.position.z), Quaternion.identity, 1.0f);
            ManoA(maneta.transform, new Vector3(0.04f, 0.03f, 0f));
            yield return Esperar(0.3f);
            maneta.GetComponent<Grifo>().Abrir(true);
            Evento("SFX/AMB_Chorro.wav", 3.6f);
            yield return Esperar(3.6f);
            maneta.GetComponent<Grifo>().Abrir(false);
            yield return Esperar(0.6f);
            ManoA(null);
            yield return Esperar(1.2f);
        }

        private IEnumerator Bateria()
        {
            var golpe = Object.FindObjectsByType<GolpeMano>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0].transform;
            golpe.SetParent(null, true);
            golpe.gameObject.SetActive(true);
            ManoA(golpe, Vector3.zero);
            // Un ritmo sencillo de rock: bombo, charles, caja, charles... y un crash al final.
            string[] golpes = { "BigKick", "HHT", "Bigsnare", "HHT", "BigKick", "HHT", "Bigsnare", "BigTOM", "BigFL", "Crash" };
            string[] sonidos = { "Bombo", "Charles", "Caja", "Charles", "Bombo", "Charles", "Caja", "Tom_Agudo", "Tom_Grave", "Crash" };
            for (int i = 0; i < golpes.Length; i++)
            {
                var pieza = GameObject.Find(golpes[i]).GetComponent<Collider>().bounds;
                Vector3 arriba = pieza.center + Vector3.up * 0.3f + Vector3.back * 0.1f;
                golpe.position = arriba;
                yield return Esperar(0.12f);
                Evento("Instrumentos/" + sonidos[i] + ".wav", 0f, 0.1f);
                float t = 0f;
                while (t < 0.1f)
                {
                    t += Time.deltaTime;
                    golpe.position = Vector3.Lerp(arriba, pieza.center, t / 0.1f);
                    yield return null;
                }
                yield return Esperar(0.08f);
            }
            golpe.position = new Vector3(0f, -5f, 0f);
            ManoA(null);
            yield return Esperar(0.8f);
        }

        private IEnumerator Guitarra()
        {
            var guitarra = GameObject.Find("Guitarra_Electrica");
            var si = guitarra.GetComponent<StageInstrument>();
            var sonar = typeof(StageInstrument).GetMethod("SonarClip", BindingFlags.NonPublic | BindingFlags.Instance);
            var acordes = Leer<AudioClip[]>(si, "acordes");
            var ini = guitarra.transform.Find("Mastil_Inicio");
            var fin = guitarra.transform.Find("Mastil_Fin");
            string[] notas = { "Mi", "Mi", "Sol", "La", "Do", "La" };
            int[] indice = { 0, 0, 1, 2, 3, 2 };
            for (int i = 0; i < notas.Length; i++)
            {
                // La mano va al traste que toca, como se veria con las gafas.
                float k = (indice[i] + 0.5f) / 4f;
                ManoA(ini, Vector3.Lerp(Vector3.zero, fin.position - ini.position, k));
                yield return Esperar(0.15f);
                if (acordes != null && indice[i] < acordes.Length) sonar.Invoke(si, new object[] { acordes[indice[i]], 1f, 1f });
                Evento("Instrumentos/Guitarra_" + notas[i] + "5.wav", 0.6f);
                yield return Esperar(0.5f);
            }
            ManoA(null);
            yield return Esperar(0.6f);
        }

        private IEnumerator Dardos()
        {
            var centro = GameObject.Find("Diana").transform.position;
            Vector3[] desvio = { new Vector3(0f, 0.01f, 0.01f), new Vector3(0f, 0.05f, -0.04f), new Vector3(0f, -0.08f, 0.06f) };
            for (int i = 0; i < 3; i++)
            {
                var dardo = GameObject.Find("Dardo_0" + (i + 1));
                var rb = dardo.GetComponent<Rigidbody>();
                Vector3 objetivo = centro + desvio[i];
                dardo.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                dardo.transform.position = objetivo + new Vector3(-1.1f, 0f, 0f);
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.right * 7f;
                yield return Esperar(1.0f);
            }
        }

        private IEnumerator Punetazo(float velocidad)
        {
            var torso = GameObject.Find("Torso").transform;
            var golpe = Object.FindObjectsByType<GolpeMano>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0].transform;
            golpe.SetParent(null, true);
            golpe.gameObject.SetActive(true);
            Vector3 hacia = (camPos - torso.position); hacia.y = 0f; hacia.Normalize();
            Vector3 desde = torso.position + hacia * 0.6f + Vector3.up * 0.15f;
            Vector3 hasta = torso.position + hacia * 0.05f + Vector3.up * 0.15f;
            golpe.position = desde;
            ManoA(golpe, Vector3.zero);
            yield return Esperar(0.25f);
            float d = Vector3.Distance(desde, hasta);
            float t = 0f;
            while (t < d / velocidad)
            {
                t += Time.deltaTime;
                golpe.position = Vector3.Lerp(desde, hasta, t * velocidad / d);
                yield return null;
            }
            yield return Esperar(0.1f);
            golpe.position = desde + Vector3.up * 2f;
            ManoA(null);
        }

        private IEnumerator Lanzar()
        {
            var npc = Buscar<Parroquiano>();
            Campo(npc, "segundosLevantarse", 3.5f);
            var cabeza = GameObject.Find("Cabeza");
            ManoA(cabeza.transform, new Vector3(0f, 0.15f, 0f));
            cabeza.GetComponent<XRGrabInteractable>().selectEntered.Invoke(new SelectEnterEventArgs());
            yield return Esperar(0.3f);
            foreach (var rb in npc.GetComponentsInChildren<Rigidbody>())
                rb.linearVelocity = new Vector3(1.2f, 3.2f, 1.8f);
            ManoA(null);
        }

        private IEnumerator Beber()
        {
            // El vaso es la cerveza del grifo, que pega menos: se exagera para el video.
            Campo(Borrachera.Instance, "porVaso", 2.4f);
            var vaso = GameObject.Find("Vaso_02").transform;
            vaso.GetComponent<Glass>().Llenar(1f);
            var rb = vaso.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            ManoA(vaso, new Vector3(0.06f, 0f, 0f));
            Vector3 boca = camPos + camRot * new Vector3(0f, -0.08f, 0.14f);
            yield return Llevar(vaso, boca, vaso.rotation, 1.0f);
            Vector3 derecha = camRot * Vector3.right;
            yield return Llevar(vaso, boca, Quaternion.AngleAxis(75f, derecha) * vaso.rotation, 0.5f);
            yield return Esperar(2.0f);
            yield return Llevar(vaso, new Vector3(-4.1f, 1.11f, 1.9f), Quaternion.identity, 0.8f);
            ManoA(null);
            yield return Esperar(1.0f);
        }

        private IEnumerator GirarLetrero()
        {
            var letrero = Buscar<OpenSign>().transform;
            var rb = letrero.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Quaternion desde = letrero.rotation;
            ManoA(letrero, new Vector3(0.12f, -0.1f, -0.05f));
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                letrero.rotation = Quaternion.AngleAxis(Mathf.SmoothStep(0f, 170f, t / 2f), Vector3.up) * desde;
                yield return null;
            }
            ManoA(null);
        }

        // ---------- Ayudas ----------

        private void Ajustes()
        {
            var bs = BarScore.Instance;
            if (bs != null) Campo(bs, "retrasoVeredicto", 2.2f);
        }

        private void Poner(Vector3 pos, Vector3 mira)
        {
            camPos = pos;
            camRot = Quaternion.LookRotation(mira - pos, Vector3.up);
        }

        private IEnumerator Ir(Vector3 pos, Vector3 mira, float seg)
        {
            Vector3 p0 = camPos; Quaternion r0 = camRot;
            Quaternion r1 = Quaternion.LookRotation(mira - pos, Vector3.up);
            float t = 0f;
            while (t < seg)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / seg);
                camPos = Vector3.Lerp(p0, pos, k);
                camRot = Quaternion.Slerp(r0, r1, k);
                yield return null;
            }
        }

        private IEnumerator Esperar(float seg)
        {
            float t = 0f;
            while (t < seg) { t += Time.deltaTime; yield return null; }
        }

        private IEnumerator Llevar(Transform obj, Vector3 pos, Quaternion rot, float seg)
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Vector3 p0 = obj.position; Quaternion r0 = obj.rotation;
            float t = 0f;
            while (t < seg)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / seg);
                obj.SetPositionAndRotation(Vector3.Lerp(p0, pos, k), Quaternion.Slerp(r0, rot, k));
                yield return null;
            }
            obj.SetPositionAndRotation(pos, rot);
        }

        private void Rotulo(string texto)
        {
            if (textoRotulo != null)
                rotulos.Add(inicioRotulo.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                            Segundo.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "|" + textoRotulo);
            textoRotulo = texto;
            inicioRotulo = Segundo;
        }

        /// <summary>
        /// Apunta un audio para mezclarlo en el montaje: el recorrido no graba
        /// sonido, asi que las voces y efectos se ponen luego con ffmpeg.
        /// Formato: segundo|archivo|duracion (0 = el archivo entero).
        /// </summary>
        private void Evento(string archivo, float duracion = 0f, float desfase = 0f)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            eventos.Add((Segundo + desfase).ToString("F2", ci) + "|" + archivo + "|" + duracion.ToString("F2", ci));
        }

        private void ManoA(Transform objetivo, Vector3 desfase = default(Vector3))
        {
            manoObjetivo = objetivo;
            manoDesfase = desfase;
        }

        private void CrearMano()
        {
            var vis = GameObject.Find("XR Origin Hands (XR Rig)");
            Transform modelo = null;
            if (vis != null)
                foreach (var t in vis.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Right Controller Visual") { modelo = t; break; }
            if (modelo == null) return;
            var copia = Instantiate(modelo.gameObject);
            foreach (var mb in copia.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            copia.name = "Mano_Demo";
            copia.SetActive(false);
            mano = copia.transform;
        }

        private static void Pulsar(GameObject go)
        {
            var ia = go.GetComponent<XRBaseInteractable>();
            if (ia != null) ia.selectEntered.Invoke(new SelectEnterEventArgs());
        }

        private static void Tocar(GameObject go)
        {
            var ia = go.GetComponent<XRBaseInteractable>();
            if (ia == null) return;
            ia.activated.Invoke(new ActivateEventArgs());
            ia.selectEntered.Invoke(new SelectEnterEventArgs());
        }

        private static void Socket(GameObject socketGo, GameObject objeto, bool entrar)
        {
            var socket = socketGo.GetComponent<XRSocketInteractor>();
            var ia = objeto.GetComponent<XRGrabInteractable>();
            if (socket == null || ia == null) return;
            var mgr = socket.interactionManager;
            if (entrar) mgr.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)ia);
            else if (socket.IsSelecting((IXRSelectInteractable)ia)) mgr.SelectExit((IXRSelectInteractor)socket, (IXRSelectInteractable)ia);
        }

        private static T Buscar<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }

        private static void Campo(object o, string nombre, object valor)
        {
            if (o == null) return;
            var f = o.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) f.SetValue(o, valor);
        }

        private static T Leer<T>(object o, string nombre) where T : class
        {
            var f = o.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Instance);
            return f != null ? f.GetValue(o) as T : null;
        }
    }
}
#endif
