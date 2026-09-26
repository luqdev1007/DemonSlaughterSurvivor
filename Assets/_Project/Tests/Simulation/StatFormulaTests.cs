using Game.Core;
using Game.Simulation.Services;
using NUnit.Framework;
using System;

namespace Game.Simulation.Tests
{
    public sealed class StatFormulaTests
    {
        private const float BaseDamage = 10f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void TwoIncreasedUpgradesAddUp()
        {
            StatTotals totals = StatTotals.Neutral;

            StatFormula.Accumulate(ref totals, StatOp.Increased, 0.2f);
            StatFormula.Accumulate(ref totals, StatOp.Increased, 0.2f);

            Assert.AreEqual(14.0f, StatFormula.Evaluate(BaseDamage, totals), Tolerance);
        }

        [Test]
        public void RageMultipliesOnTopOfIncreased()
        {
            StatTotals totals = StatTotals.Neutral;

            StatFormula.Accumulate(ref totals, StatOp.Increased, 0.2f);
            StatFormula.Accumulate(ref totals, StatOp.Increased, 0.2f);
            StatFormula.Accumulate(ref totals, StatOp.More, 0.3f);

            Assert.AreEqual(18.2f, StatFormula.Evaluate(BaseDamage, totals), Tolerance);
        }

        [Test]
        public void FiveIncreasedUpgradesAndRage()
        {
            StatTotals totals = StatTotals.Neutral;

            for (int upgrade = 0; upgrade < 5; upgrade++)
                StatFormula.Accumulate(ref totals, StatOp.Increased, 0.2f);

            StatFormula.Accumulate(ref totals, StatOp.More, 0.3f);

            Assert.AreEqual(26.0f, StatFormula.Evaluate(BaseDamage, totals), Tolerance);
        }

        [Test]
        public void FlatIsAddedBeforePercentages()
        {
            StatTotals totals = StatTotals.Neutral;

            StatFormula.Accumulate(ref totals, StatOp.Flat, 5f);
            StatFormula.Accumulate(ref totals, StatOp.Flat, 5f);
            StatFormula.Accumulate(ref totals, StatOp.Increased, 0.5f);
            StatFormula.Accumulate(ref totals, StatOp.More, 1f);
            StatFormula.Accumulate(ref totals, StatOp.More, -0.5f);

            Assert.AreEqual(30f, StatFormula.Evaluate(BaseDamage, totals), Tolerance);
        }

        [Test]
        public void NeutralTotalsReturnTheBaseBitForBit()
        {
            float[] bases = { 2.5f, 1f, 100f, 0.33f, 10f, 20f, 60f };

            for (int index = 0; index < bases.Length; index++)
            {
                float result = StatFormula.Evaluate(bases[index], StatTotals.Neutral);

                Assert.AreEqual(BitConverter.SingleToInt32Bits(bases[index]), BitConverter.SingleToInt32Bits(result));
            }
        }

        [Test]
        public void UnknownOperationThrows()
        {
            StatTotals totals = StatTotals.Neutral;

            Assert.Throws<ArgumentOutOfRangeException>(() => StatFormula.Accumulate(ref totals, (StatOp)99, 1f));
        }
    }
}
