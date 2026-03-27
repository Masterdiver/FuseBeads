using FuseBeads.Resources.Strings;

namespace FuseBeads.Helpers;

[ContentProperty(nameof(Key))]
public class LocalizeExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = string.Empty;

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return AppResources.ResourceManager.GetString(Key, AppResources.Culture) ?? Key;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
