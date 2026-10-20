# Contributing

Thanks for taking the time to improve File Compare Tool.

## Before opening a pull request

1. Fork the repository and create a focused branch.
2. Build the project:

   ```powershell
   dotnet build FileCompareTool.csproj -c Debug
   ```

3. Run the full test harness:

   ```powershell
   dotnet run --project Tests/FileCompareTool.Tests/FileCompareTool.Tests.csproj -c Debug
   ```

4. For changes to deletion, scanning or path validation, describe the safety impact in the pull request.

## Safety invariants

These behaviors must not regress:

- A files are never deleted.
- Only files captured in the current B scan snapshot can be deleted.
- Overlapping A/B paths are rejected.
- Deletion is confirmed and uses the Windows Recycle Bin.

## Localization

The UI supports Simplified Chinese and English. When adding visible text:

- Add the key to both catalogs in `Services/LocalizationService.cs`.
- Use `DynamicResource` for XAML text.
- Use `LocalizationService.T` / `Format` for code-generated messages.
- Update tests if the resource catalog changes.
