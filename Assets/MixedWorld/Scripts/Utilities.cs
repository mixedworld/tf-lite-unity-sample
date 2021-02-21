using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MixedWorld.Core
{

    /// <summary>
    /// Utilities class contains a collection of static methods useable at runtime.
    /// The Class follows the LazySingleton Pattern and selfinstantiates when called the first time.
    /// </summary>
    public class Utilities : MonoBehaviour
    {

        static Utilities mInstance;

        /// <summary>
        /// Checks if an instance of this class already exists, otherwise creates a new GO and appends a new instance as component.
        /// returns the only existing aka Singleton instance.
        /// </summary>
        public static Utilities Instance
        {
            get
            {
                return mInstance ? (mInstance = new GameObject("SingletonUtilities").AddComponent<Utilities>()) : mInstance;
            }
        }

        #region Static Functions
        /// <summary>
        /// Creates a Guid from a given seed.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public static Guid SeededGuid(int seed, System.Random random = null)
        {
            if (random == null) random = new System.Random(seed);
            return Guid.Parse(string.Format("{0:X4}{1:X4}-{2:X4}-{3:X4}-{4:X4}-{5:X4}{6:X4}{7:X4}",
                random.Next(0, 0xffff), random.Next(0, 0xffff),
                random.Next(0, 0xffff),
                random.Next(0, 0xffff) | 0x4000,
                random.Next(0, 0x3fff) | 0x8000,
                random.Next(0, 0xffff), random.Next(0, 0xffff), random.Next(0, 0xffff)));
        }

        /// <summary>
        /// Creates a 10 char length Guid from a given seed.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public static string SeededUID(int seed, System.Random random = null)
        {
            if (random == null) random = new System.Random(seed);
            return string.Format("{0:X4}{1:X4}{2:X2}", random.Next(0, 0xffff), random.Next(0, 0xffff),random.Next(0, 0xff));
        }

        /// <summary>
        /// Converts the current datetime to unix utc timestamp.
        /// </summary>
        /// <returns>timestamp as int</returns>
        public static int Timestamp()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        #endregion

        #region Runtime Functions
        #endregion
    }
}
