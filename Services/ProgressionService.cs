using PKHeX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class ProgressionService : ServiceBase
{
    [ApiAction("getEventFlag", ApiCategory.Progress, "Get an event flag value by index.")]
    public string GetEventFlag(SaveFile save, int flagIndex)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (flagIndex < 0)
                return ErrorResponse("Flag index must be non-negative");

            var saveType = save.GetType();
            var getEventFlagMethod = saveType.GetMethod("GetEventFlag");
            
            if (getEventFlagMethod == null)
            {
                return ErrorResponse($"Event flags not supported for {save.GetType().Name}. This feature requires generation-specific handling.");
            }

            var value = (bool)getEventFlagMethod.Invoke(save, new object[] { flagIndex })!;

            return SuccessResponse(new
            {
                flagIndex,
                value,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event flag: {ex}");
            return ErrorResponse($"Failed to get event flag: {ex.Message}");
        }
    }

    [ApiAction("setEventFlag", ApiCategory.Progress, "Set an event flag value by index.")]
    public string SetEventFlag(SaveFile save, int flagIndex, bool value)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (flagIndex < 0)
                return ErrorResponse("Flag index must be non-negative");

            var saveType = save.GetType();
            var setEventFlagMethod = saveType.GetMethod("SetEventFlag");
            
            if (setEventFlagMethod == null)
            {
                return ErrorResponse($"Event flags not supported for {save.GetType().Name}. This feature requires generation-specific handling.");
            }

            setEventFlagMethod.Invoke(save, new object[] { flagIndex, value });

            return SuccessResponse(new
            {
                success = true,
                message = $"Event flag {flagIndex} set to {value}",
                flagIndex,
                value,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting event flag: {ex}");
            return ErrorResponse($"Failed to set event flag: {ex.Message}");
        }
    }

    [ApiAction("getEventConst", ApiCategory.Progress, "Get an event const/work value by index.")]
    public string GetEventConst(SaveFile save, int constIndex)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (constIndex < 0)
                return ErrorResponse("Const index must be non-negative");

            var saveType = save.GetType();
            var getWorkMethod = saveType.GetMethod("GetWork");
            
            if (getWorkMethod == null)
            {
                return ErrorResponse($"Event constants/work not supported for {save.GetType().Name}. This feature requires generation-specific handling.");
            }

            var constValue = getWorkMethod.Invoke(save, new object[] { constIndex });
            int value = Convert.ToInt32(constValue);

            return SuccessResponse(new
            {
                constIndex,
                value,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event const: {ex}");
            return ErrorResponse($"Failed to get event const: {ex.Message}");
        }
    }

    [ApiAction("setEventConst", ApiCategory.Progress, "Set an event const/work value by index.")]
    public string SetEventConst(SaveFile save, int constIndex, int value)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (constIndex < 0)
                return ErrorResponse("Const index must be non-negative");

            var saveType = save.GetType();
            var setWorkMethod = saveType.GetMethod("SetWork");
            
            if (setWorkMethod == null)
            {
                return ErrorResponse($"Event constants/work not supported for {save.GetType().Name}. This feature requires generation-specific handling.");
            }

            var parameters = setWorkMethod.GetParameters();
            if (parameters.Length >= 2)
            {
                var valueParam = parameters[1];
                object convertedValue;
                
                if (valueParam.ParameterType == typeof(ushort) || valueParam.ParameterType == typeof(UInt16))
                {
                    convertedValue = Convert.ToUInt16(value);
                }
                else
                {
                    convertedValue = value;
                }
                
                setWorkMethod.Invoke(save, new object[] { constIndex, convertedValue });
            }

            return SuccessResponse(new
            {
                success = true,
                message = $"Event const {constIndex} set to {value}",
                constIndex,
                value,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting event const: {ex}");
            return ErrorResponse($"Failed to set event const: {ex.Message}");
        }
    }

    [ApiAction("getHallOfFame", ApiCategory.Progress, "Get Hall of Fame entries", RequiresSession = true)]
    public string GetHallOfFame(SaveFile save)
    {
        try
        {
            var saveType = save.GetType();
            var hofDataProp = saveType.GetProperty("HallOfFameData");
            
            if (hofDataProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Hall of Fame is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var hofCountProp = saveType.GetProperty("HallOfFameCount");
            var getEntryMethod = saveType.GetMethod("GetHallOfFameEntry");
            
            if (hofCountProp == null || getEntryMethod == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Hall of Fame access not available for this save type."
                });
            }

            var count = (int)hofCountProp.GetValue(save)!;
            var entries = new List<object>();
            
            for (int i = 0; i < count; i++)
            {
                var entry = getEntryMethod.Invoke(save, new object[] { i }) as IList<PKM>;
                if (entry == null || entry.Count == 0)
                    continue;

                var team = new List<object>();
                foreach (var pk in entry)
                {
                    if (pk.Species == 0)
                        continue;

                    team.Add(new
                    {
                        species = pk.Species,
                        nickname = pk.Nickname,
                        level = pk.CurrentLevel,
                        isShiny = pk.IsShiny,
                        gender = pk.Gender,
                        nature = (byte)pk.Nature,
                        ability = pk.Ability,
                        pid = pk.PID
                    });
                }

                if (team.Count > 0)
                {
                    entries.Add(new
                    {
                        index = i,
                        team = team
                    });
                }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                entryCount = count,
                entries = entries
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get Hall of Fame: {ex.Message}" });
        }
    }

    [ApiAction("setHallOfFameEntry", ApiCategory.Progress, "Set a Hall of Fame entry", RequiresSession = true)]
    public string SetHallOfFameEntry(SaveFile save, int index, JArray team)
    {
        try
        {
            var saveType = save.GetType();
            var hofDataProp = saveType.GetProperty("HallOfFameData");
            
            if (hofDataProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Hall of Fame is not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var hofCountProp = saveType.GetProperty("HallOfFameCount");
            var setEntryMethod = saveType.GetMethod("SetHallOfFameEntry");
            
            if (hofCountProp == null || setEntryMethod == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Hall of Fame modification not available for this save type."
                });
            }

            var count = (int)hofCountProp.GetValue(save)!;
            
            if (index < 0 || index >= count)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = $"Invalid Hall of Fame index. Must be between 0 and {count - 1}"
                });
            }

            var pkList = new List<PKM>();
            foreach (var pkData in team)
            {
                var pk = save.BlankPKM;
                
                if (pkData["species"] != null)
                    pk.Species = pkData["species"]!.ToObject<ushort>();
                if (pkData["level"] != null)
                    pk.CurrentLevel = (byte)pkData["level"]!.ToObject<int>();
                if (pkData["nickname"] != null)
                    pk.Nickname = pkData["nickname"]!.ToString();
                if (pkData["gender"] != null)
                    pk.Gender = (byte)pkData["gender"]!.ToObject<int>();
                if (pkData["nature"] != null)
                    pk.Nature = (Nature)pkData["nature"]!.ToObject<byte>();
                if (pkData["pid"] != null)
                    pk.PID = pkData["pid"]!.ToObject<uint>();

                pkList.Add(pk);
            }

            setEntryMethod.Invoke(save, new object[] { index, pkList });

            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = $"Hall of Fame entry {index} updated successfully",
                index = index,
                teamSize = pkList.Count
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to set Hall of Fame entry: {ex.Message}" });
        }
    }

    [ApiAction("getBattleFacilityStats", ApiCategory.Progress, "Get Battle Facility statistics", RequiresSession = true)]
    public string GetBattleFacilityStats(SaveFile save)
    {
        try
        {
            if (save is not ITrainerStatRecord recordSave)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Battle Facility stats are not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var stats = new List<object>();
            for (int i = 0; i < recordSave.RecordCount; i++)
            {
                var value = recordSave.GetRecord(i);
                
                stats.Add(new
                {
                    index = i,
                    value = value
                });
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                battleFacilityStats = stats,
                count = stats.Count
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get battle facility stats: {ex.Message}" });
        }
    }

    [ApiAction("getRecords", ApiCategory.Trainer, "Get trainer statistics and records (generation-specific via ITrainerStatRecord).")]
    public string GetRecords(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (save is not ITrainerStatRecord recordSave)
            {
                return ErrorResponse($"Trainer records not supported for {save.GetType().Name} (Generation {save.Generation})");
            }

            var records = new List<object>();
            var count = recordSave.RecordCount;

            for (int i = 0; i < count; i++)
            {
                var value = recordSave.GetRecord(i);
                var max = recordSave.GetRecordMax(i);
                
                records.Add(new
                {
                    index = i,
                    value,
                    max
                });
            }

            return SuccessResponse(new
            {
                recordCount = count,
                records
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting records: {ex}");
            return ErrorResponse($"Failed to get records: {ex.Message}");
        }
    }

    [ApiAction("setRecord", ApiCategory.Trainer, "Set a specific trainer record value (generation-specific via ITrainerStatRecord).")]
    public string SetRecord(SaveFile save, int index, int value)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (save is not ITrainerStatRecord recordSave)
            {
                return ErrorResponse($"Trainer records not supported for {save.GetType().Name} (Generation {save.Generation})");
            }

            if (index < 0 || index >= recordSave.RecordCount)
            {
                return ErrorResponse($"Record index {index} out of range (0-{recordSave.RecordCount - 1})");
            }

            var max = recordSave.GetRecordMax(index);
            if (value > max)
            {
                return ErrorResponse($"Value {value} exceeds maximum {max} for record index {index}");
            }

            recordSave.SetRecord(index, value);

            return SuccessResponse(new
            {
                success = true,
                message = $"Record {index} set to {value}",
                index,
                value,
                max
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting record: {ex}");
            return ErrorResponse($"Failed to set record: {ex.Message}");
        }
    }

    [ApiAction("getRecordValue", ApiCategory.Trainer, "Get a specific trainer record value (generation-specific via ITrainerStatRecord).")]
    public string GetRecordValue(SaveFile save, int index)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (save is not ITrainerStatRecord recordSave)
            {
                return ErrorResponse($"Trainer records not supported for {save.GetType().Name} (Generation {save.Generation})");
            }

            if (index < 0 || index >= recordSave.RecordCount)
            {
                return ErrorResponse($"Record index {index} out of range (0-{recordSave.RecordCount - 1})");
            }

            var value = recordSave.GetRecord(index);
            var max = recordSave.GetRecordMax(index);

            return SuccessResponse(new
            {
                index,
                value,
                max
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting record value: {ex}");
            return ErrorResponse($"Failed to get record value: {ex.Message}");
        }
    }
}
