using System;
using NUnit.Framework;
using VideoClient.Util;

namespace VideoClient
{
    public class Test
    {
        [Test]
        public static void TestCheckEnv()
        {
            Console.WriteLine(EnvCheckUtil.GetEnvironmentInfo());
            Console.WriteLine(
                $@"is os app same bit > {(Environment.Is64BitOperatingSystem == Environment.Is64BitProcess)}");
        }
    }
}