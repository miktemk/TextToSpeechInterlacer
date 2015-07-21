using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TtsMultilang
{
    public static class Utils
    {
        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> func)
        {
			var index = 0;
			foreach (var x in list) {
				if (func(x))
					return index;
				index++;
			}
            return -1;
        }

		/// <summary>
		/// null params will return false
		/// </summary>
		public static bool ContainsNoCase(this string text, string x) {
			if (text == null || x == null)
				return false;
			return text.ToLower().Contains(x.ToLower());
		}

		/// <summary>
		/// null params will return false
		/// </summary>
		public static bool StartsWithNoCase(this string text, string x) {
			if (text == null || x == null)
				return false;
			return text.ToLower().StartsWith(x.ToLower());
		}
		

    }
}
