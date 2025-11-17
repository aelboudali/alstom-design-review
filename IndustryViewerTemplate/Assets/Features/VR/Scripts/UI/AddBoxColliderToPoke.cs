using System;
using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace Unity.Industry.Viewer.VR
{
    [RequireComponent(typeof(XRPokeFilter))]
    public class AddBoxColliderToPoke : MonoBehaviour
    {
        private XRPokeFilter m_PokeFilter;

        private bool pass = false;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        IEnumerator Start()
        {
            m_PokeFilter = GetComponent<XRPokeFilter>();
            if (m_PokeFilter == null) yield break;
            yield return null;
            var boxCollider = GetComponent<BoxCollider>();
            while (boxCollider == null)
            {
                yield return null;
                boxCollider = GetComponent<BoxCollider>();
            }
            if(boxCollider == null) yield break;
            yield return null;
            m_PokeFilter.pokeCollider = boxCollider;
            while(m_PokeFilter.pokeCollider == null)
            {
                yield return null;
                m_PokeFilter.pokeCollider = boxCollider;
            }

            if (m_PokeFilter.pokeCollider == null)
            {
                yield break;
            }
            m_PokeFilter.enabled = true;
            pass = true;
        }

        private void Update()
        {
            if (!pass || m_PokeFilter.pokeCollider != null) return;
            var boxCollider = GetComponent<BoxCollider>();
            if(boxCollider != null)
            {
                m_PokeFilter.pokeCollider = boxCollider;
            }
        }
    }
}
