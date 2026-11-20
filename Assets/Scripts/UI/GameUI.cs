using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Interfaz del juego construida por código (una sola fuente de verdad, fácil de mantener):
/// menú, "toca para volar", HUD, pausa, opciones y fin de partida.
/// Lienzo de referencia 1280x720 ajustado por alto; respeta el área segura (notch).
/// </summary>
public class GameUI : MonoBehaviour
{
    // Paleta de UI (la misma de los edificios).
    public static readonly Color White = new Color(0.96f, 0.93f, 1f);
    public static readonly Color Pink = new Color(1f, 0.2f, 0.63f);
    public static readonly Color Cyan = new Color(0.25f, 0.96f, 0.82f);
    public static readonly Color Amber = new Color(1f, 0.83f, 0.42f);
    public static readonly Color Dim = new Color(0.55f, 0.48f, 0.75f);
    public static readonly Color Ink = new Color(0.05f, 0.01f, 0.15f);

    private LogicScript _logic;
    private RectTransform _safe;

    private GameObject _menu, _ready, _hud, _pause, _options, _over, _pilot;
    private RectTransform _popups;
    private PixelText _multiplier, _readyMode, _overMode, _overMissions, _bossName, _bossWarning, _menuPilot;
    private readonly Dictionary<GameMode, PixelText> _modeBest = new Dictionary<GameMode, PixelText>();
    private Image _puIcon, _puBar, _shieldIcon, _bossBar;
    private GameObject _puGroup, _bossGroup;
    private float _bossWarningTime;
    private readonly Dictionary<PowerUp, Sprite> _puIcons = new Dictionary<PowerUp, Sprite>();
    private readonly List<Action> _pilotRefreshers = new List<Action>();
    private GameObject _optionsReturnTo;
    private PixelText _score, _readyHint, _newBest, _overScore, _overBest, _rankName, _nextRank;
    private Image _flash, _overMedal;
    private Button _continue, _retry, _menuNoAds, _menuRanking, _overRanking;
    private PixelText _continueText, _toast;
    private float _toastTime;
    private float _scorePunch;
    private readonly List<Action> _optionRefreshers = new List<Action>();

    private Sprite _panel, _button, _buttonAlt;

    public static GameUI Create(LogicScript logic)
    {
        var go = new GameObject("Game UI");
        var ui = go.AddComponent<GameUI>();
        ui._logic = logic;
        ui.Build();
        return ui;
    }

    // ================================================================== construcción

    private void Build()
    {
        _panel = Resources.Load<Sprite>("Pixel/ui_panel");
        _button = Resources.Load<Sprite>("Pixel/ui_button");
        _buttonAlt = Resources.Load<Sprite>("Pixel/ui_button_alt");

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.pixelPerfect = false;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1f;
        scaler.referencePixelsPerUnit = 100;
        gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        _safe = Node("Safe Area", transform);
        Stretch(_safe);
        ApplySafeArea();

        BuildMenu();
        BuildReady();
        BuildHud();
        BuildPause();
        BuildOptions();
        BuildGameOver();
        BuildPilot();

        _popups = Node("Popups", transform);
        Stretch(_popups);

        var flash = Node("Flash", transform);
        Stretch(flash);
        _flash = flash.gameObject.AddComponent<Image>();
        _flash.color = Color.clear;
        _flash.raycastTarget = false;

        var toast = Node("Toast", transform);
        Place(toast, new Vector2(0.5f, 0f), new Vector2(0, 260), new Vector2(900, 50));
        _toast = toast.gameObject.AddComponent<PixelText>();
        _toast.PixelSize = 5;
        _toast.color = Amber;
        _toast.Glow = true;
        _toast.GlowColor = new Color(0.05f, 0.01f, 0.15f, 0.9f);
        toast.gameObject.SetActive(false);

        NoAdsStore.Changed += RefreshStoreButtons;
        NoAdsStore.PurchaseFinished += OnPurchaseFinished;
        HideAll();
        RefreshStoreButtons();
    }

    private Rect _appliedSafeArea;

