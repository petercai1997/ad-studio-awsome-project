using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UnityStandardAssets.Characters.FirstPerson
{
    public class PlayerInteraction : MonoBehaviour {

        private readonly int pickUpFOV = 60;
        //[SerializeField] AudioClip takeItem;
        //[SerializeField] AudioClip teleport;
        private Transform target;
        private bool holding;

        private Collider itemChecking;
        private Shader shader1;
        private Shader shader2;
        private Renderer[] rend = { };
        private AudioSource putin;
        private AudioSource findpickup;
        private AudioSource locked;
        private bool soundplayed;
        private bool alreadyfind;
        private bool filled;
        [HideInInspector] public bool islooking;
        [HideInInspector] public bool photo_changed;
        [HideInInspector] public bool photo_pickedup;

        [HideInInspector] public List<GameObject> inventory;
        private bool inSight = false;

        private bool checkingMirror=false;
        private Collider mirror;

        private bool checkingInventory = false;
        private bool checkingSafe;
        public GameObject book;
        public Image inspector;
        [SerializeField] public Canvas inventoryUI;
        private Animator inventoryAnim;
        //[SerializeField] public Canvas inspectorUI;
        private Animator inspectorAnim;
        [SerializeField] public Sprite UIMask;

        [SerializeField] public Text prompt;
        public GameObject lockUI;
        //LayoutGroup inventoryUI;
        [HideInInspector] public int cd;
        private int cd_sound;
        private int wait;
        
        private int playerLayerMask= 1<<9;
        public Lv1Progress lv1_p;
        private void Start()
        {
            Debug.Log(LayerMask.GetMask("Room"));
            holding = false;
            soundplayed = false;
            alreadyfind = false;
            filled = false;
            photo_changed = false;
            islooking = false;
            photo_pickedup = false;
            cd = 30;
            cd_sound = 180;
            wait = 180;
            target = Camera.main.transform;
            shader1 = Shader.Find("Standard (Roughness setup)");
            shader2 = Shader.Find("Shader_highlight/0.TheFirstShader");
            inventory = new List<GameObject>(5);
            for (int i = 0; i < 5; i++) 
                inventory.Add(new GameObject());

            inventoryAnim = inventoryUI.GetComponent<Animator>();
            //inspectorAnim = inspectorUI.GetComponent<Animator>();

            AudioSource[] audios = GetComponents<AudioSource>();
            putin = audios[2];
            findpickup = audios[1];
            locked = audios[3];

            //if (SceneManager.GetActiveScene().name.Equals("FirstLevel"))
            //{
            //    lv1_p = GetComponent<Lv1Progress>();
            //}
            book.SetActive(false);
        }

      
        private void LateUpdate()
        {
            if(cd<30)
                cd++;
            if (cd_sound < 180)
                cd_sound++;
            if (wait < 180)
                wait++;
            if (checkingSafe)
            {
                if (Input.GetKeyDown(KeyCode.E) && cd > 20)
                {
                    checkingSafe = false;
                    lockUI.SetActive(false);
                    PlayerEnable(true);
                }
                if (lockUI.GetComponent<Lock>().unlocked)
                {
                    checkingSafe = false;
                    lockUI.SetActive(false);
                    PlayerEnable(true);
                    itemChecking.GetComponent<Collider>().enabled = false;
                    itemChecking.GetComponent<Animator>().SetBool("lock", true);
                }
            }
            else if (!checkingMirror)
            {
                if (!islooking)
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        ToggleInventory();
                    }
                   
                }
            }
            else
            {
                if (lv1_p.note2Found)
                    prompt.text = "Press A/D to change angle. Press E to quit.";
                else
                    prompt.text = "Press E to quit.";
                target.transform.LookAt(mirror.gameObject.transform);
                Vector3 direction = transform.position- mirror.transform.position;
                float angle = Vector3.SignedAngle(new Vector3(direction.x, 0, direction.z), mirror.transform.up, Vector3.up);
                if (mirror.GetComponent<MirrorTrigger>().reveal(angle))
                {
                    StartCoroutine(MirrorDone(mirror,1));
                }
                if (Input.GetKey(KeyCode.D)&& angle<35)
                {
                    GetComponent<Rigidbody>().AddForce(transform.right * 1f, ForceMode.Impulse);
                }
                if (Input.GetKey(KeyCode.A) && angle >- 35)
                {
                    GetComponent<Rigidbody>().AddForce(transform.right * -1f, ForceMode.Impulse);
                }

                if (cd >= 30 && (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.E)))
                {
                    cd = 0;
                    prompt.text = "";
                    checkingMirror = false;
                    mirror.GetComponent<MirrorReflection>().m_TextureSize = 32;
                    GetComponent<RigidbodyFirstPersonController>().enableInput = true;

                }
    

            }

            if (!inSight)
                itemChecking = null;

            //if (Input.GetKeyUp(KeyCode.O))
            //{
            //    int i = 0;
            //    foreach (LayoutGroup lp in inventoryUI.GetComponentsInChildren<LayoutGroup>())
            //    {
            //        if (lp.tag != "ItemUI")
            //            continue;
            //        Toggle checker = lp.GetComponentInChildren<Toggle>();
            //        if (checker.isOn)
            //        {
            //            lp.gameObject.name = "";
            //            lp.GetComponentsInChildren<Image>()[1].sprite = UIMask;
            //            checker.isOn = false;

            //            GameObject item = inventory[i];
            //            inventory[i] = null;
            //            item.transform.position = transform.position + transform.forward * 3;
            //            item.SetActive(true);
            //            break;

            //        }
            //        i++;
            //    }
            //}
        }

        void OnTriggerEnter(Collider col)
        {

        }

        void OnTriggerStay(Collider col)
        {
            if (col.tag=="Note"|| col.tag.Contains("Pickupable"))
            {
                Debug.Log(col.name + " "+ itemChecking + " " + checkingInventory + " " + checkingSafe);
            }
            if (checkingInventory|| checkingSafe)
                return;
            inSight = false;
            
            if (itemChecking && col != itemChecking) return;

            if (col.gameObject.tag.Equals("Mirror"))
            {
                mirror=itemChecking = col;
                if (!checkingMirror)
                {
                    Vector3 direction = col.transform.position - target.transform.position;
                    float angle = Vector3.Angle(direction, target.transform.forward);
                    prompt.text="";
                    if (angle <= pickUpFOV * 0.8f)
                    {
                        RaycastHit hit;
                        Debug.DrawRay(col.transform.position , col.transform.up, Color.red);
                        if (Physics.Raycast(col.transform.position, col.transform.up, out hit, 1))
                        {
                            //Debug.Log(hit.collider.name);
                            if (hit.collider.tag.Contains("Player"))
                            {
                                inSight = true;
                                if(lv1_p.note2Found)
                                    prompt.text = "Press E to inspect.";
                                if (cd >= 30 && (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.E)))
                                {
                                    cd = 0;
                                    itemChecking = col;
                                    checkingMirror = true;
                                    Vector3 tmp = col.gameObject.transform.position + col.gameObject.transform.up * 1.3f;
                                    transform.position = ( new Vector3(tmp.x,transform.position.y,tmp.z));
                                    target.transform.LookAt(col.gameObject.transform);
                                    GetComponent<RigidbodyFirstPersonController>().enableInput = false;
                                    if (lv1_p.note2Found)
                                        prompt.text = "Press A/D to change angle. Press E to quit.";
                                    else
                                        prompt.text = "Press E to quit.";
                                    return;

                                }
                            }
                        }
                    }
                    else
                    {
                        prompt.text="";
                    }
                }
                
            }else if (col.gameObject.tag.Equals("Interactable")|| col.gameObject.tag.Equals("Safe") )
            {
                Vector3 direction = col.transform.position.normalized - target.transform.position.normalized;
                float angle = Vector3.Angle(direction, target.transform.forward);
                // Debug.Log(angle);

                itemChecking = col;
                
                RaycastHit hit;
                Debug.DrawRay(target.transform.position, target.transform.forward*5, Color.blue);

                if (Physics.Raycast(target.transform.position, target.transform.forward, out hit, 5))
                {
                    Debug.Log(hit.collider.name);
                    if (hit.collider.Equals(col)){
                        if (itemChecking.name.Equals("Sink") && filled)
                        {
                            inSight = true;
                            
                            rend = itemChecking.GetComponentsInChildren<Renderer>();
                            foreach (Renderer r in rend)
                            {
                                r.material.shader = shader1;
                            }   
                            if (wait >= 180)
                            {
                                lv1_p.filled_water.GetComponent<Renderer>().material.shader = shader2;
                                lv1_p.changephoto = true;
                            }    
                        }
                        else
                        {
                            lv1_p.changephoto = false;
                            inSight = true;
                            rend = col.GetComponentsInChildren<Renderer>();
                            foreach (Renderer r in rend)
                            {
                                r.material.shader = shader2;
                            }
                            if (cd >= 30 && (Input.GetMouseButtonUp(0) || Input.GetKeyDown(KeyCode.E)))
                            {
                                cd = 0;
                                if (col.gameObject.tag.Equals("Safe"))
                                {
                                    lockUI.SetActive(true);
                                    PlayerEnable(false);
                                    checkingSafe = true;
                                }
                                else
                                {

                                    Interactable i = col.gameObject.GetComponent<Interactable>();
                                    if (!i.requirement)
                                    {
                                        i.Interact();
                                        if (itemChecking.gameObject.name.Equals("Chest"))
                                        {
                                            itemChecking = null;
                                        }

                                    }
                                    else if (inventory.Contains(i.requirement))
                                    {
                                        if (col.gameObject.name.Equals("Sink"))
                                        {
                                            Debug.Log("filled");
                                            wait = 0;
                                            filled = true;
                                        }
                                        if (col.gameObject.name.Equals("Fireplace")) {                                                
                                            lv1_p.safeFound = true;
                                        }
                                        if (col.gameObject.name.Equals("MainDoor"))
                                        {
                                            lv1_p.finished = true;
                                        }

                                        DestroyObj(inventory.IndexOf(i.requirement));
                                        i.Interact();
                                        i.requirement = null;
                                        itemChecking = null;
                                    }
                                    else
                                    {

                                        StartCoroutine(ShowPrompt(i.promptForRequirement, 2));
                                        if(i.requirement.gameObject.name.Equals("key") || i.requirement.gameObject.name.Equals("Main Key"))
                                        {
                                            locked.Play();
                                        }
                                       

                                    }
                                }
                            }
                        }
                        
                    }
                }
            
            }
            else if (col.gameObject.tag.Contains("Pickupable")|| col.gameObject.tag.Equals("Note"))
            {
                Vector3 direction = col.GetComponent<Renderer>().bounds.center - target.transform.position;
                float angle = Vector3.Angle(direction, target.transform.forward);
                itemChecking = col;
                //Debug.Log(angle);

                if (angle <= pickUpFOV * 0.5f)
                {

                    RaycastHit hit;
                    Debug.DrawRay(target.transform.position, direction.normalized*5, Color.red);
                    if (Physics.Raycast(target.transform.position, direction.normalized , out hit,playerLayerMask, 5))
                    {
                        //Debug.Log(hit.collider.name);
                        if (hit.collider.Equals(col)){
                            inSight = true;
                            rend = col.GetComponentsInChildren<Renderer>();
                            if (!soundplayed && cd_sound == 180 && !alreadyfind)
                            {
                                //findpickup.Play();
                                cd_sound = 0;
                                soundplayed = true;
                            }
                            alreadyfind = true;
                            foreach (Renderer r in rend)
                            {
                                r.material.shader = shader2;
                            }
                            if (cd >= 30 && (Input.GetMouseButtonUp(0) || Input.GetKeyDown(KeyCode.E)))
                            {
                                cd = 0;
                                if (col.gameObject.tag.Contains("Pickupable"))
                                {
                                    int i = 0;
                                    foreach (Image img in inventoryUI.GetComponentsInChildren<Image>())
                                    {
                                        if (img.tag != "ItemUI")
                                            continue;
                                        ItemUI item = img.GetComponentInChildren<ItemUI>();
                                        if (!item.isOn)
                                        {
                                            item.objName = col.gameObject.name;
                                            item.isOn = true;
                                            item.item = col.GetComponent<Item>();
                                            item.GetComponent<Image>().sprite = item.item.smallSprite;
                                            inventory[i] = col.gameObject;

                                            col.gameObject.SetActive(false);
                                            inSight = false;
                                            StartCoroutine(ShowPrompt(item.objName + " found", 2));
                                            
                                            if (item.objName == "Diary")
                                            {
                                                lv1_p.diaryFound = true;
                                            }


                                            if (col.gameObject.transform.parent.gameObject.name.Equals("photo 1"))
                                            {
                                                photo_pickedup = true;
                                                col.gameObject.transform.parent.gameObject.SetActive(false);
                                            }

                                            itemChecking = null;
                                            //putin.Play();
                                            StartCoroutine(PopInventory());
                                            break;

                                        }
                                        i++;
                                    }
                                }
                                else if (col.gameObject.tag.Equals("Note"))
                                {
                                    if (!lv1_p.diaryFound)
                                    {
                                        StartCoroutine(ShowPrompt("A paper scrap, I should find something to hold it."));
                                    }
                                    else
                                    {
                                        if (col.gameObject.name == "Note2")
                                            lv1_p.note2Found = true;
                                        else if (col.gameObject.name == "Note3")
                                            lv1_p.note3Found = true;
                                        else if (col.gameObject.name == "Newspaper")
                                            lv1_p.newspaperFound = true;
                                        else if (col.gameObject.name == "Painting")
                                            lv1_p.paintFound = true;
                                        col.gameObject.SetActive(false);
                                        StartCoroutine(ShowPrompt("A paper scrap, added it to the diary."));

                                    }

                                }
                                //else if (col.gameObject.name.Equals("Photo_changed"))
                                //{
                                //    if (!lv1_p.diaryFound)
                                //    {
                                //        StartCoroutine(ShowPrompt("A photo, I should find something to hold it."));
                                //    }
                                //    else
                                //    {
                                //        photo_pickedup = true;
                                //        lv1_p.photoWashed = true;
                                //        col.gameObject.transform.parent.gameObject.SetActive(false);
                                //        StartCoroutine(ShowPrompt("A paper scrap, added it to the diary."));

                                //    }

                                //}
                            }
                        }
                    }
                }



            }
            if (!inSight)
            {
                //lv1_p.filled_water.GetComponent<Renderer>().material.shader = shader1;
                foreach (Renderer r in rend)
                {
                    r.material.shader = shader1;
                }
                soundplayed = false;
                alreadyfind = false;
                if (col.gameObject.tag.Contains("Pickupable") || col.gameObject.tag.Equals("Interactable") || col.gameObject.tag.Equals("Safe"))
                {
                    
                    itemChecking = null;
                }
                if (col.gameObject.tag.Equals("Mirror"))
                {
                    prompt.text="";
                }
            }
        }


        void OnTriggerExit(Collider col)
        {
            lv1_p.filled_water.GetComponent<Renderer>().material.shader = shader1;
            foreach (Renderer r in rend)
            {
                r.material.shader = shader1;
            }
            soundplayed = false;
            alreadyfind = false;
            if (col.gameObject.tag.Contains("Pickupable") || col.gameObject.tag.Equals("Interactable")|| col.gameObject.tag.Equals("Safe"))
            {    

                inSight = false;
                itemChecking = null;
            }
            if (col.gameObject.tag.Equals("Mirror"))
            {
                prompt.text = "";
                inSight = false;
                itemChecking = null;
            }
        }
        //private void HoldItem(Collider col)
        //{
        //    Vector3 offset = target.transform.forward;
        //    //offsetScale = (float)Mathf.Cos(Vector3.Angle(target.transform.forward, transform.forward) * Mathf.PI / 180);
        //    col.transform.position = target.transform.position + offset * 0.8f;
        //    //col.GetComponent<Rigidbody>().MovePosition(target.transform.position + offset);
        //    //col.transform.LookAt(target.transform.position+ target.transform.up*(col.transform.position.y- target.transform.position.y));
        //    col.transform.LookAt(target.transform.position);
        //}

        public void PlayerEnable(bool enable)
        {
            Cursor.visible = !enable;
            if (enable)
            {
                Cursor.lockState = CursorLockMode.Locked;
                GetComponent<RigidbodyFirstPersonController>().enableInput = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                GetComponent<RigidbodyFirstPersonController>().enableInput = false;
            }
            

        }

        public void GetItOut(int index)
        {

            if (!inventory[index])
                return;
            int i = 0;
            foreach (Image img in inventoryUI.GetComponentsInChildren<Image>())
            {
                if (img.tag != "ItemUI")
                    continue;
                if (i != index)
                {
                    i++;
                    continue;
                }
                ItemUI item = img.GetComponent<ItemUI>();
                if (item.isOn)
                {
                    item.objName = "";
                    img.sprite = UIMask;
                    item.isOn = false;

                    GameObject it = inventory[i];
                    inventory[i] = null;
                    it.transform.position = transform.position + transform.forward * 3;
                    it.SetActive(true);
                    break;

                }
            }
        }
        public void DestroyObj(int index)
        {
            if (!inventory[index])
                return;
            int i = 0;
            foreach (Image img in inventoryUI.GetComponentsInChildren<Image>())
            {
                if (img.tag != "ItemUI")
                    continue;
                if (i != index)
                {
                    i++;
                    continue;
                }
                ItemUI itemUI = img.GetComponent<ItemUI>();
                if (itemUI.isOn)
                {
                    itemUI.objName = "";
                    img.sprite = UIMask;
                    itemUI.isOn = false;
                    itemUI.item = null;

                    inventory[i] = null;
                    break;

                }
            }
        }

        public void ToggleInventory()
        {
            checkingInventory = !checkingInventory;
            if (checkingInventory)
            {
                PlayerEnable(false);
                inventoryAnim.SetBool("open", true);
                //inspectorAnim.SetBool("open", true);

            }
            else
            {
                if (book) book.SetActive(false);
                inspector.enabled = false;
                PlayerEnable(true);
                inventoryAnim.SetBool("open", false);
                //inspectorAnim.SetBool("open", false);

            }
            //yield return new WaitForSeconds(1);
        }

        private IEnumerator MirrorDone(Collider mirror, float sec)
        {
            mirror.tag = "Untagged";
            prompt.text = "";
            checkingMirror = false;
            target.transform.LookAt(mirror.gameObject.transform);
            StartCoroutine(ShowPrompt("Something happened...", sec));
            yield return new WaitForSeconds(sec);
            GetComponent<RigidbodyFirstPersonController>().enableInput = true;
            itemChecking = null;

        }

        private IEnumerator ShowPrompt(string text, float sec=2)
        {
            prompt.text = text;
            yield return new WaitForSeconds(sec);
            prompt.text = "";
        }

        private IEnumerator PopInventory( float sec = 2)
        {
            inventoryAnim.SetBool("pop", true);
            yield return new WaitForSeconds(sec);
            inventoryAnim.SetBool("pop", false);
        }


        private IEnumerator Delay(float sec)
        {
            yield return new WaitForSeconds(sec);
             
        }

    }
}

 