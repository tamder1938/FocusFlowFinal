using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public interface IWorkoutInitService
{
    Task EnsureSeededAsync();
}
