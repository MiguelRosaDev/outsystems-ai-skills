---
name: cordova-plugin-builder
description: Complete GitHub repository template, architecture patterns, and scaffolding standards for building native Cordova plugins for OutSystems 11 MABS (MABS 12 & MABS 13) using Kotlin (Android) and Swift (iOS), including standalone plugins, SDK extensions/bridges, build hooks, and native widgets. Use whenever creating or modifying a Cordova plugin GitHub repository.
---

# cordova-plugin-builder

Building a native Cordova plugin repository on GitHub that compiles and runs reliably in **OutSystems 11 MABS (Mobile Apps Build Service — MABS 12 & MABS 13)** requires strict synchronization across `plugin.xml`, `package.json`, the JavaScript bridge (`www/`), and the native implementations (`src/android/` in Kotlin and `src/ios/` in Swift).

This skill provides a **battle-tested GitHub repository template** (located in `./templates/`) and documents the **4 real-world Cordova plugin archetypes** used in production OutSystems 11 mobile ecosystems.

---

## 1. Ready-to-Use GitHub Repository Template (`./templates/`)

When asked to create a new Cordova plugin project for GitHub, scaffold the repository from the files in `./templates/`:

```text
os-cordova-plugin-<slug>/
├── .gitignore                   # Copy from templates/gitignore.template
├── package.json                 # Copy from templates/package.json
├── plugin.xml                   # Copy from templates/plugin.xml
├── README.md                    # Copy from templates/README.md
├── Package.swift                # Optional: required ONLY when adding external iOS pods/SPM on MABS 13
├── hooks/                       # Optional: Node.js build hooks (after_prepare, after_plugin_install)
├── www/
│   └── <PluginName>.js          # Copy from templates/www/PluginName.js
└── src/
    ├── android/
    │   └── <PluginName>.kt      # Copy from templates/src/android/PluginName.kt
    └── ios/
        └── <PluginName>.swift   # Copy from templates/src/ios/PluginName.swift
```

### Template Placeholder Replacement Table

Replace these placeholders consistently across all scaffolded files:

| Placeholder | Format | Example | Where it Must Match |
| :--- | :--- | :--- | :--- |
| `__PLUGIN_SLUG__` | `kebab-case` | `biometrics-auth` | Repo name (`os-cordova-plugin-biometrics-auth`), `plugin.xml` `id`, `package.json` `name` & `cordova.id` |
| `__PLUGIN_NAME__` | `PascalCase` | `BiometricsAuth` | `www/<PluginName>.js` (`PLUGIN_NAME`), `plugin.xml` `<feature name="...">`, Kotlin class name, Swift `@objc(...)` & class name |
| `__PLUGIN_PACKAGE__` | `lowercase` | `biometricsauth` | Android Kotlin package (`com.outsystems.plugins.biometricsauth`) and `plugin.xml` `target-dir` |
| `__GITHUB_ORG__` | GitHub user/org | `MiguelRosaDev` | `plugin.xml` `<repo>` and `package.json` `repository` / `bugs` / `homepage` |
| `__AUTHOR__` | String | `OutSystems Mobile Team` | `plugin.xml` `<author>` and `package.json` `author` |
| `__PLUGIN_DESCRIPTION__` | Sentence | `Native biometric authentication plugin.` | `plugin.xml`, `package.json`, and `README.md` |

> **Critical Identity Rule**:
> 1. The shared identifier `__PLUGIN_NAME__` **must be identical** in `www/<PluginName>.js` (`exec(..., PLUGIN_NAME, ...)`), `plugin.xml` (`<feature name="__PLUGIN_NAME__">`), and the native class registrations.
> 2. **Never** name a Swift plugin class after a Foundation or Swift standard library type (`Locale`, `Date`, `URL`, `Data`, `Error`). Because Swift plugin sources compile into the main app module in MABS, a colliding class name shadows that Foundation type for every other Swift plugin in the app.
> 3. In `package.json`, the key inside `engines.cordovaDependencies` (e.g. `"1.0.0"`) **must equal** the root `"version"` field.

---

## 2. The 4 Production Cordova Plugin Archetypes

Choose the right architecture based on what the OutSystems 11 mobile application needs:

### Archetype A: Standalone Modern Plugin (Kotlin + Swift)
Use when building a brand-new native capability or multi-method utility plugin (e.g., `os-cordova-plugin-nos-utils`, `outsystems-plugin-esim`, `ASWebAuthentication`).

