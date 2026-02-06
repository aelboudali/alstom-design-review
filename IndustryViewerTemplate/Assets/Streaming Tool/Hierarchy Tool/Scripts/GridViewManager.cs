using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Unity.Industry.Viewer.Streaming.Hierarchy
{
    public class GridViewManager : MonoBehaviour
    {
        private const string GridSizeName = "Grid-Size";
        
        [SerializeField] protected float m_fGridUnit;
        private Material m_Material;
        private readonly int m_GridSizeId = Shader.PropertyToID("_GridSize");
        
        private ARSession m_ARSession;
        private MeshRenderer m_MeshRenderer;
        private bool m_IsGridVisible => m_MeshRenderer != null && m_MeshRenderer.enabled;

        void Start()
        {
            if (!PlayerPrefs.HasKey(GridSizeName))
            {
                m_fGridUnit = 0.1f;
                PlayerPrefs.SetFloat(GridSizeName, m_fGridUnit);
            }
            else
            {
                m_fGridUnit  = PlayerPrefs.GetFloat(GridSizeName, 0.1f);
            }
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_ARSession = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
            SetGridUnit(m_fGridUnit);
            GridModelVisibility(true);
        }

        private void Update()
        {
            if (m_ARSession != null)
            {
                switch (m_ARSession.enabled && m_ARSession.gameObject.activeInHierarchy)
                {
                    case true when m_IsGridVisible:
                        GridModelVisibility(false);
                        break;
                    case false when !m_IsGridVisible:
                        GridModelVisibility(true);
                        break;
                }
            }
            if(TransformController.Instance == null) return;
            transform.position = new Vector3(TransformController.Instance.transform.position.x, TransformController.Instance.transform.position.y + 0.001f, TransformController.Instance.transform.position.z);
        }

        public void SetGridUnit(float gridSize)
        {
            m_fGridUnit = Mathf.Clamp(gridSize, 0.001f, 1000f);
            PlayerPrefs.SetFloat(GridSizeName, m_fGridUnit);
            m_Material ??= GetComponent<MeshRenderer>().sharedMaterial;
            //m_Material.SetFloat(m_GridSizeId, gridSize * 0.01f);  // to mm (render in 10cm unit)
            m_Material.SetFloat(m_GridSizeId, m_fGridUnit * 10.0f);    // to M (render in 10cm unit)
        }

        public float GetGridUnit()
        {
            return m_fGridUnit;
        }

        private void GridModelVisibility(bool visible)
        {
            m_MeshRenderer.enabled = visible;
        }
    }
}