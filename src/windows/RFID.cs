using System;
using cordova_uwp_rfid;
using Windows.Networking.Proximity;

namespace cordova_uwp_rfid {
	public delegate void MessageHandler(String message);

	public sealed class MessageEventArgs {
		public String message {
			get;
			set;
		}
	}

	public sealed class RFID {
		#region SINGLETON
		private static volatile RFID instance;
		private static object sync = new Object();

		public static RFID Instance {
			get {
				if (RFID.instance == null) {
					lock (sync) {
						if (RFID.instance == null) {
							RFID.instance = new RFID();
						}
					}
				}

				return RFID.instance;
			}
		}
		#endregion

		private RFIDSocket rfidSocket;
		public event EventHandler<MessageEventArgs> Message;
		public event EventHandler<MessageEventArgs> Failure;
		public bool Active {
			get;
			private set;
		}

		private RFID() {
			Active = false;
			rfidSocket = new RFIDSocket();
		}

		public void Enable() {

			try {

				this.Message(this, new MessageEventArgs() { message = "Test..." });

				/*rfidSocket.ArrivedHandler = (sender) => {
					if (this.Message != null) {
						this.Message(this, new MessageEventArgs() { message = "Device Arrived" });
					}
				};
				//
				rfidSocket.DepartedHandler = (sender) => {
					if (this.Message != null) {
						this.Message(this, new MessageEventArgs() { message = "Device Departed" });
					}
				};

				rfidSocket.NDEFMessageHandler = (message) => {
					if (this.Message != null) {
						this.Message(this, new MessageEventArgs() { message = message.Payload });
					}
				};

				rfidSocket.SubscribeForMessages();*/

				Active = true;

			} catch (Exception e) {
				if (this.Failure != null) {
					this.Failure(this, new MessageEventArgs() { message = e.Message });
				}
			}

		}

		public ProximityDevice Device {
			get {
				if (rfidSocket != null) {
					return this.rfidSocket.Device;
				} else {
					return null;
				}
			}
		}

		public RFIDSocket Socket {
			get {
				return rfidSocket;
			}
		}

	}
}
