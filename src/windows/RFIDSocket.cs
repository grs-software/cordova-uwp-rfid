using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace cordova_uwp_rfid {
	public delegate void NDEFMessageHandler(NDEFRecordShort message);
	public delegate void NDEFMessageHandler2(String message);

	#region RecordType
	public enum HeaderMask {
		MessageBegin = 0x80,
		MessageEnd = 0x40,
		ChunkFlag = 0x20,
		ShortRecord = 0x10,
		IndexLengthPresent = 0x08,
		RecordTypeMask = 0x07
	}
	public enum RecordType {
		Empty = 0x00,
		NfcRtd = 0x01,
		Mime = 0x02,
		Uri = 0x03,
		ExternalRtd = 0x04,
		Unknown = 0x05,
		Unchanged = 0x06,
		Reserved = 0x07
	}
	#endregion

	#region NDEFRecordShort
	public sealed class NDEFRecordShort {

		public static NDEFRecordShort CreateNewForWrite(RecordType recordType, String type, String payload) {
			NDEFRecordShort record = new NDEFRecordShort();

			byte[] bytesType = Encoding.ASCII.GetBytes(type);
			byte[] bytesPayload = Encoding.ASCII.GetBytes(payload);

			// Record Daten initialisieren
			record.Data = new byte[3 + bytesType.Length + bytesPayload.Length]; // 3 Bytes Versatz vom Header + Längenangaben

			// Header setzen
			record.Data[0] = (byte)(HeaderMask.MessageBegin | HeaderMask.MessageEnd | HeaderMask.ShortRecord);
			record.Data[0] |= (byte)recordType;

			// Type Länge
			record.Data[1] = (byte)bytesType.Length;

			// Payload Länge
			record.Data[2] = (byte)bytesPayload.Length;

			for (int i = 0; i < bytesType.Length; i++) {
				record.Data[i + 3] = bytesType[i]; // 3 Bytes Versatz vom Header + Längenangaben
			}

			for (int i = 0; i < bytesPayload.Length; i++) {
				record.Data[i + 3 + bytesType.Length] = bytesPayload[i]; // 3 Bytes Versatz vom Header + Längenangaben zusätzlich der Offset der durch den Type entsteht
			}

			return record;
		}

		public NDEFRecordShort() {

		}

		public bool MessageBegin {
			get {
				return hasHeaderMask(HeaderMask.MessageBegin);
			}
		}

		public bool MessageEnd {
			get {
				return hasHeaderMask(HeaderMask.MessageEnd);
			}
		}

		public bool Chunked {
			get {
				return hasHeaderMask(HeaderMask.ChunkFlag);
			}
		}

		public bool ShortRecord {
			get {
				return hasHeaderMask(HeaderMask.ShortRecord);
			}
		}

		public bool IdentityLengthPresent {
			get {
				return hasHeaderMask(HeaderMask.IndexLengthPresent);
			}
		}

		public RecordType RecordType {
			get {
				return (RecordType)getHeaderMasked(HeaderMask.RecordTypeMask);
			}
		}

		public byte TypeLength {
			get {
				return Data[1];
			}
		}

		public byte PayloadLength {
			get {
				return Data[2];
			}
		}

		public string Type {
			get {
				byte[] typeArray = new byte[TypeLength];

				for (int i = 0; i < TypeLength; i++) {
					typeArray[i] = Data[i + 3]; // Type beginnt erst ab 3
				}

				return Encoding.UTF8.GetString(typeArray);
			}
		}

		public string Payload {
			get {
				byte[] payloadArray = new byte[PayloadLength];

				for (int i = 0; i < PayloadLength; i++) {
					payloadArray[i] = Data[i + TypeLength + 3]; // Type beginnt erst ab 3 + das gesamte Type-Segment
				}

				return Encoding.UTF8.GetString(payloadArray);
			}
		}

		public byte[] Data {
			get;
			set;
		}

		private bool hasHeaderMask(HeaderMask mask) {
			return getHeaderMasked(mask) == (byte)mask;
		}

		private byte getHeaderMasked(HeaderMask mask) {
			return (byte)(Data[0] & ((byte)mask));
		}

		public string HeaderToString {
			get {
				string output = "";
				output += (MessageBegin ? 1 : 0);
				output += (MessageEnd ? 1 : 0);
				output += (Chunked ? 1 : 0);
				output += (ShortRecord ? 1 : 0);
				output += (IdentityLengthPresent ? 1 : 0);
				output += RecordType.ToString("g");
				return output;
			}
		}
	}
	#endregion

	public sealed class RFIDSocket {

		private const int NO_SUBSCRIPTION = -1;
		private ProximityDevice device;
		private DeviceArrivedEventHandler arrivedHandler;
		private DeviceDepartedEventHandler departedHandler;
		private NDEFMessageHandler ndefMessageHandler;
		private long subscriptionID;
		private NDEFRecordShort record;
		private MessageTransmittedHandler transmitHandler;
		private String lastMessage;

		public RFIDSocket() {
			this.device = ProximityDevice.GetDefault();
			this.subscriptionID = RFIDSocket.NO_SUBSCRIPTION;
			this.arrivedHandler = null;
			this.departedHandler = null;
			this.transmitHandler = null;
			this.lastMessage = "Nix drin";
		}

		~RFIDSocket() {
			this.UnsubscribeForMessages();
		}

		private void PublishRecordToTag(NDEFRecordShort record, MessageTransmittedHandler handler) {
			if (record == null ||
				record.Data == null)
				return;

			IBuffer buffer = record.Data.AsBuffer();

			device.PublishBinaryMessage("NDEF:WriteTag", buffer, (sender, msgID) => {
				if (handler != null) {
					handler(sender, msgID);
				}

				device.StopPublishingMessage(msgID);
			});
		}

		public void StartPublishing(NDEFRecordShort record, MessageTransmittedHandler handler) {
			this.record = record;
			this.transmitHandler = handler;
			this.device.DeviceArrived += Device_DeviceArrived;
			PublishingMode = true;
		}

		public void StopPublishing() {
			this.device.DeviceArrived -= Device_DeviceArrived;
			PublishingMode = false;
		}

		public void SubscribeForMessages() {

			this.subscriptionID = this.device.SubscribeForMessage("NDEF", (sender, message) => {

				byte[] rawMessage = message.Data.ToArray();

				NDEFRecordShort record = new NDEFRecordShort();
				record.Data = rawMessage;

				lastMessage = record.Payload;
		    });
		}

		public void UnsubscribeForMessages() {
			// Plausi...
			if (RFIDSocket.NO_SUBSCRIPTION == this.subscriptionID)
				return;

			this.device.StopSubscribingForMessage(subscriptionID);
			this.subscriptionID = RFIDSocket.NO_SUBSCRIPTION;
		}

		private void Device_DeviceArrived(ProximityDevice sender) {
			PublishRecordToTag(this.record, this.transmitHandler);
		}

		private void messageReceivedHandler(ProximityDevice sender, ProximityMessage message) {

			byte[] rawMessage = message.Data.ToArray();

			NDEFRecordShort record = new NDEFRecordShort();
			record.Data = rawMessage;

			if (this.ndefMessageHandler != null) {
				this.ndefMessageHandler(record);
			}

		}

		#region GetterAndSetter
		public bool PublishingMode {
			get;
			private set;
		}

		public NDEFMessageHandler MessageHandler {
			get {
				return this.ndefMessageHandler;
			}

			set {
				this.ndefMessageHandler = value;
			}

		}

		public DeviceArrivedEventHandler ArrivedHandler {
			get {
				return this.arrivedHandler;
			}

			set {

				if (this.arrivedHandler != null) {
					device.DeviceArrived -= this.arrivedHandler;
				}

				this.arrivedHandler = value;

				if (value != null) {
					device.DeviceArrived += value;
				}
			}
		}

		public DeviceDepartedEventHandler DepartedHandler {
			get {
				return this.departedHandler;
			}

			set {
				if (this.departedHandler != null) {
					device.DeviceDeparted -= this.departedHandler;
				}

				this.departedHandler = value;

				if (value != null) {
					device.DeviceDeparted += value;
				}
			}
		}

		public ProximityDevice Device {
			get {
				return this.device;
			}

			set {
				this.device = value;
			}
		}

		public String LastMessage {
			get {
				return this.lastMessage;
			}

			set {
				this.lastMessage = value;
			}
		}
		#endregion
	}
}