- **Engines baseline**: `cordova >=12.0.0`, `cordova-android >=13.0.0`, `cordova-ios >=7.1.0`.
- **JS Namespace**: Exposes methods via `<clobbers target="cordova.plugins.custom.<PluginName>" />`.
- **Android (Kotlin)**:
  - Enable Kotlin automatically in `plugin.xml` inside `<config-file target="res/xml/config.xml" parent="/*">`:
    ```xml
    <preference name="GradlePluginKotlinEnabled" value="true" />
    <preference name="GradlePluginKotlinCodeStyle" value="official" />
    <preference name="GradlePluginKotlinVersion" value="1.9.24" />
    ```
  - Place source files in `src/android/<PluginName>.kt` mapped to `target-dir="app/src/main/kotlin/com/outsystems/plugins/<package>"`.
  - Use `org.apache.cordova.PermissionHelper` for runtime permissions (`PermissionHelper.hasPermission`, `PermissionHelper.requestPermission`) and override `onRequestPermissionResult`.
- **iOS (Swift — MABS 12 & MABS 13 Dual Compatibility)**:
  - **Do NOT add `cordova-plugin-add-swift-support`**: `cordova-ios >= 7.1.0` supports Swift natively; legacy Swift-support hooks corrupt Xcode build settings across plugins.
  - **Do NOT use a plain `import Cordova`**: The host app's bridging header already exposes `CDVPlugin`, and an unguarded `import Cordova` fails in MABS builds. Use `#if canImport(Cordova)` only when supporting Swift Package Manager:
    ```swift
    #if canImport(Cordova)
    import Cordova
    #endif
    ```
  - **Explicit `CDVPluginResult` type annotation & no optional chaining**:
    Always declare `let pluginResult: CDVPluginResult = CDVPluginResult(...)` and call `pluginResult.setKeepCallbackAs(true)` **without `?.`**. In `cordova-ios 8` (MABS 13), `CDVPluginResult` initializers are non-optional, so `pluginResult?.` fails compilation.
  - **Safe `NSLog` format string**:
    Always write `NSLog("%@", "\(tag) message")` — never pass interpolated strings as the first argument (`NSLog("\(tag) \(input)")`), which crashes if `input` contains `%@` or `%`.

---

### Archetype B: Extension / SDK Bridge Plugin (`<dependency>` + `<merges>`)
Use when you need to add missing methods to an existing massive plugin (e.g., `cordova-plugin-firebase-messaging-extension`, `cordova-plugin-firebase-analytics-extension`) or bridge two native SDKs (e.g., `cordova-plugin-firebase-salesforce-bridge`) **without forking the upstream repository**.

1. **Declare the upstream plugin as a `<dependency>` in `plugin.xml`**:
   ```xml
   <dependency id="cordova-outsystems-firebase-cloud-messaging"
               url="https://github.com/OutSystems/cordova-outsystems-firebase-cloud-messaging.git#2.4.0-OS2" />
   ```
2. **Use `<merges>` instead of `<clobbers>` in `<js-module>`**:
   This injects your new JS methods directly into the existing plugin's global object without overwriting its existing methods:
   ```xml
   <js-module name="FirebaseMessagingExtension" src="www/FirebaseMessagingExtension.js">
       <merges target="cordova.plugins.firebase.messaging" />
   </js-module>
   ```
3. **Register a dedicated `<feature>` for your extension class**:
   `www/FirebaseMessagingExtension.js` calls `exec(resolve, reject, "FirebaseMessagingExtension", "hasPermission", [])`, which routes solely your new methods to your lightweight `FirebaseMessagingExtension` native class.
4. **SDK Bridge Services (Android)**:
   When bridging two push/messaging providers (e.g., Firebase FCM and Salesforce Marketing Cloud), register a routing `FirebaseMessagingService` in `AndroidManifest.xml` under `parent="/manifest/application"` that inspects incoming `RemoteMessage` payloads and forwards them to the appropriate SDK handler.

---

### Archetype C: Plugins with External iOS Dependencies on MABS 13 (CocoaPods + SwiftPM)
MABS 12 (`cordova-ios 7`) resolves iOS dependencies via CocoaPods, whereas MABS 13 (`cordova-ios 8`) builds plugins with external dependencies as **Swift Packages (SPM)**. To make a single plugin repository build on both MABS 12 and MABS 13:

1. **Set `package="swift"` on `<platform name="ios">` in `plugin.xml`**:
   ```xml
   <platform name="ios" package="swift">
   ```
