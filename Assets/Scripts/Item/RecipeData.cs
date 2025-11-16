using UnityEngine;

[CreateAssetMenu(menuName = "MyFarm/RecipeData")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public Sprite icon;
    public int recipeMinutes = 3;
    public GameObject outputPrefebs;

    [System.Serializable] public struct Cost { public ItemType type; public int count; }
    public Cost[] costs;

    public ItemType outputItem;
    public int outItemCount = 1;

}
    
