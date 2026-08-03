using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using Unity.Mathematics;

namespace OccaSoftware.ResponsiveSmokes.Runtime
{
    [AddComponentMenu("OccaSoftware/Responsive Smokes/Smoke")]
    [ExecuteAlways]
    public class InteractiveSmoke : MonoBehaviour
    {
        [Header("Performance")]
        [SerializeField]
        [Tooltip("The quality of the generated SDF.")]
        SdfQuality sdfQuality;

        [SerializeField]
        [Tooltip("Set the maximum frame cost (in milliseconds) that the volumetric smoke generation system will allocate.")]
        private float maximumFrameCostMilliseconds = 0.5f;

        [SerializeField]
        [Tooltip("Set the maximum number of steps in the volume's ray march.")]
        [Range(12, 128)]
        private int maximumStepCount = 64;

        [Header("Density Options")]
        [SerializeField]
        [Tooltip("Set the maximum density of the smoke (i.e., the density of the smoke during the active lifetime).")]
        private float maximumDensity = 10f;

        [SerializeField]
        [Tooltip("Set the minimum visibility of the smoke. Visibility below this value is clamped down to 0.")]
        [Range(0, 1)]
        private float minimumVisibility = 0.03f;

        [Header("Noise Options")]
        [SerializeField]
        [Tooltip("Set the volumetric noise used to erode the smoke volume.")]
        private Texture3D noiseTexture;

        [SerializeField]
        [Tooltip("Set the world-space distance over which the smoke is eroded by the volumetric noise.")]
        [Min(0)]
        private float erosionScale = 1f;

        [SerializeField]
        [Tooltip("Set the wind speed and direction that is used to offset the volumetric noise.")]
        private Vector3 wind = new Vector3(0.1f, 0.1f, 0.1f);

        [SerializeField]
        [Tooltip("Set the scale of the volumetric noise.")]
        [Min(0)]
        private float scale = 3f;

        [Header("Lighting Options")]
        [SerializeField]
        [ColorUsage(false, true)]
        [Tooltip("Tint the main light color.")]
        private Color mainLightTint = Color.white;

        [SerializeField]
        [ColorUsage(false, true)]
        [Tooltip("Tint the ambient light color.")]
        private Color ambientColorTint = Color.white;

        [SerializeField]
        [ColorUsage(false, true)]
        [Tooltip("Set the smoke albedo.")]
        private Color albedo = Color.white;

        [SerializeField, Range(1, 4)]
        [Tooltip("Set the number of shadow steps.")]
        private int shadowStepCount = 3;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Set the length of each shadow step.")]
        private float shadowStepSize = 0.3f;

        [Header("Generation Options")]
        [SerializeField]
        [Tooltip("Represents the percentage of the unpadded voxel volume that the volumetric smoke will attempt to fill.")]
        [Range(0, 1)]
        private float targetFillPercentage = 0.3f;

        [SerializeField]
        [Tooltip("Time between physics generation steps (in seconds).")]
        private float expansionUpdateFrequency = 0.0167f;

        [SerializeField]
        [Tooltip("Set the target duration over which the smoke will attempt to expand.")]
        private float expansionDuration = 1f;

        [Header("Lifetime")]
        [SerializeField]
        [Tooltip("Set the duration over which the smoke goes from 0 density to maximum density.")]
        [Min(0)]
        private float fadeInDuration = 1f;

        [SerializeField]
        [Tooltip("Set the duration for which the smoke will be at maximum density.")]
        [Min(0)]
        private float activeLifetime = 10f;

        [SerializeField]
        [Tooltip("Set the target duration over which the smoke goes from maximum density to 0.")]
        private float fadeOutDuration = 1f;

        [Header("Options")]
        [SerializeField]
        [Tooltip("Set what the smoke should do when it has finished.")]
        private CleanupOptions cleanupOptions = CleanupOptions.Destroy;

        [SerializeField]
        [Tooltip("Set what the smoke should do when it is enabled.")]
        private StartupOptions startupOptions = StartupOptions.OnEnable;

#if UNITY_EDITOR
		[Header("Utility")]
		[SerializeField]
		[Tooltip("Forces the smoke to re-generate in the editor. Useful for testing.")]
		private bool regenerate = false;
#endif

        #region Public Getters
        /// <summary>
        /// Gets the current state of the volumetric smoke.
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            return isAlive;
        }
        #endregion



