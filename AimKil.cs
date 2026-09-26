// ============================================================
// AIMKIL  C# BY DANGER MAX REACT
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace AotForms
{
    internal static class AimKil
    {
        private static bool IsValidTarget(Entity entity)
        {
            if (entity == null) return false;
            if (entity.Address == 0) return false;
            if (!entity.IsKnown) return false;
            if (entity.IsDead) return false;
            if (entity.Health <= 0) return false;
            if (Config.IgnoreKnocked && entity.IsKnocked) return false;
            if (entity.Address == Core.LocalPlayer) return false;
            return true;
        }
        private static Entity GetClosestEnemy(int mode)
        {
            var aliveEntities = Core.Entities.Values
                .Where(e => IsValidTarget(e))
                .ToList();

            if (aliveEntities.Count == 0) return null;

            if (mode == 0) 
            {
                Entity target = null;
                float closestDist = float.MaxValue;
                var screenCenter = new Vector2(Core.Width / 2f, Core.Height / 2f);

                foreach (var entity in aliveEntities)
                {
                    Vector2 headScreen = W2S.WorldToScreen(Core.CameraMatrix, entity.Head, Core.Width, Core.Height);
                    if (headScreen.X < 1 || headScreen.Y < 1 || headScreen.X > Core.Width || headScreen.Y > Core.Height)
                        continue;

                    float dx = headScreen.X - screenCenter.X;
                    float dy = headScreen.Y - screenCenter.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (dist < closestDist && dist < Config.aimbotFOV)
                    {
                        closestDist = dist;
                        target = entity;
                    }
                }
                return target;
            }
            else
            {
                Entity target = null;
                float closestDist = float.MaxValue;
                Vector3 localHead = Core.LocalMainCamera;

                foreach (var entity in aliveEntities)
                {
                    float dist = Vector3.Distance(localHead, entity.Head);
                    if (dist < closestDist && dist < Config.aimbotFOV)
                    {
                        closestDist = dist;
                        target = entity;
                    }
                }
                return target;
            }
        }

        private static void ApplyAimKil(Entity target)
        {
            if (target == null) return;

            try
            {
                var playerLook = MathUtils.GetRotationToLocation(target.Head, 0.1f, Core.LocalMainCamera);
                InternalMemory.Write(Core.LocalPlayer + Offsets.AimRotation, playerLook);

                // إطلاق نار تلقائي (اختياري)
                if (Config.AimkilAutoFire)
                {
                    InternalMemory.Write<bool>(Core.LocalPlayer + Offsets.IsFiring, true);
                }
            }
            catch { }
        }

        internal static void Work()
        {
            while (true)
            {
                if (!Config.Aimkil)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (Config.CheckWeapon && !HasWeapon())
                {
                    Thread.Sleep(10);
                    continue;
                }

                Entity target = GetClosestEnemy(Config.NoTargetBulletMode);

                if (target != null)
                {
                    ApplyAimKil(target);
                }
                else
                {
                    try
                    {
                        InternalMemory.Write<bool>(Core.LocalPlayer + Offsets.IsFiring, false);
                    }
                    catch { }
                }

                Thread.Sleep(5);
            }
        }

        private static bool HasWeapon()
        {
            try
            {
                if (InternalMemory.Read<uint>(Core.LocalPlayer + Offsets.Weapon, out uint weapon) && weapon != 0)
                    return true;
                return false;
            }
            catch { return true; }
        }
    }
}