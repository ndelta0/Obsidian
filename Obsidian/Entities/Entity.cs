﻿using Obsidian.API;
using Obsidian.Chat;
using Obsidian.Net;
using Obsidian.Net.Packets.Play.Clientbound;
using Obsidian.WorldData;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Obsidian.Entities
{
    public class Entity : IEquatable<Entity>, IEntity
    {
        internal static int[] MiscEntities;

        public World World { get; set; }
        public IWorld WorldLocation => World;

        #region Location properties
        internal PositionF LastPosition { get; set; }

        internal Angle LastPitch { get; set; }

        internal Angle LastYaw { get; set; }

        internal int TeleportId { get; set; }

        public PositionF Position { get; set; }

        public Angle Pitch { get; set; }

        public Angle Yaw { get; set; }

        public PositionF LookDirection
        {
            get
            {
                float yaw = Yaw, pitch = Pitch; // avoid multiple conversions
                float pitchCos = MathF.Cos(pitch);
                PositionF direction = new PositionF
                {
                    X = MathF.Cos(yaw) * pitchCos,
                    Y = MathF.Sin(yaw) * pitchCos,
                    Z = MathF.Sin(pitch)
                };
                return direction;
            }
        }
        #endregion Location properties

        public int EntityId { get; internal set; }

        public Guid Uuid { get; set; } = Guid.NewGuid();

        public EntityBitMask EntityBitMask { get; set; } = EntityBitMask.None;

        public Pose Pose { get; set; } = Pose.Standing;

        public EntityType Type { get; set; }

        public int Air { get; set; } = 300;

        public ChatMessage CustomName { get; private set; }

        public bool CustomNameVisible { get; private set; }
        public bool Silent { get; private set; }
        public bool NoGravity { get; set; }
        public bool OnGround { get; set; }

        internal static void Initialize()
        {
            MiscEntities = new[]
            {
                (int)EntityType.Arrow,
                (int)EntityType.SpectralArrow,
                (int)EntityType.Boat,
                (int)EntityType.DragonFireball,
                (int)EntityType.AreaEffectCloud,
                (int)EntityType.EndCrystal,
                (int)EntityType.EvokerFangs,
                (int)EntityType.ExperienceOrb,
                (int)EntityType.FireworkRocket,
                (int)EntityType.FallingBlock,
                (int)EntityType.Item,
                (int)EntityType.ItemFrame,
                (int)EntityType.Fireball,
                (int)EntityType.LeashKnot,
                (int)EntityType.LightningBolt,
                (int)EntityType.LlamaSpit,
                (int)EntityType.Minecart,
                (int)EntityType.ChestMinecart,
                (int)EntityType.CommandBlockMinecart,
                (int)EntityType.FurnaceMinecart,
                (int)EntityType.HopperMinecart,
                (int)EntityType.SpawnerMinecart,
                (int)EntityType.TntMinecart,
                (int)EntityType.Painting,
                (int)EntityType.Tnt,
                (int)EntityType.ShulkerBullet,
                (int)EntityType.EnderPearl,
                (int)EntityType.Snowball,
                (int)EntityType.SmallFireball,
                (int)EntityType.Egg,
                (int)EntityType.ExperienceBottle,
                (int)EntityType.Potion,
                (int)EntityType.Trident,
                (int)EntityType.FishingBobber,
                (int)EntityType.EyeOfEnder
            };
        }

        public Entity()
        {
        }

        #region Update methods
        internal virtual async Task UpdateAsync(Server server, PositionF position, bool onGround)
        {
            var newPos = position * 32 * 64;
            var lastPos = this.LastPosition * 32 * 64;

            short newX = (short)(newPos.X - lastPos.X);
            short newY = (short)(newPos.Y - lastPos.Y);
            short newZ = (short)(newPos.Z - lastPos.Z);

            var isNewLocation = position != this.LastPosition;

            if (isNewLocation)
            {
                server.BroadcastPacketWithoutQueue(new EntityPosition
                {
                    EntityId = this.EntityId,

                    DeltaX = newX,
                    DeltaY = newY,
                    DeltaZ = newZ,

                    OnGround = onGround
                }, this.EntityId);

                this.UpdatePosition(position, onGround);
            }
            await Task.CompletedTask;
        }

        internal virtual async Task UpdateAsync(Server server, Angle yaw, Angle pitch, bool onGround)
        {
            var isNewRotation = yaw != this.LastYaw || pitch != this.LastPitch;

            if (isNewRotation)
            {
                server.BroadcastPacketWithoutQueue(new EntityRotation
                {
                    EntityId = this.EntityId,
                    OnGround = onGround,
                    Yaw = yaw,
                    Pitch = pitch
                }, this.EntityId);

                this.CopyLook();
                this.UpdatePosition(yaw, pitch, onGround);
            }
            await Task.CompletedTask;
        }

        internal virtual async Task UpdateAsync(Server server, PositionF position, Angle yaw, Angle pitch, bool onGround)
        {
            var newPos = position * 32 * 64;
            var lastPos = this.LastPosition * 32 * 64;

            short newX = (short)(newPos.X - lastPos.X);
            short newY = (short)(newPos.Y - lastPos.Y);
            short newZ = (short)(newPos.Z - lastPos.Z);

            var isNewLocation = position != this.LastPosition;

            var isNewRotation = yaw != this.LastYaw || pitch != this.LastPitch;


            if (isNewLocation)
            {
                if (isNewRotation)
                {
                    server.BroadcastPacketWithoutQueue(new EntityPositionAndRotation
                    {
                        EntityId = this.EntityId,

                        DeltaX = newX,
                        DeltaY = newY,
                        DeltaZ = newZ,

                        Yaw = yaw,

                        Pitch = pitch,

                        OnGround = onGround
                    }, this.EntityId);

                    this.CopyLook();
                }
                else
                {
                    server.BroadcastPacketWithoutQueue(new EntityPosition
                    {
                        EntityId = this.EntityId,

                        DeltaX = newX,
                        DeltaY = newY,
                        DeltaZ = newZ,

                        OnGround = onGround
                    }, this.EntityId);
                }

                this.UpdatePosition(position, yaw, pitch, onGround);
            }
            await Task.CompletedTask;
        }

        internal void CopyPosition(bool withLook = false)
        {
            this.LastPosition = this.Position;

            if (withLook)
                this.CopyLook();
        }

        internal void CopyLook()
        {
            this.LastYaw = this.Yaw;
            this.LastPitch = this.Pitch;
        }

        public void UpdatePosition(PositionF pos, bool onGround = true)
        {
            this.CopyPosition();
            this.Position = pos;
            this.OnGround = onGround;
        }

        public void UpdatePosition(PositionF pos, Angle yaw, Angle pitch, bool onGround = true)
        {
            this.CopyPosition(true);
            this.Position = pos;
            this.Yaw = yaw;
            this.Pitch = pitch;
            this.OnGround = onGround;
        }

        public void UpdatePosition(float x, float y, float z, bool onGround = true)
        {
            this.CopyPosition();
            this.Position = new PositionF(x, y, z);
            this.OnGround = onGround;
        }

        public void UpdatePosition(Angle yaw, Angle pitch, bool onGround = true)
        {
            this.CopyLook();
            this.Yaw = yaw;
            this.Pitch = pitch;
            this.OnGround = onGround;
        }
        #endregion

        public Task RemoveAsync() => this.World.DestroyEntityAsync(this);

        public virtual async Task WriteAsync(MinecraftStream stream)
        {
            await stream.WriteEntityMetdata(0, EntityMetadataType.Byte, (byte)this.EntityBitMask);

            await stream.WriteEntityMetdata(1, EntityMetadataType.VarInt, this.Air);

            await stream.WriteEntityMetdata(2, EntityMetadataType.OptChat, this.CustomName, this.CustomName != null);

            await stream.WriteEntityMetdata(3, EntityMetadataType.Boolean, this.CustomNameVisible);
            await stream.WriteEntityMetdata(4, EntityMetadataType.Boolean, this.Silent);
            await stream.WriteEntityMetdata(5, EntityMetadataType.Boolean, this.NoGravity);
            await stream.WriteEntityMetdata(6, EntityMetadataType.Pose, this.Pose);
        }

        public virtual void Write(MinecraftStream stream)
        {
            stream.WriteEntityMetadataType(0, EntityMetadataType.Byte);
            stream.WriteUnsignedByte((byte)EntityBitMask);

            stream.WriteEntityMetadataType(1, EntityMetadataType.VarInt);
            stream.WriteVarInt(Air);

            stream.WriteEntityMetadataType(2, EntityMetadataType.OptChat);
            stream.WriteBoolean(CustomName is not null);
            if (CustomName is not null)
                stream.WriteChat(CustomName);

            stream.WriteEntityMetadataType(3, EntityMetadataType.Boolean);
            stream.WriteBoolean(CustomNameVisible);

            stream.WriteEntityMetadataType(4, EntityMetadataType.Boolean);
            stream.WriteBoolean(Silent);

            stream.WriteEntityMetadataType(5, EntityMetadataType.Boolean);
            stream.WriteBoolean(NoGravity);

            stream.WriteEntityMetadataType(6, EntityMetadataType.Pose);
            stream.WriteVarInt((int)Pose);
        }

        public IEnumerable<IEntity> GetEntitiesNear(float distance) => this.World.GetEntitiesNear(this.Position, distance);

        public virtual Task TickAsync() => Task.CompletedTask;

        public bool Equals([AllowNull] Entity other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return this.EntityId == other.EntityId;
        }

        public override bool Equals(object obj) => Equals(obj as Entity);

        public static implicit operator int(Entity entity) => entity.EntityId;

        public static bool operator ==(Entity a, Entity b)
        {
            if (ReferenceEquals(a, b))
                return true;

            return a.Equals(b);
        }

        public static bool operator !=(Entity a, Entity b) => !(a == b);

        public override int GetHashCode() => this.EntityId.GetHashCode();
    }
}