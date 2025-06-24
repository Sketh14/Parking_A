/// <summary>
/// Extension of original Code by Nikkolai Davenport <nikkolai@unity3d.com> 
/// </summary>

//https://docs.unity3d.com/Packages/com.unity.ads@3.4/manual/MonetizationBasicIntegrationUnity.html#testing

#define TESTING
#define DELAY_TESTING

using System;
using System.Threading.Tasks;
using System.Threading;

using UnityEngine;

#if UNITY_IOS || UNITY_ANDROID
using UnityEngine.Advertisements;
#endif

namespace Parking_A.Global
{

	public class UnityAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
	{
		public enum AdStatus
		{
			AD_NOT_LOADED = 0, LOADING_AD = 1 << 0, AD_LOADED = 1 << 1, SHOWING_AD = 1 << 2,
			AD_COMPLETED = 1 << 3, AD_SKIPPED = 1 << 4,
			AD_ERROR = 1 << 5, AD_NOT_SUPPORTED = 1 << 6, USER_REQUESTED_AD = 1 << 7
		}
		public enum AdType { INTERSTITIAL_AD, REWARDED_AD }

#if TESTING
		private const string _IOSGAMEID = "5883760";
		private const string _ANDROIDGAMEID = "5883761";
		private readonly string[] _ADIDS = new string[] { "Interstitial_Android", "Rewarded_Android" };
#else
		private const string _IOSGAMEID = "1234";
		private const string _ANDROIDGAMEID = "1234";
		private readonly string[] _ADIDS = new string[] { "Interstitial_Id", "Rewarded_Id" };
#endif

		private AdType _currentAdType;
		public AdStatus CurrentAdsStatus;
		private int _loadFailCount;

		public bool enableTestMode = true;

		private CancellationTokenSource _cts;

		public Action<AdType> OnAdRequested;
		public Action<AdStatus> OnAdStatusChange;

		#region Singleton
		private static UnityAdsManager _instance;
		public static UnityAdsManager Instance { get => _instance; }

		private void Awake()
		{
			if (_instance == null)
				_instance = this;
			else
				Destroy(gameObject);
		}
		#endregion Singleton


		//--- Unity Ads Setup and Initialization

		private void OnDestroy()
		{
			if (_cts != null) _cts.Cancel();

			OnAdRequested -= ShowAd;
		}

		private void Start()
		{
			_currentAdType = AdType.REWARDED_AD;
			CurrentAdsStatus = AdStatus.AD_NOT_LOADED;
			_loadFailCount = 0;
			_cts = new CancellationTokenSource();
			InitializeAds();

			OnAdRequested += ShowAd;
		}

		private void InitializeAds()
		{
			Debug.Log("Running precheck for Unity Ads initialization...");

			string gameID = null;

#if UNITY_IOS
			gameID = _IOSGAMEID;
#elif UNITY_ANDROID
			gameID = _ANDROIDGAMEID;
#endif

			if (!Advertisement.isSupported)
				Debug.LogWarning("Unity Ads is not supported on the current runtime platform.");
			else if (Advertisement.isInitialized)
			{
				_loadFailCount = 0;
				CurrentAdsStatus &= ~AdStatus.AD_ERROR;
				Debug.LogWarning("Unity Ads is already initialized.");
			}
			else if (string.IsNullOrEmpty(gameID))
				Debug.LogError("The game ID value is not set. A valid game ID is required to initialize Unity Ads.");
			else
			{
				if (enableTestMode && !Debug.isDebugBuild)
					Debug.LogWarning("Development Build must be enabled in Build Settings to enable test mode for Unity Ads.");

				bool isTestModeEnabled = Debug.isDebugBuild && enableTestMode;
				Debug.Log(string.Format("Precheck done. Initializing Unity Ads for game ID {0} with test mode {1}...",
										gameID, isTestModeEnabled ? "enabled" : "disabled"));

#if !UNITY_IOS || !UNITY_ANDROID
				Debug.LogWarning($"Unity Ads test will not work without an internet connection in Editor");
#endif

				Advertisement.Initialize(gameID, isTestModeEnabled, this);
			}
		}

