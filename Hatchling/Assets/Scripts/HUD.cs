﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using CardboardKeep;

public class HUD : MonoBehaviour {

    public bool UsingUI;

    private Queue<Dialogue> dialogueQueue = new Queue<Dialogue>();



    private Text infoText;
    public string InfoStr {
        get {
            return infoText.text;
        }
        set {
            infoText.text = value;
        }
    }

    public void SetHealthStat(int healthStat) {
        HealthText.text = healthStat.ToString();
    }
    public void SetDefenseStat(int defenseStat) {
        DefenseText.text = defenseStat.ToString();
    }
    public void SetAttackStat(int attackStat) {
        AttackText.text = attackStat.ToString();
    }
    public void SetHungerStat(int hungerStat) {
        HungerText.text = hungerStat.ToString();
    }

    public GameObject CurrentChest;

    [System.NonSerialized]
    public GameObject InventoryPanel, ExtraInventoryPanel, CraftingPanel, CraftingPanelView, PausePanel, ArmorPanel, ChestPanel, DialoguePanel, DialogueFace;
    [System.NonSerialized]
    public Text DialogueText, HealthText, DefenseText, AttackText, HungerText, ThirstText, OxygenText;
    [System.NonSerialized]
    public GameObject Player, Cam;
    [System.NonSerialized]
    public Inventory Inventory;
    [System.NonSerialized]
    public CameraBehavior CamBehavior;
    [System.NonSerialized]
    public UConsole console;
    [System.NonSerialized]
    public UnityStandardAssets.Characters.FirstPerson.FirstPersonController FPC;

    public bool CursorFree {
        get {
            return Cursor.visible;
        }
        set {
            Cursor.lockState = value?CursorLockMode.None:CursorLockMode.Locked;
            Cursor.visible = value;
            FPC.m_MouseLook.lockCursor = !value;
        }
    }


    void Awake() {
        Player = GameObject.FindWithTag("MainPlayer");
        InventoryPanel = GameObject.FindWithTag("InventoryPanel");
        ExtraInventoryPanel = GameObject.FindWithTag("ExtraInventoryPanel");
        CraftingPanel = GameObject.FindWithTag("CraftingPanel");
        CraftingPanelView = GameObject.FindWithTag("CraftingPanelView");
        PausePanel = GameObject.FindWithTag("PausePanel");
        ArmorPanel = GameObject.FindWithTag("ArmorPanel");
        ChestPanel = GameObject.FindWithTag("ChestPanel");
        DialoguePanel = GameObject.FindWithTag("DialoguePanel");
        DialogueFace = GameObject.FindWithTag("DialogueFace");
        DialogueText = GameObject.FindWithTag("DialogueText").GetComponent<Text>();
        HealthText = GameObject.FindWithTag("HealthText").GetComponent<Text>();
        DefenseText = GameObject.FindWithTag("DefenseText").GetComponent<Text>();
        AttackText = GameObject.FindWithTag("AttackText").GetComponent<Text>();
        HungerText = GameObject.FindWithTag("HungerText").GetComponent<Text>();
        ThirstText = GameObject.FindWithTag("ThirstText").GetComponent<Text>();
        OxygenText = GameObject.FindWithTag("OxygenText").GetComponent<Text>();
		infoText = GameObject.FindWithTag("InfoText").GetComponent<Text>();
        Inventory = Player.GetComponent<Inventory>();
        Cam = GameObject.FindWithTag("MainCamera");
        CamBehavior = Cam.GetComponent<CameraBehavior>();
        console = GameObject.FindWithTag("ConsolePanel").GetComponent<UConsole>();
        FPC = GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController> ();
    }

	void Start () {
        PauseMenuOpen = ChestMenuOpen = false;
        DialogueContinue(); //Unless we have dialogue at the start of the game, this just closes the pause menu
	}

	// Update is called once per frame
	void Update () {

	}

