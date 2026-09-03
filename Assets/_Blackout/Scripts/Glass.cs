using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// Vaso de la barra. Se llena al servirle una botella encima y se bebe
    /// acercandolo a la boca e inclinandolo.
    /// </summary>
    public class Glass : MonoBehaviour
    {
        [Header("Liquido")]
        [Tooltip("Hijo que representa el contenido. Se escala segun el nivel.")]
        [SerializeField] private Transform liquido;
        [SerializeField] private float alturaMaxima = 0.09f;
        [SerializeField, Range(0f, 1f)] private float nivel = 0f;

        [Header("Beber")]
        [SerializeField] private float distanciaBoca = 0.3f;
        [SerializeField] private float anguloTrago = 40f;
        [SerializeField] private float ritmoTrago = 0.6f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipTrago;

        private Transform cabeza;
        private float baseY;

        public float Nivel { get { return nivel; } }

        private void Awake()
        {
            if (liquido != null) baseY = liquido.localPosition.y;
            if (Camera.main != null) cabeza = Camera.main.transform;
            ActualizarVisual();
        }

        public void Llenar(float cantidad)
        {
            nivel = Mathf.Clamp01(nivel + cantidad);
            ActualizarVisual();
        }

        private void Update()
        {
            if (cabeza == null)
            {
                if (Camera.main != null) cabeza = Camera.main.transform;
                return;
            }
            if (nivel <= 0f) return;
            if (Vector3.Distance(transform.position, cabeza.position) > distanciaBoca) return;
            if (Vector3.Angle(transform.up, Vector3.up) < anguloTrago) return;

            nivel = Mathf.Max(0f, nivel - ritmoTrago * Time.deltaTime);
            ActualizarVisual();

            if (sfx != null && clipTrago != null && !sfx.isPlaying)
                sfx.PlayOneShot(clipTrago);
        }

        private void ActualizarVisual()
        {
            if (liquido == null) return;
            bool hay = nivel > 0.01f;
            if (liquido.gameObject.activeSelf != hay) liquido.gameObject.SetActive(hay);
            if (!hay) return;

            float alto = Mathf.Max(0.001f, nivel * alturaMaxima);
            var e = liquido.localScale;
            liquido.localScale = new Vector3(e.x, alto, e.z);
            var p = liquido.localPosition;
            liquido.localPosition = new Vector3(p.x, baseY + alto * 0.5f, p.z);
        }
    }
}
