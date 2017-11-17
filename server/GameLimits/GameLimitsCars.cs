using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Cars", "Game Limits", "1.0.0")]
    [Description("The cars module")]

    public class GameLimitsCars : RustPlugin
    {
        /// <summary>
        /// The spawn locations for the cars to spawn.
        /// </summary>
        private List<Vector3> spawns = new List<Vector3>();

        /// <summary>
        /// The car prefab to spawn in the world
        /// </summary>
        private static readonly string carPrefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";

        /// <summary>
        /// A whitelist of monuments where cars can spawn
        /// </summary>
        private static readonly string[] spawnWhitelist = {
            "assets/bundled/prefabs/autospawn/monument/large/launch_site_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/water_treatment_plant_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/trainyard_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/military_tunnel_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/airfield_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/gas_station_1.prefab",
        };

        /// <summary>
        /// When the server is initialized, find all car spawns and set the timer
        /// </summary>
        private void OnServerInitialized()
        {
            // Find all spawn locations for the car
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
                if (spawnWhitelist.Any(s => s.Equals(monument.name)))
                    spawns.Add(monument.transform.position + new Vector3(20f, 50f, 20f));

            // Set the respawn check timer
            timer.Every(60f * 60f, () => Respawn());
            Respawn();
        }

        /// <summary>
        /// Check all spawn locations if there is a car nearby, if not, spawn a new car.
        /// </summary>
        void Respawn()
        {
            // Spawn new cars
            foreach (var spawn in spawns)
            {
                List<BaseCar> entities = Facepunch.Pool.GetList<BaseCar>();
                Vis.Entities(spawn, 150f, entities);

                if (entities.Count > 0)
                    continue;

                BaseEntity ent = (BaseCar)GameManager.server.CreateEntity(carPrefab, spawn);
                ent.Spawn();

                Puts($"Spawned new car at {spawn.x} {spawn.y} {spawn.z}");

                Facepunch.Pool.FreeList(ref entities);
            }

            // Remove cars that are placed under water
            foreach (BaseCar car in UnityEngine.Object.FindObjectsOfType<BaseCar>())
            {
                if (!car.IsMounted() && WaterLevel.Factor(car.WorldSpaceBounds().ToBounds()) > 0.7f)
                {
                    Puts($"Despawned car underwater from {car.transform.position.x} {car.transform.position.y} {car.transform.position.z}");
                    car.DieInstantly();
                }
            }
        }
    }
}
