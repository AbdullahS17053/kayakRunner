using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;

public class Interstitial : MonoBehaviour
{
    private InterstitialAd interstitialAd;
    private bool adLoaded = false;
    private bool adShown = false; // ✅ ensures ad shows only once

    void Start()
    {
        // Initialize AdMob
        MobileAds.Initialize((InitializationStatus initStatus) => { });

        // Load ad on start
        LoadInterstitialAd();
        //ShowInterstitialAd();
    }

    public void LoadInterstitialAd()
    {
#if UNITY_ANDROID
        string adUnitId = "ca-app-pub-2779022537358935/7813408428";
#elif UNITY_IPHONE
        string adUnitId = "ca-app-pub-2779022537358935/7813408428";
#else
        string adUnitId = "unused";
#endif

        // Clean up old ad if any
        interstitialAd?.Destroy();
        interstitialAd = null;
        adLoaded = false;
        
        Debug.Log("Loading Interstitial Ad...");

        // Create an ad request
        AdRequest adRequest = new AdRequest();

        // ✅ New async Load() method
        InterstitialAd.Load(adUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Interstitial failed to load: " + error);
                    return;
                }

                Debug.Log("Interstitial loaded successfully.");
                interstitialAd = ad;
                adLoaded = true;

                // Register events
                RegisterEventHandlers(interstitialAd);
                // ✅ Show ad automatically after it is loaded, but only once
                if (!adShown)
                {
                    interstitialAd.Show();
                    adShown = true;
                }
            });
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial opened.");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial closed. Reloading...");
            LoadInterstitialAd(); // auto-reload
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial failed to show: " + error);
        };
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Showing Interstitial Ad...");
            interstitialAd.Show();
            adShown = true; // ✅ mark ad as shown
        }
        else
        {
            Debug.Log("Interstitial not ready, loading new one...");
            LoadInterstitialAd();
        }
    }

    private void OnDestroy()
    {
        interstitialAd?.Destroy();
    }
}