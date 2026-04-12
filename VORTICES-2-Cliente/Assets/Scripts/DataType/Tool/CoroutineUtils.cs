using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Vortices
{
    public static class CoroutineUtils
    {
        public static IEnumerator AsIEnumerator(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

    }

}
