using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using UnityEngine.Purchasing;
#endif

/// <summary>
/// Compra única "Sin anuncios" (Unity IAP 5 sobre Google Play Billing).
///
/// - Producto no consumible <see cref="ProductId"/>; debe existir con ese mismo ID en Play Console.
/// - Al iniciar consulta las compras del jugador: restaura la compra en un teléfono nuevo y la
///   revoca si Google la reembolsó. Mientras tanto usa la copia local (funciona sin conexión).
/// - Quita los intersticiales. El anuncio recompensado sigue disponible porque es opcional.
/// - En WebGL no hay tienda: <see cref="Available"/> es false y el botón no se muestra.
/// </summary>
public class NoAdsStore : MonoBehaviour
{
    public const string ProductId = "remove_ads";

    public static NoAdsStore Instance { get; private set; }

#pragma warning disable CS0067 // en WebGL no hay tienda y los eventos nunca se disparan
    /// <summary>Cambió el estado (precio cargado, compra hecha, revocada…). La UI se refresca.</summary>
    public static event Action Changed;
    /// <summary>Resultado de un intento de compra: true = comprado.</summary>
    public static event Action<bool> PurchaseFinished;
#pragma warning restore CS0067

    public static bool AdsRemoved => GameSettings.AdsRemoved;
    public static bool Available => Instance != null && Instance._product != null;
    public static string PriceText => Instance?._price ?? "";

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private PurchasingController _controller;
    private string _store;
    private Product _product;
#else
    private object _product;
#endif
    private string _price;
    private bool _buying;

    public static void EnsureCreated()
    {
        if (Instance != null) return;
        var go = new GameObject("No Ads Store");
        DontDestroyOnLoad(go);
        go.AddComponent<NoAdsStore>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        Connect();
#endif
    }

    public static void Buy()
    {
        if (Instance == null || AdsRemoved || Instance._buying) return;
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (Instance._product == null) return;
        Instance._buying = true;
        Instance._controller.PurchaseProduct(Instance._product);
#endif
    }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private async void Connect()
    {
        try
        {
            _store = UnityIAPServices.GetDefaultStore();
            _controller = new PurchasingController(_store);
            _controller.OnStoreConnected += OnStoreConnected;
            _controller.OnStoreDisconnected += (store, d) => Debug.LogWarning("IAP: tienda desconectada: " + d.message);
            _controller.OnProductsFetched += OnProductsFetched;
            _controller.OnProductsFetchFailed += (store, f) => Debug.LogWarning("IAP: no se cargó el producto: " + f.FailureReason);
            _controller.OnPurchasesFetched += OnPurchasesFetched;
            _controller.OnPurchasesFetchFailed += (store, f) => Debug.LogWarning("IAP: no se pudieron consultar compras: " + f.message);
            _controller.OnPurchasePending += OnPurchasePending;
            _controller.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _controller.OnPurchaseFailed += OnPurchaseFailed;
            _controller.OnPurchaseDeferred += (store, order) => Debug.Log("IAP: compra pendiente de aprobación (pago diferido).");
            await _controller.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning("IAP no disponible: " + e.Message);
        }
    }

    private void OnStoreConnected(string store)
    {
        _controller.FetchProducts(new List<ProductDefinition> { new ProductDefinition(ProductId, ProductType.NonConsumable) }, store);
    }

    private void OnProductsFetched(string store, List<Product> products)
    {
        _product = products.FirstOrDefault(p => p.definition.id == ProductId && p.availableToPurchase);
        _price = _product?.metadata.localizedPriceString;
        _controller.FetchPurchases(store);
        Changed?.Invoke();
    }

    private void OnPurchasesFetched(string store, Orders orders)
    {
        bool owned = orders.ConfirmedOrders.Any(Contains) || orders.PendingOrders.Any(Contains);
        // La consulta respondió: es la verdad de Google (restaura o revoca por reembolso).
        GameSettings.AdsRemoved = owned;
        foreach (var pending in orders.PendingOrders.Where(Contains)) _controller.ConfirmPurchase(pending);
        Changed?.Invoke();
    }

    private void OnPurchasePending(string store, PendingOrder order)
    {
        if (!Contains(order)) return;
        // Se otorga primero y luego se confirma; si la app se cierra antes, Google la reenvía.
        GameSettings.AdsRemoved = true;
        _controller.ConfirmPurchase(order);
        FinishBuying(true);
    }

    private void OnPurchaseConfirmed(string store, Order order)
    {
        if (order is FailedOrder failed)
            Debug.LogWarning("IAP: confirmación fallida: " + failed.FailureReason + " " + failed.Details);
    }

    private void OnPurchaseFailed(string store, FailedOrder order)
    {
        Debug.Log("IAP: compra no completada: " + order.FailureReason);
        FinishBuying(false);
    }

    private void FinishBuying(bool success)
    {
        bool wasBuying = _buying;
        _buying = false;
        Changed?.Invoke();
        if (wasBuying) PurchaseFinished?.Invoke(success);
    }

    private static bool Contains(Order order) =>
        order.CartOrdered.Items().Any(i => i.Product?.definition.id == ProductId);
#endif
}
