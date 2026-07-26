using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudCode;

/// <summary>
/// Drives real In-App Purchases through Unity IAP (com.unity.purchasing v5) and
/// persists entitlements per-user with UGS Authentication + Cloud Save.
///
/// Persistence strategy:
///   * Cloud Save is the per-user source of truth and follows the player's
///     account across devices.
///   * The store's own purchase history (FetchPurchases) restores non-consumable
///     entitlements and is also authoritative — owned products are always granted.
///   * PlayerPrefs is kept as an offline cache so flags are available immediately
///     on launch and when the device is offline.
///
/// The public API surface (instance, flags, events, Buy* methods) is unchanged so
/// existing consumers (GameManager, WinScreen, Inspector button hooks) keep working.
/// </summary>
public class PurchaseManager : MonoBehaviour
{
    public static PurchaseManager instance;

    // -------------------------------------------------------------------------
    // Product IDs — must match the product identifiers configured on the store
    // (Google Play Console) and in the Unity Dashboard.
    // -------------------------------------------------------------------------
    public const string ProductGemBoost = "gemboost";
    public const string ProductNoAds    = "noads";

    // Cloud Save keys
    private const string KeyGemBoost = "gem_boost_active";
    private const string KeyNoAds    = "no_ads_active";
    private const string KeyGemCount = "gem_count";

    // -------------------------------------------------------------------------
    // Public state (read by other systems)
    // -------------------------------------------------------------------------
    public bool IsGemBoostActive { get; private set; }
    public bool IsNoAdsActive    { get; private set; }
    public bool IsReady          { get; private set; }

    /// <summary>The player's persisted gem balance, tied to the signed-in account.</summary>
    public int GemCount { get; private set; }

    public event Action OnPurchasesLoaded;
    public event Action<string> OnPurchaseSuccess;
    public event Action<string> OnPurchaseFailedEvent;

    /// <summary>Raised whenever the gem balance changes; passes the new total.</summary>
    public event Action<int> OnGemCountChanged;

    // -------------------------------------------------------------------------
    // IAP state
    // -------------------------------------------------------------------------
    private StoreController m_StoreController;
    private bool m_StoreConnected;
    private string activePlayerId;

    private enum ReceiptValidationResult
    {
        Valid,
        Invalid,
        Retry,
    }

    private const int MaxReceiptValidationAttempts = 3;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        _ = InitializeAsync();
        
