﻿using System;
using System.Security.Cryptography;

namespace MinitoriCore
{

    public static class RandomUtil
    {

        static RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        public static int Int(int max) =>
          Int(0, max);

        public static int Int(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                _rng.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (int)(min + (max - min) * (scale / (double)uint.MaxValue));
        }

        public static string dice(int count, int die)
        {
            if (count > 100) throw new Exception("Too Many Dice");
            string output = "";
            for (int i = 0; i < count; i++)
            {
                output += (Int(0, die) + 1);
                if (i + 1 < count) output += '+';
            }
            return output;
        }

    }

}
