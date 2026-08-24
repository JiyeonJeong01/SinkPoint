//The main script that controls when legs will update to ensure that legs are updated evenly.




using System.Collections.Generic;
using UnityEngine;


namespace DistantLands
{
    public class LegController : MonoBehaviour
    {


        public Leg[] legs;
        [HideInInspector]
        public List<Leg> legUpdateImportance;
        [Tooltip("끄면 예전처럼 모든 다리가 매 프레임 새 발 위치 Raycast를 수행합니다. 프레임 비교용으로 사용합니다.")]
        public bool limitGroundProbes = true;
        [Tooltip("한 프레임에 새 발 위치 Raycast를 수행할 최대 다리 수입니다. 낮을수록 가볍고, 너무 낮으면 발 반응이 늦어집니다.")]
        public int maxGroundProbeLegsPerFrame = 4;

        private int nextProbeIndex;



        // Start is called before the first frame update
        void Start()
        {

            if (legUpdateImportance == null)
                legUpdateImportance = new List<Leg>();

            legUpdateImportance.AddRange(legs);

        }

        // Update is called once per frame
        void Update()
        {

            List<Leg> usedLegs = new List<Leg>();
            int groundedCount = CountGroundedLegs();
            int probeBudget = limitGroundProbes
                ? Mathf.Clamp(maxGroundProbeLegsPerFrame, 1, Mathf.Max(1, legUpdateImportance.Count))
                : legUpdateImportance.Count;
            int probeStartIndex = nextProbeIndex;

            for (int index = 0; index < legUpdateImportance.Count; index++)
            {
                Leg i = legUpdateImportance[index];
                bool allowGroundProbe = IsProbeIndex(index, probeStartIndex, probeBudget, legUpdateImportance.Count);

                i.UpdateLeg(allowGroundProbe, groundedCount);

                if (!i.grounded)
                    usedLegs.Add(i);

            }

            foreach (Leg i in usedLegs)
                legUpdateImportance.Remove(i);

            if (legUpdateImportance.Count > 0)
                nextProbeIndex = (probeStartIndex + probeBudget) % legUpdateImportance.Count;

        }

        private void LateUpdate()
        {

            if (legUpdateImportance == null)
                legUpdateImportance = new List<Leg>();

            foreach (Leg i in legs)
            {

                if (!legUpdateImportance.Contains(i))
                    legUpdateImportance.Add(i);


            }
        }

        private int CountGroundedLegs()
        {
            int groundedCount = 0;
            foreach (Leg i in legs)
                if (i != null && i.grounded)
                    groundedCount++;

            return groundedCount;
        }

        private bool IsProbeIndex(int index, int startIndex, int count, int length)
        {
            if (length <= 0)
                return false;

            for (int offset = 0; offset < count; offset++)
                if (index == (startIndex + offset) % length)
                    return true;

            return false;
        }

    }
}
