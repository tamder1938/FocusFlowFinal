using System.Threading.Tasks;

namespace FocusFlowFinal.Services.Security;

public interface ISecureStorage
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
}
