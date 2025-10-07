using PKHeX.Core;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class PokemonStorageService : ServiceBase
{
    [ApiAction("getBoxNames", ApiCategory.Storage, "Get all box names.")]
    public string GetBoxNames(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var getBoxNameMethod = saveType.GetMethod("GetBoxName");
            
            if (getBoxNameMethod == null)
            {
                var boxNames = new List<string>();
                for (int i = 0; i < save.BoxCount; i++)
                {
                    boxNames.Add($"Box {i + 1}");
                }
                return SuccessResponse(new { boxNames, supported = false, message = "Box naming not supported for this generation" });
            }

            var boxNames2 = new List<string>();
            for (int i = 0; i < save.BoxCount; i++)
            {
                var boxName = (string?)getBoxNameMethod.Invoke(save, new object[] { i });
                boxNames2.Add(boxName ?? $"Box {i + 1}");
            }

            return SuccessResponse(new { boxNames = boxNames2 });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting box names: {ex}");
            return ErrorResponse("Failed to get box names");
        }
    }

    [ApiAction("getBoxWallpapers", ApiCategory.Storage, "Get box wallpaper settings for all boxes.")]
    public string GetBoxWallpapers(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var getBoxWallpaperMethod = saveType.GetMethod("GetBoxWallpaper");
            
            if (getBoxWallpaperMethod == null)
            {
                return SuccessResponse(new 
                { 
                    supported = false,
                    message = $"Box wallpapers not supported for {save.GetType().Name} (Generation {save.Generation})",
                    wallpapers = new int[save.BoxCount]
                });
            }

            var wallpapers = new List<object>();
            for (int i = 0; i < save.BoxCount; i++)
            {
                var wallpaper = (int)getBoxWallpaperMethod.Invoke(save, new object[] { i })!;
                wallpapers.Add(new { box = i, wallpaper });
            }

            return SuccessResponse(new { supported = true, wallpapers });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting box wallpapers: {ex}");
            return ErrorResponse($"Failed to get box wallpapers: {ex.Message}");
        }
    }

    [ApiAction("setBoxWallpaper", ApiCategory.Storage, "Set box wallpaper for a specific box.")]
    public string SetBoxWallpaper(SaveFile save, int box, int wallpaper)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (box < 0 || box >= save.BoxCount)
                return ErrorResponse($"Invalid box index. Must be between 0 and {save.BoxCount - 1}");

            var saveType = save.GetType();
            var setBoxWallpaperMethod = saveType.GetMethod("SetBoxWallpaper");
            
            if (setBoxWallpaperMethod == null)
            {
                return ErrorResponse($"Box wallpapers not supported for {save.GetType().Name} (Generation {save.Generation})");
            }

            setBoxWallpaperMethod.Invoke(save, new object[] { box, wallpaper });

            return SuccessResponse(new
            {
                success = true,
                message = $"Box {box} wallpaper set to {wallpaper}",
                box,
                wallpaper
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting box wallpaper: {ex}");
            return ErrorResponse($"Failed to set box wallpaper: {ex.Message}");
        }
    }

    [ApiAction("getBattleBox", ApiCategory.Storage, "Get Battle Box Pokémon (generation-specific).")]
    public string GetBattleBox(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var battleBoxCountProperty = saveType.GetProperty("BattleBoxCount");

            if (battleBoxCountProperty == null)
            {
                return ErrorResponse($"Battle Box not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave/getSaveInfo to verify support.");
            }

            var count = (int)battleBoxCountProperty.GetValue(save)!;
            var battleBoxPokemon = new List<object>();

            var getBattleBoxSlotMethod = saveType.GetMethod("GetBattleBoxSlot");
            if (getBattleBoxSlotMethod == null)
            {
                return ErrorResponse($"Battle Box slot access not available for {save.GetType().Name}. Check the 'features' array from loadSave/getSaveInfo to verify support.");
            }

            for (int i = 0; i < count; i++)
            {
                var pkm = getBattleBoxSlotMethod.Invoke(save, new object[] { i }) as PKM;
                
                if (pkm == null || pkm.Species == 0)
                {
                    battleBoxPokemon.Add(new { slot = i, empty = true });
                }
                else
                {
                    battleBoxPokemon.Add(new
                    {
                        slot = i,
                        species = pkm.Species,
                        nickname = pkm.Nickname,
                        level = pkm.CurrentLevel,
                        isShiny = pkm.IsShiny
                    });
                }
            }

            return SuccessResponse(new
            {
                battleBoxCount = count,
                pokemon = battleBoxPokemon,
                generation = save.Generation
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting battle box: {ex}");
            return ErrorResponse($"Failed to get battle box: {ex.Message}");
        }
    }

    [ApiAction("setBattleBoxSlot", ApiCategory.Storage, "Set a Pokémon in a Battle Box slot (generation-specific).")]
    public string SetBattleBoxSlot(SaveFile save, int slot, string base64PokemonData)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            var battleBoxCountProperty = saveType.GetProperty("BattleBoxCount");

            if (battleBoxCountProperty == null)
            {
                return ErrorResponse($"Battle Box not supported for {save.GetType().Name} (Generation {save.Generation}). Check the 'features' array from loadSave/getSaveInfo to verify support.");
            }

            var count = (int)battleBoxCountProperty.GetValue(save)!;

            if (slot < 0 || slot >= count)
                return ErrorResponse($"Invalid slot. Must be between 0 and {count - 1}");

            if (string.IsNullOrEmpty(base64PokemonData))
                return ErrorResponse("Base64 Pokemon data is required");

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
                return ErrorResponse("Unable to parse Pokemon data");

            var setBattleBoxSlotMethod = saveType.GetMethod("SetBattleBoxSlot");
            if (setBattleBoxSlotMethod == null)
            {
                return ErrorResponse($"Battle Box slot modification not available for {save.GetType().Name}. Check the 'features' array from loadSave/getSaveInfo to verify support.");
            }

            setBattleBoxSlotMethod.Invoke(save, new object[] { slot, pokemon });

            return SuccessResponse(new
            {
                success = true,
                message = $"Battle Box slot {slot} updated successfully",
                slot,
                species = pokemon.Species
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting battle box slot: {ex}");
            return ErrorResponse($"Failed to set battle box slot: {ex.Message}");
        }
    }

    [ApiAction("getDaycare", ApiCategory.Storage, "Get daycare information (generation-specific).")]
    public string GetDaycare(SaveFile save, int location = 0)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var saveType = save.GetType();
            
            var isDaycareOccupiedMethod = saveType.GetMethod("IsDaycareOccupied");
            var isDaycareHasEggMethod = saveType.GetMethod("IsDaycareHasEgg");
            
            if (isDaycareOccupiedMethod == null && isDaycareHasEggMethod == null)
            {
                return ErrorResponse($"Daycare access not supported for {save.GetType().Name} (Generation {save.Generation}). This feature requires generation-specific implementation.");
            }

            var daycareData = new
            {
                location,
                generation = save.Generation,
                slot0Occupied = isDaycareOccupiedMethod?.Invoke(save, new object[] { location, 0 }) as bool? ?? false,
                slot1Occupied = isDaycareOccupiedMethod?.Invoke(save, new object[] { location, 1 }) as bool? ?? false,
                hasEgg = isDaycareHasEggMethod?.Invoke(save, new object[] { location }) as bool? ?? false
            };

            return SuccessResponse(daycareData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting daycare: {ex}");
            return ErrorResponse($"Failed to get daycare: {ex.Message}");
        }
    }
}
