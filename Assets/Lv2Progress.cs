﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lv2Progress : MonoBehaviour {
    public GameObject oldBunny;
    public GameObject[] bunnys;
    public Filter filter;
    public void BunnyHop()
    {
        StartCoroutine(SwitchBunnys());
    }

    IEnumerator SwitchBunnys()
    {
        foreach (GameObject b in bunnys)
        {
            filter.blink();
            yield return new WaitForSeconds(0.2f);
            oldBunny.SetActive(false);
            b.SetActive(true);
            oldBunny = b;
            yield return new WaitForSeconds(0.5f);
        }
        
    }
}
