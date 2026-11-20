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

    // Anuncio recompensado ("ver un anuncio para continuar"). Hay que crear el bloque en AdMob
    // y pegar aquí su ID; mientras esté vacío, la versión de Play no ofrece continuar.
    private const string RewardedUnitId = "";
    private const string TestRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";

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

    private RewardedAd _rewarded;
    private DateTime _rewardedLoadedAt;
    private bool _rewardedLoading;
    private int _rewardedRetry;
    private bool _rewardEarned;
    private Action<bool> _afterRewarded;

    /// <summary>
    /// Plataformas con anuncios. En WebGL/escritorio (solo para previsualizar) el SDK no existe
    /// y lanzaría excepciones, así que el gestor queda inactivo y el juego sigue sin anuncios.
    /// </summary>
#if UNITY_ANDROID || UNITY_IOS
    private static readonly bool AdsSupported = true;
#else
    private static readonly bool AdsSupported = false;
#endif

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (AdsSupported) RequestConsentThenInitialize();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            _ad?.Destroy();
            _rewarded?.Destroy();
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
        // Compra "Sin anuncios": nunca intersticiales.
        if (GameSettings.AdsRemoved || _instance == null || !_instance.TryShow(continueGame))
        {
            continueGame();
        }
    }

    /// <summary>¿Se puede ofrecer "continuar viendo un anuncio" ahora mismo?</summary>
    public static bool RewardedReady
    {
        get
        {
            // Sin SDK (WebGL/escritorio) las builds de desarrollo simulan el anuncio para probar el flujo.
            if (!AdsSupported) return Debug.isDebugBuild;
            var i = _instance;
            return i != null && i._sdkReady && !i._showing && i._rewarded != null && i._rewarded.CanShowAd()
                   && (DateTime.UtcNow - i._rewardedLoadedAt).TotalSeconds <= AdExpirySeconds;
        }
    }

    /// <summary>
    /// Muestra el anuncio recompensado. <paramref name="done"/> recibe true solo si el jugador
    /// lo vio hasta ganar la recompensa (se llama una vez, al cerrar el anuncio).
    /// </summary>
    public static void ShowRewarded(Action<bool> done)
    {
        if (!AdsSupported)
        {
            done(Debug.isDebugBuild);
            return;
        }
        var i = _instance;
        if (i == null || !RewardedReady)
        {
            done(false);
            return;
        }
        i._showing = true;
        i._rewardEarned = false;
        i._afterRewarded = done;
        i._rewarded.Show(_ => OnMainThread(() => i._rewardEarned = true));
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

        MobileAds.Initialize(_ => OnMainThread(() =>
        {
            // El SDK exige inicializar antes de fijar el volumen: llamarlo antes lanzaba
            // IllegalStateException y cortaba la inicialización (nunca cargaban anuncios).
            MobileAds.SetApplicationVolume(0.2f);
            _sdkReady = true;
            if (!GameSettings.AdsRemoved) LoadAd(); // precarga: se mostrará como pronto tras varias partidas
            LoadRewarded();
        }));
    }

    /// <summary>Cargar un anuncio levanta el WebView del SDK y puede trabar: nunca en pleno vuelo.</summary>
    private static bool PlayerIsFlying => LogicScript.Instance != null && LogicScript.Instance.State == GameState.Playing;

    private void LoadAd()
    {
        if (!_sdkReady || _loading || _showing || GameSettings.AdsRemoved) return;
        if (PlayerIsFlying)
        {
            Invoke(nameof(LoadAd), 5f);
            return;
        }
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

    private void LoadRewarded()
    {
        string unitId = Debug.isDebugBuild ? TestRewardedUnitId : RewardedUnitId;
        if (!_sdkReady || _rewardedLoading || string.IsNullOrEmpty(unitId)) return;
        if (PlayerIsFlying)
        {
            Invoke(nameof(LoadRewarded), 5f);
            return;
        }
        _rewardedLoading = true;

        _rewarded?.Destroy();
        _rewarded = null;

        RewardedAd.Load(unitId, new AdRequest(), (ad, error) => OnMainThread(() =>
        {
            _rewardedLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("Recompensado no cargado: " + error);
                _rewardedRetry++;
                Invoke(nameof(LoadRewarded), Mathf.Min(300f, 15f * Mathf.Pow(2, _rewardedRetry)));
                return;
            }
            _rewardedRetry = 0;
            _rewarded = ad;
            _rewardedLoadedAt = DateTime.UtcNow;
            ad.OnAdFullScreenContentClosed += () => OnMainThread(OnRewardedFinished);
            ad.OnAdFullScreenContentFailed += err => OnMainThread(() =>
            {
                Debug.LogWarning("Recompensado no se pudo mostrar: " + err);
                OnRewardedFinished();
            });
        }));
    }

    private void OnRewardedFinished()
    {
        _showing = false;
        // Acaba de ver un anuncio: el próximo intersticial espera el ciclo completo.
        _gamesSinceLastAd = 0;
        _lastAdTime = Time.realtimeSinceStartup;
        var done = _afterRewarded;
        _afterRewarded = null;
        bool earned = _rewardEarned;
        _rewardEarned = false;
        LoadRewarded();
        done?.Invoke(earned);
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

    /// <summary>True si UMP exige ofrecer el botón de privacidad (se muestra en Opciones).</summary>
    public static bool PrivacyOptionsRequired
    {
        get
        {
            if (!AdsSupported || _instance == null) return false;
            try
            {
                return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public static void ShowPrivacyOptions()
    {
        if (!AdsSupported) return;
        ConsentForm.ShowPrivacyOptionsForm(err =>
        {
            if (err != null) Debug.LogWarning("UMP: " + err.Message);
        });
    }
}
