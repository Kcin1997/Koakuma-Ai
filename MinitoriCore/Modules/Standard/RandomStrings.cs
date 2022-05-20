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
                using var reader = new StreamReader(@"objects.txt");
                var temp = reader.ReadToEnd();
                objects = temp.Split('\n').Select(x => x.Trim()).Where(x => x != "").ToArray();
            }
        }
    }
}