        #region Private Variables
        private Texture3D sdfTexture = null;
        private Material smokeMaterial = null;

        private float density
        {
            get => maximumDensity * density01;
        }
        private float density01;

        private bool isAlive = false;

        private HashSet<int3> coordSet = new HashSet<int3>();

        private ConstantData Constants;

        private PriorityQueue<VoxelData, float> builderQueue = new PriorityQueue<VoxelData, float>();

        private PriorityQueue<QueueElement, float> sdfQueue;

        private bool[] isFilled;
        private bool[] isClosed;
        private float[] distance;

        private RaycastHit[] raycastHits = new RaycastHit[1];

        private System.Diagnostics.Stopwatch earlyExitTracker = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch raycastTimer = new System.Diagnostics.Stopwatch();

        private HashSet<InteractiveExplosion> explosions = new HashSet<InteractiveExplosion>(10);
        private HashSet<InteractiveProjectile> projectiles = new HashSet<InteractiveProjectile>(10);
        #endregion

        private void OnValidate()
        {
            targetFillPercentage = Mathf.Clamp01(targetFillPercentage);
#if UNITY_EDITOR
			if (regenerate)
			{
				Init();
				regenerate = false;
			}
#endif
        }

        public void Smoke()
        {
            Init();
        }

        private void OnEnable()
        {
            if (startupOptions == StartupOptions.OnEnable)
                Init();
        }

        private void Update()
        {
            if (sdfTexture == null)
            {
                Init();
            }

            SetShaderProperties();
        }

        /// <summary>
        /// Adds an explosion to this interactive smoke's history.
        /// </summary>
        /// <param name="explosion"></param>
        public void TryAddExplosion(InteractiveExplosion explosion)
        {
            if (explosions.Count >= Common._MAX_EXPLOSIONS)
                return;

            explosions.Add(explosion);
        }

        /// <summary>
        /// Adds a projectile to this interactive smoke's history.
        /// </summary>
        /// <param name="projectile"></param>
        public void TryAddProjectile(InteractiveProjectile projectile)
        {
            if (projectiles.Count >= Common._MAX_PROJECTILES)
                return;

            projectiles.Add(projectile);
        }

        private void SetShaderProperties()
        {
            smokeMaterial.SetTexture(ShaderParams._PropagationVolume, sdfTexture);
            smokeMaterial.SetFloat(ShaderParams._Density, density);
            smokeMaterial.SetFloat(ShaderParams._MaxSteps, maximumStepCount);
            smokeMaterial.SetFloat(ShaderParams._MinimumVisibility, minimumVisibility);
            smokeMaterial.SetFloat(ShaderParams._ErosionScale, erosionScale);
            smokeMaterial.SetFloat(ShaderParams._InvErosionScale, 1.0f / erosionScale);
            smokeMaterial.SetTexture(ShaderParams._NoiseTexture, noiseTexture);
            smokeMaterial.SetVector(ShaderParams._NoiseWind, wind);
            smokeMaterial.SetFloat(ShaderParams._NoiseScale, scale);
            smokeMaterial.SetVector(ShaderParams._ObjectPosition, transform.position);
            smokeMaterial.SetFloat(ShaderParams._ObjectScale, transform.localScale.x);
            smokeMaterial.SetFloat(ShaderParams._InvObjectScale, 1.0f / transform.localScale.x);

            smokeMaterial.SetColor(ShaderParams._MainLightTint, mainLightTint);
            smokeMaterial.SetColor(ShaderParams._AmbientColorTint, ambientColorTint);
            smokeMaterial.SetColor(ShaderParams._Albedo, albedo);
            smokeMaterial.SetFloat(ShaderParams._ShadowStepSize, shadowStepSize);
            smokeMaterial.SetFloat(ShaderParams._ShadowStepCount, shadowStepCount);

            SetGrenadeProperties();
            SetProjectileProperties();
        }

        internal static class Common
        {
            public static int _MAX_EXPLOSIONS = 3;
            public static int _MAX_PROJECTILES = 10;
        }

        private Vector4[] explosionTransforms = new Vector4[Common._MAX_EXPLOSIONS];
        private float[] explosionIntensities = new float[Common._MAX_EXPLOSIONS];

