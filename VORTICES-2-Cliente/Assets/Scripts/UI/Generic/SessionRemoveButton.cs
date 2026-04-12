using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class SessionRemoveButton : MonoBehaviour
    {
        public SessionController controller;
        public UISession session;

        public void RemoveSession()
        {
            controller.RemoveSession(session);
        }
    }
}

