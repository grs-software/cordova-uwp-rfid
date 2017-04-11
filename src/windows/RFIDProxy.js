cordova.commandProxy.add("RFID", {
	Enable: function (successCallback, errorCallback) {
		var options = {
			keepCallback: true
		};

		if (typeof window._RFID === "undefined") {
			window._RFID = cordova_uwp_rfid.RFID.instance;
		}

		window._RFID.addEventListener("message", function (e) {
			if (typeof e !== "undefined" &&
				typeof e.message !== "undefined") {
				if (typeof successCallback !== "undefined") {
					successCallback(e.message, options);
				}
			} else {
				if (typeof errorCallback !== "undefined") {
					errorCallback("Error: Empty message", options);
				}
			}
		});
		window._RFID.addEventListener("failure", function (e) {
			if (typeof errorCallback !== "undefined") {
				errorCallback(e.message, options);
			}
		});

		window._RFID.enable();
	}
});