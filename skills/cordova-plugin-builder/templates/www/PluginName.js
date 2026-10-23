var exec = require('cordova/exec');

// Must match the <feature> name in plugin.xml and the class registered
// for each native platform.
var PLUGIN_NAME = '__PLUGIN_NAME__';

var __PLUGIN_NAME__ = {
    /**
     * Calls the native `greet` action.
     *
     * @param {string}   name       Input string passed to the native side.
     * @param {Function} onSuccess  Called with the native response.
     * @param {Function} onError    Called with an error message on failure.
     */
    greet: function (name, onSuccess, onError) {
        exec(onSuccess, onError, PLUGIN_NAME, 'greet', [name]);
    }
};

module.exports = __PLUGIN_NAME__;
