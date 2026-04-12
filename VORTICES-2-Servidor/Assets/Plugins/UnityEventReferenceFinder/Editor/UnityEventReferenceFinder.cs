using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.Events;
using System.Linq;

namespace UnityEventReferenceViewer
{
    public class EventReferenceInfo
    {
        public Object Owner { get; set; }
        public List<Object> Listeners { get; set; } = new List<Object>();
        public List<string> MethodNames { get; set; } = new List<string>();
    }

    public class UnityEventReferenceFinder : MonoBehaviour
    {

        [ContextMenu("FindReferences")]
        public void FindReferences()
        {
            FindAllUnityEventsReferences();
        }

        public static List<EventReferenceInfo> FindAllUnityEventsReferences()
        {
            var infos = new List<EventReferenceInfo>();
            foreach (var behaviour in Resources.FindObjectsOfTypeAll<Component>())
            {
                var events = behaviour.GetType().GetTypeInfo().DeclaredFields.Where(f => f.FieldType.IsSubclassOf(typeof(UnityEventBase))).ToList();
                foreach (var e in events)
                {
                    var eventValue = e.GetValue(behaviour) as UnityEventBase;
                    int count = eventValue.GetPersistentEventCount();
                    var info = new EventReferenceInfo();
                    info.Owner = behaviour;

                    for (int i = 0; i < count; i++)
                    {
                        var obj = eventValue.GetPersistentTarget(i);
                        var method = eventValue.GetPersistentMethodName(i);

                        info.Listeners.Add(obj);
                        info.MethodNames.Add(obj.GetType().Name.ToString() + "." + method);
                    }

                    infos.Add(info);
                }
            }
            return infos;
        }
    }
}