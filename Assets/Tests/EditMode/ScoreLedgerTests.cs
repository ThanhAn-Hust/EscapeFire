using System;
using NUnit.Framework;
using EscapeFire.Analytics;

namespace EscapeFire.Tests
{
    public class ScoreLedgerTests
    {
        [Test]
        public void AddAndDeduct_UpdateTotalAndEntries()
        {
            var ledger = new ScoreLedger();
            ledger.Add(20, "Ngắt Aptomat", 5f);
            ledger.Deduct(15, "Mở cửa vội", 30f);

            Assert.AreEqual(5, ledger.Total);
            Assert.AreEqual(2, ledger.Entries.Count);
            Assert.AreEqual(-15, ledger.Entries[1].Delta);
        }

        [Test]
        public void NegativePoints_Throw()
        {
            var ledger = new ScoreLedger();
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Add(-1, "x", 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Deduct(-1, "x", 0f));
        }

        [Test]
        public void Format_MatchesLegacyLogFormat()
        {
            var ledger = new ScoreLedger();
            ledger.Add(10, "Báo cháy", 3.26f);
            ledger.Deduct(15, "Mở cửa", 12f);

            Assert.AreEqual("[+10 pts] Báo cháy (Time: 3.3s)", ScoreLedger.Format(ledger.Entries[0]));
            Assert.AreEqual("[-15 pts] Mở cửa (Time: 12.0s)", ScoreLedger.Format(ledger.Entries[1]));
        }
    }
}
