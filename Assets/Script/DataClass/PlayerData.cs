using System;
using UnityEngine;

[Serializable]
public class PlayerData 
{
    public string playerName, playerDeviceName;

    public PlayerData(string name, string deviceName)
    {
        playerName = name;
        playerDeviceName = deviceName;
    }
}
