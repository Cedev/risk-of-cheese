using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AmmoLocker
{
    public static class StockTracer
    {
        private static int tracingStocks = 0;

        [ConCommand(commandName = "cheeseboard_ammolocker_tracestock", flags = ConVarFlags.None, helpText = "Start tracing changes to skill stocks")]
        public static void TraceStock(ConCommandArgs args)
        {
            var tracing = Interlocked.Exchange(ref tracingStocks, 1);
            if (tracing != 0) { return; }

            Tracer.Instance.Format<GenericSkill>(gs => string.Format("{0} {{stacks: {1}/{2}}}", gs, gs.stock, gs.maxStock));
            Tracer.Instance.Filter<GenericSkill>(gs => gs.characterBody && gs.characterBody.isPlayerControlled);

            On.RoR2.GenericSkill.AddOneStock += LogStock<On.RoR2.GenericSkill.hook_AddOneStock>();
            On.RoR2.GenericSkill.ApplyAmmoPack += LogStock<On.RoR2.GenericSkill.hook_ApplyAmmoPack>();
            On.RoR2.GenericSkill.AssignSkill += LogStock<On.RoR2.GenericSkill.hook_AssignSkill>();
            On.RoR2.GenericSkill.DeductStock += LogStock<On.RoR2.GenericSkill.hook_DeductStock>();
            On.RoR2.GenericSkill.RemoveAllStocks += LogStock<On.RoR2.GenericSkill.hook_RemoveAllStocks>();
            On.RoR2.GenericSkill.Reset += LogStock<On.RoR2.GenericSkill.hook_Reset>();
            On.RoR2.GenericSkill.RestockContinuous += LogStock<On.RoR2.GenericSkill.hook_RestockContinuous>();
            On.RoR2.GenericSkill.RestockSteplike += LogStock<On.RoR2.GenericSkill.hook_RestockSteplike>();
            On.RoR2.GenericSkill.Start += LogStock<On.RoR2.GenericSkill.hook_Start>();
            On.RoR2.GenericSkill.OnExecute += LogStock<On.RoR2.GenericSkill.hook_OnExecute>();

            On.RoR2.Skills.SkillDef.OnExecute += LogStock<On.RoR2.Skills.SkillDef.hook_OnExecute>();

            On.RoR2.GenericSkill.RecalculateMaxStock += LogStock<On.RoR2.GenericSkill.hook_RecalculateMaxStock>();
        }

        private static T LogStock<T>()
        {
            return Tracing.Trace<T>(Tracer.Instance);
        }
    }
}
