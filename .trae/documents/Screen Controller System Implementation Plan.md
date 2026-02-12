To set up code for individual screens, we need to move away from a monolithic manager and create a modular system where each screen has its own controller script.

Here is the implementation plan:

### 1. Create a Base Controller (`BaseScreen.cs`)
We will create an abstract base class that all screens will inherit from. This ensures every screen follows the same structure.
*   **Key Features:**
    *   `Initialize(VisualElement root)`: Called by the manager to give the screen access to its UI elements.
    *   `OnShow()`: Triggered when the screen becomes visible (good for resetting inputs or animations).
    *   `OnHide()`: Triggered when the screen is hidden.
    *   `ScreenName`: A property to match the UXML name (e.g., "LoginScreen").

### 2. Update `ScreenManager.cs`
We will upgrade the `ScreenManager` to be "smart":
*   It will automatically find all `BaseScreen` components attached to the same GameObject.
*   When `ShowScreen` is called, it will not only toggle visibility but also notify the relevant controller via `OnShow()`/`OnHide()`.

### 3. Create a Concrete Example (`LoginScreen.cs`)
We will implement the logic for the **LoginScreen** as a proof of concept.
*   It will inherit from `BaseScreen`.
*   It will find the "Login" button and text fields from the UI Toolkit document.
*   It will handle the button click event.

### 4. Implementation Steps
1.  **Create `Assets/Scripts/Screens/BaseScreen.cs`**: Define the abstract class.
2.  **Create `Assets/Scripts/Screens/LoginScreen.cs`**: Implement the login logic.
3.  **Refactor `ScreenManager.cs`**: Integrate the controller discovery and notification system.

This approach keeps your code clean, separated, and easy to expand as you add more screens.