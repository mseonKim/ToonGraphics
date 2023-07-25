using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToonGraphics
{
    public class EyeVectorProvider : MonoBehaviour
    {
        public Transform eyeFront;
        public Transform eyeCenter;
        public Transform eyeUp;
        public Material eyeMaterial;
        private string forwardVectorString = "_EyeForward";
        private string upVectorString = "_EyeUp";

        void OnValidate()
        {
            if (eyeFront != null && eyeCenter != null)
            {
                Vector3 dir = eyeFront.position - eyeCenter.position;
                dir = dir.normalized;
                eyeMaterial.SetVector(forwardVectorString, new Vector4(dir.x, dir.y, dir.z, 1));
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (eyeFront != null && eyeCenter != null)
            {
                Vector3 dir = eyeFront.position - eyeCenter.position;
                dir = dir.normalized;
                eyeMaterial.SetVector(forwardVectorString, new Vector4(dir.x, dir.y, dir.z, 1));
            }

            if (eyeUp != null && eyeCenter != null)
            {
                Vector3 dir = eyeUp.position - eyeCenter.position;
                dir = dir.normalized;
                eyeMaterial.SetVector(upVectorString, new Vector4(dir.x, dir.y, dir.z, 1));
            }
        }
    }
}