		#region IUnityAdsInitializationListener

		public void OnInitializationComplete()
		{
			Debug.Log($"Unity Ads Initialized!");

			_loadFailCount = 0;
			CurrentAdsStatus &= ~AdStatus.AD_ERROR;

			Advertisement.Load(_ADIDS[(int)_currentAdType], this);

			CurrentAdsStatus |= AdStatus.LOADING_AD;
		}

		public async void OnInitializationFailed(UnityAdsInitializationError errorType, string message)
		{
			Debug.LogWarning($"Error Initializing Ads! | ErrorTyep: {errorType} | ErrorMsg: {message}");
			switch (errorType)
			{
				case UnityAdsInitializationError.AD_BLOCKER_DETECTED:
					//Show user to disable Ads blocker

					break;

				case UnityAdsInitializationError.INTERNAL_ERROR:
					//Try to Initialize again
					_loadFailCount++;
					if (_loadFailCount > 4)
					{
						Debug.LogError($"Load failed | _loadFailCount: {_loadFailCount}");
						CurrentAdsStatus |= AdStatus.AD_ERROR;
						return;
					}

					await Task.Delay(2000);
					if (_cts.IsCancellationRequested) return;

					// Debug.Log($"SDK not properly initialized | Initializing again | _loadFailCount: {_loadFailCount}");
					InitializeAds();

					break;

				case UnityAdsInitializationError.INVALID_ARGUMENT:
					Debug.LogError($"Invalid Parameters! | Check GameId");
					break;

				case UnityAdsInitializationError.UNKNOWN:
					goto case UnityAdsInitializationError.INTERNAL_ERROR;
			}
		}

		#endregion IUnityAdsInitializationListener

		#region IUnityAdsLoadListener

		public async void OnUnityAdsAdLoaded(string placementId)
		{
			CurrentAdsStatus &= ~AdStatus.AD_NOT_LOADED;
			CurrentAdsStatus |= AdStatus.AD_LOADED;

#if DELAY_TESTING
			await Task.Delay(5000);         //Awaiting if any system needs to use the CurrentAdsStatus flag
#endif

			OnAdStatusChange?.Invoke(AdStatus.AD_LOADED);

			Debug.Log($"{_currentAdType} Loaded");
		}

		public async void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError errorType, string message)
		{
			Debug.LogError($"Failed to load Ad | Id: {placementId} | ErrorType: {errorType} | ErrorMsg: {message}");

			switch (errorType)
			{
				case UnityAdsLoadError.INITIALIZE_FAILED:
					// Debug.Log($"SDK not properly initialized | Initializing again");

					// Try initializing again
					InitializeAds();

					break;

				case UnityAdsLoadError.INTERNAL_ERROR:
				case UnityAdsLoadError.TIMEOUT:
				case UnityAdsLoadError.UNKNOWN:
					//Try to load Ads again
					Advertisement.Load(_ADIDS[(int)_currentAdType], this);

					break;

				case UnityAdsLoadError.INVALID_ARGUMENT:
					Debug.LogError($"Invalid Parameters! | Check AdId");
					break;

				case UnityAdsLoadError.NO_FILL:
					if ((CurrentAdsStatus & AdStatus.USER_REQUESTED_AD) != 0)
					{
						//Show something to user to try Ads after some time as they are loading
					}

					AdType requestedAdType = _currentAdType;
					await Task.Delay(2000);
					if (_cts.IsCancellationRequested) return;

					Advertisement.Load(_ADIDS[(int)requestedAdType], this);

					break;
			}
		}

		#endregion IUnityAdsLoadListener

		#region IUnityAdsShowListener

