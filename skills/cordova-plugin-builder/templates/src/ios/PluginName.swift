import Foundation

#if canImport(Cordova)
import Cordova
#endif

/**
 * __PLUGIN_NAME__: Native iOS implementation in Swift.
 *
 * Rules for OutSystems MABS (cordova-ios 7 & 8):
 * 1. Never use an unguarded `import Cordova` (bridging header provides CDVPlugin; `#if canImport(Cordova)` supports SPM).
 * 2. Always type `let pluginResult: CDVPluginResult = ...` explicitly and avoid optional chaining (`pluginResult?.`) so it compiles on both cordova-ios 7 and 8.
 * 3. Always log with `NSLog("%@", "\(tag) ...")` to prevent format-string crashes.
 */
@objc(__PLUGIN_NAME__)
class __PLUGIN_NAME__: CDVPlugin {

    private let tag = "[__PLUGIN_NAME__]"

    override func pluginInitialize() {
        NSLog("%@", "\(tag) Plugin initialized")
    }

    @objc(greet:)
    func greet(_ command: CDVInvokedUrlCommand) {
        NSLog("%@", "\(tag) execute: action=greet")

        let raw = command.arguments.first as? String
        let trimmed = raw?.trimmingCharacters(in: .whitespacesAndNewlines) ?? ""

        let pluginResult: CDVPluginResult
        if trimmed.isEmpty {
            NSLog("%@", "\(tag) greet: rejected empty name")
            pluginResult = CDVPluginResult(
                status: CDVCommandStatus_ERROR,
                messageAs: "Expected a non-empty string argument."
            )
        } else {
            let message = "Hello, \(trimmed)!"
            NSLog("%@", "\(tag) greet: replying \"\(message)\"")
            pluginResult = CDVPluginResult(
                status: CDVCommandStatus_OK,
                messageAs: message
            )
        }

        commandDelegate.send(pluginResult, callbackId: command.callbackId)
    }
}
