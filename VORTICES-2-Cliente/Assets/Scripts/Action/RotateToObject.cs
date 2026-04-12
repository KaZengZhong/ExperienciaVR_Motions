using UnityEngine;

namespace Vortices
{
    public class RotateToObject : MonoBehaviour
    {
        [SerializeField] private Vector3 followObject;

        private Transform followingPosition;
        private bool follow;

        // Settings
        public Vector3 offset;
        public string followName = "";
        private bool followAxisY;


        public void StartRotating(bool followAxisY)
        {
            follow = true;
            this.followAxisY = followAxisY;

            if (followName != "")
            {
                followingPosition = GameObject.Find(followName).transform;

            }
            else if (followObject == Vector3.zero)
            {
                followObject = Camera.main.gameObject.transform.position;
            }
        }

        private void Update()
        {
            if(follow)
            {
                if (!followAxisY)
                {
                    followObject = new Vector3(followingPosition.position.x, transform.position.y, followingPosition.position.z);
                }
                else
                {
                    followObject = followingPosition.position;
                }
                Quaternion lookRotation = Quaternion.LookRotation(followObject - transform.position);
                Quaternion lookDirection = lookRotation * Quaternion.Euler(offset.x, offset.y, offset.z);
                transform.rotation = lookDirection;
            }
        }
    }
}
