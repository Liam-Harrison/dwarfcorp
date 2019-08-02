using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public partial class RailHelper
    {
        private static CraftItem RailCraftItem = new CraftItem
        {
            Description = "Rail.",
            RequiredResources = new List<Quantitiy<Resource.ResourceTags>>
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Rail, 1)
                        },
            Icon = new Gui.TileReference("resources", 38),
            BaseCraftTime = 10,
            Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround },
            CraftLocation = "",
            Name = "Rail",
            Type = CraftItem.CraftType.Object,
            AddToOwnedPool = true,
            Moveable = false
        };

        public static RailEntity CreatePreviewBody(ComponentManager Manager, VoxelHandle Location, JunctionPiece Piece)
        {
            var r = new RailEntity(Manager, Location, Piece);
            Manager.RootComponent.AddChild(r);
            r.SetFlagRecursive(GameComponent.Flag.Active, false);

            foreach (var tinter in r.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            r.SetFlag(GameComponent.Flag.ShouldSerialize, false);
            //Todo: Add craft details component.
            return r;
        }

        public static bool CanPlace(WorldManager World, List<RailEntity> PreviewBodies)
        {
            for (var i = 0; i < PreviewBodies.Count; ++i)
            {
                PreviewBodies[i].PropogateTransforms();
                if (!RailHelper.CanPlace(World, PreviewBodies[i]))
                    return false;
            }
            return true;
        }

        public static bool CanPlace(WorldManager World, RailEntity PreviewEntity)
        {
            // Todo: Make sure this uses BuildObjectTool.IsValidPlacement to enforce building rules.

            var junctionPiece = PreviewEntity.GetPiece();
            var actualPosition = PreviewEntity.GetContainingVoxel();
            if (!actualPosition.IsValid) return false;
            if (!actualPosition.IsEmpty) return false;

            if (actualPosition.Coordinate.Y == 0) return false; // ???

            var voxelUnder = VoxelHelpers.GetVoxelBelow(actualPosition);
            if (voxelUnder.IsEmpty) return false;
            var box = actualPosition.GetBoundingBox().Expand(-0.2f);

            foreach (var entity in World.EnumerateIntersectingObjects(box, CollisionType.Static))
            {
                if ((entity as GameComponent).IsDead)
                    continue;

                if (Object.ReferenceEquals(entity, PreviewEntity)) continue;
                if (Object.ReferenceEquals(entity.GetRoot(), PreviewEntity.GetRoot())) continue;
                if (entity is GenericVoxelListener) continue;
                if (entity is WorkPile) continue;
                if (entity is Health) continue;
                if (entity is CraftDetails) continue;

                if (FindPossibleCombination(junctionPiece, entity).HasValue(out var possibleCombination))
                {
                    var combinedPiece = new Rail.JunctionPiece
                    {
                        RailPiece = possibleCombination.Result,
                        Orientation = Rail.OrientationHelper.Rotate((entity as RailEntity).GetPiece().Orientation, (int)possibleCombination.ResultRelativeOrientation),
                    };

                    PreviewEntity.UpdatePiece(combinedPiece, PreviewEntity.GetContainingVoxel());
                    return true;
                }

                if (Debugger.Switches.DrawToolDebugInfo)
                    Drawer3D.DrawBox(box, Color.Yellow, 0.2f, false);

                World.UserInterface.ShowTooltip(String.Format("Can't place {0}. Entity in the way: {1}", junctionPiece.RailPiece, entity.ToString()));
                return false;
            }

            return true;
        }

        public static MaybeNull<CombinationTable.Combination> FindPossibleCombination(Rail.JunctionPiece Piece, GameComponent Entity)
        {
            if (Entity is RailEntity)
            {
                var baseJunction = (Entity as RailEntity).GetPiece();
                if (Library.GetRailPiece(baseJunction.RailPiece).HasValue(out var basePiece))
                {
                    var relativeOrientation = Rail.OrientationHelper.Relative(baseJunction.Orientation, Piece.Orientation);

                    if (basePiece.Name == Piece.RailPiece && relativeOrientation == PieceOrientation.North)
                        return new CombinationTable.Combination
                        {
                            Result = basePiece.Name,
                            ResultRelativeOrientation = PieceOrientation.North
                        };

                    var matchingCombination = Library.FindRailCombination(basePiece.Name, Piece.RailPiece, relativeOrientation);
                    return matchingCombination;
                }
            }

            return null;
        }

        public static void Place(WorldManager World, List<RailEntity> PreviewBodies, bool GodModeSwitch)
        {
            // Assume CanPlace was called and returned true.

            var assignments = new List<Task>();

            for (var i = 0; i < PreviewBodies.Count; ++i)
            {
                var body = PreviewBodies[i];
                var piece = body.GetPiece();
                var actualPosition = body.GetContainingVoxel();
                var addNewDesignation = true;
                var hasResources = false;
                var finalEntity = body;

                foreach (var entity in World.EnumerateIntersectingObjects(actualPosition.GetBoundingBox().Expand(-0.2f), CollisionType.Static))
                {
                    if ((entity as GameComponent).IsDead)
                        continue;
                    if ((entity as RailEntity) == null)
                        continue;

                    if (!addNewDesignation) break;
                    if (Object.ReferenceEquals(entity, body)) continue;

                    var existingDesignation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, entity));
                    if (existingDesignation != null)
                    {
                        (entity as RailEntity).UpdatePiece(piece, actualPosition);
                        (existingDesignation.Tag as CraftDesignation).Progress = 0.0f;
                        body.GetRoot().Delete();
                        addNewDesignation = false;
                        finalEntity = entity as RailEntity;
                    }
                    else
                    {
                        (entity as RailEntity).GetRoot().Delete();
                        hasResources = true;
                    }

                }

                if (!GodModeSwitch && addNewDesignation)
                {
                    var startPos = body.Position + new Vector3(0.0f, -0.3f, 0.0f);
                    var endPos = body.Position;

                    var designation = new CraftDesignation
                    {
                        Entity = body,
                        WorkPile = new WorkPile(World.ComponentManager, startPos),
                        OverrideOrientation = false,
                        Valid = true,
                        ItemType = RailCraftItem,
                        SelectedResources = new List<ResourceAmount> { new ResourceAmount("Rail", 1) },
                        Location = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(body.Position)),
                        HasResources = hasResources,
                        ResourcesReservedFor = null,
                        Orientation = 0.0f,
                        Progress = 0.0f,
                    };

                    body.SetFlag(GameComponent.Flag.ShouldSerialize, true);
                    World.ComponentManager.RootComponent.AddChild(designation.WorkPile);
                    designation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), endPos));
                    World.ParticleManager.Trigger("puff", endPos, Color.White, 10);

                    var task = new CraftItemTask(designation);
                    World.PersistentData.Designations.AddEntityDesignation(body, DesignationType.Craft, designation, task);
                    assignments.Add(task);
                }

                if (GodModeSwitch)
                {
                    // Go ahead and activate the entity and destroy the designation and workpile.
                    var existingDesignation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.Craft).FirstOrDefault(d => Object.ReferenceEquals(d.Body, finalEntity));
                    if (existingDesignation != null)
                    {
                        var designation = existingDesignation.Tag as CraftDesignation;
                        if (designation != null && designation.WorkPile != null)
                            designation.WorkPile.GetRoot().Delete();
                        World.PersistentData.Designations.RemoveEntityDesignation(finalEntity, DesignationType.Craft);
                    }

                    finalEntity.SetFlagRecursive(GameComponent.Flag.Active, true);
                    finalEntity.SetVertexColorRecursive(Color.White);
                    finalEntity.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    finalEntity.SetFlag(GameComponent.Flag.ShouldSerialize, true);
                    World.PlayerFaction.OwnedObjects.Add(finalEntity);
                    foreach (var tinter in finalEntity.EnumerateAll().OfType<Tinter>())
                        tinter.Stipple = false;
                }
            }

            if (!GodModeSwitch && assignments.Count > 0)
                World.TaskManager.AddTasks(assignments);
        }
    }
}
