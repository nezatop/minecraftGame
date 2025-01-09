#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MultiCraft.Scripts.Engine.Core.Blocks;
using MultiCraft.Scripts.Engine.Core.CraftingSystem;
using MultiCraft.Scripts.Engine.Core.Items;

public class BlockItemEditorWindow : EditorWindow
{
    private string blockName = "New Block";
    private bool isFlora = false;
    private ToolType toolToDig = ToolType.None;
    private int minToolLevelToDig = 0;
    private int blockDurability = -1;
    private bool isTransparent = false;
    private bool haveInventory = false;
    private Texture2D[] blockTextures = new Texture2D[6];
    private AudioClip stepSound, digSound, placeSound;

    private string itemName = "New Item";
    private Sprite itemIcon;
    private ItemType itemType;
    private int maxStackSize = 1;
    private CraftRecipe craftRecipe;
    private int maxDurability = 0;
    private int damage = 0;
    private ToolType itemToolType;
    private int miningLevel = 0;
    private float miningMultiplier = 1f;

    [MenuItem("MultiCraft/Block & Item Editor")]
    public static void OpenWindow()
    {
        GetWindow<BlockItemEditorWindow>("Block & Item Editor");
    }

    private void OnGUI()
    {
        DrawBlockAndItemEditor();
    }

    private void DrawBlockAndItemEditor()
    {
        blockName = EditorGUILayout.TextField("Block Name", blockName);
        isFlora = EditorGUILayout.Toggle("Is Flora", isFlora);
        toolToDig = (ToolType)EditorGUILayout.EnumPopup("Tool To Dig", toolToDig);
        minToolLevelToDig = EditorGUILayout.IntField("Min Tool Level To Dig", minToolLevelToDig);
        blockDurability = EditorGUILayout.IntField("Durability", blockDurability);
        isTransparent = EditorGUILayout.Toggle("Is Transparent", isTransparent);
        haveInventory = EditorGUILayout.Toggle("Have Inventory", haveInventory);

        GUILayout.Label("Block Textures", EditorStyles.boldLabel);
        for (int i = 0; i < blockTextures.Length; i++)
        {
            blockTextures[i] = (Texture2D)EditorGUILayout.ObjectField($"Texture {i + 1}", blockTextures[i], typeof(Texture2D), false);
        }

        GUILayout.Label("Block Sounds", EditorStyles.boldLabel);
        stepSound = (AudioClip)EditorGUILayout.ObjectField("Step Sound", stepSound, typeof(AudioClip), false);
        digSound = (AudioClip)EditorGUILayout.ObjectField("Dig Sound", digSound, typeof(AudioClip), false);
        placeSound = (AudioClip)EditorGUILayout.ObjectField("Place Sound", placeSound, typeof(AudioClip), false);

        itemName = blockName;
        itemIcon = (Sprite)EditorGUILayout.ObjectField("Icon", itemIcon, typeof(Sprite), false);
        itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);
        maxStackSize = EditorGUILayout.IntField("Max Stack Size", maxStackSize);

        // Custom CraftRecipe Editor
        GUILayout.Label("Craft Recipe", EditorStyles.boldLabel);
        if (craftRecipe == null)
        {
            craftRecipe = new CraftRecipe();
        }

        craftRecipe.Amount = EditorGUILayout.IntField("Amount", craftRecipe.Amount);

        if (craftRecipe.Items == null)
        {
            craftRecipe.Items = new List<ItemScriptableObject>();
        }

        EditorGUILayout.LabelField("Items:");
        for (int i = 0; i < craftRecipe.Items.Count; i++)
        {
            craftRecipe.Items[i] = (ItemScriptableObject)EditorGUILayout.ObjectField($"Item {i + 1}", craftRecipe.Items[i], typeof(ItemScriptableObject), false);
        }

        if (GUILayout.Button("Add Item"))
        {
            craftRecipe.Items.Add(null);
        }

        if (craftRecipe.Items.Count > 0 && GUILayout.Button("Remove Last Item"))
        {
            craftRecipe.Items.RemoveAt(craftRecipe.Items.Count - 1);
        }

        maxDurability = EditorGUILayout.IntField("Max Durability", maxDurability);
        damage = EditorGUILayout.IntField("Damage", damage);
        itemToolType = (ToolType)EditorGUILayout.EnumPopup("Tool Type", itemToolType);
        miningLevel = EditorGUILayout.IntField("Mining Level", miningLevel);
        miningMultiplier = EditorGUILayout.FloatField("Mining Multiplier", miningMultiplier);
        
        if (GUILayout.Button("Save Block & Item"))
        {
            Create();
        }
    }
    private void Create()
    {
        ItemScriptableObject item = CreateInstance<ItemScriptableObject>();
        item.Name = itemName;
        item.Icon = itemIcon;
        item.Type = itemType;
        item.MaxStackSize = maxStackSize;
        item.CraftRecipe = craftRecipe;
        item.MaxDurability = maxDurability;
        item.Damage = damage;
        item.ToolType = itemToolType;
        item.MiningLevel = miningLevel;
        item.MiningMultiplier = miningMultiplier;
        item.IsPlaceable = true; // Assume the item is placeable since it's linked to a block

        string itemFolderPath = $"Assets/Multicraft/ScriptableObject/Items/{itemType}";
        if (!AssetDatabase.IsValidFolder(itemFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/Multicraft/ScriptableObject/Items", itemType.ToString());
        }
        string itemPath = $"{itemFolderPath}/{itemName}.asset";
        AssetDatabase.CreateAsset(item, itemPath);

        BlockScriptableObject block = CreateInstance<BlockScriptableObject>();
        block.Name = blockName;
        block.IsFlora = isFlora;
        block.ToolToDig = toolToDig;
        block.MinToolLevelToDig = minToolLevelToDig;
        block.Durability = blockDurability;
        block.IsTransparent = isTransparent;
        block.HaveInventory = haveInventory;
        block.Textures = new Texture2D[blockTextures.Length];
        for (int i = 0; i < blockTextures.Length; i++)
        {
            block.Textures[i] = blockTextures[i];
        }

        block.StepSound = stepSound;
        block.DigSound = digSound;
        block.PlaceSound = placeSound;

        string blockPath = $"Assets/Multicraft/ScriptableObject/Blocks/{itemName}.asset";
        AssetDatabase.CreateAsset(block, blockPath);

        // Link the Item to the Block
        item.BlockType = block;
        block.DropItem = item;

        AssetDatabase.SaveAssets();
    }
}
#endif