package com.outsystems.plugins.__PLUGIN_PACKAGE__

import android.util.Log
import org.apache.cordova.CallbackContext
import org.apache.cordova.CordovaPlugin
import org.json.JSONArray

/**
 * __PLUGIN_NAME__: Native Android implementation in Kotlin.
 */
class __PLUGIN_NAME__ : CordovaPlugin() {

    companion object {
        private const val TAG = "[__PLUGIN_NAME__]"
    }

    override fun pluginInitialize() {
        super.pluginInitialize()
        Log.d(TAG, "Plugin initialized")
    }

    override fun execute(
        action: String,
        args: JSONArray,
        callbackContext: CallbackContext
    ): Boolean {
        Log.d(TAG, "execute: action=$action")

        return try {
            when (action) {
                "greet" -> {
                    val name = if (args.isNull(0)) null else args.optString(0)
                    greet(name, callbackContext)
                    true
                }
                else -> {
                    Log.w(TAG, "Unknown action: $action")
                    false
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Exception during execute: ${e.message}", e)
            callbackContext.error("__PLUGIN_NAME__ error: ${e.message}")
            true
        }
    }

    private fun greet(name: String?, callbackContext: CallbackContext) {
        val trimmed = name?.trim().orEmpty()
        if (trimmed.isEmpty()) {
            Log.w(TAG, "greet: rejected empty name")
            callbackContext.error("Expected a non-empty string argument.")
            return
        }

        // For heavy I/O or network tasks, dispatch to the background thread pool:
        // cordova.threadPool.execute { ... }
        val message = "Hello, $trimmed!"
        Log.d(TAG, "greet: replying \"$message\"")
        callbackContext.success(message)
    }
}
