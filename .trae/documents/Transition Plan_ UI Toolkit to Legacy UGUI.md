The most common and seamless way to handle this in Unity is the **"Hybrid Single-Scene Handoff"**.

Since you already have a `Menu.unity` scene with your legacy `MenuView` (UGUI), we can simply overlay the new UI Toolkit Onboarding on top of it. When onboarding finishes, we disable the UI Toolkit document and enable the Legacy Canvas.

### **The Strategy: "Overlay & Handoff"**

1.  **State 1: Onboarding (Top Layer)**
    *   **Active**: UI Toolkit (`UIDocument`).
    *   **Inactive**: Legacy UGUI (`Canvas` or `MenuView` root object).
    *   **Logic**: Your new `LoginNavigationController` handles the login flows.

2.  **State 2: Handoff (The Switch)**
    *   When login/signup succeeds, `LoginNavigationController` fires an event (e.g., `OnOnboardingComplete`).
    *   `MenuView` listens for this event.

3.  **State 3: Legacy Menu (Bottom Layer)**
    *   **Action**: `MenuView` disables the `UIDocument` GameObject and enables its own `Canvas`.
    *   **Result**: The user is now in your existing legacy flow.

---

### **Implementation Plan**

#### **Step 1: Update `LoginNavigationController.cs`**
*   Add a `UnityEvent` called `OnOnboardingComplete`.
*   Invoke this event when the "Success" condition is met (e.g., after "Account Created" or "Successful Login").

#### **Step 2: Update `MenuView.cs`**
*   Add a reference to the `LoginNavigationController` (or the Onboarding GameObject).
*   Add a method `ShowLegacyMenu()` that:
    1.  Fades in or enables the Legacy Canvas.
    2.  Disables the Onboarding GameObject.
*   Modify `Start()`:
    *   Check if a user is already logged in (Supabase).
    *   **If Yes**: Skip onboarding, call `ShowLegacyMenu()` immediately.
    *   **If No**: Show Onboarding, Hide Legacy Menu.

#### **Step 3: Connect in Editor**
*   In the Inspector, link the `OnOnboardingComplete` event from Step 1 to the `ShowLegacyMenu` function in Step 2.

### **Why this approach?**
*   **Zero Loading Time**: No scene loading bars between login and menu.
*   **Data Persistence**: Since you are in the same scene, the Supabase session data is immediately available to the Legacy UI without needing `DontDestroyOnLoad` singletons for everything.
