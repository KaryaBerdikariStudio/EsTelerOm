using System;
using UnityEngine;

[Serializable]
public class SkorData 
{
    public string playerName;
    public int playerScore;
    public string scoreDate;


    public SkorData(string playerName,int playerScore)
    {
        this.playerName = playerName;
        this.playerScore = playerScore;
    }

    //helper from api network manager to get skor data from db
    public void InputSkorData()
    {
        scoreDate = DateTime.Now.ToString("d");
    }
}
