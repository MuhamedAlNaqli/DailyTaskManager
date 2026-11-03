using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DailyTaskManager.UI.Tests;

/// <summary>Small file rules of the consultant app: a document is never overwritten, and a path is inside a folder only when it really is</summary>
[TestClass]
public class FileNamingAndPathTests
{
    [TestMethod]
    public void ADocumentThatExists_IsNeverReplaced()
    {
        var folder = Path.Combine(Path.GetTempPath(), "dtm-docs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var first = Path.Combine(folder, "Faktura.docx");
            Assert.AreEqual(first, DailyTaskManager.Infrastructure.Services.DocumentGenerator.FreeName(first));

            File.WriteAllText(first, "edited by hand");
            Assert.AreEqual(Path.Combine(folder, "Faktura (2).docx"), DailyTaskManager.Infrastructure.Services.DocumentGenerator.FreeName(first));

            File.WriteAllText(Path.Combine(folder, "Faktura (2).docx"), "x");
            Assert.AreEqual(Path.Combine(folder, "Faktura (3).docx"), DailyTaskManager.Infrastructure.Services.DocumentGenerator.FreeName(first));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void APathIsInsideAFolderOnlyWhenItReallyIs()
    {
        var folder = Path.Combine(Path.GetTempPath(), "images");
        Assert.IsTrue(DailyTaskManager.Infrastructure.Services.BusinessService.IsInside(folder, Path.Combine(folder, "a.png")));
        Assert.IsFalse(DailyTaskManager.Infrastructure.Services.BusinessService.IsInside(folder, folder + "-other" + Path.DirectorySeparatorChar + "a.png"), "a folder with the same beginning");
        Assert.IsFalse(DailyTaskManager.Infrastructure.Services.BusinessService.IsInside(folder, Path.Combine(folder, "..", "secret.png")), "a way out through ..");
    }
}
