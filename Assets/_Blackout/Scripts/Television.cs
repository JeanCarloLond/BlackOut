using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// 07 El televisor. La tele muerta sobre la barra trae la voz del mundo
    /// de afuera: estatica al prender y luego un canal de conciertos.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class Television : MonoBehaviour
    {
        [Header("Mandos")]
        [SerializeField] private XRSimpleInteractable botonEncendido;
        [SerializeField] private XRSimpleInteractable botonCanal;

        [Header("Pantalla")]
        [SerializeField] private Renderer pantalla;
        [SerializeField] private Color colorApagada = new Color(0.02f, 0.02f, 0.03f);
        [SerializeField] private Color colorEncendida = new Color(0.55f, 0.72f, 1f);
        [SerializeField] private float emisionEncendida = 2.2f;

        [Header("Canales")]
        [SerializeField] private VideoClip[] canales;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [Tooltip("Estatica al prender, antes de que entre el canal.")]
        [SerializeField] private AudioClip clipEstatica;
        [Tooltip("Clic del boton de encendido.")]
        [SerializeField] private AudioClip clipClic;

        [Header("Haptico")]
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.4f;
        [SerializeField] private float hapticDuration = 0.05f;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private VideoPlayer player;
        private MaterialPropertyBlock mpb;
        private int canal;
        private bool encendida;

        private void Awake()
        {
            player = GetComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            mpb = new MaterialPropertyBlock();
            PintarPantalla(colorApagada, 0f);
        }

        private void OnEnable()
        {
            if (botonEncendido != null) botonEncendido.selectEntered.AddListener(OnEncender);
            if (botonCanal != null) botonCanal.selectEntered.AddListener(OnCambiarCanal);
        }

        private void OnDisable()
        {
            if (botonEncendido != null) botonEncendido.selectEntered.RemoveListener(OnEncender);
            if (botonCanal != null) botonCanal.selectEntered.RemoveListener(OnCambiarCanal);
        }

        private void OnEncender(SelectEnterEventArgs args)
        {
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            if (sfx != null && clipClic != null) sfx.PlayOneShot(clipClic);

            encendida = !encendida;

            if (!encendida)
            {
                player.Stop();
                PintarPantalla(colorApagada, 0f);
                return;
            }

            if (sfx != null && clipEstatica != null) sfx.PlayOneShot(clipEstatica);
            PintarPantalla(colorEncendida, emisionEncendida);
            Sintonizar(canal);
        }

        private void OnCambiarCanal(SelectEnterEventArgs args)
        {
            if (!encendida) return;
            HapticFeedback.Send(args.interactorObject, hapticAmplitude, hapticDuration);
            if (sfx != null && clipClic != null) sfx.PlayOneShot(clipClic);

            if (canales == null || canales.Length == 0) return;
            canal = (canal + 1) % canales.Length;
            Sintonizar(canal);
        }

        private void Sintonizar(int indice)
        {
            if (canales == null || canales.Length == 0) return;
            indice = Mathf.Clamp(indice, 0, canales.Length - 1);
            if (canales[indice] == null) return;

            player.clip = canales[indice];
            player.Play();
        }

        private void PintarPantalla(Color color, float intensidad)
        {
            if (pantalla == null) return;
            pantalla.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColor, color * Mathf.Max(0f, intensidad));
            pantalla.SetPropertyBlock(mpb);
        }
    }
}
