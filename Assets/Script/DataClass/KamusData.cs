using UnityEngine;

[System.Serializable]
public class KamusData
{
    public string tipe;
    public string bahasaDaerah;
    public string bahasaIndonesia;

    public KamusData(string tipe, string bahasaDaerah, string bahasaIndonesia)
    {
        this.tipe = tipe;
        this.bahasaDaerah = bahasaDaerah;
        this.bahasaIndonesia = bahasaIndonesia;
    }
}
