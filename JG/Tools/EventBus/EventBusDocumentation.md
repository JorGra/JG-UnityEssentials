# Event Bus System

The event bus infrastructure in `JG-UnityEssentials/JG/Tools/EventBus` provides a lightweight publish/subscribe layer for gameplay and tooling code. It supplies runtime conveniences and editor safety features so teams can connect systems without littering manual register/deregister code. This document walks through every supported usage pattern so you can jump straight to the approach you need.

## Quick Start (TL;DR)

1. **Declare an event payload**: implement `IEvent` on a struct or class to describe the data you want to broadcast.

```csharp
public readonly struct ThemeChangedEvent : IEvent
{
    public ThemeAsset Theme { get; init; }
}
```

2. **Raise the event** from anywhere in your codebase.

```csharp
EventBus<ThemeChangedEvent>.Raise(new ThemeChangedEvent { Theme = theme });
```

3. **Listen from a `MonoBehaviour`** with the one-line helpers. Subscriptions are disposed automatically when the component disables or is destroyed.

```csharp
public sealed class ThemeableButton : MonoBehaviour
{
    void OnEnable()
    {
        this.SubscribeEvent<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
        ApplyTheme(ThemeManager.Instance.CurrentTheme);
    }
}
```

4. **Keep the handle when you need manual control**. The returned `EventSubscription<T>` lets you pause or dispose the listener on demand.

```csharp
private EventSubscription<PlaySoundEvent> subscription;

void OnEnable()
{
    subscription = EventBus<PlaySoundEvent>.Subscribe(OnPlaySound, owner: this);
}

public void StopListening()
{
    subscription?.Dispose();
}
```

## Core Building Blocks

| Type | What it does | Notes |
|------|--------------|-------|
| `IEvent` | Marker interface every payload implements. Works with structs or classes. | `Assets/JG-UnityEssentials/JG/Tools/EventBus/IEvent.cs` |
| `EventBus<T>` | Static hub that stores listeners for payload `T` and synchronously calls them when `Raise` is invoked. | Supports `Subscribe`, `Register`, `Deregister`, `Raise`. |
| `EventSubscription<T>` | Disposable handle returned from `Subscribe`. Disposing removes the listener. | Store when you need manual lifetime control. |
| `EventBinding<T>` | Legacy binding container kept for backwards compatibility. | Prefer `Subscribe`/helpers unless migrating old code. |
| `EventSubscriptionExtensions` | Adds `this.SubscribeEvent<T>` helpers to `MonoBehaviour`. | Uses `EventSubscriptionTracker` under the hood. |
| `EventSubscriptionGroup` | Tracks bus subscriptions plus arbitrary `IDisposable` instances as a batch. | Useful for coordinating multiple resources. |
| `EventSubscriptionTracker` | Hidden component that owns one `EventSubscriptionGroup` per subscribing component and disposes listeners on disable/destroy. | Automatically added to the listener's `GameObject`. |
| `EventBusUtil` | Bootstraps all event buses, clears them on exiting Play Mode, and exposes aggregated type lists. | Lives in `Assets/JG-UnityEssentials/JG/Tools/EventBus/EventBusUtil.cs`. |

## Declaring Events

- Implement `IEvent` on the payload type. The type may be a struct (value semantics) or class (reference semantics).
- Keep payloads lightweight. The event is passed to every listener, so avoid large object graphs or allocations in the constructor.
- No explicit registration is required. `EventBusUtil.Initialize` pre-computes every `EventBus<T>` at startup so the first call to `Raise` has no reflection cost.

## Raising Events

- Use `EventBus<T>.Raise(payload)` to notify listeners. Invocation order matches subscription order.
- Events run synchronously on the calling thread. Raise from Unity's main thread unless every listener is thread-safe.
- Parameterless listeners are supported: `EventBus<T>.Subscribe(Action handler)` runs the action even if the payload is ignored.
- Avoid raising from `OnDisable` if you expect the same component to be unsubscribed already; listeners may have been pruned.

## Listening Patterns

### `MonoBehaviour` components (recommended)

