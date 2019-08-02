using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CreatureAI : GameComponent
    {
        public Task CurrentTask = null; // Todo: MaybeNull
        public List<Task> Tasks = new List<Task>();
        [JsonIgnore] protected MaybeNull<Act> CurrentAct = null;
        public BoundingBox PositionConstraint = new BoundingBox(new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue), new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        public EnemySensor Sensor { get; set; } // Todo: Don't serialize this.
        public CreatureMovement Movement { get; set; }
        [JsonProperty] private String LastMesage = "";
        [JsonIgnore] public PlanSubscriber PlanSubscriber = null;
        private Timer AutoGatherTimer = new Timer(MathFunctions.Rand() * 5 + 3, false);
        private Timer _preEmptTimer = new Timer(1.21f, false);
        [JsonIgnore] public Blackboard Blackboard = new Blackboard(); // Todo: Does not serializing this break all tasks?
        public string Biography = "";
        public string LastFailedAct = null;

        protected struct FailedTask
        {
            public Task TaskFailure;
            public DateTime FailedTime;
        }

        protected List<FailedTask> FailedTasks = new List<FailedTask>();

        [OnDeserialized]
        public void OnDeserialize(StreamingContext ctx)
        {
            PlanSubscriber = new PlanSubscriber((ctx.Context as WorldManager).PlanService);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            if (GetRoot().GetComponent<EnemySensor>().HasValue(out var Sensor))
                Sensor.OnEnemySensed += Sensor_OnEnemySensed;
        }

        public CreatureAI()
        {
        }

        public CreatureAI(
            ComponentManager Manager,
            string name,
            EnemySensor sensor) :
            base(name, Manager)
        {
            Movement = new CreatureMovement(this);
            PlanSubscriber = new PlanSubscriber(Manager.World.PlanService);

            Sensor = sensor;
            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
            Sensor.Creature = this;
        }

        private Creature _cachedCreature = null;

        [JsonIgnore] public Creature Creature
        {
            get
            {
                if (Parent == null)
                    return null;
                if (_cachedCreature == null)
                    _cachedCreature = Parent.EnumerateAll().OfType<Creature>().FirstOrDefault();
                System.Diagnostics.Debug.Assert(_cachedCreature != null, "AI Could not find creature");
                return _cachedCreature;
            }
        }

        /// <summary> Wrapper around Creature.Physics </summary>
        [JsonIgnore] // Todo: The problem with these wrappers is that everything works with CreatureAI instead of Creature.
        public Physics Physics
        {
            get { return Creature.Physics; }
        }

        /// <summary> Wrapper around Creature.Faction </summary>
        [JsonIgnore]
        public Faction Faction
        {
            get { return Creature.Faction; }
            set { Creature.Faction = value; }
        }

        /// <summary> Wrapper around Creature.Stats </summary>
        [JsonIgnore]
        public CreatureStats Stats
        {
            get { return Creature.Stats; }
        }

        /// <summary> Wrapper around Creature.Physics.GlobalTransform.Translation </summary>
        [JsonIgnore]
        public Vector3 Position // Todo: Remove wrapper
        {
            get { return Creature.Physics.GlobalTransform.Translation; }
            set
            {
                Matrix newTransform = Creature.Physics.LocalTransform;
                newTransform.Translation = value;
                Creature.Physics.LocalTransform = newTransform;
            }
        }       

        public bool WasTaskFailed(Task task)
        {
            return FailedTasks.Any(t => t.TaskFailure.Equals(task));
        }

        private void UpdateFailedTasks(DateTime now)
        {
            FailedTasks.RemoveAll(task => (now - task.FailedTime).Hours >= 1);
        }

        public void ResetPositionConstraint()
        {
            PositionConstraint = new BoundingBox(new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue),
            new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));            
        }

        public virtual void AddXP(int amount) { }

        private void Sensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            if (Creature == null)
                return;

            if (enemies.Count > 0)
            {
                enemies.RemoveAll(threat => threat == null || threat.Creature == null);
                foreach (CreatureAI threat in enemies.Where(threat => threat != null && threat.Creature != null && !Faction.Threats.Contains(threat.Creature)))
                    Faction.Threats.Add(threat.Creature);
            }
        }

        public Task GetEasiestTask(List<Task> tasks)
        {
            if (tasks == null)
                return null;

            float bestCost = float.MaxValue;
            Task bestTask = null;
            var bestPriority = TaskPriority.Eventually;

            foreach (var task in tasks)
            {
                float cost = task.ComputeCost(Creature);

                if (task.IsFeasible(Creature) == Feasibility.Feasible && task.Priority >= bestPriority && cost < bestCost && !WasTaskFailed(task))
                {
                    bestCost = cost;
                    bestTask = task;
                    bestPriority = task.Priority;
                }
            }

            return bestTask;
        }

        /// <summary> Looks for any tasks with higher priority than the current task. Cancel the current task and do that one instead. </summary>
        public void PreEmptTasks()
        {
            if (CurrentTask == null) return;

            Task newTask = null;

            _preEmptTimer.Update(DwarfTime.LastTime);

            if (_preEmptTimer.HasTriggered)
            {
                var inventory = Creature.Inventory;
                if (inventory != null && inventory.Resources.Any(resource => Library.GetResourceType(resource.Resource).Tags.Contains(Resource.ResourceTags.Potion)))
                {
                    var applicablePotions = inventory.Resources.Where(resource => !resource.MarkedForRestock).
                        Select(resource => Library.GetResourceType(resource.Resource)).
                        Where(resource => resource.Tags.Contains(Resource.ResourceTags.Potion) 
                        && resource.PotionType != null
                        && resource.PotionType.ShouldDrink(Creature));
                    var potion = applicablePotions.FirstOrDefault();
                    if (potion != null)
                    {
                        potion.PotionType.Drink(Creature);
                        inventory.Remove(new ResourceAmount(potion), Inventory.RestockType.Any);
                    }
                }

                foreach (Task task in Tasks)
                {
                    if (task.Priority > CurrentTask.Priority && task.IsFeasible(Creature) == Feasibility.Feasible)
                    {
                        newTask = task;
                        break;
                    }
                }
            }

            if (_preEmptTimer.HasTriggered && newTask == null && Faction == World.PlayerFaction && !Stats.IsOnStrike)
                newTask = World.TaskManager.GetBestTask(this, (int)CurrentTask.Priority);

            if (newTask != null)
            {
                if (CurrentTask.ShouldRetry(Creature))
                    ReassignCurrentTask();
                ChangeTask(newTask);
            }
        }

        /// <summary> remove any impossible or already completed tasks </summary>
        public void DeleteBadTasks()
        {
            UpdateFailedTasks(World.Time.CurrentDate);
            var badTasks = Tasks.Where(task => task.ShouldDelete(Creature)).ToList();
            foreach(var task in badTasks)
            {
                task.OnUnAssign(this);
                Tasks.Remove(task);
            }
           
        }

        public bool IsPositionConstrained()
        {
            return (PositionConstraint.Max.X) < float.MaxValue;
        }

        /// <summary> Animate the PlayState Camera to look at this creature </summary>
        public void ZoomToMe()
        {
            Manager.World.Renderer.Camera.ZoomTo(Position + Vector3.Up * 8.0f);

            var above = VoxelHelpers.FindFirstVoxelAbove(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Position)));

            if (above.IsValid)
                World.Renderer.SetMaxViewingLevel(above.Coordinate.Y);
            else
                World.Renderer.SetMaxViewingLevel(World.WorldSizeInVoxels.Y);
        }

        public void HandleReproduction()
        {
            if (!Creature.Stats.Species.CanReproduce) return;
            if (Creature.IsPregnant) return;
            if (!MathFunctions.RandEvent(0.0002f)) return;
            if (CurrentTask != null) return;
            IEnumerable<CreatureAI> potentialMates =
                Faction.Minions.Where(minion => minion != this && Mating.CanMate(minion.Creature, this.Creature));
            CreatureAI closestMate = null;
            float closestDist = float.MaxValue;

            foreach (var ai in potentialMates)
            {
                var dist = (ai.Position - Position).LengthSquared();
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closestMate = ai;
            }

            if (closestMate != null && closestDist < 30)
                Tasks.Add(new MateTask(closestMate));
        }

        protected void ChangeAct(MaybeNull<Act> NewAct)
        {
            if (CurrentAct.HasValue(out Act currentAct))
                currentAct.OnCanceled();
                        
            CurrentAct = NewAct;
        }

        public void SetCurrentTaskNull()
        {
            ChangeTask(null);
        }

        public void CancelCurrentTask()
        {
            if (CurrentTask != null && Faction == World.PlayerFaction)
            {
                World.TaskManager.CancelTask(CurrentTask);
            }
            else
            {
                SetCurrentTaskNull();
            }
        }


        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera) 
        {
            base.Update(gameTime, chunks, camera);

            if (!Active)
                return;

            Creature.NoiseMaker.BasePitch = Stats.VoicePitch;

            // Non-dwarves are always at full energy.
            Stats.Energy.CurrentValue = 100.0f;

            AutoGatherTimer.Update(gameTime);

            if (AutoGatherTimer.HasTriggered)
            {
                if (!String.IsNullOrEmpty(Faction.Race.BecomeWhenEvil) && MathFunctions.RandEvent(0.01f))
                {
                    Faction.Minions.Remove(this);
                    Faction = World.Factions.Factions[Faction.Race.BecomeWhenEvil];
                    Faction.AddMinion(this);
                }
                else if (!String.IsNullOrEmpty(Faction.Race.BecomeWhenNotEvil) && MathFunctions.RandEvent(0.01f))
                {
                    Faction.Minions.Remove(this);
                    Faction = World.Factions.Factions[Faction.Race.BecomeWhenNotEvil];
                    Faction.AddMinion(this);
                }

                foreach (var body in World.EnumerateIntersectingObjects(Physics.BoundingBox.Expand(3.0f)).OfType<ResourceEntity>().Where(r => r.Active && r.AnimationQueue.Count == 0))
                {
                    var resource = Library.GetResourceType(body.Resource.Type);
                    if (resource.Tags.Contains(Resource.ResourceTags.Edible))
                    {
                        if ((Faction.Race.EatsMeat && resource.Tags.Contains(Resource.ResourceTags.AnimalProduct)) ||
                            (Faction.Race.EatsPlants && !resource.Tags.Contains(Resource.ResourceTags.AnimalProduct)))
                        {
                            Creature.GatherImmediately(body);
                            AssignTask(new ActWrapperTask(new EatFoodAct(this, false)));
                        }
                    }
                }

                OrderEnemyAttack();
            }

            DeleteBadTasks();
            PreEmptTasks();
            HandleReproduction();

            // Try to find food if we are hungry. Wait - doesn't this rob the player?
            if (Stats.Hunger.IsDissatisfied() && World.CountResourcesWithTag(Resource.ResourceTags.Edible) > 0)
            {
                Task toReturn = new SatisfyHungerTask();
                if (Stats.Hunger.IsCritical())
                    toReturn.Priority = TaskPriority.Urgent;
                if (!Tasks.Contains(toReturn) && CurrentTask != toReturn)
                    AssignTask(toReturn);
            }

            if (CurrentTask == null) // We need something to do.
            {
                    var goal = GetEasiestTask(Tasks);

                    if (goal != null)
                        ChangeTask(goal);
                    else
                    {
                        var newTask = ActOnIdle();
                        if (newTask != null)
                            ChangeTask(newTask);
                    }
            }
            else
            {
                if (!CurrentAct.HasValue()) // Should be impossible to have a current task and no current act.
                {
                    // Try and recover the correct act.
                    // <blecki> I always run with a breakpoint set here... just in case.
                    ChangeAct(CurrentTask.CreateScript(Creature));

                    // This is a bad situation!
                    if (!CurrentAct.HasValue())
                        ChangeTask(null);
                }

                if (CurrentAct.HasValue(out Act currentAct))
                {
                    var status = currentAct.Tick();
                    bool retried = false;

                    if (CurrentAct.HasValue(out Act newCurrentAct) && CurrentTask != null)
                    {
                        if (status == Act.Status.Fail)
                        {
                            LastFailedAct = newCurrentAct.Name;

                            if (!FailedTasks.Any(task => task.TaskFailure.Equals(CurrentTask)))
                                FailedTasks.Add(new FailedTask() { TaskFailure = CurrentTask, FailedTime = World.Time.CurrentDate });

                            if (CurrentTask.ShouldRetry(Creature))
                                if (!Tasks.Contains(CurrentTask))
                                {
                                    ReassignCurrentTask();
                                    retried = true;
                                }
                        }
                    }

                    if (CurrentTask != null && CurrentTask.IsComplete(World))
                        ChangeTask(null);
                    else if (status != Act.Status.Running && !retried)
                        ChangeTask(null);
                }
            }

            // With a small probability, the creature will drown if its under water.
            if (MathFunctions.RandEvent(GameSettings.Default.DrownChance))
            {
                var above = VoxelHelpers.GetVoxelAbove(Physics.CurrentVoxel);
                var below = VoxelHelpers.GetVoxelBelow(Physics.CurrentVoxel);
                bool shouldDrown = (above.IsValid && (!above.IsEmpty || above.LiquidLevel > 0));
                if ((Physics.IsInLiquid || (!Movement.CanSwim && (below.IsValid && (below.LiquidLevel > 5)))) 
                    && (!Movement.CanSwim || shouldDrown))
                    Creature.Damage(Movement.CanSwim ? 1.0f : 30.0f, Health.DamageType.Normal);
            }

            if (PositionConstraint.Contains(Physics.LocalPosition) == ContainmentType.Disjoint)
            {
                Physics.LocalPosition = MathFunctions.Clamp(Physics.Position, PositionConstraint);
                Physics.PropogateTransforms();
            }
        }

        /// <summary> The Act that the creature performs when its told to "wander" (when it has nothing to do) </summary>
        public virtual Act ActOnWander()
        {
            return new WanderAct(this, 5, 1.5f + MathFunctions.Rand(-0.25f, 0.25f), 1.0f);
        }

        /// <summary> 
        /// Task the creature performs when it has no more tasks. In this case the creature will gather any necessary
        /// resources and place any blocks. If it doesn't have anything to do, it may wander somewhere or use an item
        /// to improve its wellbeing.
        /// </summary>
        public virtual Task ActOnIdle()
        {
            if (Creature.Physics.IsInLiquid)
                return new FindLandTask();

            if (GetRoot().GetComponent<Flammable>().HasValue(out var flames) && flames.IsOnFire)
                return new LongWanderAct(this) { Name = "Freak out!", PathLength = 2, Radius = 5 }.AsTask();

            return new LookInterestingTask();
        }

        /// <summary> Tell the creature to kill the given body. </summary>
        public void Kill(GameComponent entity)
        {
            var killTask = new KillEntityTask(entity, KillEntityTask.KillType.Auto) { ReassignOnDeath = false } ;
            if (!Tasks.Contains(killTask))
                AssignTask(killTask);
        }

        /// <summary> Tell the creature to find a path to the edge of the world and leave it. </summary>
        public void LeaveWorld()
        {
            Task leaveTask = new LeaveWorldTask
            {
                Priority = TaskPriority.Urgent,
                AutoRetry = true,
                Name = "Leave the world."
            };
            AssignTask(leaveTask);
        }

        /// <summary> Tell the creature to speak with another </summary>
        public virtual void Converse(CreatureAI other)
        {
        }

        /// <summary> Returns whether or not the creature has a task with the same name as another </summary>
        public bool HasTaskWithName(Task other)
        {
            return Tasks.Any(task => task.Name == other.Name);
        }

        /// <summary> For any enemy that this creature's enemy sensor knows about, order the creature to attack these enemies </summary>
        public virtual void OrderEnemyAttack()
        {
            foreach (CreatureAI enemy in Sensor.Enemies.Where(e => e != null && !e.IsDead && e.Creature != null))
            {
                if (enemy.Stats.IsFleeing)
                    continue;

                Task task = new KillEntityTask(enemy.Physics, KillEntityTask.KillType.Auto);
                if (!HasTaskWithName(task))
                {
                    Creature.AI.AssignTask(task);
                    MakeBattleAnnouncement(enemy);
                }
            }
        }

        protected virtual void MakeBattleAnnouncement(CreatureAI Enemy)
        { }

        /// <summary> Pay the creature this amount of money. The money goes into the creature's wallet. </summary>
        public void AddMoney(DwarfBux pay)
        {
            Stats.Money += pay;
            bool good = pay > 0;
            Color textColor = good ? GameSettings.Default.Colors.GetColor("Positive", Color.Green) : GameSettings.Default.Colors.GetColor("Negative", Color.Red);
            string prefix = good ? "+" : "";
            IndicatorManager.DrawIndicator(prefix + pay,
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        private string GetHappinessDescription(Status Happiness)
        {
            if (Happiness.CurrentValue >= Happiness.MaxValue)
                return "VERY HAPPY";
            else if (Happiness.CurrentValue <= Happiness.MinValue)
                return "LIVID";
            else if (Happiness.IsSatisfied())
                return "SATISFIED";
            else if (Happiness.IsDissatisfied())
                return "UNHAPPY";
            else
                return "OK";
        }

        public override string GetDescription()
        {
            string desc = Stats.FullName + ", level " + Stats.LevelIndex +
                          " " +
                          Stats.CurrentClass.Name + ", " + Stats.Gender.ToString() + "\n    " +
                          "Happiness: " + GetHappinessDescription(Stats.Happiness) + ". Health: " + Stats.Health.Percentage +
                          ". Hunger: " + (100 - Stats.Hunger.Percentage) + ". Energy: " + Stats.Energy.Percentage +
                          "\n";

            if (CurrentTask != null)
                desc += "    Task: " + CurrentTask.Name;

            if (CurrentAct.HasValue(out Act currentAct))
            {
                desc += "\n   Action: ";

                var act = currentAct;
                while (act != null && act.LastTickedChild != null)
                {
                    Act prevAct = act;
                    act = act.LastTickedChild;
                    if (act == null)
                    {
                        act = prevAct;
                        break;
                    }

                    if (act == act.LastTickedChild)
                        break;
                }
                desc += act.Name;
            }

            if (LastFailedAct != null)
                desc += "\n    Last failed: " + LastFailedAct;

            if (Stats.IsAsleep)
                desc += "\n UNCONSCIOUS";

            if (Creature.IsPregnant)
                desc += "\n Pregnant";

            if (Creature.IsCloaked)
                desc += "\n CLOAKED";

            if (Stats.IsOnStrike)
                desc += "\n ON STRIKE";

            if (!String.IsNullOrEmpty(LastMesage))
                desc += "\n" + LastMesage;

            return desc;
        }

        public void SetMessage(string message)
        {
            LastMesage = message;
        }

        public enum FightOrFlightResponse
        {
            Flee,
            Fight
        }

        // If true, this creature can fight the other creature. Otherwise, we want to flee it.
        public FightOrFlightResponse FightOrFlight(CreatureAI creature)
        {
            if (IsDead || creature == null || creature.IsDead)
                return FightOrFlightResponse.Fight;

            if (!Stats.Species.FeelsFear)
                return FightOrFlightResponse.Fight;

            var fear = 0.0f;

            // If our health is low, we're a little afraid.
            if (Creature.Hp < Creature.MaxHealth * 0.25f)
                fear += 0.125f;

            // If there are a lot of nearby threats vs allies, we are even more afraid.
            if (Faction.Threats.Where(threat => threat != null &&  threat.AI != null && !threat.IsDead).Sum(threat => (threat.AI.Position - Position).Length() < 6.0f ? 1 : 0) - 
                Faction.Minions.Where(minion => minion != null && !minion.IsDead).Sum(minion => (minion.Position - Position).Length() < 6.0f ? 1 : 0) > Creature.Stats.Constitution)
                fear += 0.125f;

            // In this case, we have a very very weak weapon in comparison to our enemy.
            if (Creature.Attacks[0].Weapon.DamageAmount * 20 < creature.Creature.Hp)
                fear += 0.125f;

            // If the creature has formidible weapons, we're in trouble.
            if (creature.Creature.Attacks[0].Weapon.DamageAmount * 4 > Creature.Hp)
                fear += 0.125f;

            fear = Math.Min(fear, 0.99f);

            if (MathFunctions.RandEvent(1.0f - fear))
                return FightOrFlightResponse.Fight;
            else
            { 
                Creature.AddThought("I was frightened recently.", new TimeSpan(0, 4, 0, 0), -2.0f);
                return FightOrFlightResponse.Flee;
            }
        }

        public void AssignTask(Task task)
        {
            if (task == null)
                throw new InvalidOperationException();

            if (!Tasks.Contains(task))
            {
                Tasks.Add(task);
                task.OnAssign(this);
            }
        }

        public void RemoveTask(Task task)
        {
            if (Object.ReferenceEquals(CurrentTask, task))
                SetCurrentTaskNull();
            Tasks.Remove(task);
            task.OnUnAssign(this);
        }

        public void ChangeTask(Task task)
        {
            Blackboard.Erase("NoPath");
            if (CurrentTask != null)
                CurrentTask.OnUnAssign(this);
            CurrentTask = task;
            if (CurrentTask != null)
            {
                ChangeAct(CurrentTask.CreateScript(Creature));

                if (Tasks.Contains(task))
                    Tasks.Remove(task);
                else
                    task.OnAssign(this);
            }
            else
            {
                ChangeAct(null);
            }
        }

        public void ReassignCurrentTask()
        {
            var task = CurrentTask;
            ChangeTask(null);
            AssignTask(task);
        }

        public int CountFeasibleTasks(TaskPriority minPriority)
        {
            return Tasks.Count(task => task.Priority >= minPriority && task.IsFeasible(Creature) == Feasibility.Feasible);
        }

        public override void Die()
        {
            if (CurrentTask != null)
            {
                if (CurrentTask.ReassignOnDeath && Faction == World.PlayerFaction)
                    World.TaskManager.AddTask(CurrentTask);
                ChangeTask(null);
            }
            if (PlanSubscriber != null)
            {
                PlanSubscriber.Service.RemoveSubscriber(PlanSubscriber);
            }
            base.Die();
        }

        public override void Delete()
        {
            if (CurrentTask != null)
            {
                if (CurrentTask.ReassignOnDeath && Faction == World.PlayerFaction)
                    World.TaskManager.AddTask(CurrentTask);
                ChangeTask(null);
            }
            if (PlanSubscriber != null)
            {
                PlanSubscriber.Service.RemoveSubscriber(PlanSubscriber);
            }
            base.Delete();
        }


        public void Chat()
        {
            var Employee = this;
            Func<string> get_status = () => Employee.Stats.GetStatusAdjective();

            Employee.World.Paused = true;
            // Prepare conversation memory for an envoy conversation.
            var cMem = Employee.World.ConversationMemory;
            cMem.SetValue("$world", new Yarn.Value(Employee.World));
            cMem.SetValue("$employee", new Yarn.Value(Employee));
            cMem.SetValue("$employee_name", new Yarn.Value(Employee.Stats.FullName));
            cMem.SetValue("$employee_status", new Yarn.Value(get_status()));
            var timeOfDay = "Morning";
            int hour = Employee.World.Time.CurrentDate.Hour;
            if (hour > 12)
            {
                timeOfDay = "Afternoon";
            }

            if (hour > 16)
            {
                timeOfDay = "Evening";
            }
            cMem.SetValue("$time_of_day", new Yarn.Value(timeOfDay));
            cMem.SetValue("$is_asleep", new Yarn.Value(Employee.Stats.IsAsleep));
            cMem.SetValue("$is_on_strike", new Yarn.Value(Employee.Stats.IsOnStrike));

            if (Employee.Creature.Physics.GetComponent<DwarfThoughts>().HasValue(out var thoughts))
            {
                cMem.SetValue("$grievences", new Yarn.Value(TextGenerator.GetListString(thoughts.Thoughts.Where(thought => thought.HappinessModifier < 0).Select(thought => thought.Description))));
                cMem.SetValue("$good_things", new Yarn.Value(TextGenerator.GetListString(thoughts.Thoughts.Where(thought => thought.HappinessModifier >= 0).Select(thought => thought.Description))));
            }
            else
            {
                cMem.SetValue("$grievences", new Yarn.Value(""));
                cMem.SetValue("$good_things", new Yarn.Value(""));

            }

            String[] personalities = { "happy", "grumpy", "anxious" };
            var myRandom = new Random(Employee.Stats.RandomSeed);
            cMem.SetValue("$personality", new Yarn.Value(personalities[myRandom.Next(0, personalities.Length)]));
            cMem.SetValue("$motto", new Yarn.Value(Employee.World.PlayerFaction.Economy.Information.Motto));
            cMem.SetValue("$company_name", new Yarn.Value(Employee.World.PlayerFaction.Economy.Information.Name));
            cMem.SetValue("$employee_task", new Yarn.Value(Employee.CurrentTask == null ? "Nothing" : Employee.CurrentTask.Name));
            cMem.SetValue("$employee_class", new Yarn.Value(Employee.Stats.CurrentClass.Name));
            var injuries = TextGenerator.GetListString(Employee.Creature.Stats.Buffs.OfType<Disease>().Select(disease => disease.Name));
            if (injuries == "")
            {
                injuries = "no problems";
            }
            cMem.SetValue("$injuries", new Yarn.Value(injuries));
            cMem.SetValue("$employee_pay", new Yarn.Value((float)(decimal)Employee.Stats.CurrentLevel.Pay));
            cMem.SetValue("$employee_bonus", new Yarn.Value(4 * (float)(decimal)Employee.Stats.CurrentLevel.Pay));
            cMem.SetValue("$company_money", new Yarn.Value((float)(decimal)Employee.Faction.Economy.Funds));

            if (Employee.Physics.GetComponent<Flammable>().HasValue(out var flames))
                cMem.SetValue("$is_on_fire", new Yarn.Value(flames.IsOnFire));
            else
                cMem.SetValue("$is_on_fire", new Yarn.Value(false));

            var state = new YarnState(World, ContentPaths.employee_conversation, "Start", cMem);
            state.AddEmployeePortrait(Employee);
            state.SetVoicePitch(Employee.Stats.VoicePitch);
            GameStateManager.PushState(state);
        }
    }
}
