﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Comms;
using Engine.Serialization;

namespace SCMPG
{
	public abstract class Message
	{
		private static Dictionary<int, Type> MessageTypesByMessageId;

		private static Dictionary<Type, int> MessageIdsByMessageTypes;

		static Message()
		{
			MessageTypesByMessageId = new Dictionary<int, Type>();
			MessageIdsByMessageTypes = new Dictionary<Type, int>();
			TypeInfo[] array = (from t in typeof(Message).GetTypeInfo().Assembly.DefinedTypes
								where typeof(Message).GetTypeInfo().IsAssignableFrom(t)
								orderby t.Name
								select t).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				MessageTypesByMessageId[i] = array[i].AsType();
				MessageIdsByMessageTypes[array[i].AsType()] = i;
				Archive.SetTypeSerializationOptions(array[i], useObjectInfo: false, autoConstructObject: true);
			}
		}

		public static Message Read(byte[] bytes)
		{
			try
			{
				InputArchive inputArchive = new BinaryInputArchive(new MemoryStream(bytes));

				byte b = inputArchive.Serialize<byte>("MessageId");
				if (MessageTypesByMessageId.TryGetValue(b, out Type value3))
				{
					Message message = (Message)inputArchive.Serialize(value3.Name, value3);
					return message;
				}

				throw new InvalidOperationException($"Unknown message id {b}.");
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException("Received malformed network message.", innerException);
			}
		}

		public static byte[] Write(Message message)
		{
			OutputArchive outputArchive = new BinaryOutputArchive(new MemoryStream());
			outputArchive.Serialize("MessageId", (byte)MessageIdsByMessageTypes[message.GetType()]);
			outputArchive.Serialize(message.GetType().Name, message.GetType(), message);
			byte[] result = ((MemoryStream)((BinaryOutputArchive)outputArchive).Stream).ToArray();
			return result;
		}
	}
}