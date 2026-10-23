---
name: cordova-plugin-wrapper
description: How to build clean, resilient OutSystems mobile wrapper modules for Cordova and Capacitor plugins, including Extensibility Configurations, asynchronous JavaScript action nodes, and browser fallbacks. Use whenever integrating native mobile plugins into OutSystems.
---

# cordova-plugin-wrapper

OutSystems Mobile and Reactive applications rely on Cordova / Capacitor plugins to interface with native iOS and Android APIs. To make a plugin robust in OutSystems, it must be wrapped in a dedicated OutSystems module with strict Extensibility Configurations, asynchronous handling, and desktop/browser fallbacks.

---

## 1. Extensibility Configurations (JSON)

In the OutSystems mobile wrapper module, configure the **Extensibility Configurations** property:

```json
{
    "plugin": {
        "url": "https://github.com/<User>/<PluginRepo>.git#<ReleaseTagOrBranch>",
        "variables": [
            {
                "name": "API_KEY",
                "value": "your_api_key_here"
            }
        ]
    },
    "preferences": {
        "global": [
            {
                "name": "KeepRunning",
                "value": "true"
            }
        ],
        "android": [
            {
                "name": "AndroidXEnabled",
                "value": "true"
            }
        ]
    }
}
```

### Best Practices:
* **Pin the Release Tag**: Always suffix `#v1.0.0` or a specific commit SHA / stable branch rather than leaving untagged master/main, ensuring MABS builds are reproducible.
* **Variables**: Pass environment or SDK variables directly through the `variables` array.

---

## 2. Check Plugin Availability First

Before invoking any native method, the Client Action must check whether the plugin is loaded on the current device:

```text
[CheckPluginAvailability]
   │
   ▼
[If IsPluginAvailable()]
   ├── True  ──► [Execute JavaScript Native Call]
   │
   └── False ──► [Assign Fallback / Mock Data, IsSuccess = False]
```

### Availability Helper Action (`Check<Plugin>Plugin`)
In a JavaScript node:
```javascript
$parameters.IsAvailable = (typeof cordova !== 'undefined' && 
                           typeof cordova.plugins !== 'undefined' && 
                           typeof cordova.plugins.MyPlugin !== 'undefined');
$resolve();
```

---

## 3. Asynchronous JavaScript Nodes with `$resolve()` and `$reject()`

Cordova plugins execute asynchronously via native callbacks. In OutSystems Client Actions, JavaScript nodes must always conclude with `$resolve()` or `$reject()` to prevent the screen thread from hanging:

```javascript
var options = {
    title: $parameters.Title,
    message: $parameters.Message
};

if (typeof cordova !== 'undefined' && cordova.plugins && cordova.plugins.MyPlugin) {
    cordova.plugins.MyPlugin.performAction(options, 
        function(result) {
            // Success Callback
            $parameters.IsSuccess = true;
            $parameters.ResultJson = JSON.stringify(result);
            $resolve();
        }, 
        function(error) {
            // Error Callback
            $parameters.IsSuccess = false;
            $parameters.ErrorMessage = typeof error === 'string' ? error : (error.message || 'Plugin execution failed');
            $resolve(); // Resolving allows clean OutSystems exception handling
        }
    );
} else {
    // Desktop / Browser Preview Fallback
    $parameters.IsSuccess = false;
    $parameters.ErrorMessage = 'Plugin is not available on this platform (browser/desktop simulator).';
    $resolve();
}
```

### Critical Rules:
* **Never omit `$resolve()`**: If an execution branch fails to call `$resolve()`, the OutSystems UI thread will freeze waiting for the node to complete.
* **Return Strings or Booleans**: JavaScript nodes handle primitive types and serialized JSON cleanly. Parse complex JSON into OutSystems Structures using `JSONDeserialize`.

---

## 4. Standard Client Action Output Signature

Every public wrapper Client Action exposed by the module should follow this consistent output pattern:

* `IsSuccess` (Boolean): Indicates whether the native action succeeded.
* `ErrorMessage` (Text): Human-readable error description if `IsSuccess = False`.
* Payload outputs: Specific outputs relevant to the action (e.g. `ScannedCode`, `SelectedFilePath`, `Token`).
