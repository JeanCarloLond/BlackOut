using UnityEngine;

namespace Blackout
{
    /// <summary>
    /// 06 Muro de posters. Cada cartel es una banda y una noche:
    /// lleva encima la anecdota que se cuenta al fijarlo.
    /// </summary>
    public class Poster : MonoBehaviour
    {
        [SerializeField] private string banda = "Banda";
        [Tooltip("Audio que cuenta la anecdota de esa banda.")]
        [SerializeField] private AudioClip anecdota;

        public string Banda { get { return banda; } }
        public AudioClip Anecdota { get { return anecdota; } }
    }
}
