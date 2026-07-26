using System;
using UnityEngine;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

public class AdManager : MonoBehaviour
{
    // ---- PLACEHOLDER Interstitial Ad Unit IDs ----
    private const string AndroidInterstitialAdUnitId = "ca-app-pub-7198677674712523/2644455143";
    private const string IosInterstitialAdUnitId     = "ca-app-pub-3940256099942544/4411468910";

    private static AdManager _instance;

    /// <summary>
    /// Lazily-created, scene-persistent singleton. Accessing this from anywhere
    /// spawns the manager if it does not already exist, so no scene setup is needed.
    /// </summary>
    public static AdManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[AdManager]");
                _instance = go.AddComponent<AdManager>();
            }
            return _instance;
        }
    }

    private bool _initialized;
#if GOOGLE_MOBILE_ADS
    private InterstitialAd _interstitialAd;
#endif

    private static string InterstitialAdUnitId
    {
        get
        {
#if UNITY_IPHONE && !UNITY_EDITOR
            return IosInterstitialAdUnitId;
#else
            return AndroidInterstitialAdUnitId;
#endif
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    /// <summary>Initializes the Mobile Ads SDK and pre-loads the first interstitial.</summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
#if GOOGLE_MOBILE_ADS
        MobileAds.Initialize(_ => LoadInterstitial());
#else
        Debug.Log("[AdManager] GoogleMobileAds SDK not present. Add the GOOGLE_MOBILE_ADS " +
                  "scripting define after importing the plugin. Ads will be skipped for now.");
#endif
    }

    /// <summary>Requests a fresh interstitial ad so one is ready when needed.</summary>
    public void LoadInterstitial()
    {
#if GOOGLE_MOBILE_ADS
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        var request = new AdRequest();
        InterstitialAd.Load(InterstitialAdUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdManager] Interstitial failed to load: {error}");
                return;
            }
            _interstitialAd = ad;
        });
#endif
    }

    /// <summary>
    /// Shows an interstitial ad and invokes <paramref name="onClosed"/> once the ad is
    /// dismissed (or fails to present). If no ad is ready — or the SDK is not installed —
    /// the callback runs immediately so game flow is never blocked.
    /// </summary>
    public void ShowInterstitial(Action onClosed)
    {
#if GOOGLE_MOBILE_ADS
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            var invoked = false;

            void Continue()
            {
                if (invoked) return;
                invoked = true;
                LoadInterstitial(); // pre-load the next one
                onClosed?.Invoke();
            }

            _interstitialAd.OnAdFullScreenContentClosed += Continue;
            _interstitialAd.OnAdFullScreenContentFailed += (AdError _) => Continue();
            _interstitialAd.Show();
            return;
        }

        // No ad available yet: queue one up and continue without blocking.
        LoadInterstitial();
        onClosed?.Invoke();
#else
        onClosed?.Invoke();
#endif
    }
}