        Debug.Log("[PurchaseManager] Awake");
    }

    private async Task InitializeAsync()
    {
        // AuthManager is the sole owner of UGS initialization and sign-in. Yield
        // once so every scene object's Awake method has run before looking up its
        // singleton; this avoids a startup race with AuthManager's Awake.
        await Task.Yield();

        // 1. Wait for the authenticated player before touching Cloud Save.
        try
        {
            if (AuthManager.instance == null)
            {
                Debug.LogWarning("[PurchaseManager] AuthManager is missing; Cloud Save will be unavailable.");
            }
            else
            {
                bool hasPlayerInfo = await AuthManager.instance.InitializeAndRefreshPlayerInfoAsync();
                if (!hasPlayerInfo)
                {
                    Debug.LogWarning("[PurchaseManager] Authentication did not complete; Cloud Save will be unavailable.");
                }
                else
                {
                    Debug.Log($"[PurchaseManager] Using authenticated player {AuthManager.instance.CurrentPlayerId}.");

                    // 2. Pull the per-user entitlements saved to this player's account.
                    await LoadCloudStateAsync();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PurchaseManager] Auth/Cloud Save startup failed, using local cache only: {e.Message}");
        }

        // 3. Initialize Unity IAP regardless — the store is the authoritative
        //    source for non-consumable ownership.
        InitializePurchasing();

        // Entitlement flags are usable now (cloud + local). Store restore, when it
        // completes, may grant additional owned products on top.
        MarkReady();
    }

    private void MarkReady()
    {
        if (IsReady) return;
        IsReady = true;
        OnPurchasesLoaded?.Invoke();
        Debug.Log($"[PurchaseManager] Ready — GemBoost={IsGemBoostActive}, NoAds={IsNoAdsActive}");
    }

    // -------------------------------------------------------------------------
    // Unity IAP (v5)
    // -------------------------------------------------------------------------

    private void InitializePurchasing()
    {
        if (m_StoreController != null) return;

        try
        {
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnStoreConnected     += OnStoreConnected;
            m_StoreController.OnStoreDisconnected  += OnStoreDisconnected;
            m_StoreController.OnProductsFetched    += OnProductsFetched;
            m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
            m_StoreController.OnPurchasePending    += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed  += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed      += OnPurchaseFailed;
            m_StoreController.OnPurchasesFetched   += OnPurchasesFetched;

            // Any unconfirmed purchases discovered while fetching are routed back
            // through OnPurchasePending so we grant + confirm them.
            m_StoreController.ProcessPendingOrdersOnPurchasesFetched(true);

            _ = m_StoreController.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PurchaseManager] IAP initialization failed: {e.Message}");
        }
    }

    private void OnStoreConnected()
    {
        m_StoreConnected = true;
        Debug.Log("[PurchaseManager] Store connected.");

        var products = new List<ProductDefinition>
        {
            new ProductDefinition(ProductGemBoost, ProductType.NonConsumable),
            new ProductDefinition(ProductNoAds,    ProductType.NonConsumable),
        };
        m_StoreController.FetchProducts(products);
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        m_StoreConnected = false;
        Debug.LogWarning($"[PurchaseManager] Store disconnected: {description.message}");
    }

    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"[PurchaseManager] Fetched {products.Count} product(s).");
        // Restore previously purchased non-consumables for this account/device.
        m_StoreController.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"[PurchaseManager] Product fetch failed: {failure.FailureReason}");
    }

    private void OnPurchasesFetched(Orders orders)
    {
        // Confirmed orders represent products the player already owns.
        foreach (var order in orders.ConfirmedOrders)
        {
            foreach (var productId in ProductIdsInOrder(order))
                GrantEntitlement(productId, notify: false);
        }
        Debug.Log("[PurchaseManager] Purchases restored from store.");
    }

    // -------------------------------------------------------------------------
    // Purchase flow (called by UI buttons via Inspector hooks)
    // -------------------------------------------------------------------------

    public void BuyGemBoost() => Buy(ProductGemBoost);
    public void BuyNoAds()    => Buy(ProductNoAds);

    private void Buy(string productId)
    {
        if (m_StoreController == null || !m_StoreConnected)
        {
            Debug.LogWarning($"[PurchaseManager] Store not ready; cannot purchase {productId}.");
            OnPurchaseFailedEvent?.Invoke(productId);
            return;
        }

        Debug.Log($"[PurchaseManager] Initiating purchase: {productId}");
        m_StoreController.PurchaseProduct(productId);
    }

    private void OnPurchasePending(PendingOrder order)
    {
        _ = ValidateAndConfirmAsync(order);
    }
    
    /// <summary>
    /// Re-queries the store for this account's previously purchased non-consumables
    /// and re-grants any entitlements found. Required by Apple's review guidelines
    /// (3.1.1) and useful on Android when the player reinstalls or switches devices.
    /// Safe to call multiple times — <see cref="GrantEntitlement"/> is idempotent.
    /// </summary>
    public void RestorePurchases()
    {
        if (m_StoreController == null || !m_StoreConnected)
        {
            Debug.LogWarning("[PurchaseManager] Store not ready; cannot restore purchases.");
            return;
        }

        Debug.Log("[PurchaseManager] Restoring purchases…");
        m_StoreController.FetchPurchases();
    }

    /// <summary>
    /// Calls the server-side ValidatePurchase Cloud Code function for each product
    /// in the order before granting anything. An entitlement is only ever granted
    /// once the store receipt has been independently verified — a spoofed or
    /// memory-edited client can no longer fake ownership by short-circuiting the
    /// local purchase flow.
    /// </summary>
    private async Task ValidateAndConfirmAsync(PendingOrder order)
    {
        string platform = CurrentPlatformName();
        string receipt  = ExtractReceipt(order, platform);

        for (int attempt = 1; attempt <= MaxReceiptValidationAttempts; attempt++)
        {
            bool shouldRetry = false;
            var validatedProducts = new List<string>();
            var rejectedProducts = new List<string>();

            foreach (var productId in ProductIdsInOrder(order))
            {
                ReceiptValidationResult validation = await ValidateReceiptAsync(receipt, productId, platform);
                switch (validation)
                {
                    case ReceiptValidationResult.Valid:
                        validatedProducts.Add(productId);
                        break;
                    case ReceiptValidationResult.Invalid:
                        rejectedProducts.Add(productId);
                        break;
                    case ReceiptValidationResult.Retry:
                        shouldRetry = true;
                        Debug.LogWarning($"[PurchaseManager] Receipt validation is temporarily unavailable for {productId} (attempt {attempt}/{MaxReceiptValidationAttempts}).");
                        break;
                }
            }

            if (!shouldRetry)
            {
                foreach (var productId in validatedProducts)
                    GrantEntitlement(productId, notify: true);

                foreach (var productId in rejectedProducts)
                {
                    Debug.LogWarning($"[PurchaseManager] Receipt was rejected for {productId}; entitlement withheld.");
                    OnPurchaseFailedEvent?.Invoke(productId);
                }

                // A valid receipt has been granted and a definitively invalid
                // receipt has been rejected. Both outcomes are final, so the
                // store transaction can now be completed.
                m_StoreController.ConfirmPurchase(order);
                return;
            }

            if (attempt < MaxReceiptValidationAttempts)
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }

        // Do not confirm a transaction when the validation service is down. Unity
        // IAP will surface this pending order again on the next store refresh, so
        // the player cannot lose a paid purchase because of a temporary outage.
        Debug.LogWarning("[PurchaseManager] Leaving purchase pending; validation will retry on the next store refresh.");
    }

    private static string CurrentPlatformName()
    {
        return Application.platform switch
        {
            RuntimePlatform.IPhonePlayer => "iOS",
            RuntimePlatform.Android      => "Android",
            _                            => "Editor",
        };
    }

    /// <summary>
    /// Apple: ValidatePurchase's Cloud Code expects the raw base64 App Store
    /// receipt blob (it forwards it to Apple's verifyReceipt as "receipt-data").
    /// Android/other: pass Unity IAP's wrapped receipt JSON as-is — the Cloud
    /// Code function unwraps the "Payload" field itself.
    /// </summary>
    private static string ExtractReceipt(PendingOrder order, string platform)
    {
        if (platform == "iOS")
            return order.Info.Apple?.AppReceipt ?? order.Info.Receipt;

        return order.Info.Receipt;
    }

    private async Task<ReceiptValidationResult> ValidateReceiptAsync(string receipt, string productId, string platform)
    {
        if (string.IsNullOrEmpty(receipt))
        {
            Debug.LogWarning($"[PurchaseManager] No receipt available for {productId}; waiting for a later retry.");
            return ReceiptValidationResult.Retry;
        }

        try
        {
            var args = new Dictionary<string, object>
            {
                { "receipt", receipt },
                { "productId", productId },
                { "platform", platform },
            };

            bool isValid = await CloudCodeService.Instance.CallEndpointAsync<bool>("ValidatePurchase", args);
            return isValid ? ReceiptValidationResult.Valid : ReceiptValidationResult.Invalid;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PurchaseManager] ValidatePurchase call failed for {productId}: {e.Message}");
            return ReceiptValidationResult.Retry;
        }
    }

    private void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmed:
                Debug.Log($"[PurchaseManager] Purchase confirmed: {string.Join(", ", ProductIdsInOrder(confirmed))}");
                break;
            case FailedOrder failed:
                Debug.LogWarning($"[PurchaseManager] Confirmation failed: {failed.FailureReason} - {failed.Details}");
                foreach (var productId in ProductIdsInOrder(failed))
                    OnPurchaseFailedEvent?.Invoke(productId);
                break;
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        Debug.LogWarning($"[PurchaseManager] Purchase failed: {order.FailureReason} - {order.Details}");
        foreach (var productId in ProductIdsInOrder(order))
            OnPurchaseFailedEvent?.Invoke(productId);
    }

    private static IEnumerable<string> ProductIdsInOrder(Order order)
    {
        var items = order?.CartOrdered?.Items();
        if (items == null) yield break;
        foreach (var item in items)
        {
            var id = item?.Product?.definition?.id;
            if (!string.IsNullOrEmpty(id))
                yield return id;
        }
    }

    // -------------------------------------------------------------------------
    // Entitlement granting + persistence
    // -------------------------------------------------------------------------

    private void GrantEntitlement(string productId, bool notify)
    {
        bool changed = false;

        if (productId == ProductGemBoost)
        {
            if (!IsGemBoostActive) { IsGemBoostActive = true; changed = true; }
        }
        else if (productId == ProductNoAds)
        {
            if (!IsNoAdsActive) { IsNoAdsActive = true; changed = true; }
        }
        else
        {
            Debug.LogWarning($"[PurchaseManager] Unknown product: {productId}");
            return;
        }

        SaveLocalState();
        if (changed)
            _ = SaveCloudStateAsync();

        if (notify)
            OnPurchaseSuccess?.Invoke(productId);

        Debug.Log($"[PurchaseManager] Granted entitlement: {productId} (changed={changed})");
    }

    // -------------------------------------------------------------------------
    // Gem balance (persisted per-user alongside IAP entitlements)
    // -------------------------------------------------------------------------

    /// <summary>Adds gems to the player's balance and persists locally + to Cloud Save.</summary>
    public void AddGems(int amount)
    {
        if (amount <= 0) return;
        SetGemCount(GemCount + amount);
    }

    /// <summary>
    /// Attempts to spend gems. Returns false (and changes nothing) if the player
    /// does not have enough.
    /// </summary>
    public bool SpendGems(int amount)
    {
        if (amount <= 0) return true;
        if (GemCount < amount) return false;
        SetGemCount(GemCount - amount);
        return true;
    }

    private void SetGemCount(int newValue)
    {
        if (newValue < 0) newValue = 0;
        if (newValue == GemCount) return;

        GemCount = newValue;
        SaveLocalState();
        _ = SaveCloudStateAsync();

        OnGemCountChanged?.Invoke(GemCount);
        Debug.Log($"[PurchaseManager] Gem balance updated: {GemCount}");
    }

    // -------------------------------------------------------------------------
    // Local cache (PlayerPrefs)
    // -------------------------------------------------------------------------

    private void LoadLocalState()
    {
        if (string.IsNullOrEmpty(activePlayerId))
            return;

        IsGemBoostActive = PlayerPrefs.GetInt(PlayerCacheKey(KeyGemBoost), 0) == 1;
        IsNoAdsActive    = PlayerPrefs.GetInt(PlayerCacheKey(KeyNoAds),    0) == 1;
        GemCount         = PlayerPrefs.GetInt(PlayerCacheKey(KeyGemCount), 0);
    }

    private void SaveLocalState()
    {
        if (string.IsNullOrEmpty(activePlayerId))
            return;

        PlayerPrefs.SetInt(PlayerCacheKey(KeyGemBoost), IsGemBoostActive ? 1 : 0);
        PlayerPrefs.SetInt(PlayerCacheKey(KeyNoAds),    IsNoAdsActive    ? 1 : 0);
        PlayerPrefs.SetInt(PlayerCacheKey(KeyGemCount), GemCount);
        PlayerPrefs.Save();
    }

    // -------------------------------------------------------------------------
    // Cloud Save (per-user persistence)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Re-pulls entitlements/gems from Cloud Save for whichever account is
    /// currently signed in. Call this after <see cref="AuthManager"/> switches
    /// the active player (e.g. signing into an existing email account on a new
    /// device) so the UI reflects that account's purchases instead of the
    /// anonymous session's.
    /// </summary>
    public async Task ReloadCloudStateAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized ||
            !AuthenticationService.Instance.IsSignedIn)
        {
            return;
        }

        await LoadCloudStateAsync();
    }

    /// <summary>
    /// Clears state belonging to the departing player. AuthManager calls this
    /// before it signs out or switches credentials so no entitlement or currency
    /// can be shown for the next player.
    /// </summary>
    public void ClearActivePlayerState()
    {
        activePlayerId = null;
        IsGemBoostActive = false;
        IsNoAdsActive = false;
        GemCount = 0;
        OnGemCountChanged?.Invoke(GemCount);
    }

    private async Task LoadCloudStateAsync()
    {
        SetActivePlayerFromAuthentication();

        var keys = new HashSet<string> { KeyGemBoost, KeyNoAds, KeyGemCount };
        Dictionary<string, Item> result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        // Cloud values OR-merge with the local cache so we never lose an
        // entitlement that exists on only one side.
        if (result.TryGetValue(KeyGemBoost, out var gem) && gem.Value.GetAs<bool>())
            IsGemBoostActive = true;
        if (result.TryGetValue(KeyNoAds, out var ads) && ads.Value.GetAs<bool>())
            IsNoAdsActive = true;

        // Gems are a quantity, so take the larger of cloud vs local to avoid
        // losing currency earned while offline on this device.
        if (result.TryGetValue(KeyGemCount, out var gems))
            GemCount = Math.Max(GemCount, gems.Value.GetAs<int>());

        SaveLocalState();
        OnGemCountChanged?.Invoke(GemCount);
        Debug.Log($"[PurchaseManager] Cloud Save loaded — GemBoost={IsGemBoostActive}, NoAds={IsNoAdsActive}, Gems={GemCount}");
    }

    private async Task SaveCloudStateAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized ||
                !AuthenticationService.Instance.IsSignedIn)
            {
                return;
            }

            var data = new Dictionary<string, object>
            {
                { KeyGemBoost, IsGemBoostActive },
                { KeyNoAds,    IsNoAdsActive },
                { KeyGemCount, GemCount },
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[PurchaseManager] Entitlements saved to Cloud Save.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PurchaseManager] Cloud Save write failed (local cache retained): {e.Message}");
        }
    }

    /// <summary>
    /// Selects the cache namespace for the currently authenticated player. A
    /// player switch always starts from an empty in-memory state before that
    /// player's own local/Cloud Save data is loaded.
    /// </summary>
    private void SetActivePlayerFromAuthentication()
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        if (string.IsNullOrEmpty(playerId) || playerId == activePlayerId)
            return;

        activePlayerId = playerId;
        IsGemBoostActive = false;
        IsNoAdsActive = false;
        GemCount = 0;
        LoadLocalState();
        OnGemCountChanged?.Invoke(GemCount);

        Debug.Log($"[PurchaseManager] Switched purchase state to player {activePlayerId}.");
    }

    private string PlayerCacheKey(string key) => $"{key}.{activePlayerId}";
}
