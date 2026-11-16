#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Blackout
{
    /// <summary>
    /// Prueba sin visor, solo editor. Conecta un visor y dos mandos simulados
    /// del Input System (los mismos dispositivos que usa el simulador de XRI)
    /// y hace con ellos los gestos que en la demo se disparan por eventos:
    /// agarrar un dardo con el boton de agarre, hacer el movimiento de lanzar
    /// y soltar; tirar de la maneta de un grifo; dar un punetazo al Chancho.
    ///
    /// Asi pasa por todo lo que pasa con las gafas: TrackedPoseDriver, los
    /// interactores del rig, el calculo de velocidad al soltar de
    /// XRGrabInteractable y la fisica. Lo que no puede probar es la mano de
    /// una persona: la fuerza y la punteria reales.
    ///
    /// Uso: en Play, anadir el componente a cualquier objeto. Escribe el
    /// resultado en Temp/Demo/prueba.txt y en la consola con [Prueba].
    /// </summary>
    // Antes que los interactores: el estado del mando tiene que estar puesto
    // cuando XRI lo lee en este mismo cuadro, como hace el simulador de XRI.
    [DefaultExecutionOrder(-30000)]
    public class PruebaManosSimuladas : MonoBehaviour
    {
        [SerializeField] private int fps = 60;
        [Tooltip("Velocidad de la mano en el lanzamiento, en m/s. Un lanzamiento de dardos real ronda 4-6.")]
        [SerializeField] private float velocidadLanzamiento = 4.5f;

        public bool Terminado { get; private set; }

        private XRSimulatedHMD visor;
        private XRSimulatedController manoDer;
        private XRSimulatedController manoIzq;
        private XRSimulatedControllerState estadoDer;
        private XRSimulatedControllerState estadoIzq;
        private XRSimulatedHMDState estadoVisor;
        private Transform offset;
        private readonly List<string> informe = new List<string>();
        private readonly Dictionary<XRBaseInputInteractor, XRBaseInputInteractor.InputTriggerType> modoOriginal =
            new Dictionary<XRBaseInputInteractor, XRBaseInputInteractor.InputTriggerType>();

        private void Start()
        {
            var rig = GameObject.Find("XR Origin Hands (XR Rig)");
            offset = rig.transform.Find("Camera Offset");

            visor = InputSystem.AddDevice<XRSimulatedHMD>("Visor_Simulado");
            manoDer = InputSystem.AddDevice<XRSimulatedController>("Mando_Simulado_Der");
            manoIzq = InputSystem.AddDevice<XRSimulatedController>("Mando_Simulado_Izq");
            InputSystem.SetDeviceUsage(manoDer, UnityEngine.InputSystem.CommonUsages.RightHand);
            InputSystem.SetDeviceUsage(manoIzq, UnityEngine.InputSystem.CommonUsages.LeftHand);

            estadoDer = Rastreado(new XRSimulatedControllerState());
            estadoIzq = Rastreado(new XRSimulatedControllerState());
            estadoVisor = new XRSimulatedHMDState { isTracked = true, trackingState = 3 };
            estadoVisor.centerEyeRotation = Quaternion.identity;
            estadoVisor.deviceRotation = Quaternion.identity;

            // Con la entrada inyectada, el interactor no llega a ver el cuadro
            // exacto en que se aprieta el agarre (lo que pide el modo
            // StateChange del rig), aunque el boton si llega. Mientras dura la
            // prueba se usa el modo State, que agarra mientras se mantiene
            // apretado; con mandos reales el rig se queda como esta.
            foreach (var i in FindObjectsByType<XRBaseInputInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (i is UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor) continue;
                modoOriginal[i] = i.selectActionTrigger;
                i.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
            }

            Time.captureFramerate = fps;
            StartCoroutine(Guion());
        }

        private void OnDestroy()
        {
            foreach (var par in modoOriginal) if (par.Key != null) par.Key.selectActionTrigger = par.Value;
            Time.captureFramerate = 0;
            foreach (var d in new InputDevice[] { visor, manoDer, manoIzq })
                if (d != null && d.added) InputSystem.RemoveDevice(d);
        }

        private static XRSimulatedControllerState Rastreado(XRSimulatedControllerState s)
        {
            s.isTracked = true;
            s.trackingState = 3; // posicion y rotacion
            s.deviceRotation = Quaternion.identity;
            return s;
        }

        private void Update()
        {
            // Las dos cosas. InputState.Change pone el estado ya, antes de que
            // XRI lo lea en este cuadro: un evento encolado se procesaria en la
            // actualizacion previa al render (los mandos XR la usan para la
            // pose) y XRI no veria "pulsado en este cuadro". El evento encolado
            // hace falta igual: XRInputModalityManager decide si hay mandos
            // mirando los eventos, y sin ellos no activa el rig.
            InputState.Change(visor, estadoVisor);
            InputState.Change(manoDer, estadoDer);
            InputState.Change(manoIzq, estadoIzq);
            InputSystem.QueueStateEvent(visor, estadoVisor);
            InputSystem.QueueStateEvent(manoDer, estadoDer);
            InputSystem.QueueStateEvent(manoIzq, estadoIzq);
        }

        // ---------- Guion ----------

        private IEnumerator Guion()
        {
            yield return Esperar(0.5f);
            Linea("Mandos activos: " + Activo("Right Controller") + " / " + Activo("Left Controller") +
                  " (agarre en modo State durante la prueba)");

            // De pie en la linea de tiro, mirando a la diana.
            ColocarRig(new Vector3(2.45f, 0f, -2.5f), 90f);
            Mirar(new Vector3(4.93f, 1.65f, -2.5f));
            ManoIzq(new Vector3(2.45f, 1.0f, -2.75f), Quaternion.identity);
            yield return Esperar(0.5f);

            var diana = GameObject.Find("Diana").transform.position;
            float[] zs = { -2.5f, -2.36f, -2.64f };
            for (int i = 1; i <= 3; i++)
                yield return LanzarDardo(GameObject.Find("Dardo_0" + i), new Vector3(diana.x, diana.y, zs[i - 1]));

            yield return ProbarGrifo();
            yield return ProbarPunetazo();
            yield return ProbarBateria();
            yield return ProbarGuitarra();

            Directory.CreateDirectory("Temp/Demo");
            File.WriteAllLines("Temp/Demo/prueba.txt", informe.ToArray());
            Terminado = true;
            Debug.Log("[Prueba] Terminada.\n" + string.Join("\n", informe.ToArray()));
        }

        private IEnumerator LanzarDardo(GameObject dardo, Vector3 objetivo)
        {
            var agarre = dardo.GetComponent<XRGrabInteractable>();
            var cuerpo = dardo.GetComponent<Rigidbody>();

            // La mano baja al dardo de la mesa y aprieta el agarre.
            yield return MoverMano(dardo.transform.position + Vector3.up * 0.03f, Quaternion.LookRotation(Vector3.right, Vector3.up), 0.5f);
            Agarre(true);
            yield return Esperar(0.3f);
            bool agarrado = agarre.isSelected;

            // Lo sube a la altura del ojo, echa el brazo atras y lanza.
            Vector3 atras = new Vector3(2.75f, 1.62f, objetivo.z);
            Quaternion apunta = Quaternion.LookRotation((objetivo + Vector3.up * 0.18f - atras).normalized, Vector3.up);
            yield return MoverMano(atras, apunta, 0.6f);
            // Sin pausa: XRI promedia la velocidad de los ultimos 0.25 s, y
            // una persona lanza en un solo movimiento. Se acelera hacia delante
            // y se suelta en plena carrera.
            Vector3 suelta = atras + apunta * Vector3.forward * 0.7f;
            yield return MoverMano(suelta, apunta, 0.7f / velocidadLanzamiento, false);
            Agarre(false);
            yield return MoverMano(suelta + apunta * Vector3.forward * 0.15f, apunta, 0.15f / velocidadLanzamiento, false);
            yield return Esperar(0.05f);
            float vSalida = cuerpo.linearVelocity.magnitude;
            yield return Esperar(1.2f);

            bool clavado = (bool)typeof(Dart).GetField("clavado", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dardo.GetComponent<Dart>());
            float angulo = Vector3.Angle(dardo.transform.up, Vector3.right);
            float alCentro = Vector2.Distance(new Vector2(dardo.transform.position.y, dardo.transform.position.z),
                                              new Vector2(objetivo.y, objetivo.z));
            Linea(dardo.name + ": agarrado=" + agarrado + " salida=" + vSalida.ToString("F1") + " m/s clavado=" + clavado +
                  " angulo con la diana=" + angulo.ToString("F0") + " grados, a " + (alCentro * 100f).ToString("F0") + " cm del objetivo");
        }

        private IEnumerator ProbarGrifo()
        {
            var maneta = GameObject.Find("Maneta_02");
            var salida = GameObject.Find("Salida_02").transform;
            var vaso = GameObject.Find("Vaso_02");
            var rbVaso = vaso.GetComponent<Rigidbody>();
            rbVaso.position = new Vector3(salida.position.x, 1.105f, salida.position.z);
            vaso.transform.position = rbVaso.position;
            rbVaso.linearVelocity = Vector3.zero;
            var glass = vaso.GetComponent<Glass>();
            float antes = glass.Nivel;

            ColocarRig(new Vector3(-3.45f, 0f, -0.9f), -90f);
            Mirar(salida.position);
            yield return Esperar(0.3f);
            var ia = maneta.GetComponent<XRSimpleInteractable>();
            yield return MoverMano(maneta.transform.position, Quaternion.LookRotation(Vector3.left, Vector3.up), 0.5f);
            Agarre(true);
            yield return Esperar(2.5f);
            bool seleccionada = ia.isSelected;
            Agarre(false);
            yield return MoverMano(maneta.transform.position + new Vector3(0.3f, -0.1f, 0f), Quaternion.LookRotation(Vector3.left, Vector3.up), 0.3f);
            yield return Esperar(0.4f);
            Linea("Grifo: maneta agarrada=" + seleccionada + " vaso " + (antes * 100f).ToString("F0") + "% -> " + (glass.Nivel * 100f).ToString("F0") + "%");
        }

        private IEnumerator ProbarPunetazo()
        {
            var npc = FindAnyObjectByType<Parroquiano>();
            var torso = GameObject.Find("Torso").transform;
            ColocarRig(new Vector3(-1.5f, 0f, -0.4f), -45f);
            Mirar(torso.position);
            Vector3 hacia = torso.position - new Vector3(-1.5f, torso.position.y, -0.4f);
            hacia.y = 0f; hacia.Normalize();
            Quaternion rot = Quaternion.LookRotation(hacia, Vector3.up);
            Vector3 desde = torso.position - hacia * 0.55f + Vector3.up * 0.1f;
            yield return MoverMano(desde, rot, 0.5f);
            yield return Esperar(0.3f);
            // Punetazo a 5 m/s hasta el centro del torso.
            yield return MoverMano(torso.position + Vector3.up * 0.1f, rot, 0.55f / 5f, false);
            yield return Esperar(0.4f);
            Linea("Chancho: punetazo a 5 m/s, noqueado=" + npc.Noqueado);
            yield return MoverMano(desde, rot, 0.3f);
        }

        private IEnumerator ProbarBateria()
        {
            ColocarRig(new Vector3(0.5f, 0f, 2.55f), 0f);
            Mirar(new Vector3(0.6f, 0.9f, 3.7f));
            Quaternion abajo = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            foreach (var nombre in new[] { "Bigsnare", "Crash", "HHT" })
            {
                var pieza = GameObject.Find(nombre).GetComponent<StageInstrument>();
                int veces = 0;
                System.Action<StageInstrument> contar = i => veces++;
                pieza.Sonado += contar;
                var centro = pieza.GetComponent<Collider>().bounds.center;
                yield return MoverMano(centro + Vector3.up * 0.35f, abajo, 0.5f);
                yield return Esperar(0.2f);
                // Golpe a 3 m/s, y luego otro flojo a 0.3 m/s que no deberia sonar.
                yield return MoverMano(centro + Vector3.down * 0.02f, abajo, 0.37f / 3f, false);
                yield return MoverMano(centro + Vector3.up * 0.35f, abajo, 0.4f);
                int fuerte = veces;
                yield return MoverMano(centro + Vector3.down * 0.02f, abajo, 0.37f / 0.3f, false);
                yield return MoverMano(centro + Vector3.up * 0.35f, abajo, 0.3f);
                pieza.Sonado -= contar;
                Linea("Bateria " + nombre + ": golpe a 3 m/s sono=" + (fuerte > 0) + ", roce a 0.3 m/s sono=" + (veces > fuerte));
            }
        }

        private IEnumerator ProbarGuitarra()
        {
            var guitarra = GameObject.Find("Guitarra_Electrica");
            var si = guitarra.GetComponent<StageInstrument>();
            var agarre = guitarra.GetComponent<XRGrabInteractable>();
            ColocarRig(new Vector3(-1.35f, 0f, 2.45f), 0f);
            Mirar(guitarra.transform.position);
            var cuerpo = guitarra.transform.Find("Mastil_Fin").position + Vector3.down * 0.2f;
            yield return MoverMano(cuerpo, Quaternion.LookRotation(Vector3.forward, Vector3.up), 0.5f);
            Agarre(true);
            yield return Esperar(0.4f);
            bool agarrada = agarre.isSelected;

            int veces = 0;
            System.Action<StageInstrument> contar = i => veces++;
            si.Sonado += contar;
            var traste = typeof(StageInstrument).GetMethod("Traste", BindingFlags.NonPublic | BindingFlags.Instance);
            var mano = agarre.interactorsSelecting.Count > 0 ? agarre.interactorsSelecting[0] : null;
            string resultado = "";
            // Sin la otra mano en el mastil, y luego pisando tres trastes.
            foreach (float k in new[] { -1f, 0.1f, 0.6f, 0.9f })
            {
                var ini = guitarra.transform.Find("Mastil_Inicio").position;
                var fin = guitarra.transform.Find("Mastil_Fin").position;
                ManoIzq(k < 0f ? ini + Vector3.left * 0.6f : Vector3.Lerp(ini, fin, k), Quaternion.identity);
                yield return Esperar(0.2f);
                // El gatillo tiene el mismo problema que el agarre con la entrada
                // inyectada: se dispara el evento de activar del propio XRI con
                // el interactor real que sostiene la guitarra.
                agarre.activated.Invoke(new UnityEngine.XR.Interaction.Toolkit.ActivateEventArgs { interactorObject = (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRActivateInteractor)mano, interactableObject = agarre });
                yield return Esperar(0.35f);
                int idx = mano != null ? (int)traste.Invoke(si, new object[] { mano }) : -1;
                resultado += (k < 0f ? "al aire" : "traste " + k.ToString("F1")) + "->acorde " + idx + "  ";
            }
            si.Sonado -= contar;
            Agarre(false);
            Linea("Guitarra: agarrada=" + agarrada + " rasgueos que sonaron=" + veces + "/4  " + resultado);
        }

        // ---------- Ayudas ----------

        private void ColocarRig(Vector3 pos, float giro)
        {
            var rig = offset.parent;
            rig.SetPositionAndRotation(pos, Quaternion.Euler(0f, giro, 0f));
        }

        private void Mirar(Vector3 punto)
        {
            Vector3 ojo = offset.TransformPoint(new Vector3(0f, 0.3f, 0f));
            Quaternion rot = Quaternion.Inverse(offset.rotation) * Quaternion.LookRotation(punto - ojo, Vector3.up);
            estadoVisor.centerEyePosition = new Vector3(0f, 0.3f, 0f);
            estadoVisor.devicePosition = estadoVisor.centerEyePosition;
            estadoVisor.centerEyeRotation = rot;
            estadoVisor.deviceRotation = rot;
        }

        private void ManoIzq(Vector3 mundo, Quaternion rot)
        {
            estadoIzq.devicePosition = offset.InverseTransformPoint(mundo);
            estadoIzq.deviceRotation = Quaternion.Inverse(offset.rotation) * rot;
        }

        private IEnumerator MoverMano(Vector3 mundo, Quaternion rot, float seg, bool suave = true)
        {
            Vector3 p0 = estadoDer.devicePosition;
            Quaternion r0 = estadoDer.deviceRotation;
            Vector3 p1 = offset.InverseTransformPoint(mundo);
            Quaternion r1 = Quaternion.Inverse(offset.rotation) * rot;
            float t = 0f;
            while (t < seg)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / seg);
                if (suave) k = Mathf.SmoothStep(0f, 1f, k);
                estadoDer.devicePosition = Vector3.Lerp(p0, p1, k);
                estadoDer.deviceRotation = Quaternion.Slerp(r0, r1, k);
                yield return null;
            }
        }

        private void Agarre(bool apretado)
        {
            estadoDer.grip = apretado ? 1f : 0f;
            estadoDer = estadoDer.WithButton(ControllerButton.GripButton, apretado);
        }

        private IEnumerator Esperar(float seg)
        {
            float t = 0f;
            while (t < seg) { t += Time.deltaTime; yield return null; }
        }

        private static string Activo(string nombre)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == nombre) return t.gameObject.activeInHierarchy ? "si" : "no";
            return "?";
        }

        private void Linea(string texto)
        {
            informe.Add(texto);
            Debug.Log("[Prueba] " + texto);
        }
    }
}
#endif
