using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public struct ULongGuid : IEquatable<ULongGuid>
    {
        [SerializeField]
        private ulong value1;
        [SerializeField]
        private ulong value2;

        public ULongGuid(ulong guidValue1, ulong guidValue2)
        {
            value1 = guidValue1;
            value2 = guidValue2;
        }

        public static ULongGuid NewGuid()
        {
            Guid guid = Guid.NewGuid();
            byte[] guidBytes = guid.ToByteArray();
            ulong guidValue1 = BitConverter.ToUInt64(guidBytes, 0);
            ulong guidValue2 = BitConverter.ToUInt64(guidBytes, 8);

            return new ULongGuid(guidValue1, guidValue2);
        }

        public (ulong, ulong) GetValue()
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
            if (obj is ULongGuid other)
            {
                return Equals(other);
            }
            return false;
        }

        public bool Equals(ULongGuid other)
        {
            return value1 == other.value1 && value2 == other.value2;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value1, value2);
        }

        public static bool operator ==(ULongGuid left, ULongGuid right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ULongGuid left, ULongGuid right)
        {
            return !(left == right);
        }
    }
}