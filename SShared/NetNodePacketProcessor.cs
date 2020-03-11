/*
Based on LiteNetLib/Utils/NetPacketProcessor.cs

MIT License

Copyright (c) 2019 Ruslan Pyrch

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SShared
{
    /// <summary>
    /// Like LiteNetLib.PacketProcessor, but stores multiple callbacks per type.
    /// </summary>
    public class NetNodePacketProcessor
    {
        private static class HashCache<T>
        {
            public static bool Initialized;
            public static ulong Id;
        }

        /// <summary>
        /// A event handler for when a message is received.
        /// </summary>
        public class EventDelegates<T> where T : class
        {
            public delegate void Delegate(NetPeer sender, T message);

            public delegate void OnMessageReceivedEventHandler(NetPeer sender, T message);

            public event OnMessageReceivedEventHandler OnMessageReceived;
        }

        private class TypeErasedEventHandler
        {
            public Type TType;
            public Type TEventDelegatesType;
            public object TEventDelegates;
            public delegate object TDeserializer(NetSerializer serializer, NetDataReader reader);
            public TDeserializer DeserializeT;

            public static TypeErasedEventHandler ForTType<T>() where T : class, INetSerializable, new()
            {
                return new TypeErasedEventHandler()
                {
                    TType = typeof(T),
                    TEventDelegatesType = typeof(EventDelegates<T>),
                    TEventDelegates = new EventDelegates<T>(),
                    DeserializeT = (serializer, reader) =>
                    {
                        T t = new T();
                        t.Deserialize(reader);
                        return t;
                    }
                };
            }

            public void Invoke(NetPeer sender, string eventName, object arg)
            {
                var evtField = TEventDelegatesType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => field.Name == "OnMessageReceived").First();
                var evtDelegate = (MulticastDelegate)evtField.GetValue(TEventDelegates);

                foreach (var evtHandler in evtDelegate.GetInvocationList())
                {
                    evtHandler.Method.Invoke(evtHandler.Target, new object[] { sender, arg });
                }
            }
        }
        private readonly NetSerializer _netSerializer;
        private readonly Dictionary<ulong, TypeErasedEventHandler> _eventHandlers = new Dictionary<ulong, TypeErasedEventHandler>();
        private readonly NetDataWriter _netDataWriter = new NetDataWriter();

        public NetNodePacketProcessor()
        {
            _netSerializer = new NetSerializer();
        }

        public NetNodePacketProcessor(int maxStringLength)
        {
            _netSerializer = new NetSerializer(maxStringLength);
        }

        //FNV-1 64 bit hash
        protected virtual ulong GetHash<T>()
        {
            if (HashCache<T>.Initialized)
                return HashCache<T>.Id;

            ulong hash = 14695981039346656037UL; //offset
            string typeName = typeof(T).FullName;
            for (var i = 0; i < typeName.Length; i++)
            {
                hash = hash ^ typeName[i];
                hash *= 1099511628211UL; //prime
            }
            HashCache<T>.Initialized = true;
            HashCache<T>.Id = hash;
            return hash;
        }

        protected void InvokeEvent(NetDataReader reader, NetPeer sender, object tMessage)
        {
            var hash = reader.GetULong();
            TypeErasedEventHandler handler;
            if (!_eventHandlers.TryGetValue(hash, out handler))
            {
                throw new ParseException("Undefined packet in NetDataReader");
            }
            handler.Invoke(sender, "OnMessageReceived", tMessage);
        }

        protected virtual void WriteHash<T>(NetDataWriter writer)
        {
            writer.Put(GetHash<T>());
        }

        /// <summary>
        /// Register nested property type
        /// </summary>
        /// <typeparam name="T">INetSerializable structure</typeparam>
        /// <returns>True - if register successful, false - if type already registered</returns>
        public bool RegisterNestedType<T>() where T : struct, INetSerializable
        {
            return _netSerializer.RegisterNestedType<T>();
        }

        /// <summary>
        /// Register nested property type
        /// </summary>
        /// <param name="writeDelegate"></param>
        /// <param name="readDelegate"></param>
        /// <returns>True - if register successful, false - if type already registered</returns>
        public bool RegisterNestedType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
        {
            return _netSerializer.RegisterNestedType<T>(writeDelegate, readDelegate);
        }

        /// <summary>
        /// Register nested property type
        /// </summary>
        /// <typeparam name="T">INetSerializable class</typeparam>
        /// <returns>True - if register successful, false - if type already registered</returns>
        public bool RegisterNestedType<T>(Func<T> constructor) where T : class, INetSerializable
        {
            return _netSerializer.RegisterNestedType(constructor);
        }

        /// <summary>
        /// Reads all available data from NetDataReader and calls OnReceive delegates
        /// </summary>
        /// <param name="sender">Who sent the packet</param>
        /// <param name="reader">NetDataReader with packets data</param>
        public void ReadAllPackets(NetPeer sender, NetDataReader reader)
        {
            while (reader.AvailableBytes > 0)
            {
                ulong tHash = reader.GetULong();
                if (!_eventHandlers.ContainsKey(tHash))
                {
                    throw new KeyNotFoundException($"Unknown message type: {tHash:X}");
                }
                else
                {
                    var handler = _eventHandlers[tHash];
                    var tMessage = handler.DeserializeT(_netSerializer, reader);
                    handler.Invoke(sender, "OnMessageReceived", tMessage);
                }
            }
        }

        public void Send<T>(NetPeer peer, T packet, DeliveryMethod options) where T : class, INetSerializable, new()
        {
            _netDataWriter.Reset();
            Write(_netDataWriter, packet);
            peer.Send(_netDataWriter, options);
        }

        public void Write<T>(NetDataWriter writer, T packet) where T : class, INetSerializable, new()
        {
            WriteHash<T>(writer);
            packet.Serialize(writer);
        }

        /// <summary>
        /// Returns the event callbacks invoked for messages of type `T`.
        /// </summary>
        public EventDelegates<T> Events<T>() where T : class, INetSerializable, new()
        {
            if (!_eventHandlers.ContainsKey(GetHash<T>()))
            {
                _eventHandlers[GetHash<T>()] = TypeErasedEventHandler.ForTType<T>();
            }
            return _eventHandlers[GetHash<T>()].TEventDelegates as EventDelegates<T>;
        }
    }
}
