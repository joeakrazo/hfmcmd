using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

#if !LATE_BIND
using HSVCALCULATELib;
#endif
using HFMCONSTANTSLib;

using Command;
using HFMCmd;


namespace HFM
{

    public enum EConsolidationType : short
    {
        All = tagCONSOLIDATIONTYPE.CONSOLIDATE_ALL,
        AllWithData = tagCONSOLIDATIONTYPE.CONSOLIDATE_ALLWITHDATA,
        EntityOnly = tagCONSOLIDATIONTYPE.CONSOLIDATE_ENTITYONLY,
        ForceEntityOnly = tagCONSOLIDATIONTYPE.CONSOLIDATE_FORCEENTITYONLY,
        Impacted = tagCONSOLIDATIONTYPE.CONSOLIDATE_IMPACTED
    }


    /// <summary>
    /// Wraps the HsvCalculate module, exposing its functionality for performing
    /// calculations, allocations, translations, consolidations etc.
    /// </summary>
    public class Calculate
    {
        // Reference to class logger
        protected static readonly ILog _log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        // Reference to the Session object
        private readonly Session _session;
        // Reference to a Metadata object
        private readonly Metadata _metadata;
        // Reference to HFM HsvCalculate COM object
#if LATE_BIND
        private readonly dynamic _hsvCalculate;
#else
        private readonly HsvCalculate _hsvCalculate;
#endif


        public Calculate(Session session)
        {
            _log.Trace("Constructing Calculate object");
            _session = session;
            _metadata = session.Metadata;
#if LATE_BIND
            _hsvCalculate = session.HsvSession.Calculate;
#else
            _hsvCalculate = (HsvCalculate)session.HsvSession.Calculate;
#endif
        }


        [Command("Performs an allocation")]
        public void Allocate(
                [Parameter("The scenario(s) in which to perform the allocation",
                           Alias = "Scenario")]
                IEnumerable<string> scenarios,
                [Parameter("The year(s) for which to perform the allocation",
                           Alias = "Year")]
                IEnumerable<string> years,
                [Parameter("The period(s) over which the allocation should be performed",
                           Alias = "Period")]
                IEnumerable<string> periods,
                [Parameter("The entity member(s) for which the allocation should be performed",
                           Alias = "Entity")]
                IEnumerable<string> entities,
                [Parameter("The value member(s) for which the allocation should be peformed",
                           Alias = "Value")]
                IEnumerable<string> values,
                IOutput output)
        {
            var ops = _metadata.DoSubcubeOp("Allocating", scenarios, years, periods, entities, values, output,
                                            (pov) => _hsvCalculate.Allocate(pov.Scenario.Id, pov.Year.Id, pov.Period.Id,
                                                         pov.Entity.Id, pov.Entity.ParentId, pov.Value.Id));
            _log.InfoFormat("Allocate completed: {0} performed", ops);
        }


        [Command("Performs a calculation", Name = "Calculate")]
        public void ChartLogic(
                [Parameter("The scenario(s) in which to perform the calculation",
                           Alias = "Scenario")]
                IEnumerable<string> scenarios,
                [Parameter("The year(s) for which to perform the calculation",
                           Alias = "Year")]
                IEnumerable<string> years,
                [Parameter("The period(s) over which the calculation should be performed",
                           Alias = "Period")]
                IEnumerable<string> periods,
                [Parameter("The entities for which the calculation should be performed",
                           Alias = "Entity")]
                IEnumerable<string> entities,
                [Parameter("The value member(s) for which the calculation should be peformed",
                           Alias = "Value")]
                IEnumerable<string> values,
                [Parameter("Flag indicating whether to force a calculation when not needed",
                           DefaultValue = false)]
                bool force,
                IOutput output)
        {
            var ops = _metadata.DoSubcubeOp("Caclulating", scenarios, years, periods, entities, values, output,
                                            (pov) => _hsvCalculate.ChartLogic(pov.Scenario.Id, pov.Year.Id, pov.Period.Id,
                                                         pov.Entity.Id, pov.Entity.ParentId, pov.Value.Id, force));
            _log.InfoFormat("Calculate completed: {0} performed", ops);
        }


        [Command("Performs a translation")]
        public void Translate(
                [Parameter("The scenario(s) in which to perform the translation",
                           Alias = "Scenario")]
                IEnumerable<string> scenarios,
                [Parameter("The year(s) for which to perform the translation",
                           Alias = "Year")]
                IEnumerable<string> years,
                [Parameter("The period(s) over which the translation should be performed",
                           Alias = "Period")]
                IEnumerable<string> periods,
                [Parameter("The entities for which the translation should be performed",
                           Alias = "Entity")]
                IEnumerable<string> entities,
                [Parameter("The value member(s) for which the translation should be peformed",
                           Alias = "Value")]
                IEnumerable<string> values,
                [Parameter("Flag indicating whether to force a translation when not needed",
                           DefaultValue = false)]
                bool force,
                IOutput output)
        {
            var ops = _metadata.DoSubcubeOp("Translating", scenarios, years, periods, entities, values, output,
                                            (pov) => _hsvCalculate.Translate(pov.Scenario.Id, pov.Year.Id, pov.Period.Id,
                                                         pov.Entity.Id, pov.Entity.ParentId, pov.Value.Id, force, true));
            _log.InfoFormat("Translate completed: {0} performed", ops);
        }


