using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectParticleMgr : MonoBehaviour
{
    public GameObject m_norparticle;
    public GameObject m_cashparticle;

    public void showParticle(ImageEnum imageEnum)
    {
        if (imageEnum == ImageEnum.IMG0)
        {
            m_cashparticle.SetActive(true);
        }
        else
        {
            m_norparticle.SetActive(true);
        }
    }
    
    public void closeParticle()
    {
        m_norparticle.SetActive(false);
        m_cashparticle.SetActive(false);
    }
}
