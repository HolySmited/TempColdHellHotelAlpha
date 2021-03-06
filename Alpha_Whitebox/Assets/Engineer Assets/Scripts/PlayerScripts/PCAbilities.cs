﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HighlightingSystem;

public class PCAbilities : MonoBehaviour
{
    [SerializeField]
    bool objInRange = false, interacting = false;
    [SerializeField]
    float cdTimer_Blast = 0, cdTimer_Interact = 0;

    
    public GameObject heldObj = null, lastHit = null;
    Transform heldObjParent = null;
    float heldDist = 0;

    [SerializeField]
    Texture enrgyMtrTex, enrgyMtrBckgrndTex;

    void Awake()
    {
        initializeAbilityEnergy();
    }

    void FixedUpdate()
    {
        updateInteractions();
        updateCDTimers();

        energyDebug();
    }

    void updateInteractions()
    {
        if (!interacting && cdTimer_Interact <= 0)
            objInRange = checkForObjInRange();
        if (heldObj != null)
            manageHeldObject();
    }
    void updateCDTimers()
    {
        if ((cdTimer_Blast -= Time.deltaTime) < 0) cdTimer_Blast = 0;
        if ((cdTimer_Interact -= Time.deltaTime) < 0) cdTimer_Interact = 0;
    }

    //  Check if there is an Interactable Object Immediately Before the Player
    bool checkForObjInRange()
    {
        //  Check for an Object within Range
        //  Utilizes a Raycast
        Vector3 rayOrigin = transform.position;
        Vector3 rayDir = transform.forward;
        float rayDist = PCSettings.pcSettings.interactReach;

        RaycastHit hitInfo;
        if (Physics.Raycast(rayOrigin, rayDir, out hitInfo, rayDist, PCSettings.pcSettings.interactableObjectsLM))
        {
            //  Cache HighLighter Component of Current Hit
            Highlighter highlight = hitInfo.transform.GetComponent<Highlighter>();

            //  Disable Last Hit's Highlighter if not current hit
            if (lastHit != null && lastHit.GetComponent<Highlighter>() != highlight)
                lastHit.GetComponent<Highlighter>().Off();
            //  Don't do anything if inside the object
            bool outsideObj = !hitInfo.transform.GetComponent<Collider>().bounds.Contains(GetComponent<Collider>().ClosestPointOnBounds(hitInfo.transform.position));
            if (outsideObj)
            {
                lastHit = hitInfo.transform.gameObject;
                //  Set Hit's Outline Color
                if (hitInfo.transform.GetComponent<ObjectBehavior>().trait_Severable || hitInfo.transform.GetComponent<ObjectBehavior>().trait_Sharp)
                    highlight.ConstantOn(Color.magenta);
                else if (hitInfo.transform.GetComponent<ObjectBehavior>().trait_Flammable || hitInfo.transform.GetComponent<ObjectBehavior>().trait_Flame)
                    highlight.ConstantOn(Color.red);
                else if (hitInfo.transform.GetComponent<ObjectBehavior>().trait_PowerSource)
                    highlight.ConstantOn(Color.green);
                else
                    highlight.ConstantOn(Color.yellow);
                highlight.SeeThroughOff();
            }
            else
                lastHit = null;

            return outsideObj;
        }
        //  Disable Last Hit's Highlighter
        else if (lastHit != null)
            lastHit.GetComponent<Highlighter>().Off();

        return false;
    }

    //  Interact with the Interactable Object Immediately Before the Player
    public void interactWithObject()
    {
        if (cdTimer_Interact > 0)
            return;

        Vector3 rayOrigin = transform.position, rayDir = transform.forward;
        float rayDist = PCSettings.pcSettings.interactReach;
        RaycastHit hitInfo;

        if (heldObj != null)
            if (Physics.Raycast(rayOrigin, rayDir, out hitInfo, rayDist, PCSettings.pcSettings.interactableObjectsLM))
                if (hitInfo.transform.GetComponent<ObjectBehavior>().trait_SpecialInteraction)
                    hitInfo.transform.GetComponent<InteractableObject>().specialInteract(heldObj);
                else
                    dropObject();
            else
                dropObject();
        else if (interacting || !objInRange)
            return;
        else if (Physics.Raycast(rayOrigin, rayDir, out hitInfo, rayDist, PCSettings.pcSettings.interactableObjectsLM))
        {
            ObjectBehavior ob = hitInfo.transform.GetComponent<ObjectBehavior>();
            if (ob == null) return;

            // Special Interact
            if (ob.trait_SpecialInteraction)
                hitInfo.transform.GetComponent<InteractableObject>().specialInteract(heldObj);
            //  Pickup
            else if (hitInfo.transform.GetComponent<ObjectBehavior>().trait_Holdable)
            {
                if (ob.trait_Light)
                    if (abilityEnergy.energy < PCSettings.pcSettings.pickupCost_Light) return;
                    else abilityEnergy.expendEnergy(PCSettings.pcSettings.pickupCost_Light);
                else if (ob.trait_Medium)
                    if (abilityEnergy.energy < PCSettings.pcSettings.pickupCost_Medium || abilityEnergy.levelReadable < 2) return;
                    else abilityEnergy.expendEnergy(PCSettings.pcSettings.pickupCost_Medium);
                else if (ob.trait_Heavy)
                    if (abilityEnergy.energy < PCSettings.pcSettings.pickupCost_Heavy || abilityEnergy.levelReadable < 3) return;
                    else abilityEnergy.expendEnergy(PCSettings.pcSettings.pickupCost_Heavy);
                else
                    Debug.Log("WARNING: PICKING UP OBJECT W/O WEIGHT");

                heldObj = hitInfo.transform.gameObject;
                heldObj.GetComponent<Rigidbody>().useGravity = false;

                objInRange = false;
                interacting = true;

                heldDist = Vector3.Distance(transform.position, heldObj.transform.position);
                heldObjParent = heldObj.transform.parent;
                heldObj.transform.parent = transform;

                cdTimer_Interact = PCSettings.pcSettings.interactCD;
            }
        }
    }