        [Command("Performs a consolidation")]
        public void Consolidate(
                [Parameter("The scenario(s) in which to perform the consolidation",
                           Alias = "Scenario")]
                IEnumerable<string> scenarios,
                [Parameter("The year(s) for which to perform the consolidation",
                           Alias = "Year")]
                IEnumerable<string> years,
                [Parameter("The period(s) over which the consolidation should be performed",
                           Alias = "Period")]
                IEnumerable<string> periods,
                [Parameter("The entities for which the consolidation should be performed",
                           Alias = "Entity")]
                IEnumerable<string> entities,
                [Parameter("The type of consolidation to perform",
                           DefaultValue = EConsolidationType.Impacted)]
                EConsolidationType consolidationType,
                IOutput output)
        {
            int consols = 0, skipped = 0;
            var slice = new Slice(_metadata);
            slice[EDimension.Scenario] = scenarios;
            slice[EDimension.Year] = years;
            slice[EDimension.Period] = periods;
            slice[EDimension.Entity] = entities;

            // Calculate number of iterations, and measure progress
            var POVs = slice.Combos;
            output.InitProgress("Consolidating", POVs.Length);
            foreach(var pov in POVs) {
                if(output.Cancelled) {
                    break;
                }
                if(ConsolidatePOV(pov, consolidationType, output)) { consols++; }
                else { skipped++; }
            }
            output.EndProgress();
            if(consolidationType == EConsolidationType.Impacted) {
                _log.InfoFormat("Consolidation completed: {0} performed, {1} not needed",
                                consols, skipped);
            }
            else {
                _log.InfoFormat("Consolidation completed: {0} performed", consols);
            }
        }


#if HFM_11_1_2_2
        [Command("Performs an Equity Pick-up adjustment calculation",
                 Since = "11.1.2.2")]
        public void CalculateEPU(
                [Parameter("The scenario in which to perform the equity pick-up",
                           Alias = "Scenario")]
                IEnumerable<string> scenarios,
                [Parameter("The year for which to perform the equity pick-up",
                           Alias = "Year")]
                IEnumerable<string> years,
                [Parameter("The period(s) over which the equity pick-up should be performed",
                           Alias = "Period")]
                IEnumerable<string> periods,
                [Parameter("Flag indicating whether to recalculate equity pick-up for all owner-owned pairs, " +
                           "or only those pairs that have been impacted",
                           DefaultValue = false)]
                bool force,
                IOutput output)
        {
            int ops = 0;
            var slice = new Slice(_metadata);
            slice[EDimension.Scenario] = scenarios;
            slice[EDimension.Year] = years;
            slice[EDimension.Period] = periods;

            // Calculate number of iterations, and measure progress
            var POVs = slice.Combos;
            output.InitProgress("Equity Pick-Up", POVs.Length);
            foreach(var pov in POVs) {
                _log.FineFormat("Equity Pick-Up for {0}", pov);
                HFM.Try(() => _hsvCalculate.CalcEPU(pov.Scenario.Id, pov.Year.Id, pov.Period.Id, force));
                ops++;
                if(output.IterationComplete()) {
                    break;
                }
            }
            output.EndProgress();
            _log.InfoFormat("Equity Pick-Up completed: {0} performed", ops);
        }
#endif


        /// Calculates a Scenario/Year/Period/Entity combination specified in
        /// the POV
        internal void CalculatePOV(POV pov, bool force)
        {
            _log.FineFormat("Calculating {0}", pov);
            HFM.Try(() => _hsvCalculate.ChartLogic(pov.Scenario.Id, pov.Year.Id, pov.Period.Id,
                                                   pov.Entity.Id, pov.Entity.ParentId, pov.Value.Id,
                                                   force));
        }


        /// Consolidates a Scenario/Year/Period/Entity combination specified in
        /// the POV
        internal bool ConsolidatePOV(POV pov, EConsolidationType consolidationType, IOutput output)
        {
            var si = _session.SystemInfo;

            if(consolidationType != EConsolidationType.Impacted ||
               ECalcStatus.NeedsConsolidation.IsSet(_session.Data.GetCalcStatus(pov))) {
                _log.FineFormat("Consolidating {0}", pov);
                si.MonitorBlockingTask(output);
                HFM.Try(() => _hsvCalculate.Consolidate(pov.Scenario.Id, pov.Year.Id, pov.Period.Id,
                                                        pov.Entity.Id, pov.Entity.ParentId,
                                                        (short)consolidationType));
                si.BlockingTaskComplete();
                return true;
            }
            else {
                _log.FineFormat("Consolidation not needed for {0}", pov);
                return false;
            }

        }
    }
}
