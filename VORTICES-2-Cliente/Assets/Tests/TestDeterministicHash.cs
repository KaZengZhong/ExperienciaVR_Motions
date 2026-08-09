using NUnit.Framework;
using UnityEngine;

public class TestDeterministicHash
{
    private static int DeterministicHash(string s)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in s)
                hash = hash * 31 + c;
            return Mathf.Abs(hash);
        }
    }

    [Test]
    public void MismoNombre_MismaSemilla()
    {
        int seed1 = DeterministicHash("sesion1");
        int seed2 = DeterministicHash("sesion1");
        Assert.AreEqual(seed1, seed2);
    }

    [Test]
    public void NombresDistintos_SemillasDistintas()
    {
        int seed1 = DeterministicHash("sesion1");
        int seed2 = DeterministicHash("sesion2");
        Assert.AreNotEqual(seed1, seed2);
    }
}
