#if DEVELOPMENT_BUILD || UNITY_EDITOR
#define DEBUG_NAMES
#endif

using System;
using System.Collections.Generic;

using UnityEngine;

namespace MixedWorld.Utility
{
    /// <summary>
    /// Represents a string identifier that is compressed into a 32 bit hash value to improve
    /// storage and comparison performance. 
    /// 
    /// The internal hash values are deterministic and consistent across platforms, so
    /// <see cref="NameHash32"/> can be safely serialized and used for cross-device communication. 
    /// However, due to collision probabilities and for diagnostic purposes, long-term persistent
    /// storage is not recommended. 
    /// 
    /// For collision probabilities, see: http://preshing.com/20110504/hash-collision-probabilities/
    /// </summary>
    [Serializable]
    public struct NameHash32 : IEquatable<NameHash32>
    {
        public static readonly NameHash32 Empty = (NameHash32)0;


        [SerializeField] private uint hash;

        /// <summary>
        /// Hash value of the string name that was originally used to create this struct.
        /// </summary>
        public uint Value
        {
            get { return this.hash; }
        }


        /// <summary>
        /// Creates a new <see cref="NameHash32"/> using the specified <see cref="Value"/>.
        /// </summary>
        /// <param name="value"></param>
        public NameHash32(uint value)
        {
            this.hash = value;
        }
        /// <summary>
        /// Creates a new <see cref="NameHash32"/> based on the specified string.
        /// </summary>
        /// <param name="name"></param>
        public NameHash32(string name)
        {
            this.hash = CalculateHash(name);
        }

        public bool Equals(NameHash32 other)
        {
            return this.hash == other.hash;
        }
        public override bool Equals(object obj)
        {
            if (obj is NameHash32)
                return this.Equals((NameHash32)obj);
            else
                return false;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (int)this.hash;
            }
        }
        public override string ToString()
        {
#if DEBUG_NAMES && false
            string name = GetName(this.hash);
            if (name != null)
            {
                return string.Format("{0:X8} '{1}'", this.hash, name);
            }
#endif
            return string.Format("{0:X8}", this.hash);
        }

        public static bool operator ==(NameHash32 a, NameHash32 b)
        {
            return a.hash == b.hash;
        }
        public static bool operator !=(NameHash32 a, NameHash32 b)
        {
            return a.hash != b.hash;
        }
        public static explicit operator NameHash32(uint value)
        {
            return new NameHash32(value);
        }
        public static explicit operator NameHash32(string name)
        {
            return new NameHash32(name);
        }

        private static uint CalculateHash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return Empty.Value;

            // Taken from here: https://stackoverflow.com/a/36845864, modified slightly.
            // DO NOT CHANGE, as it will break compatibility with persisted hash values.
            uint hash;
            unchecked
            {
                uint first = 5381;
                uint second = first;

                for (int i = 0; i < str.Length; i += 2)
                {
                    first = ((first << 5) + first) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    second = ((second << 5) + second) ^ str[i + 1];
                }

                hash = first + (second * 1566083941);
            }
#if DEBUG_NAMES
            RegisterHash(str, hash);
#endif
            return hash;
        }

#if DEBUG_NAMES
        private static Dictionary<uint, string> namesByHash = new Dictionary<uint, string>();

        private static void RegisterHash(string name, uint value)
        {
            string existingName;
            if (namesByHash.TryGetValue(value, out existingName) && existingName != name)
            {
                throw new Exception(string.Format(
                    "Hash collision detected between '{0}' and '{1}' with value '{2}'.",
                    name,
                    existingName,
                    value));
            }
            namesByHash[value] = name;
        }
        private static string GetName(uint value)
        {
            string name;
            if (namesByHash.TryGetValue(value, out name))
                return name;
            else
                return null;
        }
#endif
    }
}
