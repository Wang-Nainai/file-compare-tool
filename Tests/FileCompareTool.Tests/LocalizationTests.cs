using System;
using System.Linq;
using System.Threading.Tasks;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class LocalizationTests
{
    public static Task CatalogsHaveMatchingKeys()
    {
        var chinese = LocalizationService.GetCatalogKeys(LocalizationService.ChineseSimplified);
        var english = LocalizationService.GetCatalogKeys(LocalizationService.English);

        Assert.True(chinese.Count > 100, "中文资源应包含完整界面文本");
        Assert.Equal(chinese.Count, english.Count, "中英文资源键数量应一致");

        for (var i = 0; i < chinese.Count; i++)
            Assert.Equal(chinese[i], english[i], "中英文资源键应一一对应");

        return Task.CompletedTask;
    }

    public static Task SwitchingLanguageChangesFormattedText()
    {
        var original = LocalizationService.CurrentLanguage;
        try
        {
            LocalizationService.SetLanguage(LocalizationService.English, persist: false);

            Assert.Equal(LocalizationService.English, LocalizationService.CurrentLanguage,
                "应切换到英语");
            Assert.Equal("A · Reference files", LocalizationService.T("Main_A_Section"),
                "界面资源应切换为英文");
            Assert.Equal("File not found: sample.txt",
                LocalizationService.Format("Error_FileMissingFormat", "sample.txt"),
                "格式化错误信息应使用英文");
            Assert.True(
                LocalizationService.Format("Status_ScanCompleteFormat", 5, 7, 2).Contains("Completed"),
                "扫描完成状态应使用英文");

            LocalizationService.SetLanguage(LocalizationService.ChineseSimplified, persist: false);
            Assert.Equal("A · 参考文件", LocalizationService.T("Main_A_Section"),
                "应能切回中文");
            Assert.Equal("文件不存在：sample.txt",
                LocalizationService.Format("Error_FileMissingFormat", "sample.txt"),
                "格式化错误信息应切回中文");
        }
        finally
        {
            LocalizationService.SetLanguage(original, persist: false);
        }

        return Task.CompletedTask;
    }
}
