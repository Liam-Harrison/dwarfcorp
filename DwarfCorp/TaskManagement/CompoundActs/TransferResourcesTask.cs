using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class TransferResourcesTask : Task
    {
        public string StockpileFrom;
        private Stockpile stockpile;
        public ResourceAmount Resources;
        public ZoneBuilder Builder;

        public TransferResourcesTask()
        {

        }

        public TransferResourcesTask(string stockpile, ResourceAmount resources, ZoneBuilder Builder)
        {
            Priority = PriorityType.Medium;
            StockpileFrom = stockpile;
            Resources = resources;
            Name = String.Format("Transfer {0} {1} from {2}", Resources.Count, Resources.Type, stockpile);
            AutoRetry = true;
            ReassignOnDeath = true;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (!GetStockpile(agent.Faction))
                return 9999;

            return (stockpile.GetBoundingBox().Center() - agent.AI.Position).LengthSquared();
        }

        public bool GetStockpile(Faction faction)
        {
            stockpile = Builder.FindZone(StockpileFrom) as Stockpile;
            return stockpile != null;
        }

        public override bool IsComplete(Faction faction)
        {
            if (!GetStockpile(faction))
                return true;

            return !stockpile.Resources.HasResources(Resources);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || agent.Stats.IsAsleep || !agent.Active)
            {
                return Feasibility.Infeasible;
            }

            if (!GetStockpile(agent.Faction))
            {
                return Feasibility.Infeasible;
            }

            if (stockpile.Resources.HasResource(Resources))
            {
                return Feasibility.Feasible;
            }

            return Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature agent)
        {
            if (!GetStockpile(agent.Faction))
            {
                return null;
            }

            return new TransferResourcesAct(agent.AI, stockpile, Resources) { Name = "Transfer Resources" };
        }
    }
}