		public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError errorType, string message)
		{
			Debug.LogWarning($"Failed to Show Ad | Id: {placementId} | ErrorType: {errorType} | ErrorMsg: {message}");

			switch (errorType)
			{
				case UnityAdsShowError.NOT_INITIALIZED:
					// Try initializing again
					InitializeAds();

					break;

				case UnityAdsShowError.NOT_READY:
					//Show something to the user to try again after some time after ad is ready

					break;

				case UnityAdsShowError.VIDEO_PLAYER_ERROR:
					//Show something to the user to wait for some time

					break;

				case UnityAdsShowError.NO_CONNECTION:
					//Show something to the user to enable the internet connection

					break;

				case UnityAdsShowError.ALREADY_SHOWING:
					break;

				case UnityAdsShowError.INTERNAL_ERROR:
				case UnityAdsShowError.UNKNOWN:
					//Try user to try to load Ads again

					break;

				case UnityAdsShowError.INVALID_ARGUMENT:
					Debug.LogError($"Invalid Parameters! | Check AdId");
					break;
			}
		}

		public void OnUnityAdsShowStart(string placementId) { }

		public void OnUnityAdsShowClick(string placementId)
		{
			CurrentAdsStatus |= AdStatus.SHOWING_AD;
		}

		public async void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
		{
			switch (showCompletionState)
			{
				case UnityAdsShowCompletionState.COMPLETED:
					CurrentAdsStatus |= AdStatus.AD_COMPLETED;
					OnAdStatusChange?.Invoke(AdStatus.AD_COMPLETED);

#if DELAY_TESTING
					await Task.Delay(5000);         //Awaiting if any system needs to use the CurrentAdsStatus flag
#else
					await Task.Delay(1000);         //Awaiting if any system needs to use the CurrentAdsStatus flag
#endif

					if (_cts.IsCancellationRequested) return;

					//Resetting flags
					CurrentAdsStatus = AdStatus.AD_NOT_LOADED;
					Advertisement.Load(_ADIDS[(int)_currentAdType], this);

					break;

				case UnityAdsShowCompletionState.SKIPPED:
					CurrentAdsStatus |= AdStatus.AD_SKIPPED;
					OnAdStatusChange?.Invoke(AdStatus.AD_SKIPPED);

#if DELAY_TESTING
					await Task.Delay(5000);         //Awaiting if any system needs to use the CurrentAdsStatus flag
#else
					await Task.Delay(1000);         //Awaiting if any system needs to use the CurrentAdsStatus flag
#endif

					if (_cts.IsCancellationRequested) return;

					//Resetting flags
					CurrentAdsStatus = AdStatus.AD_NOT_LOADED;
					Advertisement.Load(_ADIDS[(int)_currentAdType], this);

					break;

				case UnityAdsShowCompletionState.UNKNOWN:


					//Resetting flags
					CurrentAdsStatus = AdStatus.AD_NOT_LOADED;
					Advertisement.Load(_ADIDS[(int)_currentAdType], this);

					break;
			}
		}

		#endregion IUnityAdsShowListener

		//--- Static Helper Methods

		public static bool isShowing { get { return Advertisement.isShowing; } }
		public static bool isSupported { get { return Advertisement.isSupported; } }
		public static bool isInitialized { get { return Advertisement.isInitialized; } }

		public void ShowAd(AdType adType)
		{
			_currentAdType = adType;

			if ((CurrentAdsStatus & AdStatus.AD_NOT_SUPPORTED) != 0)
				Debug.LogError("Failed to show ad. Unity Ads is not supported under the current build platform.");
			else if ((CurrentAdsStatus & AdStatus.AD_ERROR) != 0)
				InitializeAds();
			else if ((CurrentAdsStatus & AdStatus.AD_NOT_LOADED) != 0)
				Advertisement.Load(_ADIDS[(int)adType], this);
			else
				Advertisement.Show(_ADIDS[(int)adType], this);
		}
	}

}