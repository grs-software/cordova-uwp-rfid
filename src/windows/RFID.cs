using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RFIDAPI;

namespace WPCordovaClassLib.Cordova.Commands {
	public class RFID : BaseCommand {
		private RFIDSocket rfidSocket;

		public void Enable(string options) {
			PluginResult result = null;

			try {

				if (rfidSocket == null) {
					rfidSocket = new RFIDSocket();
				}

				rfidSocket.NDEFMessageHandler = (message) => {
					PluginResult iResult = new PluginResult(PluginResult.Status.OK, message.Payload);
					iResult.KeepCallback = true;
					DispatchCommandResult(result);
				};

				rfidSocket.SubscribeForMessages();

				result = new PluginResult(PluginResult.Status.NO_RESULT);
				result.KeepCallback = true;

			} catch (Exception e) {
				result = new PluginResult(PluginResult.Status.ERROR, e.Message);
			}

			DispatchCommandResult(result);
			
		}

    }
}
