using System;
using UnityEngine;

[Serializable]
public class ClienteleData 
{
    public string nama;
    public string type;
    public string status;

    public ClienteleData(string nama, string type, string status)
    {
        this.nama = nama;
        this.type = type;
        this.status = status;
    }

}
