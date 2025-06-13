using UnityEngine;

[CreateAssetMenu(fileName = "Game Config", menuName = "Scriptable Objects/Game Config")]
public class GameConfigScriptableObject : ScriptableObject
{
    public bool RandomizeLevel = false;
    public string RandomString = "";
}
