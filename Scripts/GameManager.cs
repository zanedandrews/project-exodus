using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PixelCrushers.DialogueSystem;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This singleton handles core functionality related to loading Scenes and stores gamewide references.
/// The GameObject this is attached to needs to be hardwired into the FIRST scene of any Builds we do, after that it'll just be carried over into new Scenes.
/// 
/// (2/23/23) Copy+Paste the GameManager GameObject from Intro.scene into your Scenes for the time being.
/// 
/// PROTIP: GameManager.gm is the static reference to this (callable from basically any script). 
/// Use this to your advantage (i.e, give other scripts access to the Scene we're in, create game wide references, etc.)
/// 
/// WARNING: DO NOT MAKE CHANGES TO THIS SCRIPT WITHOUT COMMENTING ^_^
/// Author: Zane
/// </summary>
/// 

public class GameManager : MonoBehaviour
{
    public static GameManager gm; // It me
    public soRep so_Rep; // Storage hub for assets, SOs, etc.
    public GameObject eventSystem;
    public SceneLoader sceneLoader;
    public CanvasManager canvasManager; // Reference for any Canvas that we initialize
    //public AudioManager audioManager;
    //public AudioMixers audioMixers;//Added this for the eMixers in AudioMixers script
    public GameObject dialogueSystem;
    [SerializeField] Transform partyMembersHolder;
    public PartyMember[] partyMembers;
    public Map map;
    //A reference to the GlobalNightLight
    public Light2D globalNightLight;

    public int previousSceneIndex;
    public Camera mainCam;
    public GameObject modestyScreen;
    //The list of indexes corresponding to random events that have already been experienced (used exclusively in RandomEventTrigger)
    public List<int> usedIndices;

    // Reference to prefab that contains objects for travel scene. Becomes reference to instantiated object in runtime.
    // Maybe moving it somewhere else later?
    public GameObject travelContainer;

    void Awake()
    {
        //Create a singleton instance of the GameManager
        if (gm != null && gm != this)
        {
            Destroy(gameObject);
        }
        else if (gm != this)
        {
            gm = this;
            DontDestroyOnLoad(gameObject);
        }
        // Added this so we're forced to edit the map prefab instead of an instance that lives in the scene (less confusing this way)
        if (map == null) map = Instantiate(map, transform);
        //Initialize usedIndices list
        usedIndices = new List<int>();
        InitPartyMembers();
    }
    void InitPartyMembers()
    {
        foreach (PartyMember partyMember in partyMembers)
        {
            // Add a UnitStats component to each party member as soon as the game starts
            UnitStats unitStats = partyMember.gameObject.AddComponent<UnitStats>();
            // Start each party member at Level 5 (we can change this later)
            unitStats.Init(partyMember.so_Unit, null, 5, false);
        }
    }
    //Returns the game settings to what they are when the game is first booted up. Used when returning to the main menu after Game Over, Quitting, Completing game, etc.
    public void ResetGame()
    {
        foreach (PartyMember partyMember in partyMembers)
        {
            //Remove the Unitstats component from each party member
            Destroy(partyMember.GetComponent<UnitStats>());
            // Add a UnitStats component to each party member as soon as the game starts
            UnitStats unitStats = partyMember.gameObject.AddComponent<UnitStats>();
            //Run Awake to initialize unitStats
            unitStats.Awake();
            // Start each party member at Level 5 (we can change this later)
            unitStats.Init(partyMember.so_Unit, null, 5, false);
        }
        map.Start();
        usedIndices.Clear();
        DialogueLua.SetVariable("usedCrystal", false);
        DialogueLua.SetVariable("gotCrystal", false);
        globalNightLight.color = new Color(32f/255f, 32f/255f, 159f/255f);
        so_Rep.resetDialogueTriggers.TriggerEvent();
    }
    public void ShowTravelSprites(bool _bool)
    {
        //The scene where this function gets passed "true" is Travel and in that scene we use a different camera
        mainCam.enabled = !_bool;
        foreach (PartyMember partyMember in partyMembers)
        {
            partyMember.TravelSprite.SetActive(_bool);
        }
    }
    //Used in events and during camp to heal the party 
    public void FullyHealParty()
    {
        //Goes thorugh each party member...
        foreach (PartyMember partyMember in partyMembers)
        {
            //...and adjusts their health such that their current health is set to their max health
            partyMember.GetComponent<UnitStats>().Health = partyMember.GetComponent<UnitStats>().MaxHealth;
        }
    }
    //Helper function that returns true if any party member has energy and can therefore run on the travel scene
    public bool CheckIfCanRun()
    {
        //Initialize a boolean variable used to determine if the party can travel fast/run on the traversal scene
        bool canRun = false;
        //Loop through party members...
        foreach (PartyMember partyMember in GameManager.gm.partyMembers)
        {
            //Determine if a party member has any energy
            canRun = partyMember.gameObject.GetComponent<UnitStats>().Energy > 0;
            //If so, break out of the loop
            if (canRun) break;
        }
        //Return true if a party member has energy or false if no party member has any energy
        return canRun;
    }
    // Central method for checking for success/failure given a likelihood of success
    // (Just putting this here to avoid potential bugs when writing this logic over and over in different circumstances)
    public bool CheckRandomSuccess(float _likelihood)
	{
        if (_likelihood < 0 || _likelihood > 1)
		{
            throw new System.Exception("Likelihood out of bounds");
		}
        return _likelihood > 0 && Random.value <= _likelihood;
	}
    //Helper function that returns true if the party is currently running on the travel scene
    public bool IsRunning()
    {
        return partyMembersHolder.GetComponent<AnimatorTransitions>().GetMoveSpeed() == 2;
    }
    public bool IsStopped()
    {
        return partyMembersHolder.GetComponent<AnimatorTransitions>().GetMoveSpeed() == 0;
    }
    public void ReviveDeadPartyMembers()
    {
        foreach (PartyMember partyMember in GameManager.gm.partyMembers)
        {
            partyMember.GetComponent<UnitStats>().TryRevive();
        }
    }
    public void ResetPartyMemberStatChanges()
    {
        foreach (PartyMember partyMember in partyMembers)
        {
            partyMember.GetComponent<UnitStats>().ResetStatChanges();
        }
        Debug.Log("Party member stat changes have been reset!");
    }

    public void ToggleModesty()
    {
        modestyScreen.SetActive(!modestyScreen.activeSelf);
    }
}