        private void SetGrenadeProperties()
        {
            if (explosions.Count > 0)
            {
                explosions.RemoveWhere(x => x == null || !x.isActiveAndEnabled || !x.IsAlive());
            }

            smokeMaterial.SetInt(ShaderParams.Explosion._ExplosionCount, explosions.Count);

            if (explosions.Count <= 0)
                return;

            // xyz => origin, w => radius
            int i = 0;
            foreach (InteractiveExplosion explosion in explosions)
            {
                Vector3 origin = explosion.GetOrigin();
                explosionTransforms[i] = new Vector4(origin.x, origin.y, origin.z, explosion.GetRadius());
                explosionIntensities[i] = explosion.GetIntensity();
                i++;
            }

            smokeMaterial.SetVectorArray(ShaderParams.Explosion._ExplosionTransform, explosionTransforms);
            smokeMaterial.SetFloatArray(ShaderParams.Explosion._ExplosionIntensity, explosionIntensities);
        }

        private Vector4[] starts = new Vector4[Common._MAX_PROJECTILES];
        private Vector4[] ends = new Vector4[Common._MAX_PROJECTILES];
        private float[] radii = new float[Common._MAX_PROJECTILES];
        private float[] intensities = new float[Common._MAX_PROJECTILES];

        private void SetProjectileProperties()
        {
            if (projectiles.Count > 0)
            {
                projectiles.RemoveWhere(x => x == null || !x.isActiveAndEnabled || !x.IsAlive());
            }

            smokeMaterial.SetInt(ShaderParams.Projectile._ProjectileCount, projectiles.Count);

            if (projectiles.Count <= 0)
                return;

            int i = 0;
            foreach (InteractiveProjectile projectile in projectiles)
            {
                Vector3 start = projectile.GetStartPoint();
                Vector3 end = projectile.GetEndPoint();

                starts[i] = new Vector4(start.x, start.y, start.z, 0);
                ends[i] = new Vector4(end.x, end.y, end.z, 0);
                radii[i] = projectile.GetRadius();
                intensities[i] = projectile.GetIntensity();
                i++;
            }

            smokeMaterial.SetVectorArray(ShaderParams.Projectile._ProjectileStart, starts);
            smokeMaterial.SetVectorArray(ShaderParams.Projectile._ProjectileEnd, ends);
            smokeMaterial.SetFloatArray(ShaderParams.Projectile._ProjectileRadius, radii);
            smokeMaterial.SetFloatArray(ShaderParams.Projectile._ProjectileIntensity, intensities);
        }

        private IEnumerator SetRelativeDensityOverTime(float duration, float start, float end)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                density01 = Mathf.Lerp(start, end, t);