`this.SubscribeEvent<T>(handler, disposeOnDisable: true)` attaches the listener to the component lifecycle.

- The hidden `EventSubscriptionTracker` attaches to the same `GameObject` and disposes listeners when the component is disabled or destroyed.
- Set `disposeOnDisable: false` if you need the listener to survive temporary disables (for example, when toggling a UI element off and on quickly).

```csharp
private EventSubscription<InventoryChangedEvent> subscription;

void Awake()
{
    subscription = this.SubscribeEvent<InventoryChangedEvent>(OnInventoryChanged, disposeOnDisable: false);
}

void OnDestroy()
{
    subscription?.Dispose();
}
```

### Non-`MonoBehaviour` listeners

Call `EventBus<T>.Subscribe(handler, owner)` directly.

```csharp
class SaveSystem : IDisposable
{
    readonly EventSubscription<GamePausedEvent> subscription;

    public SaveSystem()
    {
        subscription = EventBus<GamePausedEvent>.Subscribe(OnGamePaused);
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    void OnGamePaused(GamePausedEvent evt)
    {
        // react to pause here
    }
}
```

Provide an `owner` (`UnityEngine.Object`) when possible so destroyed objects are auto-pruned.

### Grouping multiple subscriptions

`EventSubscriptionGroup` lets you keep several listeners (and any other `IDisposable`) together.

```csharp
var group = new EventSubscriptionGroup(owner: this);
group.Listen<SettingsChangedEvent>(OnSettingsChanged);
group.Track(myDisposableResource);
```

Call `group.DisposeOnDisable()` when you want to remove only the listeners flagged for disable, or `group.DisposeAll()` to clear everything.

## Ownership, Lifetime, and Cleanup

- Every subscription stores its owner. When Unity destroys that object, the bus removes the listener before the next raise.
- `autoDeregisterOnOwnerDestroyed` (true by default) controls whether the bus should prune the listener when the owner becomes `null`. Set it to `false` if the owner is a long-lived singleton that keeps the listener active across the object's destruction/recreation.
- `EventSubscriptionTracker` monitors `MonoBehaviour.isActiveAndEnabled`. When a component transitions from enabled to disabled, the tracker disposes listeners that opted into `disposeOnDisable`.

## Editor and Tooling Support

- `EventBusUtil.InitializeEditor` registers for `EditorApplication.playModeStateChanged`. When the editor exits Play Mode, `EventBusUtil.ClearAllBuses()` runs, disposing every active binding and logging potential leaks with stack traces.
- The initialization log lists every concrete event type found via `PredefinedAssemblyUtil.GetTypes(typeof(IEvent))`. Use it to confirm your new payloads are being picked up.
- Call `EventBusUtil.ClearAllBuses()` manually from editor tooling or integration tests when you need a clean slate between runs.
- Leak warnings surface in the console for development builds and in the editor. Investigate these when you see output like `EventBus<GamePausedEvent> detected subscription leak (owner: MyComponent)`; the captured stack trace points to the registration site.

## Debugging Checklist

- Nothing fires? Ensure the payload type implements `IEvent` and that the listener is subscribing to the same generic type.
- Double execution? Check you are not calling `SubscribeEvent` from both `Awake` and `OnEnable`.
- Handler missing after disabling a component? Pass `disposeOnDisable: false` when subscribing if the component gets toggled off temporarily.
- Want to inspect current listeners? Temporarily add logging inside `EventBus<T>.Register` or `EventBus<T>.Raise`; all subscriptions funnel through those methods.

## Reference Files

- `Assets/JG-UnityEssentials/JG/Tools/EventBus/EventBus.cs`
- `Assets/JG-UnityEssentials/JG/Tools/EventBus/EventBusUtil.cs`
- `Assets/JG-UnityEssentials/JG/Tools/EventBus/EventSubscriptionExtensions.cs`
- `Assets/JG-UnityEssentials/JG/Tools/EventBus/EventSubscriptionTracker.cs`
- Samples: search for `SubscribeEvent` under `Assets/JGameFramework/Samples` for concrete usages.
