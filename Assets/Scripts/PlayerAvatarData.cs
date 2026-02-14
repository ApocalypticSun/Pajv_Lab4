using System;
using PlayFab.Json;
using UnityEngine;

[Serializable]
public class PlayerAvatarData 
{
    [field: SerializeField]
    public string OutfitID {get;  set; }
    [field: SerializeField]
    public string HairStyleID {get;  set; }
    [field: SerializeField]
    public string NecklaceAccessoryID {get;  set; }

    public string LogNumber {get;  set; }
    public string TimeSpendOnline {get;  set; }

    public string ToJson() => PlayFabSimpleJson.SerializeObject(this);
}
