﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;

namespace NetPresenter {
	class Multicaster {
		const int Port = 7575;
		static readonly IPEndPoint Endpoint = new IPEndPoint(IPAddress.Parse("234.75.75.75"), 7575);
		static readonly string InstanceId = Environment.MachineName + "/" + Process.GetCurrentProcess().Id;

		readonly Dispatcher dispatcher;
		Socket senderSocket;
		public Multicaster() {
			dispatcher = Dispatcher.CurrentDispatcher;
		}

		bool CreateSocket() {
			if (senderSocket != null && senderSocket.Connected) return true;
			if (senderSocket != null) senderSocket.Dispose();

			Log.Write("Creating send socket");

			senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, (ProtocolType)113);
			senderSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			senderSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

			try {
				senderSocket.Connect(Endpoint);
			} catch (SocketException ex) {
				Log.Write("ERROR connecting send Socket!\n" + ex);
				senderSocket.Dispose();
				senderSocket = null;
				return false;
			}
			return true;
		}

		///<summary>Configures the computer and application to support multicast sockets.</summary>
		///<returns>False if the current instance should terminate.</returns>
		public static bool CheckAccess() {
			try {
				using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, (ProtocolType)113)) {
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
					socket.Bind(new IPEndPoint(IPAddress.Any, 0));
					return true;
				}
			} catch (SocketException ex) {
				switch (ex.SocketErrorCode) {
					case SocketError.AccessDenied:
						if (MessageBoxResult.Yes == MessageBox.Show("TCP Multicast (RDM) requires administrative privileges.  Would you like to restart the application as administrator?",
																	"NetPresenter", MessageBoxButton.YesNo, MessageBoxImage.Warning)) {
							var args = Environment.GetCommandLineArgs();
							Process.Start(new ProcessStartInfo(args[0], string.Join(" ", args.Skip(1))) { Verb = "runas" });
							return false;
						}
						return true;
					case SocketError.SocketNotSupported:
						return !TryEnableMulticast(ex) || CheckAccess();
					default:
						MessageBox.Show("An unknown error occurred while creating a socket.  Network functionality will not work.\n\n" + ex.SocketErrorCode + ": " + ex.Message,
										"NetPresenter", MessageBoxButton.OK, MessageBoxImage.Error);
						return true;
				}
			}
		}

		static bool TryEnableMulticast(SocketException ex) {
			if (MessageBoxResult.Yes != MessageBox.Show("An error occurred while creating the socket used to communicate with instance on other computers: " + ex.Message + ".\n"
													  + "Would you like to enable Multicast support (requires administrative privileges)?",
														"NetPresenter", MessageBoxButton.YesNo, MessageBoxImage.Warning)) {
				Log.Write("Not enabling multicast.  Network functionality will not work.");
				return false;
			}

			const string flags = "/Online /Enable-Feature /FeatureName:MSMQ-Server /FeatureName:MSMQ-Container /FeatureName:MSMQ-Multicast";
			Log.Write("Enabling multicast: dism " + flags);
			Process.Start(new ProcessStartInfo("dism", flags) { Verb = "runas" }).WaitForExit();
			return true;
		}

		///<summary>Occurs when a command is received.</summary>
		public event EventHandler<CommandEventArgs> CommandReceived;
		///<summary>Raises the CommandReceived event.</summary>
		///<param name="e">A CommandEventArgs object that provides the event data.</param>
		internal protected virtual void OnCommandReceived(CommandEventArgs e) {
			if (CommandReceived != null)
				CommandReceived(this, e);
		}

		Thread listenerThread;
		public void StartListener() {
			listenerThread = new Thread(Listen) { IsBackground = true, Name = "Listener thread" };
			listenerThread.Start();
		}

		public void SendCommand(string commandName, params object[] args) {
			ThreadPool.QueueUserWorkItem(delegate {
				try {
					if (!CreateSocket()) {
						OnSendFail();
						return;
					}

					using (var stream = new MemoryStream()) {
						stream.WriteString(InstanceId);
						stream.WriteString(commandName);
						foreach (var arg in args) {
							var str = arg as string;
							if (str != null)
								stream.WriteString(str);

							else if (arg is double)
								stream.WriteNumber((double)arg);

							else if (arg is int)
								stream.WriteNumber((int)arg);

							else if (arg is long)
								stream.WriteNumber((long)arg);

							else if (arg is bool)
								stream.WriteNumber((bool)arg);

							else
								throw new ArgumentException("Unsupported argument " + arg.GetType(), "args");
						}
						try {
							senderSocket.Send(stream.ToArray());
						} catch (SocketException ex) {
							Log.Write("ERROR sending through socket!\n" + ex);
							senderSocket.Dispose();
							OnSendFail();
							senderSocket = null;
						}
					}
				} catch (Exception ex) {
					Log.Write("ERROR in SendCommand!\n" + ex);
				}
			});
		}
		static void OnSendFail() { }


		void Listen() {
			try {
				var accepterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, (ProtocolType)113);
				accepterSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

				accepterSocket.Bind(Endpoint);
				accepterSocket.Listen(1);
				while (true) {
					var receiver = accepterSocket.Accept();
					ThreadPool.QueueUserWorkItem(delegate {
						try {
							while (true) {
								MemoryStream stream;
								try {
									stream = receiver.ReadStream();
								} catch (SocketException ex) {
									Log.Write("ERROR reading from receiver socket!\n" + ex);

									//The other end exited
									if (ex.SocketErrorCode == SocketError.Disconnecting)
										return;     //Message: Returned by WSARecv or WSARecvFrom to indicate the remote party has initiated a graceful shutdown sequence
									else if (ex.SocketErrorCode == SocketError.ConnectionReset)     //TODO: Investigate
										return;     //Message: An existing connection was forcibly closed by the remote host
									continue;       //For all other errors, read again; I want resilience
								}


								var senderId = stream.ReadString();
								if (senderId == InstanceId)
									continue;

								var commandName = stream.ReadString();
								dispatcher.BeginInvoke(new Action(delegate {
									OnCommandReceived(new CommandEventArgs(commandName, stream));
								}));
							}
						} catch (Exception ex) {
							Log.Write("ERROR with receiver!\n" + ex);
						}
					});
				}
			} catch (Exception ex) {
				Log.Write("ERROR in Listen!\n" + ex);
			}
		}
	}

	///<summary>Provides data for CommandReceived events.</summary>
	public class CommandEventArgs : EventArgs {
		///<summary>Creates a new CommandEventArgs instance.</summary>
		public CommandEventArgs(string name, MemoryStream response) {
			Name = name;

			var argsBytes = new byte[response.Length - response.Position];
			response.ReadFill(argsBytes);
			argsCreator = () => new StreamCommandArguments(new MemoryStream(argsBytes, false));
		}
		readonly Func<CommandArguments> argsCreator;
		class StreamCommandArguments : CommandArguments {
			readonly Stream stream;
			public StreamCommandArguments(Stream stream) { this.stream = stream; }

			public override T ReadParameter<T>() {
				if (typeof(T) == typeof(string))
					return (T)(object)stream.ReadString();
				return stream.ReadNumber<T>();
			}
		}

		///<summary>Gets the command name.</summary>
		public string Name { get; private set; }
		///<summary>Gets the command parameters.</summary>
		public CommandArguments CreateArguments() { return argsCreator(); }
	}
}
