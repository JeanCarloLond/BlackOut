using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 08 Los dardos. El marcador de tiza del muro, que cuenta de verdad.
    /// Arranca en 501 y va bajando, como en cualquier bar. Si te pasas se
    /// queda en cero: el documento pide puntaje sin castigo, asi que aqui
    /// no hay penalizacion ni forma de perder.
    /// </summary>
    public class ChalkScoreboard : MonoBehaviour
    {
        [SerializeField] private Dartboard diana;
        [Tooltip("Texto de tiza donde se escribe lo que queda.")]
        [SerializeField] private TMPro.TMP_Text marcador;
        [SerializeField] private int puntuacionInicial = 501;

        [Header("Al llegar a cero")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipCierre;

        public int Restante { get; private set; }

        private bool cerrado;

        private void Awake()
        {
            Restante = puntuacionInicial;
            Escribir();
        }

        private void OnEnable()
        {
            if (diana != null) diana.Anotado += OnAnotado;
        }

        private void OnDisable()
        {
            if (diana != null) diana.Anotado -= OnAnotado;
        }

        private void OnAnotado(int puntos)
        {
            if (cerrado) return;

            Restante = Mathf.Max(0, Restante - puntos);
            Escribir();

            if (Restante > 0) return;
            cerrado = true;
            if (sfx != null && clipCierre != null) sfx.PlayOneShot(clipCierre);
        }

        private void Escribir()
        {
            if (marcador == null) return;
            marcador.text = Restante.ToString();
        }

        /// <summary>Vuelve a empezar la partida. No hay castigo por reiniciar.</summary>
        public void Reiniciar()
        {
            cerrado = false;
            Restante = puntuacionInicial;
            Escribir();
        }
    }
}
