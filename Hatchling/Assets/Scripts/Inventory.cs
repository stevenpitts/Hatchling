﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using System;
using System.Collections.Specialized;

public class Inventory : MonoBehaviour, ItemHolderInt {


    private Dictionary<string,int> counts = new Dictionary<string,int>();
    private Dictionary<string,GameObject> visibleBoxes = new Dictionary<string,GameObject>();
    private LinkedList<string> itemOrder = new LinkedList<string>();

    public int CountOf(string item) {
        try {
            return counts[item];
        }
        catch {
            return 0;
        }
    }


    public Dictionary<String,int> Counts { get { return counts;}} //TODO make this private and have all other stuff call CountOf
    public Dictionary<string,GameObject> VisibleBoxes {get {return visibleBoxes;}}

    private GameObject player;
    private PlayerBehavior pb;

    private HUD hud;

    private Dictionary<string,Dictionary<string,int>> craftingRecipes;

    [System.NonSerialized]
    public GameObject PrepareBuildObject;
    [System.NonSerialized]
    public string PrepareBuildName;
    [System.NonSerialized]
    public float PrepareBuildOffset;
    [System.NonSerialized]
    public bool PreparingBuild = false;

    public readonly int maxNumItems = 5;

    public readonly int maxExtraNumItems = 10;

    public readonly int maxStack = 50;
    public string[] noStackLimit = new string[] {"Coin",};

    public int maxTotalNumItems {
        get {
            return maxNumItems + maxExtraNumItems;
        }
    }


    public void CreateIcons() { /*C# is fucking stupid*/}
    public void DestroyIcons() { /*C# is fucking stupid*/}

    public int CurrentNumItems { get {return itemOrder.ToArray().Length;}}
    public int CurrentExtraNumItems {get {return visibleBoxes.Keys.ToArray().Length - itemOrder.ToArray().Length;}}
    public int CurrentTotalNumItems {get {return visibleBoxes.Keys.ToArray().Length;}}
    public bool NormalInventoryFull {get {return CurrentNumItems >= maxNumItems;}}
    public bool NormalInventoryOverflowing { get {return CurrentNumItems > maxNumItems;}}
    public bool ExtraInventoryFull {get {return CurrentExtraNumItems >= maxExtraNumItems;}}
    public bool ExtraInventoryOverflowing { get { return CurrentExtraNumItems > maxExtraNumItems;}}
    public bool TotalInventoryFull {get {return CurrentTotalNumItems >= maxTotalNumItems;}}
    public bool TotalInventoryOverflowing{get{return CurrentTotalNumItems >= maxTotalNumItems;}}

    public void SetCount(string item, int count) {
        int deltaItem = count - CountOf(item);
        if(deltaItem > 0) {
            for (int i=0;i<deltaItem;i++) {
                AddItem(item);
            }
        }
        else if (deltaItem < 0){
            for (int i=deltaItem;i<0;i++) {
                RemoveItem(item);
            }
        }

    }

    Vector3 GetDiscardPosition() {
        int maxDist = 10;
        RaycastHit hit = hud.CamBehavior.GetRayHit(maxDist:maxDist);
        if (object.Equals(hit,default(RaycastHit))) {
            return Camera.main.transform.position + Camera.main.transform.forward*maxDist;
        }
        return hit.point;
    }
    public void DiscardItem(string name,int numToDrop = -1) {
        if(numToDrop == -1 || numToDrop>counts[name]) {
            numToDrop = counts[name];
        }

        GameObject goTemplate = Resources.Load("InWorld/"+name) as GameObject;
        if (goTemplate == null) {
            Debug.LogError("Could not find inworld gameobject for "+name);
            return;
        }
        for (int i=0;i<numToDrop;i++) {
            //TODO add some sound to indicate dropping here
            RemoveItem(name);

            Vector3 spawnPosition = GetDiscardPosition();
            GameObject droppedGO = Instantiate(goTemplate);
            droppedGO.SetActive(true);
            droppedGO.transform.position = spawnPosition;
            try {
                droppedGO.GetComponent<PickupBehavior>().StackWithNearby();
            }
            catch(NullReferenceException) {
                Debug.LogError("Could not stack "+name+", didn't have pickupbehavior");
            }


        }
    }

    private int currentlySelectedSlot;
    public int CurrentlySelectedSlot {
        get { return currentlySelectedSlot;}
        set {
            string relevantItem = "None";

            string[] itemKeys = itemOrder.ToArray();

            int numSlots = itemKeys.Length;


            if(numSlots>0) {
                currentlySelectedSlot = value % numSlots;
                relevantItem = itemOrder.ToArray()[currentlySelectedSlot];
                foreach(string key in itemOrder) {
                    if (key.Equals(relevantItem)) {
                        visibleBoxes[key].GetComponent<Image>().color = Color.yellow;
                    }
                    else {
                        visibleBoxes[key].GetComponent<Image>().color = Color.white;
                    }
                }
            }
            player.GetComponent<PlayerBehavior>().SetEquippedItem(relevantItem);
        }
    }

    public void MoveToExtraInventory(string name) {
        if (ExtraInventoryFull) {
            DiscardItem(name);
            return;
        }
        try {
            visibleBoxes[name].transform.SetParent(hud.ExtraInventoryPanel.transform);
        }
        catch(KeyNotFoundException) {
            Debug.LogError("Tried moving "+name+" from normal to extra inventory, but visibleBoxes didn't contain "+name+".");
        }
        itemOrder.Remove(name);
        SettleSelectedItem();
    }

