using UnityEngine;
using System.Globalization;

public static class FormatUtility
{
    public static string FormatNumber(int value)
    {
        bool isNegative = value < 0;
        float num = Mathf.Abs(value);
        string result = "";

        if (num >= 1000000000)
            result = (Mathf.Floor(num / 100000000f) / 10f).ToString("0.#", CultureInfo.InvariantCulture) + "B";
        else if (num >= 1000000)
            result = (Mathf.Floor(num / 100000f) / 10f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        else if (num >= 1000)
            result = (Mathf.Floor(num / 100f) / 10f).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        else
            result = num.ToString(CultureInfo.InvariantCulture);

        return isNegative ? "-" + result : result;
    }
}
