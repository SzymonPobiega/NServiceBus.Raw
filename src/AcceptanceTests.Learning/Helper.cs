using System;
using System.IO;
using NServiceBus;
using NUnit.Framework;

public static class Helper
{
    public static void ConfigureLearning(this TransportExtensions<LearningTransport> extensions)
    {
        var testRunId = TestContext.CurrentContext.Test.ID;

        string tempDir;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            //can't use bin dir since that will be too long on the build agents
            tempDir = @"c:\temp";
        }
        else
        {
            tempDir = Path.GetTempPath();
        }

        var storageDir = Path.Combine(tempDir, testRunId);

        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, true);
        }

        extensions.StorageDirectory(storageDir);
    }
}