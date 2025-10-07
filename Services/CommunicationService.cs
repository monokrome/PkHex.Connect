using PKHeX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class CommunicationService : ServiceBase
{
    [ApiAction("getMailbox", ApiCategory.Communication, "Get all mail messages", RequiresSession = true)]
    public string GetMailbox(SaveFile save)
    {
        try
        {
            var saveType = save.GetType();
            var mailProp = saveType.GetProperty("Mail");
            
            if (mailProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail system is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var mailCountProp = saveType.GetProperty("MailCount");
            var getMailMethod = saveType.GetMethod("GetMail");
            
            if (mailCountProp == null || getMailMethod == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail access not available for this save type.",
                    message = "Mail system exists but has limited support in this version."
                });
            }

            var count = (int)mailCountProp.GetValue(save)!;
            var messages = new List<object>();
            
            for (int i = 0; i < count; i++)
            {
                var mail = getMailMethod.Invoke(save, new object[] { i });
                if (mail != null)
                {
                    var isEmptyProp = mail.GetType().GetProperty("IsEmpty");
                    var typeProp = mail.GetType().GetProperty("Type");
                    var authorProp = mail.GetType().GetProperty("AuthorName");
                    var getMessageMethod = mail.GetType().GetMethod("GetMessage");
                    
                    var isEmpty = isEmptyProp?.GetValue(mail) as bool? ?? true;
                    if (!isEmpty)
                    {
                        messages.Add(new
                        {
                            index = i,
                            isEmpty = isEmpty,
                            mailType = typeProp?.GetValue(mail) ?? 0,
                            authorName = authorProp?.GetValue(mail)?.ToString() ?? "",
                            message = getMessageMethod?.Invoke(mail, new object[] { false })?.ToString() ?? ""
                        });
                    }
                }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                mailCount = count,
                messages = messages
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get mailbox: {ex.Message}" });
        }
    }

    [ApiAction("getMailMessage", ApiCategory.Communication, "Get a specific mail message", RequiresSession = true)]
    public string GetMailMessage(SaveFile save, int index)
    {
        try
        {
            var saveType = save.GetType();
            var mailProp = saveType.GetProperty("Mail");
            
            if (mailProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail system is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var mailCountProp = saveType.GetProperty("MailCount");
            var getMailMethod = saveType.GetMethod("GetMail");
            
            if (mailCountProp == null || getMailMethod == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail access not available for this save type."
                });
            }

            var count = (int)mailCountProp.GetValue(save)!;
            
            if (index < 0 || index >= count)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = $"Invalid mail index. Must be between 0 and {count - 1}"
                });
            }

            var mail = getMailMethod.Invoke(save, new object[] { index });
            if (mail == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    isEmpty = true
                });
            }

            var isEmptyProp = mail.GetType().GetProperty("IsEmpty");
            var typeProp = mail.GetType().GetProperty("Type");
            var authorProp = mail.GetType().GetProperty("AuthorName");
            var getMessageMethod = mail.GetType().GetMethod("GetMessage");

            return JsonConvert.SerializeObject(new
            {
                success = true,
                isEmpty = isEmptyProp?.GetValue(mail) as bool? ?? true,
                mailType = typeProp?.GetValue(mail) ?? 0,
                authorName = authorProp?.GetValue(mail)?.ToString() ?? "",
                message = getMessageMethod?.Invoke(mail, new object[] { false })?.ToString() ?? "",
                index = index
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get mail message: {ex.Message}" });
        }
    }

    [ApiAction("setMailMessage", ApiCategory.Communication, "Set a mail message", RequiresSession = true)]
    public string SetMailMessage(SaveFile save, int index, JObject mailData)
    {
        try
        {
            var saveType = save.GetType();
            var mailProp = saveType.GetProperty("Mail");
            
            if (mailProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail system is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var mailCountProp = saveType.GetProperty("MailCount");
            var getMailMethod = saveType.GetMethod("GetMail");
            var setMailMethod = saveType.GetMethod("SetMail");
            
            if (mailCountProp == null || getMailMethod == null || setMailMethod == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail modification not available for this save type."
                });
            }

            var count = (int)mailCountProp.GetValue(save)!;
            
            if (index < 0 || index >= count)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = $"Invalid mail index. Must be between 0 and {count - 1}"
                });
            }

            var mail = getMailMethod.Invoke(save, new object[] { index });
            if (mail == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Failed to get mail at specified index"
                });
            }

            if (mailData["authorName"] != null)
            {
                var authorProp = mail.GetType().GetProperty("AuthorName");
                if (authorProp != null)
                    authorProp.SetValue(mail, mailData["authorName"]!.ToString());
            }
            
            setMailMethod.Invoke(save, new object[] { index, mail });

            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = $"Mail message {index} updated successfully",
                index = index
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to set mail message: {ex.Message}" });
        }
    }

    [ApiAction("deleteMail", ApiCategory.Communication, "Delete a mail message", RequiresSession = true)]
    public string DeleteMail(SaveFile save, int index)
    {
        try
        {
            var saveType = save.GetType();
            var mailProp = saveType.GetProperty("Mail");
            
            if (mailProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail system is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var mailCountProp = saveType.GetProperty("MailCount");
            var setMailMethod = saveType.GetMethod("SetMail");
            
            if (mailCountProp == null || setMailMethod == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Mail deletion not available for this save type."
                });
            }

            var count = (int)mailCountProp.GetValue(save)!;
            
            if (index < 0 || index >= count)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = $"Invalid mail index. Must be between 0 and {count - 1}"
                });
            }

            setMailMethod.Invoke(save, new object?[] { index, null });

            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = $"Mail message {index} deleted successfully",
                index = index
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to delete mail: {ex.Message}" });
        }
    }

    [ApiAction("getMysteryGifts", ApiCategory.Communication, "Get all mystery gift cards with full data")]
    public string GetMysteryGifts(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var mysteryGiftCardsProperty = saveType.GetProperty("MysteryGiftCards");
            
            if (mysteryGiftCardsProperty == null)
            {
                return ErrorResponse($"Mystery Gifts not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to see available capabilities.");
            }

            var cardsArray = mysteryGiftCardsProperty.GetValue(save) as Array;
            if (cardsArray == null)
            {
                return ErrorResponse("Mystery Gift cards array is null");
            }

            var allCards = new List<object>();
            
            for (int i = 0; i < cardsArray.Length; i++)
            {
                var card = cardsArray.GetValue(i);
                if (card == null)
                {
                    allCards.Add(new { index = i, isEmpty = true });
                    continue;
                }

                var cardType = card.GetType();
                
                var isEmptyProp = cardType.GetProperty("IsEmpty");
                if (isEmptyProp != null)
                {
                    try
                    {
                        var isEmpty = isEmptyProp.GetValue(card);
                        if (isEmpty is bool emptyBool && emptyBool)
                        {
                            allCards.Add(new { index = i, isEmpty = true });
                            continue;
                        }
                    }
                    catch { }
                }

                var cardData = new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["type"] = cardType.Name
                };

                var propertiesToExtract = new[]
                {
                    "Species", "Level", "HeldItem", "Move1", "Move2", "Move3", "Move4",
                    "IsEgg", "IsShiny", "Ball", "Nature", "Gender", "Ability", 
                    "IV_HP", "IV_ATK", "IV_DEF", "IV_SPA", "IV_SPD", "IV_SPE",
                    "EV_HP", "EV_ATK", "EV_DEF", "EV_SPA", "EV_SPD", "EV_SPE",
                    "OT_Name", "TID", "SID", "Language", "Location", "MetLevel",
                    "RibbonClassic", "RibbonWishing", "RibbonPremier", "RibbonEvent",
                    "CardID", "CardTitle", "IsItem", "ItemID", "Quantity",
                    "IsEntity", "IsBP", "BP", "RelearnMove1", "RelearnMove2", "RelearnMove3", "RelearnMove4",
                    "Form", "OT_Gender", "IsNicknamed", "Nickname", "Version", "PID", "EncryptionConstant",
                    "MetLocation", "EggLocation", "FatefulEncounter"
                };

                foreach (var propName in propertiesToExtract)
                {
                    var prop = cardType.GetProperty(propName);
                    if (prop != null)
                    {
                        try
                        {
                            var value = prop.GetValue(card);
                            if (value != null)
                            {
                                cardData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                allCards.Add(cardData);
            }

            return SuccessResponse(new
            {
                generation = save.Generation,
                totalSlots = cardsArray.Length,
                cards = allCards
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting mystery gifts: {ex}");
            return ErrorResponse($"Failed to get mystery gifts: {ex.Message}");
        }
    }

    [ApiAction("getMysteryGiftFlags", ApiCategory.Communication, "Get mystery gift received flags (generation-specific).")]
    public string GetMysteryGiftFlags(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            
            var getMysteryGiftReceivedFlagsMethod = saveType.GetMethod("GetMysteryGiftReceivedFlags");
            
            if (getMysteryGiftReceivedFlagsMethod == null)
            {
                return ErrorResponse($"Mystery Gift flags not supported for {save.GetType().Name} (Generation {save.Generation}). This feature requires generation-specific implementation.");
            }

            var flags = getMysteryGiftReceivedFlagsMethod.Invoke(save, null);
            
            if (flags is bool[] boolArray)
            {
                var flagList = new List<object>();
                for (int i = 0; i < boolArray.Length; i++)
                {
                    if (boolArray[i])
                    {
                        flagList.Add(new { index = i, received = true });
                    }
                }

                return SuccessResponse(new
                {
                    supported = true,
                    generation = save.Generation,
                    totalFlags = boolArray.Length,
                    receivedCount = flagList.Count,
                    receivedFlags = flagList
                });
            }

            return SuccessResponse(new
            {
                supported = true,
                generation = save.Generation,
                message = "Mystery Gift flags method exists but format unknown"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting mystery gift flags: {ex}");
            return ErrorResponse($"Failed to get mystery gift flags: {ex.Message}");
        }
    }

    [ApiAction("getMysteryGiftCard", ApiCategory.Communication, "Get detailed mystery gift card data")]
    public string GetMysteryGiftCard(SaveFile save, int index)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var mysteryGiftCardsProperty = saveType.GetProperty("MysteryGiftCards");
            
            if (mysteryGiftCardsProperty == null)
            {
                return ErrorResponse($"Mystery Gift cards not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to see available capabilities.");
            }

            var cards = mysteryGiftCardsProperty.GetValue(save) as Array;
            if (cards == null || index < 0 || index >= cards.Length)
            {
                return ErrorResponse($"Invalid card index. Must be between 0 and {(cards?.Length ?? 0) - 1}");
            }

            var card = cards.GetValue(index);
            if (card == null)
            {
                return SuccessResponse(new { isEmpty = true, index = index });
            }

            var cardType = card.GetType();
            var cardData = new Dictionary<string, object>
            {
                ["index"] = index,
                ["type"] = cardType.Name
            };

            var propertiesToExtract = new[]
            {
                "Species", "Level", "HeldItem", "Move1", "Move2", "Move3", "Move4",
                "IsEgg", "IsShiny", "Ball", "Nature", "Gender", "Ability", 
                "IV_HP", "IV_ATK", "IV_DEF", "IV_SPA", "IV_SPD", "IV_SPE",
                "EV_HP", "EV_ATK", "EV_DEF", "EV_SPA", "EV_SPD", "EV_SPE",
                "OT_Name", "TID", "SID", "Language", "Location", "MetLevel",
                "RibbonClassic", "RibbonWishing", "RibbonPremier", "RibbonEvent",
                "CardID", "CardTitle", "IsItem", "ItemID", "Quantity",
                "IsEntity", "IsBP", "BP", "RelearnMove1", "RelearnMove2", "RelearnMove3", "RelearnMove4",
                "CanBeReceivedByVersion", "Form", "OT_Gender", "IsNicknamed", "Nickname",
                "Version", "Date", "PID", "EncryptionConstant", "IsDateNotSet",
                "MetLocation", "EggLocation", "CNT_Cool", "CNT_Beauty", "CNT_Cute", "CNT_Smart", "CNT_Tough", "RibbonCountMemoryContest", "RibbonCountMemoryBattle",
                "FatefulEncounter", "EggMetDate", "MetDate"
            };

            foreach (var propName in propertiesToExtract)
            {
                var prop = cardType.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(card);
                        if (value != null)
                        {
                            cardData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            var isEmptyProp = cardType.GetProperty("IsEmpty");
            if (isEmptyProp != null)
            {
                try
                {
                    var isEmpty = isEmptyProp.GetValue(card);
                    if (isEmpty is bool emptyBool && emptyBool)
                    {
                        return SuccessResponse(new { isEmpty = true, index = index });
                    }
                }
                catch { }
            }

            return SuccessResponse(cardData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting mystery gift card: {ex}");
            return ErrorResponse($"Failed to get mystery gift card: {ex.Message}");
        }
    }

    [ApiAction("setMysteryGiftCard", ApiCategory.Communication, "Set mystery gift card data")]
    public string SetMysteryGiftCard(SaveFile save, int index, JObject cardData)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var mysteryGiftCardsProperty = saveType.GetProperty("MysteryGiftCards");
            
            if (mysteryGiftCardsProperty == null)
            {
                return ErrorResponse($"Mystery Gift cards not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to see available capabilities.");
            }

            var cards = mysteryGiftCardsProperty.GetValue(save) as Array;
            if (cards == null || index < 0 || index >= cards.Length)
            {
                return ErrorResponse($"Invalid card index. Must be between 0 and {(cards?.Length ?? 0) - 1}");
            }

            var card = cards.GetValue(index);
            if (card == null)
            {
                return ErrorResponse("Card slot is null and cannot be modified");
            }

            var cardType = card.GetType();
            var modifiedProperties = new List<string>();

            foreach (var prop in cardData.Properties())
            {
                var propName = char.ToUpperInvariant(prop.Name[0]) + prop.Name.Substring(1);
                var cardProp = cardType.GetProperty(propName);
                
                if (cardProp != null && cardProp.CanWrite)
                {
                    try
                    {
                        var value = prop.Value.ToObject(cardProp.PropertyType);
                        cardProp.SetValue(card, value);
                        modifiedProperties.Add(propName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not set property {propName}: {ex.Message}");
                    }
                }
            }

            cards.SetValue(card, index);
            
            mysteryGiftCardsProperty.SetValue(save, cards);

            return SuccessResponse(new
            {
                message = "Mystery Gift card modified successfully",
                index = index,
                modifiedProperties = modifiedProperties
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting mystery gift card: {ex}");
            return ErrorResponse($"Failed to set mystery gift card: {ex.Message}");
        }
    }

    [ApiAction("deleteMysteryGift", ApiCategory.Communication, "Delete a mystery gift card")]
    public string DeleteMysteryGift(SaveFile save, int index)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var mysteryGiftCardsProperty = saveType.GetProperty("MysteryGiftCards");
            
            if (mysteryGiftCardsProperty == null)
            {
                return ErrorResponse($"Mystery Gift cards not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave to see available capabilities.");
            }

            var cards = mysteryGiftCardsProperty.GetValue(save) as Array;
            if (cards == null || index < 0 || index >= cards.Length)
            {
                return ErrorResponse($"Invalid card index. Must be between 0 and {(cards?.Length ?? 0) - 1}");
            }

            var card = cards.GetValue(index);
            if (card == null)
            {
                return SuccessResponse(new
                {
                    message = "Card slot was already empty",
                    index = index
                });
            }

            var cardType = card.GetType();
            var clearMethod = cardType.GetMethod("Clear");
            
            if (clearMethod != null)
            {
                clearMethod.Invoke(card, null);
                cards.SetValue(card, index);
                mysteryGiftCardsProperty.SetValue(save, cards);
                
                return SuccessResponse(new
                {
                    message = "Mystery Gift card cleared successfully",
                    index = index,
                    method = "Clear"
                });
            }

            var constructors = cardType.GetConstructors();
            var defaultConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            
            if (defaultConstructor != null)
            {
                var emptyCard = defaultConstructor.Invoke(null);
                cards.SetValue(emptyCard, index);
                mysteryGiftCardsProperty.SetValue(save, cards);
                
                return SuccessResponse(new
                {
                    message = "Mystery Gift card replaced with empty card",
                    index = index,
                    method = "Constructor"
                });
            }

            var propertiesToClear = new[] { "Species", "Level", "HeldItem", "Move1", "Move2", "Move3", "Move4" };
            var clearedCount = 0;
            
            foreach (var propName in propertiesToClear)
            {
                var prop = cardType.GetProperty(propName);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(ushort) || prop.PropertyType == typeof(byte))
                        {
                            prop.SetValue(card, 0);
                            clearedCount++;
                        }
                    }
                    catch { }
                }
            }
            
            if (clearedCount > 0)
            {
                cards.SetValue(card, index);
                mysteryGiftCardsProperty.SetValue(save, cards);
                
                return SuccessResponse(new
                {
                    message = $"Mystery Gift card partially cleared ({clearedCount} properties reset)",
                    index = index,
                    method = "PropertyReset"
                });
            }

            return ErrorResponse("Unable to delete mystery gift card. No suitable method found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting mystery gift: {ex}");
            return ErrorResponse($"Failed to delete mystery gift: {ex.Message}");
        }
    }
}
