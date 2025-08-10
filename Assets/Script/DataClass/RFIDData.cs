using UnityEngine;

public class RFIDData
{
    public string uid;
    public string letter;
    public string nama;

    public RFIDData(string uid, string huruf, string nama)
    {
        this.uid = uid;
        this.letter = huruf;
        this.nama = nama;
    }
}