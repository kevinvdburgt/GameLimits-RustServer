using static Oxide.Plugins.GameLimits;
using System.Net;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Cars", "Game Limits", "1.0.0")]
    [Description("The cars module")]

    public class GameLimitsCars : RustPlugin
    {
        private List<Vector3> spawns = new List<Vector3>();

        const string carPrefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";

        void OnServerInitialized()
        {
            // Find all spawn locations for the car
            var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            Vector3 offset = new Vector3(20f, 20f, 20f);
            foreach (var monument in monuments)
                spawns.Add(monument.transform.position + offset);

            // Set the respawn check timer
            timer.Every(60f * 60f, () => Respawn());
            Respawn();
        }

        void Respawn()
        {
            Log("Cars", "Checking for cars to respawn", GameLimits.LogType.INFO);

            foreach (var spawn in spawns)
            {
                List<BaseCar> entities = Facepunch.Pool.GetList<BaseCar>();
                Vis.Entities(spawn, 100f, entities);

                if (entities.Count > 0)
                    continue;

                Log("Cars", $"Spawning new car at {spawn.x} {spawn.y} {spawn.z}", GameLimits.LogType.OK);

                BaseEntity ent = (BaseCar)GameManager.server.CreateEntity(carPrefab, spawn);
                ent.Spawn();
            }
        }
    }
}

// 76561198176419121
