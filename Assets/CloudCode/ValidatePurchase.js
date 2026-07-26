// Cloud Code function: ValidatePurchase
// Runtime: Node.js 18 (UGS Cloud Code)
// Deploy via the UGS CLI:
//   ugs deploy Assets/CloudCode/ValidatePurchase.js
// Or via the UGS Dashboard → Cloud Code → Scripts → New Script.
//
// Required environment variables (set in UGS Dashboard → Cloud Code → Secrets):
//   APPLE_SHARED_SECRET        — iTunes Connect shared secret for receipt validation
//   GOOGLE_PACKAGE_NAME        — Android package name (e.g. com.yourstudio.solitaire)
//   GOOGLE_SERVICE_ACCOUNT_KEY — Full contents of the service account JSON key file
//                                (Google Cloud Console → IAM → Service Accounts → Keys → Add Key → JSON)
//
// Expected parameters: { receipt: string, productId: string, platform: string }
// Returns: bool — true when the receipt is valid and the entitlement has been granted.

const { EconomyApi } = require("@unity-services/economy-2.4");

module.exports = async ({ params, context, logger }) => {
    const { receipt, productId, platform } = params;

    if (!receipt || !productId || !platform) {
        logger.error("ValidatePurchase: missing required parameter(s).");
        return false;
    }

    logger.info(`ValidatePurchase — product: ${productId}, platform: ${platform}, player: ${context.playerId}`);

    // -------------------------------------------------------------------------
    // Step 1: Platform receipt validation
    // -------------------------------------------------------------------------

    let validationPassed = false;

    try {
        switch (platform) {
            case "iOS":
                validationPassed = await validateAppleReceipt(receipt, logger);
                break;
            case "Android":
                validationPassed = await validateGoogleReceipt(receipt, productId, logger);
                break;
            default:
                // Editor / sandbox build — skip external validation
                logger.info("Sandbox platform detected — skipping store receipt validation.");
                validationPassed = true;
                break;
        }
    } catch (err) {
        logger.error(`Receipt validation threw an exception: ${err.message}`);
        return false;
    }

    if (!validationPassed) {
        logger.warning(`Receipt rejected by store for product: ${productId}.`);
        return false;
    }

    // -------------------------------------------------------------------------
    // Step 2: Redeem Real Money purchase via Economy
    // -------------------------------------------------------------------------
    // makeVirtualPurchase is for spending in-game currency only.
    // Real Money purchases (premiumiap, gemboostiap) use the dedicated redeem
    // endpoints which grant the configured inventory items server-side.

    try {
        const economy = new EconomyApi(context);

        if (platform === "iOS") {
            await economy.redeemAppleAppStorePurchase({
                projectId: context.projectId,
                playerId: context.playerId,
                playerPurchaseAppleAppStoreRequest: {
                    receipt: receipt,
                    localCost: 0,
                    localCurrency: "USD",
                },
            });
        } else if (platform === "Android") {
            await economy.redeemGooglePlayPurchase({
                projectId: context.projectId,
                playerId: context.playerId,
                playerPurchaseGooglePlayStoreRequest: {
                    purchaseData: receipt,
                    purchaseDataSignature: "",
                    localCost: 0,
                    localCurrency: "USD",
                },
            });
        } else {
            // Sandbox / Editor — Economy redemption skipped, client applies state locally
            logger.info(`Sandbox grant skipped for product: ${productId}`);
        }

        logger.info(`Entitlement granted — product: ${productId}, player: ${context.playerId}`);
        return true;
    } catch (err) {
        logger.error(`Economy redemption failed: ${err.message}`);
        return false;
    }
};

// =============================================================================
// Apple receipt validation
// =============================================================================

async function validateAppleReceipt(receipt, logger) {
    const PROD_URL    = "https://buy.itunes.apple.com/verifyReceipt";
    const SANDBOX_URL = "https://sandbox.itunes.apple.com/verifyReceipt";
    const sharedSecret = process.env.APPLE_SHARED_SECRET;

    if (!sharedSecret) {
        logger.error("APPLE_SHARED_SECRET environment variable is not set.");
        return false;
    }

    const body = JSON.stringify({ "receipt-data": receipt, password: sharedSecret });
    const headers = { "Content-Type": "application/json" };

    let json = await postJson(PROD_URL, body, headers, logger);
    if (!json) return false;

    // Status 21007: receipt is a sandbox receipt, retry against sandbox endpoint.
    if (json.status === 21007) {
        logger.info("Apple receipt is a sandbox receipt — retrying against sandbox endpoint.");
        json = await postJson(SANDBOX_URL, body, headers, logger);
        if (!json) return false;
    }

    // Status 0 means the receipt is valid.
    if (json.status !== 0) {
        logger.warning(`Apple receipt validation returned status: ${json.status}`);
        return false;
    }

    logger.info("Apple receipt is valid.");
    return true;
}

// =============================================================================
// Google Play receipt validation
// =============================================================================

