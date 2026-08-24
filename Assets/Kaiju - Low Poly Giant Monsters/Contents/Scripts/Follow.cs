//Simple navigation script to show off how the procedural animation can be used.




using UnityEngine;


namespace DistantLands
{
    public class Follow : MonoBehaviour
    {

        public Transform target;
        public float speed;
        public float angularSpeed;
        public float stoppingDistance;
        public bool alignUpToTarget;
        public bool matchTargetRotation;
        public bool paused;




        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            if (!paused && target)
            {
                Vector3 up = alignUpToTarget ? target.up : Vector3.up;
                if (Vector3.Distance(transform.position, target.position) > stoppingDistance * GetReferenceScale())
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
                }

                if (matchTargetRotation)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target.rotation, angularSpeed * Time.deltaTime);
                    return;
                }

                Vector3 lookDirection = alignUpToTarget
                    ? Vector3.ProjectOnPlane(target.position - transform.position, up)
                    : new Vector3(target.position.x, transform.position.y, target.position.z) - transform.position;

                // NavTarget과 가까워져도 벽/천장 전환용 up 방향은 계속 따라가야 한다.
                if (alignUpToTarget && lookDirection.sqrMagnitude <= 0.0001f)
                {
                    lookDirection = Vector3.ProjectOnPlane(transform.forward, up);
                }

                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDirection, up), angularSpeed * Time.deltaTime);
                }
            }

        }

        private float GetReferenceScale()
        {
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            // The stopping distance is authored for scale 1 procedural prefabs.
            return Mathf.Max(largestAxis, 0.0001f);
        }
    }
}
