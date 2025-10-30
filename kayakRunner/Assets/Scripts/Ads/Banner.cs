using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;

public class Banner : MonoBehaviour
{
    private BannerView bannerView;
    public void Start()
    {
        // Initialize Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // This callback is called once the MobileAds SDK is initialized.
        });
        RequestBanner();
    }

    private void RequestBanner()
    {

#if UNITY_ANDROID
        string adUnitId = "ca-app-pub-2779022537358935~6745252229";
#elif UNITY_IPHONE
        string adUnitId = "ca-app-pub-2779022537358935/7320144902";
#else
        string adUnitId = "unexpected_platform";
#endif

        // Create a banner at the bottom of the screen
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Top);

        // Create an empty ad request
        AdRequest adRequest = new AdRequest();

        // Load the banner with the request
        bannerView.LoadAd(adRequest);
    }

    private void OnDestroy()
    {
        // Clean up banner when object is destroyed
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
    }
}
