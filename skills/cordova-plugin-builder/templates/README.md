# os-cordova-plugin-__PLUGIN_SLUG__

__PLUGIN_DESCRIPTION__

Built for **OutSystems 11 (MABS)** using **Kotlin** on Android and **Swift** on iOS.

## Project Structure

```text
.
├── .gitignore
├── package.json                 # npm / Cordova plugin metadata
├── plugin.xml                   # Plugin manifest: JS module + native wiring per platform
├── www/
│   └── __PLUGIN_NAME__.js       # JS interface (clobbers cordova.plugins.custom.__PLUGIN_NAME__)
└── src/
    ├── android/
    │   └── __PLUGIN_NAME__.kt   # Android implementation (Kotlin, CordovaPlugin)
    └── ios/
        └── __PLUGIN_NAME__.swift # iOS implementation (Swift, CDVPlugin)
```

## JavaScript API

### `greet(name, onSuccess, onError)`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `name` | `string` | Input string passed to native code. |
| `onSuccess` | `Function` | Receives the result string. |
| `onError` | `Function` | Receives an error message on failure. |

```javascript
cordova.plugins.custom.__PLUGIN_NAME__.greet(
    'Ada',
    function (message) { console.log(message); },
    function (error)   { console.error(error); }
);
```

## Using in OutSystems 11

### 1. Extensibility Configurations

Pin the plugin to a release tag in the Plugin Wrapper module's **Extensibility Configurations**:

```json
{
    "plugin": {
        "url": "https://github.com/__GITHUB_ORG__/os-cordova-plugin-__PLUGIN_SLUG__.git#v1.0.0"
    }
}
```

### 2. Client Action JavaScript Node

Call the plugin from an asynchronous Client Action JavaScript node with defensive namespace checks and guaranteed `$resolve()`:

```javascript
var plugin = window.cordova && cordova.plugins && cordova.plugins.custom &&
    cordova.plugins.custom.__PLUGIN_NAME__;

if (!plugin || typeof plugin.greet !== 'function') {
    $parameters.IsSuccess = false;
    $parameters.ErrorMessage = '__PLUGIN_NAME__ plugin is not available.';
    $resolve();
} else {
    plugin.greet(
        $parameters.Name,
        function (result) {
            $parameters.IsSuccess = true;
            $parameters.Response = String(result);
            $resolve();
        },
        function (error) {
            $parameters.IsSuccess = false;
            $parameters.ErrorMessage = String(error);
            $resolve();
        }
    );
}
```

## Native Logging

- **Android** (`adb logcat`):
  ```bash
  adb logcat | grep "\[__PLUGIN_NAME__\]"
  ```
- **iOS** (Xcode Console / Console.app): Filter by `[__PLUGIN_NAME__]`.

## License

MIT