2. **Mark every `<pod>` with `nospm="true"`**:
   `cordova-ios 8` ignores `<pod nospm="true" />` (using `Package.swift` instead), while `cordova-ios 7` installs it via CocoaPods:
   ```xml
   <podspec>
       <config>
           <source url="https://cdn.cocoapods.org/" />
       </config>
       <pods use_frameworks="true">
           <pod name="FirebaseRemoteConfig" spec="~> 10.29.0" nospm="true" />
       </pods>
   </podspec>
   ```
3. **Add a root `Package.swift`**:
   The library product name **must match the plugin `id`** in `plugin.xml`, depend on `cordova-ios` (`branch: "master"`, which `cordova-ios` rewrites locally during build), and set `path: "src/ios"`.
4. **Guard the Cordova import in Swift**:
   ```swift
   #if canImport(Cordova)
   import Cordova
   #endif
   ```

---

### Archetype D: Build Hooks, Extensibility Preferences & Native Extensions
Use when a plugin must customize `AndroidManifest.xml`, iOS `Entitlements-*.plist`, `*-Info.plist`, `AppDelegate`, or bundle native AppWidgets (`Cordova-ios-widget-plugin`, `Cordova-android-widget-plugin`, `cordova-deeplinks-plugin`, `outsystems-plugin-esim`).

1. **Configurable Extensibility Variables (`<preference>`)**:
   Declare `<preference name="VAR_NAME" default="default_value" />` in `plugin.xml` so OutSystems applications can override it in Extensibility Configurations:
   ```xml
   <preference name="CAMERAUSAGEDESCRIPTION" default="Camera access is required." />
   <config-file target="*-Info.plist" parent="NSCameraUsageDescription">
       <string>$CAMERAUSAGEDESCRIPTION</string>
   </config-file>
   ```
2. **Node.js Build Hooks (`hooks/*.js`)**:
   - **Never use shell scripts (`.sh`)** — always use Node.js scripts wired via `<hook type="after_prepare" src="hooks/script.js" />` or `after_plugin_install`.
   - **Overriding Conflicting Android Manifest Attributes (`hooks/disable_backup.js`)**:
     When 3rd-party SDKs (like AppsFlyer) hardcode `android:allowBackup="true"`, use an `after_prepare` hook that reads `platforms/android/app/src/main/AndroidManifest.xml`, adds `xmlns:tools="http://schemas.android.com/tools"`, sets `android:allowBackup`, and appends `tools:replace="android:allowBackup"` on `<application>`.
   - **Injecting iOS Entitlements (`hooks/fixAppEntitlements.js`)**:
     To add special capabilities (e.g., `com.apple.developer.associated-domains`, `com.apple.security.application-groups`, or carrier/eSIM entitlements), write an `after_prepare` / `after_plugin_install` Node.js hook that merges the required keys into both `Entitlements-Debug.plist` and `Entitlements-Release.plist`.
3. **iOS `AppDelegate` Categories & Android Receivers/Widgets**:
   - To intercept iOS lifecycle events or Universal Links without replacing `AppDelegate`, add an Objective-C category (`AppDelegate+<Plugin>.h` and `AppDelegate+<Plugin>.m`) via `<header-file>` and `<source-file>`.
   - For Android Home Screen Widgets (`AppWidgetProvider`), register the `<receiver>` in `AndroidManifest.xml` (`parent="/manifest/application"`) and copy widget layout/info XMLs using `<resource-file src="src/android/res/layout/..." target="res/layout/..." />`.

---

## 3. Pre-Release Checklist for OutSystems MABS

Before pushing and tagging the repository on GitHub:
1. **Identity Sync Check**: Verify `__PLUGIN_NAME__` matches across `www/<PluginName>.js`, `plugin.xml` (`<feature>`), `<PluginName>.kt`, and `<PluginName>.swift` (`@objc(...)`).
2. **Version Sync Check**: Verify `version` in `plugin.xml`, `version` in `package.json`, and the `engines.cordovaDependencies["<version>"]` key in `package.json` are all identical.
3. **Thread Safety**: Any blocking I/O, network call, or heavy computation on Android runs inside `cordova.threadPool.execute { ... }`, and UI updates (such as `webView` interactions) run inside `cordova.activity.runOnUiThread { ... }`. On iOS, background work runs inside `commandDelegate.run(inBackground: { ... })` and UI presentations run on `DispatchQueue.main.async`.
4. **Git Tagging**: Always create a semantic release tag (e.g., `v1.0.0`) so OutSystems 11 Extensibility Configurations can pin an immutable commit:
   ```json
   {
       "plugin": {
           "url": "https://github.com/<org>/os-cordova-plugin-<slug>.git#v1.0.0"
       }
   }
   ```
