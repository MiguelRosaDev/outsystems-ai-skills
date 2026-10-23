---
name: cordova-plugin-builder
description: Architecture, scaffolding, and implementation standards for building native Cordova plugins compatible with OutSystems MABS (Mobile Apps Build Service), modern Android (AndroidX), and iOS (Privacy Manifests). Use whenever creating or modifying a Cordova plugin repository.
---

# cordova-plugin-builder

Building a Cordova plugin that compiles and runs reliably in OutSystems MABS (Mobile Apps Build Service) requires strict adherence to native standards on Android (AndroidX, non-blocking execution) and iOS (ARC, background threading, Apple Privacy Manifests).

This skill guides the scaffolding and implementation of native Cordova plugin repositories from scratch.

---

## 1. Standard Repository Architecture

Every Cordova plugin repository must follow this directory layout:

```text
cordova-plugin-<name>/
├── plugin.xml                  # Plugin manifest (Android & iOS configurations)
├── package.json                # npm package metadata
├── README.md                   # Installation, API documentation, and examples
├── www/
│   └── <name>.js               # JavaScript bridge with cordova.exec()
└── src/
    ├── android/
    │   ├── <PluginName>.java   # Native Android implementation (extends CordovaPlugin)
    │   └── build.gradle        # Optional Gradle dependencies (implementation '...')
    └── ios/
        ├── <PluginName>.h      # Native iOS header (extends CDVPlugin)
        ├── <PluginName>.m      # Native iOS implementation
        └── PrivacyInfo.xcprivacy # Mandatory Apple Privacy Manifest (iOS 17+)
```

---

## 2. The `plugin.xml` Manifest

The manifest tells Cordova and OutSystems MABS how to compile the native code and expose the JavaScript API.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<plugin xmlns="http://apache.org/cordova/ns/plugins/1.0"
        id="cordova-plugin-<name>"
        version="1.0.0">

    <name><PluginName></name>
    <description>Native Cordova plugin for <description></description>
    <license>MIT</license>
    <keywords>cordova,outsystems,ecosystem</keywords>

    <!-- JavaScript Bridge -->
    <js-module src="www/<name>.js" name="<PluginName>">
        <clobbers target="cordova.plugins.<PluginName>" />
    </js-module>

    <!-- ==================== ANDROID ==================== -->
    <platform name="android">
        <config-file target="res/xml/config.xml" parent="/*">
            <feature name="<PluginName>">
                <param name="android-package" value="com.outsystems.plugin.<name>.<PluginName>" />
                <param name="onload" value="true" />
            </feature>
        </config-file>

        <config-file target="AndroidManifest.xml" parent="/manifest">
            <!-- Add necessary permissions here -->
            <uses-permission android:name="android.permission.INTERNET" />
        </config-file>

        <source-file src="src/android/<PluginName>.java" target-dir="src/com/outsystems/plugin/<name>" />

        <!-- Optional: Gradle dependencies -->
        <!-- <framework src="src/android/build.gradle" custom="true" type="gradleReference" /> -->
    </platform>

    <!-- ==================== IOS ==================== -->
    <platform name="ios">
        <config-file target="config.xml" parent="/*">
            <feature name="<PluginName>">
                <param name="ios-package" value="<PluginName>" />
                <param name="onload" value="true" />
            </feature>
        </config-file>

        <!-- Permissions / Info.plist descriptions -->
        <config-file target="*-Info.plist" parent="NSCameraUsageDescription">
            <string>This app requires camera access to scan codes.</string>
        </config-file>

        <header-file src="src/ios/<PluginName>.h" />
        <source-file src="src/ios/<PluginName>.m" />

        <!-- Mandatory Apple Privacy Manifest (iOS 17+) -->
        <resource-file src="src/ios/PrivacyInfo.xcprivacy" />

        <!-- Optional: CocoaPods dependencies -->
        <!-- <podspec>
            <config>
                <source url="https://cdn.cocoapods.org/"/>
            </config>
            <pods use_frameworks="true">
                <pod name="SomeLibrary" spec="~> 1.0" />
            </pods>
        </podspec> -->
    </platform>
</plugin>
```

---

## 3. JavaScript Interface (`www/<name>.js`)

The JavaScript layer uses `cordova.exec` to communicate across the bridge. Support both standard callbacks and Promise-based patterns:

```javascript
var exec = require('cordova/exec');

var PLUGIN_NAME = '<PluginName>';

