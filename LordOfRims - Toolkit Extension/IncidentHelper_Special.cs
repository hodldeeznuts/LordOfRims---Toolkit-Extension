using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchToolkit;
using TwitchToolkit.Incidents;
using TwitchToolkit.PawnQueue;
using TwitchToolkit.Store;
using Verse;

namespace LordOfRims___Toolkit_Extension
{
    public class BuyDwarf : IncidentHelperVariables
    {
        public override bool IsPossible(string message, Viewer viewer, bool separateChannel = false)
        {
            if (!StoreIncidentMaker.CheckIfViewerHasEnoughCoins(viewer, this.storeIncident.cost, separateChannel)) return false;
            if(Current.Game.GetComponent<GameComponentPawns>().HasUserBeenNamed(viewer.username))
            {
                Toolkit.client.SendMessage($"@{viewer.username} you are already in the colony.", separateChannel);
                return false;
            }

            this.separateChannel = separateChannel;
            this.viewer = viewer;
            IIncidentTarget target = Helper.AnyPlayerMap;
            parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, target);
            map = (Map)parms.target;
        	
            bool cell = CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out loc);
            if (!cell) return false;
			return true;
        }

        public override void TryExecute()
        {
            PawnKindDef pawnKind = PawnKindDef.Named("LotRD_DwarfColonist");
			Faction ofPlayer = Faction.OfPlayer;
			bool pawnMustBeCapableOfViolence = true;
			PawnGenerationRequest request = new PawnGenerationRequest(pawnKind, ofPlayer, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, pawnMustBeCapableOfViolence, 20f, false, true, true, false, false, false, false, null, null, null, null, null, null, null, null);
			Pawn pawn = PawnGenerator.GeneratePawn(request);
            NameTriple old = pawn.Name as NameTriple;
            pawn.Name = new NameTriple(old.First, viewer.username, old.Last);
			GenSpawn.Spawn(pawn, loc, map, WipeMode.Vanish);
			string label = "Viewer Joins";
			string text = $"A new pawn has been purchased by {viewer.username}, let's welcome them to the colony.";
			PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
			Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn, null, null);

            Current.Game.GetComponent<GameComponentPawns>().AssignUserToPawn(viewer.username, pawn);
            viewer.TakeViewerCoins(this.storeIncident.cost);
            viewer.SetViewerKarma(Karma.CalculateNewKarma(viewer.GetViewerKarma(), this.storeIncident.karmaType, this.storeIncident.cost));
            StorePurchaseLogger.LogPurchase(new Purchase(viewer.username, "", this.storeIncident.karmaType, this.storeIncident.cost, "", DateTime.Now));

            Toolkit.client.SendMessage($"@{viewer.username} has purchased a pawn and is joining the colony.", separateChannel);
        }

        private IntVec3 loc;
        private Map map = null;
        private Viewer viewer = null;
        private IncidentParms parms = null;
        private bool separateChannel = false;
    }
}