async function validateGoogleReceipt(receipt, productId, logger) {
    const packageName = process.env.GOOGLE_PACKAGE_NAME;

    if (!packageName) {
        logger.error("GOOGLE_PACKAGE_NAME environment variable is not set.");
        return false;
    }

    // Mint a short-lived access token from the stored service account key.
    const accessToken = await getGoogleAccessToken(logger);
    if (!accessToken) return false;

    // Unity IAP wraps the Google receipt in a JSON object with a Payload field.
    let outer;
    try {
        outer = JSON.parse(receipt);
    } catch {
        logger.error("Failed to parse outer Google receipt JSON.");
        return false;
    }

    let payload;
    try {
        payload = JSON.parse(outer.Payload);
    } catch {
        logger.error("Failed to parse Google receipt Payload field.");
        return false;
    }

    let purchaseToken;
    try {
        const inner = JSON.parse(payload.json);
        purchaseToken = inner.purchaseToken;
    } catch {
        logger.error("Failed to extract purchaseToken from Google receipt payload.");
        return false;
    }

    if (!purchaseToken) {
        logger.error("purchaseToken is missing from the Google receipt.");
        return false;
    }

    // Call the Google Play Developer API to verify the purchase.
    const url =
        `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/` +
        `${packageName}/purchases/products/${productId}/tokens/${purchaseToken}`;

    let response;
    try {
        response = await fetch(url, {
            headers: { Authorization: `Bearer ${accessToken}` },
        });
    } catch (err) {
        logger.error(`Google Play API request failed: ${err.message}`);
        return false;
    }

    if (!response.ok) {
        logger.warning(`Google Play API returned HTTP ${response.status} for product: ${productId}`);
        return false;
    }

    const json = await response.json();

    // purchaseState: 0 = Purchased, 1 = Cancelled, 2 = Pending
    if (json.purchaseState !== 0) {
        logger.warning(`Google purchase state is ${json.purchaseState} (expected 0) for product: ${productId}`);
        return false;
    }

    // consumptionState: 0 = Not consumed — correct for non-consumable products.
    logger.info("Google receipt is valid.");
    return true;
}

// =============================================================================
// Shared HTTP helper
// =============================================================================

async function postJson(url, body, headers, logger) {
    let response;
    try {
        response = await fetch(url, { method: "POST", headers, body });
    } catch (err) {
        logger.error(`HTTP POST to ${url} failed: ${err.message}`);
        return null;
    }

    if (!response.ok) {
        logger.error(`HTTP POST to ${url} returned status: ${response.status}`);
        return null;
    }

    try {
        return await response.json();
    } catch (err) {
        logger.error(`Failed to parse JSON response from ${url}: ${err.message}`);
        return null;
    }
}

// =============================================================================
// Google service account — short-lived access token
// =============================================================================

// Service account access tokens expire after 1 hour. This function mints a
// fresh one on every invocation by signing a JWT with the stored private key
// and exchanging it with Google's OAuth 2.0 token endpoint.

async function getGoogleAccessToken(logger) {
    const keyJson = process.env.GOOGLE_SERVICE_ACCOUNT_KEY;
    if (!keyJson) {
        logger.error("GOOGLE_SERVICE_ACCOUNT_KEY environment variable is not set.");
        return null;
    }

    let key;
    try {
        key = JSON.parse(keyJson);
    } catch {
        logger.error("Failed to parse GOOGLE_SERVICE_ACCOUNT_KEY as JSON.");
        return null;
    }

    const now   = Math.floor(Date.now() / 1000);
    const scope = "https://www.googleapis.com/auth/androidpublisher";

    const header  = base64url(Buffer.from(JSON.stringify({ alg: "RS256", typ: "JWT" })));
    const payload = base64url(Buffer.from(JSON.stringify({
        iss: key.client_email,
        scope,
        aud: "https://oauth2.googleapis.com/token",
        iat: now,
        exp: now + 3600,
    })));

    const { createSign } = await import("node:crypto");
    const signer = createSign("RSA-SHA256");
    signer.update(`${header}.${payload}`);
    const signature = base64url(signer.sign(key.private_key));

    const jwt = `${header}.${payload}.${signature}`;

    let response;
    try {
        response = await fetch("https://oauth2.googleapis.com/token", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                grant_type: "urn:ietf:params:oauth:grant-type:jwt-bearer",
                assertion: jwt,
            }),
        });
    } catch (err) {
        logger.error(`Google token exchange request failed: ${err.message}`);
        return null;
    }

    if (!response.ok) {
        logger.error(`Google token exchange returned HTTP ${response.status}.`);
        return null;
    }

    const json = await response.json();
    if (!json.access_token) {
        logger.error("Google token exchange response did not contain an access_token.");
        return null;
    }

    logger.info("Google service account access token minted successfully.");
    return json.access_token;
}

function base64url(buf) {
    return buf.toString("base64")
              .replace(/\+/g, "-")
              .replace(/\//g, "_")
              .replace(/=+$/, "");
}
