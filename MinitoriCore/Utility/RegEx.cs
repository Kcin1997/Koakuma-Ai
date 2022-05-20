using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Configuration;

namespace MinitoriCore
{
    internal static class RegEx
    {
        private static readonly string diceRegex = @"\d+[dD]\d+";  // match roll
        private static readonly string opRegex = @"\s*[+\-*\/]\s*"; // match operator
        private static readonly string calcRegrex = @"(\d+[dD]\d+|\d+)";  // match roll or single number
        private static readonly string endRegex = @"(\s*#.*)*$";
        private static readonly string finalRegex = $"^{calcRegrex}({opRegex}{calcRegrex})*{endRegex}";  //match diceRegex once and then any count of (opRegrex + diceRegrex)
        private static RegexStringValidator dice = new RegexStringValidator(finalRegex);
        private static Regex dicePart = new Regex(diceRegex);
        private static Regex whitespace = new Regex(@"\s");

        internal static bool isRoll(String str)
        {
            try
            {
                dice.Validate(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static string parseRolls(String str)
        {
            
            str = whitespace.Replace(str, "");  //remove whitespace

            MatchCollection dice = dicePart.Matches(str);
            foreach (Match m in dice)
            {
                GroupCollection group = m.Groups;
                foreach (Group entry in group)
                {
                    string roll = str.Substring(entry.Index, entry.Length);
                    string[] rollPart = entry.Value.Split('D', 'd').Where(x => x.Trim().Length > 0).ToArray();
                    string temp = str.Substring(0, str.IndexOf(entry.Value));
                    if (rollPart.Length == 2) temp += '(' + RandomUtil.dice( Int32.Parse(rollPart[0]), Int32.Parse(rollPart[1]) ) + ')';
                    temp += str.Substring(str.IndexOf(entry.Value) + entry.Length);
                    str = temp;
                }
            }
            return str;
        }
    }
}