    public void MoveToNormalInventory(string name) {
        if (NormalInventoryFull) {
            DiscardItem(name);
            return;
        }
        visibleBoxes[name].transform.SetParent(hud.InventoryPanel.transform);
        itemOrder.AddLast(name);
        SettleSelectedItem();
    }

    public string CurrentlySelectedItem {
        get {
            try {
                return itemOrder.ToArray()[CurrentlySelectedSlot];
            }
            catch(IndexOutOfRangeException) {
                return "Hands";
            }
        }
        set {
            if (value.Equals("Hands")) {
                CurrentlySelectedSlot = 0;
                return;
            }
            try {
                CurrentlySelectedSlot = Array.IndexOf(itemOrder.ToArray(),value);
            }
            catch(KeyNotFoundException) {//
                Debug.LogError("Tried to select "+value+" on the inventory bar but it was not found");
            }
        }
    }

    public void SettleSelectedItem() {
        CurrentlySelectedSlot = currentlySelectedSlot;
    }

	// Use this for initialization
	void Start () {
        player = GameObject.FindWithTag("MainPlayer");
        pb = player.GetComponent<PlayerBehavior>();
        craftingRecipes = MakuUtil.LoadRecipeFile("Assets/SettingsFiles/CraftingRecipes.txt");
        hud = player.GetComponent<HUD>();
        hud.InventoryMenuOpen = false;
	}

	// Update is called once per frame
	void Update () {

	}

    public void AddItem(string name) {
        //TODO make sure that if the item exists in extra, it goes to extra, not normal
        name = char.ToUpper(name[0]) + name.Substring(1);
        if (!itemOrder.Contains(name)) {
            itemOrder.AddLast(name);
        }
        if (!counts.ContainsKey(name)) {
            counts[name] = 0;
        }
        counts[name] += 1;
        if(!visibleBoxes.ContainsKey(name)) {
            AddBox(name);
        }
        visibleBoxes[name].transform.Find("Text").GetComponent<Text>().text = ""+counts[name];

        if (itemOrder.ToArray().Length == 1) {

            CurrentlySelectedSlot = 0;
        }
        if (NormalInventoryOverflowing) {
            if (ExtraInventoryOverflowing) {
                DiscardItem(name);
            }
            else {
                MoveToExtraInventory(name);
            }
        }
        if(counts[name] > maxStack && !(noStackLimit.Contains(name))) {
            DiscardItem(name, numToDrop: counts[name]-maxStack);
        }
    }

    public void RemoveItem(string name) {
        try {
            counts[name] -= 1;
            visibleBoxes[name].transform.Find("Text").GetComponent<Text>().text = ""+counts[name];
            if(counts[name] == 0) {
                counts.Remove(name);
                itemOrder.Remove(name);
                Destroy(visibleBoxes[name]);
                visibleBoxes.Remove(name);
            }
        }
        catch (KeyNotFoundException) {
            Debug.Log("Attempted to remove item "+name+", but did not have any");
        }
        SettleSelectedItem();

    }

    Sprite GetSprite(string name) {
        Sprite t = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Resources/InventoryIcons/"+name+".png", typeof(Sprite));
        if (t == null) {
            Debug.LogError("Could not find sprite for "+name);
            t = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Resources/InventoryIcons/unknown.png", typeof(Sprite)); //Draw an unknown thing
        }
        return t;
    }

    void AddBox(string name){
        GameObject box = Instantiate(Resources.Load("InventoryIcons/BoxFab") as GameObject);
        Sprite spr = GetSprite(name);
        box.GetComponent<Image>().sprite = spr;
        box.name = name+"Box";
        box.transform.GetComponent<InfoHover>().infoStr = name;
        box.transform.SetParent(hud.InventoryPanel.transform,false);
        box.transform.GetComponent<InventoryIconBehavior>().Item = name;
        visibleBoxes[name] = box;
    }

    public void CraftItem(string name) {
        Dictionary<string,int> ingredients = craftingRecipes[name];
        foreach(KeyValuePair<string,int> ingredientPair in ingredients) {
            string ingredient = ingredientPair.Key;
            int count = ingredientPair.Value;
                for (int i=0;i<count;i++) {
                    RemoveItem(ingredient);
                }
        }
        pb.PlayFXAudio(pb.CraftingSound);
        AddItem(name);
        UpdateCraftingRecipes();
    }








    public IEnumerable<string> GetPossibleCrafts() {
        foreach(string possibleCraft in craftingRecipes.Keys) {
            if(CanCraftItem(possibleCraft)) {
                yield return possibleCraft;
            }
        }
    }

    public bool CanCraftItem(string possibleCraft) {
        Dictionary<string,int> ingredients = craftingRecipes[possibleCraft];
        foreach(KeyValuePair<string,int> ingredientPair in ingredients) {
            string ingredient = ingredientPair.Key;
            int count = ingredientPair.Value;
            if(CountOf(ingredient) < count) {
                return false;
            }
        }
        return true;
    }

    public void UpdateCraftingRecipes() {
        foreach(Transform child in hud.CraftingPanel.transform) {
            Debug.Assert(child.GetComponent<Button>() != null);
            Destroy(child.gameObject);
        }
        IEnumerable<string> possibleCrafts = GetPossibleCrafts();
        foreach(string possibleCraft in possibleCrafts) {
            GameObject possibleCraftButton = Instantiate(Resources.Load("CraftingSelectionButton") as GameObject);
            possibleCraftButton.transform.SetParent(hud.CraftingPanel.transform,false);
            possibleCraftButton.name = "Craft"+possibleCraft+"Button";
            possibleCraftButton.transform.Find("Text").GetComponent<Text>().text = possibleCraft;
            possibleCraftButton.GetComponent<CraftingSelection>().itemName = possibleCraft;
        }
    }
}
