using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public static class CircularList
    {
        public static T GetElement<T>(List<T> list, int index)
        {
            if (list == null)
            {
                Debug.LogError("[CircularList] La lista es NULL.");
                return default(T);
            }

            if (list.Count == 0)
            {
                Debug.LogError("[CircularList] La lista está VACÍA. No se puede acceder a ningún elemento.");
                return default(T);
            }

            Debug.Log($"[CircularList] Intentando acceder al índice {index} de una lista con {list.Count} elementos.");

            T element;

            if (index >= list.Count)
            {
                int newindex = index % list.Count;
                element = list[newindex];
            }
            else if (index < 0)
            {
                int newindex = (index + list.Count) % list.Count;
                if (newindex < 0)
                {
                    newindex *= -1;
                    element = list[list.Count - newindex]; 
                }
                else
                {
                    element = list[newindex];
                }
            }
            else
            {
                element = list[index];
            }

            return element;
        }

    }
}
