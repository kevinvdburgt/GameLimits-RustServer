using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rust;

namespace Oxide.Plugins
{
    [Info("Cars", "Game Limits", "2.0.0")]
    [Description("The cars plugin")]

    public class Cars : RustPlugin
    {
        static Cars plug;

        Dictionary<uint, Helper.InventoryData> trunks = new Dictionary<uint, Helper.InventoryData>();

        /// <summary>
        /// List of available spawn locations for the vehicles
        /// </summary>
        private List<Vector3> spawnLocations = new List<Vector3>();

        /// <summary>
        /// A whitelist of the monuments where cars can spawn
        /// </summary>
        private static readonly string[] spawnWhitelist =
        {
            "assets/bundled/prefabs/autospawn/monument/large/launch_site_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/water_treatment_plant_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/trainyard_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/military_tunnel_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/large/airfield_1.prefab",
            "assets/bundled/prefabs/autospawn/monument/small/gas_station_1.prefab",
        };

        private static readonly string prefabCar = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
        private static readonly string prefabChair = "assets/prefabs/deployable/chair/chair.deployed.prefab";
        private static readonly string prefabExplosion = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";
        private static readonly string prefabFridge = "assets/prefabs/deployable/fridge/fridge.deployed.prefab";

        public static int repairType;

        #region Oxide Hooks
        private void Init()
        {
            plug = this;
        }

        private void OnServerInitialized()
        {
            // Find all spawn locations
            foreach (MonumentInfo monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
                if (spawnWhitelist.Contains<string>(monument.name))
                    spawnLocations.Add(monument.transform.position + new Vector3(25f, 25f, 25f));

            // Load the car trunks
            trunks.Clear();
            Helper.DataStore.Load(ref trunks, "cartrunks");

            // Restore all car controllers
            foreach (BaseCar car in UnityEngine.Object.FindObjectsOfType<BaseCar>())
                car.gameObject.AddComponent<CarController>();

            // Find items
            repairType = ItemManager.itemList.Find(x => x.shortname == "metal.fragments")?.itemid ?? 0;

            Puts($"Found {spawnLocations.Count} spawn locations and restored {UnityEngine.Object.FindObjectsOfType<BaseCar>().Length} existing cars");

            RespawnCars();
        }

        private void Unload()
        {
            trunks.Clear();

            foreach (CarController car in UnityEngine.Object.FindObjectsOfType<CarController>())
            {
                trunks.Add(car.car.net.ID, new Helper.InventoryData(car.container.inventory));
                UnityEngine.Object.Destroy(car);
            }

            Helper.DataStore.Save(trunks, "cartrunks");
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

                    CanMountEntity(controller.car, player);
                }
            }
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller != null && controller.Driver?.userID ==  player.userID)
            {
                controller.Driver = null;
                controller.mountPoints[0].OnEntityDismounted();
                return;
            }

            CarController.InvisibleMount invisibleMount = mountable.GetComponent<CarController.InvisibleMount>();
            if (invisibleMount != null)
                invisibleMount.MountPosition.OnEntityDismounted();
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

