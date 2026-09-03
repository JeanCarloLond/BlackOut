using UnityEngine;

namespace Blackout
{
    /// <summary>Un vinilo del cajon. Lleva encima el tema que hara sonar el local.</summary>
    public class Vinyl : MonoBehaviour
    {
        [SerializeField] private string titulo = "Disco sin nombre";
        [SerializeField] private AudioClip tema;

        public string Titulo { get { return titulo; } }
        public AudioClip Tema { get { return tema; } }
    }
}
