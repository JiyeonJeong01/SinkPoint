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
        public bool paused;




        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            if (!paused && target)
                if (Vector3.Distance(transform.position, target.position) > stoppingDistance * GetReferenceScale())
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(new Vector3(target.position.x, transform.position.y, target.position.z) - transform.position, Vector3.up), angularSpeed * Time.deltaTime);
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