                CarController.MountPoint mountPoint = controller.GetClosestMountPoint(player.transform.position);
                if (mountPoint != null && !mountPoint.Entity.IsMounted())
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
                invisibleMount.MountPosition.DismountPlayer();
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

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            if (entity.GetComponent<CarController>())
                entity.GetComponent<CarController>().ManageDamage(info);
            else if (entity.GetComponent<CarController.InvisibleMount>())
                NullifyDamage(info);
            else if (entity.GetComponent<StorageContainer>() && entity.GetParentEntity()?.GetComponent<CarController>())
                NullifyDamage(info);
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || info.HitEntity == null)
                return;

            CarController controller = info.HitEntity.GetComponent<CarController>();

            if (controller == null || controller.car == null)
                return;

            if (controller.car.health < controller.car.MaxHealth())
            {
                if (player.inventory.GetAmount(repairType) >= 50)
                {
                    player.inventory.Take(null, repairType, 50);
                    controller.car.Heal(30);
                    player.Command("note.inv", new object[] { repairType, 50 * -1 });

                    player.ChatMessage($"Car has been repaired for {(int)(controller.car.health / controller.car.MaxHealth() * 100)}%!");
                }
                else player.ChatMessage("Car repair costs 10 metal fragments per hit");
            }
            else player.ChatMessage("Car has been repaired for 100%!");
        }
        #endregion

        #region Functions
        private void RespawnCars()
        {
            foreach (Vector3 location in spawnLocations)
            {
                // Detect if there are any cars nearby
                List<BaseCar> entities = Facepunch.Pool.GetList<BaseCar>();
                Vis.Entities((Vector3)location, 150f, entities);

                // When there are cars around, stop spawing
                if (entities.Count > 0)
                    continue;

                Facepunch.Pool.FreeList(ref entities);

                // Otherwise, spawn the car
                BaseEntity car = SpawnCar(location);
                Puts($"Spawned new car at {location.x}, {location.y}, {location.z}");
            }

            timer.In(10f, () => RespawnCars());
        }

        private BaseEntity SpawnCar(Vector3 position)
        {
            BaseEntity entity = GameManager.server.CreateEntity(prefabCar, position + Vector3.up, default(Quaternion));
            entity.enableSaving = true;
            entity.Spawn();

            CarController controller = entity.gameObject.AddComponent<CarController>();

            return entity;
        }

        private void NullifyDamage(HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.HitEntity = null;
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }
        #endregion

        #region Classes
        private class CarController : MonoBehaviour
        {
            public BasePlayer player;
            public BaseCar car;
            public List<MountPoint> mountPoints = new List<MountPoint>();
            public List<BasePlayer> occupants = new List<BasePlayer>();
            public bool isDying = false;
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
                car = GetComponent<BaseCar>();
                //car.enabled = false;

                if (car.IsMounted())
                    car.DismountAllPlayers();

                CreatePassengerSeats();
                CreateTrunk();
            }

            private void FixedUpdate()
            {
                if (WaterLevel.Factor(car.WorldSpaceBounds().ToBounds()) > 0.7f && !HasDriver())
                {
                    enabled = false;
                    StopAndDie();
                    car.SetFlag(BaseEntity.Flags.Reserved1, false, false);
                    return;
                }

                if (player != null)
                    foreach (MountPoint mountPoint in mountPoints)
                        mountPoint.UpdatePlayerPosition();
            }

            void OnDestroy()
            {
                EjectAllPlayers();
                DestroyAllMounts();

                BaseEntity.saveList.Remove(container);
                if (container != null)
                    container.Invoke("KillMessage", 0.1f);
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
                container = (StorageContainer)GameManager.server.CreateEntity(prefabFridge, car.transform.position, car.transform.rotation, false);
                container.enableSaving = true;
                container.skinID = (ulong)886416273;
                container.Spawn();

                container.GetComponent<DestroyOnGroundMissing>().enabled = false;
                container.GetComponent<GroundWatch>().enabled = false;

                container.SetParent(car);
                container.transform.localPosition = new Vector3(1f, 0.85f, -1.8f);
                container.transform.localRotation = Quaternion.Euler(263f + 180f, 0, 90f);

                container.panelName = "generic";
                container.isLockable = false;
                container.displayHealth = false;
                container.pickup.enabled = false;
                container.onlyAcceptCategory = ItemCategory.All;

                if (plug.trunks.ContainsKey(car.net.ID))
                    plug.trunks[car.net.ID].RestoreItems(ref container.inventory);
            }

            public bool HasDriver() => player != null;

            public void EjectAllPlayers()
            {
                foreach (MountPoint mountPoint in mountPoints)
                    mountPoint.DismountPlayer();
            }

            public MountPoint GetClosestMountPoint(Vector3 position)
            {
                MountPoint mountPoint = null;

                float closest = 10f;

                foreach (MountPoint point in mountPoints)
                {
                    float distance = Vector3.Distance(point.Position, position);
                    if (distance < closest)
                    {
                        if (point.Entity.IsMounted())
                            continue;

                        mountPoint = point;
                        closest = distance;
                    }
                }

                return mountPoint;
            }

            private void DestroyAllMounts()
            {
                foreach (MountPoint mountPoint in mountPoints)
                    mountPoint.DestroyMountPoint();
            }

            public void StopAndDie()
            {
                if (isDying)
                    return;

                enabled = false;
                isDying = true;

                if (car != null)
                {
                    car.SetFlag(BaseEntity.Flags.Reserved1, false, false);
                    foreach (var wheel in car.wheels)
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

                Effect.server.Run(prefabExplosion, transform.position);

                plug.NextTick(() =>
                {
                    if (car != null && !car.IsDestroyed)
                        car.DieInstantly();

                    Destroy(this);
                });
            }

            public void ManageDamage(HitInfo info)
            {
                if (isDying)
                {
                    plug.NullifyDamage(info);
                    return;
                }

                if (info.damageTypes.GetMajorityDamageType() == DamageType.Bullet)
                    info.damageTypes.ScaleAll(1000);
                else
                    info.damageTypes.ScaleAll(10);

                if (info.damageTypes.Total() >= car.health)
                {
                    isDying = true;
                    plug.NullifyDamage(info);
                    OnDeath();
                    return;
                }

                foreach (BasePlayer p in occupants)
                    plug.UpdateUI(p, this);
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
                        entity = controller.car;
                    else
                        GeneratePrefab();

                }

                #region Functions
                public void GeneratePrefab()
                {
                    entity = (BaseMountable)GameManager.server.CreateEntity(controller.car.chairRef.resourcePath, controller.transform.position);
                    entity.enableSaving = false;
                    entity.skinID = (ulong)1169930802;
                    entity.Spawn();

                    entity.maxMountDistance = 2f;

                    Destroy(entity.GetComponent<DestroyOnGroundMissing>());
                    Destroy(entity.GetComponent<GroundWatch>());
                    entity.GetComponent<MeshCollider>().convex = true;

                    InvisibleMount invisibleMount = entity.gameObject.AddComponent<InvisibleMount>();
                    invisibleMount.MountPosition = this;

                    entity.SetParent(controller.car);
                    entity.transform.localPosition = offset;
                }

                public void OnEntityMounted()
                {
                    controller.occupants.Add(player);
                    plug.CreateUI(player, controller);

                    if (isDriver)
                        controller.Driver = player;
                }

                public void OnEntityDismounted()
                {
                    plug.DestroyUI(player);
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
                    player.transform.rotation = entity.mountAnchor.transform.rotation;
                    player.ServerRotation = entity.mountAnchor.transform.rotation;
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

                    Vector3 dismountPosition = GetDismountPosition();

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

                private Vector3 GetDismountPosition()
                {
                    Vector3 dismountPosition = controller.car.transform.position + 
                        (controller.car.transform.right * dismountLocal.x) + 
                        (controller.car.transform.up * 1.0f) + (controller.car.transform.forward * dismountLocal.z);

                    if (TerrainMeta.HeightMap.GetHeight(dismountPosition) > dismountPosition.y)
                        dismountPosition.y = TerrainMeta.HeightMap.GetHeight(dismountPosition) + 0.5f;

                    if (!Physics.CheckCapsule(dismountPosition + new Vector3(0f, 0.41f, 0f), dismountPosition + new Vector3(0f, 1.39f, 0f), 0.4f, LayerMask.GetMask("Construction")))
                    {
                        Vector3 vector3 = dismountPosition + new Vector3(0f, player.GetHeight() * 0.5f, 0f);
                        if (entity.IsVisible(vector3) && !Physics.Linecast(entity.transform.position + new Vector3(0f, 1f, 0f), vector3, 1075904513))
                            return dismountPosition;
                    }

                    return entity.transform.position + new Vector3(0f, 1.5f, 0f);
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
                #endregion
            }

            public class InvisibleMount : MonoBehaviour
            {
                private BaseMountable mountable;
                private MountPoint mountPoint;

                private void Awake()
                {
                    mountable = GetComponent<BaseMountable>();
                    enabled = false;
                }

                public MountPoint MountPosition
                {
                    get { return mountPoint; }
                    set { mountPoint = value; }
                }
            }
            #endregion
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player, CarController controller)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || pdata.displayCarHud == false)
                return;

            DestroyUI(player);

            // Create the main container
            CuiElementContainer container = Helper.UI.Container("ui_car", "0 0 0 .5", "0.01 0.06", "0.24 0.09");
            
            // Display the container
            Helper.UI.Add(player, container);

            UpdateUI(player, controller);
            //CuiElementContainer container2 = Helper.UI.Container("ui_car2", "1 0 0 .99", "0.01 0.02", "0.25 0.05"); // REFERENCE
            //Helper.UI.Add(player, container2);
        }

        private void DestroyUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_car");
            Helper.UI.Destroy(player, "ui_car2");
        }

        private void UpdateUI(BasePlayer player, CarController controller)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || pdata.displayCarHud == false)
                return;

            Helper.UI.Destroy(player, "ui_car_container");

            CuiElementContainer container = Helper.UI.Container("ui_car_container", "0 0 0 .5", "0 0.005", "1 0.94", false, "ui_car");

            Helper.UI.Label(ref container, "ui_car_container", "1 1 1 1", "Health", 12, "0.02 0", "1 1", TextAnchor.MiddleLeft);

            // Health
            Helper.UI.Panel(ref container, "ui_car_container", "0.57 0.21 0.11 1", "0.2 0.25", "0.98 0.75", false, "ui_car_container_hp");
            Helper.UI.Panel(ref container, "ui_car_container_hp", "0.41 0.5 0.25 1", "0 0", (controller.car.health / controller.car.MaxHealth()) + " 0.92");
            
            Helper.UI.Add(player, container);
        }
        #endregion
    }
}
