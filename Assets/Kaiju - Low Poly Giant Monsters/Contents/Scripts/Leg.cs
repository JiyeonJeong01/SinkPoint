//Simple script for moving the legs of creatures.




using UnityEngine;


namespace DistantLands
{
    public class Leg : MonoBehaviour
    {


        [HideInInspector]
        public LegIK IKSolver;
        public float maxDistance;
        public float maxRayDistance;
        public float groundOffset;
        public float offsetByVelocity;
        public Transform nextFootTarget;
        [HideInInspector]
        public Transform currentTarget;
        private Vector3 point;
        public Transform root;
        public LayerMask layerMask;
        public float speed;
        public float groundSnap = 0.75f;
        public bool grounded;
        public Leg oppositeLeg;
        public Leg[] totalLegs;
        public int minLegsGrounded;
        public float failDistance;
        public float legLift;
        public float distanceToLiftLeg;
        public bool paused;


        // Start is called before the first frame update
        void Start()
        {
            IKSolver = GetComponent<LegIK>();
            IKSolver.elbow.parent = root.transform;
            IKSolver.target.parent = null;
            currentTarget = new GameObject(gameObject.name + " Target").transform;
            float scale = GetReferenceScale();

            RaycastHit hit;
            if (Physics.Raycast(nextFootTarget.position, -root.up, out hit, maxRayDistance * scale, layerMask))
            {


                point = hit.point + (root.up * groundOffset * scale);

                IKSolver.target.position = point;
                IKSolver.target.LookAt(hit.point, root.up);

                currentTarget.position = point;
                currentTarget.LookAt(hit.point, root.up);

            }

            if (Physics.Raycast(nextFootTarget.position + root.GetComponent<GetVelocityFromTransform>().velocity * offsetByVelocity, -root.up, out hit, maxRayDistance * scale, layerMask))
            {


                point = hit.point + (root.up * groundOffset * scale);




                if (Vector3.Distance(IKSolver.hand.position, hit.point + root.up * groundOffset * scale) > maxDistance * scale)
                {


                    currentTarget.position = point;
                    currentTarget.LookAt(hit.point, root.up);


                }
            }


        }

        // Update is called once per frame
        public void UpdateLeg()
        {
            float scale = GetReferenceScale();


            if (!paused)
            {
                if (CheckLegsGrounded() > minLegsGrounded)
                {

                    RaycastHit hit;
                    if (Physics.Raycast(nextFootTarget.position + root.GetComponent<GetVelocityFromTransform>().velocity * offsetByVelocity, -root.up, out hit, maxRayDistance * scale, layerMask))
                    {


                        point = hit.point + (root.up * groundOffset * scale);




                        if (Vector3.Distance(IKSolver.hand.position, hit.point + root.up * groundOffset * scale) > maxDistance * scale)
                        {


                            currentTarget.position = point;
                            currentTarget.LookAt(hit.point, root.up);


                        }
                    }
                }

                if (Vector3.Distance(currentTarget.position, IKSolver.target.position) > failDistance * scale) {

                    RaycastHit hit;
                    if (Physics.Raycast(nextFootTarget.position + root.GetComponent<GetVelocityFromTransform>().velocity * offsetByVelocity, -root.up, out hit, maxRayDistance * scale, layerMask))
                    {


                        point = hit.point + (root.up * groundOffset * scale);




                        if (Vector3.Distance(IKSolver.hand.position, hit.point + root.up * groundOffset * scale) > maxDistance * scale)
                        {


                            currentTarget.position = point;
                            currentTarget.LookAt(hit.point, root.up);


                        }
                    }
                }

                grounded = Vector3.Distance(IKSolver.target.position, currentTarget.position) < groundSnap * scale;

            }
            else
                grounded = false;


            if (Vector3.Distance(IKSolver.target.position, currentTarget.position) > distanceToLiftLeg * scale)
                IKSolver.target.position = Vector3.MoveTowards(IKSolver.target.position, currentTarget.position + root.up * legLift * scale, speed * Time.deltaTime);
            else
                IKSolver.target.position = Vector3.MoveTowards(IKSolver.target.position, currentTarget.position, speed * Time.deltaTime);



            IKSolver.target.rotation = Quaternion.RotateTowards(IKSolver.target.rotation, currentTarget.rotation, Time.deltaTime * speed * 4);




        }

        private float GetReferenceScale()
        {
            Transform reference = root != null ? root : transform;
            Vector3 scale = reference.lossyScale;
            float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            // These leg distances are authored for scale 1. Keep them proportional when the kaiju root is scaled.
            return Mathf.Max(largestAxis, 0.0001f);
        }


        public int CheckLegsGrounded()
        {

            int grounded = 0;


            foreach (Leg i in totalLegs)
                if (i.grounded)
                    grounded++;



            return grounded;

        }


        private void OnDrawGizmos()
        {


            if (currentTarget)
            {
                Gizmos.color = Color.blue;

                Gizmos.DrawRay(nextFootTarget.position, -root.up);
                Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(IKSolver.target.position, 0.5f);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(point, 0.5f);

            }


        }




    }
}
