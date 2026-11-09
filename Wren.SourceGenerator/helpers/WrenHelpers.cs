namespace Wren.SourceGenerator.helpers;

public class WrenHelpers
{
    public static string ConvertNameToWrenStyle(string csharpName)
    {
        if (string.IsNullOrEmpty(csharpName))
            return csharpName;

        return char.ToLowerInvariant(csharpName[0]) + csharpName.Substring(1);
    }
}
