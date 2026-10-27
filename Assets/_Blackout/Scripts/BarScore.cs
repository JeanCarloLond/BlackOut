using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blackout
{
    /// <summary>Cada gesto del ritual de apertura que suma en la pizarra.</summary>
    public enum Hito
    {
        Reja,             // 01 subir la reja
        Palanca,          // 02 cada palanca del tablero
        BarEncendido,     // 02 las cuatro abajo
        BotellaEnEstante, // 03 cada hueco del estante
        EstanteCompleto,  // 03 el estante entero
        Servir,           // 03 llenar un vaso
        Trago,            // 03 vaciarlo bebiendo
        Disco,            // 04 cada vinilo distinto que suena
        Escenario,        // 05 despertar la tarima
        Instrumento,      // 05 cada instrumento distinto
        Cartel,           // 06 colgar de nuevo un cartel
        Tele,             // 07 encender la tele
        Canal,            // 07 cada canal distinto
        Dardo,            // 08 lo que marque la diana
        Partida501,       // 08 cerrar el 501
        Abrir             // 09 girar el letrero
    }

    /// <summary>Como sale la noche segun lo bien que hayas preparado el bar.</summary>
    public enum Noche
    {
        Tranquila  = 0,
        Buena      = 1,
        Legendaria = 2
    }

    /// <summary>Cuanto vale un hito y cuanto puede aportar como mucho.</summary>
    [Serializable]
    public class Regla
    {
        public Hito hito;
        [Tooltip("Estacion (1-9) en la que se agrupa en la pizarra.")]
        public int estacion;
        [Tooltip("Lo que aparece flotando junto a los puntos.")]
        public string etiqueta;
        public int puntos;
        [Tooltip("Maximo que puede aportar este hito en toda la noche.")]
        public int tope;

        public Regla(Hito hito, int estacion, string etiqueta, int puntos, int tope)
        {
            this.hito = hito;
            this.estacion = estacion;
            this.etiqueta = etiqueta;
            this.puntos = puntos;
            this.tope = tope;
        }
    }

    /// <summary>
    /// Sistema de puntos de la apertura. Cada gesto del ritual suma, ninguno
    /// resta: el documento pide una experiencia sin forma de perder, asi que
    /// los puntos miden lo bien preparado que esta el bar, no si fallaste.
    ///
    /// Cada cosa cuenta una sola vez (la misma palanca, el mismo disco, el
    /// mismo cartel) y cada hito tiene tope, para que el total premie
    /// recorrer el local y no repetir un gesto en bucle.
    ///
    /// Al girar el letrero el total decide como sale la noche, y eso se oye:
    /// cuanto mejor preparado, mas gente entra.
    /// </summary>
    public class BarScore : MonoBehaviour
    {
        public static BarScore Instance { get; private set; }

        private const string ClaveRecord = "Blackout.MejorNoche";

        [Header("Reglas")]
        [SerializeField] private Regla[] reglas = ReglasPorDefecto();

        [Header("La noche")]
        [Tooltip("Fraccion del maximo a partir de la cual la noche es buena.")]
        [SerializeField, Range(0f, 1f)] private float umbralBuena = 0.4f;
        [Tooltip("Fraccion del maximo a partir de la cual la noche es legendaria.")]
        [SerializeField, Range(0f, 1f)] private float umbralLegendaria = 0.75f;
        [Tooltip("Volumen del rumor de gente al abrir: tranquila, buena, legendaria.")]
        [SerializeField] private float[] volumenGente = { 0.35f, 0.7f, 1f };

        [Header("Puntos flotando")]
        [SerializeField] private TMPro.TMP_FontAsset fuente;
        [Tooltip("Blanco calido y no rojo: casi todo el bar esta en rojo y un +N rojo se pierde sobre el estante retroiluminado.")]
        [SerializeField] private Color colorPopup = new Color(1f, 0.9f, 0.72f);
        [Tooltip("Tamano de letra del +N. En TextMeshPro 3D, 1 equivale a unos 10 cm.")]
        [SerializeField] private float tamPopup = 0.9f;
        [SerializeField] private float segundosPopup = 1.6f;
        [SerializeField] private float subidaPopup = 0.22f;
        [Tooltip("Distancia a la que aparecen los hitos grandes, frente a la vista.")]
        [SerializeField] private float distanciaVista = 1.1f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipPunto;
        [Tooltip("Al abrir, cuando se anuncia como salio la noche.")]
        [SerializeField] private AudioClip clipVeredicto;
        [Tooltip("Espera tras girar el letrero, para no pisar el parpadeo del neon.")]
        [SerializeField] private float retrasoVeredicto = 3f;

        /// <summary>Hito y puntos que acaba de sumar.</summary>
        public event Action<Hito, int> Sumado;
        /// <summary>Se dispara una vez, al girar el letrero a OPEN.</summary>
        public event Action<Noche> Calificada;

        public int Total { get; private set; }
        public int Maximo { get; private set; }
        public int Record { get; private set; }
        public bool Calificado { get; private set; }
        public Noche NocheFinal { get; private set; }

        private readonly Dictionary<Hito, Regla> porHito = new Dictionary<Hito, Regla>();
        private readonly Dictionary<Hito, int> ganado = new Dictionary<Hito, int>();
        private readonly Dictionary<Hito, HashSet<UnityEngine.Object>> contados =
            new Dictionary<Hito, HashSet<UnityEngine.Object>>();

        private Transform cabeza;
        private BarStateManager estado;
        private float ultimoPopup = -999f;
        private Vector3 lugarUltimoPopup;
        private int apilados;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (reglas == null || reglas.Length == 0) reglas = ReglasPorDefecto();
            Maximo = 0;
            foreach (var r in reglas)
            {
                if (r == null || porHito.ContainsKey(r.hito)) continue;
                porHito.Add(r.hito, r);
                Maximo += Mathf.Max(0, r.tope);
            }

            Record = PlayerPrefs.GetInt(ClaveRecord, 0);
            if (Camera.main != null) cabeza = Camera.main.transform;
        }

        private void Start()
        {
            // En Start y no en OnEnable: el manager vive en el mismo objeto y
            // su Instance puede no existir todavia.
            estado = BarStateManager.Instance;
            if (estado != null) estado.PhaseChanged += OnFase;
        }

        private void OnDestroy()
        {
            if (estado != null) estado.PhaseChanged -= OnFase;
            if (Instance == this) Instance = null;
        }

        // ---------- Atajos para las estaciones ----------
        // Toleran que no haya sistema de puntos en la escena, como el resto.

        /// <summary>Hito grande: los puntos aparecen frente a la vista.</summary>
        public static void Anotar(Hito hito, UnityEngine.Object origen)
        {
            if (Instance != null) Instance.Sumar(hito, -1, origen, Vector3.zero, true);
        }

        /// <summary>Hito en un sitio concreto. El mismo origen solo cuenta una vez.</summary>
        public static void Anotar(Hito hito, UnityEngine.Object origen, Vector3 donde)
        {
            if (Instance != null) Instance.Sumar(hito, -1, origen, donde, false);
        }

        /// <summary>Puntos variables y repetibles hasta el tope (los dardos).</summary>
        public static void Anotar(Hito hito, int puntos, Vector3 donde)
        {
            if (Instance != null) Instance.Sumar(hito, puntos, null, donde, false);
        }

        /// <summary>Volumen del rumor de gente segun como va la noche.</summary>
        public static float VolumenGente()
        {
            if (Instance == null) return 1f;
            var v = Instance.volumenGente;
            int i = (int)Instance.Calcular(Instance.Total);
            if (v == null || i >= v.Length) return 1f;
            return Mathf.Clamp01(v[i]);
        }

        // ---------- Consulta para la pizarra ----------

        public int GanadoEn(int estacion)
        {
            int n = 0;
            foreach (var r in porHito.Values)
                if (r.estacion == estacion) n += Ganado(r.hito);
            return n;
        }

        public int MaximoEn(int estacion)
        {
            int n = 0;
            foreach (var r in porHito.Values)
                if (r.estacion == estacion) n += Mathf.Max(0, r.tope);
            return n;
        }

        public Noche Calcular(int total)
        {
            float k = Maximo > 0 ? (float)total / Maximo : 0f;
            if (k >= umbralLegendaria) return Noche.Legendaria;
            if (k >= umbralBuena) return Noche.Buena;
            return Noche.Tranquila;
        }

        public static string Nombre(Noche noche)
        {
            switch (noche)
            {
                case Noche.Legendaria: return "NOCHE LEGENDARIA";
                case Noche.Buena:      return "BUENA NOCHE";
                default:               return "NOCHE TRANQUILA";
            }
        }

        // ---------- Nucleo ----------

        /// <summary>
        /// Suma un hito y devuelve lo que realmente sumo (0 si ya contaba o
        /// llego al tope). Con puntos negativos usa los de la regla.
        /// </summary>
        public int Sumar(Hito hito, int puntos, UnityEngine.Object origen, Vector3 donde, bool frenteVista)
        {
            Regla r;
            if (!porHito.TryGetValue(hito, out r)) return 0;

            if (origen != null)
            {
                HashSet<UnityEngine.Object> vistos;
                if (!contados.TryGetValue(hito, out vistos))
                {
                    vistos = new HashSet<UnityEngine.Object>();
                    contados.Add(hito, vistos);
                }
                if (!vistos.Add(origen)) return 0;
            }

            int ya = Ganado(hito);
            int suma = Mathf.Min(puntos < 0 ? r.puntos : puntos, r.tope - ya);
            if (suma <= 0) return 0;

            ganado[hito] = ya + suma;
            Total += suma;

            // Despues de abrir el total puede seguir subiendo (los dardos);
            // el record lo recoge, la noche ya quedo decidida.
            if (Calificado) GuardarRecord();

            Celebrar(r, suma, donde, frenteVista);
            if (Sumado != null) Sumado(hito, suma);
            return suma;
        }

        private int Ganado(Hito hito)
        {
            int n;
            return ganado.TryGetValue(hito, out n) ? n : 0;
        }

        private void OnFase(BarPhase fase)
        {
            if (fase != BarPhase.Open || Calificado) return;

            Calificado = true;
            NocheFinal = Calcular(Total);
            GuardarRecord();

            if (Calificada != null) Calificada(NocheFinal);
            StartCoroutine(AnunciarNoche());
        }

        /// <summary>
        /// Al abrir estas fuera, en la fachada, y la pizarra queda dentro.
        /// El veredicto aparece tambien frente a la vista, cuando el neon ya prendio.
        /// </summary>
        private IEnumerator AnunciarNoche()
        {
            yield return new WaitForSeconds(retrasoVeredicto);

            if (cabeza == null && Camera.main != null) cabeza = Camera.main.transform;
            if (cabeza != null)
            {
                Vector3 donde = cabeza.position + cabeza.forward * (distanciaVista * 1.4f) + Vector3.up * 0.15f;
                string texto = Nombre(NocheFinal) + "\n<size=60%>" + Total + " PUNTOS</size>";
                ScorePopup.Lanzar(fuente, texto, colorPopup, donde, tamPopup * 1.6f, segundosPopup * 3f, subidaPopup * 0.5f);
            }
            if (sfx != null && clipVeredicto != null) { sfx.pitch = 1f; sfx.PlayOneShot(clipVeredicto); }
        }

        /// <summary>Olvida la mejor noche guardada. Para empezar limpio una demo.</summary>
        public static void BorrarRecord()
        {
            PlayerPrefs.DeleteKey(ClaveRecord);
            PlayerPrefs.Save();
            if (Instance != null) Instance.Record = 0;
        }

        private void GuardarRecord()
        {
            if (Total <= Record) return;
            Record = Total;
            PlayerPrefs.SetInt(ClaveRecord, Record);
            PlayerPrefs.Save();
        }

        private void Celebrar(Regla r, int suma, Vector3 donde, bool frenteVista)
        {
            if (cabeza == null && Camera.main != null) cabeza = Camera.main.transform;

            // Los hitos grandes van algo por encima de la mirada y mas grandes:
            // suelen coincidir con el ultimo gesto pequeno (la cuarta palanca),
            // cuyo +N sale a la altura de la mano.
            bool hayLugar = !frenteVista || cabeza != null;
            float tam = frenteVista ? tamPopup * 1.3f : tamPopup;
            if (frenteVista && cabeza != null)
                donde = cabeza.position + cabeza.forward * distanciaVista + Vector3.up * 0.15f;

            // Dos hitos a la vez en el mismo sitio (el primer golpe al escenario
            // da instrumento y escenario) se apilan en vez de pisarse.
            if (Time.time - ultimoPopup < 0.5f && Vector3.Distance(donde, lugarUltimoPopup) < 0.3f) apilados++;
            else apilados = 0;
            ultimoPopup = Time.time;
            lugarUltimoPopup = donde;
            donde += Vector3.up * (0.18f * apilados); // lo que ocupa un +N con su etiqueta

            string texto = "+" + suma;
            if (!string.IsNullOrEmpty(r.etiqueta)) texto += "\n<size=55%>" + r.etiqueta + "</size>";
            if (hayLugar) ScorePopup.Lanzar(fuente, texto, colorPopup, donde, tam, segundosPopup, subidaPopup);

            // La diana ya suena con su propio tono al clavar.
            if (sfx != null && clipPunto != null && r.hito != Hito.Dardo)
            {
                // Mas agudo cuanto mas vale el gesto.
                sfx.pitch = Mathf.Lerp(0.9f, 1.3f, Mathf.Clamp01(suma / 100f));
                sfx.PlayOneShot(clipPunto);
            }
        }

        /// <summary>
        /// Valores de partida, elegidos sin probar con el visor. Suman 1335:
        /// los gestos que abren el bar valen mas que los de recreo.
        /// </summary>
        public static Regla[] ReglasPorDefecto()
        {
            return new[]
            {
                new Regla(Hito.Reja,             1, "REJA ARRIBA",        50,  50),
                new Regla(Hito.Palanca,          2, "LUZ",                25, 100),
                new Regla(Hito.BarEncendido,     2, "BAR ENCENDIDO",     100, 100),
                new Regla(Hito.BotellaEnEstante, 3, "BOTELLA EN SU SITIO", 20,  80),
                new Regla(Hito.EstanteCompleto,  3, "ESTANTE ORDENADO",   60,  60),
                new Regla(Hito.Servir,           3, "TRAGO SERVIDO",      40,  80),
                new Regla(Hito.Trago,            3, "SALUD",              30,  60),
                new Regla(Hito.Disco,            4, "DISCO NUEVO",        25, 125),
                new Regla(Hito.Escenario,        5, "ESCENARIO DESPIERTO", 50,  50),
                new Regla(Hito.Instrumento,      5, "PRUEBA DE SONIDO",   10, 100),
                new Regla(Hito.Cartel,           6, "CARTEL COLGADO",     15, 105),
                new Regla(Hito.Tele,             7, "TELE ENCENDIDA",     30,  30),
                new Regla(Hito.Canal,            7, "CANAL NUEVO",        15,  45),
                new Regla(Hito.Dardo,            8, "",                    0, 150),
                new Regla(Hito.Partida501,       8, "501 CERRADO",       100, 100),
                new Regla(Hito.Abrir,            9, "ABIERTO",           100, 100)
            };
        }
    }
}