var MyPlugin = {
    performAction: function(options, successCallback, errorCallback) {
        options = options || {};
        exec(successCallback, errorCallback, PLUGIN_NAME, 'performAction', [options]);
    },

    // Promise-enabled helper
    performActionAsync: function(options) {
        return new Promise(function(resolve, reject) {
            MyPlugin.performAction(options, resolve, reject);
        });
    }
};

module.exports = MyPlugin;
```

---

## 4. Android Implementation (`<PluginName>.java`)

### Critical Rules:
1. **Never block the UI Thread**: The `execute()` method runs on the main Android UI thread. Network requests, disk I/O, or heavy calculations must be dispatched to `cordova.getThreadPool().execute()`. Blocking the main thread triggers Android ANR (Application Not Responding) dialogs.
2. **Always use AndroidX**: Never import `android.support.*`. Use `androidx.core.app.*`, `androidx.appcompat.*`, etc.

```java
package com.outsystems.plugin.<name>;

import org.apache.cordova.CordovaPlugin;
import org.apache.cordova.CallbackContext;
import org.apache.cordova.PluginResult;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

public class <PluginName> extends CordovaPlugin {

    @Override
    public boolean execute(String action, JSONArray args, final CallbackContext callbackContext) throws JSONException {
        if ("performAction".equals(action)) {
            final JSONObject options = args.optJSONObject(0);

            // Execute long-running tasks in the background thread pool
            cordova.getThreadPool().execute(new Runnable() {
                @Override
                public void run() {
                    try {
                        // Native logic here
                        JSONObject result = new JSONObject();
                        result.put("status", "success");
                        result.put("timestamp", System.currentTimeMillis());

                        callbackContext.success(result);
                    } catch (Exception e) {
                        callbackContext.error("Execution error: " + e.getMessage());
                    }
                }
            });
            return true;
        }
        return false;
    }
}
```

---

## 5. iOS Implementation (`<PluginName>.h` & `<PluginName>.m`)

### Header (`<PluginName>.h`):
```objc
#import <Cordova/CDVPlugin.h>

@interface <PluginName> : CDVPlugin

- (void)performAction:(CDVInvokedUrlCommand*)command;

@end
```

### Implementation (`<PluginName>.m`):
```objc
#import "<PluginName>.h"

@implementation <PluginName>

- (void)performAction:(CDVInvokedUrlCommand*)command {
    // Run in background queue to keep UI responsive
    [self.commandDelegate runInBackground:^{
        NSDictionary* options = [command.arguments objectAtIndex:0];

        @try {
            // Native logic here
            NSDictionary* response = @{
                @"status": @"success",
                @"timestamp": @([[NSDate date] timeIntervalSince1970])
            };

            CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus:CDVCommandStatus_OK messageAsDictionary:response];
            [self.commandDelegate sendPluginResult:pluginResult callbackId:command.callbackId];
        }
        @catch (NSException *exception) {
            CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus:CDVCommandStatus_ERROR messageAsString:exception.reason];
            [self.commandDelegate sendPluginResult:pluginResult callbackId:command.callbackId];
        }
    }];
}

@end
```

---

## 6. Apple Privacy Manifest (`PrivacyInfo.xcprivacy`)

Since iOS 17 (mandatory for App Store submissions), Apple requires a `PrivacyInfo.xcprivacy` file bundled with every plugin. Place it in `src/ios/PrivacyInfo.xcprivacy`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>NSPrivacyTracking</key>
    <false/>
    <key>NSPrivacyTrackingDomains</key>
    <array/>
    <key>NSPrivacyCollectedDataTypes</key>
    <array/>
    <key>NSPrivacyAccessedAPITypes</key>
    <array/>
</dict>
</plist>
```

---

## 7. MABS Compatibility Rules Checklist

Before pushing a Cordova plugin for use with OutSystems MABS:
1. **No Hardcoded Versions**: In Gradle / CocoaPods, avoid pinning hard dependencies (e.g. `com.google.android.gms:play-services:18.0.0`) that conflict with other plugins. Use dynamic ranges or OutSystems standard variables.
2. **No Custom Hook Scripts**: Avoid shell script hooks (`<hook type="before_build" src="scripts/run.sh" />`) as MABS cloud build agents restrict arbitrary shell execution for security.
3. **AndroidX Compatibility**: Ensure all Java/Kotlin dependencies support AndroidX.
4. **Git Tags**: Always tag releases (e.g. `v1.0.0`) on GitHub so OutSystems Extensibility Configurations can pin a stable release:
   `"url": "https://github.com/User/Repo.git#v1.0.0"`
