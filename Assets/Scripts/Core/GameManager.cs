using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MonsterWorldLike.Economy;
using MonsterWorldLike.Garden;
using MonsterWorldLike.Progression;

namespace MonsterWorldLike.Core
{
    public class GameManager : MonoBehaviour
    {
        private const float MinAutosaveIntervalSeconds = 5f;

        public static GameManager Instance { get; private set; }

        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool autosaveEnabled = true;
        [SerializeField, Min(MinAutosaveIntervalSeconds)] private float autosaveIntervalSeconds = 20f;
        [SerializeField, Min(0.5f)] private float maxLoadApplyWaitSeconds = 8f;

        private float autosaveTimer;
        private float pendingApplyRetryTimer;

        [SerializeField, Min(0.1f)] private float pendingApplyRetryIntervalSeconds = 0.5f;

        public float AutosaveIntervalSeconds
        {
            get => autosaveIntervalSeconds;
            set => autosaveIntervalSeconds = Mathf.Max(MinAutosaveIntervalSeconds, value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            AutosaveIntervalSeconds = autosaveIntervalSeconds;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EconomyManager.InstanceReady += OnSystemInstanceReady;
            GardenManager.InstanceReady += OnSystemInstanceReady;
            ProgressionManager.InstanceReady += OnSystemInstanceReady;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EconomyManager.InstanceReady -= OnSystemInstanceReady;
            GardenManager.InstanceReady -= OnSystemInstanceReady;
            ProgressionManager.InstanceReady -= OnSystemInstanceReady;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            AutosaveIntervalSeconds = autosaveIntervalSeconds;
            maxLoadApplyWaitSeconds = Mathf.Max(0.5f, maxLoadApplyWaitSeconds);
            pendingApplyRetryIntervalSeconds = Mathf.Max(0.1f, pendingApplyRetryIntervalSeconds);
        }

        private void Start()
        {
            if (loadOnStart)
            {
                StartCoroutine(LoadAndApplyWhenSceneReady());
            }
        }

        private IEnumerator LoadAndApplyWhenSceneReady()
        {
            var loadResult = SaveManager.LoadDataFromDisk();
            if (loadResult != LoadDataResult.Loaded)
            {
                if (loadResult == LoadDataResult.LoadFailed)
                {
                    Debug.LogWarning("Save file exists but could not be loaded.");
                }

                yield break;
            }

            var waited = 0f;
            while (!SaveManager.CanApplyLoadedDataToScene() && waited < maxLoadApplyWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!SaveManager.TryApplyPendingLoadedData())
            {
                Debug.LogWarning("Save loaded and kept pending. Will retry when scene systems are ready.");
            }
        }

        private void Update()
        {
            TryApplyPendingLoadedDataInCurrentScene();

            if (!autosaveEnabled)
            {
                return;
            }

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer < autosaveIntervalSeconds)
            {
                return;
            }

            if (SaveManager.TrySave())
            {
                autosaveTimer = 0f;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrySaveAndResetAutosaveTimer();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                TrySaveAndResetAutosaveTimer();
            }
        }

        private void OnApplicationQuit()
        {
            SaveManager.TrySave(force: true);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveManager.TryApplyPendingLoadedData();
        }

        private void OnSystemInstanceReady()
        {
            SaveManager.TryApplyPendingLoadedData();
        }

        private void TryApplyPendingLoadedDataInCurrentScene()
        {
            if (!SaveManager.HasPendingLoadedData)
            {
                pendingApplyRetryTimer = 0f;
                return;
            }

            pendingApplyRetryTimer += Time.unscaledDeltaTime;
            if (pendingApplyRetryTimer < pendingApplyRetryIntervalSeconds)
            {
                return;
            }

            pendingApplyRetryTimer = 0f;
            SaveManager.TryApplyPendingLoadedData();
        }

        private void TrySaveAndResetAutosaveTimer()
        {
            if (SaveManager.TrySave())
            {
                autosaveTimer = 0f;
            }
        }
    }
}
