using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// El reloj de la pared. Arranca a las 20:30 y corre: llegas antes de
    /// abrir y el tiempo pasa mientras preparas el local. Nadie te mete
    /// prisa, pero el reloj esta ahi.
    /// </summary>
    public class WallClock : MonoBehaviour
    {
        [Header("Manecillas")]
        [SerializeField] private Transform manecillaHora;
        [SerializeField] private Transform manecillaMinuto;
        [SerializeField] private Transform segundero;

        [Header("Hora")]
        [Tooltip("Hora a la que empieza la experiencia, en horas decimales. 20.5 = 20:30.")]
        [SerializeField] private float horaInicial = 20.5f;
        [Tooltip("1 = tiempo real. Subelo si quieres que se note el paso del rato.")]
        [SerializeField] private float velocidad = 1f;

        [Header("Tictac")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipTic;
        [Tooltip("El tictac solo se oye de cerca; deja el clip vacio para no usarlo.")]
        [SerializeField] private float distanciaTic = 2.2f;

        private float horas;
        private int ultimoSegundo = -1;
        private Transform cabeza;

        /// <summary>Hora actual en horas decimales, por si alguna estacion la necesita.</summary>
        public float Hora { get { return horas % 24f; } }

        private void Awake()
        {
            horas = horaInicial;
            if (Camera.main != null) cabeza = Camera.main.transform;
            Colocar();
        }

        private void Update()
        {
            horas += (Time.deltaTime / 3600f) * velocidad;
            Colocar();
            Tictac();
        }

        private void Colocar()
        {
            float h = horas % 12f;
            float m = (horas * 60f) % 60f;
            float s = (horas * 3600f) % 60f;

            // Las manecillas giran sobre el eje Z, el que apunta fuera de la esfera
            if (manecillaHora != null)    manecillaHora.localRotation    = Quaternion.Euler(0f, 0f, -h * 30f);
            if (manecillaMinuto != null)  manecillaMinuto.localRotation  = Quaternion.Euler(0f, 0f, -m * 6f);
            if (segundero != null)        segundero.localRotation        = Quaternion.Euler(0f, 0f, -s * 6f);
        }

        private void Tictac()
        {
            if (sfx == null || clipTic == null) return;
            if (cabeza == null)
            {
                if (Camera.main == null) return;
                cabeza = Camera.main.transform;
            }
            if (Vector3.Distance(transform.position, cabeza.position) > distanciaTic) return;

            int seg = Mathf.FloorToInt((horas * 3600f) % 60f);
            if (seg == ultimoSegundo) return;
            ultimoSegundo = seg;
            sfx.PlayOneShot(clipTic, 0.25f);
        }
    }
}