    private void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        _appliedSafeArea = safe;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        _safe.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _safe.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
    }

    private void BuildMenu()
    {
        _menu = MakeScreen("Menu").gameObject;
        var r = (RectTransform)_menu.transform;

        var title = Label(r, "FLAPPY", 15, White, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(900, 130));
        title.Glow = true;
        title.GlowColor = new Color(0.61f, 0.13f, 0.85f, 0.8f);
        var year = Label(r, "2077", 21, Pink, new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(900, 170));
        year.Glow = true;
        year.GlowColor = new Color(0.35f, 0.0f, 0.48f, 0.9f);
        LabelKey(r, "tagline", 4, Cyan, new Vector2(0.5f, 1f), new Vector2(0, -368), new Vector2(900, 40));

        // Modos de juego: cada uno con su récord debajo.
        var modes = new[] { GameMode.Classic, GameMode.Arcade, GameMode.Daily };
        for (int i = 0; i < modes.Length; i++)
        {
            var mode = modes[i];
            float x = (i - 1) * 270f; // cabe en 4:3 (960 de ancho de lienzo)
            MakeButton(r, GameModes.Key(mode), new Vector2(0.5f, 0f), new Vector2(x, 236), new Vector2(250, 92), mode != GameMode.Classic, () =>
            {
                Sfx.Click();
                _logic.Play(mode);
            });
            _modeBest[mode] = Label(r, "", 4, Amber, new Vector2(0.5f, 0f), new Vector2(x, 168), new Vector2(250, 36));
        }
        MakeButton(r, "options", new Vector2(0.5f, 0f), new Vector2(-170, 76), new Vector2(300, 72), false, () =>
        {
            Sfx.Click();
            OpenOptions(_menu);
        });
        var pilot = MakeButton(r, "pilot", new Vector2(0.5f, 0f), new Vector2(170, 76), new Vector2(300, 72), false, () =>
        {
            Sfx.Click();
            OpenPilot();
        });
        _menuPilot = pilot.GetComponentInChildren<PixelText>();

        _menuNoAds = MakeButton(r, "no_ads", new Vector2(0f, 1f), new Vector2(190, -62), new Vector2(320, 72), false, () =>
        {
            Sfx.Click();
            NoAdsStore.Buy();
        });
        _menuRanking = MakeIconButton(r, "Pixel/ic_trophy", new Vector2(1f, 1f), new Vector2(-70, -66), new Vector2(92, 92), () =>
        {
            Sfx.Click();
            Leaderboard.Show();
        });

    }

    private void BuildReady()
    {
        _ready = MakeScreen("Ready").gameObject;
        var r = (RectTransform)_ready.transform;
        _readyHint = LabelKey(r, "tap_to_fly", 9, White, new Vector2(0.5f, 0.5f), new Vector2(160, -150), new Vector2(760, 90));
        _readyHint.Glow = true;
        _readyHint.gameObject.AddComponent<Blink>();
        if (!Application.isMobilePlatform)
            LabelKey(r, "tap_hint_keys", 4, Dim, new Vector2(0.5f, 0.5f), new Vector2(160, -220), new Vector2(760, 40));

        _readyMode = Label(r, "", 4, Cyan, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1000, 40));

        // Flecha pixel que apunta al auto.
        var arrow = Label(r, "<<", 7, Pink, new Vector2(0.5f, 0.5f), new Vector2(-95, 40), new Vector2(120, 70));
        arrow.gameObject.AddComponent<Bob>().Amplitude = new Vector2(-12, 0);
    }

    private void BuildHud()
    {
        _hud = MakeScreen("HUD").gameObject;
        var r = (RectTransform)_hud.transform;
        _score = Label(r, "0", 13, White, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(500, 120));
        _score.Glow = true;
        _score.GlowColor = new Color(1f, 0.2f, 0.63f, 0.6f);

        var pause = MakeIconButton(r, "Pixel/ic_pause", new Vector2(1f, 1f), new Vector2(-70, -66), new Vector2(92, 92), () =>
        {
            Sfx.Click();
            _logic.Pause();
        });
        pause.name = "Pause Button";

        _multiplier = Label(r, "", 6, Pink, new Vector2(0.5f, 1f), new Vector2(0, -158), new Vector2(240, 46));
        _multiplier.Glow = true;
        _multiplier.GlowColor = new Color(0.35f, 0f, 0.48f, 0.9f);

        foreach (PowerUp pu in new[] { PowerUp.Shield, PowerUp.Magnet, PowerUp.Slow, PowerUp.Double })
            _puIcons[pu] = Resources.Load<Sprite>("Pixel/ic_pu_" + pu.ToString().ToLowerInvariant());
        _puGroup = Node("PowerUp", r).gameObject;
        var pg = (RectTransform)_puGroup.transform;
        Place(pg, new Vector2(0f, 1f), new Vector2(150, -66), new Vector2(220, 70));
        _puIcon = Icon(pg, null, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(52, 52));
        var barBg = Node("Bar bg", pg);
        Place(barBg, new Vector2(0f, 0.5f), new Vector2(140, 0), new Vector2(140, 16));
        barBg.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.03f, 0.25f, 0.9f);
        var bar = Node("Bar", barBg);
        bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(0f, 1f); bar.pivot = new Vector2(0f, 0.5f);
        bar.offsetMin = bar.offsetMax = Vector2.zero; bar.sizeDelta = new Vector2(140, 0);
        _puBar = bar.gameObject.AddComponent<Image>();
        _puBar.raycastTarget = false;
        _shieldIcon = Icon(r, "Pixel/ic_pu_shield", new Vector2(0f, 1f), new Vector2(54, -140), new Vector2(52, 52));

        _bossGroup = Node("Boss", r).gameObject;
        var bg = (RectTransform)_bossGroup.transform;
        Place(bg, new Vector2(0.5f, 1f), new Vector2(0, -214), new Vector2(560, 70));
        _bossName = Label(bg, "", 4, Pink, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(560, 30));
        var bbg = Node("Bar bg", bg);
        Place(bbg, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(520, 18));
        bbg.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.03f, 0.25f, 0.9f);
        var bb = Node("Bar", bbg);
        bb.anchorMin = new Vector2(0f, 0f); bb.anchorMax = new Vector2(0f, 1f); bb.pivot = new Vector2(0f, 0.5f);
        bb.offsetMin = bb.offsetMax = Vector2.zero; bb.sizeDelta = new Vector2(520, 0);
        _bossBar = bb.gameObject.AddComponent<Image>();
        _bossBar.color = Pink;
        _bossBar.raycastTarget = false;

        _bossWarning = Label(r, "", 8, Pink, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1100, 140));
        _bossWarning.Glow = true;
        _bossWarning.GlowColor = new Color(0.35f, 0f, 0.48f, 0.9f);
        _bossWarning.gameObject.AddComponent<Blink>().Period = 0.4f;
        ResetHud();
    }

    private void ResetHud()
    {
        _multiplier.text = "";
        _puGroup.SetActive(false);
        _shieldIcon.gameObject.SetActive(false);
        _bossGroup.SetActive(false);
        _bossWarning.gameObject.SetActive(false);
    }

    private void BuildPause()
    {
        _pause = MakeScreen("Pause").gameObject;
        var r = (RectTransform)_pause.transform;
        Dimmer(r, 0.55f);
        var panel = Panel(r, new Vector2(0, 0), new Vector2(560, 470));
        LabelKey(panel, "paused", 10, White, new Vector2(0.5f, 1f), new Vector2(0, -78), new Vector2(480, 90)).Glow = true;
        MakeButton(panel, "resume", new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(380, 84), true, () =>
        {
            Sfx.Click();
            _logic.Resume();
        });
        MakeButton(panel, "options", new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(380, 76), false, () =>
        {
            Sfx.Click();
            OpenOptions(_pause);
        });
        MakeButton(panel, "menu", new Vector2(0.5f, 1f), new Vector2(0, -382), new Vector2(380, 76), false, () =>
        {
            Sfx.Back();
            _logic.GoToMenu();
        });
    }

    private void BuildOptions()
    {
        _options = MakeScreen("Options").gameObject;
        var r = (RectTransform)_options.transform;
        Dimmer(r, 0.7f);
        var panel = Panel(r, new Vector2(0, 0), new Vector2(920, 680));
        LabelKey(panel, "options", 9, White, new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(700, 80)).Glow = true;

        float y = -146f;
        const float row = 66f;

        VolumeRow(panel, "music", y, () => GameSettings.MusicSteps, v => GameSettings.MusicSteps = v);
        y -= row;
        VolumeRow(panel, "sfx", y, () => GameSettings.SfxSteps, v =>
        {
            GameSettings.SfxSteps = v;
            Sfx.Toggle(); // se oye el nivel nuevo
        });
        y -= row;

        SelectorRow(panel, "track", y,
            () => GameSettings.MusicTrack == GameSettings.Shuffle ? Loc.Get("shuffle") : Loc.Format("track_n", GameSettings.MusicTrack + 1),
            dir =>
            {
                int count = pickMusic.Instance != null ? pickMusic.Instance.TrackCount : 10;
                // Ciclo: ALEATORIA, 1, 2, ... n, ALEATORIA ...
                int v = GameSettings.MusicTrack + 1;      // 0 = aleatoria, 1..n = pistas
                v = (v + dir + count + 1) % (count + 1);
                GameSettings.MusicTrack = v - 1;
                pickMusic.Instance?.ApplyTrackChoice();
            });
        y -= row;

        SelectorRow(panel, "language", y, () => Loc.LanguageName(Loc.Current), dir =>
        {
            int n = Loc.LanguageCount;
            Loc.SetLanguage((Language)(((int)Loc.Current + dir + n) % n));
        });
        y -= row;

        if (Application.isMobilePlatform)
        {
            ToggleRow(panel, "vibration", y, () => GameSettings.Vibration, v =>
            {
                GameSettings.Vibration = v;
                if (v) Haptics.Pulse(40);
            });
            y -= row;
        }
        ToggleRow(panel, "shake", y, () => GameSettings.ScreenShake, v =>
        {
            GameSettings.ScreenShake = v;
            if (v) Fx.Instance?.Shake(0.15f, 0.25f);
        });
        y -= row;
        NoAdsRow(panel, y);

        var privacy = MakeButton(panel, "privacy", new Vector2(0f, 0f), new Vector2(190, 62), new Vector2(300, 64), false, () =>
        {
            Sfx.Click();
            Interstitial.ShowPrivacyOptions();
        });
        _optionRefreshers.Add(() => privacy.gameObject.SetActive(Interstitial.PrivacyOptionsRequired));

        MakeButton(panel, "back", new Vector2(0.5f, 0f), new Vector2(0, 62), new Vector2(300, 72), true, () =>
        {
            Sfx.Back();
            CloseOptions();
        });
        var version = LabelKey(panel, "version", 3, Dim, new Vector2(1f, 0f), new Vector2(-150, 56), new Vector2(250, 30));
        version.SetKey("version", Application.version);

        Loc.Changed += RefreshOptions;
    }

    private void BuildGameOver()
    {
        _over = MakeScreen("Game Over").gameObject;
        var r = (RectTransform)_over.transform;
        Dimmer(r, 0.35f);
        var title = LabelKey(r, "game_over", 11, Pink, new Vector2(0.5f, 1f), new Vector2(0, -64), new Vector2(900, 100));
        title.Glow = true;
        title.GlowColor = new Color(0.35f, 0.0f, 0.48f, 0.9f);

        _overMode = Label(r, "", 3, Cyan, new Vector2(0.5f, 1f), new Vector2(0, -118), new Vector2(700, 26));
        _overMissions = Label(r, "", 4, Amber, new Vector2(0.5f, 0.5f), new Vector2(0, -118), new Vector2(1000, 34));
        _newBest = LabelKey(r, "new_best", 5, Amber, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(700, 44));
        _newBest.gameObject.AddComponent<Blink>();

        var panel = Panel(r, new Vector2(0, 40), new Vector2(640, 260));
        LabelKey(panel, "score", 4, Dim, new Vector2(0f, 1f), new Vector2(190, -44), new Vector2(300, 36));
        _overScore = Label(panel, "0", 10, White, new Vector2(0f, 1f), new Vector2(190, -108), new Vector2(320, 86));
        LabelKey(panel, "best", 4, Dim, new Vector2(0f, 1f), new Vector2(190, -168), new Vector2(300, 36));
        _overBest = Label(panel, "0", 6, Amber, new Vector2(0f, 1f), new Vector2(190, -214), new Vector2(320, 52));

        _overMedal = Icon(panel, null, new Vector2(1f, 1f), new Vector2(-160, -100), new Vector2(110, 110));
        _rankName = Label(panel, "", 4, White, new Vector2(1f, 1f), new Vector2(-160, -186), new Vector2(290, 36));
        _nextRank = Label(panel, "", 3, Dim, new Vector2(1f, 1f), new Vector2(-160, -226), new Vector2(290, 30));

        _continue = MakeButton(r, "continue_ad", new Vector2(0.5f, 0f), new Vector2(0, 172), new Vector2(560, 80), true, () =>
        {
            Sfx.Click();
            _logic.ContinueWithAd();
        });
        _continueText = _continue.GetComponentInChildren<PixelText>();

        _retry = MakeButton(r, "retry", new Vector2(0.5f, 0f), new Vector2(-200, 62), new Vector2(360, 80), true, () =>
        {
            Sfx.Click();
            _logic.restartGame();
        });
        MakeButton(r, "menu", new Vector2(0.5f, 0f), new Vector2(200, 62), new Vector2(360, 80), false, () =>
        {
            Sfx.Back();
            _logic.GoToMenu();
        });
        _overRanking = MakeIconButton(r, "Pixel/ic_trophy", new Vector2(1f, 1f), new Vector2(-70, -66), new Vector2(92, 92), () =>
        {
            Sfx.Click();
            Leaderboard.Show();
        });
    }

    // ================================================================== API de pantallas

    private void HideAll()
    {
        _menu.SetActive(false);
        _ready.SetActive(false);
        _hud.SetActive(false);
        _pause.SetActive(false);
        _options.SetActive(false);
        _over.SetActive(false);
        _pilot.SetActive(false);
    }

    public void ShowMenu()
    {
        HideAll();
        foreach (var kv in _modeBest)
        {
            int best = GameSettings.BestFor(kv.Key);
            kv.Value.text = kv.Key == GameMode.Daily
                ? Loc.Format("today_best", GameModes.DailyLabel, best)
                : Loc.Get("best") + " " + best;
        }
        _menuPilot.text = Loc.Format("pilot_level", GameSettings.Level);
        Show(_menu);
    }

    public void ShowReady(int score)
    {
        HideAll();
        _score.text = score.ToString();
        var mode = LogicScript.Mode;
        _readyMode.text = mode == GameMode.Arcade ? Loc.Get("hint_arcade")
            : mode == GameMode.Daily ? Loc.Format("hint_daily", GameModes.DailyLabel) : "";
        Show(_ready);
        _hud.SetActive(true);
        _hud.transform.Find("Pause Button")?.gameObject.SetActive(false);
    }

    public void ShowHud()
    {
        HideAll();
        if (LogicScript.Mode == GameMode.Classic) ResetHud();
        _hud.SetActive(true);
        _hud.transform.Find("Pause Button")?.gameObject.SetActive(true);
    }

    public void HideHud() => _hud.SetActive(false);

    public void ShowPause()
    {
        _hud.SetActive(false);
        Show(_pause);
    }

    public void SetScore(int score, bool big)
    {
        _score.text = score.ToString();
        _scorePunch = big ? 1f : 0.5f;
    }

    public void ShowGameOver(int score, int best, bool newBest, List<Mission> missions)
    {
        HideAll();
        Show(_over);
        var mode = LogicScript.Mode;
        bool canContinue = _logic.CanContinue;
        _continue.gameObject.SetActive(canContinue);
        SetContinueBusy(false);
        _continueText.SetKey("continue_ad");
        // El botón principal (magenta) es "continuar" si se ofrece; si no, "otra vez".
        _retry.GetComponent<Image>().sprite = canContinue ? _buttonAlt : _button;
        _overRanking.gameObject.SetActive(Leaderboard.Available);
        _newBest.gameObject.SetActive(newBest);
        _overBest.text = best.ToString();
        _overMode.text = mode == GameMode.Daily ? Loc.Format("hint_daily", GameModes.DailyLabel) : Loc.Get(GameModes.Key(mode));
        var thresholds = GameModes.RankThresholds(mode);
        int rank = LogicScript.RankFor(score, mode);
        SetMedal(_overMedal, rank);
        _rankName.SetKey(LogicScript.RankKeys[rank]);
        if (rank + 1 < thresholds.Length)
        {
            _nextRank.gameObject.SetActive(true);
            _nextRank.SetKey("next_rank", thresholds[rank + 1]);
        }
        else _nextRank.gameObject.SetActive(false);

        // Misiones cumplidas y subida de nivel.
        if (missions != null && missions.Count > 0)
        {
            _overMissions.text = missions.Count == 1
                ? Loc.Format("mission_done", missions[0].Description, Missions.LastXpGained)
                : Loc.Format("missions_done", missions.Count, Missions.LastXpGained);
            _overMissions.gameObject.SetActive(true);
        }
        else _overMissions.gameObject.SetActive(false);
        if (Missions.LastLevelAfter > Missions.LastLevelBefore)
        {
            var unlocked = CarSkins.UnlockedAt(Missions.LastLevelAfter);
            Toast(unlocked != null
                ? Loc.Format("level_up_skin", Missions.LastLevelAfter, Loc.Get(unlocked.NameKey))
                : Loc.Format("level_up", Missions.LastLevelAfter));
            Sfx.NewBest();
        }
        StartCoroutine(CountUp(_overScore, score, newBest));
    }

    public void SetContinueBusy(bool busy) => _continue.interactable = !busy;

    /// <summary>El anuncio no estaba o no se completó: se quita la opción de continuar.</summary>
    public void ShowContinueUnavailable()
    {
        _continue.gameObject.SetActive(false);
        _retry.GetComponent<Image>().sprite = _button;
        Toast(Loc.Get("ad_unavailable"));
    }

    public void Toast(string text)
    {
        _toast.text = text;
        _toast.gameObject.SetActive(true);
        _toastTime = 2.2f;
    }

    private void RefreshStoreButtons()
    {
        bool showBuy = NoAdsStore.Available && !NoAdsStore.AdsRemoved;
        _menuNoAds.gameObject.SetActive(showBuy);
        if (showBuy) _menuNoAds.GetComponentInChildren<PixelText>().SetKey("no_ads_price", NoAdsStore.PriceText);
        _menuRanking.gameObject.SetActive(Leaderboard.Available);
        if (_overRanking != null) _overRanking.gameObject.SetActive(Leaderboard.Available);
        RefreshOptions();
    }

    private void OnPurchaseFinished(bool success)
    {
        Toast(Loc.Get(success ? "thanks" : "purchase_failed"));
        if (success) Sfx.NewBest();
    }

    /// <summary>Cierra submenús con el botón atrás. Devuelve true si consumió el evento.</summary>
    public bool HandleBack()
    {
        if (_options.activeSelf)
        {
            Sfx.Back();
            CloseOptions();
            return true;
        }
        if (_pilot.activeSelf)
        {
            Sfx.Back();
            ShowMenu();
            return true;
        }
        return false;
    }

    public void Flash(Color color) => StartCoroutine(FlashRoutine(color));

    // ================================================================== arcade: HUD y textos flotantes

    public void SetArcadeHud(ArcadeDirector d)
    {
        if (!_hud.activeSelf) return;
        int m = d.Multiplier * (d.TimedPower == PowerUp.Double ? 2 : 1);
        _multiplier.text = m > 1 ? "x" + m : "";
        bool timed = d.TimedPower != PowerUp.None;
        _puGroup.SetActive(timed);
        if (timed)
        {
            _puIcon.sprite = _puIcons[d.TimedPower];
            _puBar.rectTransform.sizeDelta = new Vector2(140f * d.TimedFraction, 0f);
            _puBar.color = d.TimedPower == PowerUp.Magnet ? new Color(1f, 0.36f, 0.36f)
                : d.TimedPower == PowerUp.Slow ? new Color(0.26f, 0.55f, 1f) : Amber;
        }
        _shieldIcon.gameObject.SetActive(d.HasShield);
    }

    /// <summary>"+N" flotante sobre un punto del mundo.</summary>
    public void PopPoints(Vector3 world, string text, Color color) => PopText(world, text, color);

    /// <summary>Texto flotante (puntos, "¡PERFECTO!", power-up) que sube y se desvanece.</summary>
    public void PopText(Vector3 world, string text, Color color)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector2 screen = cam.WorldToScreenPoint(world);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_popups, screen, null, out var local)) return;
        var t = Label(_popups, text, 4, color, new Vector2(0.5f, 0.5f), local, new Vector2(400, 40));
        t.GlowColor = new Color(0.05f, 0.01f, 0.15f, 0.9f);
        t.Glow = true;
        StartCoroutine(FloatAway(t));
    }

    private static IEnumerator FloatAway(PixelText t)
    {
        var rt = t.rectTransform;
        Vector2 start = rt.anchoredPosition;
        Color c = t.color;
        float time = 0f;
        while (time < 0.8f && t != null)
        {
            time += Time.unscaledDeltaTime;
            rt.anchoredPosition = start + Vector2.up * (60f * time / 0.8f);
            c.a = 1f - Mathf.Clamp01((time - 0.4f) / 0.4f);
            t.color = c;
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    public void ShowBossWarning(string bossName)
    {
        _bossWarning.text = Loc.Get("boss_alert") + "\n" + bossName;
        _bossWarning.gameObject.SetActive(true);
        _bossWarningTime = 2.2f;
    }

    public void ShowBossBar(string bossName)
    {
        _bossName.text = bossName;
        _bossGroup.SetActive(true);
        SetBossHealth(1f);
    }

    public void SetBossHealth(float fraction) => _bossBar.rectTransform.sizeDelta = new Vector2(520f * Mathf.Clamp01(fraction), 0f);

    public void HideBossBar() => _bossGroup.SetActive(false);

    // ================================================================== piloto: nivel, misiones y colores

    private void BuildPilot()
    {
        _pilot = MakeScreen("Pilot").gameObject;
        var r = (RectTransform)_pilot.transform;
        Dimmer(r, 0.7f);
        var panel = Panel(r, Vector2.zero, new Vector2(920, 690));

        var level = Label(panel, "", 7, White, new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(800, 60));
        level.Glow = true;
        var xpBg = Node("XP bg", panel);
        Place(xpBg, new Vector2(0.5f, 1f), new Vector2(0, -102), new Vector2(600, 18));
        xpBg.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.03f, 0.25f, 0.9f);
        var xp = Node("XP", xpBg);
        xp.anchorMin = new Vector2(0f, 0f); xp.anchorMax = new Vector2(0f, 1f); xp.pivot = new Vector2(0f, 0.5f);
        xp.offsetMin = xp.offsetMax = Vector2.zero;
        var xpImg = xp.gameObject.AddComponent<Image>();
        xpImg.color = Cyan;
        var xpText = Label(panel, "", 3, Dim, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(600, 26));
        _pilotRefreshers.Add(() =>
        {
            var (cur, need) = GameSettings.LevelProgress();
            level.text = Loc.Format("pilot_level", GameSettings.Level);
            xp.sizeDelta = new Vector2(600f * cur / need, 0f);
            xpText.text = cur + " / " + need + " XP";
        });

        LabelKey(panel, "missions", 5, Pink, new Vector2(0.5f, 1f), new Vector2(0, -176), new Vector2(600, 40));
        for (int i = 0; i < Missions.Slots; i++)
        {
            int index = i;
            float y = -224 - i * 52;
            var desc = Label(panel, "", 4, White, new Vector2(0f, 1f), new Vector2(310, y), new Vector2(540, 36));
            desc.Alignment = PixelText.Align.Left;
            var prog = Label(panel, "", 4, Amber, new Vector2(1f, 1f), new Vector2(-160, y), new Vector2(240, 36));
            prog.Alignment = PixelText.Align.Right;
            _pilotRefreshers.Add(() =>
            {
                var list = Missions.Active;
                if (index >= list.Count) return;
                var m = list[index];
                desc.text = m.Description;
                prog.text = m.Progress + "/" + m.Target + "  +" + m.Xp;
            });
        }

        LabelKey(panel, "colors", 5, Pink, new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(600, 40));
        for (int i = 0; i < CarSkins.All.Length; i++)
        {
            var skin = CarSkins.All[i];
            float x = (i - (CarSkins.All.Length - 1) / 2f) * 106f;
            var b = ButtonBase(panel, "Skin " + i, new Vector2(0.5f, 1f), new Vector2(x, -490), new Vector2(98, 106), _buttonAlt, () =>
            {
                if (!skin.Unlocked)
                {
                    Sfx.Back();
                    Toast(Loc.Format("skin_locked", skin.UnlockLevel));
                    return;
                }
                Sfx.Toggle();
                GameSettings.SelectedSkin = skin.Index;
                _logic.carScript.ApplySkin(skin);
                RefreshPilot();
            });
            var img = Icon((RectTransform)b.transform, null, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(88, 62));
            img.sprite = skin.Up ?? _logic.carScript.carUp;
            if (skin.Holo) img.color = new Color(0.8f, 1f, 1f);
            var lockText = Label((RectTransform)b.transform, "", 3, Amber, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(96, 24));
            var bimg = b.GetComponent<Image>();
            _pilotRefreshers.Add(() =>
            {
                bool selected = CarSkins.Selected.Index == skin.Index;
                bimg.sprite = selected ? _button : _buttonAlt;
                img.color = skin.Unlocked ? (skin.Holo ? new Color(0.8f, 1f, 1f) : Color.white) : new Color(0.2f, 0.15f, 0.35f, 0.9f);
                lockText.text = skin.Unlocked ? "" : Loc.Format("level_short", skin.UnlockLevel);
            });
        }
        var skinName = Label(panel, "", 4, Cyan, new Vector2(0.5f, 1f), new Vector2(0, -572), new Vector2(600, 30));
        _pilotRefreshers.Add(() => skinName.text = Loc.Get(CarSkins.Selected.NameKey));

        MakeButton(panel, "back", new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(300, 68), true, () =>
        {
            Sfx.Back();
            ShowMenu();
        });
    }

    private void OpenPilot()
    {
        HideAll();
        RefreshPilot();
        Show(_pilot);
    }

    private void RefreshPilot()
    {
        foreach (var refresh in _pilotRefreshers) refresh();
    }

    // ================================================================== opciones

    private void OpenOptions(GameObject from)
    {
        _optionsReturnTo = from;
        from.SetActive(false);
        RefreshOptions();
        Show(_options);
    }

    private void CloseOptions()
    {
        _options.SetActive(false);
        if (_optionsReturnTo == _menu) ShowMenu();
        else if (_optionsReturnTo != null) Show(_optionsReturnTo);
    }

    private void RefreshOptions()
    {
        foreach (var refresh in _optionRefreshers) refresh();
        if (_menu.activeSelf) ShowMenu();
    }

    private void VolumeRow(RectTransform panel, string key, float y, Func<int> get, Action<int> set)
    {
        RowLabel(panel, key, y);
        var blocks = new Image[GameSettings.VolumeSteps];
        float x0 = 60f;
        for (int i = 0; i < blocks.Length; i++)
        {
            int level = i + 1;
            var b = Node("Step " + level, panel);
            Place(b, new Vector2(0.5f, 1f), new Vector2(x0 + i * 30f, y), new Vector2(22, 40));
            var img = b.gameObject.AddComponent<Image>();
            var btn = b.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                set(get() == level ? level - 1 : level); // tocar el último bloque lo apaga
                RefreshOptions();
            });
            blocks[i] = img;
        }
        MakeTextButton(panel, "-", new Vector2(0.5f, 1f), new Vector2(x0 - 62f, y), new Vector2(64, 56), () =>
        {
            set(get() - 1);
            RefreshOptions();
        });
        MakeTextButton(panel, "+", new Vector2(0.5f, 1f), new Vector2(x0 + 30f * blocks.Length + 32f, y), new Vector2(64, 56), () =>
        {
            set(get() + 1);
            RefreshOptions();
        });
        _optionRefreshers.Add(() =>
        {
            int v = get();
            for (int i = 0; i < blocks.Length; i++)
            {
                bool on = i < v;
                float t = i / (float)(blocks.Length - 1);
                blocks[i].color = on ? Color.Lerp(Cyan, Pink, t) : new Color(0.18f, 0.1f, 0.35f, 1f);
                blocks[i].rectTransform.sizeDelta = new Vector2(22, on ? 26 + i * 2.2f : 22);
            }
        });
    }

    private void SelectorRow(RectTransform panel, string key, float y, Func<string> value, Action<int> change)
    {
        RowLabel(panel, key, y);
        var text = Label(panel, "", 5, Cyan, new Vector2(0.5f, 1f), new Vector2(205, y), new Vector2(280, 50));
        MakeTextButton(panel, "<", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(64, 56), () =>
        {
            Sfx.Toggle();
            change(-1);
            RefreshOptions();
        });
        MakeTextButton(panel, ">", new Vector2(0.5f, 1f), new Vector2(410, y), new Vector2(64, 56), () =>
        {
            Sfx.Toggle();
            change(+1);
            RefreshOptions();
        });
        _optionRefreshers.Add(() => text.text = value());
    }

    private void ToggleRow(RectTransform panel, string key, float y, Func<bool> get, Action<bool> set)
    {
        RowLabel(panel, key, y);
        Button button = null;
        PixelText text = null;
        button = MakeTextButton(panel, "", new Vector2(0.5f, 1f), new Vector2(205, y), new Vector2(200, 56), () =>
        {
            Sfx.Toggle();
            set(!get());
            RefreshOptions();
        });
        text = button.GetComponentInChildren<PixelText>();
        var img = button.GetComponent<Image>();
        _optionRefreshers.Add(() =>
        {
            bool on = get();
            text.text = Loc.Get(on ? "on" : "off");
            text.color = on ? White : Dim;
            img.sprite = on ? _button : _buttonAlt;
        });
    }

    private void RowLabel(RectTransform panel, string key, float y)
    {
        var l = LabelKey(panel, key, 5, White, new Vector2(0f, 1f), new Vector2(225, y), new Vector2(330, 50));
        l.Alignment = PixelText.Align.Left;
    }

    // ================================================================== animación

    private void Update()
    {
        // Al girar el teléfono (landscape izquierda/derecha) el notch cambia de lado.
        if (Screen.safeArea != _appliedSafeArea) ApplySafeArea();
        if (_bossWarningTime > 0f)
        {
            _bossWarningTime -= Time.unscaledDeltaTime;
            if (_bossWarningTime <= 0f) _bossWarning.gameObject.SetActive(false);
        }
        if (_toastTime > 0f)
        {
            _toastTime -= Time.unscaledDeltaTime;
            if (_toastTime <= 0f) _toast.gameObject.SetActive(false);
        }
        _scorePunch = Mathf.MoveTowards(_scorePunch, 0f, Time.unscaledDeltaTime * 4f);
        if (_score != null) _score.rectTransform.localScale = Vector3.one * (1f + _scorePunch * 0.25f);
    }

    private void Show(GameObject screen)
    {
        screen.SetActive(true);
        StartCoroutine(PopIn((RectTransform)screen.transform));
    }

    private static IEnumerator PopIn(RectTransform rt)
    {
        var group = rt.GetComponent<CanvasGroup>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.18f;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
            if (group != null) group.alpha = e;
            yield return null;
        }
        rt.localScale = Vector3.one;
        if (group != null) group.alpha = 1f;
    }

    private IEnumerator CountUp(PixelText text, int target, bool newBest)
    {
        float duration = Mathf.Clamp(target * 0.04f, 0.2f, 1.2f);
        float t = 0f;
        int shown = -1;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            int v = Mathf.RoundToInt(Mathf.Lerp(0, target, Mathf.Clamp01(t / duration)));
            if (v != shown)
            {
                shown = v;
                text.text = v.ToString();
            }
            yield return null;
        }
        text.text = target.ToString();
        if (newBest) Sfx.NewBest();
    }

    private IEnumerator FlashRoutine(Color color)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.35f;
            var c = color;
            c.a *= 1f - Mathf.Clamp01(t);
            _flash.color = c;
            yield return null;
        }
        _flash.color = Color.clear;
    }

    private void OnDestroy()
    {
        Loc.Changed -= RefreshOptions;
        NoAdsStore.Changed -= RefreshStoreButtons;
        NoAdsStore.PurchaseFinished -= OnPurchaseFinished;
    }

    private void NoAdsRow(RectTransform panel, float y)
    {
        var label = LabelKey(panel, "no_ads", 5, White, new Vector2(0f, 1f), new Vector2(225, y), new Vector2(330, 50));
        label.Alignment = PixelText.Align.Left;
        var button = MakeTextButton(panel, "", new Vector2(0.5f, 1f), new Vector2(205, y), new Vector2(280, 56), () =>
        {
            Sfx.Click();
            NoAdsStore.Buy();
        });
        var text = button.GetComponentInChildren<PixelText>();
        _optionRefreshers.Add(() =>
        {
            bool owned = NoAdsStore.AdsRemoved;
            // Sin tienda (navegador) y sin compra: la fila no se muestra.
            bool visible = owned || NoAdsStore.Available;
            button.gameObject.SetActive(visible);
            label.gameObject.SetActive(visible);
            button.interactable = !owned;
            text.text = owned ? Loc.Get("no_ads_owned") : NoAdsStore.PriceText;
            text.color = owned ? Cyan : White;
            button.GetComponent<Image>().sprite = owned ? _buttonAlt : _button;
        });
    }

    // ================================================================== fábrica de widgets

    private static RectTransform Node(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private RectTransform MakeScreen(string name)
    {
        var rt = Node(name, _safe);
        Stretch(rt);
        rt.gameObject.AddComponent<CanvasGroup>();
        return rt;
    }

    private void Dimmer(RectTransform parent, float alpha)
    {
        var d = Node("Dim", parent);
        // Más allá del área segura: cubre toda la pantalla.
        d.anchorMin = new Vector2(-0.5f, -0.5f);
        d.anchorMax = new Vector2(1.5f, 1.5f);
        d.offsetMin = d.offsetMax = Vector2.zero;
        var img = d.gameObject.AddComponent<Image>();
        img.color = new Color(0.03f, 0f, 0.1f, alpha);
    }

    private RectTransform Panel(RectTransform parent, Vector2 pos, Vector2 size)
    {
        var p = Node("Panel", parent);
        Place(p, new Vector2(0.5f, 0.5f), pos, size);
        var img = p.gameObject.AddComponent<Image>();
        img.sprite = _panel;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.4f;
        return p;
    }

    private static PixelText Label(RectTransform parent, string text, float pixel, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = Node("Text", parent);
        Place(rt, anchor, pos, size);
        var t = rt.gameObject.AddComponent<PixelText>();
        t.PixelSize = pixel;
        t.color = color;
        t.text = text;
        return t;
    }

    private static PixelText LabelKey(RectTransform parent, string key, float pixel, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var t = Label(parent, "", pixel, color, anchor, pos, size);
        t.name = "Text " + key;
        t.SetKey(key);
        return t;
    }

    private static Image Icon(RectTransform parent, string path, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = Node("Icon", parent);
        Place(rt, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        if (path != null) img.sprite = Resources.Load<Sprite>(path);
        return img;
    }

    private static void SetMedal(Image img, int rank)
    {
        string[] medals = { null, "medal_bronze", "medal_silver", "medal_gold", "medal_neon" };
        string m = medals[Mathf.Clamp(rank, 0, medals.Length - 1)];
        img.gameObject.SetActive(m != null);
        if (m != null) img.sprite = Resources.Load<Sprite>("Pixel/" + m);
    }

    private Button MakeButton(RectTransform parent, string key, Vector2 anchor, Vector2 pos, Vector2 size, bool primary, Action onClick)
    {
        var b = ButtonBase(parent, key, anchor, pos, size, primary ? _button : _buttonAlt, onClick);
        var t = Label((RectTransform)b.transform, "", primary ? 6 : 5, primary ? White : White, new Vector2(0.5f, 0.5f), new Vector2(0, 3), size - new Vector2(40, 24));
        t.SetKey(key);
        return b;
    }

    private Button MakeTextButton(RectTransform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
    {
        var b = ButtonBase(parent, "Button " + text, anchor, pos, size, _buttonAlt, onClick);
        Label((RectTransform)b.transform, text, 5, White, new Vector2(0.5f, 0.5f), new Vector2(0, 2), size - new Vector2(20, 16));
        return b;
    }

    private Button MakeIconButton(RectTransform parent, string iconPath, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
    {
        var b = ButtonBase(parent, "Icon Button", anchor, pos, size, _buttonAlt, onClick);
        Icon((RectTransform)b.transform, iconPath, new Vector2(0.5f, 0.5f), new Vector2(0, 3), size * 0.5f);
        return b;
    }

    private Button ButtonBase(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, Action onClick)
    {
        var rt = Node(name, parent);
        Place(rt, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.25f;
        var btn = rt.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.9f);
        colors.fadeDuration = 0.05f;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());
        rt.gameObject.AddComponent<PressScale>();
        return btn;
    }
}

/// <summary>Encoge el botón mientras se presiona (respuesta táctil).</summary>
public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private float _target = 1f;

    public void OnPointerDown(PointerEventData e) => _target = 0.93f;
    public void OnPointerUp(PointerEventData e) => _target = 1f;
    public void OnPointerExit(PointerEventData e) => _target = 1f;

    private void OnDisable()
    {
        _target = 1f;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        float s = Mathf.MoveTowards(transform.localScale.x, _target, Time.unscaledDeltaTime * 2.5f);
        transform.localScale = new Vector3(s, s, 1f);
    }
}

/// <summary>Parpadeo en escalones (estilo arcade).</summary>
public class Blink : MonoBehaviour
{
    public float Period = 0.9f;
    private Graphic _g;

    private void Awake() => _g = GetComponent<Graphic>();

    private void Update()
    {
        if (_g == null) return;
        var c = _g.color;
        c.a = Mathf.Repeat(Time.unscaledTime, Period) < Period * 0.65f ? 1f : 0.25f;
        _g.color = c;
    }
}

/// <summary>Vaivén suave (flecha de "toca para volar").</summary>
public class Bob : MonoBehaviour
{
    public Vector2 Amplitude = new Vector2(0, 8);
    public float Speed = 5f;
    private Vector2 _home;
    private RectTransform _rt;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _home = _rt.anchoredPosition;
    }

    private void Update()
    {
        float s = Mathf.Round(Mathf.Sin(Time.unscaledTime * Speed) * 2f) / 2f; // escalones de medio paso
        _rt.anchoredPosition = _home + Amplitude * s;
    }
}
