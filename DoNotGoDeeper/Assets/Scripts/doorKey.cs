using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pickkey : MonoBehaviour
{
  public Component doorcolliderhere;
  public GameObject KeyGone;

void OnTriggerStay() {
if(Input.GetKey(KeyCode.E))

doorcolliderhere.GetComponent<BoxCollider>().enabled = true;

if(Input.GetKey(KeyCode.E))
KeyGone.SetActive(false);

}

}