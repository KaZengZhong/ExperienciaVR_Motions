using System.Collections.Generic;

namespace Vortices
{
    public static class CircularList
    {
        public static T GetElement<T>(List<T> list, int index)
        {
            T element;
        
            if(index >= list.Count)
            {
                int newindex = index % list.Count;
                element = list[newindex];
            } else if(index < 0)
            {
                int newindex = (index + list.Count) % list.Count;
                if(newindex < 0)
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
