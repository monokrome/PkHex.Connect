using PKHeX.Core;
using Newtonsoft.Json.Linq;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class GameKnowledgeService : ServiceBase
{
    [ApiAction("getPouchItems", ApiCategory.Items, "Get all items in the player's inventory/bag organized by pouches (e.g., Items, Medicine, Balls, TMs, Berries, Key Items). Returns pouchType, pouchIndex, items array, and totalSlots for each pouch.")]
    public string GetPouchItems(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var inventory = save.Inventory;
            if (inventory == null)
                return ErrorResponse("Inventory not supported for this save file type");

            var pouches = new List<object>();

            for (int i = 0; i < inventory.Count; i++)
            {
                var pouch = inventory[i];
                if (pouch == null) continue;

                var itemList = new List<object>();
                
                foreach (var item in pouch.Items)
                {
                    if (item.Index > 0 && item.Count > 0)
                    {
                        itemList.Add(new
                        {
                            itemId = item.Index,
                            count = item.Count
                        });
                    }
                }

                pouches.Add(new
                {
                    pouchType = pouch.Type.ToString(),
                    pouchIndex = i,
                    items = itemList,
                    totalSlots = pouch.Items.Length
                });
            }

            return SuccessResponse(new { pouches });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting items: {ex}");
            return ErrorResponse($"Failed to get items: {ex.Message}");
        }
    }

    [ApiAction("addItemToPouch", ApiCategory.Items, "Add an item to the player's inventory pouch. Items are organized in pouches (e.g., Items, Medicine, Balls). Use getPouchItems to see available pouches and their indices.")]
    public string AddItemToPouch(SaveFile save, 
        [ApiParameter(Description = "The item ID to add")]
        int itemId, 
        [ApiParameter(Description = "Number of items to add (1-999)", Default = "1")]
        int count = 1, 
        [ApiParameter(Required = false, Description = "Optional pouch index from getPouchItems response. If not specified, searches all pouches for space.")]
        int? pouchIndex = null)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (itemId <= 0)
                return ErrorResponse("Invalid item ID");

            if (count <= 0 || count > 999)
                return ErrorResponse("Count must be between 1 and 999");

            var inventory = save.Inventory;
            if (inventory == null)
                return ErrorResponse("Inventory not supported for this save file type");

            if (pouchIndex.HasValue)
            {
                if (pouchIndex.Value < 0 || pouchIndex.Value >= inventory.Count)
                    return ErrorResponse($"Invalid pouch index. Must be 0-{inventory.Count - 1}");

                var targetPouch = inventory[pouchIndex.Value];
                return TryAddItemToPouch(targetPouch, itemId, count, pouchIndex.Value, save);
            }

            for (int i = 0; i < inventory.Count; i++)
            {
                var pouch = inventory[i];
                if (pouch == null) continue;

                var result = TryAddItemToPouch(pouch, itemId, count, i, save);
                var json = JObject.Parse(result);
                if (json["success"]?.Value<bool>() == true)
                {
                    return result;
                }
            }

            return ErrorResponse($"No empty slots available in any pouch");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error giving item: {ex}");
            return ErrorResponse($"Failed to give item: {ex.Message}");
        }
    }

    private string TryAddItemToPouch(InventoryPouch pouch, int itemId, int count, int pouchIndex, SaveFile save)
    {
        for (int i = 0; i < pouch.Items.Length; i++)
        {
            if (pouch.Items[i].Index == itemId)
            {
                var newCount = Math.Min(pouch.Items[i].Count + count, pouch.MaxCount);
                pouch.Items[i] = new InventoryItem { Index = itemId, Count = newCount };
                
                var updatedInventory = save.Inventory.ToList();
                updatedInventory[pouchIndex] = pouch;
                save.Inventory = updatedInventory;
                
                return SuccessResponse(new { 
                    success = true, 
                    message = $"Stacked item {itemId} in pouch {pouchIndex}, new count: {newCount}",
                    pouchIndex,
                    itemId,
                    count = newCount
                });
            }
        }

        for (int i = 0; i < pouch.Items.Length; i++)
        {
            if (pouch.Items[i].Index == 0)
            {
                var finalCount = Math.Min(count, pouch.MaxCount);
                pouch.Items[i] = new InventoryItem { Index = itemId, Count = finalCount };
                
                var updatedInventory = save.Inventory.ToList();
                updatedInventory[pouchIndex] = pouch;
                save.Inventory = updatedInventory;
                
                return SuccessResponse(new { 
                    success = true, 
                    message = $"Added {finalCount}x item {itemId} to pouch {pouchIndex} slot {i}",
                    pouchIndex,
                    slot = i,
                    itemId,
                    count = finalCount
                });
            }
        }

        return ErrorResponse($"No empty slots available in pouch {pouchIndex}");
    }

    [ApiAction("removeItemFromPouch", ApiCategory.Items, "Remove an item from the player's inventory pouch.")]
    public string RemoveItemFromPouch(SaveFile save, int itemId, int count = 1)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (itemId <= 0)
                return ErrorResponse("Invalid item ID");

            if (count <= 0)
                return ErrorResponse("Count must be greater than 0");

            var inventory = save.Inventory;
            if (inventory == null)
                return ErrorResponse("Inventory not supported for this save file type");

            for (int p = 0; p < inventory.Count; p++)
            {
                var pouch = inventory[p];
                if (pouch == null) continue;

                for (int i = 0; i < pouch.Items.Length; i++)
                {
                    if (pouch.Items[i].Index == itemId)
                    {
                        var currentCount = pouch.Items[i].Count;
                        var newCount = currentCount - count;

                        if (newCount <= 0)
                        {
                            pouch.Items[i] = new InventoryItem { Index = 0, Count = 0 };
                            
                            var updatedInventory = save.Inventory.ToList();
                            updatedInventory[p] = pouch;
                            save.Inventory = updatedInventory;
                            
                            return SuccessResponse(new { 
                                success = true, 
                                message = $"Removed item {itemId} from inventory",
                                pouchIndex = p,
                                slot = i,
                                itemId
                            });
                        }
                        else
                        {
                            pouch.Items[i] = new InventoryItem { Index = itemId, Count = newCount };
                            
                            var updatedInventory = save.Inventory.ToList();
                            updatedInventory[p] = pouch;
                            save.Inventory = updatedInventory;
                            
                            return SuccessResponse(new { 
                                success = true, 
                                message = $"Decreased item {itemId} count to {newCount}",
                                pouchIndex = p,
                                slot = i,
                                itemId,
                                count = newCount
                            });
                        }
                    }
                }
            }

            return ErrorResponse($"Item {itemId} not found in inventory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing item: {ex}");
            return ErrorResponse($"Failed to remove item: {ex.Message}");
        }
    }

    [ApiAction("getPokedex", ApiCategory.Pokedex, "Get Pokedex data for all species with their seen/caught status.")]
    public string GetPokedex(SaveFile save)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            var pokedexData = new List<object>();
            
            for (ushort species = 1; species <= save.MaxSpeciesID; species++)
            {
                var caught = save.GetCaught(species);
                var seen = save.GetSeen(species);
                
                pokedexData.Add(new
                {
                    species = species,
                    caught = caught,
                    seen = seen
                });
            }

            return SuccessResponse(new { pokedex = pokedexData });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting pokedex: {ex}");
            return ErrorResponse($"Failed to get pokedex: {ex.Message}");
        }
    }

    [ApiAction("setPokedexCaught", ApiCategory.Pokedex, "Mark a species as caught in the Pokedex.")]
    public string SetPokedexCaught(SaveFile save, int species, bool value = true, int form = 0)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (species <= 0 || species > save.MaxSpeciesID)
                return ErrorResponse($"Invalid species ID. Must be between 1 and {save.MaxSpeciesID}");

            save.SetCaught((ushort)species, value);

            return SuccessResponse(new 
            { 
                success = true, 
                message = $"Species {species} caught status set to {value}",
                species = species,
                caught = value
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting pokedex caught: {ex}");
            return ErrorResponse($"Failed to set pokedex caught: {ex.Message}");
        }
    }

    [ApiAction("setPokedexSeen", ApiCategory.Pokedex, "Mark a species as seen in the Pokedex.")]
    public string SetPokedexSeen(SaveFile save, int species, bool value = true, int form = 0)
    {
        try
        {
            var error = ValidateSave(save);
            if (error != null) return error;

            if (species <= 0 || species > save.MaxSpeciesID)
                return ErrorResponse($"Invalid species ID. Must be between 1 and {save.MaxSpeciesID}");

            save.SetSeen((ushort)species, value);

            return SuccessResponse(new 
            { 
                success = true, 
                message = $"Species {species} seen status set to {value}",
                species = species,
                seen = value
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting pokedex seen: {ex}");
            return ErrorResponse($"Failed to set pokedex seen: {ex.Message}");
        }
    }

    [ApiAction("getSpeciesName", ApiCategory.Knowledge, "Get the name of a Pokémon species by ID.", RequiresSession = false)]
    public string GetSpeciesName(int id)
    {
        try
        {
            if (id < 0 || id >= GameInfo.Strings.Species.Count)
                return ErrorResponse($"Invalid species ID: {id}");

            var name = GameInfo.Strings.Species[id];
            
            if (string.IsNullOrEmpty(name))
                return ErrorResponse($"No name found for species ID: {id}");

            return SuccessResponse(new { id, name });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting species name: {ex}");
            return ErrorResponse("Failed to get species name");
        }
    }

    [ApiAction("getMoveName", ApiCategory.Knowledge, "Get the name of a move by ID.", RequiresSession = false)]
    public string GetMoveName(int id)
    {
        try
        {
            if (id < 0 || id >= GameInfo.Strings.Move.Count)
                return ErrorResponse($"Invalid move ID: {id}");

            var name = GameInfo.Strings.Move[id];
            
            if (string.IsNullOrEmpty(name))
                return ErrorResponse($"No name found for move ID: {id}");

            return SuccessResponse(new { id, name });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting move name: {ex}");
            return ErrorResponse("Failed to get move name");
        }
    }

    [ApiAction("getAbilityName", ApiCategory.Knowledge, "Get the name of an ability by ID.", RequiresSession = false)]
    public string GetAbilityName(int id)
    {
        try
        {
            if (id < 0 || id >= GameInfo.Strings.Ability.Count)
                return ErrorResponse($"Invalid ability ID: {id}");

            var name = GameInfo.Strings.Ability[id];
            
            if (string.IsNullOrEmpty(name))
                return ErrorResponse($"No name found for ability ID: {id}");

            return SuccessResponse(new { id, name });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting ability name: {ex}");
            return ErrorResponse("Failed to get ability name");
        }
    }

    [ApiAction("getItemName", ApiCategory.Knowledge, "Get the name of an item by ID.", RequiresSession = false)]
    public string GetItemName(int id)
    {
        try
        {
            if (id < 0 || id >= GameInfo.Strings.Item.Count)
                return ErrorResponse($"Invalid item ID: {id}");

            var name = GameInfo.Strings.Item[id];
            
            if (string.IsNullOrEmpty(name))
                return ErrorResponse($"No name found for item ID: {id}");

            return SuccessResponse(new { id, name });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting item name: {ex}");
            return ErrorResponse("Failed to get item name");
        }
    }

    [ApiAction("getNatureName", ApiCategory.Knowledge, "Get the name of a nature by ID.", RequiresSession = false)]
    public string GetNatureName(int id)
    {
        try
        {
            if (id < 0 || id >= GameInfo.Strings.Natures.Count)
                return ErrorResponse($"Invalid nature ID: {id}");

            var name = GameInfo.Strings.Natures[id];
            
            if (string.IsNullOrEmpty(name))
                return ErrorResponse($"No name found for nature ID: {id}");

            return SuccessResponse(new { id, name });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting nature name: {ex}");
            return ErrorResponse("Failed to get nature name");
        }
    }

    [ApiAction("getTypeName", ApiCategory.Knowledge, "Get the name of a type by ID.", RequiresSession = false)]
    public string GetTypeName(int id)
    {
        try
        {
            if (id < 0 || id >= GameInfo.Strings.Types.Count)
                return ErrorResponse($"Invalid type ID: {id}");

            var name = GameInfo.Strings.Types[id];
            
            if (string.IsNullOrEmpty(name))
                return ErrorResponse($"No name found for type ID: {id}");

            return SuccessResponse(new { id, name });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting type name: {ex}");
            return ErrorResponse("Failed to get type name");
        }
    }

    [ApiAction("getAllSpecies", ApiCategory.Knowledge, "Get all Pokémon species with their IDs and names.", RequiresSession = false)]
    public string GetAllSpecies()
    {
        try
        {
            var speciesList = new List<object>();
            
            for (int i = 0; i < GameInfo.Strings.Species.Count; i++)
            {
                var name = GameInfo.Strings.Species[i];
                if (!string.IsNullOrEmpty(name))
                {
                    speciesList.Add(new { id = i, name });
                }
            }

            return SuccessResponse(new { species = speciesList, count = speciesList.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all species: {ex}");
            return ErrorResponse("Failed to get all species");
        }
    }

    [ApiAction("getAllMoves", ApiCategory.Knowledge, "Get all moves with their IDs and names.", RequiresSession = false)]
    public string GetAllMoves()
    {
        try
        {
            var movesList = new List<object>();
            
            for (int i = 0; i < GameInfo.Strings.Move.Count; i++)
            {
                var name = GameInfo.Strings.Move[i];
                if (!string.IsNullOrEmpty(name))
                {
                    movesList.Add(new { id = i, name });
                }
            }

            return SuccessResponse(new { moves = movesList, count = movesList.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all moves: {ex}");
            return ErrorResponse("Failed to get all moves");
        }
    }

    [ApiAction("getAllAbilities", ApiCategory.Knowledge, "Get all abilities with their IDs and names.", RequiresSession = false)]
    public string GetAllAbilities()
    {
        try
        {
            var abilitiesList = new List<object>();
            
            for (int i = 0; i < GameInfo.Strings.Ability.Count; i++)
            {
                var name = GameInfo.Strings.Ability[i];
                if (!string.IsNullOrEmpty(name))
                {
                    abilitiesList.Add(new { id = i, name });
                }
            }

            return SuccessResponse(new { abilities = abilitiesList, count = abilitiesList.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all abilities: {ex}");
            return ErrorResponse("Failed to get all abilities");
        }
    }

    [ApiAction("getAllItems", ApiCategory.Knowledge, "Get all items with their IDs and names.", RequiresSession = false)]
    public string GetAllItems()
    {
        try
        {
            var itemsList = new List<object>();
            
            for (int i = 0; i < GameInfo.Strings.Item.Count; i++)
            {
                var name = GameInfo.Strings.Item[i];
                if (!string.IsNullOrEmpty(name))
                {
                    itemsList.Add(new { id = i, name });
                }
            }

            return SuccessResponse(new { items = itemsList, count = itemsList.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all items: {ex}");
            return ErrorResponse("Failed to get all items");
        }
    }

    [ApiAction("getAllNatures", ApiCategory.Knowledge, "Get all natures with their IDs and names.", RequiresSession = false)]
    public string GetAllNatures()
    {
        try
        {
            var naturesList = new List<object>();
            
            for (int i = 0; i < GameInfo.Strings.Natures.Count; i++)
            {
                var name = GameInfo.Strings.Natures[i];
                if (!string.IsNullOrEmpty(name))
                {
                    naturesList.Add(new { id = i, name });
                }
            }

            return SuccessResponse(new { natures = naturesList, count = naturesList.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all natures: {ex}");
            return ErrorResponse("Failed to get all natures");
        }
    }

    [ApiAction("getAllTypes", ApiCategory.Knowledge, "Get all types with their IDs and names.", RequiresSession = false)]
    public string GetAllTypes()
    {
        try
        {
            var typesList = new List<object>();
            
            for (int i = 0; i < GameInfo.Strings.Types.Count; i++)
            {
                var name = GameInfo.Strings.Types[i];
                if (!string.IsNullOrEmpty(name))
                {
                    typesList.Add(new { id = i, name });
                }
            }

            return SuccessResponse(new { types = typesList, count = typesList.Count });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all types: {ex}");
            return ErrorResponse("Failed to get all types");
        }
    }
}
