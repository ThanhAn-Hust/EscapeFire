using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeFire.Core
{
    public static class SceneNames
    {
        public const string MAIN_MENU = "00_MainMenu";
        public const string LAB = "BasicScene"; // 01_ComputerLab_Sim in the spec; kept as BasicScene to preserve history.
        public const string AAR = "02_AAR_Evaluation";
    }

    /// <summary>
    /// Async scene loading with an optional fade (CanvasGroup on a camera-attached overlay).
    /// Works without any instance in the scene: <see cref="Load"/> spawns a persistent runner on demand.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        private static SceneFlowManager _instance;

        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 0.6f;

        public static bool IsLoading { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            if (fadeGroup != null) fadeGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            IsLoading = false;
        }

        public static void Load(string sceneName)
        {
            if (IsLoading || string.IsNullOrEmpty(sceneName)) return;
            if (_instance == null)
            {
                _instance = new GameObject("[SceneFlowManager]").AddComponent<SceneFlowManager>();
            }
            _instance.StartCoroutine(_instance.LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            yield return Fade(0f, 1f);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[SceneFlow] Scene '{sceneName}' is not in Build Settings.");
                IsLoading = false;
                yield return Fade(1f, 0f);
                yield break;
            }
            while (!op.isDone) yield return null;

            yield return Fade(1f, 0f);
            IsLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeGroup == null || fadeDuration <= 0f) yield break;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = to;
        }
    }
}
