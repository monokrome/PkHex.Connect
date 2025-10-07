# PKHeX WebSocket Server


## Overview

A cross-platform WebSocket server exposing PKHeX.Core functionality to read and manipulate Pokémon save files. This enables applications in any language to interact with PKHeX's core library, offering broad compatibility beyond Windows or C#.


## Features

- **Cross-Platform WebSocket API**: JSON-based protocol for PKHeX.Core operations
- **Multi-Session Support**: Concurrent save file handling with unique session IDs
- **Self-Documenting**: Auto-generated AsyncAPI 2.6.0 schema and HTML documentation
- **Comprehensive Coverage**: Full access to PKHeX.Core capabilities including:
  - Pokémon manipulation (read, write, modify, move, legality check)
  - Trainer information and badges
  - Items and Pokédex management
  - Event flags and mystery gifts
  - Daycare data
  - Game data lookups (species, moves, abilities, items, natures, types)
  - Hall of Fame timing and trainer records


## Architecture

**Core Components:**

- **WebSocketHandler.cs**: Manages connections and processes JSON messages
- **PKHeXService.cs**: Coordinates operations, delegating to specialized services
- **Services/**: Modular classes for specific functionalities:
  - `SaveFileService` - Save file loading/saving
  - `PokemonService` - Pokemon manipulation
  - `TrainerService` - Trainer info, badges, and playtime
  - `LegalityService` - Legality checking
  - `BoxService` - Box names and wallpapers
  - `ItemService` - Item management
  - `PokedexService` - Pokedex status
  - `EventService` - Event flags/constants
  - `DaycareService` - Daycare data
  - `MysteryGiftService` - Mystery gift data
  - `GameDataService` - Game data lookups
  - `RecordsService` - Trainer statistics and records
- **Metadata/**: API documentation infrastructure
  - `ApiDocumentationAttributes.cs` - Annotation attributes
  - `SchemaBuilder.cs` - AsyncAPI schema generation
  - `HtmlDocumentationGenerator.cs` - HTML documentation


## API Uniformity

All Pokémon-related endpoints return consistent data structures following REST-like uniformity principles. Every Pokemon object includes:

- `pid` - Personality ID
- `species`, `nickname`, `level`
- `isEgg`, `isShiny`, `gender`
- `nature`, `ability`, `heldItem`
- `moves` - All 4 moves
- `stats` - Current/max HP and all battle stats
- `ivs` - Individual values
- `evs` - Effort values
- `originalTrainer`, `trainerID`, `secretID`
- `ball`, `metLevel`, `metLocation`


## Quick Start

1. **Run the server:**
   ```bash
   dotnet run
   ```

2. **Access documentation:**
   - Open `http://localhost:8080/` for HTML documentation
   - Send `OPTIONS /` for AsyncAPI 2.6.0 schema

3. **Connect via WebSocket:**
   ```javascript
   const ws = new WebSocket('ws://localhost:8080/');
   ```

4. **Load a save file:**
   ```json
   {
     "action": "loadSave",
     "base64Data": "<base64-encoded-save-file>"
   }
   ```


## API Categories

- **Save File**: `loadSave`, `saveSave`, `getSaveInfo`, `listSessions`, `unloadSession`
- **Pokémon Read**: `getPokemon`, `getAllPokemon`, `checkLegality`, `exportShowdown`
- **Pokémon Write**: `setPokemon`, `modifyPokemon`, `deletePokemon`, `movePokemon`, `importShowdown`, `legalizePokemon`
- **Trainer**: `getTrainerInfo`, `setTrainerInfo`, `getSecondsToStart`, `setSecondsToStart`, `getSecondsToFame`, `setSecondsToFame`
- **Party Pokémon**: `getParty`, `getPartySlot`, `setPartySlot`, `deletePartySlot`
- **Box**: `getBoxNames`, `getBoxWallpapers`, `setBoxWallpaper`
- **Item**: `getItems`, `giveItem`, `removeItem`
- **Pokédex**: `getPokedex`, `setPokedexCaught`, `setPokedexSeen`
- **Badges**: `getBadges`, `setBadge`
- **Events/Progress**: `getEventFlag`, `setEventFlag`, `getEventConst`, `setEventConst`
- **Daycare**: `getDaycare`
- **Mystery Gift**: `getMysteryGifts`, `getMysteryGiftFlags`
- **Game Data Lookups**: `getSpeciesName`, `getMoveName`, `getAbilityName`, `getItemName`, `getNatureName`, `getTypeName`, `getAllSpecies`, `getAllMoves`, `getAllAbilities`, `getAllItems`, `getAllNatures`, `getAllTypes`
- **Trainer Records**: `getRecords`, `setRecord`, `getRecordValue`
- **Misc**: `getSecondsPlayed`, `setGameTime`


## Dependencies

- **.NET 9.0**: Core development framework
- **PKHeX.Core 25.9.25**: Pokémon save file manipulation library
- **ASP.NET Core**: WebSocket server infrastructure
- **Newtonsoft.Json**: JSON serialization


## Testing

Run the test suite:
```bash
dotnet test
```


## License

This project uses PKHeX.Core which is licensed under the GPLv3.