    //  Use the Blast Ability
    public void blastAbility()
    {
        if (cdTimer_Blast > 0 || PCSettings.pcSettings.blastCost > abilityEnergy.energy) return;
        dropObject();

        abilityEnergy.expendEnergy(PCSettings.pcSettings.blastCost);
        createBlastWave();

        // Turn off last hit highlighting
        if (lastHit != null)
            lastHit.GetComponent<Highlighter>().Off();

        //  Apply Cool Downs
        cdTimer_Blast = PCSettings.pcSettings.blastCD;
        cdTimer_Interact = PCSettings.pcSettings.interactCD;
    }

    //  Apply a Force Vector to All Objects in the Blast's Path
    void createBlastWave()
    {
        //  Applies a forward force to all objects in a forward cone.

        //  RayCast Variables for a SphereCastAll and Detecting which Objects are in the Cone
        Vector3 rayOrigin = transform.position, rayDir = transform.forward;
        float coneAngle = PCSettings.pcSettings.blastAngle;
        //  Bell-Curve Cone
        float rayRadius = PCSettings.pcSettings.blastLength;

        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, rayRadius, rayDir, rayRadius, PCSettings.pcSettings.interactableObjectsLM);
        foreach (RaycastHit hitInfo in hits)
        {
            if (hitInfo.transform.GetComponent<ObjectBehavior>() != null && hitInfo.transform.GetComponent<ObjectBehavior>().trait_SpecialInteraction)
                continue;
            float angle = Vector3.Angle(rayDir, rayOrigin - hitInfo.transform.position);
            float dist = Vector3.Distance(rayOrigin, hitInfo.transform.position);
            if (angle < 180 - coneAngle || angle > 180 + coneAngle || dist > PCSettings.pcSettings.blastLength) continue; //  continue if object is not within the specified cone's bounds

            float forceMult = Mathf.Pow(1 - dist / PCSettings.pcSettings.blastLength, PCSettings.pcSettings.blastDecay);  //  exponentially scale the force's power according to distance
            Vector3 forceV = forceMult * PCSettings.pcSettings.blastForce * Vector3.Normalize(hitInfo.transform.position - transform.position);
            hitInfo.transform.GetComponent<Rigidbody>().AddForce(forceV);

            hitInfo.transform.GetComponent<InteractableObject>().blastInteract();
        }
    }

    //  Drop the Held Object
    void dropObject()
    {
        if (heldObj == null) return;    //  return if not holding an object

        heldObj.GetComponent<Rigidbody>().velocity = new Vector3();
        heldObj.GetComponent<Rigidbody>().useGravity = true;

        if (heldObj.layer == 2) heldObj.layer = 11;

        heldObj.transform.parent = heldObjParent;

        heldObj = null;
        interacting = false;
    }

    //  Manage the Held Object
    void manageHeldObject()
    {
        // Turn off highlighting
        heldObj.GetComponent<Highlighter>().Off();

        //  Break Interaction if Object is Too Far Away
        Vector3 restPoint = transform.position + heldDist * transform.forward;
        float dist = Vector3.Distance(heldObj.transform.position, restPoint);
        float distPercent = dist / PCSettings.pcSettings.breakHoldDist;
        if (distPercent > 1)
        {
            dropObject();
            return;
        }

        Vector3 velocity = GetComponent<Rigidbody>().velocity;
        if (dist != 0)
        {
            //  Move Object to Resting Point
            float objectVelocity = heldObj.GetComponent<ObjectSettings>().heldSpeed;

            Vector3 trajectory = restPoint - heldObj.transform.position;
            velocity += Mathf.Pow(distPercent, 0.5f) * objectVelocity * Vector3.Normalize(trajectory);
            //  To Prevent Jittering, Stop Object if it Could Reach the Point by Next Frame
            if (velocity.magnitude < objectVelocity * Time.deltaTime)
            {
                heldObj.GetComponent<Rigidbody>().velocity = new Vector3();
                return;
            }
            //  Update Velocity to Match Current Trajectory
        }
        heldObj.GetComponent<Rigidbody>().velocity = velocity;
    }

    public class AbilityEnergy
    {
        #region Attributes
        MonoBehaviour m;

        int _level = 0;

        float[] energyMaxes = { 20, 40, 60 };
        float _energy = 0, _energyInUse = 0;

        bool _regenerating = false;

        Texture enrgyMtrTex, enrgyMtrBckgrndTex;
        Color guiColor = new Color(255,255,255,255);
        float enrgyMtrBlockL = 100f, enrgyMtrBlockH = 31.25f, screenscalePosX = 0.9f, screenscalePosY = 0.8f;
        #endregion

        public AbilityEnergy(MonoBehaviour _m, Texture t1, Texture t2)
        {
            _energy = energyMax;
            m = _m; regenerating = true;
            enrgyMtrTex = t1;
            enrgyMtrBckgrndTex = t2;
        }

        public int level { get { return _level; } set { _level = Mathf.Clamp(value, 0, 2); } }
        public int levelReadable { get { return _level + 1; } set { _level = Mathf.Clamp(value - 1, 0, 2); } }
        public int levelUp() { return (_level = Mathf.Clamp(_level + 1, 0, 2)); }

        public float energyMax { get { return energyMaxes[_level]; } }
        public float energyMaxWLimit { get { return energyMaxes[_level] - energyInUse; } }

        public float energy { get { return _energy; } }
        public float energyInUse { get { return _energyInUse; } }

        public float expendEnergy(float expending)
        {
            return (_energy = Mathf.Clamp(energy - expending, 0, energyMaxWLimit));
        }
        public float reduceEnergyMax(float reduction)
        {
            reduction = Mathf.Abs(reduction);
            if (energy < reduction || energyInUse + reduction > energyMaxWLimit)
                return -1;
            _energyInUse += reduction;
            expendEnergy(reduction);
            return energyMaxWLimit;
        }
        public float restoreEnergyMax(float accretion)
        {
            accretion = -Mathf.Abs(accretion);
            if (energyInUse - accretion < 0)
                return -1;
            _energyInUse -= accretion;
            return energyMaxWLimit;
        }

        bool regenerating { get { return _regenerating; } set { if ((_regenerating = value)) m.StartCoroutine(regen()); } }
        IEnumerator regen()
        {
            yield return null;  //  Need to wait a frame before starting energy regeneration
            while (regenerating)
            {
                expendEnergy(-(Time.deltaTime * PCSettings.pcSettings.energyRegenRate));
                yield return new WaitForEndOfFrame();
            }
        }

        public void setGUIColor(Color c) { guiColor = c; }

        float barL = 0;
        public void drawEnergyBarValues()
        {
            GUI.color = Color.cyan;
            GUI.Label(new Rect(Screen.width * screenscalePosX - barL, Screen.height * screenscalePosY - 30, 200, 50), "Energy: " + energy.ToString());
        }
        public void drawEnergyBar()
        {
            GUI.color = guiColor;
            barL = levelReadable * enrgyMtrBlockL;
            GUI.DrawTexture(new Rect(Screen.width * screenscalePosX, Screen.height * screenscalePosY, -barL, enrgyMtrBlockH), enrgyMtrBckgrndTex);
            GUI.DrawTexture(new Rect(Screen.width * screenscalePosX, Screen.height * screenscalePosY, -barL * energy / energyMax, enrgyMtrBlockH), enrgyMtrTex);
        }
    }
    AbilityEnergy _abilityEnergy;
    public AbilityEnergy abilityEnergy { get { return _abilityEnergy; } }
    void initializeAbilityEnergy()
    {
        _abilityEnergy = new AbilityEnergy(this, enrgyMtrTex, enrgyMtrBckgrndTex);
    }

    void OnGUI()
    {
        abilityEnergy.drawEnergyBarValues();
        abilityEnergy.drawEnergyBar();
    }

    public bool levelup = false;
    void energyDebug()
    {
        if (levelup)
        {
            levelup = false;
            abilityEnergy.levelUp();
        }
    }

    void EmergencyReset()
    {
        heldObj = null;
        lastHit = null;
        heldDist = 0;
        objInRange = false;
        interacting = false;
        Debug.Log("THREW EMERGENCY RESET!");
    }
}