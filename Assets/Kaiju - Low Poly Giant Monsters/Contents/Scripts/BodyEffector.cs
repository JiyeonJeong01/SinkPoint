//Sets the Y position of a transform based off of several other transforms and a sine wave.




using UnityEngine;

namespace DistantLands
{
    public class BodyEffector : MonoBehaviour
    {


        public Transform[] effectors;

        public float offset;
        public float sinDepth = 0;
        public float sinWidth = 15;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {


            float i = 0;
            float scale = GetReferenceScale();


            foreach (Transform j in effectors)
                i += j.position.y;

            i /= effectors.Length;
            i += offset * scale + (Mathf.Sin(Time.time * 90 / sinWidth) * sinDepth * scale);


            transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, i, Time.deltaTime), transform.position.z);





        }

        private float GetReferenceScale()
        {
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            // Body offsets are authored for scale 1 procedural prefabs.
            return Mathf.Max(largestAxis, 0.0001f);
        }
    }
}
