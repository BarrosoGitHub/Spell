using UnityEngine;
#if MM_UI
using UnityEngine.UI;
#endif
using System.Collections;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoreMountains.Tools
{	
	/// <summary>
	/// A class to load scenes using a loading screen instead of just the default API
	/// This class used to be known as LoadingSceneManager, and has now been renamed to MMSceneLoadingManager for consistency
	/// </summary>
	public partial class MMSceneLoadingManager : MonoBehaviour 
	{
		public enum LoadingStatus
		{
			LoadStarted, BeforeEntryFade, EntryFade, AfterEntryFade, UnloadOriginScene, LoadDestinationScene,
			LoadProgressComplete, InterpolatedLoadProgressComplete, BeforeSceneActivation, ExitFade, DestinationSceneActivation, UnloadSceneLoader, LoadTransitionComplete, AfterSceneActivation
		}

		public partial struct LoadingSceneEvent
		{
			public LoadingStatus Status;
			public string SceneName;
			public LoadingSceneEvent(string sceneName, LoadingStatus status)
			{
				Status = status;
				SceneName = sceneName;
			}
			[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
			static LoadingSceneEvent e;
			public static void Trigger(string sceneName, LoadingStatus status)
			{
				e.Status = status;
				e.SceneName = sceneName;
				MMEventManager.TriggerEvent(e);
			}
		}

		[Header("Binding")]
		/// The name of the scene to load while the actual target scene is loading (usually a loading screen)
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static string LoadingScreenSceneName="LoadingScreen";

		[Header("GameObjects")]
		#if MM_UI
		/// the text object where you want the loading message to be displayed
		public Text LoadingText;
		#endif
		/// the canvas group containing the progress bar
		public CanvasGroup LoadingProgressBar;
		/// the canvas group containing the animation
		public CanvasGroup LoadingAnimation;
		/// the canvas group containing the animation to play when loading is complete
		public CanvasGroup LoadingCompleteAnimation;

		[Header("Time")]
		/// the duration (in seconds) of the initial fade in
		public float StartFadeDuration=0.2f;
		/// the speed of the progress bar
		public float ProgressBarSpeed=2f;
		/// the duration (in seconds) of the load complete fade out
		public float ExitFadeDuration=0.2f;
		/// the delay (in seconds) before leaving the scene when complete
		public float LoadCompleteDelay=0.5f;

		protected AsyncOperation _asyncOperation;
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static string _sceneToLoad = "";
		protected float _fadeDuration = 0.5f;
		protected float _fillTarget=0f;
		protected string _loadingTextValue;
		#if MM_UI
		protected Image _progressBarImage;
		#endif

		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static MMTweenType _tween;
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static bool _editorMode = false;
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		protected static string _sceneToLoadPath = "";

		/// <summary>
		/// Call this static method to load a scene from anywhere
		/// </summary>
		/// <param name="sceneToLoad">Level name.</param>
		/// <param name="editorMode">if true, when in the editor, the loading screen and destination scenes will be loaded via EditorSceneManager.LoadSceneInPlayMode / LoadSceneAsyncInPlayMode, letting you load scenes that aren't in the build settings. Scene names can then be either a scene name or its full asset path (Assets/.../MyScene.unity). This has no effect in builds.</param>
		public static void LoadScene(string sceneToLoad, bool editorMode = false)
		{
			if (LoadingScreenSceneName!=null)
			{
				if (!LoadLoadingScreenScene(sceneToLoad, LoadingScreenSceneName, editorMode))
				{
					return;
				}
				LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadStarted);
			}
		}

		/// <summary>
		/// Call this static method to load a scene from anywhere
		/// </summary>
		/// <param name="sceneToLoad">Level name.</param>
		/// <param name="loadingSceneName">the name of the loading screen scene to use</param>
		/// <param name="editorMode">if true, when in the editor, the loading screen and destination scenes will be loaded via EditorSceneManager.LoadSceneInPlayMode / LoadSceneAsyncInPlayMode, letting you load scenes that aren't in the build settings. Scene names can then be either a scene name or its full asset path (Assets/.../MyScene.unity). This has no effect in builds.</param>
		public static void LoadScene(string sceneToLoad, string loadingSceneName, bool editorMode = false)
		{
			LoadLoadingScreenScene(sceneToLoad, loadingSceneName, editorMode);
		}

		/// <summary>
		/// Stores the scene to load and loads the loading screen scene, via EditorSceneManager.LoadSceneInPlayMode if in editor mode, via the regular SceneManager otherwise
		/// Returns false if, in editor mode, one of the scenes couldn't be found in the project
		/// </summary>
		/// <param name="sceneToLoad"></param>
		/// <param name="loadingSceneName"></param>
		/// <param name="editorMode"></param>
		/// <returns></returns>
		protected static bool LoadLoadingScreenScene(string sceneToLoad, string loadingSceneName, bool editorMode)
		{
			_editorMode = false;
			#if UNITY_EDITOR
				if (editorMode)
				{
					string sceneToLoadPath = MMScene.GetEditorScenePath(sceneToLoad);
					if (string.IsNullOrEmpty(sceneToLoadPath))
					{
						Debug.LogError("MMSceneLoadingManager : impossible to load the '"+sceneToLoad+"' scene, " +
						               "there is no such scene in the project.");
						return false;
					}
					string loadingScreenScenePath = MMScene.GetEditorScenePath(loadingSceneName);
					if (string.IsNullOrEmpty(loadingScreenScenePath))
					{
						Debug.LogError("MMSceneLoadingManager : impossible to load the '"+loadingSceneName+"' scene, " +
						               "there is no such scene in the project.");
						return false;
					}
					_editorMode = true;
					_sceneToLoadPath = sceneToLoadPath;
					_sceneToLoad = System.IO.Path.GetFileNameWithoutExtension(sceneToLoadPath);
					Application.backgroundLoadingPriority = ThreadPriority.High;
					EditorSceneManager.LoadSceneInPlayMode(loadingScreenScenePath, new LoadSceneParameters(LoadSceneMode.Single));
					return true;
				}
			#endif

			_sceneToLoad = sceneToLoad;
			Application.backgroundLoadingPriority = ThreadPriority.High;
			SceneManager.LoadScene(loadingSceneName);
			return true;
		}

		/// <summary>
		/// Starts the async load of the destination scene, via EditorSceneManager.LoadSceneAsyncInPlayMode if in editor mode, via the regular SceneManager otherwise
		/// </summary>
		/// <returns></returns>
		protected virtual AsyncOperation LoadDestinationSceneAsync()
		{
			#if UNITY_EDITOR
				if (_editorMode)
				{
					return EditorSceneManager.LoadSceneAsyncInPlayMode(_sceneToLoadPath, new LoadSceneParameters(LoadSceneMode.Single));
				}
			#endif

			return SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Single);
		}

		/// <summary>
		/// On Start(), we start loading the new level asynchronously
		/// </summary>
		protected virtual void Start()
		{
			_tween = new MMTweenType(MMTween.MMTweenCurve.EaseOutCubic);
			#if MM_UI
			_progressBarImage = LoadingProgressBar.GetComponent<Image>();
			_loadingTextValue = LoadingText.text;
			#endif
			if (!string.IsNullOrEmpty(_sceneToLoad))
			{
				StartCoroutine(LoadAsynchronously());
			}        
		}

		/// <summary>
		/// Every frame, we fill the bar smoothly according to loading progress
		/// </summary>
		protected virtual void Update()
		{
			Time.timeScale = 1f;
			#if MM_UI
				_progressBarImage.fillAmount = MMMaths.Approach(_progressBarImage.fillAmount,_fillTarget,Time.deltaTime*ProgressBarSpeed);
			#endif
		}

		/// <summary>
		/// Loads the scene to load asynchronously.
		/// </summary>
		protected virtual IEnumerator LoadAsynchronously() 
		{
			// we setup our various visual elements
			LoadingSetup();

			// we fade from black
			MMFadeOutEvent.Trigger(StartFadeDuration, _tween);
			yield return MMCoroutine.WaitFor(StartFadeDuration);
            
			// we start loading the scene
			_asyncOperation = LoadDestinationSceneAsync();
			_asyncOperation.allowSceneActivation = false;

			// while the scene loads, we assign its progress to a target that we'll use to fill the progress bar smoothly
			while (_asyncOperation.progress < 0.9f) 
			{
				_fillTarget = _asyncOperation.progress;
				yield return null;
			}
			// when the load is close to the end (it'll never reach it), we set it to 100%
			_fillTarget = 1f;

			// we wait for the bar to be visually filled to continue
			#if MM_UI
			while (!Mathf.Approximately(_progressBarImage.fillAmount, _fillTarget))
			{
				yield return null;
			}
			#endif

			// the load is now complete, we replace the bar with the complete animation
			LoadingComplete();
			yield return MMCoroutine.WaitFor(LoadCompleteDelay);

			// we fade to black
			MMFadeInEvent.Trigger(ExitFadeDuration, _tween);
			yield return MMCoroutine.WaitFor(ExitFadeDuration);

			// we switch to the new scene
			_asyncOperation.allowSceneActivation = true;
			LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadTransitionComplete);
		}

		/// <summary>
		/// Sets up all visual elements, fades from black at the start
		/// </summary>
		protected virtual void LoadingSetup() 
		{
			LoadingCompleteAnimation.alpha = 0;
			#if MM_UI
			_progressBarImage.fillAmount = 0f;
			LoadingText.text = _loadingTextValue;
			#endif
		}

		/// <summary>
		/// Triggered when the actual loading is done, replaces the progress bar with the complete animation
		/// </summary>
		protected virtual void LoadingComplete() 
		{
			LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.InterpolatedLoadProgressComplete);
			LoadingCompleteAnimation.gameObject.SetActive(true);
			#if MM_UI
			StartCoroutine(MMFade.FadeCanvasGroup(LoadingProgressBar,0.1f,0f));
			StartCoroutine(MMFade.FadeCanvasGroup(LoadingAnimation,0.1f,0f));
			StartCoroutine(MMFade.FadeCanvasGroup(LoadingCompleteAnimation,0.1f,1f));
			#endif
		}
	}
}