    public void InitiateDialogue(string fileName) {
        string path = "Assets/Dialogue/"+fileName+".txt";
        StreamReader reader = new StreamReader(path);
        string currentLine;
        while(true){
            currentLine = reader.ReadLine();
            if(currentLine != null){
                int nameSeperator = currentLine.IndexOf(' ');
                string speakerName = currentLine.Substring(0,nameSeperator);
                string dialogueText = currentLine.Substring(nameSeperator+1);
                Dialogue nextDialogue = new Dialogue(speakerName, dialogueText);
                dialogueQueue.Enqueue(nextDialogue);
            }
            else {
                break;
            }
        }
        reader.Close();
        DialogueContinue();
    }

    public void DialogueContinue() { //called when continue button is pressed
        if (dialogueQueue.Count == 0) {
            DialoguePanelOpen = false;
        }
        else {
            DialoguePanelOpen = true;
            Dialogue nextDialogue = dialogueQueue.Dequeue();
            DialogueFace.GetComponent<Image>().sprite = nextDialogue.Face;
            DialogueText.GetComponent<Text>().text = nextDialogue.text;
        }

    }

    public void ClearInfoBox() {
        InfoStr = "";
    }

    public void CloseAllUI() {
        InventoryMenuOpen = PauseMenuOpen = ChestMenuOpen = false;
    }

    public bool AnySpecialPanelOpen() {
        return PauseMenuOpen || ChestMenuOpen;
    }

    private bool dialoguePanelOpen = false;
    public bool DialoguePanelOpen {
        get { return dialoguePanelOpen;}
        set {
            CursorFree = value;
            DialoguePanel.SetActive(value);
            UsingUI = value;
        }
    }

    private bool inventoryMenuOpen = false;
    public bool InventoryMenuOpen {
        get {return inventoryMenuOpen;}
        set {
            CursorFree = value;
            CraftingPanelView.SetActive(value);
            ExtraInventoryPanel.SetActive(value);
            ArmorPanel.SetActive(value);
            if (value) {
                Inventory.UpdateCraftingRecipes();
            }
            inventoryMenuOpen = value;
            UsingUI = value;
        }
    }

    private bool pauseMenuOpen = false;
    public bool PauseMenuOpen {
        get {
            return pauseMenuOpen;
        }
        set {
            CursorFree = value;
            PausePanel.SetActive(value);
            if(value) {
                //update the pause menu if necessary here
            }
            pauseMenuOpen = value;
            UsingUI = value;
        }
    }

    private bool chestMenuOpen = false;
    public bool ChestMenuOpen {
        get { return chestMenuOpen;}
        set {
            if (value && CurrentChest == null) {
                Debug.LogError("Attempted to open chest menu without CurrentChest being assigned in HUD");
            }
            if(value) {
                CurrentChest.GetComponent<ContainerInt>().CreateIcons();
            }
            else if (!value && CurrentChest != null) {
                CurrentChest.GetComponent<ContainerInt>().DestroyIcons();
            }
            CursorFree = value;
            ChestPanel.SetActive(value);
            chestMenuOpen = value;
            ExtraInventoryPanel.SetActive(value);
            UsingUI = value;
        }
    }
    public bool ConsoleMenuOpen {
        get {
            return console.on;
        }
    }

}
public class Dialogue {

    private static Dictionary<string,Sprite> faces = new Dictionary<string,Sprite>();

    public string faceName;

    public Sprite Face {
        get {
            return faces[faceName];
        }
    }
    public string text;

    public Dialogue(string faceName, string text) {
        this.faceName = faceName;
        if (!faces.ContainsKey(faceName)) {
            faces[faceName] = GetSprite(faceName);
        }
        this.text = text;
    }

    Sprite GetSprite(string name) {
        Sprite t = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Resources/DialogueFaces/"+name+".png", typeof(Sprite));
        if (t == null) {
            Debug.LogError("Could not find sprite for "+name);
            t = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Resources/DialogueFaces/unknown.png", typeof(Sprite)); //Draw an unknown thing
        }
        return t;
    }

}
