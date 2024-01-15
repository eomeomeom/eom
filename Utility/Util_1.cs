using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class Extentions
{
    private static System.Random shuffleRnad = new System.Random();

    public static string ToString(this float value, Data.Enum.ValueType valueType)
    {   
        if (valueType == Data.Enum.ValueType.PERCENT)
        {
            string ret = (value*100).ToString();
            ret += "%";
            return ret;
        }
        return value.ToString();
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = shuffleRnad.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

public static class Utility
{
    public class WeightedRandom
    {
        private List<KeyValuePair<string, double>> candidates;

        public WeightedRandom(List<KeyValuePair<string, double>> target)
        {
            candidates = new List<KeyValuePair<string, double>>();

            double totalWeight = 0;
            foreach (KeyValuePair<string, double> pair in target)
            {
                totalWeight += pair.Value;
            }
            foreach (KeyValuePair<string, double> pair in target)
            {
                candidates.Add(new KeyValuePair<string, double>(pair.Key, pair.Value / totalWeight));
            }
            candidates.OrderBy(x => x.Value).ToArray();
        }

        public string GetRandom()
        {
            string ret = null;
            double pivot = Random.value;
            double acc = 0;
            foreach (KeyValuePair<string, double> pair in candidates)
            {
                acc += pair.Value;
                if (pivot <= acc)
                {
                    ret = pair.Key;
                    break;
                }
            }
            return ret;
        }
    }

    public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
    {
        // Check arguments.
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException("cipherText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("Key");

        // Declare the string used to hold
        // the decrypted text.
        string plaintext = null;

        // Create an RijndaelManaged object
        // with the specified key and IV.
        using (RijndaelManaged rijAlg = new RijndaelManaged())
        {
            rijAlg.Key = Key;
            rijAlg.IV = IV;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {

                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

        }

        return plaintext;
    }

    public static DateTime TimeStampToDateTime(long value)
    {
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dt = dt.AddSeconds(value).ToUniversalTime();
        return dt;
    }



    public static int RandomChoose(List<float> list)
    {
        float max = 0f;
        for (int i = 0; i < list.Count; ++i)
            max += list[i];

        float random = Random.Range(0.0f, max);

        float f = 0.0f;
        for (int i = 0; i < list.Count; ++i)
        {
            if (random >= f && random < list[i] + f)
                return i;

            f += list[i];
        }
        return -1;
    }

    public static string GetUUID()
    {
        return System.Guid.NewGuid().ToString();
    }

    public static bool MatchStringCaseIgnored(string a, string b)
    {
        return (0 == string.Compare(a, b, true));
    }

    public static bool StartsStringCaseIgnored(string str, string start)
    {
        if (null == str)
        {
            return (start == null);
        }

        return str.StartsWith(start, true, null);
    }


    public static bool IsStringValid(string str)
    {
        return (false == string.IsNullOrEmpty(str));
    }

    public static void DestroyImmediate(Object obj)
    {
        if (obj == null)
            return;

        if (true == Application.isEditor)
            Object.DestroyImmediate(obj);
        else
            Object.Destroy(obj);
    }

    public static bool ContainsCaseIgnored(this List<string> list, string a)
    {
        return (list.FindIndex(x => MatchStringCaseIgnored(a, x)) >= 0);
    }

    public static bool ContainsCaseIgnored(string container, string contained)
    {
        if (false == IsStringValid(container))
            return !IsStringValid(contained);

        return (container.IndexOf(contained, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public static void ChangeLayer(Transform trans, string name)
    {
        ChangeLayersRecursively(trans, name);
    }

    private static void ChangeLayersRecursively(Transform trans, string name)
    {
        trans.gameObject.layer = LayerMask.NameToLayer(name);
        foreach (Transform child in trans)
        {
            ChangeLayersRecursively(child, name);
        }
    }

    public static class ConvertNumberDigit
    {
        private static readonly (string, string)[] digitWordArr = new (string, string)[]
        {
            ("", ""),
            ("K", "k"), // kilo
            ("M", "m"), // mega
            ("G", "g"), // giga
            ("T", "t"), // tera
            ("P", "p"), // peta
            ("E", "e"), // exa
            ("Z", "z"), // zetta
            ("Y", "y"), // yotta
            ("R", "r"), // ronna
            ("Q", "q"), // quecca
        };
        private const int CONVERT_LIMIT_DIGIT = 4;
        private const int CONVERT_IGNORE_START_DIGIT = 5;
        public const string WRONG_CONVERT_RESULT = "Wrong Number String";

        public static string GetDigitWordByIndex(int index, bool smallLetter)
        {
            index = Mathf.Clamp(index, 0, digitWordArr.Length - 1);
            return smallLetter switch
            {
                false => digitWordArr[index].Item1,
                _ => digitWordArr[index].Item2
            };
        }
        public static string GetDigitWord(int placeNumber, bool smallLetter) => GetDigitWordByIndex(placeNumber / 3, smallLetter);

        public static string FromString(string numberStr, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
        {
            if (!decimal.TryParse(numberStr, out var parseResult)) return WRONG_CONVERT_RESULT;

            int len = numberStr.Length;
            if (len == 0) return "";

            bool isNegative = numberStr[0] == '-';
            if (isNegative) len--; // 음수 부호는 길이에 포함시키지 않음

            if (convertLimitDigit < 3) convertLimitDigit = 3;
            // 자릿수 변환 제한에 걸리면 변환하지 않고 리턴
            if (len <= convertLimitDigit) return numberStr;

            int quotient = (len - 1) / 3;
            int pivot = quotient * 3;
            int startIndex = len - pivot;
            if (isNegative) startIndex++;

            numberStr = numberStr.Remove(startIndex);
            var word = GetDigitWordByIndex(quotient, smallLetter);
            return numberStr + word;
        }
        public static string FromInt32(int value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
        {
            var ret = FromString(value.ToString(), smallLetter, convertLimitDigit);
            if(int.TryParse(ret, out int result))
            {
                ret = string.Format("{0:N0}", result);
            }
            return ret;
        }
        public static string FromUInt32(uint value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
            => FromString(value.ToString(), smallLetter, convertLimitDigit);
        public static string FromLong(long value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
            => FromString(value.ToString(), smallLetter, convertLimitDigit);
        public static string FromULong(ulong value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
            => FromString(value.ToString(), smallLetter, convertLimitDigit);
        public static string FromSingle(float value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
        {
            var ret = value.ToString();
            int decimalPoint = ret.IndexOf('.');
            if (decimalPoint > 0) ret = ret.Remove(decimalPoint);
            return FromString(ret, smallLetter, convertLimitDigit);
        }
        public static string FromDouble(double value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
        {
            var ret = value.ToString();
            int decimalPoint = ret.IndexOf('.');
            if (decimalPoint > 0) ret = ret.Remove(decimalPoint);
            return FromString(ret, smallLetter, convertLimitDigit);
        }
        public static string FromDecimal(decimal value, bool smallLetter, int convertLimitDigit = CONVERT_LIMIT_DIGIT)
        {
            var ret = value.ToString();
            int decimalPoint = ret.IndexOf('.');
            if (decimalPoint > 0) ret = ret.Remove(decimalPoint);
            return FromString(ret, smallLetter, convertLimitDigit);
        }
    }

    public static Color GetGradeColor(Data.Enum.Grade grade)
    {
        Color ret = Color.white;
        switch (grade)
        {
            case Data.Enum.Grade.RARE:
                ret = new Color(0f, 0f, 1f);
                break;
            case Data.Enum.Grade.EPIC:
                ret = new Color(1f, 0f, 1f);
                break;
            case Data.Enum.Grade.LEGEND:
                ret = new Color(1f, 1f, 0f);
                break;
            case Data.Enum.Grade.MYTH:
                ret = new Color(1f, 0f, 0f);
                break;
        }
        return ret;
    }
    
    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    public static T Clone<T>(this T src)
    {
        if (src is ICloneable)
        {
            return (T) (src as ICloneable).Clone();
        }

        using (var stream = new System.IO.MemoryStream())
        {
            var serializer = new DataContractSerializer(typeof(T));
            serializer.WriteObject(stream, src);
            stream.Position = 0;

            return (T) serializer.ReadObject(stream);
        }
    }
}


