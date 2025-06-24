/// <summary>
/// Extension of original Code by Nikkolai Davenport <nikkolai@unity3d.com> 
/// </summary>

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
			AD_NOT_LOADED, LOADING_AD, AD_LOADED, SHOWING_AD, AD_SEEN,
			AD_COMPLETED, AD_SKIPPED,
			AD_ERROR, AD_NOT_SUPPORTED, USER_REQUESTED_AD
		}
		public enum AdType { INTERSTITIAL_AD, REWARDED_AD }

		private const string _IOSGAMEID = "24300";
		private const string _ANDROIDGAMEID = "24299";

		private readonly string[] _ADIDS = new string[] { "Interstitial_Android", "Rewarded_Android" };
		private AdType _currentAdType;
		private AdStatus _currentAdsStatus;
		private int _loadFailCount;

		public bool enableTestMode = true;

		private CancellationTokenSource _cts;

		public Action<AdType> OnAdRequested;
		public Action<AdStatus> OnAdStatusChange;

		#region Singleton
		private static UnityAdsManager _instance;
		public static UnityAdsManager Instance;

		private void OnAwake()
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
			_currentAdType = AdType.INTERSTITIAL_AD;
			_currentAdsStatus = AdStatus.AD_NOT_LOADED;
			_loadFailCount = 0;
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
				Debug.LogWarning("Unity Ads is already initialized.");
			else if (string.IsNullOrEmpty(gameID))
				Debug.LogError("The game ID value is not set. A valid game ID is required to initialize Unity Ads.");
			else
			{
				if (enableTestMode && !Debug.isDebugBuild)
					Debug.LogWarning("Development Build must be enabled in Build Settings to enable test mode for Unity Ads.");

				bool isTestModeEnabled = Debug.isDebugBuild && enableTestMode;
				Debug.Log(string.Format("Precheck done. Initializing Unity Ads for game ID {0} with test mode {1}...",
										gameID, isTestModeEnabled ? "enabled" : "disabled"));

				Advertisement.Initialize(gameID, isTestModeEnabled, this);
#if UNITY_IOS || UNITY_ANDROID
#else
			_currentAdsStatus |= AdStatus.AD_NOT_SUPPORTED;
			Debug.LogWarning("Unity Ads is not supported under the current build platform.");
#endif
			}
		}

		#region IUnityAdsInitializationListener

		public void OnInitializationComplete()
		{
			Debug.Log($"Unity Ads Initialized!");

#if UNITY_IOS || UNITY_ANDROID
			Advertisement.Load(_ADIDS[(int)_currentAdType], this);
#endif

			_currentAdsStatus |= AdStatus.LOADING_AD;
		}

		public async void OnInitializationFailed(UnityAdsInitializationError errorType, string message)
		{
			Debug.LogError($"Error Initializing Ads! | Error: {message}");
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
						_currentAdsStatus |= AdStatus.AD_ERROR;
						return;
					}

					await Task.Delay(2000);
					InitializeAds();

					break;

				case UnityAdsInitializationError.INVALID_ARGUMENT:
					Debug.LogError($"Invalid Parameters! | Check GameId");
					break;

				case UnityAdsInitializationError.UNKNOWN:
					break;
			}
		}

		#endregion IUnityAdsInitializationListener

		#region IUnityAdsLoadListener

		public void OnUnityAdsAdLoaded(string placementId)
		{
			_currentAdsStatus &= ~AdStatus.AD_NOT_LOADED;
			_currentAdsStatus |= AdStatus.AD_LOADED;
		}

		public async void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError errorType, string message)
		{
			Debug.LogWarning($"Failed to load Ad | Id: {placementId} | ErrorType: {errorType} | ErrorMsg: {message}");

			switch (errorType)
			{
				case UnityAdsLoadError.INITIALIZE_FAILED:
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
					if ((_currentAdsStatus & AdStatus.USER_REQUESTED_AD) != 0)
					{
						//Show something to user to try Ads after some time as they are loading
					}

					AdType requestedAdType = _currentAdType;
					await Task.Delay(2000);
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
			_currentAdsStatus |= AdStatus.SHOWING_AD;
		}

		public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
		{
			_currentAdsStatus |= AdStatus.AD_SEEN;

			switch (showCompletionState)
			{
				case UnityAdsShowCompletionState.COMPLETED:
					Advertisement.Load(_ADIDS[(int)_currentAdType], this);
					_currentAdsStatus |= AdStatus.AD_COMPLETED;

					break;

				case UnityAdsShowCompletionState.SKIPPED:
					Advertisement.Load(_ADIDS[(int)_currentAdType], this);
					_currentAdsStatus |= AdStatus.AD_SKIPPED;

					break;

				case UnityAdsShowCompletionState.UNKNOWN:
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

			if ((_currentAdsStatus & AdStatus.AD_NOT_SUPPORTED) != 0)
				Debug.LogError("Failed to show ad. Unity Ads is not supported under the current build platform.");
			else if ((_currentAdsStatus & AdStatus.AD_ERROR) != 0)
				InitializeAds();
			else if ((_currentAdsStatus & AdStatus.AD_NOT_LOADED) != 0)
				Advertisement.Load(_ADIDS[(int)adType], this);
			else
				Advertisement.Show(_ADIDS[(int)adType], this);
		}
	}

}