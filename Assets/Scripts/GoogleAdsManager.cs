using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.SceneManagement;

public class GoogleAdsManager : MonoBehaviour
{
#if UNITY_ANDROID
    private string interId = "ca-app-pub-3940256099942544~3347511713";
#elif UNITY_IPHONE
    private string interId = "ca-app-pub-3940256099942544~1458002511";
#else
        private string interId = "unused";
#endif

    private InterstitialAd _interstitialAd;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(initStatus => LoadInterstitialAd());
    }

    #region Interstitial Ads
    private void LoadInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        InterstitialAd.Load(interId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            // if error is not null, the load request failed.
            if (error != null || ad == null)
            {
                Debug.LogError("interstitial ad failed to load an ad " +
                               "with error : " + error);
                // Retry after 5 seconds
                Invoke("LoadInterstitialAd", 5f);
                return;
            }

            Debug.Log("Interstitial ad loaded with response : "
                      + ad.GetResponseInfo());

            _interstitialAd = ad;
            InterstitialEvent(_interstitialAd);
            InterstitialReloadHandler(_interstitialAd);
        });
    }

    public void ShowInterstitialAd()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogError("Interstitial ad is not ready yet.");
            LoadInterstitialAd();
        }
    }

    public bool IsInterstitialAdReady()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
            return true;
        else
            return false;
    }

    private void InterstitialEvent(InterstitialAd interstitialAd)
    {
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");
            SceneManager.LoadScene("Game");
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);
        };
    }

    private void InterstitialReloadHandler(InterstitialAd interstitialAd)
    {
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
        };
    }
    #endregion

}
