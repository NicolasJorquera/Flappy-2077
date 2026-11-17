using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

/// <summary>
/// Gestor único de anuncios intersticiales (sobrevive a la recarga de escena).
///
/// Reglas para cumplir la política Better Ads / anuncios intrusivos de Google Play:
/// - Nunca al abrir la app, al volver a ella ni durante una partida.
/// - Solo en una pausa natural: cuando el jugador pulsa "reintentar" en la pantalla
///   de game over, antes de empezar la partida siguiente.
/// - Como máximo uno cada <see cref="MinGamesBetweenAds"/> partidas terminadas y cada
///   <see cref="MinSecondsBetweenAds"/> segundos, y nunca en los primeros
///   <see cref="MinSecondsAfterLaunch"/> segundos de uso.
/// - Si el anuncio no está ya cargado, no se espera: el juego sigue sin anuncio.
/// - Antes de pedir anuncios se obtiene el consentimiento con UMP (GDPR/US states).
/// </summary>
public class Interstitial : MonoBehaviour
{
    public const int MinGamesBetweenAds = 3;
    public const float MinSecondsBetweenAds = 180f;
    public const float MinSecondsAfterLaunch = 120f;

    private const string AdUnitId = "ca-app-pub-3038495459046159/8975863890";
    // Bloque de prueba oficial de Google, usado en builds de desarrollo.
    private const string TestAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    private const double AdExpirySeconds = 55 * 60; // los intersticiales caducan a la hora

    private static Interstitial _instance;

    private InterstitialAd _ad;
    private DateTime _adLoadedAt;
    private bool _initStarted;
    private bool _sdkReady;
    private bool _loading;
    private bool _showing;
    private int _retryCount;
    private int _gamesSinceLastAd;
    private float _lastAdTime = float.NegativeInfinity;
    private Action _afterAd;

    /// <summary>La pantalla de inicio lo activa para mostrar el botón de privacidad si hace falta.</summary>
    public static bool PrivacyButtonVisible { get; set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        RequestConsentThenInitialize();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            _ad?.Destroy();
        }
    }

    // ---------- API usada por el juego ----------

    /// <summary>Llamar una vez por partida terminada (game over).</summary>
    public static void NotifyGameFinished()
    {
        if (_instance != null) _instance._gamesSinceLastAd++;
    }

    /// <summary>
    /// Pausa natural tras el game over. Muestra un intersticial solo si toca según las reglas
    /// y ya está cargado; en cualquier caso, <paramref name="continueGame"/> se ejecuta una vez
    /// (al cerrar el anuncio o de inmediato si no hay anuncio).
    /// </summary>
    public static void ShowAtBreakThen(Action continueGame)
    {
        if (_instance == null || !_instance.TryShow(continueGame))
        {
            continueGame();
        }
    }

    // ---------- Lógica interna ----------

    private bool IsDueForAd()
    {
        float now = Time.realtimeSinceStartup;
        return _gamesSinceLastAd >= MinGamesBetweenAds
               && now >= MinSecondsAfterLaunch
               && now - _lastAdTime >= MinSecondsBetweenAds;
    }

    private bool TryShow(Action continueGame)
    {
        if (_showing) return true; // ya hay un anuncio en pantalla; el cierre continuará el juego
        if (!_sdkReady || !IsDueForAd()) return false;

        if (_ad == null || !_ad.CanShowAd() || (DateTime.UtcNow - _adLoadedAt).TotalSeconds > AdExpirySeconds)
        {
            LoadAd();
            return false;
        }

        _showing = true;
        _afterAd = continueGame;
        _ad.Show();
        return true;
    }

    private void OnAdFinished()
    {
        _showing = false;
        _gamesSinceLastAd = 0;
        _lastAdTime = Time.realtimeSinceStartup;

        var next = _afterAd;
        _afterAd = null;
        LoadAd();
        next?.Invoke();
    }

    private void RequestConsentThenInitialize()
    {
        var request = new ConsentRequestParameters();
        ConsentInformation.Update(request, updateError => OnMainThread(() =>
        {
            if (updateError != null)
            {
                Debug.LogWarning("UMP: no se pudo actualizar el consentimiento: " + updateError.Message);
                InitializeIfAllowed();
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMainThread(() =>
            {
                if (formError != null)
                {
                    Debug.LogWarning("UMP: error en el formulario de consentimiento: " + formError.Message);
                }
                InitializeIfAllowed();
            }));
        }));

        // Con consentimiento de una sesión anterior se puede empezar ya, sin esperar.
        InitializeIfAllowed();
    }

    private void InitializeIfAllowed()
    {
        if (_initStarted || !ConsentInformation.CanRequestAds()) return;
        _initStarted = true;

        MobileAds.SetApplicationVolume(0.2f);
        MobileAds.Initialize(_ => OnMainThread(() =>
        {
            _sdkReady = true;
            LoadAd(); // precarga: se mostrará como pronto tras varias partidas
        }));
    }

    private void LoadAd()
    {
        if (!_sdkReady || _loading || _showing) return;
        _loading = true;

        _ad?.Destroy();
        _ad = null;

        string unitId = Debug.isDebugBuild ? TestAdUnitId : AdUnitId;
        InterstitialAd.Load(unitId, new AdRequest(), (ad, error) => OnMainThread(() =>
        {
            _loading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("Intersticial no cargado: " + error);
                _retryCount++;
                float delay = Mathf.Min(300f, 15f * Mathf.Pow(2, _retryCount));
                Invoke(nameof(LoadAd), delay);
                return;
            }

            _retryCount = 0;
            _ad = ad;
            _adLoadedAt = DateTime.UtcNow;
            ad.OnAdFullScreenContentClosed += () => OnMainThread(OnAdFinished);
            ad.OnAdFullScreenContentFailed += err => OnMainThread(() =>
            {
                Debug.LogWarning("Intersticial no se pudo mostrar: " + err);
                OnAdFinished();
            });
        }));
    }

    // Los callbacks del SDK pueden llegar desde otro hilo; la lógica de Unity va en el principal.
    private static readonly Queue<Action> MainThreadQueue = new Queue<Action>();

    private static void OnMainThread(Action action)
    {
        lock (MainThreadQueue) MainThreadQueue.Enqueue(action);
    }

    private void Update()
    {
        while (true)
        {
            Action next;
            lock (MainThreadQueue)
            {
                if (MainThreadQueue.Count == 0) break;
                next = MainThreadQueue.Dequeue();
            }
            next();
        }
    }

    // ---------- Opciones de privacidad (requisito de UMP) ----------

    private void OnGUI()
    {
        if (!PrivacyButtonVisible || _showing) return;
        if (ConsentInformation.PrivacyOptionsRequirementStatus != PrivacyOptionsRequirementStatus.Required) return;

        float w = Screen.width * 0.32f, h = Screen.height * 0.05f;
        var style = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(h * 0.4f) };
        if (GUI.Button(new Rect(Screen.width * 0.03f, Screen.height - h - Screen.height * 0.03f, w, h), "Privacidad", style))
        {
            ConsentForm.ShowPrivacyOptionsForm(err =>
            {
                if (err != null) Debug.LogWarning("UMP: " + err.Message);
            });
        }
    }
}
