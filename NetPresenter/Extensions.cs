using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using WinDraw = System.Drawing;
using WinForms = System.Windows.Forms;
using System.Net.Sockets;
using System.IO;

namespace NetPresenter {
	static class Extensions {
		public static Rect ToRect(this WinDraw.Rectangle r) {
			return new Rect(r.X, r.Y, r.Width, r.Height);
		}
	}
	static class SocketExtensions {
		[ThreadStatic]
		static byte[] buffer;
		public static MemoryStream ReadStream(this Socket socket) {
			if (buffer == null) buffer = new byte[65536];
			var retVal = new MemoryStream();

			int read = socket.Receive(buffer);
			retVal.Write(buffer, 0, read);
			retVal.Position = 0;

			return retVal;
		}

	}
	static class StreamExtensions {
		#region Streams
		///<summary>Writes raw bytes to a stream.</summary>
		///<param name="stream">The stream to write to.</param>
		///<param name="data">The bytes to write.</param>
		///<remarks>This method removes the need for temporary variables to get the length of the byte array.</remarks>
		public static void WriteAllBytes(this Stream stream, byte[] data) { stream.Write(data, 0, data.Length); }

		///<summary>Fills a byte array from a stream.</summary>
		///<returns>The number of bytes read.  If the end of the stream was reached, this will be less than the size of the array.</returns>
		///<remarks>Stream.Read is not guaranteed to read length bytes even if it doesn't hit the end of the stream, so I wrote this method, which is.</remarks>
		public static int ReadFill(this Stream stream, byte[] buffer) { return stream.ReadFill(buffer, buffer.Length); }
		///<summary>Reads a given number of bytes into a byte array from a stream.</summary>
		///<returns>The number of bytes read.  If the end of the stream was reached, this will be less than the length.</returns>
		///<remarks>Stream.Read is not guaranteed to read length bytes even if it doesn't hit the end of the stream, so I wrote this method, which is.</remarks>
		public static int ReadFill(this Stream stream, byte[] buffer, int length) {
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			int position = 0;
			while (position < length) {
				var bytesRead = stream.Read(buffer, position, length - position);
				if (bytesRead == 0) break;
				position += bytesRead;
			}
			return position;
		}

		#region Numbers
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, short value) { stream.Write(BitConverter.GetBytes(value), 0, 2); }
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, int value) { stream.Write(BitConverter.GetBytes(value), 0, 4); }
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, long value) { stream.Write(BitConverter.GetBytes(value), 0, 8); }

		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, ushort value) { stream.Write(BitConverter.GetBytes(value), 0, 2); }
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, uint value) { stream.Write(BitConverter.GetBytes(value), 0, 4); }
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, ulong value) { stream.Write(BitConverter.GetBytes(value), 0, 8); }

		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, float value) { stream.Write(BitConverter.GetBytes(value), 0, 4); }
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, double value) { stream.Write(BitConverter.GetBytes(value), 0, 8); }

		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, char value) { stream.Write(BitConverter.GetBytes(value), 0, 2); }
		///<summary>Writes a number to a stream.</summary>
		public static void WriteNumber(this Stream stream, bool value) { stream.Write(BitConverter.GetBytes(value), 0, 1); }

		[ThreadStatic]
		static byte[] convertBuffer;
		static byte[] Read(this Stream stream, int length) {
			if (stream == null) throw new ArgumentNullException("stream");

			if (convertBuffer == null) convertBuffer = new byte[16];
			stream.ReadFill(convertBuffer, length);
			return convertBuffer;
		}
		///<summary>Reads a number from a stream.</summary>
		public static T ReadNumber<T>(this Stream stream) {
			if (stream == null) throw new ArgumentNullException("stream");

			switch (typeof(T).Name) {
				case "Byte": return (T)(object)stream.ReadByte();
				case "Int16": return (T)(object)BitConverter.ToInt16(stream.Read(2), 0);
				case "Int32": return (T)(object)BitConverter.ToInt32(stream.Read(4), 0);
				case "Int64": return (T)(object)BitConverter.ToInt64(stream.Read(8), 0);

				case "UInt16": return (T)(object)BitConverter.ToUInt16(stream.Read(2), 0);
				case "UInt32": return (T)(object)BitConverter.ToUInt32(stream.Read(4), 0);
				case "UInt64": return (T)(object)BitConverter.ToUInt64(stream.Read(8), 0);

				case "Single": return (T)(object)BitConverter.ToSingle(stream.Read(4), 0);
				case "Double": return (T)(object)BitConverter.ToDouble(stream.Read(8), 0);

				case "Char": return (T)(object)BitConverter.ToChar(stream.Read(2), 0);
				case "Boolean": return (T)(object)BitConverter.ToBoolean(stream.Read(1), 0);
				default:
					throw new InvalidOperationException("Cannot read type " + typeof(T));
			}
		}
		static readonly Encoding stringEncoding = Encoding.UTF8;
		///<summary>Writes a Pascal string to a stream.</summary>
		///<param name="target">The stream to write to.</param>
		///<param name="text">The string to write.</param>
		public static void WriteString(this Stream target, string text) {
			if (target == null) throw new ArgumentNullException("target");

			var bytes = stringEncoding.GetBytes(text);
			target.WriteNumber(bytes.Length);
			target.Write(bytes, 0, bytes.Length);
		}
		///<summary>Reads a Pascal string from a stream.</summary>
		///<param name="source">The stream to read from.</param>
		public static string ReadString(this Stream source) {
			var byteCount = source.ReadNumber<int>();
			if (byteCount == 0) return String.Empty;					//WCF can't handle Read(*,*,0) and closes the stream
			var bytes = new byte[byteCount];
			source.Read(bytes, 0, byteCount);
			return stringEncoding.GetString(bytes);
		}
		#endregion
		#endregion
	}
}
