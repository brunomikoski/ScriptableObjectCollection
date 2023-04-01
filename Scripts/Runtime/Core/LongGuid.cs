using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public struct LongGuid : IEquatable<LongGuid>
    {
        [SerializeField]
        private long value1;
        [SerializeField]
        private long value2;

        public LongGuid(long guidValue1, long guidValue2)
        {
            value1 = guidValue1;
            value2 = guidValue2;
        }

        public static LongGuid NewGuid()
        {
            Guid guid = Guid.NewGuid();
            byte[] guidBytes = guid.ToByteArray();
            long guidValue1 = BitConverter.ToInt64(guidBytes, 0);
            long guidValue2 = BitConverter.ToInt64(guidBytes, 8);

            return new LongGuid(guidValue1, guidValue2);
        }

        public (long, long) GetValue()
        {
            return (value1, value2);
        }

        public bool IsValid()
        {
            return value1 != 0 || value2 != 0;
        }

        public override string ToString()
        {
            return value1.ToString("X16") + value2.ToString("X16");
        }

        public override bool Equals(object obj)
        {
            if (obj is LongGuid other)
            {
                return Equals(other);
            }
            return false;
        }

        public bool Equals(LongGuid other)
        {
            return value1 == other.value1 && value2 == other.value2;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(value1, value2).GetHashCode();
        }

        public static bool operator ==(LongGuid left, LongGuid right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LongGuid left, LongGuid right)
        {
            return !(left == right);
        }
        
        public byte[] ToByteArray()
        {
            byte[] byteArray = new byte[16];
            BitConverter.GetBytes(value1).CopyTo(byteArray, 0);
            BitConverter.GetBytes(value2).CopyTo(byteArray, 8);
            return byteArray;
        }
        
        public static LongGuid FromByteArray(byte[] byteArray)
        {
            if (byteArray.Length != 16)
            {
                throw new ArgumentException("Invalid byte array length. Expected 16 bytes.");
            }

            long guidValue1 = BitConverter.ToInt64(byteArray, 0);
            long guidValue2 = BitConverter.ToInt64(byteArray, 8);

            return new LongGuid(guidValue1, guidValue2);
        }
    }
}