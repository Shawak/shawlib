using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ShawLib
{
    public static class Extensions
    {
        public static readonly Encoding Encoding = Encoding.UTF8; //Encoding.GetEncoding("ISO-8859-1");

        // Values to Bytes
        static TypeSwitchReturnableArgs bytesBitConverterProvider = new TypeSwitchReturnableArgs()
            .Case<bool, bool>((bool value) => { return BitConverter.GetBytes(value); })
            .Case<byte, byte>((byte value) => { return new byte[] { value }; })
            .Case<sbyte, sbyte>((sbyte value) => { return new byte[] { (byte)value }; })
            .Case<short, short>((short value) => { return BitConverter.GetBytes(value); })
            .Case<ushort, ushort>((ushort value) => { return BitConverter.GetBytes(value); })
            .Case<int, int>((int value) => { return BitConverter.GetBytes(value); })
            .Case<uint, uint>((uint value) => { return BitConverter.GetBytes(value); })
            .Case<float, float>((float value) => { return BitConverter.GetBytes(value); })
            .Case<long, long>((long value) => { return BitConverter.GetBytes(value); })
            .Case<ulong, ulong>((ulong value) => { return BitConverter.GetBytes(value); })
            .Case<double, double>((double value) => { return BitConverter.GetBytes(value); })
            .Case<char, char>((char value) => { return BitConverter.GetBytes(value); })
            .Case<string, string>((string value) => { return Encoding.GetBytes(value); });

        public static byte[] GetBytes(this IConvertible val)
        {
            return (byte[])bytesBitConverterProvider.Switch(val.GetType(), val);
        }

        // Bytes to Values
        static TypeSwitchReturnableArgs typeBitConverterProvider = new TypeSwitchReturnableArgs()
            .Case<bool, byte[]>((byte[] bytes) => { return BitConverter.ToBoolean(bytes, 0); })
            .Case<byte, byte[]>((byte[] bytes) => { return bytes[0]; })
            .Case<sbyte, byte[]>((byte[] bytes) => { return (sbyte)bytes[0]; })
            .Case<short, byte[]>((byte[] bytes) => { return BitConverter.ToInt16(bytes, 0); })
            .Case<ushort, byte[]>((byte[] bytes) => { return BitConverter.ToUInt16(bytes, 0); })
            .Case<int, byte[]>((byte[] bytes) => { return BitConverter.ToInt32(bytes, 0); })
            .Case<uint, byte[]>((byte[] bytes) => { return BitConverter.ToUInt32(bytes, 0); })
            .Case<float, byte[]>((byte[] bytes) => { return BitConverter.ToSingle(bytes, 0); })
            .Case<long, byte[]>((byte[] bytes) => { return BitConverter.ToInt64(bytes, 0); })
            .Case<ulong, byte[]>((byte[] bytes) => { return BitConverter.ToUInt64(bytes, 0); })
            .Case<double, byte[]>((byte[] bytes) => { return BitConverter.ToDouble(bytes, 0); })
            .Case<char, byte[]>((byte[] bytes) => { return (char)((ushort)bytes[0] + bytes[1]); })
            .Case<string, byte[]>((byte[] bytes) => { return Encoding.GetString(bytes); });

        static Dictionary<Type, int> valueTypeLengths = new Dictionary<Type, int>()
        {
            { typeof(bool), 1 },
            { typeof(byte), 1 },
            { typeof(sbyte), 1 },
            { typeof(short), 2 },
            { typeof(ushort), 2 },
            { typeof(int), 4 },
            { typeof(uint), 4 },
            { typeof(float), 4 },
            { typeof(long), 8 },
            { typeof(ulong), 8 },
            { typeof(double), 8 },
            { typeof(char), 2 }
        };

        public static int MemSize(this IConvertible value)
        {
            return value.GetType().MemSize();
        }

        public static int MemSize(this Type type)
        {
            if (!valueTypeLengths.ContainsKey(type))
                //throw new Exception("can't get memory size of type " + type);
                return Marshal.SizeOf(type);

            return valueTypeLengths[type];
        }

        // Streams

        public static T To<T>(this byte[] bytes) where T : IConvertible
        {
            return (T)typeBitConverterProvider.Switch(typeof(T), bytes);
        }

        public static Stream Add(this Stream stream, IConvertible val)
        {
            byte[] data;
            if (val.GetType() == typeof(string))
            {
                data = val != null ? val.GetBytes() : String.Empty.GetBytes();
                var lengthBytes = data.Length.GetBytes();
                stream.Write(lengthBytes, 0, lengthBytes.Length);
            }
            else
            {
                data = val.GetBytes();
            }
            return stream.Add(data);
        }

        public static Stream Add(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            return stream;
        }

        public static T Get<T>(this Stream stream) where T : IConvertible
        {
            var type = typeof(T);
            var size = type == typeof(string) ? stream.Get<int>() : valueTypeLengths[type];
            var data = stream.Get(size);
            return data.To<T>();
        }

        public static byte[] Get(this Stream stream, int count)
        {
            var data = new byte[count];
            stream.Read(data, 0, count);
            return data;
        }

        // DateTime

        public static DateTime ConvertFromUnixTimestamp(this uint timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        public static uint ConvertToUnixTimestamp(this DateTime date)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var diff = date.ToUniversalTime() - origin;
            return (uint)Math.Floor(diff.TotalSeconds);
        }

        // Cast

        public static T To<T>(this IConvertible value) where T : IConvertible
        {
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
    }
}
