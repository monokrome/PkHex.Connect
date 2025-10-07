using PKHeX.Core;
using Newtonsoft.Json;
using PKHeXWebSocketServer.Metadata;

namespace PKHeXWebSocketServer.Services;

public class WorldFacilitiesService : ServiceBase
{
    [ApiAction("getSecretBase", ApiCategory.World, "Get Secret Base data (Gen 3/4)", RequiresSession = true)]
    public string GetSecretBase(SaveFile save)
    {
        try
        {
            if (save.Generation != 3 && save.Generation != 4 && save.Generation != 6)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Secret Bases are only supported in Generation 3, 4 (limited), and 6 (ORAS). Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var saveType = save.GetType();
            var secretBaseProp = saveType.GetProperty("SecretBase");
            
            if (secretBaseProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Secret Base data is not available for this save file. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var secretBase = secretBaseProp.GetValue(save);
            if (secretBase == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    hasSecretBase = false,
                    message = "No Secret Base data found"
                });
            }

            var secretBaseType = secretBase.GetType();
            var secretBaseData = new Dictionary<string, object>
            {
                ["generation"] = save.Generation
            };

            var propertiesToExtract = new[]
            {
                "Location", "LocationIndex", "RouteLocation",
                "RegistryStatus", "OwnerName", "OwnerGender", "OwnerID", "OwnerSID",
                "Decorations", "DecorationCount", "DecorationsPlaced",
                "Team", "TeamCount", "BattleType",
                "SecretBaseName", "Flags", "Language"
            };

            foreach (var propName in propertiesToExtract)
            {
                var prop = secretBaseType.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(secretBase);
                        if (value != null)
                        {
                            if (value is Array arr)
                            {
                                secretBaseData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = arr.Length;
                            }
                            else
                            {
                                secretBaseData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                            }
                        }
                    }
                    catch { }
                }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                hasSecretBase = true,
                generation = save.Generation,
                secretBase = secretBaseData
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get Secret Base data: {ex.Message}" });
        }
    }

    [ApiAction("getEntralinkData", ApiCategory.World, "Get Entralink/Join Avenue data (Gen 5)", RequiresSession = true)]
    public string GetEntralinkData(SaveFile save)
    {
        try
        {
            if (save.Generation != 5)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Entralink is only supported in Generation 5. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var saveType = save.GetType();
            var entralinkProp = saveType.GetProperty("Entralink");
            
            if (entralinkProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Entralink data is not available for this save file. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var entralink = entralinkProp.GetValue(save);
            if (entralink == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    hasEntralink = false,
                    message = "No Entralink data found"
                });
            }

            var entralinkType = entralink.GetType();
            var entralinkData = new Dictionary<string, object>
            {
                ["generation"] = save.Generation
            };

            var propertiesToExtract = new[]
            {
                "PassPower", "PassPowerCount", "PassPowerFlags",
                "ForestLevel", "ForestType", "WhiteForest", "BlackCity",
                "MissionStatus", "MissionCount", "MissionFlags",
                "BridgePass", "EntreeForestLevel", "EntreeForestArea",
                "FunfestMissions", "PassOrbs", "PassPowerActivate"
            };

            foreach (var propName in propertiesToExtract)
            {
                var prop = entralinkType.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(entralink);
                        if (value != null)
                        {
                            if (value is Array arr)
                            {
                                entralinkData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = arr.Length;
                            }
                            else
                            {
                                entralinkData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                            }
                        }
                    }
                    catch { }
                }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                hasEntralink = true,
                generation = save.Generation,
                entralink = entralinkData
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get Entralink data: {ex.Message}" });
        }
    }

    [ApiAction("getFestivalPlaza", ApiCategory.World, "Get Festival Plaza data (Gen 7)", RequiresSession = true)]
    public string GetFestivalPlaza(SaveFile save)
    {
        try
        {
            if (save.Generation != 7)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Festival Plaza is only supported in Generation 7. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var saveType = save.GetType();
            var festivalPlazaProp = saveType.GetProperty("FestivalPlaza");
            
            if (festivalPlazaProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Festival Plaza data is not available for this save file. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var plaza = festivalPlazaProp.GetValue(save);
            if (plaza == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    hasFestivalPlaza = false,
                    message = "No Festival Plaza data found"
                });
            }

            var plazaType = plaza.GetType();
            var plazaData = new Dictionary<string, object>
            {
                ["generation"] = save.Generation
            };

            var propertiesToExtract = new[]
            {
                "Rank", "FestaCoins", "FC",
                "FacilityColor", "FacilityIntroduceCount",
                "Facility1", "Facility2", "Facility3", "Facility4", "Facility5", "Facility6", "Facility7",
                "FacilityType1", "FacilityType2", "FacilityType3", "FacilityType4", "FacilityType5", "FacilityType6", "FacilityType7",
                "ThemeIndex", "MessageIndex", "UsedFestaCoins", "TotalFestaCoins"
            };

            foreach (var propName in propertiesToExtract)
            {
                var prop = plazaType.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(plaza);
                        if (value != null)
                        {
                            plazaData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                        }
                    }
                    catch { }
                }
            }

            var facilitiesProp = plazaType.GetProperty("Facilities");
            if (facilitiesProp != null)
            {
                try
                {
                    var facilities = facilitiesProp.GetValue(plaza);
                    if (facilities is Array facilityArray)
                    {
                        var facilityList = new List<object>();
                        for (int i = 0; i < facilityArray.Length; i++)
                        {
                            var facility = facilityArray.GetValue(i);
                            if (facility != null)
                            {
                                var facilityType = facility.GetType();
                                var facilityData = new Dictionary<string, object?>();
                                
                                var facProps = new[] { "Type", "Index", "Color", "NPC" };
                                foreach (var fp in facProps)
                                {
                                    var facProp = facilityType.GetProperty(fp);
                                    if (facProp != null)
                                    {
                                        try
                                        {
                                            facilityData[fp.ToLower()] = facProp.GetValue(facility);
                                        }
                                        catch { }
                                    }
                                }
                                
                                if (facilityData.Count > 0)
                                {
                                    facilityList.Add(facilityData);
                                }
                            }
                        }
                        
                        if (facilityList.Count > 0)
                        {
                            plazaData["facilities"] = facilityList;
                        }
                    }
                }
                catch { }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                hasFestivalPlaza = true,
                generation = save.Generation,
                festivalPlaza = plazaData
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get Festival Plaza data: {ex.Message}" });
        }
    }

    [ApiAction("getPokePelago", ApiCategory.World, "Get Poké Pelago data (Gen 7)", RequiresSession = true)]
    public string GetPokePelago(SaveFile save)
    {
        try
        {
            if (save.Generation != 7)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Poké Pelago is only supported in Generation 7. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var saveType = save.GetType();
            var pokePelagoProp = saveType.GetProperty("PokePelago");
            
            if (pokePelagoProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Poké Pelago data is not available for this save file. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var pelago = pokePelagoProp.GetValue(save);
            if (pelago == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    hasPokePelago = false,
                    message = "No Poké Pelago data found"
                });
            }

            var pelagoType = pelago.GetType();
            var pelagoData = new Dictionary<string, object>
            {
                ["generation"] = save.Generation
            };

            var propertiesToExtract = new[]
            {
                "Isle1Level", "Isle2Level", "Isle3Level", "Isle4Level", "Isle5Level",
                "Isle1Development", "Isle2Development", "Isle3Development", "Isle4Development", "Isle5Development",
                "BeanPouch", "BeanCount", "PlainBeans", "PatternedBeans", "RainbowBeans",
                "TotalBeansCollected", "ExplorationTime", "HotSpringTime", "TrainingTime",
                "HarvestTime", "PokemonCount", "Visitors"
            };

            foreach (var propName in propertiesToExtract)
            {
                var prop = pelagoType.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(pelago);
                        if (value != null)
                        {
                            pelagoData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                        }
                    }
                    catch { }
                }
            }

            var islandsProp = pelagoType.GetProperty("Islands");
            if (islandsProp != null)
            {
                try
                {
                    var islands = islandsProp.GetValue(pelago);
                    if (islands is Array islandArray)
                    {
                        var islandList = new List<object>();
                        for (int i = 0; i < islandArray.Length; i++)
                        {
                            var island = islandArray.GetValue(i);
                            if (island != null)
                            {
                                var islandType = island.GetType();
                                var islandData = new Dictionary<string, object?>();
                                
                                var islandProps = new[] { "Level", "DevelopmentLevel", "Type", "PokemonCount" };
                                foreach (var ip in islandProps)
                                {
                                    var islandProp = islandType.GetProperty(ip);
                                    if (islandProp != null)
                                    {
                                        try
                                        {
                                            islandData[ip.ToLower()] = islandProp.GetValue(island);
                                        }
                                        catch { }
                                    }
                                }
                                
                                if (islandData.Count > 0)
                                {
                                    islandList.Add(islandData);
                                }
                            }
                        }
                        
                        if (islandList.Count > 0)
                        {
                            pelagoData["islands"] = islandList;
                        }
                    }
                }
                catch { }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                hasPokePelago = true,
                generation = save.Generation,
                pokePelago = pelagoData
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get Poké Pelago data: {ex.Message}" });
        }
    }

    [ApiAction("getPokeJobs", ApiCategory.World, "Get Pokémon Jobs data (Gen 8)", RequiresSession = true)]
    public string GetPokeJobs(SaveFile save)
    {
        try
        {
            if (save.Generation != 8)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Pokémon Jobs are only supported in Generation 8. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var saveType = save.GetType();
            var pokeJobsProp = saveType.GetProperty("PokeJobs");
            
            if (pokeJobsProp == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Pokémon Jobs data is not available for this save file. Check the 'features' array from loadSave to see available capabilities."
                });
            }

            var pokeJobs = pokeJobsProp.GetValue(save);
            if (pokeJobs == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    hasPokeJobs = false,
                    message = "No Pokémon Jobs data found"
                });
            }

            var pokeJobsType = pokeJobs.GetType();
            var pokeJobsData = new Dictionary<string, object>
            {
                ["generation"] = save.Generation
            };

            var propertiesToExtract = new[]
            {
                "JobCount", "ActiveJobs", "CompletedJobs",
                "TotalJobsCompleted", "TotalPokemonSent",
                "Job1Status", "Job2Status", "Job3Status", "Job4Status", "Job5Status",
                "Job1TimeRemaining", "Job2TimeRemaining", "Job3TimeRemaining", "Job4TimeRemaining", "Job5TimeRemaining",
                "Job1Reward", "Job2Reward", "Job3Reward", "Job4Reward", "Job5Reward"
            };

            foreach (var propName in propertiesToExtract)
            {
                var prop = pokeJobsType.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(pokeJobs);
                        if (value != null)
                        {
                            pokeJobsData[char.ToLowerInvariant(propName[0]) + propName.Substring(1)] = value;
                        }
                    }
                    catch { }
                }
            }

            var jobsProp = pokeJobsType.GetProperty("Jobs");
            if (jobsProp != null)
            {
                try
                {
                    var jobs = jobsProp.GetValue(pokeJobs);
                    if (jobs is Array jobArray)
                    {
                        var jobList = new List<object>();
                        for (int i = 0; i < jobArray.Length; i++)
                        {
                            var job = jobArray.GetValue(i);
                            if (job != null)
                            {
                                var jobType = job.GetType();
                                var jobData = new Dictionary<string, object?>
                                {
                                    ["index"] = i
                                };
                                
                                var jobProps = new[] { "Status", "TimeRemaining", "CompletionTime", "Reward", "Type", "PokemonCount", "IsActive", "IsCompleted" };
                                foreach (var jp in jobProps)
                                {
                                    var jobProp = jobType.GetProperty(jp);
                                    if (jobProp != null)
                                    {
                                        try
                                        {
                                            jobData[jp.ToLower()] = jobProp.GetValue(job);
                                        }
                                        catch { }
                                    }
                                }
                                
                                if (jobData.Count > 1)
                                {
                                    jobList.Add(jobData);
                                }
                            }
                        }
                        
                        if (jobList.Count > 0)
                        {
                            pokeJobsData["jobs"] = jobList;
                        }
                    }
                }
                catch { }
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                hasPokeJobs = true,
                generation = save.Generation,
                pokeJobs = pokeJobsData
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = $"Failed to get Pokémon Jobs data: {ex.Message}" });
        }
    }
}
