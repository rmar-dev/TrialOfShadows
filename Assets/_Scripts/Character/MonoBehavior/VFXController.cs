using System.Collections.Generic;
using _Scripts.Core;
using Gamekit2D;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Character.MonoBehavior
{
    [System.Serializable]
    public class VFX
    {
        [System.Serializable]
        public class VFXOverride
        {
            public TileBase tile;
            public GameObject prefab;
        }

        public GameObject prefab;
        public float lifetime = 1;
        public VFXOverride[] vfxOverride;

        [System.NonSerialized] public VFXInstancePool Pool;
        [System.NonSerialized] public Dictionary<TileBase, VFXInstancePool> VFXOverrideDictionnary;
    }

    public class VFXInstance : PoolObject<VFXInstancePool, VFXInstance>, System.IComparable<VFXInstance>
    {
        public float Expires;
        public Animation Animation;
        public AudioSource AudioSource;
        public ParticleSystem[] ParticleSystems;
        public Transform Transform;
        public Transform Parent;

        protected override void SetReferences()
        {
            Transform = instance.transform;
            Animation = instance.GetComponentInChildren<Animation>();
            AudioSource = instance.GetComponentInChildren<AudioSource>();
            ParticleSystems = instance.GetComponentsInChildren<ParticleSystem>();
        }

        public override void WakeUp()
        {
            instance.SetActive(true);
            for (var i = 0; i < ParticleSystems.Length; i++)
                ParticleSystems[i].Play();
            if (Animation != null)
            {
                Animation.Rewind();
                Animation.Play();
            }
            if (AudioSource != null)
                AudioSource.Play();
        }

        public override void Sleep()
        {
            for (var i = 0; i < ParticleSystems.Length; i++)
            {
                ParticleSystems[i].Stop();
            }
            if (Animation != null)
                Animation.Stop();
            if (AudioSource != null)
                AudioSource.Stop();
            instance.SetActive(false);
        }

        public void SetPosition(Vector3 position)
        {
            Transform.localPosition = position;
        }

        public int CompareTo(VFXInstance other)
        {
            return Expires.CompareTo(other.Expires);
        }

    }

    public class VFXInstancePool : ObjectPool<VFXInstancePool, VFXInstance>
    {

    }

    public class VFXController : MonoBehaviour
    {
        public static VFXController Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<VFXController> ();

                if (instance != null)
                    return instance;

                return CreateDefault ();
            }
        }

        protected static VFXController instance;

        public static VFXController CreateDefault ()
        {
            VFXController controllerPrefab = Resources.Load<VFXController> ("VFXController");
            instance = Instantiate (controllerPrefab);
            return instance;
        }

        struct PendingVFX : System.IComparable<PendingVFX>
        {
            public VFX VFX;
            public Vector3 Position;
            public float StartAt;
            public bool Flip;
            public Transform Parent;
            public TileBase TileOverride;

            public int CompareTo(PendingVFX other)
            {
                return StartAt.CompareTo(other.StartAt);
            }
        }


        public VFX[] vfxConfig;

        Dictionary<int, VFX> m_FxPools = new Dictionary<int, VFX>();
        PriorityQueue<VFXInstance> m_RunningFx = new PriorityQueue<VFXInstance>();
        PriorityQueue<PendingVFX> m_PendingFx = new PriorityQueue<PendingVFX>();

        public void Awake()
        {
            if (Instance != this)
            {
                Destroy (gameObject);
                return;
            }

            DontDestroyOnLoad (gameObject);

            foreach (var vfx in vfxConfig)
            {
                vfx.Pool = gameObject.AddComponent<VFXInstancePool>();
                vfx.Pool.initialPoolCount = 2;
                vfx.Pool.prefab = vfx.prefab;

                vfx.VFXOverrideDictionnary = new Dictionary<TileBase, VFXInstancePool>();
                for(int i = 0; i < vfx.vfxOverride.Length; ++i)
                {
                    TileBase tb = vfx.vfxOverride[i].tile;

                    GameObject obj = new GameObject("vfxOverride");
                    obj.transform.SetParent(transform);
                    vfx.VFXOverrideDictionnary[tb] = obj.AddComponent<VFXInstancePool>();
                    vfx.VFXOverrideDictionnary[tb].initialPoolCount = 2;
                    vfx.VFXOverrideDictionnary[tb].prefab = vfx.vfxOverride[i].prefab;
                }

                m_FxPools[StringToHash(vfx.prefab.name)] = vfx;
            }
        }

        public void Trigger(string triggerName, Vector3 position, float startDelay, bool flip, Transform parent, TileBase tileOverride = null)
        {
            Trigger(StringToHash(triggerName), position, startDelay, flip, parent, tileOverride);
        }

        public void Trigger(int hash, Vector3 position, float startDelay, bool flip, Transform parent, TileBase tileOverride = null)
        {
            VFX vfx;
            if (!m_FxPools.TryGetValue(hash, out vfx))
            {
                Debug.LogError("VFX does not exist.");
            }
            else
            {
                if (startDelay > 0)
                {
                    m_PendingFx.Push(new PendingVFX() { VFX = vfx, Position = position, StartAt = Time.time + startDelay, Flip = flip, Parent = parent, TileOverride = tileOverride });
                }
                else
                    CreateInstance(vfx, position, flip, parent, tileOverride);
            }
        }

        void Update()
        {
            while (!m_RunningFx.Empty && m_RunningFx.First.Expires <= Time.time)
            {
                var instance = m_RunningFx.Pop();
                instance.objectPool.Push(instance);
            }
            while (!m_PendingFx.Empty && m_PendingFx.First.StartAt <= Time.time)
            {
                var task = m_PendingFx.Pop();
                CreateInstance(task.VFX, task.Position, task.Flip, task.Parent, task.TileOverride);
            }
            var instances = m_RunningFx.items;
            for (var i = 0; i < instances.Count; i++)
            {
                var vfx = instances[i];
                if (vfx.Parent != null)
                    vfx.Transform.position = vfx.Parent.position;
            }
        }

        void CreateInstance(VFX vfx, Vector4 position, bool flip, Transform parent, TileBase tileOverride)
        {
            VFXInstancePool poolToUse = null;

            if (tileOverride == null || !vfx.VFXOverrideDictionnary.TryGetValue(tileOverride, out poolToUse))
                poolToUse = vfx.Pool;

            var instance = poolToUse.Pop();

            instance.Expires = Time.time + vfx.lifetime;
            if (flip)
                instance.Transform.localScale = new Vector3(-1, 1, 1);
            else
                instance.Transform.localScale = new Vector3(1, 1, 1);
            instance.Parent = parent;
            instance.SetPosition(position);
            m_RunningFx.Push(instance);
        }

        public static int StringToHash(string name)
        {
            return name.GetHashCode();
        }


    }
}