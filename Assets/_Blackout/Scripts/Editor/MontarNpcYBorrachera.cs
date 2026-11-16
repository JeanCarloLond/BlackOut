using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Blackout.Herramientas
{
    /// <summary>
    /// Monta el parroquiano y la borrachera en la escena abierta. Idempotente,
    /// como Montar sistema de puntos: si ya existen los rehace.
    /// </summary>
    public static class MontarNpcYBorrachera
    {
        private const string RutaSettings = "Assets/_Blackout/Settings/";
        private const string RutaMateriales = "Assets/_Blackout/Materials/";
        private const string RutaSfx = "Assets/_Blackout/Audio/SFX/";
        private const string NombreNpc = "Parroquiano_Chancho";

        // Junto a la barra, de espaldas a ella y mirando a la entrada: es lo
        // primero que ves al encender las luces.
        private static readonly Vector3 SitioNpc = new Vector3(-2.3f, 0f, 0.4f);
        private static readonly Vector3 MiraHacia = new Vector3(0f, 0f, -3f);

        // ---------- Post-procesado y borrachera ----------

        [MenuItem("Blackout/Montar post-procesado y borrachera")]
        public static void MontarBorrachera()
        {
            RehacerPostFX();
            var borracho = PerfilBorrachera();

            // La camara tenia el post-procesado apagado (lo trae asi el template
            // por rendimiento en Quest): sin esto no se ve ni el bloom ni la borrachera.
            var cam = GameObject.Find("Main Camera");
            var datos = cam != null ? cam.GetComponent<UniversalAdditionalCameraData>() : null;
            if (datos != null) { Undo.RecordObject(datos, "Post-procesado"); datos.renderPostProcessing = true; }
            else Debug.LogWarning("[Borrachera] No encontre la Main Camera con UniversalAdditionalCameraData.");
            ActivarPostEnRenderers();

            var gm = GameObject.Find("GameManager");
            if (gm == null) { Debug.LogError("[Borrachera] No hay GameManager."); return; }

            var viejo = gm.transform.Find("Borrachera");
            if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
            var go = new GameObject("Borrachera");
            Undo.RegisterCreatedObjectUndo(go, "Borrachera");
            go.transform.SetParent(gm.transform, false);

            var volumen = go.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 20f; // por encima del de la escena, que esta en 10
            volumen.weight = 0f;
            volumen.sharedProfile = borracho;

            var audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            go.AddComponent<Borrachera>();

            // La botella necesita su sonido de trago para beber a morro.
            var trago = Clip("SFX_Trago.wav");
            foreach (var b in Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(b);
                so.FindProperty("clipTrago").objectReferenceValue = trago;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(gm.scene);
            Debug.Log("[Borrachera] Montado. Guarda la escena.");
        }

        /// <summary>
        /// Blackout_PostFX tenia sus cinco efectos sin guardar dentro del asset
        /// (componentes con fileID 0): se crearon por codigo sin AddObjectToAsset.
        /// Se rehacen con los efectos que describe el README.
        /// </summary>
        private static void RehacerPostFX()
        {
            var ruta = RutaSettings + "Blackout_PostFX.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);
            }
            Vaciar(perfil);

            var bloom = Efecto<Bloom>(perfil);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.7f);

            Efecto<Tonemapping>(perfil).mode.Override(TonemappingMode.ACES);

            var vineta = Efecto<Vignette>(perfil);
            vineta.intensity.Override(0.3f);
            vineta.smoothness.Override(0.45f);

            var grano = Efecto<FilmGrain>(perfil);
            grano.type.Override(FilmGrainLookup.Thin1);
            grano.intensity.Override(0.15f);

            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// El renderer de Android (el que usan las Quest) venia del template sin
        /// PostProcessData, que es el interruptor general del post-procesado en URP.
        /// Se le asigna el mismo que ya tiene el de PC. Cuesta rendimiento en Quest.
        /// </summary>
        private static void ActivarPostEnRenderers()
        {
            PostProcessData datos = null;
            var renderers = new System.Collections.Generic.List<UniversalRendererData>();
            foreach (var g in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var ruta = AssetDatabase.GUIDToAssetPath(g);
                if (ruta.StartsWith("Packages/")) continue;
                var r = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ruta);
                renderers.Add(r);
                if (datos == null && r.postProcessData != null) datos = r.postProcessData;
            }
            if (datos == null)
            {
                Debug.LogWarning("[Borrachera] Ningun renderer tiene PostProcessData para copiar.");
                return;
            }
            foreach (var r in renderers)
            {
                if (r.postProcessData != null) continue;
                r.postProcessData = datos;
                EditorUtility.SetDirty(r);
                Debug.Log("[Borrachera] Post-procesado activado en " + AssetDatabase.GetAssetPath(r));
            }
            AssetDatabase.SaveAssets();
        }

        private static VolumeProfile PerfilBorrachera()
        {
            var ruta = RutaSettings + "Blackout_Borrachera.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);
            }
            Vaciar(perfil);

            // Lo lejano se emborrona: lo cercano (el vaso, la mano) sigue nitido.
            var dof = Efecto<DepthOfField>(perfil);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(0.8f);
            dof.gaussianEnd.Override(4f);
            dof.gaussianMaxRadius.Override(1.5f);

            // La vista se cierra.
            var vineta = Efecto<Vignette>(perfil);
            vineta.intensity.Override(0.55f);
            vineta.smoothness.Override(0.6f);
            vineta.color.Override(new Color(0.12f, 0f, 0.02f));

            // Colores subidos; el tono lo hace oscilar Borrachera en Play.
            var color = Efecto<ColorAdjustments>(perfil);
            color.saturation.Override(35f);
            color.contrast.Override(15f);
            color.hueShift.Override(0f);

            // Las luces de neon se emborronan mas.
            Efecto<Bloom>(perfil).intensity.Override(2.2f);

            // Con tope bajo: URP la desaconseja en VR porque marea.
            Efecto<ChromaticAberration>(perfil).intensity.Override(0.3f);

            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        private static void Vaciar(VolumeProfile perfil)
        {
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(perfil)))
                if (sub is VolumeComponent) Object.DestroyImmediate(sub, true);
            perfil.components.Clear();
        }

        private static T Efecto<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            var c = perfil.Add<T>(true);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, perfil);
            return c;
        }

        // ---------- Parroquiano ----------

        [MenuItem("Blackout/Montar parroquiano")]
        public static void MontarParroquiano()
        {
            var viejo = GameObject.Find(NombreNpc);
            if (viejo != null) Undo.DestroyObjectImmediate(viejo);

            var piel = Material("Mat_Chancho_Piel", new Color(0.96f, 0.62f, 0.66f), 0.35f);
            var camiseta = Material("Mat_Chancho_Camiseta", new Color(0.05f, 0.05f, 0.06f), 0.2f);
            var pantalon = Material("Mat_Chancho_Pantalon", new Color(0.12f, 0.17f, 0.3f), 0.15f);
            var negro = Material("Mat_Chancho_Ojos", new Color(0.02f, 0.02f, 0.02f), 0.85f);
            var hocico = Material("Mat_Chancho_Hocico", new Color(0.88f, 0.48f, 0.54f), 0.4f);

            var raiz = new GameObject(NombreNpc);
            Undo.RegisterCreatedObjectUndo(raiz, NombreNpc);
            Vector3 frente = MiraHacia - SitioNpc; frente.y = 0f;
            raiz.transform.SetPositionAndRotation(SitioNpc, Quaternion.LookRotation(frente));

            // Todas las piezas son hermanas bajo la raiz: XRGrabInteractable recoge
            // los colliders de sus hijos, y la cabeza no debe arrastrar al torso.
            var torso = Pieza("Torso", raiz.transform, PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.42f, 0.32f, 0.36f), camiseta, 14f);
            var cabeza = Pieza("Cabeza", raiz.transform, PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0.02f), new Vector3(0.4f, 0.36f, 0.38f), piel, 4f);
            var brazoI = Pieza("Brazo_Izq", raiz.transform, PrimitiveType.Capsule, new Vector3(-0.27f, 1.02f, 0f), new Vector3(0.12f, 0.24f, 0.12f), piel, 2f);
            var brazoD = Pieza("Brazo_Der", raiz.transform, PrimitiveType.Capsule, new Vector3(0.27f, 1.02f, 0f), new Vector3(0.12f, 0.24f, 0.12f), piel, 2f);
            var piernaI = Pieza("Pierna_Izq", raiz.transform, PrimitiveType.Capsule, new Vector3(-0.1f, 0.36f, 0f), new Vector3(0.16f, 0.36f, 0.16f), pantalon, 4f);
            var piernaD = Pieza("Pierna_Der", raiz.transform, PrimitiveType.Capsule, new Vector3(0.1f, 0.36f, 0f), new Vector3(0.16f, 0.36f, 0.16f), pantalon, 4f);

            // La cara: decorado sin collider, hijo de la cabeza.
            Adorno("Hocico", cabeza, PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0.5f), new Vector3(0.42f, 0.12f, 0.4f), Quaternion.Euler(90f, 0f, 0f), hocico);
            Adorno("Narina_Izq", cabeza, PrimitiveType.Sphere, new Vector3(-0.08f, -0.08f, 0.62f), Vector3.one * 0.08f, Quaternion.identity, negro);
            Adorno("Narina_Der", cabeza, PrimitiveType.Sphere, new Vector3(0.08f, -0.08f, 0.62f), Vector3.one * 0.08f, Quaternion.identity, negro);
            Adorno("Ojo_Izq", cabeza, PrimitiveType.Sphere, new Vector3(-0.2f, 0.16f, 0.42f), Vector3.one * 0.12f, Quaternion.identity, negro);
            Adorno("Ojo_Der", cabeza, PrimitiveType.Sphere, new Vector3(0.2f, 0.16f, 0.42f), Vector3.one * 0.12f, Quaternion.identity, negro);
            Adorno("Oreja_Izq", cabeza, PrimitiveType.Cube, new Vector3(-0.3f, 0.48f, 0f), new Vector3(0.28f, 0.3f, 0.06f), Quaternion.Euler(0f, 0f, 25f), hocico);
            Adorno("Oreja_Der", cabeza, PrimitiveType.Cube, new Vector3(0.3f, 0.48f, 0f), new Vector3(0.28f, 0.3f, 0.06f), Quaternion.Euler(0f, 0f, -25f), hocico);

            // Articulaciones: pivote en el cuello, los hombros y las caderas.
            Articular(cabeza, torso, raiz.transform.TransformPoint(new Vector3(0f, 1.32f, 0f)), 35f, 30f);
            Articular(brazoI, torso, raiz.transform.TransformPoint(new Vector3(-0.27f, 1.26f, 0f)), 80f, 60f);
            Articular(brazoD, torso, raiz.transform.TransformPoint(new Vector3(0.27f, 1.26f, 0f)), 80f, 60f);
            Articular(piernaI, torso, raiz.transform.TransformPoint(new Vector3(-0.1f, 0.72f, 0f)), 70f, 25f);
            Articular(piernaD, torso, raiz.transform.TransformPoint(new Vector3(0.1f, 0.72f, 0f)), 70f, 25f);

            // La cadera: un muelle al mundo que lo mantiene en su sitio y derecho.
            var cadera = torso.gameObject.AddComponent<ConfigurableJoint>();
            cadera.connectedBody = null;
            cadera.anchor = torso.transform.InverseTransformPoint(raiz.transform.TransformPoint(new Vector3(0f, 0.8f, 0f)));
            cadera.rotationDriveMode = RotationDriveMode.Slerp;

            foreach (var rb in new[] { cabeza, torso, brazoI, brazoD })
                Agarrable(rb.gameObject);

            var audio = raiz.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.maxDistance = 12f;

            var npc = raiz.AddComponent<Parroquiano>();
            var so = new SerializedObject(npc);
            so.FindProperty("torso").objectReferenceValue = torso;
            so.FindProperty("sfx").objectReferenceValue = audio;
            so.FindProperty("clipGolpe").objectReferenceValue = Clip("SFX_Tambor.wav");
            so.FindProperty("clipNoqueo").objectReferenceValue = Clip("SFX_Platillo.wav");
            so.ApplyModifiedPropertiesWithoutUndo();

            MontarManos();

            EditorSceneManager.MarkSceneDirty(raiz.scene);
            Selection.activeGameObject = raiz;
            Debug.Log("[Parroquiano] Montado. Guarda la escena.");
        }

        private static void MontarManos()
        {
            foreach (var nombre in new[] { "Left Controller", "Right Controller" })
            {
                var mando = GameObject.Find(nombre);
                if (mando == null) { Debug.LogWarning("[Parroquiano] No encontre " + nombre); continue; }
                var viejo = mando.transform.Find("Golpe_Mano");
                if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

                var go = new GameObject("Golpe_Mano");
                Undo.RegisterCreatedObjectUndo(go, "Golpe_Mano");
                go.transform.SetParent(mando.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 0.04f); // los nudillos, delante del mando
                var esfera = go.AddComponent<SphereCollider>();
                esfera.radius = 0.07f;
                esfera.isTrigger = true;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                go.AddComponent<GolpeMano>();
            }
        }

        private static Rigidbody Pieza(string nombre, Transform padre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat, float masa)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = masa;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            return rb;
        }

        private static void Adorno(string nombre, Rigidbody padre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = escala;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void Articular(Rigidbody pieza, Rigidbody torso, Vector3 pivoteMundo, float giro, float lateral)
        {
            var j = pieza.gameObject.AddComponent<ConfigurableJoint>();
            j.connectedBody = torso;
            j.anchor = pieza.transform.InverseTransformPoint(pivoteMundo);
            j.xMotion = ConfigurableJointMotion.Locked;
            j.yMotion = ConfigurableJointMotion.Locked;
            j.zMotion = ConfigurableJointMotion.Locked;
            j.angularXMotion = ConfigurableJointMotion.Limited;
            j.angularYMotion = ConfigurableJointMotion.Limited;
            j.angularZMotion = ConfigurableJointMotion.Limited;
            j.lowAngularXLimit = Limite(-giro);
            j.highAngularXLimit = Limite(giro);
            j.angularYLimit = Limite(lateral);
            j.angularZLimit = Limite(lateral);
            j.rotationDriveMode = RotationDriveMode.Slerp;
            j.projectionMode = JointProjectionMode.PositionAndRotation;
            j.enableCollision = false;
        }

        private static SoftJointLimit Limite(float grados)
        {
            var l = new SoftJointLimit();
            l.limit = grados;
            return l;
        }

        private static void Agarrable(GameObject go)
        {
            var g = go.AddComponent<XRGrabInteractable>();
            // Por velocidad y no cinematico: asi la pieza tira del resto del cuerpo
            // por las articulaciones y al soltarla sale lanzada.
            g.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            g.throwOnDetach = true;
            g.throwVelocityScale = 1.5f;
            g.useDynamicAttach = true;
        }

        private static Material Material(string nombre, Color color, float brillo)
        {
            var ruta = RutaMateriales + nombre + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", brillo);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AudioClip Clip(string archivo)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSfx + archivo);
            if (clip == null) Debug.LogWarning("[Parroquiano] Falta " + RutaSfx + archivo);
            return clip;
        }
    }
}
