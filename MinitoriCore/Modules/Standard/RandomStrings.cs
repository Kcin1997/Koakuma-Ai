using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;

namespace MinitoriCore
{
    public class RandomStrings
    {
        public readonly string[] objects;

        public RandomStrings()
        {
            if (File.Exists("objects.txt"))
            {
                using (var reader = new StreamReader(@"objects.txt"))
                {
                    var temp = reader.ReadToEnd();
                    objects = temp.Split('\n').Select(x => x.Trim()).Where(x => x != "").ToArray();
                }
            }
        }

        private readonly RandomNumberGenerator rand = RandomNumberGenerator.Create();

        public int RandomInteger(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                rand.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (int)(min + (max - min) *
                (scale / (double)uint.MaxValue));
        }
    }
}
