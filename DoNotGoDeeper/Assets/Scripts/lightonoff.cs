using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lightonoff : MonoBehaviour
{
    public GameObject txtToDisplay;
    public GameObject lightorobj;
    public float detectionRange = 20f;

    private bool PlayerInZone;
    private Transform player;

    private void Start()
    {
        PlayerInZone = false;
        txtToDisplay.SetActive(false);
        player = GameObject.FindWithTag("Player").transform;
        
        if (player == null)
            Debug.Log("ERROR: No object tagged Player found!");
        else
            Debug.Log("Player found: " + player.name);
            
        if (lightorobj == null)
            Debug.Log("ERROR: lightorobj not assigned in Inspector!");
            
        if (txtToDisplay == null)
            Debug.Log("ERROR: txtToDisplay not assigned in Inspector!");
    }

    private void Update()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        //Debug.Log("Distance to player: " + distance);

        if (distance < detectionRange)
        {
            if (!PlayerInZone)
            {
                txtToDisplay.SetActive(true);
                PlayerInZone = true;
                Debug.Log("Player entered zone!");
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("F pressed, toggling light!");
                Light lightComponent = lightorobj.GetComponent<Light>();
                lightComponent.enabled = !lightComponent.enabled;            }
        }
        else
        {
            if (PlayerInZone)
            {
                txtToDisplay.SetActive(false);
                PlayerInZone = false;
                Debug.Log("Player left zone!");
            }
        }
    }
}