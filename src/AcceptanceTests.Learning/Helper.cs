using NServiceBus;
using NUnit.Framework;
using System;
using System.IO;

public static class Helper
{
    public static LearningTransport ConfigureLearning()
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

        return new LearningTransport
        {
            StorageDirectory = storageDir
        };
    }
}