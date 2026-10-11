using AkariTools.Framework.Models;

namespace AkariTools.Framework.Services;

public interface IHardwareService
{
    Task<HardwareInfo> LoadHardwareInfoAsync();
}
