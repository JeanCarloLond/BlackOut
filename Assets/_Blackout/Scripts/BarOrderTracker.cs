using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// Cuenta los huecos ocupados del estante. Ordenar la barra es una de las
    /// mecanicas con proposito: marca avance, sin puntaje ni castigo.
    /// </summary>
    public class BarOrderTracker : MonoBehaviour
    {
        [Tooltip("Si se deja vacio, toma todos los ShelfSlot hijos.")]
        [SerializeField] private ShelfSlot[] huecos;

        [Header("Al completar el estante")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipCompletado;

        private bool completadoUnaVez;

        public int Ocupados { get; private set; }
        public int Total { get { return huecos != null ? huecos.Length : 0; } }

        private void Awake()
        {
            if (huecos == null || huecos.Length == 0)
                huecos = GetComponentsInChildren<ShelfSlot>(true);
        }

        private void OnEnable()
        {
            foreach (var h in huecos)
                if (h != null) h.Cambiado += OnHuecoCambiado;
        }

        private void OnDisable()
        {
            foreach (var h in huecos)
                if (h != null) h.Cambiado -= OnHuecoCambiado;
        }

        private void OnHuecoCambiado(ShelfSlot hueco)
        {
            int n = 0;
            foreach (var h in huecos)
                if (h != null && h.Ocupado) n++;
            Ocupados = n;

            if (completadoUnaVez || Total == 0 || Ocupados < Total) return;
            completadoUnaVez = true;

            if (sfx != null && clipCompletado != null) sfx.PlayOneShot(clipCompletado);
        }
    }
}
