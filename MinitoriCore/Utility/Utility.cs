﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace MinitoriCore
{

    public static class Utility
    {

        //TODO: Expose as config option
        const int MaxRetries = 20;
        const string LogDateFormat = "yyyy-MM-dd HH:mm:ss";

        public static string DateString(DateTime date)
        {
            return date.ToString(LogDateFormat);
        }

        public static string DateString(DateTimeOffset date)
        {
            return date.ToString(LogDateFormat);
        }

        public static async Task FileIO(Func<Task> fileIOaction,
                                        Action retry = null,
                                        Action failure = null)
        {
            var success = false;
            var tries = 0;
            while (!success)
            {
                try
                {
                    await fileIOaction();
                    success = true;
                }
                catch (IOException)
                {
                    if (tries <= MaxRetries)
                    {
                        retry?.Invoke();
                        tries++;
                        await Task.Delay(100);
                    }
                    else
                    {
                        //Log.Error("Failed to perform file IO. Max retries exceeded.");
                        failure?.Invoke();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Trace: {ex}");
                   //Log.Error(e);
                }
            }
        }

    }

}
