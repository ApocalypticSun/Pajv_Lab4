using NaughtyAttributes;
using UnityEngine;
using PlayFab.ClientModels;
using PlayFab;
using System.Collections.Generic;
using System;
using PlayFab.Json;
using NUnit.Framework.Interfaces;
using PFProg = PlayFab.ProgressionModels;
using System.Collections;

public class PlayFabExamples : MonoBehaviour
{
    [SerializeField]
    public string AvatarName;
    [SerializeField] 
    private PlayerAvatarData playerAvatarData;
    [SerializeField]
    private float GiveXP;
    [SerializeField] 
    [ReadOnly] 
    private PlayerAvatarData currentPlayerAvatarData;
    [SerializeField] 
    private int scoreToSubmit = 0;


    private  float WaitingTime = 0f;
    private bool isRunning = false;
    private float startTime;
    private float elapsedTime;
    private string _entityId;
    private string _entityType;

    [SerializeField] private string playerEmail = "ceva@ceva.com";
    [Button]
    private void LoginWithCustomID()
    {
        var request  = new LoginWithCustomIDRequest
        {
            CreateAccount = true,
            CustomId = PlayFabSettings.DeviceUniqueIdentifier
        };
        PlayFabClientAPI.LoginWithCustomID(request,
        result =>
        {
            Debug.Log($"Logged in succesfully: {result.PlayFabId}");
            _entityId = result.EntityToken.Entity.Id;
            _entityType = result.EntityToken.Entity.Type;
            ToggleTimer();
            Invoke ("CallCloudScript",WaitingTime);
        }, error =>
        {
            Debug.LogError($"Error: {error.GenerateErrorReport()}");
        }
        );
        // start timer.
    }
    [Button]
    private void ExitGame()
    {
        ToggleTimer();
    }
    [Button]
    private void GiveXPToPlayer()
    {
        SendXp(GiveXP);
    }
    [Button]
    private void SavePlayerAvatarData ()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
                {
                    { AvatarName, playerAvatarData.ToJson() }
                }
        };
        PlayFabClientAPI.UpdateUserData(request,
        result => Debug.Log("Data has been saved remotely!"),
        error => Debug.LogError(error.GenerateErrorReport())
        );
    }
    
    [Button]
    public void GetAvatarData()
    {
        var request = new GetUserDataRequest
        {
            Keys = new List<string> {AvatarName}
        };

        PlayFabClientAPI.GetUserData(request,
        result =>
        {
            if(result.Data != null && result.Data.TryGetValue(AvatarName, out var record))
            {
                currentPlayerAvatarData = PlayFabSimpleJson.DeserializeObject<PlayerAvatarData>(record.Value);
                Debug.Log($"[PlayFabExamples] Retrieved Avatar Data: OutfitID = {currentPlayerAvatarData.OutfitID}, HairStyleID = {currentPlayerAvatarData.HairStyleID}, NecklaceAccessoryID = {currentPlayerAvatarData.NecklaceAccessoryID}");
            }
            else
            {
                Debug.Log("[PlayFabExamples] No avatar data found.");
            }
        },
        OnPlayFabError);
    }
    
    [Button]
    private void SubmitScore()
    {
        var request = new PFProg.UpdateStatisticsRequest
        {
            Entity = new PFProg.EntityKey
            {
                Id = _entityId,
                Type = _entityType
            },
            Statistics = new List<PFProg.StatisticUpdate>
            {
                new PFProg.StatisticUpdate
                {
                    Name = "HighScore",
                    Scores = new List<string> { scoreToSubmit.ToString() }
                }
            }
        };

        PlayFabProgressionAPI.UpdateStatistics(request,
            _ => Debug.Log($"Statistic updated! Score={scoreToSubmit}"),
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }
    [Button]
    private void GetLeaderboard()
    {
        var request = new PFProg.GetEntityLeaderboardRequest
        {
            LeaderboardName = "Test",
            StartingPosition = 1,  // 1 = beginning of leaderboard (not 0!).
            PageSize = 10          // Min 1, max 100.
        };
        PlayFabProgressionAPI.GetLeaderboard(request,
            result =>
            {
                foreach (var entry in result.Rankings)
                {
                    //var score = (entry.Scores != null && entry.Scores.Count > 0) ? entry.Scores[0] : "0";
                    //Debug.Log($"{entry.Rank}. {entry.DisplayName}: {score}");
                    Debug.Log($"{entry.Rank}. {entry.DisplayName}: {entry.Scores[0]}");
                }
            },
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }
    

    //Nu se atinge
    private void CallCloudScript()
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "incrementCounter",
            GeneratePlayStreamEvent = true
        };
        PlayFabClientAPI.ExecuteCloudScript(request,
        result =>
        {
            /* var counterValue = Convert.ToInt32(result.FunctionResult);
            Debug.Log($"Returned counter value: {counterValue}"); */
            var resultDict = PlayFabSimpleJson.DeserializeObject<Dictionary<string,object>>(result.FunctionResult.ToString());
            if(resultDict != null && resultDict.TryGetValue("Counter", out var counter))
            {
                Debug.Log($"Log Counter: {counter}");
            }
            else
            {
                Debug.Log("Log Counter not found in the response.");
            }
        },
        error =>
        {            
            Debug.LogError($"Cloud Script error: {error.GenerateErrorReport()}");
        });
    }
    private void OnPlayFabError(PlayFabError playFabError)
    {
        Debug.LogError($"[PlayFabExamples] PlayFab Error: {playFabError.GenerateErrorReport()}");
    }

    public void ToggleTimer()
    {
        if (!isRunning)
        {
            startTime = Time.realtimeSinceStartup;
            isRunning = true;
        }
        else
        {
            elapsedTime = Time.realtimeSinceStartup - startTime;
            isRunning = false;
            Debug.Log($"Timer stopped. Time elapsed: {elapsedTime:F2} seconds");
            CallCloudScriptTimp(elapsedTime);
        }
    }
    private void CallCloudScriptTimp(float sessionTime)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "TimeSpendOnline",
            FunctionParameter = new
            {
                sessionTime = sessionTime
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
        result =>
        {
            var resultDict =
                PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(
                    result.FunctionResult.ToString());

            if (resultDict != null && resultDict.TryGetValue("TimeSpendOnline", out var total))
            {
                Debug.Log($"Total Time Spend Online: {total} seconds");
            }
            else
            {
                Debug.LogWarning("TimeSpendOnline not found in response");
            }
        },
        error =>
        {
            Debug.LogError($"Cloud Script error: {error.GenerateErrorReport()}");
        });
    }
    private void SendXp(float xpToAdd)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "AddXP",
            FunctionParameter = new
            {
                xp = xpToAdd
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
        result =>
        {
            var dict = PlayFabSimpleJson.DeserializeObject<Dictionary<string, object>>(
                result.FunctionResult.ToString());

            if (dict == null)
            {
                Debug.LogWarning("No data returned from Cloud Script");
                return;
            }

            float xp = dict.TryGetValue("XP", out var xpObj) ? Convert.ToSingle(xpObj) : 0f;
            int level = dict.TryGetValue("Level", out var lvlObj) ? Convert.ToInt32(lvlObj) : 1;
            bool leveledUp = dict.TryGetValue("LeveledUp", out var luObj) && Convert.ToBoolean(luObj);

            /* if (dict.TryGetValue("XP", out var xpObj) &&
                dict.TryGetValue("Level", out var lvlObj))
            {
                float xp = Convert.ToSingle(xpObj);
                int level = Convert.ToInt32(lvlObj);

                Debug.Log($"Level: {level} | XP: {xp:F2}");
            }
            else
            {
                Debug.LogWarning("XP or Level missing from response");
            } */
            Debug.Log($"Level: {level} | XP: {xp:F2} | LeveledUp: {leveledUp}");
            if(leveledUp == true)
                CallSendLevelUpEmail();
        },
        error =>
        {
            Debug.LogError($"Cloud Script error: {error.GenerateErrorReport()}");
        });
    }
    private IEnumerator SendLevelUpEmailAfterDelay(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        CallSendLevelUpEmail();
    }
    private void CallSendLevelUpEmail()
    {
        var req = new ExecuteCloudScriptRequest
        {
            FunctionName = "SendLevelUpEmail",
            FunctionParameter = new { playerEmail = playerEmail },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(req,
            _ => Debug.Log("SendLevelUpEmail called."),
            err => Debug.LogError(err.GenerateErrorReport()));
    }
}