                yield return null;
            }
        }

        private void Init()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            isAlive = true;
            earlyExitTracker.Restart();
            SetupVariables();
            SetupTexture();
            SetupMaterial();
            PrepareCollections();

            StartCoroutine(SetRelativeDensityOverTime(fadeInDuration, 0f, 1f));
            StartCoroutine(PropagateVoxels());
        }

        private void SetupVariables()
        {
            Constants.Setup(GetResolution(sdfQuality), targetFillPercentage, transform.localScale.x);
            CreateDirectionData();
        }

        private void SetupTexture()
        {
            sdfTexture = new Texture3D(
                Constants.PaddedTextureResolution,
                Constants.PaddedTextureResolution,
                Constants.PaddedTextureResolution,
                TextureFormat.RFloat,
                false
            );

            float[] clear = new float[Constants.PaddedTextureVoxelCount];
            Array.Fill(clear, Mathf.Infinity);
            sdfTexture.SetPixelData(clear, 0);
            sdfTexture.Apply(false);
        }

        private void SetupMaterial()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            smokeMaterial = new Material(Shader.Find("OccaSoftware/ResponsiveSmoke/Smoke"));
            renderer.sharedMaterial = smokeMaterial;
        }

        private void PrepareCollections()
        {
            coordSet = new HashSet<int3>((int)(Constants.PaddedTextureVoxelCount * Constants.TargetFillRatio));
            sdfQueue = new PriorityQueue<QueueElement, float>(Constants.PaddedTextureVoxelCount * 2, new SortAscending());

            distance = new float[Constants.PaddedTextureVoxelCount];
            isClosed = new bool[Constants.PaddedTextureVoxelCount];
            isFilled = new bool[Constants.PaddedTextureVoxelCount];

            explosions.Clear();
            projectiles.Clear();
        }

        private IEnumerator PropagateVoxels()
        {
            yield return StartCoroutine(PropagateSmoke());
            yield return new WaitForSeconds(activeLifetime);
            yield return StartCoroutine(SetRelativeDensityOverTime(fadeOutDuration, 1f, 0f));
            Cleanup();
        }

        private void Cleanup()
        {
            isAlive = false;
            explosions.Clear();
            projectiles.Clear();
            if (cleanupOptions == CleanupOptions.Disable)
            {
                if (Application.isPlaying)
                {
                    gameObject.SetActive(false);
                }
            }
            else if (cleanupOptions == CleanupOptions.Destroy)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }

        #region Build SDF
        public IEnumerator BuildSDF()
        {
            sdfQueue.Clear();
            Array.Fill(isClosed, false);
            Array.Fill(isFilled, false);
            Array.Fill(distance, Mathf.Infinity);

            // fill array pass
            foreach (int3 coord in coordSet)
            {
                isFilled[GetIndex(coord.x, coord.y, coord.z)] = true;
            }

            // initialize pass
            f = AddToQueueInit;
            for (int z = 0; z < Constants.PaddedTextureResolution; z++)
            {
                for (int y = 0; y < Constants.PaddedTextureResolution; y++)
                {
                    for (int x = 0; x < Constants.PaddedTextureResolution; x++)
                    {
                        int index = GetIndex(x, y, z);
                        if (!isFilled[index])
                        {
                            isClosed[index] = true;
                            distance[index] = 0;

                            ComputeNeighbors(x, y, z);

                            if (earlyExitTracker.Elapsed.TotalMilliseconds > maximumFrameCostMilliseconds)
                            {
                                yield return null;
                                earlyExitTracker.Restart();
                            }
                        }
                    }
                }
            }

            // priority queue pass
            f = AddToQueue;
            QueueElement current;
            while (sdfQueue.Count > 0)
            {
                current = sdfQueue.Dequeue();
                int index = current.index;
                if (isClosed[index])
                    continue;

                isClosed[index] = true;
                distance[index] = current.distance;
                ComputeNeighbors(current.x, current.y, current.z);

                if (earlyExitTracker.Elapsed.TotalMilliseconds > maximumFrameCostMilliseconds)
                {
                    yield return null;
                    earlyExitTracker.Restart();
                }
            }

            sdfTexture.SetPixelData(distance, 0);
            sdfTexture.Apply(false);
        }

        private float Length(int x, int y, int z)
        {
            return math.sqrt(x * x + y * y + z * z);
        }

        private void AddToQueueInit(int x, int y, int z, int dx, int dy, int dz)
        {
            float dist = -Length(dx, dy, dz) * 0.5f;
            sdfQueue.Enqueue(new QueueElement(x, y, z, dist, GetIndex(x, y, z)), dist);
        }

        private void AddToQueue(int x, int y, int z, int dx, int dy, int dz)
        {
            float sourceDistance = distance[GetIndex(x - dx, y - dy, z - dz)];
            float dist = -Length(dx, dy, dz) + sourceDistance;
            sdfQueue.Enqueue(new QueueElement(x, y, z, dist, GetIndex(x, y, z)), dist);
        }

        private Action<int, int, int, int, int, int> f;

        private void ComputeNeighbors(int x, int y, int z)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x2 = x + dx;
                        int y2 = y + dy;
                        int z2 = z + dz;

                        if (IsValid(x2, y2, z2) && IsFilled(x2, y2, z2) && !isClosed[GetIndex(x2, y2, z2)])
                        {
                            f(x2, y2, z2, dx, dy, dz);
                        }
                    }
                }
            }
        }

        private bool IsValid(int x, int y, int z)
        {
            if (
                x < 0
                || y < 0
                || z < 0
                || x >= Constants.PaddedTextureResolution
                || y >= Constants.PaddedTextureResolution
                || z >= Constants.PaddedTextureResolution
            )
                return false;

            return true;
        }

        private bool IsFilled(int x, int y, int z)
        {
            return isFilled[GetIndex(x, y, z)];
        }

        private int GetIndex(int x, int y, int z)
        {
            return z * Constants.PaddedTextureResolution * Constants.PaddedTextureResolution + y * Constants.PaddedTextureResolution + x;
        }
        #endregion


        #region Propagate Smoke
        private IEnumerator PropagateSmoke()
        {
            raycastTimer.Restart();
            builderQueue.Clear();

            coordSet.Add(Constants.Center);
            AddLocalGroup(new VoxelData(Constants.Center, transform.position));

            int timeStepMs = (int)(expansionUpdateFrequency * 1000f);
            int expansionAmountThisTimestep = 1;
            int numberOfTimesteps = Mathf.CeilToInt(expansionDuration / expansionUpdateFrequency);
            numberOfTimesteps = Mathf.Max(1, numberOfTimesteps);
            int expansionRate = Mathf.CeilToInt((float)Constants.TargetVoxelCount / numberOfTimesteps);
            if (numberOfTimesteps < 1)
            {
                expansionRate = int.MaxValue;
            }

            while (builderQueue.Count > 0 && coordSet.Count < Constants.TargetVoxelCount)
            {
                VoxelData target = builderQueue.Dequeue();

                if (coordSet.Contains(target.coord))
                    continue;

                if (Physics.RaycastNonAlloc(target.parentPosition, target.direction, raycastHits, Constants.VoxelSizeWS * target.magnitude) <= 0)
                {
                    coordSet.Add(target.coord);
                    expansionAmountThisTimestep++;

                    AddLocalGroup(target);

                    if (expansionAmountThisTimestep > expansionRate)
                    {
                        yield return StartCoroutine(BuildSDF());
                        int delay = timeStepMs - (int)raycastTimer.ElapsedMilliseconds;
                        delay = Mathf.Max(delay, 0);
                        yield return new WaitForSeconds(delay / 1000f);
                        raycastTimer.Restart();
                        earlyExitTracker.Restart();
                        expansionAmountThisTimestep = 0;
                    }
                }

                if (earlyExitTracker.Elapsed.TotalMilliseconds > maximumFrameCostMilliseconds)
                {
                    yield return null;
                    earlyExitTracker.Restart();
                    raycastTimer.Restart();
                }
            }

            yield return StartCoroutine(BuildSDF());
        }

        private void AddLocalGroup(VoxelData target)
        {
            foreach (DirectionData direction in directionDatas)
            {
                VoxelData newTarget = new VoxelData();

                newTarget.coord = target.coord + direction.directionInt;

                if (coordSet.Contains(newTarget.coord))
                    continue;

                if (!IsInBounds(newTarget.coord))
                    continue;

                newTarget.worldPosition = target.worldPosition + direction.direction * Constants.VoxelSizeWS * direction.magnitude;
                newTarget.distance = target.distance + direction.magnitude;
                newTarget.direction = direction.direction;
                newTarget.magnitude = direction.magnitude;
                newTarget.parentPosition = target.worldPosition;

                builderQueue.Enqueue(newTarget, newTarget.distance);
            }
        }

        /// <summary>
        /// Determines if the coordinate is within the voxel structure bounds.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        private bool IsInBounds(int3 coord)
        {
            if (coord.x < 1 || coord.y < 1 || coord.z < 1)
                return false;

            if (
                coord.x >= Constants.PaddedTextureResolution - 1
                || coord.y >= Constants.PaddedTextureResolution - 1
                || coord.z >= Constants.PaddedTextureResolution - 1
            )
                return false;

            return true;
        }
        #endregion


        #region Structs
        struct VoxelData
        {
            public int3 coord;
            public float3 worldPosition;
            public float distance;

            public float3 parentPosition;
            public float3 direction;
            public float magnitude;

            public VoxelData(float3 worldPosition)
            {
                coord = new int3(0, 0, 0);
                distance = 0;
                this.worldPosition = worldPosition;
                direction = 0;
                magnitude = 0;
                parentPosition = 0;
            }

            public VoxelData(int3 coord, float3 worldPosition)
            {
                this.coord = coord;
                distance = 0;
                this.worldPosition = worldPosition;
                direction = 0;
                magnitude = 0;
                parentPosition = 0;
            }

            public VoxelData(int3 coord, float3 worldPosition, float distance)
            {
                this.coord = coord;
                this.distance = distance;
                this.worldPosition = worldPosition;
                direction = 0;
                magnitude = 0;
                parentPosition = 0;
            }
        }

        private struct QueueElement
        {
            public int x,
                y,
                z;
            public float distance;
            public int index;

            public QueueElement(int x, int y, int z, float distance, int index)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.distance = distance;
                this.index = index;
            }
        }

        private struct ConstantData
        {
            public int TextureResolution;
            public int TargetVoxelCount;
            public int UnpaddedTextureVoxelCount;
            public int PaddedTextureVoxelCount;
            public int PaddedTextureResolution;
            public float VoxelSizeWS;
            public int Center;
            public float TargetFillRatio;

            public void Setup(int textureResolution, float targetFillRatio, float objectScale)
            {
                TextureResolution = textureResolution;
                TargetFillRatio = targetFillRatio;
                PaddedTextureResolution = TextureResolution + 2;

                UnpaddedTextureVoxelCount = TextureResolution * TextureResolution * TextureResolution;

                TargetVoxelCount = (int)Mathf.Floor(TargetFillRatio * UnpaddedTextureVoxelCount);

                PaddedTextureVoxelCount = PaddedTextureResolution * PaddedTextureResolution * PaddedTextureResolution;
                VoxelSizeWS = objectScale / PaddedTextureResolution;
                Center = (PaddedTextureResolution - 1) / 2;
            }
        }
        #endregion


        #region Direction Data
        private struct DirectionData
        {
            public float3 direction;
            public int3 directionInt;
            public float magnitude;

            public DirectionData(int3 directionInt)
            {
                this.directionInt = directionInt;
                direction = directionInt;
                magnitude = directionInt.Length();
            }
        }

        private static void CreateDirectionData()
        {
            directionDatas = new List<DirectionData>(27);
            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        directionDatas.Add(new DirectionData(new int3(x, y, z)));
                    }
                }
            }
        }

        private static List<DirectionData> directionDatas;
        #endregion


        #region Quality
        private enum SdfQuality
        {
            Low,
            Medium,
            High
        }

        private int GetResolution(SdfQuality quality)
        {
            switch (quality)
            {
                case SdfQuality.Low:
                    return 7;
                case SdfQuality.Medium:
                    return 9;
                case SdfQuality.High:
                    return 11;
                default:
                    return 9;
            }
        }
        #endregion


        private class SortAscending : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                if (x > y)
                    return -1;

                if (x < y)
                    return 1;

                return 0;
            }
        }
    }

    internal static class Extensions
    {
        public static float Length(this int3 a)
        {
            return math.sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
        }
    }

    internal static class ShaderParams
    {
        public static int _PropagationVolume = Shader.PropertyToID("_PropagationVolume");
        public static int _Density = Shader.PropertyToID("_Density");
        public static int _MaxSteps = Shader.PropertyToID("_MaxSteps");
        public static int _MinimumVisibility = Shader.PropertyToID("_MinimumVisibility");
        public static int _ErosionScale = Shader.PropertyToID("_ErosionScale");
        public static int _InvErosionScale = Shader.PropertyToID("_InvErosionScale");
        public static int _NoiseTexture = Shader.PropertyToID("_NoiseTexture");
        public static int _NoiseWind = Shader.PropertyToID("_NoiseWind");
        public static int _NoiseScale = Shader.PropertyToID("_NoiseScale");

        public static int _ObjectPosition = Shader.PropertyToID("_ObjectPosition");
        public static int _ObjectScale = Shader.PropertyToID("_ObjectScale");
        public static int _InvObjectScale = Shader.PropertyToID("_InvObjectScale");

        public static int _MainLightTint = Shader.PropertyToID("_MainLightTint");
        public static int _AmbientColorTint = Shader.PropertyToID("_AmbientColorTint");
        public static int _Albedo = Shader.PropertyToID("_Albedo");
        public static int _ShadowStepSize = Shader.PropertyToID("_ShadowStepSize");
        public static int _ShadowStepCount = Shader.PropertyToID("_ShadowStepCount");

        public static class Explosion
        {
            public static int _ExplosionCount = Shader.PropertyToID("_ExplosionCount");
            public static int _ExplosionTransform = Shader.PropertyToID("_ExplosionTransform");
            public static int _ExplosionIntensity = Shader.PropertyToID("_ExplosionIntensity");
        }

        public static class Projectile
        {
            public static int _ProjectileStart = Shader.PropertyToID("_ProjectileStart");
            public static int _ProjectileEnd = Shader.PropertyToID("_ProjectileEnd");
            public static int _ProjectileRadius = Shader.PropertyToID("_ProjectileRadius");
            public static int _ProjectileIntensity = Shader.PropertyToID("_ProjectileIntensity");
            public static int _ProjectileCount = Shader.PropertyToID("_ProjectileCount");
        }
    }
}
