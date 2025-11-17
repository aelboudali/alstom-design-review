using System;
using UnityEngine;

namespace Unity.Industry.Viewer.Shared
{
    public class Billboard: MonoBehaviour
    {
        [SerializeField]
        Vector3 m_RotationOffset;

        void LateUpdate()
        {
            if(Camera.main == null)
                return;
            var rotation = Camera.main.transform.rotation * Quaternion.Euler(m_RotationOffset);
            transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
        }
    }
}
