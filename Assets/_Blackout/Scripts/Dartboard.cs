using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 08 Los dardos. Diana desgastada con puntaje opcional y sin castigo:
    /// una pausa ludica, no una prueba que se pueda perder.
    /// </summary>
    public class Dartboard : MonoBehaviour
    {
        [Tooltip("Centro de la diana. Si se deja vacio usa el propio transform.")]
        [SerializeField] private Transform centro;
        [SerializeField] private float radio = 0.22f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("Tono de puntaje, distinto segun lo cerca que caiga.")]
        [SerializeField] private AudioClip clipTono;

        /// <summary>Puntos del ultimo dardo clavado.</summary>
        public event System.Action<int> Anotado;

        public int Total { get; private set; }

        private void Awake()
        {
            if (centro == null) centro = transform;
        }

        /// <summary>Puntua un impacto y devuelve los puntos. Nunca resta.</summary>
        public int Puntuar(Vector3 puntoImpacto)
        {
            float d = Vector3.Distance(puntoImpacto, centro.position);
            float k = Mathf.Clamp01(d / Mathf.Max(0.0001f, radio));

            int puntos;
            if (k < 0.15f) puntos = 50;
            else if (k < 0.35f) puntos = 25;
            else if (k < 0.65f) puntos = 10;
            else puntos = 5;

            Total += puntos;

            if (sfx != null && clipTono != null)
            {
                // Mas agudo cuanto mas cerca del centro.
                sfx.pitch = Mathf.Lerp(1.35f, 0.85f, k);
                sfx.PlayOneShot(clipTono);
            }

            if (Anotado != null) Anotado(puntos);
            return puntos;
        }
    }
}
