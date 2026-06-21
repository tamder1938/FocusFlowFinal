using FocusFlowFinal.Models.Sound;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface ISoundRepository
{
    IEnumerable<UserSound> GetAll();
    int   Upsert(UserSound sound);
    void  Delete(int id);
}
