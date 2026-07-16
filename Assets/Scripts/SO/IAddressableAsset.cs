using System.Collections.Generic;

public interface IAddressableAsset
{
    public int ID { get; }
    public string DisplayName { get; }
    public List<string> Labels { get; }
}