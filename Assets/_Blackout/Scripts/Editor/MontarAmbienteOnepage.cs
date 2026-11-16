using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Blackout.Herramientas
{
    /// <summary>
    /// Acerca la escena al one page de la entrega 01 para la presentacion
    /// final: el mobiliario del dibujo, licores distintos, cerveza de grifo
    /// con espuma y el brillo calido en lo agarrable. Idempotente: cada menu
    /// borra lo que monto antes y lo rehace.
    /// </summary>
    public static class MontarAmbienteOnepage
    {
        private const string RutaMat = "Assets/_Blackout/Materials/";
        private const string RutaEtiquetas = "Assets/_Blackout/Textures/Etiquetas/";
        private const string RutaSfx = "Assets/_Blackout/Audio/SFX/";
        private const string Lit = "Universal Render Pipeline/Lit";

        [MenuItem("Blackout/Onepage/Montar todo")]
        public static void Todo()
        {
            Mobiliario();
            LicoresYCerveza();
            Vinilos();
            PapelCarteles();
            Tele();
            Voces();
            Nitidez();
            Dardos();
            Instrumentos();
            Pizarra();
            LuzBarra();
            Brillo();
            Debug.Log("[Onepage] Todo montado. Guarda la escena.");
        }

        // ---------- Mobiliario ----------

        // El dibujo del one page tiene una mesa redonda y una larga con sillas
        // en el centro, y taburetes en las mesas altas. Todo queda fuera del
        // carril de los dardos (x > 2.2, z < -1.6) y deja libre el paso de la
        // entrada al escenario por x = 0.
        private static readonly Vector3 SitioRedonda = new Vector3(-1.35f, 0f, -1.75f);
        private static readonly Vector3 SitioLarga = new Vector3(-0.75f, 0f, 0.75f);

        [MenuItem("Blackout/Onepage/Mobiliario")]
        public static void Mobiliario()
        {
            var viejo = GameObject.Find("Mobiliario_Centro");
            if (viejo != null) Undo.DestroyObjectImmediate(viejo);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name.StartsWith("Banqueta_Mesa_")) Undo.DestroyObjectImmediate(t.gameObject);

            var raiz = new GameObject("Mobiliario_Centro");
            Undo.RegisterCreatedObjectUndo(raiz, "Mobiliario");

            var madera = Material("Mat_Madera_Mueble", m =>
            {
                var fuente = AssetDatabase.LoadAssetAtPath<Material>(RutaMat + "Mat_Madera_Oscura.mat");
                if (fuente != null) m.CopyPropertiesFromMaterial(fuente);
                // Las caras de un cubo van de 0 a 1: con el tiling del suelo
                // las vetas saldrian como tablones enormes en una pata.
                m.SetTextureScale("_BaseMap", new Vector2(0.35f, 0.35f));
                m.SetColor("_BaseColor", new Color(0.85f, 0.62f, 0.42f));
            });
            var negro = AssetDatabase.LoadAssetAtPath<Material>(RutaMat + "Mat_Mesa_Negra.mat");
            var acero = AssetDatabase.LoadAssetAtPath<Material>(RutaMat + "Mat_Acero_Negro.mat");

            // Mesa redonda baja con tres sillas, cerca de la tele.
            MesaRedonda(raiz.transform, "Mesa_Redonda", SitioRedonda, negro, acero);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                var p = SitioRedonda + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.62f;
                Silla(raiz.transform, "Silla_Redonda_0" + (i + 1), p, SitioRedonda, madera);
            }

            // Mesa larga con cuatro sillas, frente al escenario.
            MesaLarga(raiz.transform, "Mesa_Larga", SitioLarga, madera);
            float[] xs = { -0.3f, 0.3f };
            int n = 1;
            foreach (float dz in new[] { -0.6f, 0.6f })
                foreach (float dx in xs)
                {
                    var p = SitioLarga + new Vector3(dx, 0f, dz);
                    Silla(raiz.transform, "Silla_Larga_0" + n++, p, new Vector3(p.x, 0f, SitioLarga.z), madera);
                }

            foreach (var t in raiz.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            Taburetes();
            EditorSceneManager.MarkSceneDirty(raiz.scene);
            Debug.Log("[Onepage] Mobiliario montado.");
        }

        private static void Silla(Transform padre, string nombre, Vector3 sitio, Vector3 miraA, Material madera)
        {
            var silla = new GameObject(nombre);
            silla.transform.SetParent(padre, false);
            silla.transform.position = sitio;
            var dir = miraA - sitio; dir.y = 0f;
            silla.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            Pieza(silla.transform, "Asiento", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.04f, 0.42f), madera);
            int k = 1;
            foreach (float x in new[] { -0.18f, 0.18f })
                foreach (float z in new[] { -0.18f, 0.18f })
                    Pieza(silla.transform, "Pata_0" + k++, PrimitiveType.Cube, new Vector3(x, 0.215f, z), new Vector3(0.035f, 0.43f, 0.035f), madera);
            foreach (float x in new[] { -0.18f, 0.18f })
                Pieza(silla.transform, "Poste" + (x < 0 ? "_Izq" : "_Der"), PrimitiveType.Cube, new Vector3(x, 0.68f, -0.19f), new Vector3(0.035f, 0.46f, 0.035f), madera);
            Pieza(silla.transform, "Respaldo", PrimitiveType.Cube, new Vector3(0f, 0.81f, -0.19f), new Vector3(0.36f, 0.14f, 0.025f), madera);
            Pieza(silla.transform, "Travesano", PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.19f), new Vector3(0.36f, 0.035f, 0.025f), madera);

            Caja(silla, new Vector3(0f, 0.235f, 0f), new Vector3(0.44f, 0.47f, 0.44f));
            Caja(silla, new Vector3(0f, 0.69f, -0.19f), new Vector3(0.42f, 0.44f, 0.05f));
        }

        private static void MesaRedonda(Transform padre, string nombre, Vector3 sitio, Material tablero, Material acero)
        {
            var mesa = new GameObject(nombre);
            mesa.transform.SetParent(padre, false);
            mesa.transform.position = sitio;
            Pieza(mesa.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f), new Vector3(0.45f, 0.015f, 0.45f), acero);
            Pieza(mesa.transform, "Columna", PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), new Vector3(0.07f, 0.36f, 0.07f), acero);
            Pieza(mesa.transform, "Tablero", PrimitiveType.Cylinder, new Vector3(0f, 0.745f, 0f), new Vector3(0.75f, 0.02f, 0.75f), tablero);
            Pieza(mesa.transform, "Canto", PrimitiveType.Cylinder, new Vector3(0f, 0.745f, 0f), new Vector3(0.78f, 0.014f, 0.78f), acero);
            Caja(mesa, new Vector3(0f, 0.745f, 0f), new Vector3(0.75f, 0.04f, 0.75f));
            Caja(mesa, new Vector3(0f, 0.37f, 0f), new Vector3(0.1f, 0.72f, 0.1f));
        }

        private static void MesaLarga(Transform padre, string nombre, Vector3 sitio, Material madera)
        {
            var mesa = new GameObject(nombre);
            mesa.transform.SetParent(padre, false);
            mesa.transform.position = sitio;
            Pieza(mesa.transform, "Tablero", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(1.3f, 0.05f, 0.75f), madera);
            int k = 1;
            foreach (float x in new[] { -0.58f, 0.58f })
                foreach (float z in new[] { -0.3f, 0.3f })
                    Pieza(mesa.transform, "Pata_0" + k++, PrimitiveType.Cube, new Vector3(x, 0.36f, z), new Vector3(0.06f, 0.72f, 0.06f), madera);
            Caja(mesa, new Vector3(0f, 0.75f, 0f), new Vector3(1.3f, 0.05f, 0.75f));
            Caja(mesa, new Vector3(0f, 0.36f, 0f), new Vector3(1.22f, 0.72f, 0.66f));
        }

        /// <summary>Dos banquetas por mesa alta, copiadas de las de la barra.</summary>
        private static void Taburetes()
        {
            var modelo = GameObject.Find("Banqueta_01");
            if (modelo == null) { Debug.LogWarning("[Onepage] No encuentro Banqueta_01 para copiar."); return; }
            var fuente = PrefabUtility.GetCorrespondingObjectFromSource(modelo);
            var padre = GameObject.Find("Mesas_Local");

            var sitios = new[]
            {
                new Vector3(2.0f, 0f, -0.15f), new Vector3(1.3f, 0f, -1.0f),   // Mesa_01
                new Vector3(2.4f, 0f, 1.4f), new Vector3(3.25f, 0f, 0.62f),    // Mesa_02
                new Vector3(0.5f, 0f, 1.85f), new Vector3(1.6f, 0f, 1.55f),    // Mesa_03
            };
            float[] giros = { 20f, 110f, 250f, 75f, 160f, 300f };
            for (int i = 0; i < sitios.Length; i++)
            {
                var go = fuente != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(fuente, modelo.scene)
                    : Object.Instantiate(modelo);
                Undo.RegisterCreatedObjectUndo(go, "Taburete");
                go.name = "Banqueta_Mesa_0" + (i + 1);
                if (padre != null) go.transform.SetParent(padre.transform, true);
                go.transform.localScale = modelo.transform.lossyScale;
                go.transform.SetPositionAndRotation(sitios[i], Quaternion.Euler(0f, giros[i], 0f));
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }

            // Las banquetas venian sin collider: el Chancho y las botellas las
            // atravesaban. Una caja por banqueta, tambien en las de la barra.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("Banqueta_") || t.GetComponent<BoxCollider>() != null) continue;
                if (t.GetComponentInChildren<Renderer>() == null) continue;
                var rot = t.rotation;
                t.rotation = Quaternion.identity;
                var rs = t.GetComponentsInChildren<Renderer>();
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                t.rotation = rot;
                var caja = Undo.AddComponent<BoxCollider>(t.gameObject);
                var s = t.lossyScale;
                var c = b.center - t.position;
                caja.center = new Vector3(c.x / s.x, c.y / s.y, c.z / s.z);
                caja.size = new Vector3(b.size.x / s.x, b.size.y / s.y, b.size.z / s.z);
            }
        }

        // ---------- Licores y cerveza ----------

        private struct Licor
        {
            public string bebida, etiqueta;
            public Color liquido, cristal;
            public float graduacion;
            public Licor(string bebida, string etiqueta, Color liquido, Color cristal, float graduacion)
            {
                this.bebida = bebida; this.etiqueta = etiqueta; this.liquido = liquido;
                this.cristal = cristal; this.graduacion = graduacion;
            }
        }

        // Marcas inventadas: el repo es publico y no se usan marcas reales.
        private static readonly Licor[] Licores =
        {
            new Licor("Ron Tres Acordes", "Ron", new Color(0.45f, 0.18f, 0.04f), new Color(0.35f, 0.2f, 0.08f, 0.55f), 1f),
            new Licor("Whisky El Ampli", "Whisky", new Color(0.55f, 0.28f, 0.06f), new Color(0.85f, 0.85f, 0.8f, 0.3f), 1.1f),
            new Licor("Ginebra Acople", "Ginebra", new Color(0.5f, 0.58f, 0.66f), new Color(0.55f, 0.75f, 0.9f, 0.35f), 1f),
            new Licor("Licor Verde Bis", "Licor", new Color(0.2f, 0.5f, 0.08f), new Color(0.1f, 0.35f, 0.12f, 0.55f), 0.8f),
        };

        [MenuItem("Blackout/Onepage/Licores y cerveza")]
        public static void LicoresYCerveza()
        {
            var liquido = Material("Mat_Liquido", m => m.SetFloat("_Smoothness", 0.85f));
            var espuma = Material("Mat_Espuma", m =>
            {
                m.SetColor("_BaseColor", new Color(0.96f, 0.92f, 0.82f));
                m.SetFloat("_Smoothness", 0.15f);
            });
            var chorroClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSfx + "AMB_Chorro.wav");
            var tintineo = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSfx + "SFX_Servir.wav");

            // Vidrio de verdad: con el vaso opaco el liquido solo se veia desde arriba.
            var vidrio = Material("Mat_Vidrio", m =>
            {
                m.SetColor("_BaseColor", new Color(0.8f, 0.86f, 0.86f, 0.22f));
                m.SetFloat("_Smoothness", 0.7f);
                Transparente(m);
            });

            var botellas = new List<Bottle>(Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            botellas.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < botellas.Count; i++)
                MontarBotella(botellas[i], Licores[i % Licores.Length], liquido, chorroClip);

            foreach (var vaso in Object.FindObjectsByType<Glass>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                MontarVaso(vaso, vidrio, espuma, tintineo);

            MontarGrifos(liquido, chorroClip);
            Debug.Log("[Onepage] " + botellas.Count + " licores y los grifos montados.");
        }

        private static void MontarBotella(Bottle botella, Licor licor, Material liquido, AudioClip chorroClip)
        {
            var t = botella.transform;
            var cristal = Material("Mat_Botella_" + licor.etiqueta, m =>
            {
                m.SetColor("_BaseColor", licor.cristal);
                m.SetFloat("_Smoothness", 0.7f);
                Transparente(m);
            });
            var papel = Material("Mat_Etiqueta_" + licor.etiqueta, m =>
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaEtiquetas + "Etiqueta_" + licor.etiqueta + ".png"));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.25f);
            });

            foreach (var nombre in new[] { "Cuerpo", "Cuello" })
            {
                var parte = t.Find(nombre);
                if (parte == null) continue;
                var r = parte.GetComponent<Renderer>();
                Undo.RecordObject(r, "Botella");
                r.sharedMaterial = cristal;
            }

            Borrar(t, "Liquido"); Borrar(t, "Etiqueta"); Borrar(t, "Chorro");
            var dentro = Pieza(t, "Liquido", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f), new Vector3(0.068f, 0.11f, 0.068f), liquido);
            // La textura lleva la etiqueta dos veces, una por cada media vuelta.
            // Girada 90 grados, una copia queda de cara a la barra y la union
            // entre las dos queda de lado.
            var etiqueta = Pieza(t, "Etiqueta", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f), new Vector3(0.0815f, 0.04f, 0.0815f), papel);
            etiqueta.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var chorro = Pieza(t, "Chorro", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.008f, 0.1f, 0.008f), liquido);
            chorro.gameObject.SetActive(false);

            var so = new SerializedObject(botella);
            so.FindProperty("bebida").stringValue = licor.bebida;
            so.FindProperty("colorLiquido").colorValue = licor.liquido;
            so.FindProperty("graduacion").floatValue = licor.graduacion;
            so.FindProperty("liquidoVisual").objectReferenceValue = dentro;
            so.FindProperty("chorro").objectReferenceValue = chorro.gameObject;
            if (chorroClip != null) so.FindProperty("clipServir").objectReferenceValue = chorroClip;
            so.ApplyModifiedProperties();
            Undo.RecordObject(botella.gameObject, "Botella");
        }

        private static void MontarVaso(Glass vaso, Material vidrio, Material espuma, AudioClip brindis)
        {
            var t = vaso.transform;
            var cuerpo = t.Find("Cuerpo");
            if (cuerpo != null)
            {
                var r = cuerpo.GetComponent<Renderer>();
                r.sharedMaterial = vidrio;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            Borrar(t, "Espuma");
            var capa = Pieza(t, "Espuma", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.062f, 0.005f, 0.062f), espuma);
            capa.gameObject.SetActive(false);

            var so = new SerializedObject(vaso);
            so.FindProperty("espumaVisual").objectReferenceValue = capa;
            if (brindis != null) so.FindProperty("clipBrindis").objectReferenceValue = brindis;
            so.ApplyModifiedProperties();
        }

        private struct Cerveza
        {
            public string nombre; public Color color, pomo; public float graduacion, espuma;
            public Cerveza(string nombre, Color color, Color pomo, float graduacion, float espuma)
            { this.nombre = nombre; this.color = color; this.pomo = pomo; this.graduacion = graduacion; this.espuma = espuma; }
        }

        private static readonly Cerveza[] Cervezas =
        {
            new Cerveza("Rubia", new Color(0.72f, 0.42f, 0.06f), new Color(0.9f, 0.7f, 0.1f), 0.35f, 1.6f),
            new Cerveza("Roja", new Color(0.45f, 0.12f, 0.04f), new Color(0.7f, 0.1f, 0.08f), 0.4f, 1.4f),
            new Cerveza("Negra", new Color(0.1f, 0.05f, 0.03f), new Color(0.05f, 0.04f, 0.04f), 0.45f, 2f),
        };

        /// <summary>
        /// Los tres grifos eran decorado. Cada maneta pasa a colgar de un
        /// pivote en su base para poder tirar de ella, y lleva un pomo del
        /// color de su cerveza para distinguirlas de un vistazo.
        /// </summary>
        private static void MontarGrifos(Material liquido, AudioClip chorroClip)
        {
            var grifos = GameObject.Find("Grifos_Cerveza");
            if (grifos == null) { Debug.LogWarning("[Onepage] No hay Grifos_Cerveza."); return; }
            var g = grifos.transform;

            for (int i = 0; i < Cervezas.Length; i++)
            {
                string n = "0" + (i + 1);
                var cerveza = Cervezas[i];
                var pivote = g.Find("Pivote_Maneta_" + n);
                var maneta = pivote != null ? pivote.Find("Maneta_" + n) : g.Find("Maneta_" + n);
                var cano = g.Find("Cano_" + n);
                if (maneta == null || cano == null) { Debug.LogWarning("[Onepage] Falta la maneta o el caño " + n); continue; }

                if (pivote == null)
                {
                    pivote = new GameObject("Pivote_Maneta_" + n).transform;
                    Undo.RegisterCreatedObjectUndo(pivote.gameObject, "Grifo");
                    pivote.SetParent(g, false);
                    pivote.position = maneta.position - maneta.up * maneta.lossyScale.y;
                    pivote.rotation = Quaternion.identity;
                    Undo.SetTransformParent(maneta, pivote, "Grifo");
                }

                // Pomo arriba de la maneta. Cuelga del pivote y no de la maneta,
                // que tiene escala no uniforme y lo deformaria.
                Borrar(pivote, "Pomo_" + n);
                var pomoMat = Material("Mat_Pomo_" + cerveza.nombre, m =>
                {
                    m.SetColor("_BaseColor", cerveza.pomo);
                    m.SetFloat("_Smoothness", 0.8f);
                });
                var pomo = Pieza(pivote, "Pomo_" + n, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.036f, pomoMat);
                pomo.position = maneta.position + maneta.up * maneta.lossyScale.y;

                // Salida en la punta baja del caño.
                var a = cano.position + cano.up * cano.lossyScale.y;
                var b = cano.position - cano.up * cano.lossyScale.y;
                Borrar(g, "Salida_" + n);
                var salida = new GameObject("Salida_" + n).transform;
                Undo.RegisterCreatedObjectUndo(salida.gameObject, "Grifo");
                salida.SetParent(g, false);
                salida.position = (a.y < b.y ? a : b) + Vector3.down * 0.012f;

                Borrar(g, "Chorro_" + n);
                var chorro = Pieza(g, "Chorro_" + n, PrimitiveType.Cylinder, salida.position, new Vector3(0.012f, 0.1f, 0.012f), liquido);
                chorro.gameObject.SetActive(false);

                var col = maneta.GetComponent<CapsuleCollider>();
                if (col == null) col = Undo.AddComponent<CapsuleCollider>(maneta.gameObject);
                col.radius = 1.4f;
                col.height = 2.6f;

                if (maneta.GetComponent<XRSimpleInteractable>() == null) Undo.AddComponent<XRSimpleInteractable>(maneta.gameObject);
                var audio = maneta.GetComponent<AudioSource>();
                if (audio == null) audio = Undo.AddComponent<AudioSource>(maneta.gameObject);
                audio.playOnAwake = false;
                audio.spatialBlend = 1f;
                audio.volume = 0.6f;
                var grifo = maneta.GetComponent<Grifo>();
                if (grifo == null) grifo = Undo.AddComponent<Grifo>(maneta.gameObject);

                var so = new SerializedObject(grifo);
                so.FindProperty("cerveza").stringValue = cerveza.nombre;
                so.FindProperty("color").colorValue = cerveza.color;
                so.FindProperty("graduacion").floatValue = cerveza.graduacion;
                so.FindProperty("espuma").floatValue = cerveza.espuma;
                so.FindProperty("pivote").objectReferenceValue = pivote;
                so.FindProperty("salida").objectReferenceValue = salida;
                so.FindProperty("chorro").objectReferenceValue = chorro.gameObject;
                so.FindProperty("sfx").objectReferenceValue = audio;
                so.FindProperty("clipChorro").objectReferenceValue = chorroClip;
                so.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(grifos.scene);
        }

        // ---------- Vinilos ----------

        private static readonly Color[] Galletas =
        {
            new Color(0.75f, 0.12f, 0.1f), new Color(0.9f, 0.75f, 0.2f), new Color(0.15f, 0.35f, 0.7f),
            new Color(0.9f, 0.88f, 0.8f), new Color(0.25f, 0.55f, 0.25f),
        };

        /// <summary>
        /// Dos discos tenian el material vacio (se veian magenta) y el resto
        /// usaba el Lit gris por defecto del paquete. Todos pasan a vinilo
        /// negro brillante con una galleta de color en el centro.
        /// </summary>
        [MenuItem("Blackout/Onepage/Vinilos")]
        public static void Vinilos()
        {
            var negro = Material("Mat_Vinilo", m =>
            {
                m.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.035f));
                m.SetFloat("_Smoothness", 0.85f);
            });
            var discos = new List<Vinyl>(Object.FindObjectsByType<Vinyl>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            discos.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < discos.Count; i++)
            {
                var t = discos[i].transform;
                var disco = t.Find("Disco");
                if (disco == null) continue;
                var r = disco.GetComponent<Renderer>();
                Undo.RecordObject(r, "Vinilo");
                r.sharedMaterial = negro;

                var color = Galletas[i % Galletas.Length];
                var papel = Material("Mat_Galleta_0" + (i + 1), m =>
                {
                    m.SetColor("_BaseColor", color);
                    m.SetFloat("_Smoothness", 0.3f);
                });
                Borrar(t, "Galleta");
                var s = disco.localScale;
                Pieza(t, "Galleta", PrimitiveType.Cylinder, disco.localPosition,
                    new Vector3(s.x * 0.33f, s.y * 1.08f, s.z * 0.33f), papel);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void PapelCarteles()
        {
            var papel = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSfx + "SFX_Papel.wav");
            foreach (var slot in Object.FindObjectsByType<PosterSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(slot);
                so.FindProperty("clipPapel").objectReferenceValue = papel;
                so.ApplyModifiedProperties();
            }
        }

        // ---------- Tele ----------

        /// <summary>
        /// Los canales eran cartas de ajuste en H.264, que el editor de Linux
        /// no importa. Ahora son WebM VP8, que Unity lee en todas las
        /// plataformas: un concierto de dominio publico (St. Louis Blues, 1929),
        /// un canal propio con la musica de la casa y la estatica de siempre.
        /// </summary>
        [MenuItem("Blackout/Onepage/Canales de la tele")]
        public static void Tele()
        {
            var tv = Object.FindAnyObjectByType<Television>(FindObjectsInactive.Include);
            if (tv == null) { Debug.LogWarning("[Onepage] No hay Television."); return; }
            string[] nombres = { "Canal_01_Concierto_1929.webm", "Canal_02_Ensayo.webm", "Canal_03_Sin_Senal.webm" };

            var so = new SerializedObject(tv);
            var lista = so.FindProperty("canales");
            lista.arraySize = nombres.Length;
            for (int i = 0; i < nombres.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/_Blackout/Video/" + nombres[i]);
            // La pantalla emitia un color plano sin textura: con la tele
            // encendida se veia un rectangulo blanco y el video quedaba tapado.
            // Con el video como mapa de emision, brilla la imagen y no el cristal.
            so.FindProperty("emisionEncendida").floatValue = 1.3f;
            so.ApplyModifiedProperties();
            var pantalla = so.FindProperty("pantalla").objectReferenceValue as Renderer;
            if (pantalla != null && pantalla.sharedMaterial != null)
            {
                var m = pantalla.sharedMaterial;
                m.SetTexture("_EmissionMap", m.GetTexture("_BaseMap"));
                EditorUtility.SetDirty(m);
            }

            // El sonido del canal sale de la tele y se apaga con la distancia:
            // de cerca se oye el concierto, desde la pista manda el vinilo.
            var vp = tv.GetComponent<UnityEngine.Video.VideoPlayer>();
            var fuente = vp != null ? vp.GetTargetAudioSource(0) : null;
            if (fuente != null)
            {
                Undo.RecordObject(fuente, "Tele");
                fuente.spatialBlend = 1f;
                fuente.volume = 0.55f;
                fuente.rolloffMode = AudioRolloffMode.Linear;
                fuente.minDistance = 0.8f;
                fuente.maxDistance = 5f;
            }
            Mando(tv);
            EditorSceneManager.MarkSceneDirty(tv.gameObject.scene);
        }

        /// <summary>Mando a distancia sobre la barra, al lado de la tele.</summary>
        private static void Mando(Television tv)
        {
            var viejo = GameObject.Find("Mando_TV");
            if (viejo != null) Undo.DestroyObjectImmediate(viejo);

            var plastico = Material("Mat_Plastico_Negro", m =>
            {
                m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.055f));
                m.SetFloat("_Smoothness", 0.45f);
            });
            var goma = Material("Mat_Boton_Goma", m =>
            {
                m.SetColor("_BaseColor", new Color(0.35f, 0.35f, 0.37f));
                m.SetFloat("_Smoothness", 0.2f);
            });
            var rojo = Material("Mat_Piloto", m =>
            {
                m.SetColor("_BaseColor", new Color(0.5f, 0.05f, 0.04f));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });

            var padre = tv.transform.parent;
            var go = new GameObject("Mando_TV");
            Undo.RegisterCreatedObjectUndo(go, "Mando");
            if (padre != null) go.transform.SetParent(padre, true);
            go.transform.SetPositionAndRotation(new Vector3(-3.93f, 1.115f, -1.45f), Quaternion.Euler(0f, 20f, 0f));

            Pieza(go.transform, "Cuerpo", PrimitiveType.Cube, Vector3.zero, new Vector3(0.05f, 0.02f, 0.17f), plastico);
            Pieza(go.transform, "Boton_Encendido", PrimitiveType.Cube, new Vector3(0f, 0.012f, 0.06f), new Vector3(0.016f, 0.006f, 0.016f), rojo);
            int k = 1;
            for (int fila = 0; fila < 4; fila++)
                for (int col = -1; col <= 1; col++)
                    Pieza(go.transform, "Boton_0" + k++, PrimitiveType.Cube,
                        new Vector3(col * 0.014f, 0.011f, 0.03f - fila * 0.018f), new Vector3(0.009f, 0.005f, 0.009f), goma);
            var piloto = Pieza(go.transform, "Piloto", PrimitiveType.Cube, new Vector3(0f, 0.004f, 0.086f), new Vector3(0.008f, 0.006f, 0.004f), rojo);

            var caja = go.AddComponent<BoxCollider>();
            caja.center = new Vector3(0f, 0.002f, 0f);
            caja.size = new Vector3(0.05f, 0.026f, 0.17f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.15f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var agarre = go.AddComponent<XRGrabInteractable>();
            agarre.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            agarre.useDynamicAttach = true;
            var mando = go.AddComponent<MandoTele>();

            var so = new SerializedObject(mando);
            so.FindProperty("tele").objectReferenceValue = tv;
            so.FindProperty("piloto").objectReferenceValue = piloto.GetComponent<Renderer>();
            so.ApplyModifiedProperties();
        }

        // ---------- Voces ----------

        /// <summary>
        /// La capa narrativa explicita del documento: la voz del dueno al
        /// llegar y al abrir, y una anecdota por cartel. Son locuciones de
        /// prueba hechas con Piper (voz es_MX ald, dataset Unlicense); se
        /// pueden cambiar por grabaciones propias con el mismo nombre de
        /// archivo y no hay que tocar nada mas.
        /// </summary>
        [MenuItem("Blackout/Onepage/Voces")]
        public static void Voces()
        {
            const string ruta = "Assets/_Blackout/Audio/Voz/";
            var bsm = Object.FindAnyObjectByType<BarStateManager>(FindObjectsInactive.Include);
            if (bsm != null)
            {
                var so = new SerializedObject(bsm);
                so.FindProperty("vozEntrada").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta + "Voz_Entrada.wav");
                so.FindProperty("vozCierre").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta + "Voz_Cierre.wav");
                so.ApplyModifiedProperties();
            }

            int n = 0;
            foreach (var cartel in Object.FindObjectsByType<Poster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta + "Anecdota_" + cartel.Banda + ".wav");
                if (clip == null) { Debug.LogWarning("[Onepage] Sin anecdota para " + cartel.Banda); continue; }
                var so = new SerializedObject(cartel);
                so.FindProperty("anecdota").objectReferenceValue = clip;
                so.ApplyModifiedProperties();
                n++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Onepage] Voz del dueño y " + n + " anecdotas asignadas.");
        }

        // ---------- Imagen ----------

        /// <summary>
        /// Contra lo "granulado": el suelo y los muros tenian filtrado
        /// anisotropico 1, y en VR casi todo el suelo se ve en angulo rasante,
        /// donde la textura se emborrona y titila. Con 4 se ve nitida a coste
        /// bajo. El grano de pelicula se quito en Montar post-procesado.
        /// </summary>
        [MenuItem("Blackout/Onepage/Nitidez de texturas")]
        public static void Nitidez()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Blackout/Textures/PBR" }))
            {
                var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (imp == null || imp.anisoLevel >= 4) continue;
                imp.anisoLevel = 4;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
                n++;
            }
            Debug.Log("[Onepage] Filtrado anisotropico 4 en " + n + " texturas.");
        }

        // ---------- Dardos ----------

        /// <summary>
        /// El dardo se agarraba por donde cayera la mano y quedaba en la
        /// orientacion en que estuviera. Con un punto de agarre en el barril,
        /// la punta queda siempre hacia delante del mando, como se sostiene un
        /// dardo de verdad. El prefab y las copias sueltas de la escena.
        /// </summary>
        [MenuItem("Blackout/Onepage/Agarre de los dardos")]
        public static void Dardos()
        {
            const string rutaPrefab = "Assets/_Blackout/Prefabs/Dardo.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab) != null)
            {
                var raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
                PonerAgarre(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            foreach (var dardo in Object.FindObjectsByType<Dart>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!PrefabUtility.IsPartOfPrefabInstance(dardo)) PonerAgarre(dardo.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void PonerAgarre(GameObject dardo)
        {
            var agarre = dardo.GetComponent<XRGrabInteractable>();
            if (agarre == null) return;
            var punto = dardo.transform.Find("Agarre");
            if (punto == null)
            {
                punto = new GameObject("Agarre").transform;
                punto.SetParent(dardo.transform, false);
            }
            // El forward del punto de agarre se alinea con el del mando, y la
            // punta del dardo va por el eje Y local: girar -90 en X los junta.
            punto.localPosition = new Vector3(0f, 0.005f, 0f);
            punto.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            agarre.attachTransform = punto;
            agarre.useDynamicAttach = false;
            EditorUtility.SetDirty(agarre);
        }

        // ---------- Instrumentos ----------

        private const string RutaInstr = "Assets/_Blackout/Audio/Instrumentos/";

        /// <summary>
        /// La bateria sonaba al "agarrarla" con un tambor de interfaz para
        /// todo. Ahora cada pieza tiene su sonido sintetizado y suena al
        /// pegarle con la mano o con una baqueta. Las guitarras pasan de una
        /// nota suelta a cuatro acordes segun el traste.
        /// </summary>
        [MenuItem("Blackout/Onepage/Instrumentos")]
        public static void Instrumentos()
        {
            // pieza, sonido, tono minimo, tono maximo
            var bateria = new[]
            {
                new object[] { "BigKick", "Bombo", 0.97f, 1.03f },
                new object[] { "Bigsnare", "Caja", 0.97f, 1.04f },
                new object[] { "Reggitom", "Tom_Agudo", 0.98f, 1.03f },
                new object[] { "BigTOM", "Tom_Agudo", 0.86f, 0.9f },
                new object[] { "BigFL", "Tom_Grave", 0.97f, 1.02f },
                new object[] { "BigFL_1", "Tom_Grave", 0.84f, 0.88f },
                new object[] { "HHT", "Charles", 0.96f, 1.05f },
                new object[] { "Crash", "Crash", 0.97f, 1.03f },
                new object[] { "Crash_1", "China", 0.97f, 1.03f },
                new object[] { "Ride", "Ride", 0.97f, 1.03f },
            };
            foreach (var b in bateria)
            {
                var go = GameObject.Find((string)b[0]);
                var si = go != null ? go.GetComponent<StageInstrument>() : null;
                if (si == null) { Debug.LogWarning("[Onepage] Falta la pieza " + b[0]); continue; }
                var so = new SerializedObject(si);
                so.FindProperty("modo").enumValueIndex = (int)StageInstrument.Modo.Percutir;
                var notas = so.FindProperty("notas");
                notas.arraySize = 1;
                notas.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaInstr + b[1] + ".wav");
                so.FindProperty("rangoTono").vector2Value = new Vector2((float)b[2], (float)b[3]);
                so.FindProperty("reboteSegundos").floatValue = 0.08f;
                so.ApplyModifiedProperties();
            }

            Baquetas();
            Guitarra("Guitarra_Electrica", "Guitarra_", "5");
            Guitarra("Bajo", "Bajo_", "");
            Guitarra("Guitarra_Acustica", "Acustica_", "");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Onepage] Instrumentos montados.");
        }

        private static void Guitarra(string nombre, string prefijo, string sufijo)
        {
            var go = GameObject.Find(nombre);
            var si = go != null ? go.GetComponent<StageInstrument>() : null;
            if (si == null) { Debug.LogWarning("[Onepage] Falta " + nombre); return; }

            // El mastil es el eje mas largo del modelo. De sus dos extremos, el
            // del cuerpo es el que tiene mas malla cerca (el centroide de los
            // vertices cae de su lado). La cejuela queda al 78 % hacia la pala
            // y la union con el cuerpo al 45 %, medidas de una guitarra real.
            var verts = new List<Vector3>();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null)
                    foreach (var v in mf.sharedMesh.vertices) verts.Add(go.transform.InverseTransformPoint(mf.transform.TransformPoint(v)));
            if (verts.Count == 0) return;
            var b = new Bounds(verts[0], Vector3.zero);
            Vector3 centroide = Vector3.zero;
            foreach (var v in verts) { b.Encapsulate(v); centroide += v; }
            centroide /= verts.Count;
            Vector3 tam = Vector3.Scale(b.size, go.transform.lossyScale);
            int eje = tam.x > tam.y ? (tam.x > tam.z ? 0 : 2) : (tam.y > tam.z ? 1 : 2);
            Vector3 dir = Vector3.zero; dir[eje] = 1f;
            Vector3 extA = b.center - dir * b.extents[eje], extB = b.center + dir * b.extents[eje];
            bool cuerpoEnA = Vector3.Distance(centroide, extA) < Vector3.Distance(centroide, extB);
            Vector3 cuerpo = cuerpoEnA ? extA : extB, pala = cuerpoEnA ? extB : extA;

            var inicio = Marca(go.transform, "Mastil_Inicio", Vector3.Lerp(cuerpo, pala, 0.78f));
            var fin = Marca(go.transform, "Mastil_Fin", Vector3.Lerp(cuerpo, pala, 0.45f));

            var so = new SerializedObject(si);
            var acordes = so.FindProperty("acordes");
            string[] notas = { "Mi", "Sol", "La", "Do" };
            acordes.arraySize = notas.Length;
            for (int i = 0; i < notas.Length; i++)
                acordes.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaInstr + prefijo + notas[i] + sufijo + ".wav");
            so.FindProperty("mastilInicio").objectReferenceValue = inicio;
            so.FindProperty("mastilFin").objectReferenceValue = fin;
            so.FindProperty("reboteSegundos").floatValue = 0.15f;
            so.ApplyModifiedProperties();
        }

        private static Transform Marca(Transform padre, string nombre, Vector3 local)
        {
            var t = padre.Find(nombre);
            if (t == null)
            {
                t = new GameObject(nombre).transform;
                Undo.RegisterCreatedObjectUndo(t.gameObject, nombre);
                t.SetParent(padre, false);
            }
            t.localPosition = local;
            t.localRotation = Quaternion.identity;
            return t;
        }

        /// <summary>
        /// Habia una sola baqueta. Se duplica para tocar con las dos manos y
        /// cada una lleva en la punta un GolpeMano: el mismo trigger de las
        /// manos, que mide la velocidad y hace sonar la pieza que toca.
        /// </summary>
        private static void Baquetas()
        {
            var original = GameObject.Find("Baqueta");
            if (original == null) { Debug.LogWarning("[Onepage] No hay Baqueta."); return; }
            var segunda = GameObject.Find("Baqueta_2");
            if (segunda == null)
            {
                segunda = Object.Instantiate(original, original.transform.parent);
                segunda.name = "Baqueta_2";
                Undo.RegisterCreatedObjectUndo(segunda, "Baqueta");
                segunda.transform.position = original.transform.position + original.transform.right * 0.06f;
                segunda.transform.rotation = original.transform.rotation;
            }
            foreach (var baq in new[] { original, segunda })
            {
                Borrar(baq.transform, "Punta_Golpe");
                var rs = baq.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                // La punta es el extremo del eje largo mas alejado del punto de agarre.
                var verts = new List<Vector3>();
                foreach (var mf in baq.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null)
                        foreach (var v in mf.sharedMesh.vertices) verts.Add(baq.transform.InverseTransformPoint(mf.transform.TransformPoint(v)));
                var b = new Bounds(verts[0], Vector3.zero);
                foreach (var v in verts) b.Encapsulate(v);
                Vector3 tam = Vector3.Scale(b.size, baq.transform.lossyScale);
                int eje = tam.x > tam.y ? (tam.x > tam.z ? 0 : 2) : (tam.y > tam.z ? 1 : 2);
                Vector3 dir = Vector3.zero; dir[eje] = 1f;
                var agarre = baq.GetComponent<XRGrabInteractable>();
                Vector3 asa = agarre != null && agarre.attachTransform != null
                    ? baq.transform.InverseTransformPoint(agarre.attachTransform.position) : b.center - dir * b.extents[eje];
                Vector3 extA = b.center - dir * b.extents[eje], extB = b.center + dir * b.extents[eje];
                Vector3 punta = Vector3.Distance(asa, extA) > Vector3.Distance(asa, extB) ? extA : extB;

                var go = new GameObject("Punta_Golpe");
                Undo.RegisterCreatedObjectUndo(go, "Punta");
                go.transform.SetParent(baq.transform, false);
                go.transform.localPosition = Vector3.Lerp(b.center, punta, 0.9f);
                var esfera = go.AddComponent<SphereCollider>();
                esfera.isTrigger = true;
                float escala = Mathf.Max(baq.transform.lossyScale.x, Mathf.Max(baq.transform.lossyScale.y, baq.transform.lossyScale.z));
                esfera.radius = 0.025f / Mathf.Max(0.0001f, escala);
                go.AddComponent<GolpeMano>();
            }
        }

        // ---------- Pizarra y luz ----------

        /// <summary>
        /// Pizarra de caballete en la acera, junto a la reja: lo primero que
        /// ve quien se pone las gafas. Explica los controles sin sacar a nadie
        /// del bar, con la tiza del mismo bar.
        /// </summary>
        [MenuItem("Blackout/Onepage/Pizarra de como se juega")]
        public static void Pizarra()
        {
            var vieja = GameObject.Find("Pizarra_Como_Se_Juega");
            if (vieja != null) Undo.DestroyObjectImmediate(vieja);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Blackout/Textures/Pizarra/Pizarra_Como_Se_Juega.png");
            var tiza = Material("Mat_Pizarra_Como_Se_Juega", m =>
            {
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.1f);
                // Es de noche y la calle esta oscura: la tiza brilla un poco
                // para poder leerla sin gastar una luz mas.
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", new Color(0.55f, 0.55f, 0.52f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            var madera = AssetDatabase.LoadAssetAtPath<Material>(RutaMat + "Mat_Madera_Mueble.mat");

            var raiz = new GameObject("Pizarra_Como_Se_Juega");
            Undo.RegisterCreatedObjectUndo(raiz, "Pizarra");
            // En la acera, a la izquierda de la reja, de cara a quien llega.
            var sitio = new Vector3(-1.95f, 0f, -4.95f);
            var llegada = new Vector3(0.5f, 0f, -7.6f);
            raiz.transform.position = sitio;
            raiz.transform.rotation = Quaternion.LookRotation(llegada - sitio, Vector3.up);

            // Hoja delantera inclinada como un caballete; la textura en un
            // quad que mira al frente (el quad se ve por su cara -Z).
            var hoja = new GameObject("Hoja").transform;
            hoja.SetParent(raiz.transform, false);
            hoja.localPosition = new Vector3(0f, 0f, 0.21f);
            hoja.localRotation = Quaternion.Euler(-11f, 0f, 0f);
            Pieza(hoja, "Tablero", PrimitiveType.Cube, new Vector3(0f, 0.55f, -0.012f), new Vector3(0.64f, 0.86f, 0.02f), madera);
            var cara = Pieza(hoja, "Cara", PrimitiveType.Quad, new Vector3(0f, 0.55f, 0.0f), new Vector3(0.6f, 0.8f, 1f), tiza);
            cara.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (float x in new[] { -0.3f, 0.3f })
                Pieza(hoja, "Pata", PrimitiveType.Cube, new Vector3(x, 0.5f, -0.03f), new Vector3(0.04f, 1.0f, 0.03f), madera);

            var trasera = new GameObject("Trasera").transform;
            trasera.SetParent(raiz.transform, false);
            trasera.localPosition = new Vector3(0f, 0f, -0.19f);
            trasera.localRotation = Quaternion.Euler(11f, 0f, 0f);
            Pieza(trasera, "Tablero", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.012f), new Vector3(0.64f, 0.86f, 0.02f), madera);

            var caja = raiz.AddComponent<BoxCollider>();
            caja.center = new Vector3(0f, 0.5f, 0f);
            caja.size = new Vector3(0.66f, 1.0f, 0.5f);
            foreach (var t in raiz.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            EditorSceneManager.MarkSceneDirty(raiz.scene);
        }

        /// <summary>
        /// La palanca 1 sube todas sus luces a la misma intensidad, y las tres
        /// del estante estan a medio metro de la barra: la dejaban blanca de
        /// brillo y no se veian las botellas. Se bajan solo esas.
        /// </summary>
        [MenuItem("Blackout/Onepage/Luz de la barra")]
        public static void LuzBarra()
        {
            var palanca = GameObject.Find("Palanca_01");
            var ls = palanca != null ? palanca.GetComponent<LightSwitch>() : null;
            if (ls == null) return;
            var so = new SerializedObject(ls);
            var luces = so.FindProperty("luces");
            var escala = so.FindProperty("escalaPorLuz");
            escala.arraySize = luces.arraySize;
            for (int i = 0; i < luces.arraySize; i++)
            {
                var l = luces.GetArrayElementAtIndex(i).objectReferenceValue;
                escala.GetArrayElementAtIndex(i).floatValue = l != null && l.name.StartsWith("Luz_Estante") ? 0.35f : 1f;
            }
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(palanca.scene);
        }

        // ---------- Brillo ----------

        [MenuItem("Blackout/Onepage/Brillo en lo agarrable")]
        public static void Brillo()
        {
            int n = 0;
            foreach (var it in Object.FindObjectsByType<XRBaseInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Las zonas de teletransporte tambien son interactables: el suelo
                // entero brillaria al apuntarlo.
                if (it is BaseTeleportationInteractable) continue;
                var brillo = it.GetComponent<BrilloAgarrable>();
                if (brillo == null) brillo = Undo.AddComponent<BrilloAgarrable>(it.gameObject);

                var fuera = new List<Object>();
                foreach (var r in it.GetComponentsInChildren<Renderer>(true))
                    if (r.name == "Liquido" || r.name == "Espuma" || r.name.StartsWith("Chorro")) fuera.Add(r);
                var so = new SerializedObject(brillo);
                var lista = so.FindProperty("excluir");
                lista.arraySize = fuera.Count;
                for (int i = 0; i < fuera.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = fuera[i];
                so.ApplyModifiedProperties();
                n++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Onepage] Brillo en " + n + " objetos agarrables.");
        }

        // ---------- Utilidades ----------

        private static Transform Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            Undo.RegisterCreatedObjectUndo(go, nombre);
            go.name = nombre;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        private static void Caja(GameObject go, Vector3 centro, Vector3 tam)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = centro;
            c.size = tam;
        }

        private static void Borrar(Transform padre, string nombre)
        {
            var t = padre.Find(nombre);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }

        private static Material Material(string nombre, System.Action<Material> ajustar)
        {
            string ruta = RutaMat + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find(Lit));
                AssetDatabase.CreateAsset(m, ruta);
            }
            ajustar(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>URP/Lit en modo transparente con mezcla alfa normal.</summary>
        private static void Transparente(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            // Sin "conservar especular": con la luz roja tan fuerte el brillo
            // del vidrio tapaba el color del licor y las botellas se veian blancas.
            m.SetFloat("_BlendModePreserveSpecular", 0f);
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
        }
    }
}
