using PKHeX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class PokemonManagementService : ServiceBase
{
    private object FormatPokemonData(PKM pokemon)
    {
        return new
        {
            pid = pokemon.PID,
            species = pokemon.Species,
            nickname = pokemon.Nickname,
            level = pokemon.CurrentLevel,
            isEgg = pokemon.IsEgg,
            isShiny = pokemon.IsShiny,
            gender = pokemon.Gender,
            nature = pokemon.Nature,
            ability = pokemon.Ability,
            heldItem = pokemon.HeldItem,
            moves = new[] { pokemon.Move1, pokemon.Move2, pokemon.Move3, pokemon.Move4 },
            stats = new
            {
                hp = pokemon.Stat_HPCurrent,
                maxHp = pokemon.Stat_HPMax,
                attack = pokemon.Stat_ATK,
                defense = pokemon.Stat_DEF,
                spAttack = pokemon.Stat_SPA,
                spDefense = pokemon.Stat_SPD,
                speed = pokemon.Stat_SPE
            },
            ivs = new
            {
                hp = pokemon.IV_HP,
                attack = pokemon.IV_ATK,
                defense = pokemon.IV_DEF,
                spAttack = pokemon.IV_SPA,
                spDefense = pokemon.IV_SPD,
                speed = pokemon.IV_SPE
            },
            evs = new
            {
                hp = pokemon.EV_HP,
                attack = pokemon.EV_ATK,
                defense = pokemon.EV_DEF,
                spAttack = pokemon.EV_SPA,
                spDefense = pokemon.EV_SPD,
                speed = pokemon.EV_SPE
            },
            originalTrainer = pokemon.OriginalTrainerName,
            trainerID = pokemon.DisplayTID,
            secretID = pokemon.DisplaySID,
            ball = pokemon.Ball,
            metLevel = pokemon.MetLevel,
            metLocation = pokemon.MetLocation
        };
    }

    [ApiAction("getPokemon", ApiCategory.Pokemon, "Get detailed information about a specific Pokémon.")]
    public string GetPokemonData(SaveFile save, int box, int slot)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            var pokemon = GetPokemonAt(save, box, slot);

            if (pokemon == null || pokemon.Species == 0)
                return SuccessResponse(new { empty = true });

            return SuccessResponse(FormatPokemonData(pokemon));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting Pokemon data: {ex}");
            return ErrorResponse("Failed to get Pokemon data");
        }
    }

    [ApiAction("getAllPokemon", ApiCategory.Pokemon, "Get a list of all non-empty Pokémon in boxes.")]
    public string GetAllPokemon(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var allPokemon = new List<object>();

            for (int box = 0; box < save.BoxCount; box++)
            {
                for (int slot = 0; slot < save.BoxSlotCount; slot++)
                {
                    var pokemon = GetPokemonAt(save, box, slot);
                    if (pokemon != null && pokemon.Species != 0)
                    {
                        var pokemonData = FormatPokemonData(pokemon);
                        allPokemon.Add(new
                        {
                            box,
                            slot,
                            pokemon = pokemonData
                        });
                    }
                }
            }

            return SuccessResponse(new { pokemon = allPokemon, count = allPokemon.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all Pokemon: {ex}");
            return ErrorResponse("Failed to get all Pokemon");
        }
    }

    [ApiAction("exportShowdown", ApiCategory.Pokemon, "Export a Pokémon in Showdown format.")]
    public string ExportShowdown(SaveFile save, int box, int slot)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            var pokemon = GetPokemonAt(save, box, slot);

            if (pokemon == null || pokemon.Species == 0)
                return ErrorResponse("No Pokemon in this slot");

            var showdown = ShowdownParsing.GetShowdownText(pokemon);

            return SuccessResponse(new { success = true, showdown });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting to Showdown: {ex}");
            return ErrorResponse("Failed to export to Showdown format");
        }
    }

    [ApiAction("setPokemon", ApiCategory.Pokemon, "Replace a Pokémon with binary data.")]
    public string SetPokemonData(SaveFile save, int box, int slot, [ApiParameter(Format = "base64")] string base64PokemonData)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            if (string.IsNullOrEmpty(base64PokemonData))
                return ErrorResponse("Pokemon data is required");

            byte[] data;
            try
            {
                data = Convert.FromBase64String(base64PokemonData);
            }
            catch (FormatException)
            {
                return ErrorResponse("Invalid base64 Pokemon data");
            }

            var pokemon = save.GetBoxSlotAtIndex(box, slot);
            if (data.Length != pokemon.Data.Length)
                return ErrorResponse($"Pokemon data must be {pokemon.Data.Length} bytes");

            data.CopyTo(pokemon.Data);
            pokemon.RefreshChecksum();
            SetPokemonAt(save, box, slot, pokemon);

            return SuccessResponse(new { success = true, message = "Pokemon updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting Pokemon: {ex}");
            return ErrorResponse("Failed to set Pokemon");
        }
    }

    [ApiAction("modifyPokemon", ApiCategory.Pokemon, "Modify specific Pokémon properties.")]
    public string ModifyPokemon(SaveFile save, int box, int slot, JObject modifications)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            var pokemon = GetPokemonAt(save, box, slot);
            if (pokemon == null || pokemon.Species == 0)
                return ErrorResponse("No Pokemon in this slot");

            foreach (var prop in modifications.Properties())
            {
                switch (prop.Name.ToLower())
                {
                    case "species":
                        pokemon.Species = prop.Value.ToObject<ushort>();
                        break;
                    case "nickname":
                        pokemon.Nickname = prop.Value.ToString();
                        break;
                    case "level":
                        pokemon.CurrentLevel = (byte)prop.Value.ToObject<int>();
                        break;
                    case "nature":
                        pokemon.Nature = (Nature)prop.Value.ToObject<int>();
                        break;
                    case "ability":
                        pokemon.Ability = prop.Value.ToObject<int>();
                        break;
                    case "helditem":
                        pokemon.HeldItem = prop.Value.ToObject<int>();
                        break;
                    case "gender":
                        pokemon.Gender = (byte)prop.Value.ToObject<int>();
                        break;
                    case "shiny":
                        bool shouldBeShiny = prop.Value.ToObject<bool>();
                        if (shouldBeShiny && !pokemon.IsShiny)
                            CommonEdits.SetShiny(pokemon, Shiny.AlwaysStar);
                        else if (!shouldBeShiny && pokemon.IsShiny)
                            CommonEdits.SetShiny(pokemon, Shiny.Never);
                        break;
                    case "moves":
                        var moves = prop.Value.ToObject<int[]>();
                        if (moves != null && moves.Length >= 1) pokemon.Move1 = (ushort)moves[0];
                        if (moves != null && moves.Length >= 2) pokemon.Move2 = (ushort)moves[1];
                        if (moves != null && moves.Length >= 3) pokemon.Move3 = (ushort)moves[2];
                        if (moves != null && moves.Length >= 4) pokemon.Move4 = (ushort)moves[3];
                        break;
                    case "ivs":
                        var ivs = prop.Value.ToObject<Dictionary<string, int>>();
                        if (ivs != null)
                        {
                            if (ivs.ContainsKey("hp")) pokemon.IV_HP = ivs["hp"];
                            if (ivs.ContainsKey("attack")) pokemon.IV_ATK = ivs["attack"];
                            if (ivs.ContainsKey("defense")) pokemon.IV_DEF = ivs["defense"];
                            if (ivs.ContainsKey("spAttack")) pokemon.IV_SPA = ivs["spAttack"];
                            if (ivs.ContainsKey("spDefense")) pokemon.IV_SPD = ivs["spDefense"];
                            if (ivs.ContainsKey("speed")) pokemon.IV_SPE = ivs["speed"];
                        }
                        break;
                    case "evs":
                        var evs = prop.Value.ToObject<Dictionary<string, int>>();
                        if (evs != null)
                        {
                            if (evs.ContainsKey("hp")) pokemon.EV_HP = evs["hp"];
                            if (evs.ContainsKey("attack")) pokemon.EV_ATK = evs["attack"];
                            if (evs.ContainsKey("defense")) pokemon.EV_DEF = evs["defense"];
                            if (evs.ContainsKey("spAttack")) pokemon.EV_SPA = evs["spAttack"];
                            if (evs.ContainsKey("spDefense")) pokemon.EV_SPD = evs["spDefense"];
                            if (evs.ContainsKey("speed")) pokemon.EV_SPE = evs["speed"];
                        }
                        break;
                }
            }

            pokemon.RefreshChecksum();
            SetPokemonAt(save, box, slot, pokemon);

            return SuccessResponse(new { success = true, message = "Pokemon modified successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error modifying Pokemon: {ex}");
            return ErrorResponse("Failed to modify Pokemon");
        }
    }

    [ApiAction("deletePokemon", ApiCategory.Pokemon, "Clear a box slot.")]
    public string DeletePokemon(SaveFile save, int box, int slot)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            var blank = save.BlankPKM;
            SetPokemonAt(save, box, slot, blank);

            return SuccessResponse(new { success = true, message = "Pokemon deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting Pokemon: {ex}");
            return ErrorResponse("Failed to delete Pokemon");
        }
    }

    [ApiAction("movePokemon", ApiCategory.Pokemon, "Move or swap Pokémon between slots.")]
    public string MovePokemon(SaveFile save, int fromBox, int fromSlot, int toBox, int toSlot)
    {
        try
        {
            var error1 = ValidateBoxSlot(save, fromBox, fromSlot);
            if (error1 != null) return error1;

            var error2 = ValidateBoxSlot(save, toBox, toSlot);
            if (error2 != null) return error2;

            var source = GetPokemonAt(save, fromBox, fromSlot);
            var dest = GetPokemonAt(save, toBox, toSlot);

            if (source == null || source.Species == 0)
                return ErrorResponse("No Pokemon in source slot");

            SetPokemonAt(save, toBox, toSlot, source);
            SetPokemonAt(save, fromBox, fromSlot, dest ?? save.BlankPKM);

            return SuccessResponse(new { success = true, message = "Pokemon moved successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving Pokemon: {ex}");
            return ErrorResponse("Failed to move Pokemon");
        }
    }

    [ApiAction("importShowdown", ApiCategory.Pokemon, "Import a Pokémon from Showdown format.")]
    public string ImportShowdown(SaveFile save, int box, int slot, string showdownText)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            if (string.IsNullOrEmpty(showdownText))
                return ErrorResponse("Showdown text is required");

            var showdown = new ShowdownSet(showdownText);
            var blank = EntityBlank.GetBlank(save.Generation, (GameVersion)save.Version);
            blank.ApplySetDetails(showdown);
            blank.RefreshChecksum();

            SetPokemonAt(save, box, slot, blank);

            return SuccessResponse(new { success = true, message = "Pokemon imported from Showdown format (manual legality check recommended)" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing Showdown: {ex}");
            return ErrorResponse("Failed to import from Showdown format");
        }
    }

    [ApiAction("getParty", ApiCategory.Pokemon, "Get all Pokémon in the party.")]
    public string GetParty(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var partyList = new List<object>();
            for (int i = 0; i < save.PartyCount; i++)
            {
                var pokemon = save.GetPartySlotAtIndex(i);
                if (pokemon != null && pokemon.Species > 0)
                {
                    var pokemonData = FormatPokemonData(pokemon);
                    partyList.Add(new
                    {
                        slot = i,
                        pokemon = pokemonData
                    });
                }
            }

            return SuccessResponse(new { party = partyList, count = save.PartyCount });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting party: {ex}");
            return ErrorResponse("Failed to get party");
        }
    }

    [ApiAction("getPartySlot", ApiCategory.Pokemon, "Get detailed information about a specific party slot.")]
    public string GetPartySlot(SaveFile save, int slot)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (slot < 0 || slot >= 6)
                return ErrorResponse($"Invalid party slot: {slot}. Must be 0-5");

            if (slot >= save.PartyCount)
                return SuccessResponse(new { empty = true });

            var pokemon = save.GetPartySlotAtIndex(slot);

            if (pokemon == null || pokemon.Species == 0)
                return SuccessResponse(new { empty = true });

            return SuccessResponse(FormatPokemonData(pokemon));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting party slot: {ex}");
            return ErrorResponse("Failed to get party slot");
        }
    }

    [ApiAction("setPartySlot", ApiCategory.Pokemon, "Replace a Pokémon in a party slot with binary data.")]
    public string SetPartySlot(SaveFile save, int slot, [ApiParameter(Format = "base64")] string base64PokemonData)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (slot < 0 || slot >= 6)
                return ErrorResponse($"Invalid party slot: {slot}. Must be 0-5");

            if (string.IsNullOrEmpty(base64PokemonData))
                return ErrorResponse("Pokemon data is required");

            byte[] data;
            try
            {
                data = Convert.FromBase64String(base64PokemonData);
            }
            catch (FormatException)
            {
                return ErrorResponse("Invalid base64 data");
            }

            var pokemon = EntityFormat.GetFromBytes(data);
            if (pokemon == null)
                return ErrorResponse("Invalid Pokemon data");

            pokemon.RefreshChecksum();

            save.SetPartySlotAtIndex(pokemon, slot);

            return SuccessResponse(new { success = true, message = "Party slot updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting party slot: {ex}");
            return ErrorResponse("Failed to set party slot");
        }
    }

    [ApiAction("deletePartySlot", ApiCategory.Pokemon, "Remove a Pokémon from the party.")]
    public string DeletePartySlot(SaveFile save, int slot)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (slot < 0 || slot >= 6)
                return ErrorResponse($"Invalid party slot: {slot}. Must be 0-5");

            if (slot >= save.PartyCount)
                return ErrorResponse("Slot is already empty");

            save.DeletePartySlot(slot);

            return SuccessResponse(new { success = true, message = "Pokemon removed from party" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting party slot: {ex}");
            return ErrorResponse("Failed to delete party slot");
        }
    }

    [ApiAction("getRibbons", ApiCategory.Pokemon, "Get ribbons for a Pokémon", RequiresSession = true)]
    public string GetRibbons(SaveFile save, int box, int slot)
    {
        try
        {
            var pk = save.GetBoxSlotAtIndex(box, slot);
            if (pk == null || pk.Species == 0)
            {
                return JsonConvert.SerializeObject(new { error = "No Pokémon in this slot" });
            }

            if (pk is not IRibbonIndex)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Ribbons are not supported for this Pokémon or generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var ribbons = new List<object>();
            var ribbonInfo = RibbonInfo.GetRibbonInfo(pk);
            
            foreach (var ribbon in ribbonInfo)
            {
                var hasRibbon = ReflectUtil.GetValue(pk, ribbon.Name) as bool? ?? false;
                ribbons.Add(new
                {
                    name = ribbon.Name,
                    hasRibbon = hasRibbon
                });
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                box = box,
                slot = slot,
                species = pk.Species,
                ribbonCount = ribbons.Count(r => ((dynamic)r).hasRibbon),
                ribbons = ribbons
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get ribbons: {ex.Message}" });
        }
    }

    [ApiAction("setRibbon", ApiCategory.Pokemon, "Set a ribbon for a Pokémon", RequiresSession = true)]
    public string SetRibbon(SaveFile save, int box, int slot, string ribbonName, bool value)
    {
        try
        {
            var pk = save.GetBoxSlotAtIndex(box, slot);
            if (pk == null || pk.Species == 0)
            {
                return JsonConvert.SerializeObject(new { error = "No Pokémon in this slot" });
            }

            if (pk is not IRibbonIndex)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Ribbons are not supported for this Pokémon or generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var ribbonInfo = RibbonInfo.GetRibbonInfo(pk);
            var ribbon = ribbonInfo.FirstOrDefault(r => r.Name == ribbonName);

            if (ribbon == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = $"Ribbon '{ribbonName}' not found for this Pokémon"
                });
            }

            ReflectUtil.SetValue(pk, ribbon.Name, value);
            save.SetBoxSlotAtIndex(pk, box, slot);

            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = $"Ribbon '{ribbonName}' {(value ? "added to" : "removed from")} Pokémon",
                box = box,
                slot = slot,
                ribbonName = ribbonName,
                value = value
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to set ribbon: {ex.Message}" });
        }
    }

    [ApiAction("getRibbonCount", ApiCategory.Pokemon, "Get total ribbon count for a Pokémon", RequiresSession = true)]
    public string GetRibbonCount(SaveFile save, int box, int slot)
    {
        try
        {
            var pk = save.GetBoxSlotAtIndex(box, slot);
            if (pk == null || pk.Species == 0)
            {
                return JsonConvert.SerializeObject(new { error = "No Pokémon in this slot" });
            }

            if (pk is not IRibbonIndex)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Ribbons are not supported for this Pokémon or generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var ribbonInfo = RibbonInfo.GetRibbonInfo(pk);
            var count = ribbonInfo.Count(r => ReflectUtil.GetValue(pk, r.Name) as bool? ?? false);

            return JsonConvert.SerializeObject(new
            {
                success = true,
                box = box,
                slot = slot,
                species = pk.Species,
                ribbonCount = count
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get ribbon count: {ex.Message}" });
        }
    }

    [ApiAction("getContestStats", ApiCategory.Pokemon, "Get contest stats for a Pokémon", RequiresSession = true)]
    public string GetContestStats(SaveFile save, int box, int slot)
    {
        try
        {
            var pk = save.GetBoxSlotAtIndex(box, slot);
            if (pk == null || pk.Species == 0)
            {
                return JsonConvert.SerializeObject(new { error = "No Pokémon in this slot" });
            }

            if (pk is not IContestStats)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Contest stats are not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var pkType = pk.GetType();
            var coolProp = pkType.GetProperty("CNT_Cool") ?? pkType.GetProperty("Contest_Cool");
            var beautyProp = pkType.GetProperty("CNT_Beauty") ?? pkType.GetProperty("Contest_Beauty");
            var cuteProp = pkType.GetProperty("CNT_Cute") ?? pkType.GetProperty("Contest_Cute");
            var smartProp = pkType.GetProperty("CNT_Smart") ?? pkType.GetProperty("Contest_Smart");
            var toughProp = pkType.GetProperty("CNT_Tough") ?? pkType.GetProperty("Contest_Tough");

            return JsonConvert.SerializeObject(new
            {
                success = true,
                box = box,
                slot = slot,
                species = pk.Species,
                contestStats = new
                {
                    cool = coolProp?.GetValue(pk) ?? 0,
                    beauty = beautyProp?.GetValue(pk) ?? 0,
                    cute = cuteProp?.GetValue(pk) ?? 0,
                    smart = smartProp?.GetValue(pk) ?? 0,
                    tough = toughProp?.GetValue(pk) ?? 0,
                    sheen = 0
                }
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get contest stats: {ex.Message}" });
        }
    }

    [ApiAction("setContestStat", ApiCategory.Pokemon, "Set a contest stat for a Pokémon", RequiresSession = true)]
    public string SetContestStat(SaveFile save, int box, int slot, string stat, int value)
    {
        try
        {
            var pk = save.GetBoxSlotAtIndex(box, slot);
            if (pk == null || pk.Species == 0)
            {
                return JsonConvert.SerializeObject(new { error = "No Pokémon in this slot" });
            }

            if (pk is not IContestStats)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Contest stats are not supported for this generation. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var byteValue = (byte)Math.Clamp(value, 0, 255);
            var pkType = pk.GetType();

            switch (stat.ToLower())
            {
                case "cool":
                    var coolProp = pkType.GetProperty("CNT_Cool") ?? pkType.GetProperty("Contest_Cool");
                    coolProp?.SetValue(pk, byteValue);
                    break;
                case "beauty":
                    var beautyProp = pkType.GetProperty("CNT_Beauty") ?? pkType.GetProperty("Contest_Beauty");
                    beautyProp?.SetValue(pk, byteValue);
                    break;
                case "cute":
                    var cuteProp = pkType.GetProperty("CNT_Cute") ?? pkType.GetProperty("Contest_Cute");
                    cuteProp?.SetValue(pk, byteValue);
                    break;
                case "smart":
                    var smartProp = pkType.GetProperty("CNT_Smart") ?? pkType.GetProperty("Contest_Smart");
                    smartProp?.SetValue(pk, byteValue);
                    break;
                case "tough":
                    var toughProp = pkType.GetProperty("CNT_Tough") ?? pkType.GetProperty("Contest_Tough");
                    toughProp?.SetValue(pk, byteValue);
                    break;
                default:
                    return JsonConvert.SerializeObject(new
                    {
                        error = "Invalid contest stat. Must be one of: cool, beauty, cute, smart, tough"
                    });
            }

            save.SetBoxSlotAtIndex(pk, box, slot);

            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = $"Contest stat '{stat}' set to {byteValue}",
                box = box,
                slot = slot,
                stat = stat,
                value = byteValue
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to set contest stat: {ex.Message}" });
        }
    }

    [ApiAction("checkLegality", ApiCategory.Pokemon, "Check if a Pokémon is legal/legitimate.")]
    public string CheckLegality(SaveFile save, int box, int slot)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            var pokemon = GetPokemonAt(save, box, slot);

            if (pokemon == null || pokemon.Species == 0)
                return ErrorResponse("No Pokemon in this slot");

            var analysis = new LegalityAnalysis(pokemon);

            var result = new
            {
                valid = analysis.Valid,
                parsed = analysis.Parsed,
                report = analysis.Report(),
                encounterType = analysis.EncounterOriginal?.GetType().Name ?? "Unknown",
                encounterMatch = analysis.EncounterMatch?.GetType().Name ?? "Unknown"
            };

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking legality: {ex}");
            return ErrorResponse("Failed to check legality");
        }
    }

    [ApiAction("legalizePokemon", ApiCategory.Pokemon, "Apply basic normalization to a Pokémon (heal HP/PP, refresh checksum).")]
    public string LegalizePokemon(SaveFile save, int box, int slot)
    {
        try
        {
            var error = ValidateBoxSlot(save, box, slot);
            if (error != null) return error;

            var pokemon = GetPokemonAt(save, box, slot);

            if (pokemon == null || pokemon.Species == 0)
                return ErrorResponse("No Pokemon in this slot");

            pokemon.HealPP();
            pokemon.Heal();
            pokemon.RefreshChecksum();
            
            SetPokemonAt(save, box, slot, pokemon);

            var analysis = new LegalityAnalysis(pokemon);

            return SuccessResponse(new
            {
                success = true,
                message = "Pokemon healed and normalized (HP/PP restored). Note: This does NOT fix legality issues - use checkLegality to verify.",
                valid = analysis.Valid,
                report = analysis.Report()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error normalizing Pokemon: {ex}");
            return ErrorResponse("Failed to normalize Pokemon");
        }
    }
}
