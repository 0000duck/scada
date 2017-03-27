﻿/*
 * Copyright 2016 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : The class contains utility methods for the whole system
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2007
 * Modified : 2017
 */

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Scada
{
    /// <summary>
    /// The class contains utility methods for the whole system
    /// <para>Класс, содержащий вспомогательные методы для всей системы</para>
    /// </summary>
    public static partial class ScadaUtils
    {
        /// <summary>
        /// Задержка потока для экономии ресурсов, мс
        /// </summary>
        public const int ThreadDelay = 100;

        /// <summary>
        /// Начало отчёта времени, которое используется приложениями Rapid SCADA
        /// </summary>
        /// <remarks>Совпадает с началом отсчёта времени в OLE Automation и Delphi</remarks>
        public static readonly DateTime ScadaEpoch = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc);

        private static NumberFormatInfo nfi; // формат вещественных чисел


        /// <summary>
        /// Конструктор
        /// </summary>
        static ScadaUtils()
        {
            nfi = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        }


        /// <summary>
        /// Добавить "\" к имени директории, если необходимо
        /// </summary>
        public static string NormalDir(string dir)
        {
            dir = dir == null ? "" : dir.Trim();
            if (dir.Length > 0 && !dir.EndsWith(Path.DirectorySeparatorChar.ToString())) 
                dir += Path.DirectorySeparatorChar;
            return dir;
        }

        /// <summary>
        /// Скорректировать разделитель директории
        /// </summary>
        public static string CorrectDirectorySeparator(string path)
        {
            // Path.AltDirectorySeparatorChar == '/' для Mono на Linux, что некорректно 
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Комбинировать заданные дату и время в единое значение
        /// </summary>
        public static DateTime CombineDateTime(DateTime date, double time)
        {
            return date.AddDays(time - Math.Truncate(time));
        }

        /// <summary>
        /// Декодировать вещественное значение времени, преобразовав его в формат DateTime
        /// </summary>
        /// <remarks>Совместим с методом DateTime.FromOADate()</remarks>
        public static DateTime DecodeDateTime(double dateTime)
        {
            return ScadaEpoch.AddDays(dateTime);
        }

        /// <summary>
        /// Закодировать дату и время в вещественное значение времени
        /// </summary>
        /// <remarks>Совместим с методом DateTime.ToOADate()</remarks>
        public static double EncodeDateTime(DateTime dateTime)
        {
            return (dateTime - ScadaEpoch).TotalDays;
        }

        /// <summary>
        /// Определить, является ли заданная строка записью даты, используя Localization.Culture
        /// </summary>
        public static bool StrIsDate(string s)
        {
            DateTime dateTime;
            return DateTime.TryParse(s, Localization.Culture, DateTimeStyles.None, out dateTime) ?
                dateTime.TimeOfDay.TotalMilliseconds == 0 : false;
        }

        /// <summary>
        /// Преобразовать строку в дату, используя Localization.Culture
        /// </summary>
        public static DateTime StrToDate(string s)
        {
            DateTime dateTime;
            if (DateTime.TryParse(s, Localization.Culture, DateTimeStyles.None, out dateTime))
                return dateTime.Date;
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// Попытаться преобразовать строку в дату и время, используя Localization.Culture
        /// </summary>
        public static bool TryParseDateTime(string s, out DateTime result)
        {
            return DateTime.TryParse(s, Localization.Culture, DateTimeStyles.None, out result);
        }

        /// <summary>
        /// Преобразовать строку в вещественное число
        /// </summary>
        /// <remarks>Метод работает с разделителями целой части '.' и ','.
        /// Если преобразование невозможно, возвращается double.NaN</remarks>
        public static double StrToDouble(string s)
        {
            try { return ParseDouble(s); }
            catch { return double.NaN; }
        }

        /// <summary>
        /// Преобразовать строку в вещественное число
        /// </summary>
        /// <remarks>Метод работает с разделителями целой части '.' и ','.
        /// Если преобразование невозможно, вызывается исключение FormatException</remarks>
        public static double StrToDoubleExc(string s)
        {
            try { return ParseDouble(s); }
            catch { throw new FormatException(string.Format(CommonPhrases.NotNumber, s)); }
        }

        /// <summary>
        /// Преобразовать строку в вещественное число
        /// </summary>
        /// <remarks>Метод работает с разделителями целой части '.' и ','</remarks>
        public static double ParseDouble(string s)
        {
            lock (nfi)
            {
                nfi.NumberDecimalSeparator = s.Contains(".") ? "." : ",";
                return double.Parse(s, nfi);
            }
        }

        /// <summary>
        /// Попытаться преобразовать строку в вещественное число
        /// </summary>
        /// <remarks>Метод работает с разделителями целой части '.' и ','</remarks>
        public static bool TryParseDouble(string s, out double result)
        {
            lock (nfi)
            {
                nfi.NumberDecimalSeparator = s.Contains(".") ? "." : ",";
                return double.TryParse(s, NumberStyles.Float, nfi, out result);
            }
        }

        /// <summary>
        /// Преобразовать массив байт в строку на основе 16-ричного представления
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            return BytesToHex(bytes, 0, bytes == null ? 0 : bytes.Length);
        }

        /// <summary>
        /// Преобразовать заданный диапазон массива байт в строку на основе 16-ричного представления
        /// </summary>
        public static string BytesToHex(byte[] bytes, int index, int count)
        {
            StringBuilder sb = new StringBuilder();
            int last = index + count;
            for (int i = index; i < last; i++)
                sb.Append(bytes[i].ToString("X2"));
            return sb.ToString();
        }

        /// <summary>
        /// Преобразовать строку 16-ричных чисел в массив байт
        /// </summary>
        public static bool HexToBytes(string s, out byte[] bytes, bool skipWhiteSpace = false)
        {
            int strLen = s == null ? 0 : s.Length;
            int bufLen = strLen / 2;
            byte[] buf = new byte[bufLen];

            int strInd = 0;
            int bufInd = 0;
            bool parseOK = true;

            while (strInd < strLen && parseOK)
            {
                if (skipWhiteSpace)
                {
                    while (strInd < strLen && char.IsWhiteSpace(s[strInd]))
                    {
                        strInd++;
                    }
                    if (strInd == strLen)
                        break;
                }

                try
                {
                    buf[bufInd] = byte.Parse(s.Substring(strInd, 2), NumberStyles.HexNumber);
                    bufInd++;
                    strInd += 2;
                }
                catch
                {
                    parseOK = false;
                }
            }

            if (parseOK && bufInd > 0)
            {
                if (bufInd < bufLen)
                {
                    bytes = new byte[bufInd];
                    Array.Copy(buf, 0, bytes, 0, bufInd);
                }
                else
                {
                    bytes = buf;
                }
                return true;
            }
            else
            {
                bytes = buf;
                return false;
            }
        }

        /// <summary>
        /// Вычислить хеш-функцию MD5 по массиву байт
        /// </summary>
        public static string ComputeHash(byte[] bytes)
        {
            return BytesToHex(MD5.Create().ComputeHash(bytes));
        }

        /// <summary>
        /// Вычислить хеш-функцию MD5 по строке
        /// </summary>
        public static string ComputeHash(string s)
        {
            return ComputeHash(Encoding.UTF8.GetBytes(s));
        }
        
        /// <summary>
        /// Глубокое (полное) клонирование объекта
        /// </summary>
        /// <remarks>Все клонируемые объекты должны иметь атрибут Serializable</remarks>
        public static object DeepClone(object obj, SerializationBinder binder = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                if (binder != null)
                    bf.Binder = binder;
                bf.Serialize(ms, obj);

                ms.Position = 0;
                return bf.Deserialize(ms);
            }
        }

        /// <summary>
        /// Скорректировать имя типа для работы DeepClone
        /// </summary>
        public static void CorrectTypeName(ref string typeName)
        {
            if (typeName.Contains("System.Collections.Generic.List"))
            {
                // удаление информации о сборке
                int ind1 = typeName.IndexOf(",");
                int ind2 = typeName.IndexOf("]");
                if (ind1 < ind2)
                    typeName = typeName.Remove(ind1, ind2 - ind1);
            }
        }

        /// <summary>
        /// Получить время последней записи в файл
        /// </summary>
        public static DateTime GetLastWriteTime(string fileName)
        {
            try
            {
                return File.Exists(fileName) ? File.GetLastWriteTime(fileName) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}