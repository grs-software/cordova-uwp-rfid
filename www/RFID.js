﻿var RFID = {
	Enable: function (successCallback, errorCallback) {
		cordova.exec(successCallback, errorCallback, "RFID", "Enable");
	}
}

module.exports = RFID;