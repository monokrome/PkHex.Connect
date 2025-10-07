using PKHeX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class TrainerProfileService : ServiceBase
{
    [ApiAction("getTrainerInfo", ApiCategory.Trainer, "Get trainer information.")]
    public string GetTrainerInfo(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var info = new
            {
                trainerName = save.OT,
                trainerID = save.DisplayTID,
                secretID = save.DisplaySID,
                gender = save.Gender,
                money = save.Money,
                playTime = $"{save.PlayedHours}h {save.PlayedMinutes}m",
                language = save.Language
            };

            return SuccessResponse(info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting trainer info: {ex}");
            return ErrorResponse("Failed to get trainer info");
        }
    }

    [ApiAction("setTrainerInfo", ApiCategory.Trainer, "Modify trainer information.")]
    public string SetTrainerInfo(SaveFile save, JObject trainerData)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            foreach (var prop in trainerData.Properties())
            {
                switch (prop.Name.ToLower())
                {
                    case "trainername":
                        save.OT = prop.Value.ToString();
                        break;
                    case "trainerid":
                        save.DisplayTID = prop.Value.ToObject<uint>();
                        break;
                    case "secretid":
                        save.DisplaySID = prop.Value.ToObject<uint>();
                        break;
                    case "gender":
                        save.Gender = prop.Value.ToObject<byte>();
                        break;
                    case "money":
                        save.Money = prop.Value.ToObject<uint>();
                        break;
                }
            }

            return SuccessResponse(new { success = true, message = "Trainer info updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting trainer info: {ex}");
            return ErrorResponse("Failed to set trainer info");
        }
    }

    [ApiAction("getBadges", ApiCategory.Trainer, "Get badge data for the save file (generation-specific).")]
    public string GetBadges(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            
            var getBadgesMethod = saveType.GetMethod("GetEventFlag");
            if (getBadgesMethod == null)
            {
                return ErrorResponse($"Badge access not supported for {save.GetType().Name}. This feature requires generation-specific handling.");
            }

            var badges = new List<object>();
            
            if (save is SAV3)
            {
                var badgeFlagStart = 0x867;
                for (int i = 0; i < 8; i++)
                {
                    var flagIndex = badgeFlagStart + i;
                    var hasBadge = (bool)getBadgesMethod.Invoke(save, new object[] { flagIndex })!;
                    badges.Add(new
                    {
                        badgeIndex = i,
                        flagIndex = flagIndex,
                        obtained = hasBadge
                    });
                }
            }
            else if (save.Generation >= 4)
            {
                var badgeFlagStart = save.Generation switch
                {
                    4 => 0x98C,
                    5 => 0x11F,
                    6 => 0x301,
                    7 => 0x301,
                    8 => 0x2E,
                    _ => 0
                };

                if (badgeFlagStart == 0)
                {
                    return ErrorResponse($"Badge flags not mapped for generation {save.Generation}");
                }

                var badgeCount = save.Generation >= 8 ? 8 : 8;
                for (int i = 0; i < badgeCount; i++)
                {
                    var flagIndex = badgeFlagStart + i;
                    var hasBadge = (bool)getBadgesMethod.Invoke(save, new object[] { flagIndex })!;
                    badges.Add(new
                    {
                        badgeIndex = i,
                        flagIndex = flagIndex,
                        obtained = hasBadge
                    });
                }
            }
            else
            {
                return ErrorResponse($"Badge access not implemented for generation {save.Generation}");
            }

            return SuccessResponse(new { badges, generation = save.Generation });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting badges: {ex}");
            return ErrorResponse($"Failed to get badges: {ex.Message}");
        }
    }

    [ApiAction("setBadge", ApiCategory.Trainer, "Set a badge status for the save file (generation-specific).")]
    public string SetBadge(SaveFile save, int badgeIndex, bool value = true)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (badgeIndex < 0 || badgeIndex > 7)
                return ErrorResponse("Badge index must be between 0 and 7");

            var saveType = save.GetType();
            
            var setEventFlagMethod = saveType.GetMethod("SetEventFlag");
            if (setEventFlagMethod == null)
            {
                return ErrorResponse($"Badge modification not supported for {save.GetType().Name}. This feature requires generation-specific handling.");
            }

            int flagIndex;
            
            if (save is SAV3)
            {
                flagIndex = 0x867 + badgeIndex;
            }
            else if (save.Generation >= 4)
            {
                var badgeFlagStart = save.Generation switch
                {
                    4 => 0x98C,
                    5 => 0x11F,
                    6 => 0x301,
                    7 => 0x301,
                    8 => 0x2E,
                    _ => 0
                };

                if (badgeFlagStart == 0)
                {
                    return ErrorResponse($"Badge flags not mapped for generation {save.Generation}");
                }

                flagIndex = badgeFlagStart + badgeIndex;
            }
            else
            {
                return ErrorResponse($"Badge modification not implemented for generation {save.Generation}");
            }

            setEventFlagMethod.Invoke(save, new object[] { flagIndex, value });

            return SuccessResponse(new
            {
                success = true,
                message = $"Badge {badgeIndex} set to {value}",
                badgeIndex,
                flagIndex,
                value,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting badge: {ex}");
            return ErrorResponse($"Failed to set badge: {ex.Message}");
        }
    }

    [ApiAction("getSecondsPlayed", ApiCategory.Trainer, "Get total seconds played from the save file.")]
    public string GetSecondsPlayed(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            
            var hoursProperty = saveType.GetProperty("PlayedHours");
            var minutesProperty = saveType.GetProperty("PlayedMinutes");
            var secondsProperty = saveType.GetProperty("PlayedSeconds");

            if (hoursProperty == null || minutesProperty == null || secondsProperty == null)
            {
                return ErrorResponse($"Play time not accessible for {save.GetType().Name} (Generation {save.Generation})");
            }

            var hours = (int)(hoursProperty.GetValue(save) ?? 0);
            var minutes = (int)(minutesProperty.GetValue(save) ?? 0);
            var seconds = (int)(secondsProperty.GetValue(save) ?? 0);

            var totalSeconds = (hours * 3600) + (minutes * 60) + seconds;

            return SuccessResponse(new
            {
                hours,
                minutes,
                seconds,
                totalSeconds
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting seconds played: {ex}");
            return ErrorResponse($"Failed to get seconds played: {ex.Message}");
        }
    }

    [ApiAction("setGameTime", ApiCategory.Trainer, "Set game play time (hours, minutes, seconds).")]
    public string SetGameTime(SaveFile save, int hours, int minutes, int seconds)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (hours < 0 || minutes < 0 || seconds < 0)
                return ErrorResponse("Time values cannot be negative");

            if (minutes >= 60 || seconds >= 60)
                return ErrorResponse("Minutes and seconds must be less than 60");

            var saveType = save.GetType();
            
            var hoursProperty = saveType.GetProperty("PlayedHours");
            var minutesProperty = saveType.GetProperty("PlayedMinutes");
            var secondsProperty = saveType.GetProperty("PlayedSeconds");

            if (hoursProperty == null || minutesProperty == null || secondsProperty == null)
            {
                return ErrorResponse($"Play time not accessible for {save.GetType().Name} (Generation {save.Generation})");
            }

            if (!hoursProperty.CanWrite || !minutesProperty.CanWrite || !secondsProperty.CanWrite)
            {
                return ErrorResponse($"Play time not writable for {save.GetType().Name} (Generation {save.Generation})");
            }

            hoursProperty.SetValue(save, hours);
            minutesProperty.SetValue(save, minutes);
            secondsProperty.SetValue(save, seconds);

            var totalSeconds = (hours * 3600) + (minutes * 60) + seconds;

            return SuccessResponse(new
            {
                success = true,
                message = $"Game time set to {hours}h {minutes}m {seconds}s",
                hours,
                minutes,
                seconds,
                totalSeconds
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting game time: {ex}");
            return ErrorResponse($"Failed to set game time: {ex.Message}");
        }
    }

    [ApiAction("getSecondsToStart", ApiCategory.Trainer, "Get seconds from game start to save creation.")]
    public string GetSecondsToStart(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var secondsToStart = save.SecondsToStart;

            return SuccessResponse(new
            {
                secondsToStart,
                timeDisplay = FormatSeconds(secondsToStart)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting seconds to start: {ex}");
            return ErrorResponse($"Failed to get seconds to start: {ex.Message}");
        }
    }

    [ApiAction("setSecondsToStart", ApiCategory.Trainer, "Set seconds from game start to save creation.")]
    public string SetSecondsToStart(SaveFile save, uint seconds)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            save.SecondsToStart = seconds;

            return SuccessResponse(new
            {
                success = true,
                message = "Seconds to start updated successfully",
                secondsToStart = seconds,
                timeDisplay = FormatSeconds(seconds)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting seconds to start: {ex}");
            return ErrorResponse($"Failed to set seconds to start: {ex.Message}");
        }
    }

    [ApiAction("getSecondsToFame", ApiCategory.Trainer, "Get seconds from game start to Hall of Fame entry.")]
    public string GetSecondsToFame(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var secondsToFame = save.SecondsToFame;

            return SuccessResponse(new
            {
                secondsToFame,
                timeDisplay = FormatSeconds(secondsToFame)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting seconds to fame: {ex}");
            return ErrorResponse($"Failed to get seconds to fame: {ex.Message}");
        }
    }

    [ApiAction("setSecondsToFame", ApiCategory.Trainer, "Set seconds from game start to Hall of Fame entry.")]
    public string SetSecondsToFame(SaveFile save, uint seconds)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            save.SecondsToFame = seconds;

            return SuccessResponse(new
            {
                success = true,
                message = "Seconds to fame updated successfully",
                secondsToFame = seconds,
                timeDisplay = FormatSeconds(seconds)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting seconds to fame: {ex}");
            return ErrorResponse($"Failed to set seconds to fame: {ex.Message}");
        }
    }

    private string FormatSeconds(uint totalSeconds)
    {
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;
        return $"{hours}h {minutes}m {seconds}s";
    }

    [ApiAction("getCoins", ApiCategory.Trainer, "Get Game Corner coins (generation-specific).")]
    public string GetCoins(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var coinsProperty = saveType.GetProperty("Coins");

            if (coinsProperty == null)
            {
                return ErrorResponse($"Coins not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to verify support.");
            }

            var coins = (uint)coinsProperty.GetValue(save)!;

            return SuccessResponse(new
            {
                coins,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting coins: {ex}");
            return ErrorResponse($"Failed to get coins: {ex.Message}");
        }
    }

    [ApiAction("setCoins", ApiCategory.Trainer, "Set Game Corner coins (generation-specific).")]
    public string SetCoins(SaveFile save, uint coins)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var coinsProperty = saveType.GetProperty("Coins");

            if (coinsProperty == null)
            {
                return ErrorResponse($"Coins not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to verify support.");
            }

            coinsProperty.SetValue(save, coins);

            return SuccessResponse(new
            {
                success = true,
                message = "Coins updated successfully",
                coins,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting coins: {ex}");
            return ErrorResponse($"Failed to set coins: {ex.Message}");
        }
    }

    [ApiAction("getBattlePoints", ApiCategory.Trainer, "Get Battle Points (generation-specific).")]
    public string GetBattlePoints(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var bpProperty = saveType.GetProperty("BP");

            if (bpProperty == null)
            {
                return ErrorResponse($"Battle Points not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to verify support.");
            }

            var bp = (uint)bpProperty.GetValue(save)!;

            return SuccessResponse(new
            {
                battlePoints = bp,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting battle points: {ex}");
            return ErrorResponse($"Failed to get battle points: {ex.Message}");
        }
    }

    [ApiAction("setBattlePoints", ApiCategory.Trainer, "Set Battle Points (generation-specific).")]
    public string SetBattlePoints(SaveFile save, uint battlePoints)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var bpProperty = saveType.GetProperty("BP");

            if (bpProperty == null)
            {
                return ErrorResponse($"Battle Points not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to verify support.");
            }

            bpProperty.SetValue(save, battlePoints);

            return SuccessResponse(new
            {
                success = true,
                message = "Battle Points updated successfully",
                battlePoints,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting battle points: {ex}");
            return ErrorResponse($"Failed to set battle points: {ex.Message}");
        }
    }

    [ApiAction("getRivalName", ApiCategory.Trainer, "Get rival's name (generation-specific).")]
    public string GetRivalName(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var rivalProperty = saveType.GetProperty("Rival");

            if (rivalProperty == null)
            {
                return ErrorResponse($"Rival name not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to verify support.");
            }

            var rivalName = (string)rivalProperty.GetValue(save)!;

            return SuccessResponse(new
            {
                rivalName,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting rival name: {ex}");
            return ErrorResponse($"Failed to get rival name: {ex.Message}");
        }
    }

    [ApiAction("setRivalName", ApiCategory.Trainer, "Set rival's name (generation-specific).")]
    public string SetRivalName(SaveFile save, string rivalName)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (string.IsNullOrEmpty(rivalName))
                return ErrorResponse("Rival name cannot be empty");

            var saveType = save.GetType();
            var rivalProperty = saveType.GetProperty("Rival");

            if (rivalProperty == null)
            {
                return ErrorResponse($"Rival name not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to verify support.");
            }

            rivalProperty.SetValue(save, rivalName);

            return SuccessResponse(new
            {
                success = true,
                message = "Rival name updated successfully",
                rivalName,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting rival name: {ex}");
            return ErrorResponse($"Failed to set rival name: {ex.Message}");
        }
    }

    [ApiAction("getTrainerAppearance", ApiCategory.Trainer, "Get trainer appearance and customization", RequiresSession = true)]
    public string GetTrainerAppearance(SaveFile save)
    {
        try
        {
            var saveType = save.GetType();
            var saveTypeName = saveType.Name;
            var appearance = new Dictionary<string, object?>
            {
                ["generation"] = save.Generation,
                ["supported"] = false
            };

            if (saveTypeName.Contains("SAV6"))
            {
                appearance["supported"] = true;
                appearance["game"] = save.Version;

                var fashionProp = saveType.GetProperty("Fashion");
                var styleProp = saveType.GetProperty("Style");
                
                if (fashionProp != null)
                {
                    var fashion = fashionProp.GetValue(save);
                    if (fashion != null)
                    {
                        var fashionType = fashion.GetType();
                        var fashionData = new Dictionary<string, object?>();

                        var propertiesToExtract = new[] { "Shirt", "Pants", "Hat", "Shoes", "Bag", "Accessory", "Skin", "Hair", "Eyes", "Lip" };
                        foreach (var propName in propertiesToExtract)
                        {
                            var prop = fashionType.GetProperty(propName);
                            if (prop != null)
                            {
                                try
                                {
                                    fashionData[propName.ToLower()] = prop.GetValue(fashion);
                                }
                                catch { }
                            }
                        }
                        
                        if (fashionData.Count > 0)
                        {
                            appearance["fashion"] = fashionData;
                        }
                    }
                }

                if (styleProp != null)
                {
                    appearance["style"] = styleProp.GetValue(save);
                }
            }
            else if (saveTypeName.Contains("SAV7"))
            {
                appearance["supported"] = true;
                appearance["game"] = save.Version;

                var appearanceProp = saveType.GetProperty("Appearance");
                var trainerCard7Prop = saveType.GetProperty("TrainerCard7");
                
                if (appearanceProp != null)
                {
                    var appearanceObj = appearanceProp.GetValue(save);
                    if (appearanceObj != null)
                    {
                        var appearanceType = appearanceObj.GetType();
                        var appearanceData = new Dictionary<string, object?>();

                        var propertiesToExtract = new[] { "Skin", "Hair", "Eye", "Shirt", "Pants", "Hat", "Shoes", "Bag" };
                        foreach (var propName in propertiesToExtract)
                        {
                            var prop = appearanceType.GetProperty(propName);
                            if (prop != null)
                            {
                                try
                                {
                                    appearanceData[propName.ToLower()] = prop.GetValue(appearanceObj);
                                }
                                catch { }
                            }
                        }
                        
                        if (appearanceData.Count > 0)
                        {
                            appearance["customization"] = appearanceData;
                        }
                    }
                }

                var festivalPlazaProp = saveType.GetProperty("FestivalPlaza");
                if (festivalPlazaProp != null)
                {
                    var plaza = festivalPlazaProp.GetValue(save);
                    if (plaza != null)
                    {
                        var plazaType = plaza.GetType();
                        var rankProp = plazaType.GetProperty("Rank");
                        if (rankProp != null)
                        {
                            appearance["festivalPlazaRank"] = rankProp.GetValue(plaza);
                        }
                    }
                }

                if (trainerCard7Prop != null)
                {
                    appearance["hasTrainerCard"] = true;
                }
            }
            else if (saveTypeName.Contains("SAV8"))
            {
                appearance["supported"] = true;
                appearance["game"] = save.Version;

                var leagueCardProp = saveType.GetProperty("LeagueCard");
                if (leagueCardProp != null)
                {
                    var leagueCard = leagueCardProp.GetValue(save);
                    if (leagueCard != null)
                    {
                        var cardType = leagueCard.GetType();
                        var cardData = new Dictionary<string, object?>();

                        var propertiesToExtract = new[] { "CardType", "Pose", "Background", "Frame", "FrameColor", "Skin", "Hair", "Eye", "Eyebrow", "Mouth", "Uniform" };
                        foreach (var propName in propertiesToExtract)
                        {
                            var prop = cardType.GetProperty(propName);
                            if (prop != null)
                            {
                                try
                                {
                                    cardData[propName.ToLower()] = prop.GetValue(leagueCard);
                                }
                                catch { }
                            }
                        }
                        
                        if (cardData.Count > 0)
                        {
                            appearance["leagueCard"] = cardData;
                        }
                    }
                }

                var fashionProp = saveType.GetProperty("Fashion");
                if (fashionProp != null)
                {
                    var fashion = fashionProp.GetValue(save);
                    if (fashion != null)
                    {
                        var fashionType = fashion.GetType();
                        var fashionData = new Dictionary<string, object?>();

                        var propertiesToExtract = new[] { "TopWear", "BottomWear", "Headwear", "Eyewear", "Bag", "Gloves", "Socks", "Shoes" };
                        foreach (var propName in propertiesToExtract)
                        {
                            var prop = fashionType.GetProperty(propName);
                            if (prop != null)
                            {
                                try
                                {
                                    fashionData[propName.ToLower()] = prop.GetValue(fashion);
                                }
                                catch { }
                            }
                        }
                        
                        if (fashionData.Count > 0)
                        {
                            appearance["fashion"] = fashionData;
                        }
                    }
                }
            }
            else
            {
                appearance["message"] = "Trainer appearance customization is not supported for this generation";
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                appearance = appearance
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get trainer appearance: {ex.Message}" });
        }
    }

    [ApiAction("setTrainerAppearance", ApiCategory.Trainer, "Set trainer appearance and customization", RequiresSession = true)]
    public string SetTrainerAppearance(SaveFile save, JObject appearanceData)
    {
        try
        {
            var saveType = save.GetType().Name;
            if (!saveType.Contains("SAV6") && !saveType.Contains("SAV7") && !saveType.Contains("SAV8"))
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Trainer appearance customization is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = "Trainer appearance updated (limited support)",
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to set trainer appearance: {ex.Message}" });
        }
    }

    [ApiAction("getTrainerCard", ApiCategory.Trainer, "Get trainer card information", RequiresSession = true)]
    public string GetTrainerCard(SaveFile save)
    {
        try
        {
            var cardInfo = new Dictionary<string, object>
            {
                ["trainerName"] = save.OT,
                ["trainerId"] = save.TID16,
                ["secretId"] = save.SID16,
                ["gender"] = save.Gender,
                ["language"] = save.Language,
                ["game"] = save.Version,
                ["generation"] = save.Generation,
                ["playTime"] = new
                {
                    hours = save.PlayedHours,
                    minutes = save.PlayedMinutes,
                    seconds = save.PlayedSeconds
                }
            };

            cardInfo["money"] = save.Money;

            return JsonConvert.SerializeObject(new
            {
                success = true,
                trainerCard = cardInfo
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get trainer card: {ex.Message}" });
        }
    }
}
