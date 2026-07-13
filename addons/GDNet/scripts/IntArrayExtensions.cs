using System;
using System.Collections.Generic;

namespace GDNetExtensions
{
    public static class IntArrayExtensions
    {
        public static bool Erase(this int[] array, int value)
        {
            if (array == null || array.Length == 0) return false;

            int index = Array.IndexOf(array, value);
            if (index == -1) return false;

            // Сдвигаем элементы влево
            for (int i = index; i < array.Length - 1; i++)
            {
                array[i] = array[i + 1];
            }

            Array.Resize(ref array, array.Length - 1);
            return true;
        }

        public static bool RemoveAt(this int[] array, int index)
        {
            if (array == null || array.Length == 0) return false;
            if (index < 0 || index >= array.Length) return false;

            for (int i = index; i < array.Length - 1; i++)
            {
                array[i] = array[i + 1];
            }

            Array.Resize(ref array, array.Length - 1);
            return true;
        }

        public static bool Has(this int[] array, int value)
        {
            return array != null && Array.IndexOf(array, value) != -1;
        }

        public static int Find(this int[] array, int value)
        {
            return array == null ? -1 : Array.IndexOf(array, value);
        }
    }

}

