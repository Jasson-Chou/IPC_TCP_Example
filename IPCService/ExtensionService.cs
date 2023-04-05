using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCService
{
    internal static class ClientPackagesExtension
    {
        public static void Close(this IEnumerable<ClientPackage> clientPakgs)
        {
            foreach (var clientPackage in clientPakgs)
            {
                clientPackage.Close();
            }
        }
    }

    internal static class ArrayExtensions
    {
        public static void Clear<T>(this T[] array)
        {
            Array.Clear(array, 0, array.Length);
        }

        public static void Fill<T>(this T[] array, T value)
        {
            int i = array.Length - 1;
            do
            {
                array[i] = value;
                --i;
            } while (i != 0);
        }

        public static void CopyTo<T>(this T[] sourceBuffer, T[] targetBuffer, int targetIndex, int copySize)
        {
            int buffIdx = 0;
            for (int idx = targetIndex; idx < targetIndex + copySize; idx++)
            {
                targetBuffer[idx] = sourceBuffer[buffIdx];
                ++buffIdx;
            }
        }

        public static void CopyTo<T>(this T[] sourceArray, int sourceIndex, T[] targetBuffer, int targetIndex, int copySize)
        {
            int cnt = 0;
            while (cnt < copySize)
            {
                targetBuffer[targetIndex] = sourceArray[sourceIndex];
                ++targetIndex;
                ++sourceIndex;
                ++cnt;
            }
        }
    }
}
