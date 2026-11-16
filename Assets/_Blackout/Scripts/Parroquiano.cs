using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout
{
    /// <summary>
    /// El Chancho, el parroquiano de siempre: se queda en el bar despues de
    /// cerrar y nadie sabe como entro. Lo pidio el profesor: un personaje al
    /// que se pueda golpear y lanzar.
    ///
    /// Es un ragdoll que nunca es cinematico. De pie lo sostienen muelles en
    /// las articulaciones y en la cadera, asi que un golpe flojo lo hace
    /// tambalearse como un tentetieso. Un golpe fuerte o agarrarlo lo deja
    /// sin muelles: cae, se puede lanzar, y al rato se levanta solo en su
    /// sitio. No es cinematico a proposito: XRGrabInteractable restaura el
    /// estado cinematico al soltar, y un cuerpo cinematico no sale lanzado.
    /// </summary>
    public class Parroquiano : MonoBehaviour
    {
        [Header("Cuerpo")]
        [Tooltip("La pieza que sostiene a las demas. Lleva el muelle a la cadera.")]
        [SerializeField] private Rigidbody torso;
        [Tooltip("Si se deja vacio, toma todos los Rigidbody hijos.")]
        [SerializeField] private Rigidbody[] partes;

        [Header("De pie")]
        [Tooltip("Rigidez de la postura. Mas alto, mas tieso.")]
        [SerializeField] private float muellePostura = 400f;
        [SerializeField] private float amortiguaPostura = 25f;
        [Tooltip("Lo que lo mantiene en su sitio y derecho.")]
        [SerializeField] private float muelleCadera = 700f;
        [SerializeField] private float amortiguaCadera = 35f;

        [Header("Golpes")]
        [Tooltip("Impulso a partir del cual cae redondo.")]
        [SerializeField] private float umbralNoqueo = 7f;
        [Tooltip("Tiempo muerto entre golpes para que un punetazo no cuente varias veces.")]
        [SerializeField] private float reboteSegundos = 0.15f;

        [Header("Levantarse")]
        [Tooltip("Segundos en el suelo antes de volver a su sitio.")]
        [SerializeField] private float segundosLevantarse = 8f;

        [Header("Sonido")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip clipGolpe;
        [Tooltip("Al caer redondo.")]
        [SerializeField] private AudioClip clipNoqueo;

        public bool Noqueado { get; private set; }

        private ConfigurableJoint[] articulaciones;
        private ConfigurableJoint cadera;
        private readonly List<Pose> poseInicial = new List<Pose>();
        private readonly List<XRGrabInteractable> agarres = new List<XRGrabInteractable>();
        private float momentoNoqueo;
        private float ultimoGolpe = -999f;

        private void Awake()
        {
            if (partes == null || partes.Length == 0) partes = GetComponentsInChildren<Rigidbody>();
            if (torso != null) cadera = torso.GetComponent<ConfigurableJoint>();

            var lista = new List<ConfigurableJoint>();
            foreach (var p in partes)
            {
                if (p == null) continue;
                poseInicial.Add(new Pose(p.transform.position, p.transform.rotation));
                var j = p.GetComponent<ConfigurableJoint>();
                if (j != null && j != cadera) lista.Add(j);
                var a = p.GetComponent<XRGrabInteractable>();
                if (a != null) agarres.Add(a);
            }
            articulaciones = lista.ToArray();
            Muelles(true);
        }

        private void OnEnable()
        {
            foreach (var a in agarres) a.selectEntered.AddListener(OnAgarrado);
        }

        private void OnDisable()
        {
            foreach (var a in agarres) a.selectEntered.RemoveListener(OnAgarrado);
        }

        private void Update()
        {
            if (!Noqueado || Agarrado) return;
            if (Time.time - momentoNoqueo < segundosLevantarse) return;
            Levantarse();
        }

        public bool Agarrado
        {
            get
            {
                foreach (var a in agarres) if (a.isSelected) return true;
                return false;
            }
        }

        /// <summary>Un golpe de la mano. Impulso en N·s.</summary>
        public void Recibir(Rigidbody parte, Vector3 impulso, Vector3 punto)
        {
            if (Time.time - ultimoGolpe < reboteSegundos) return;
            ultimoGolpe = Time.time;

            if (parte == null) parte = torso;
            parte.AddForceAtPosition(impulso, punto, ForceMode.Impulse);

            float fuerza = impulso.magnitude;
            if (sfx != null && clipGolpe != null)
            {
                sfx.pitch = Random.Range(0.85f, 1.15f);
                sfx.PlayOneShot(clipGolpe, Mathf.Clamp01(0.3f + fuerza / umbralNoqueo));
            }
            if (fuerza >= umbralNoqueo) Noquear();
        }

        private void OnAgarrado(SelectEnterEventArgs args)
        {
            // Nadie se deja levantar en volandas y sigue de pie.
            Noquear();
        }

        public void Noquear()
        {
            momentoNoqueo = Time.time;
            if (Noqueado) return;
            Noqueado = true;
            Muelles(false);
            if (sfx != null && clipNoqueo != null) sfx.PlayOneShot(clipNoqueo);
        }

        /// <summary>Vuelve a su sitio de pie, como si nada.</summary>
        public void Levantarse()
        {
            for (int i = 0; i < partes.Length && i < poseInicial.Count; i++)
            {
                var p = partes[i];
                if (p == null) continue;
                p.linearVelocity = Vector3.zero;
                p.angularVelocity = Vector3.zero;
                p.position = poseInicial[i].position;
                p.rotation = poseInicial[i].rotation;
                p.transform.SetPositionAndRotation(poseInicial[i].position, poseInicial[i].rotation);
            }
            Physics.SyncTransforms();
            Muelles(true);
            Noqueado = false;
        }

        private void Muelles(bool dePie)
        {
            var postura = Drive(dePie ? muellePostura : 0f, dePie ? amortiguaPostura : 2f);
            foreach (var j in articulaciones)
                if (j != null) j.slerpDrive = postura;

            if (cadera == null) return;
            var sosten = Drive(dePie ? muelleCadera : 0f, dePie ? amortiguaCadera : 0f);
            cadera.xDrive = sosten;
            cadera.yDrive = sosten;
            cadera.zDrive = sosten;
            cadera.slerpDrive = sosten;
        }

        private static JointDrive Drive(float muelle, float amortigua)
        {
            var d = new JointDrive();
            d.positionSpring = muelle;
            d.positionDamper = amortigua;
            d.maximumForce = float.MaxValue;
            return d;
        }
    }
}
