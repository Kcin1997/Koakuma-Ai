using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MinitoriCore
{
    public class JsonStorage
    {
        public static async Task SerializeObjectToFile<T>(T obj, string fileName)
        {
            #region Validations

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (String.IsNullOrEmpty(fileName) || String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            #endregion

            while (true)
            {
                try
                {
                    using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                        await writer.WriteAsync(JsonConvert.SerializeObject(obj, Formatting.Indented));
                    break;
                }
                catch (IOException) //In use
                {
                    await Task.Delay(1000);
                }
            }
        }

        public static T DeserializeObjectFromFile<T>(string fileName)
        {
            #region Validations

            if (String.IsNullOrEmpty(fileName) || String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (!File.Exists(fileName))
                throw new FileNotFoundException($"{fileName} is missing.");

            #endregion

            return JsonConvert.DeserializeObject<T>(File.ReadAllText(fileName));
        }
    }
}
