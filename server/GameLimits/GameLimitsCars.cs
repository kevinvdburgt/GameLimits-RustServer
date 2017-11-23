using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Oxide.Game.Rust.Cui;
using System.Reflection;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Cars", "Game Limits", "1.0.0")]
    [Description("The cars module")]

    public class GameLimitsCars : RustPlugin
    {
        static GameLimitsCars plug;

        /// <summary>
        /// The list with spawn locations
        /// </summary>
        private List<Vector3> spawnLocations = new List<Vector3>();

        /// <summary>
        /// The monument names where cars will spawn
        /// </summary>
        private static readonly string[] spawnList =
        {
            "assets/bundled/prefabs/autospawn/monument/large/launch_site_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/water_treatment_plant_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/trainyard_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/military_tunnel_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/airfield_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/gas_station_1.prefab",
        };

        /// <summary>
        /// Prefab car
        /// </summary>
        private static readonly string carPrefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";

        /// <summary>
        /// Prefab chair
        /// </summary>
        private static readonly string chairPrefab = "assets/prefabs/deployable/chair/chair.deployed.prefab";

        /// <summary>
        /// Prefab explosions
        /// </summary>
        private static readonly string explosionPrefab = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";

        /// <summary>
        /// Prefab fridge
        /// </summary>
        private static readonly string fridgePrefab = "assets/prefabs/deployable/fridge/fridge.deployed.prefab";

        #region Server Events
        /// <summary>
        /// Called after the server startup has been completed and is awaiting connections
        /// </summary>
        private void OnServerInitialized()
        {
            plug = this;

            // Find spawn locations based on the spawn whitelist of monuments
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
                if (spawnList.Contains<string>(monument.name))
                    spawnLocations.Add(monument.transform.position + new Vector3(25f, 25f, 25f));

            // Restore the car controller to all cars
            foreach (BaseCar car in UnityEngine.Object.FindObjectsOfType<BaseCar>())
                car.gameObject.AddComponent<CarController>();

            // Respawn the cars
            timer.In(1f, () => RespawnCars());
        }

        private void Unload()
        {
            foreach (CarController car in UnityEngine.Object.FindObjectsOfType<CarController>())
            {
                car.container.inventory.itemList.ForEach((Item i) =>
                {
                    Puts($"Car inventory: {i.name}");
                });


                UnityEngine.Object.Destroy(car);
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null)
                return;

            if (input.WasJustPressed(BUTTON.USE))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 3f))
                {
                    CarController controller = hit.GetEntity()?.GetComponent<CarController>();

                    if (controller == null || !controller.HasDriver() || controller.occupants.Contains(player))
                        return;

                    CanMountEntity(controller.entity, player);
                    return;
                }
            }
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller != null && controller.Driver?.userID == player.userID)
            {
                controller.Driver = null;
                controller.mountPoints[0].OnEntityDismounted();
                return;
            }

            CarController.InvisibleMount invisibleMount = mountable.GetComponent<CarController.InvisibleMount>();
            if (invisibleMount != null)
                invisibleMount.MountPostion.OnEntityDismounted();
        }

        private object CanMountEntity(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller != null)
            {
                if (player.isMounted)
                    return false;

                if (controller.isDying)
                    return false;

                CarController.MountPoint mountPoint = controller.GetClosetMountPoint(player.transform.position);
                if(mountPoint != null && !mountPoint.Entity.IsMounted())
                {
                    mountPoint.MountPlayer(player);
                    return false;
                }
            }
            
            return null;
        }

        private object CanDismountEntity(BaseMountable mountable, BasePlayer player)
        {
            CarController.InvisibleMount invisibleMount = mountable.GetComponent<CarController.InvisibleMount>();
            
            if (invisibleMount != null)
            {
                invisibleMount.MountPostion.DismountPlayer();
                return false;
            }
            
            return null;
        }

        private object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            // Preventing from picking up items attached to the CarController
            if (entity.GetComponentInParent<CarController>())
                return false;

            return null;
        }
        #endregion

        #region Functions
        private void RespawnCars()
        {
            foreach (Vector3 location in spawnLocations)
            {
                // Find cars in range of the spawn location
                List<BaseCar> entities = Facepunch.Pool.GetList<BaseCar>();
                Vis.Entities((Vector3)location, 150f, entities);

                // When there already is a car in range, dont respawn it.
                if (entities.Count > 0)
                    continue;

                // Spawns a new car
                BaseEntity car = SpawnCar(location);
                Puts($"Spawned new car at {location.x}, {location.y}, {location.z}");

                Facepunch.Pool.FreeList(ref entities);
            }

            timer.In(3600f * 2f, () => RespawnCars());
        }

        private BaseEntity SpawnCar(Vector3 position)
        {
            BaseEntity entity = GameManager.server.CreateEntity(carPrefab, position + Vector3.up, default(Quaternion));
            entity.enableSaving = true;
            entity.Spawn();

            CarController controller = entity.gameObject.AddComponent<CarController>();

            return entity;
        }
        #endregion

        #region Classes
        private class CarController : MonoBehaviour
        {
            public BasePlayer player;
            public BaseCar entity;

            public List<MountPoint> mountPoints = new List<MountPoint>();
            public List<BasePlayer> occupants = new List<BasePlayer>();

            public bool isDying;

            public StorageContainer container;

            public BasePlayer Driver
            {
                get
                {
                    return player;
                }
                set
                {
                    player = value;
                }
            }

            private void Awake()
            {
                entity = GetComponent<BaseCar>();

                if (entity.IsMounted())
                    entity.DismountAllPlayers();

                CreatePassengerSeats();
                CreateTrunk();
            }

            private void FixedUpdate()
            {
                if (WaterLevel.Factor(entity.WorldSpaceBounds().ToBounds()) > 0.7f && !HasDriver())
                {
                    enabled = false;
                    StopToDie();
                    entity.SetFlag(BaseEntity.Flags.Reserved1, false, false);
                    return;
                }

                if (player != null)
                    foreach (MountPoint mountPoint in mountPoints)
                        mountPoint.UpdatePlayerPosition();
            }

            #region Functions
            private void CreatePassengerSeats()
            {
                mountPoints = new List<MountPoint>
                {
                    new MountPoint(this, new Vector3(-0.5f, 0.16f, 0.57f), new Vector3(-1.2f, 0.1f, 0.5f), true),
                    new MountPoint(this, new Vector3(0.54f, 0.16f, 0.57f), new Vector3(1.2f, 0.1f, 0.5f)),
                    new MountPoint(this, new Vector3(-0.48f, 0.16f, -0.55f), new Vector3(-1.2f, 0.1f, -0.5f)),
                    new MountPoint(this, new Vector3(0.48f, 0.16f, -0.55f), new Vector3(1.2f, 0.1f, -0.5f)),
                };
            }

            private void CreateTrunk()
            {
                container = (StorageContainer)GameManager.server.CreateEntity(fridgePrefab, entity.transform.position, entity.transform.rotation, false);
                container.enableSaving = false;
                container.skinID = (ulong)886416273;
                container.Spawn();

                container.GetComponent<DestroyOnGroundMissing>().enabled = false;
                container.GetComponent<GroundWatch>().enabled = false;

                container.SetParent(entity);
                container.transform.localPosition = new Vector3(1f, 0.85f, -1.8f);
                container.transform.localRotation = Quaternion.Euler(263f + 180f, 0, 90f);

                container.panelName = "generic";
                container.isLockable = false;
                container.displayHealth = false;
                container.pickup.enabled = false;
                container.onlyAcceptCategory = ItemCategory.All;
            }

            public bool HasDriver() => player != null;

            public void EjectAllPlayers()
            {
                foreach (MountPoint mountPoint in mountPoints)
                    mountPoint.DismountPlayer();
            }

            public MountPoint GetClosetMountPoint(Vector3 position)
            {
                MountPoint mountPoint = null;
                float closestDistance = 10f;
                foreach (MountPoint point in mountPoints)
                {
                    float distance = Vector3.Distance(point.Position, position);
                    if (distance < closestDistance)
                    {
                        if (point.Entity.IsMounted())
                            continue;

                        mountPoint = point;
                        closestDistance = distance;
                    }
                }
                return mountPoint;
            }

            private void DestroyAllMounts()
            {
                foreach (MountPoint mountPoint in mountPoints)
                    mountPoint.DestroyMountPoint();
            }

            public void StopToDie()
            {
                enabled = false;
                isDying = true;

                if (entity != null)
                {
                    entity.SetFlag(BaseEntity.Flags.Reserved1, false, false);
                    foreach (var wheel in entity.wheels)
                    {
                        wheel.wheelCollider.motorTorque = 0;
                        wheel.wheelCollider.brakeTorque = float.MaxValue;
                    }
                }

                EjectAllPlayers();
                InvokeHandler.Invoke(this, OnDeath, 5f);
            }

            private void OnDeath()
            {
                enabled = false;

                EjectAllPlayers();
                DestroyAllMounts();

                Effect.server.Run(explosionPrefab, transform.position);

                container.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", transform.position + Vector3.up + (UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2f, 3f)), new Quaternion());

                plug.NextTick(() =>
                {
                    if (entity != null && !entity.IsDestroyed)
                        entity.DieInstantly();

                    Destroy(this);
                });
            }

            void OnDestroy()
            {
                EjectAllPlayers();
                DestroyAllMounts();
                container.Invoke("KillMessage", 0.1f);
                Destroy(this);
            }
            #endregion

            #region Classes
            public class MountPoint
            {
                private BaseMountable entity;
                private BasePlayer player;
                private CarController controller;
                private bool isDriver;
                private Vector3 offset;
                private Vector3 dismountLocal;

                public Vector3 Position
                {
                    get
                    {
                        if (entity == null && !isDriver)
                            GeneratePrefab();

                        return isDriver ? entity.transform.position + (entity.transform.forward * 0.5f) + (entity.transform.up * 0.1f) + (entity.transform.right * -0.5f) : entity.transform.position;
                    }
                }

                public BaseMountable Entity
                {
                    get
                    {
                        return entity;
                    }
                }

                public bool IsDriver
                {
                    get
                    {
                        return isDriver;
                    }
                }

                public MountPoint(CarController controller, Vector3 offset, Vector3 dismountLocal, bool isDriver = false)
                {
                    this.controller = controller;
                    this.isDriver = isDriver;
                    this.offset = offset;
                    this.dismountLocal = dismountLocal;

                    if (isDriver)
                    {
                        entity = controller.entity;
                    }
                    else
                    {
                        GeneratePrefab();
                    }

                }

                public void GeneratePrefab()
                {
                    entity = (BaseMountable)GameManager.server.CreateEntity(controller.entity.chairRef.resourcePath, controller.transform.position);
                    entity.enableSaving = false;
                    entity.skinID = (ulong)1169930802;
                    entity.Spawn();

                    entity.maxMountDistance = 2f;

                    entity.GetComponent<DestroyOnGroundMissing>().enabled = false;
                    entity.GetComponent<GroundWatch>().enabled = false;
                    entity.GetComponent<MeshCollider>().convex = true;

                    InvisibleMount invisibleMount = entity.gameObject.AddComponent<InvisibleMount>();
                    invisibleMount.MountPostion = this;

                    entity.SetParent(controller.entity);
                    entity.transform.localPosition = offset;
                }

                public void OnEntityMounted()
                {
                    controller.occupants.Add(player);
                    
                    if (isDriver)
                    {
                        controller.Driver = player;
                        // controller.OnDriverMounted();
                    }
                }

                public void OnEntityDismounted()
                {
                    controller.occupants.Remove(player);
                    player = null;
                }

                public void MountPlayer(BasePlayer player)
                {
                    this.player = player;
                    player.EnsureDismounted();
                    typeof(BaseMountable).GetField("_mounted", (BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(entity, player);
                    player.MountObject(entity, 0);
                    player.MovePosition(entity.mountAnchor.transform.position);
                    player.transform.rotation = entity.mountAnchor.rotation;
                    player.OverrideViewAngles(entity.mountAnchor.transform.rotation.eulerAngles);
                    player.eyes.NetworkUpdate(entity.mountAnchor.transform.rotation);
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
                    entity.SetFlag(BaseEntity.Flags.Busy, true, false);
                    
                    OnEntityMounted();
                }
                
                public void DismountPlayer()
                {
                    if (player == null)
                        return;

                    Vector3 dismountPosition = controller.entity.transform.position +
                        (controller.entity.transform.right * dismountLocal.x) +
                        (controller.entity.transform.up * 0.1f) +
                        (controller.entity.transform.forward * dismountLocal.z);

                    if (TerrainMeta.HeightMap.GetHeight(dismountPosition) > dismountPosition.y)
                        dismountPosition.y = TerrainMeta.HeightMap.GetHeight(dismountPosition) + 0.5f;

                    Quaternion dismountRotation = entity.dismountAnchor.rotation;
                    player.DismountObject();

                    player.transform.rotation = dismountRotation;
                    player.MovePosition(dismountPosition);
                    player.eyes.NetworkUpdate(dismountRotation);
                    player.SendNetworkUpdateImmediate(false);
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", dismountPosition);
                    typeof(BaseMountable).GetField("_mounted", (BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(entity, null);
                    entity.SetFlag(BaseEntity.Flags.Busy, false, false);

                    OnEntityDismounted();
                }

                public void UpdatePlayerPosition()
                {
                    if (player == null)
                        return;

                    player.MovePosition(entity.mountAnchor.transform.position);
                }

                public void DestroyMountPoint()
                {
                    if (isDriver)
                        return;

                    if (entity.IsMounted())
                        DismountPlayer();

                    if (entity != null && !entity.IsDestroyed)
                    {
                        Destroy(entity.GetComponent<InvisibleMount>());
                        entity.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
            }

            public class InvisibleMount : MonoBehaviour
            {
                private BaseMountable entity;
                private MountPoint mountPoint;

                private void Awake()
                {
                    entity = GetComponent<BaseMountable>();
                    enabled = false;
                }

                public MountPoint MountPostion
                {
                    get
                    {
                        return mountPoint;
                    }
                    set
                    {
                        mountPoint = value;
                    }
                }
            }
            #endregion
        }
        #endregion


        #region OLD
        /*
        private List<Vector3> spawns = new List<Vector3>();

        static GameLimitsCars ins;

        private static readonly string carPrefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
        private static readonly string chairPrefab = "assets/prefabs/deployable/chair/chair.deployed.prefab";
        private static readonly string explosionPrefab = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";

        static int colliderRemovable = LayerMask.GetMask("Construction", "Deployed", "Default");

        #region Server Events
        private void OnServerInitialized()
        {
            ins = this;

            // Find all spawn locations for the car
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
                if (spawnWhitelist.Any(s => s.Equals(monument.name)))
                    spawns.Add(monument.transform.position + new Vector3(20f, 50f, 20f));

            // Set the respawn check timer
            timer.Every(60f * 60f * 6f, () => Respawn());
            Respawn();

            // Restore all car controllers
            var objects = UnityEngine.Object.FindObjectsOfType<BaseCar>();
            if (objects != null)
                foreach (var obj in objects)
                    obj.gameObject.AddComponent<CarController>();
        }

        private void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<CarController>();
            if (objects != null)
                foreach (var obj in objects)
                    UnityEngine.Object.Destroy(obj);
        }

        //[ChatCommand("test")]
        //private void OnChatCommandTest(BasePlayer player, string command, string[] args)
        //{
        //    SpawnCar(player.transform.position);
        //}

        private object CanMountEntity(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();

            if (controller == null || player == null)
                return null;
            
            if (player.isMounted || controller.isDying)
                return false;

            return null;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || !input.WasJustPressed(BUTTON.USE))
                return;

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 3f, colliderRemovable))
                return;

            if (!Physics.SphereCast(player.eyes.position, 0.1f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 0.5f))
                return;

            CarController controller = hit.GetEntity()?.GetComponent<CarController>();
            if (controller == null)
                return;

            controller.MountPlayer(player);
        }

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller == null)
                controller = mountable.GetParentEntity().GetComponent<CarController>();

            if (controller == null || controller.car == null)
                return;

            controller.OnPlayerEnter(player);
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller == null)
                controller = mountable.GetParentEntity().GetComponent<CarController>();

            if (controller == null || controller.car == null)
                return;

            controller.OnPlayerExit(player);
        }

        bool CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            if (player == null || entity == null || entity.GetParentEntity() == null)
                return true;

            CarController controller = entity.GetParentEntity().GetComponent<CarController>();
            if (controller == null)
                return true;

            return false;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            if (entity.GetComponent<CarController>())
                entity.GetComponent<CarController>().OnDamage(info);
        }
        #endregion

        #region Functions
        void Respawn()
        {
            // Spawn new cars
            foreach (var spawn in spawns)
            {
                List<BaseCar> entities = Facepunch.Pool.GetList<BaseCar>();
                Vis.Entities(spawn, 150f, entities);

                if (entities.Count > 0)
                    continue;

                BaseEntity ent = SpawnCar(spawn);
                Puts($"Spawned new car at {spawn.x} {spawn.y} {spawn.z}");

                Facepunch.Pool.FreeList(ref entities);
            }

            // Remove cars that are placed under water
            var objects = UnityEngine.Object.FindObjectsOfType<CarController>();
            if (objects != null)
                foreach (var obj in objects)
                    obj.CheckWaterLevel();
        }

        private BaseEntity SpawnCar(Vector3 position, Quaternion rotation = default(Quaternion), bool enableSaving = false)
        {
            BaseCar car = (BaseCar)GameManager.server.CreateEntity(carPrefab, position + Vector3.up, rotation);
            car.enableSaving = enableSaving;
            car.Spawn();

            CarController controller = car.gameObject.AddComponent<CarController>();

            return car;
        }
        #endregion

        #region GUI
        private void CreateGUI(BasePlayer player, CarController controller)
        {
            DestroyGUI(player);

            var container = UI.CreateElementContainer("gl_carhud", "0 0 0 0.95", "0.35 0.96", "0.65 1", false, "Under");

            UI.CreateLabel(ref container, "gl_carhud", "HP: 34", 12, "0 0", "1 1", TextAnchor.MiddleCenter, "1 1 1 1");

            CuiHelper.AddUi(player, container);
        }

        private void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_carhud");
        }
        #endregion

        #region Classes
        public class CarController : MonoBehaviour
        {
            public BaseCar car;
            public Vector3 position;
            public Quaternion rotation;
            public BaseEntity passengerSeat1;
            public BaseEntity passengerSeat2;
            public BaseEntity passengerSeat3;
            public BaseEntity box;
            public bool isDying;
            public bool deleteCar;
            public Transform[] dismounts;

            private void Awake()
            {
                car = GetComponent<BaseCar>();
                position = car.transform.position;
                rotation = car.transform.rotation;

                // Passenger seat 1 (front)
                passengerSeat1 = GameManager.server.CreateEntity(chairPrefab, position, rotation, false);
                passengerSeat1.transform.localEulerAngles = new Vector3(0, 0, 0);
                passengerSeat1.transform.localPosition = new Vector3(0.57f, 0.1f, 0.55f);
                passengerSeat1.SetParent(car, 0U);
                passengerSeat1.Spawn();
                ((BaseMountable)passengerSeat1).dismountPositions = car.dismountPositions;

                // Passenger seat 2
                passengerSeat2 = GameManager.server.CreateEntity(chairPrefab, position, rotation, false);
                passengerSeat2.transform.localEulerAngles = new Vector3(0, 0, 0);
                passengerSeat2.transform.localPosition = new Vector3(0.57f, 0.1f, -0.55f);
                passengerSeat2.SetParent(car, 0U);
                passengerSeat2.Spawn();
                ((BaseMountable)passengerSeat2).dismountPositions = car.dismountPositions;

                // Passenger seat 3
                passengerSeat3 = GameManager.server.CreateEntity(chairPrefab, position, rotation, false);
                passengerSeat3.transform.localEulerAngles = new Vector3(0, 0, 0);
                passengerSeat3.transform.localPosition = new Vector3(-0.57f, 0.1f, -0.55f);
                passengerSeat3.SetParent(car, 0U);
                passengerSeat3.Spawn();
                ((BaseMountable)passengerSeat3).dismountPositions = car.dismountPositions;

                // Fridge container
                box = GameManager.server.CreateEntity("assets/prefabs/deployable/fridge/fridge.deployed.prefab", position, rotation, false);
                box.transform.localEulerAngles = new Vector3(263f, 0, 90f);
                box.transform.localPosition = new Vector3(1f, 0.9f, -1.8f);
                box.SetParent(car, 0U);
                box.Spawn();
                ((StorageContainer)box).onlyAcceptCategory = ItemCategory.All;

                // Health management
                isDying = false;
                deleteCar = false;
            }

            private void Update()
            {

            }
            
            public void OnPlayerEnter(BasePlayer player)
            {
                //ins.CreateGUI(player, this);
                
                //player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                //player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            }

            public void OnPlayerExit(BasePlayer player)
            {
                //ins.DestroyGUI(player);
                //player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                //player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            }

            public bool MountPlayer(BasePlayer player)
            {
                //if (player.isMounted)
                //    return false;

                if (!car.IsMounted())
                {
                    car.MountPlayer(player);
                    return true;
                }
                else if (((BaseMountable)passengerSeat1).IsMounted() == false)
                {
                    ((BaseMountable)passengerSeat1).MountPlayer(player);
                    player.SetParent(passengerSeat1, 0);
                    return true;
                }
                else if (((BaseMountable)passengerSeat2).IsMounted() == false)
                {
                    ((BaseMountable)passengerSeat2).MountPlayer(player);
                    player.SetParent(passengerSeat2, 0);
                    return true;
                }
                else if (((BaseMountable)passengerSeat3).IsMounted() == false)
                {
                    ((BaseMountable)passengerSeat3).MountPlayer(player);
                    player.SetParent(passengerSeat3, 0);
                    return true;
                }

                return false;
            }

            public bool IsMounted()
            {
                return (car.IsMounted() || ((BaseMountable)passengerSeat1).IsMounted() || ((BaseMountable)passengerSeat2).IsMounted() || ((BaseMountable)passengerSeat3).IsMounted());
            }

            public void CheckWaterLevel()
            {
                if (WaterLevel.Factor(car.WorldSpaceBounds().ToBounds()) > 0.7f && !IsMounted())
                {
                    deleteCar = true;
                    StopToDie();
                }
            }

            public void OnDamage(HitInfo info)
            {
                if (isDying)
                {
                    info.damageTypes = new Rust.DamageTypeList();
                    info.HitEntity = null;
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                    return;
                }

                if (info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Bullet)
                    info.damageTypes.ScaleAll(200);

                if (info.damageTypes.Total() >= car.health)
                {
                    isDying = true;
                    info.damageTypes = new Rust.DamageTypeList();
                    info.HitEntity = null;
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                    OnDestroy();
                    return;
                }

                ins.Puts($"CAR HP: {car.health}");
            }

            public void StopToDie()
            {
                if (car != null)
                {
                    car.SetFlag(BaseEntity.Flags.Reserved1, false, false);

                    foreach (var wheel in car.wheels)
                    {
                        wheel.wheelCollider.motorTorque = 0;
                        wheel.wheelCollider.brakeTorque = float.MaxValue;
                    }

                    car.GetComponent<Rigidbody>().velocity = Vector3.zero;
                }
                OnDestroy();
            }

            void OnDestroy()
            {
                isDying = true;

                BaseEntity.saveList.Remove(passengerSeat1);
                if (passengerSeat1 != null)
                    passengerSeat1.Invoke("KillMessage", 0.1f);

                BaseEntity.saveList.Remove(passengerSeat2);
                if (passengerSeat2 != null)
                    passengerSeat2.Invoke("KillMessage", 0.1f);

                BaseEntity.saveList.Remove(passengerSeat3);
                if (passengerSeat3 != null)
                    passengerSeat3.Invoke("KillMessage", 0.1f);

                BaseEntity.saveList.Remove(box);
                if (box != null)
                    box.Invoke("KillMessage", 0.1f);

                InvokeHandler.Invoke(this, () =>
                {
                    Effect.server.Run(explosionPrefab, transform.position);

                    ins.NextTick(() =>
                    {
                        if (car != null && !car.IsDestroyed && deleteCar)
                            car.DieInstantly();
                        GameObject.Destroy(this);
                    });

                }, 1f);
            }
        }
        #endregion
        */
        #endregion
    }

}
